using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Diagnostics;
using System.Data;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Joiner
{
    class TableAccessor
    {
        Server _server;
        string _defaultDatabase;
        Dictionary<string, List<Rule>> _dbForeignKeyRules;

        public TableAccessor(string defaultDatabase)
        {
            SqlConnectionInfo connectionInfo = Common.Connection.GetActiveConnectionInfo();
            ServerConnection connection = new ServerConnection(connectionInfo);
            connection.Connect();
            _server = new Server(connection);
            _defaultDatabase = defaultDatabase;
            _dbForeignKeyRules = new Dictionary<string, List<Rule>>();
        }

        Table FindTable(Database db, string schema, string name)
        {
            foreach (Table tab in db.Tables)
            {
                if ((name == tab.Name) && (String.IsNullOrEmpty(schema) || (tab.Schema == schema)))
                {
                    return tab;
                }
            }
            return null;
        }

        List<Rule> LoadForeignKeyRules(string database)
        {
            var rules = new List<Rule>();
            Database db = _server.Databases[database];
            var dataSet = db.ExecuteWithResults(@"
select fk.object_id as fk_id,
       SCHEMA_NAME(fk.schema_id) as fk_schema, 
       fk.name as fk_name, 

       SCHEMA_NAME(base.schema_id) as base_schema, 
       base.name as base_name, 
       basecol.name as base_column,

       SCHEMA_NAME(ref.schema_id) as ref_schema, 
       ref.name as ref_name, 
       refcol.name as ref_column
from sys.foreign_keys fk
join sys.tables base
  on base.object_id = fk.parent_object_id
join sys.tables ref
  on ref.object_id = fk.referenced_object_id
join sys.foreign_key_columns fkc
  on fkc.constraint_object_id = fk.object_id
join sys.columns basecol
  on basecol.object_id = fkc.parent_object_id
 and basecol.column_id = fkc.parent_column_id
join sys.columns refcol
  on refcol.object_id = fkc.referenced_object_id
 and refcol.column_id = fkc.referenced_column_id 
");
            var table = dataSet.Tables[0];
            int? lastFk = null;
            TableInfo baseTable = null;
            TableInfo refTable = null;
            string condition = null;
            foreach (DataRow row in table.Rows)
            {
                int? fkId = row["fk_id"] as int?;
                string fkSchema = row["fk_schema"] as string;
                string fkName = row["fk_name"] as string;

                string baseSchema = row["base_schema"] as string;
                string baseName = row["base_name"] as string;
                string baseColumn = row["base_column"] as string;

                string refSchema = row["ref_schema"] as string;
                string refName = row["ref_name"] as string;
                string refColumn = row["ref_column"] as string;

                if (lastFk != fkId)
                {
                    if (condition != null)
                    {
                        rules.Add(new Rule(baseTable, refTable, condition));
                    }
                    lastFk = fkId;
                    baseTable = CreateTableInfo(FindTable(db, baseSchema, baseName));
                    refTable = CreateTableInfo(FindTable(db, refSchema, refName));
                    condition = "";
                }
                if (!String.IsNullOrEmpty(condition))
                {
                    condition += " and ";
                }
                condition += baseTable.Alias() + "." + baseColumn + " = " + refTable.Alias() + "." + refColumn;
            }
            if (condition != null)
            {
                rules.Add(new Rule(baseTable, refTable, condition));
            }
            
            return rules;
        }

        public List<Rule> GetForeignKeyRules(string database)
        {
            List<Rule> rules;
            if (!_dbForeignKeyRules.TryGetValue(database, out rules))
            {
                rules = LoadForeignKeyRules(database);
                _dbForeignKeyRules[database] = rules;
            }
            return rules;
        }

        public string AliasFromName(string name)
        {
            string alias = "";
            bool lastLower = true;
            bool lastUnderscore = true;
            for (int i = 0; i < name.Length; ++i)
            {
                if ((Char.IsUpper(name[i]) && lastLower) || (Char.IsLetter(name[i]) && lastUnderscore))
                {
                    alias += Char.ToLower(name[i]);
                    lastLower = false;
                    lastUnderscore = false;
                }
                else if (name[i] == '_')
                {
                    lastUnderscore = true;
                }
                else if (Char.IsLower(name[i]))
                {
                    lastLower = true;
                }
            }
            return alias;
        }

        public TableInfo CreateTableInfo(Table table)
        {
            var id = new List<string>();
            if (table.Parent.Name != _defaultDatabase)
            {
                id.Add(table.Parent.Name);
            }
            if (table.Schema != "dbo")
            {
                id.Add(table.Schema);
            }
            else if (id.Count > 0)
            {
                id.Add("");
            }
            id.Add(table.Name);
            return new TableInfo(id, table.Urn, AliasFromName(table.Name));
        }

        public bool ResolveTable(TableInfo t)
        {
            string database = _defaultDatabase;
            string schema = null;
            var id = t.GetId();
            if (id.Length > 2)
            {
                database = id[0];
                schema = id[1];
            }

            Database db = _server.Databases[database];
            Table tab = FindTable(db, schema, id.Last());
            if (tab != null)
            {
                t.SetUrn(tab.Urn);
            }
            return tab != null;
        }
    }
}
