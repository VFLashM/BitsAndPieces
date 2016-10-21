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

        public string Alias()
        {
            return alias ?? id.Last();
        }
    }
}
