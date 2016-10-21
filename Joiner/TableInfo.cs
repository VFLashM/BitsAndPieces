using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Joiner
{
    class TableInfo
    {
        private List<string> id;
        private Urn urn = null;
        private string alias;
        private List<string> columns = null;
        private List<string> primaryKey = null;

        public TableInfo(List<string> id, string alias)
        {
            this.id = id;
            this.alias = alias;
        }

        public string[] GetId()
        {
            return id.ToArray();
        }

        public string Database()
        {
            return id.Count >= 3 ? id[0] : null;
        }

        public List<string> Columns()
        {
            return columns;
        }

        public List<string> PrimaryKey()
        {
            return primaryKey;
        }

        public void Bind(TableViewBase table)
        {
            this.urn = table.Urn;
            this.columns = new List<string>();
            foreach (Column col in table.Columns)
            {
                this.columns.Add(col.Name);
            }
            foreach (Index ind in table.Indexes)
            {
                if (ind.IndexKeyType == IndexKeyType.DriPrimaryKey)
                {
                    this.primaryKey = new List<string>();
                    foreach (IndexedColumn col in ind.IndexedColumns)
                    {
                        this.primaryKey.Add(col.Name);
                    }
                }
            }
        }

        public string Alias()
        {
            return alias ?? id.Last();
        }

        public string Def()
        {
            return String.Join(".", id) + (alias != null ? (" as " + alias) : "");
        }

        /*
        public TableInfo Renamed(string newAlias)
        {
            var renamed = new TableInfo(id, newAlias);
            renamed.urn = urn;
            renamed.columns = columns;
            renamed.primaryKey = primaryKey;
            return renamed;
        }
         */

        public bool Match(TableInfo other)
        {
            if (other == null)
            {
                return false;
            }
            if (urn != null || other.urn != null)
            {
                return urn == other.urn;
            }
            return id == other.id;
        }
    }
}
