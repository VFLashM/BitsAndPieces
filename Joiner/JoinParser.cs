using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Joiner
{
    class JoinParser
    {
        static readonly Regex fromRegex = new Regex(@"\bfrom\b\s*", RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
        static readonly Regex indentRegex = new Regex(@"([ \t]*).*$");
        static readonly Regex joinRegex = new Regex(@"\bjoin\b\s*", RegexOptions.IgnoreCase);
        static readonly Regex identifierRegex = new Regex("^(" + String.Join("|", new string[] {
            @"\[(?<id>[^\]]+)\]", // brackets identifier
            @"""(?<id>[^""]+)""", // quoted identifier
            @"(?<id>[a-zA-Z_@#][a-zA-Z_@#$0-9]*)", // regular identifier
        }) + ")");
        static readonly Regex aliasRegex = new Regex(@"^\s+(?<as>as\s+)?(?<alias>[a-zA-Z_][a-zA-Z_0-9]*)", RegexOptions.IgnoreCase);
        static readonly Regex onRegex = new Regex(@"\bon\b\s*", RegexOptions.IgnoreCase);
        static readonly Regex whitespaceRegex = new Regex(@"^\s*$");
        static readonly Regex tableDefRegex = new Regex(
            @"declare\s+(?<name>@[a-zA-Z_][a-zA-Z_0-9]*)\s+table\s*\(\s*" + "|" + // table var
            @"create\s+table\s+(?<name>#*[a-zA-Z_][a-zA-Z_0-9]*)\s*\(\s*"         // regular table
            , RegexOptions.IgnoreCase);
        static readonly Regex commaParenRegex = new Regex(@"([),])\s*");

        static bool IsBalanced(string str, int from, int to)
        {
            int pcount = 0;
            for (int i = from; i < to; ++i)
            {
                if (str[i] == '(')
                {
                    pcount += 1;
                }
                else if (str[i] == ')')
                {
                    pcount -= 1;
                }
            }
            return pcount == 0;
        }

        static bool ConsumeRegexBalanced(ref string str, Regex regex, out Match outMatch)
        {
            foreach (Match match in regex.Matches(str))
            {
                if (IsBalanced(str, 0, match.Index))
                {
                    outMatch = match;
                    str = str.Substring(match.Index + match.Length);
                    return true;
                }
            }
            outMatch = null;
            return false;
        }

        /*
        static bool ConsumeParens(ref string str)
        {
            if (!str.StartsWith("("))
            {
                return false;
            }
            int offset = 0;
            int balance = 0;
            while (offset < str.Length)
            {
                switch (str[offset])
                {
                    case ')':
                        balance -= 1;
                        break;
                    case '(':
                        balance += 1;
                        break;
                }
                offset += 1;
                if (balance == 0)
                {
                    str = str.Substring(offset);
                    return true;
                }
            }
            return false;
        }
         */

        static bool ConsumeId(ref string str, out List<String> id)
        {
            id = new List<string>();

            while (true)
            {
                Match match;
                if (ConsumeRegexBalanced(ref str, identifierRegex, out match))
                {
                    id.Add(match.Groups["id"].Value);
                    if (str.StartsWith("."))
                    {
                        str = str.Substring(1);
                    }
                    else
                    {
                        return true;
                    }
                }
                else if (str.StartsWith("."))
                {
                    id.Add("");
                    str = str.Substring(1);
                }
                else
                {
                    return false;
                }
            }
        }

        static bool ConsumeTable(ref string str, out TableInfo table, Dictionary<string, List<string>> localTables)
        {
            List<string> id;
            if (!ConsumeId(ref str, out id))
            {
                table = null;
                return false;
            }

            List<string> localTableColumns = null;
            if (id.Count == 1 && localTables.ContainsKey(id[0]))
            {
                localTableColumns = localTables[id[0]];
            }

            Match match;
            string alias = null;
            string beforeAliasStr = str;    
            if (ConsumeRegexBalanced(ref str, aliasRegex, out match))
            {
                alias = match.Groups["alias"].Value;
                if (!match.Groups["as"].Success && (alias.ToLower() == "join" || alias.ToLower() == "on"))
                {
                    str = beforeAliasStr;
                    alias = null;
                }
            }

            table = new TableInfo(id, alias, localTableColumns);
            return true;
        }

        static public ContextInfo ParseContext(string body)
        {
            Match match = null;
            foreach (Match trymatch in fromRegex.Matches(body))
            {
                match = trymatch;
                if (match != null)
                {
                    break;
                }
            }
            if (match == null)
            {
                return null;
            }

            var beforeFrom = body.Substring(0, match.Index);
            string fromIndent = indentRegex.Match(beforeFrom).Groups[1].Value;
            body = body.Substring(match.Index + match.Length);

            var localTables = ParseLocalTables(beforeFrom);

            var tables = new List<TableInfo>();
            TableInfo newTable;
            if (!ConsumeTable(ref body, out newTable, localTables))
            {
                return null;
            }
            tables.Add(newTable);
            if (whitespaceRegex.IsMatch(body))
            {
                return new ContextInfo(fromIndent, tables, null, false);
            }
            if (!ConsumeRegexBalanced(ref body, joinRegex, out match))
            {
                return null;
            }

            do
            {
                if (whitespaceRegex.IsMatch(body))
                {
                    return new ContextInfo(fromIndent, tables, null, true);
                }
                if (!ConsumeTable(ref body, out newTable, localTables))
                {
                    return null;
                }
                if (whitespaceRegex.IsMatch(body))
                {
                    return new ContextInfo(fromIndent, tables, newTable, false);
                }
                if (!ConsumeRegexBalanced(ref body, onRegex, out match))
                {
                    return null;
                }
                if (whitespaceRegex.IsMatch(body))
                {
                    return new ContextInfo(fromIndent, tables, newTable, true);
                }
                tables.Add(newTable);
            }
            while (ConsumeRegexBalanced(ref body, joinRegex, out match));

            return new ContextInfo(fromIndent, tables, null, false);
        }

        static List<string> ParseTableColumns(string body)
        {
            var columns = new List<string>();
            while (true)
            {
                Match match;
                if (ConsumeRegexBalanced(ref body, identifierRegex, out match))
                {
                    columns.Add(match.Groups["id"].Value);
                    if (ConsumeRegexBalanced(ref body, commaParenRegex, out match))
                    {
                        if (match.Groups[1].Value == ",")
                        {
                            continue;
                        }
                    }
                }
                return columns;
            }
        }

        static public Dictionary<string, List<string>> ParseLocalTables(string body)
        {
            var res = new Dictionary<string, List<string>>();
            foreach (Match match in tableDefRegex.Matches(body))
            {
                var name = match.Groups["name"].Value;
                var def = body.Substring(match.Index + match.Length);
                var columns = ParseTableColumns(def);
                if (columns != null)
                {
                    res[name] = columns;
                }
            }
            return res;
        }
    }
}
