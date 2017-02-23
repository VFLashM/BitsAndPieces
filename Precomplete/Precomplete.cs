using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Text.RegularExpressions;

namespace Precomplete
{
    class Precomplete
    {
        static readonly Regex wordRegex = new Regex(@"[^.@]\b([a-zA-Z_@#][a-zA-Z_@#$0-9]*)\s*[(]?$", RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
        static readonly Regex fromJoinRegex = new Regex(@"(from|join)\s([a-zA-Z_@#][a-zA-Z_@#$0-9]*)\s*[(]?$", RegexOptions.RightToLeft | RegexOptions.IgnoreCase);
        static readonly Regex execRegex = new Regex(@"\bexec\s([a-zA-Z_@#][a-zA-Z_@#$0-9]*)\s*$", RegexOptions.RightToLeft | RegexOptions.IgnoreCase);

        private class DatabaseInfo
        {
            public Dictionary<string, string> tables = new Dictionary<string, string>();
            public Dictionary<string, string> functions = new Dictionary<string, string>();
            public Dictionary<string, string> procedures = new Dictionary<string, string>();
        }
        private class ServerInfo
        {
            public Dictionary<string, DatabaseInfo> databases = new Dictionary<string,DatabaseInfo>();
        }
        private class ServerInfoCache
        {
            public string[] databases;
            public DateTime timestamp;
            public ServerInfo info;
        }
        private Dictionary<string, ServerInfoCache> _cache = new Dictionary<string, ServerInfoCache>();

        ServerInfo GetServerInfo(UIConnectionInfo uiConnectionInfo)
        {
            var databases = Properties.Settings.Default.GetDatabases();
            if (databases.Length == 0)
            {
                return null;
            }

            if (_cache.ContainsKey(uiConnectionInfo.ServerName))
            {
                ServerInfoCache serverInfoCache = _cache[uiConnectionInfo.ServerName];
                if (Enumerable.SequenceEqual(serverInfoCache.databases, databases) &&
                    DateTime.Now.Subtract(serverInfoCache.timestamp).TotalMinutes < 30)
                {
                    return serverInfoCache.info;
                }
            }

            ServerInfo res = new ServerInfo();
            SqlConnectionInfo connectionInfo = Common.Connection.GetConnectionInfo(uiConnectionInfo);
            ServerConnection connection = new ServerConnection(connectionInfo);
            Server server = new Server(connection);
            foreach (Database database in server.Databases)
            {
                if (databases.Contains(database.Name))
                {
                    DatabaseInfo databaseInfo = new DatabaseInfo();
                    foreach (Table table in database.Tables)
                    {
                        databaseInfo.tables[table.Name] = table.Schema;
                    }
                    foreach (View view in database.Views)
                    {
                        // views are indistinguishable from tables in preloader's context
                        databaseInfo.tables[view.Name] = view.Schema;
                    }
                    foreach (UserDefinedFunction function in database.UserDefinedFunctions)
                    {
                        databaseInfo.functions[function.Name] = function.Schema;
                    }
                    foreach (StoredProcedure proc in database.StoredProcedures)
                    {
                        databaseInfo.procedures[proc.Name] = proc.Schema;
                    }
                    res.databases[database.Name] = databaseInfo;
                }
            }

            {
                ServerInfoCache serverInfoCache = new ServerInfoCache();
                serverInfoCache.info = res;
                serverInfoCache.databases = databases;
                serverInfoCache.timestamp = DateTime.Now;
                _cache[uiConnectionInfo.ServerName] = serverInfoCache;
            }

            return res;
        }

        public Precomplete()
        {
            
        }

        public void Apply(TextDocument textDoc, TextPoint startPoint, TextPoint endPoint)
        {
            string delta = startPoint.CreateEditPoint().GetText(endPoint);
            if (!(delta == " " || delta == "\t" || delta == "\r\n" || delta == "("))
            {
                return;
            }

            UIConnectionInfo uiConnectionInfo = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo.UIConnectionInfo;
            ServerInfo serverInfo = GetServerInfo(uiConnectionInfo);
            if (serverInfo == null)
            {
                return;
            }

            string text = textDoc.StartPoint.CreateEditPoint().GetText(endPoint);
            text = text.Replace(Environment.NewLine, "\n");
            int cursorPos = textDoc.Selection.ActivePoint.AbsoluteCharOffset - 1;
            string activeDatabase = Common.Connection.GetActiveDatabase(text, cursorPos);

            var wordMatch = wordRegex.Match(text);
            if (!wordMatch.Success)
            {
                return;
            }
            var wordGroup = wordMatch.Groups[1];
            string word = wordGroup.Value;

            string prependDb = null;
            string prependSchema = null;
            if (fromJoinRegex.Match(text).Success)
            {
                foreach (var db in serverInfo.databases)
                {
                    if (db.Key != activeDatabase)
                    {
                        if (db.Value.tables.ContainsKey(word))
                        {
                            prependDb = db.Key;
                            prependSchema = db.Value.tables[word];
                            if (prependSchema == "dbo") prependSchema = "";
                            break;
                        }
                    }
                }
            }
            else if (execRegex.Match(text).Success)
            {
                foreach (var db in serverInfo.databases)
                {
                    if (db.Key != activeDatabase)
                    {
                        if (db.Value.procedures.ContainsKey(word))
                        {
                            prependDb = db.Key;
                            prependSchema = db.Value.procedures[word];
                            if (prependSchema == "dbo") prependSchema = "";
                            break;
                        }
                    }
                }
            }
            else if (delta == "(")
            {
                foreach (var db in serverInfo.databases)
                {
                    if (db.Value.functions.ContainsKey(word))
                    {
                        prependDb = db.Key;
                        prependSchema = db.Value.functions[word];
                        break;
                    }
                }
            }
            string prependText = "";
            if (prependDb != null)
            {
                prependText += prependDb + ".";
            }
            if (prependSchema != null)
            {
                prependText += prependSchema + ".";
            }
            if (prependText != "")
            {
                var editPoint = textDoc.CreateEditPoint();
                editPoint.MoveToAbsoluteOffset(wordGroup.Index + 1);
                editPoint.Insert(prependText);
            }
        }
    }
}
