using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.UI.VSIntegration;

namespace Common
{
    public class Connection
    {
        public static string GetActiveDatabase(string text)
        {
            string currentDatabase = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo.UIConnectionInfo.AdvancedOptions["DATABASE"];
            return Parser.ParseUseDatabase(text) ?? currentDatabase;
        }
    }
}
