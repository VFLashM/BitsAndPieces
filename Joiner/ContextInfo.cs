using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joiner
{
    class ContextInfo
    {
        public readonly string fromIndent;
        public readonly List<TableInfo> joinedTables;
        public readonly TableInfo newTable;
        public readonly bool hasGlue;

        public ContextInfo(string fromIndent, List<TableInfo> joinedTables, TableInfo newTable, bool hasGlue)
        {
            this.fromIndent = fromIndent;
            this.joinedTables = joinedTables;
            this.newTable = newTable;
            this.hasGlue = hasGlue;
        }

        public List<TableInfo> AllTables()
        {
            var res = new List<TableInfo>();
            res.AddRange(joinedTables);
            if (newTable != null)
            {
                res.Add(newTable);
            }
            return res;
        }
    }
}
