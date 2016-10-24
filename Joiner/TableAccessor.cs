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
            if (!_server.Databases.Contains(_defaultDatabase))
            {
                _defaultDatabase = Common.Connection.GetActiveDatabase(null);
            }
            _dbForeignKeyRules = new Dictionary<string, List<Rule>>();
        }

        TableViewBase FindTable(Database db, string schema, string name)
        {
            foreach (Table tab in db.Tables)
            {
                if ((name == tab.Name) && (String.IsNullOrEmpty(schema) || (tab.Schema == schema)))
                {
                    return tab;
                }
            }
            foreach (View tab in db.Views)
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

       OBJECT_SCHEMA_NAME(fk.parent_object_id) as base_schema, 
       OBJECT_NAME(fk.parent_object_id) as base_name, 
       basecol.name as base_column,

       OBJECT_SCHEMA_NAME(fk.referenced_object_id) as ref_schema, 
       OBJECT_NAME(fk.referenced_object_id) as ref_name, 
       refcol.name as ref_column
from sys.foreign_keys fk
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
            Rule.Builder builder = null;
            TableInfo baseTable = null;
            TableInfo refTable = null;
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
                    if (builder != null)
                    {
                        rules.Add(builder.Finish());
                    }
                    lastFk = fkId;
                    baseTable = CreateTableInfo(database, FindTable(db, baseSchema, baseName));
                    refTable = CreateTableInfo(database, FindTable(db, refSchema, refName));
                    builder = new Rule.Builder(baseTable, refTable, fkName);
                }
                builder.Add(baseColumn, refColumn);
            }
            if (builder != null)
            {
                rules.Add(builder.Finish());
            }
            
            return rules;
        }

        public List<Rule> GetForeignKeyRules(string database)
        {
            List<Rule> rules;
            database = database ?? _defaultDatabase;
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

        public TableInfo CreateTableInfo(string database, TableViewBase table)
        {
            var id = new List<string>();
            if (database != _defaultDatabase)
            {
                id.Add(database);
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
            var res = new TableInfo(id, AliasFromName(table.Name));
            res.Bind(table);
            return res;
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
            TableViewBase tab = FindTable(db, schema, id.Last());
            if (tab != null)
            {
                t.Bind(tab);
            }
            return tab != null;
        }
    }
}
