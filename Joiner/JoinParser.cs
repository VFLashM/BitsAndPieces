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
        static readonly Regex identifierRegex = new Regex(@"^\s*(" + String.Join("|", new string[] {
            @"\[(?<id>[^\]]+)\]", // brackets identifier
            @"""(?<id>[^""]+)""", // quoted identifier
            @"(?<id>[a-zA-Z_@#][a-zA-Z_@#$0-9]*)", // regular identifier
        }) + @")\s*");
        static readonly Regex onRegex = new Regex(@"\bon\b\s*", RegexOptions.IgnoreCase);
        static readonly Regex whitespaceRegex = new Regex(@"^\s*$");
        static readonly Regex tableDefRegex = new Regex(
            @"declare\s+(?<name>@[a-zA-Z_][a-zA-Z_0-9]*)\s+table\s*" + "|" + // table var
            @"create\s+table\s+(?<name>#*[a-zA-Z_][a-zA-Z_0-9]*)\s*"         // regular table
            , RegexOptions.IgnoreCase);
        static readonly Regex openParenRegex = new Regex(@"^\s*\(");
        static readonly Regex parenRegex = new Regex(@"\)\s*");
        static readonly Regex commaParenRegex = new Regex(@"([),])\s*");
        static readonly string[] keywords = new string[] {
            "ABSOLUTE", "EXEC", "OVERLAPS", "ACTION", "EXECUTE", "PAD", "ADA", "EXISTS", 
            "PARTIAL", "ADD", "EXTERNAL", "PASCAL", "ALL", "EXTRACT", "POSITION", 
            "ALLOCATE", "FALSE", "PRECISION", "ALTER", "FETCH", "PREPARE", "AND", 
            "FIRST", "PRESERVE", "ANY", "FLOAT", "PRIMARY", "ARE", "FOR", "PRIOR", 
            "AS", "FOREIGN", "PRIVILEGES", "ASC", "FORTRAN", "PROCEDURE", "ASSERTION", 
            "FOUND", "PUBLIC", "AT", "FROM", "READ", "AUTHORIZATION", "FULL", "REAL", 
            "AVG", "GET", "REFERENCES", "BEGIN", "GLOBAL", "RELATIVE", "BETWEEN", "GO", 
            "RESTRICT", "BIT", "GOTO", "REVOKE", "BIT_LENGTH", "GRANT", "RIGHT", "BOTH", 
            "GROUP", "ROLLBACK", "BY", "HAVING", "ROWS", "CASCADE", "HOUR", "SCHEMA", 
            "CASCADED", "IDENTITY", "SCROLL", "CASE", "IMMEDIATE", "SECOND", "CAST", "IN", 
            "SECTION", "CATALOG", "INCLUDE", "SELECT", "CHAR", "INDEX", "SESSION", 
            "CHAR_LENGTH", "INDICATOR", "SESSION_USER", "CHARACTER", "INITIALLY", "SET", 
            "CHARACTER_LENGTH", "INNER", "SIZE", "CHECK", "INPUT", "SMALLINT", "CLOSE", 
            "INSENSITIVE", "SOME", "COALESCE", "INSERT", "SPACE", "COLLATE", "INT", "SQL", 
            "COLLATION", "INTEGER", "SQLCA", "COLUMN", "INTERSECT", "SQLCODE", "COMMIT", 
            "INTERVAL", "SQLERROR", "CONNECT", "INTO", "SQLSTATE", "CONNECTION", "IS", 
            "SQLWARNING", "CONSTRAINT", "ISOLATION", "SUBSTRING", "CONSTRAINTS", "JOIN", 
            "SUM", "CONTINUE", "KEY", "SYSTEM_USER", "CONVERT", "LANGUAGE", "TABLE", 
            "CORRESPONDING", "LAST", "TEMPORARY", "COUNT", "LEADING", "THEN", "CREATE", 
            "LEFT", "TIME", "CROSS", "LEVEL", "TIMESTAMP", "CURRENT", "LIKE", "TIMEZONE_HOUR", 
            "CURRENT_DATE", "LOCAL", "TIMEZONE_MINUTE", "CURRENT_TIME", "LOWER", "TO", 
            "CURRENT_TIMESTAMP", "MATCH", "TRAILING", "CURRENT_USER", "MAX", "TRANSACTION", 
            "CURSOR", "MIN", "TRANSLATE", "DATE", "MINUTE", "TRANSLATION", "DAY", "MODULE", 
            "TRIM", "DEALLOCATE", "MONTH", "TRUE", "DEC", "NAMES", "UNION", "DECIMAL", 
            "NATIONAL", "UNIQUE", "DECLARE", "NATURAL", "UNKNOWN", "DEFAULT", "NCHAR", 
            "UPDATE", "DEFERRABLE", "NEXT", "UPPER", "DEFERRED", "NO", "USAGE", "DELETE", 
            "NONE", "USER", "DESC", "NOT", "USING", "DESCRIBE", "NULL", "VALUE", "DESCRIPTOR", 
            "NULLIF", "VALUES", "DIAGNOSTICS", "NUMERIC", "VARCHAR", "DISCONNECT", "OCTET_LENGTH", 
            "VARYING", "DISTINCT", "OF", "VIEW", "DOMAIN", "ON", "WHEN", "DOUBLE", "ONLY", 
            "WHENEVER", "DROP", "OPEN", "WHERE", "ELSE", "OPTION", "WITH", "END", "OR", 
            "WORK", "END-EXEC", "ORDER", "WRITE", "ESCAPE", "OUTER", "YEAR", "EXCEPT", 
            "OUTPUT", "ZONE", "EXCEPTION",
        };

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

        static bool ConsumeParens(ref string str)
        {
            Match match = openParenRegex.Match(str);
            if (match != null)
            {
                var outstr = str.Substring(match.Length);
                if (ConsumeRegexBalanced(ref outstr, parenRegex, out match))
                {
                    str = outstr;
                    return true;
                }
            }
            return false;            
        }

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

        static bool ConsumeWord(ref string str, out string word)
        {
            Match match;
            bool matched = ConsumeRegexBalanced(ref str, identifierRegex, out match);
            word = matched ? match.Groups["id"].Value : null;
            return matched;
        }

        static bool ConsumeAlias(ref string str, out string alias)
        {
            var srcStr = str;
            if (ConsumeWord(ref str, out alias))
            {
                var upperAlias = alias.ToUpper();
                if (upperAlias == "AS")
                {
                    if (!ConsumeWord(ref str, out alias))
                    {
                        return false;
                    }
                }
                if (keywords.Contains(upperAlias))
                {
                    str = srcStr;
                    alias = null;
                }
            }
            return true;
        }

        static bool ConsumeTable(ref string str, out TableInfo table, Dictionary<string, List<string>> localTables)
        {
            if (ConsumeParens(ref str)) // subquery
            {
                string alias;
                if (!ConsumeAlias(ref str, out alias))
                {
                    table = null;
                    return false;
                }

                List<string> columns = null;
                if (openParenRegex.IsMatch(str))
                {
                    if (!ConsumeTableColumns(ref str, out columns))
                    {
                        table = null;
                        return false;
                    }
                }

                table = new TableInfo(null, alias, columns);
                return true;
            }
            else
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

                ConsumeParens(ref str); // for stored procs

                string alias;
                if (!ConsumeAlias(ref str, out alias))
                {
                    table = null;
                    return false;
                }

                table = new TableInfo(id, alias, localTableColumns);
                return true;
            }
        }

        static public ContextInfo ParseContext(string body)
        {
            Match match = null;
            foreach (Match trymatch in fromRegex.Matches(body))
            {
                if (IsBalanced(body, trymatch.Index, body.Length))
                {
                    match = trymatch;
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

        static bool ConsumeTableColumns(ref string body, out List<string> columns)
        {
            Match match;
            if (!ConsumeRegexBalanced(ref body, openParenRegex, out match))
            {
                columns = null;
                return false;
            }
            columns = new List<string>();
            while (true)
            {
                if (ConsumeRegexBalanced(ref body, identifierRegex, out match))
                {
                    columns.Add(match.Groups["id"].Value);
                    if (ConsumeRegexBalanced(ref body, commaParenRegex, out match))
                    {
                        if (match.Groups[1].Value == ",")
                        {
                            continue;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                columns = null;
                return false;
            }
        }

        static public Dictionary<string, List<string>> ParseLocalTables(string body)
        {
            var res = new Dictionary<string, List<string>>();
            foreach (Match match in tableDefRegex.Matches(body))
            {
                var name = match.Groups["name"].Value;
                var def = body.Substring(match.Index + match.Length);
                List<string> columns;
                if (ConsumeTableColumns(ref def, out columns))
                {
                    res[name] = columns;
                }
            }
            return res;
        }
    }
}
