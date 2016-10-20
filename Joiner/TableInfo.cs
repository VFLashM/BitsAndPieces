using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joiner
{
    class TableInfo
    {
        private List<string> id;
        private string alias;

        public TableInfo(List<string> id, string alias)
        {
            this.id = id;
            this.alias = alias;
        }
    }
}
