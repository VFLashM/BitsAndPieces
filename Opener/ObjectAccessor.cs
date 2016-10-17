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
        string _serverName;
        Server _server;
        Scripter _scripter;

        public class ObjectInfo
        {
            readonly public string name;
            readonly public Urn urn;
            readonly public string type;

            public ObjectInfo(string name, Urn urn, string type)
            {
                this.name = name;
                this.urn = urn;
                this.type = type;
            }
            public ObjectInfo(string database, ScriptSchemaObjectBase obj, string type)
            {
                this.name = database + "." + obj.Schema + "." + obj.Name;
                this.urn = obj.Urn;
                this.type = type;
            }
        }

        public ObjectAccessor()
        {
            UIConnectionInfo uiConnectionInfo = ServiceCache.ScriptFactory.CurrentlyActiveWndConnectionInfo.UIConnectionInfo;
            SqlConnectionInfo connectionInfo = new SqlConnectionInfo();
            connectionInfo.ApplicationName = "SSMS Plugin Bits and Pieces";
            connectionInfo.ServerName = uiConnectionInfo.ServerName;
            connectionInfo.UserName = uiConnectionInfo.UserName;
            connectionInfo.Password = uiConnectionInfo.Password;
            connectionInfo.UseIntegratedSecurity = String.IsNullOrEmpty(uiConnectionInfo.Password);
            _serverName = connectionInfo.ServerName;

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

        public string ServerName()
        {
            return _serverName;
        }

        public ObjectInfo FindObject(string name)
        {
            foreach (var info in GetObjects())
            {
                string[] parts = info.name.Split('.');
                if (name == info.name || // full match
                    name == parts[0] + ".." + parts[2] || // database..name
                    name == parts[1] + "." + parts[2] || // schema.name
                    name == parts[2]) // raw name
                {
                    return info;
                }
            }
            return null;
        }

        public List<ObjectInfo> GetObjects()
        {
            var result = new List<ObjectInfo>();
            string[] databases = Properties.Settings.Default.GetDatabases();

            foreach (Database database in _server.Databases)
            {
                if (databases.Contains(database.Name))
                {
                    foreach (StoredProcedure obj in database.StoredProcedures)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "procedure"));
                    }
                    foreach (UserDefinedFunction obj in database.UserDefinedFunctions)
                    {
                        // type detection is way too slow
                        //string type = (obj.DataType == null) ? "table function" : "scalar function";
                        result.Add(new ObjectInfo(database.Name, obj, "function"));
                    }
                    foreach (Table tbl in database.Tables)
                    {
                        result.Add(new ObjectInfo(database.Name, tbl, "table"));
                        foreach (Trigger trig in tbl.Triggers)
                        {
                            string trname = database.Name + "." + tbl.Schema + "." + tbl.Name + ":" + trig.Name;
                            result.Add(new ObjectInfo(trname, trig.Urn, "trigger"));
                        }
                    }
                    foreach (View obj in database.Views)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "view"));
                    }
                    foreach (UserDefinedTableType obj in database.UserDefinedTableTypes)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "table type"));
                    }
                    foreach (UserDefinedDataType obj in database.UserDefinedDataTypes)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "data type"));
                    }
                }
            }
            return result;
        }

        public string GetObjectText(Urn urn)
        {
            var obj = _server.GetSmoObject(urn);
            if (obj is Table) // do not return create for tables, pretty much useless for edits
            {
                return null;
            }

            StringCollection body = _scripter.Script(new Urn[] { urn });
            String[] bodyArray = new String[body.Count];
            body.CopyTo(bodyArray, 0);

            // replace create with alter
            var textObj = obj as ITextObject;
            if (textObj != null)
            {
                bodyArray[body.Count - 1] = textObj.ScriptHeader(true) + textObj.TextBody;
            }

            return String.Join("\nGO\n", bodyArray);
        }
    }
}
