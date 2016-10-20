using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joiner
{
    class ColumnDef
    {
        public readonly string name;
        public readonly string type;
        public ColumnDef(string name, string type)
        {
            this.name = name;
            this.type = type;
        }
    }
}
