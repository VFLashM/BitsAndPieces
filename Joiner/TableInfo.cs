using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Joiner
{
    class TableInfo
    {
        private List<string> id;
        private Urn urn = null;
        private string alias;

        public TableInfo(List<string> id, string alias)
        {
            this.id = id;
            this.alias = alias;
        }

        public TableInfo(List<string> id, Urn urn, string alias)
        {
            this.urn = urn;
            this.id = id;
            this.alias = alias;
        }

        public string[] GetId()
        {
            return id.ToArray();
        }

        public void SetUrn(Urn urn)
        {
            this.urn = urn;
        }

        public string Alias()
        {
            return alias ?? id.Last();
        }

        public string Def()
        {
            return String.Join(".", id) + (alias != null ? (" as " + alias) : "");
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
            return id == other.id;
        }
    }
}
