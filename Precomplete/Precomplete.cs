using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

namespace Precomplete
{
    class Precomplete
    {
        public Precomplete()
        {
            SqlConnectionInfo connectionInfo = Common.Connection.GetActiveConnectionInfo();
            ServerConnection connection = new ServerConnection(connectionInfo);
        }

        public void Apply(TextDocument doc, TextPoint startPoint, TextPoint endPoint)
        {

        }
    }
}
