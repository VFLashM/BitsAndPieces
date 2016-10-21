using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Joiner
{
    class RuleGenerator
    {
        static bool ContainsAll(List<string> set, List<string> subset)
        {
            return !subset.Except(set).Any();
        }

        static Rule CreateForPk(TableInfo t1, TableInfo t2, List<string> columns)
        {
            var b = new Rule.Builder(t1, t2, "auto pk");
            foreach (var col in columns)
            {
                b.Add(col, col);
            }
            return b.Finish();
        }

        public static List<Rule> Create(TableInfo t1, TableInfo t2)
        {
            var rules = new List<Rule>();
            var c1 = t1.Columns();
            var c2 = t2.Columns();
            if (c1 == null || c2 == null)
            {
                return rules;
            }

            var intersection = c1.Intersect(c2).ToList();

            if (!t1.Match(t2))
            {
                var pk1 = t1.PrimaryKey();
                var pk2 = t2.PrimaryKey();
                if (pk1 != null && ContainsAll(intersection, pk1))
                {
                    rules.Add(CreateForPk(t1, t2, pk1));
                }
                if (pk2 != null && pk2 != pk1 && ContainsAll(intersection, pk2))
                {
                    rules.Add(CreateForPk(t1, t2, pk2));
                }
            }

            if (rules.Count == 0)
            {
                foreach (var common in intersection)
                {
                    rules.Add(Rule.Builder.CreateSimple(t1, common, t2, common, "auto name"));
                }
            }

            if (rules.Count == 0)
            {
            }
            return rules;
        }
    }
}
