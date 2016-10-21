using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Joiner
{
    class Rule
    {
        TableInfo t1;
        TableInfo t2;
        string condition;

        public string Apply(TableInfo a1, TableInfo a2)
        {
            if (a1 == t2 && a2 == t1)
            {
                return Apply(a2, a1);
            }
            if (a1 == t1 && a2 == t2)
            {
                Regex pattern = new Regex(@"\b(?<alias>" + Regex.Escape(t1.Alias()) + "|" + Regex.Escape(t2.Alias()) + @")\.", RegexOptions.RightToLeft);
                var res = condition;
                foreach (Match match in pattern.Matches(condition))
                {
                    string replacement = null;
                    if (match.Groups["alias"].Value == t1.Alias())
                    {
                        replacement = a1.Alias();
                    }
                    else if (match.Groups["alias"].Value == t2.Alias())
                    {
                        replacement = a2.Alias();
                    }
                    res = res.Substring(0, match.Index) + replacement + "." + res.Substring(match.Index + match.Length);
                }
                return res;
            }
            return null;
        }
    }
}
