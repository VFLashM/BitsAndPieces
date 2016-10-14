using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Collections.Specialized;

namespace Opener
{
    class ObjectAccessor
    {
        Server _server;
        Scripter _scripter;

        public ObjectAccessor()
        {
            UIConnectionInfo uiConnectionInfo = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo.UIConnectionInfo;
            SqlConnectionInfo connectionInfo = new SqlConnectionInfo();
            connectionInfo.ApplicationName = "SSMS Plugin Bits and Pieces";
            connectionInfo.ServerName = uiConnectionInfo.ServerName;
            connectionInfo.UserName = uiConnectionInfo.UserName;
            connectionInfo.Password = uiConnectionInfo.Password;
            connectionInfo.UseIntegratedSecurity = String.IsNullOrEmpty(uiConnectionInfo.Password);

            ServerConnection connection = new ServerConnection(connectionInfo);
            connection.Connect();

            _server = new Server(connection);
            _scripter = new Scripter(_server);
            _scripter.Options.IncludeDatabaseContext = true;
            // does not work: _scripter.Options.ScriptBatchTerminator = true;
            _scripter.Options.IncludeDatabaseRoleMemberships = true;
            _scripter.Options.IncludeFullTextCatalogRootPath = true;
            _scripter.Options.IncludeHeaders = true;
        }

        public List<Urn> GetObjects()
        {
            var result = new List<Urn>();
            foreach (Database database in _server.Databases)
            {
                if (database.Name == "SRA_Main" || database.Name == "SRA_Track")
                {
                    foreach (StoredProcedure proc in database.StoredProcedures)
                    {
                        result.Add(proc.Urn);
                    }
                }
            }
            return result;
        }

        public string GetObjectText(Urn urn)
        {
            StringCollection body = _scripter.Script(new Urn[] { urn });
            String[] bodyArray = new String[body.Count];
            body.CopyTo(bodyArray, 0);
            return String.Join("\nGO\n", bodyArray);
        }
    }
}
