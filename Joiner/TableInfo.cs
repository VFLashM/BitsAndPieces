using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using System.Text.RegularExpressions;

namespace Joiner
{
    class TableInfo
    {
        private List<string> id;
        private Urn urn = null;
        private string alias;
        private List<string> columns;
        private List<string> primaryKey = null;

        public TableInfo(List<string> id, string alias, List<string> columns = null)
        {
            this.id = id;
            this.alias = alias;
            this.columns = columns;
        }

        public string[] GetId()
        {
            return id != null ? id.ToArray() : null;
        }

        public string Database()
        {
            return (id != null && id.Count >= 3) ? id[0] : null;
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

        public void Bind(UserDefinedFunction udf)
        {
            this.urn = udf.Urn;
            this.columns = new List<string>();
            foreach (Column col in udf.Columns)
            {
                this.columns.Add(col.Name);
            }
        }

        public string Alias()
        {
            return alias ?? id.Last();
        }

        public string Def(string currentDatabase)
        {
            var localId = this.id.ToList();
            if (currentDatabase == Database())
            {
                localId.RemoveAt(0);
                if (String.IsNullOrWhiteSpace(localId[0]))
                {
                    localId.RemoveAt(0);
                }
            }
            return String.Join(".", localId) + ((alias != null) ? (" as " + alias) : "");
        }

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
            if (id != null || other.id != null)
            {
                return id == other.id;
            }
            return alias == other.alias;
        }

        static string MakeUnique(string value, List<string> others)
        {
            while (others.Contains(value))
            {
                var match = Regex.Match(value, @"([1-9][0-9]*)$");
                if (match.Success)
                {
                    var number = Convert.ToInt32(match.Groups[1].Value);
                    number += 1;
                    value = value.Substring(0, value.Length - match.Length);
                    value = value + number.ToString();
                }
                else
                {
                    value = value + "2";
                }
            }
            return value;
        }

        internal TableInfo NewWithUniqueAlias(List<string> usedAliases)
        {
            var currentAlias = alias ?? TableAccessor.AliasFromName(id.Last());
            var uniqueAlias = MakeUnique(currentAlias, usedAliases);

            var res = new TableInfo(id, uniqueAlias);
            res.urn = this.urn;
            res.columns = this.columns;
            res.primaryKey = this.primaryKey;
            return res;
        }
    }
}
