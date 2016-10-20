using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joiner
{
    class ContextInfo
    {
        List<TableInfo> joinedTables;
        TableInfo newTable;
        bool hasGlue;

        public ContextInfo(List<TableInfo> joinedTables, TableInfo newTable, bool hasGlue)
        {
            this.joinedTables = joinedTables;
            this.newTable = newTable;
            this.hasGlue = hasGlue;
        }
    }
}
