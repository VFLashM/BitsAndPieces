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
            @"declare\s+(?<name>@[a-zA-Z_][a-zA-Z_0-9]*)\s+table\s*" + "|" + // table var
            @"create\s+table\s+(?<name>#+[a-zA-Z_][a-zA-Z_0-9]*)\s*"         // temp table
            , RegexOptions.IgnoreCase);

        static bool ConsumeRegex(ref string str, Regex regex, out Match match)
        {
            match = regex.Match(str);
            if (match.Success)
            {
                str = str.Substring(match.Index + match.Length);
            }
            return match.Success;
        }

        static bool ConsumeId(ref string str, out List<String> id)
        {
            id = new List<string>();

            while (true)
            {
                Match match;
                if (ConsumeRegex(ref str, identifierRegex, out match))
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
                    id.Add(null);
                    str = str.Substring(1);
                }
                else
                {
                    return false;
                }
            }
        }

        static bool ConsumeTable(ref string str, out TableInfo table)
        {
            List<string> id;
            if (!ConsumeId(ref str, out id))
            {
                table = null;
                return false;
            }

            Match match;
            string alias = null;
            string beforeAliasStr = str;
            if (ConsumeRegex(ref str, aliasRegex, out match))
            {
                alias = match.Groups["alias"].Value;
                if (!match.Groups["as"].Success && (alias.ToLower() == "join" || alias.ToLower() == "on"))
                {
                    str = beforeAliasStr;
                    alias = null;
                }
            }

            table = new TableInfo(id, alias);
            return true;
        }

        static public ContextInfo ParseContext(string body)
        {
            Match match;
            if (!ConsumeRegex(ref body, fromRegex, out match))
            {
                return null;
            }

            var tables = new List<TableInfo>();
            TableInfo newTable;
            if (!ConsumeTable(ref body, out newTable))
            {
                return null;
            }
            tables.Add(newTable);
            if (whitespaceRegex.IsMatch(body))
            {
                return new ContextInfo(tables, null, false);
            }
            if (!ConsumeRegex(ref body, joinRegex, out match))
            {
                return null;
            }

            do
            {
                if (whitespaceRegex.IsMatch(body))
                {
                    return new ContextInfo(tables, null, true);
                }
                if (!ConsumeTable(ref body, out newTable))
                {
                    return null;
                }
                if (whitespaceRegex.IsMatch(body))
                {
                    return new ContextInfo(tables, newTable, false);
                }
                if (!ConsumeRegex(ref body, onRegex, out match))
                {
                    return null;
                }
                if (whitespaceRegex.IsMatch(body))
                {
                    return new ContextInfo(tables, newTable, true);
                }
                tables.Add(newTable);
            }
            while (ConsumeRegex(ref body, joinRegex, out match));

            return new ContextInfo(tables, null, false);
        }

        static List<ColumnDef> ParseTableColumns(string body)
        {
            return null;
        }

        static public Dictionary<string, List<ColumnDef>> ParseTables(string body)
        {
            var res = new Dictionary<string, List<ColumnDef>>();
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
