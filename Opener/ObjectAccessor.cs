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
            readonly public string schema;
            readonly public Urn urn;
            readonly public string type;

            public ObjectInfo(string database, string schema, string name, Urn urn, string type)
            {
                this.name = database + ((schema != null) ? ("." + schema + ".") : ".") + name;
                this.schema = schema;
                this.urn = urn;
                this.type = type;
            }
            public ObjectInfo(string database, ScriptSchemaObjectBase obj, string type)
            {
                this.name = database + "." + obj.Schema + "." + obj.Name;
                this.schema = obj.Schema;
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

        static ObjectInfo MatchObject(List<ObjectInfo> objects, string name)
        {
            foreach (var info in objects)
            {
                string[] parts = info.name.Split('.');
                string database = parts[0];
                string objname = parts[parts.Length - 1];
                string schema = parts.Length > 2 ? parts[1] : "";

                if (name == info.name ||
                    name == database + ".." + objname ||
                    name == schema + "." + objname ||
                    name == objname)
                {
                    return info;
                }
            }
            return null;
        }

        public ObjectInfo FindObject(string name, string databaseHint)
        {
            var objects = GetObjects();
            return MatchObject(objects, databaseHint + "." + name) 
                ?? MatchObject(objects, databaseHint + ".." + name)
                ?? MatchObject(objects, name);
        }

        public List<ObjectInfo> GetObjects()
        {
            var result = new List<ObjectInfo>();
            string[] databases = Properties.Settings.Default.GetDatabases();
            string[] schemas = Properties.Settings.Default.GetSchemas();

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
                            result.Add(new ObjectInfo(database.Name, tbl.Schema, tbl.Name + ":" + trig.Name, trig.Urn, "trigger"));
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
                    foreach (UserDefinedType obj in database.UserDefinedTypes)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "clr type"));
                    }
                    foreach (ExtendedStoredProcedure obj in database.ExtendedStoredProcedures)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "extended procedure"));
                    }
                    foreach (UserDefinedAggregate obj in database.UserDefinedAggregates)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "aggregate"));
                    }
                    foreach (DatabaseDdlTrigger obj in database.Triggers)
                    {
                        result.Add(new ObjectInfo(database.Name, null, obj.Name, obj.Urn, "ddl trigger"));
                    }
                    /* these are not very useful it seems
                    foreach (Rule obj in database.Rules)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "rule"));
                    }
                    foreach (SqlAssembly obj in database.Assemblies)
                    {
                        var name = database.Name + '.' + obj.Name;
                        result.Add(new ObjectInfo(name, obj.Urn, "assembly"));
                    }
                    foreach (Default obj in database.Defaults)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "default"));
                    }
                    foreach (PlanGuide obj in database.PlanGuides)
                    {
                        var name = database.Name + '.' + obj.Name;
                        result.Add(new ObjectInfo(name, obj.Urn, "plan guide"));
                    }
                     */
                }
            }
            result.RemoveAll(item => item.schema != null && !schemas.Contains(item.schema));
            return result;
        }

        public string GetObjectText(Urn urn)
        {
            var obj = _server.GetSmoObject(urn);
            var textObj = obj as ITextObject;
            if (textObj == null) // do not return create for tables and other non-alterable objects
            {
                return null;
            }

            StringCollection body = _scripter.Script(new Urn[] { urn });
            String[] bodyArray = new String[body.Count];
            body.CopyTo(bodyArray, 0);

            // replace create with alter
            bodyArray[body.Count - 1] = textObj.ScriptHeader(true) + textObj.TextBody;

            return String.Join("\nGO\n", bodyArray);
        }
    }
}
