using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

namespace Precomplete
{
    class Precomplete
    {
        private class DatabaseInfo
        {
            public Dictionary<string, string> tables = new Dictionary<string, string>();
            public Dictionary<string, string> functions = new Dictionary<string, string>();
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
                    foreach (UserDefinedFunction function in database.UserDefinedFunctions)
                    {
                        databaseInfo.functions[function.Name] = function.Schema;
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
            UIConnectionInfo uiConnectionInfo = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo.UIConnectionInfo;
            ServerInfo serverInfo = GetServerInfo(uiConnectionInfo);
            if (serverInfo == null)
            {
                return;
            }

            string text = textDoc.StartPoint.CreateEditPoint().GetText(endPoint);
            /*
            var editPoint = textDoc.StartPoint.CreateEditPoint();
            string text = editPoint.GetText(textDoc.EndPoint);
            text = text.Replace(Environment.NewLine, "\n");
            int cursorPos = textDoc.Selection.ActivePoint.AbsoluteCharOffset - 1;
             */
        }
    }
}
