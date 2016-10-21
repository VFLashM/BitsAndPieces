using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joiner
{
    class RuleGenerator
    {
        public static List<Rule> Create(TableInfo t1, TableInfo t2)
        {
            var rules = new List<Rule>();
            var c1 = t1.Columns();
            var c2 = t2.Columns();
            if (c1 == null || c2 == null)
            {
                return rules;
            }

            foreach (var common in c1.Intersect(c2))
            {
                rules.Add(Rule.Builder.CreateSimple(t1, common, t2, common, "auto"));
            }

            if (rules.Count == 0)
            {
            }
            return rules;
        }
    }
}
