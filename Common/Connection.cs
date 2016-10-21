using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.Smo.RegSvrEnum;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

namespace Common
{
    public class Connection
    {
        public static string GetActiveDatabase(string text, int? atPos = null)
        {
            string currentDatabase = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo.UIConnectionInfo.AdvancedOptions["DATABASE"];
            return Parser.ParseUseDatabase(text, atPos) ?? currentDatabase;
        }

        public static SqlConnectionInfo GetActiveConnectionInfo()
        {
            UIConnectionInfo uiConnectionInfo = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo.UIConnectionInfo;
            SqlConnectionInfo connectionInfo = new SqlConnectionInfo();
            connectionInfo.ApplicationName = "SSMS Plugin Bits and Pieces";
            connectionInfo.ServerName = uiConnectionInfo.ServerName;
            connectionInfo.UserName = uiConnectionInfo.UserName;
            connectionInfo.Password = uiConnectionInfo.Password;
            connectionInfo.UseIntegratedSecurity = String.IsNullOrEmpty(uiConnectionInfo.Password);
            return connectionInfo;
        }
    }
}
