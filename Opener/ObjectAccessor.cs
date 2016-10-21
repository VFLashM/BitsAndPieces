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
            readonly public string type;
            readonly public string database;
            readonly public string schema;
            readonly public string name;
            readonly public string subname;
            readonly public Urn urn;
            readonly public string fullName;

            string CreateFullName()
            {
                string res = database;
                if (schema != null)
                {
                    res += '.' + schema;
                }
                res += '.' + name;
                if (subname != null)
                {
                    res += ':' + subname;
                }
                return res;
            }

            public ObjectInfo(string database, NamedSmoObject obj, string type)
            {
                this.type = type;
                this.database = database;
                this.schema = null;
                this.name = obj.Name;
                this.subname = null;
                this.urn = obj.Urn;
                this.fullName = CreateFullName();
            }
            public ObjectInfo(string database, ScriptSchemaObjectBase obj, string type)
            {
                this.type = type;
                this.database = database;
                this.schema = obj.Schema;
                this.name = obj.Name;
                this.subname = null;
                this.urn = obj.Urn;
                this.fullName = CreateFullName();
            }
            public ObjectInfo(string database, ScriptSchemaObjectBase parent, NamedSmoObject subobj, string type)
            {
                this.type = type;
                this.database = database;
                this.schema = parent.Schema;
                this.name = parent.Name;
                this.subname = subobj.Name;
                this.urn = subobj.Urn;
                this.fullName = CreateFullName();
            }
        }

        public ObjectAccessor()
        {
            SqlConnectionInfo connectionInfo = Common.Connection.GetActiveConnectionInfo();
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
                if (databases.Contains(database.Name) && database.IsAccessible)
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
                    foreach (Table obj in database.Tables)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "table"));
                        foreach (Trigger trig in obj.Triggers)
                        {
                            result.Add(new ObjectInfo(database.Name, obj, trig, "trigger"));
                        }
                        /* somewhat useful, but slow everyhing down too much
                        foreach (Index ind in obj.Indexes)
                        {
                            result.Add(new ObjectInfo(database.Name, obj, ind, "index"));
                        }
                        foreach (Check chk in obj.Checks)
                        {
                            result.Add(new ObjectInfo(database.Name, obj, chk, "constraint"));
                        }
                        */
                    }
                    foreach (View obj in database.Views)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "view"));
                        /* somewhat useful, but slow everyhing down too much
                        foreach (Trigger trig in obj.Triggers)
                        {
                            result.Add(new ObjectInfo(database.Name, obj, trig, "view trigger"));
                        }
                        foreach (Index ind in obj.Indexes)
                        {
                            result.Add(new ObjectInfo(database.Name, obj, ind, "view index"));
                        }
                         */
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
                        result.Add(new ObjectInfo(database.Name, obj, "ddl trigger"));
                    }
                    /* these are not very useful it seems
                    foreach (Rule obj in database.Rules)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "rule"));
                    }
                    foreach (SqlAssembly obj in database.Assemblies)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "assembly"));
                    }
                    foreach (Default obj in database.Defaults)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "default"));
                    }
                    foreach (PlanGuide obj in database.PlanGuides)
                    {
                        result.Add(new ObjectInfo(database.Name, obj, "plan guide"));
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
