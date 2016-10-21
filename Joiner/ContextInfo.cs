﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joiner
{
    class ContextInfo
    {
        public readonly List<TableInfo> joinedTables;
        public readonly TableInfo newTable;
        public readonly bool hasGlue;

        public ContextInfo(List<TableInfo> joinedTables, TableInfo newTable, bool hasGlue)
        {
            this.joinedTables = joinedTables;
            this.newTable = newTable;
            this.hasGlue = hasGlue;
        }
    }
}