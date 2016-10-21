using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Diagnostics;

namespace Joiner
{
    class TableAccessor
    {
        Server _server;
        string _defaultDatabase;

        public TableAccessor(string defaultDatabase)
        {
            SqlConnectionInfo connectionInfo = Common.Connection.GetActiveConnectionInfo();
            ServerConnection connection = new ServerConnection(connectionInfo);
            connection.Connect();
            _server = new Server(connection);
            _defaultDatabase = defaultDatabase;
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

        public List<Rule> GetTableRules(Table table)
        {
            var rules = new List<Rule>();
            foreach (ForeignKey key in table.ForeignKeys)
            {
                string condition = "";

                Table refTable = null;
                foreach (Table other in table.Parent.Tables)
                {
                    if (other.Name == key.ReferencedTable && other.Schema == key.ReferencedTableSchema)
                    {
                        refTable = other;
                        break;
                    }
                }

                Index refKey = null;
                foreach (Index index in refTable.Indexes)
                {
                    if (index.Name == key.ReferencedKey)
                    {
                        refKey = index;
                        break;
                    }
                }

                int maxCount = Math.Max(key.Columns.Count, refKey.IndexedColumns.Count);
                for (int i = 0; i < maxCount; ++i)
                {
                    condition += AliasFromName(table.Name) + "." + key.Columns[i].Name + " == " +
                              AliasFromName(refTable.Name) + "." + refKey.IndexedColumns[i].Name;
                }
                
                rules.Add(new Rule(
                    CreateTableInfo(table), 
                    CreateTableInfo(refTable),
                    condition));
            }
            return rules;
        }

        public List<Rule> ResolveTable(TableInfo t)
        {
            string database = _defaultDatabase;
            var id = t.GetId();
            if (id.Length > 2)
            {
                database = id[0];
            }

            Database db = _server.Databases[database];
            foreach (Table tab in db.Tables)
            {
                if (tab.Name == id.Last())
                {
                    if (id.Length < 3 || id[1] == null || id[1] == tab.Schema)
                    {
                        t.SetUrn(tab.Urn);
                        return GetTableRules(tab);
                    }
                }
            }

            return null;
        }
    }
}
