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
using System.Data;

namespace Opener
{
    class ObjectAccessor
    {
        Dictionary<string, string> typeUrnMap = new Dictionary<string, string>{
            // objects
            {"AGGREGATE_FUNCTION",               "UserDefinedAggregate"},
            {"CHECK_CONSTRAINT",                 "Check"},
            {"CLR_SCALAR_FUNCTION",              "UserDefinedFunction"},
            {"CLR_STORED_PROCEDURE",             "StoredProcedure"},
            {"CLR_TABLE_VALUED_FUNCTION",        "UserDefinedFunction"},
            {"CLR_TRIGGER",                      "Trigger"},
            {"DEFAULT_CONSTRAINT",               null /* "Column/Default" */}, // needs column name
            {"EXTENDED_STORED_PROCEDURE",        "ExtendedStoredProcedure"},
            {"FOREIGN_KEY_CONSTRAINT",           "ForeignKey"},
            {"INTERNAL_TABLE",                   null}, // not accessible via urn
            {"PLAN_GUIDE",                       "PlanGuide"},
            {"PRIMARY_KEY_CONSTRAINT",           null}, // found as Index, but hangs IObjectExplorerService.FindNode
            {"REPLICATION_FILTER_PROCEDURE",     null}, // is it useful ?
            {"RULE",                             "Rule"},
            {"SEQUENCE_OBJECT",                  "Sequence"},
            {"SERVICE_QUEUE",                    null}, // is it useful ?
            {"SQL_INLINE_TABLE_VALUED_FUNCTION", "UserDefinedFunction"},
            {"SQL_SCALAR_FUNCTION",              "UserDefinedFunction"},
            {"SQL_STORED_PROCEDURE",             "StoredProcedure"},
            {"SQL_TABLE_VALUED_FUNCTION",        "UserDefinedFunction"},
            {"SQL_TRIGGER",                      "Trigger"},
            {"SYNONYM",                          "Synonym"},
            {"SYSTEM_TABLE",                     null}, // not accessible via urn
            {"TYPE_TABLE",                       null}, // found in sys.types
            {"UNIQUE_CONSTRAINT",                null}, // found as Index, but hangs IObjectExplorerService.FindNode
            {"USER_TABLE",                       "Table"},
            {"VIEW",                             "View"},
            
            // indices
            {"CLUSTERED_INDEX",                  "Index"},
            {"NONCLUSTERED_INDEX",               "Index"},

            // types
            {"TABLE_TYPE",                       "UserDefinedTableType"},
            {"CLR_TYPE",                         "UserDefinedType"},
            {"DATA_TYPE",                        "UserDefinedDataType"},
        };

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

            public ObjectInfo(string database, string schema, string name, string subname, Urn urn, string type)
            {
                this.type = type;
                this.database = database;
                this.schema = schema;
                this.name = name;
                this.subname = subname;
                this.urn = urn;
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
                if (name == info.fullName ||
                    name == info.subname ||
                    (info.subname == null && (
                        name == info.database + ".." + info.name ||
                        name == info.schema + "." + info.name ||
                        name == info.name
                     )
                    )
                   )
                {
                    return info;
                }
            }
            return null;
        }

        public ObjectInfo FindObject(string name, string databaseHint)
        {
            var objects = GetObjects(databaseHint, false);
            return MatchObject(objects, databaseHint + "." + name) 
                ?? MatchObject(objects, databaseHint + ".." + name)
                ?? MatchObject(objects, name);
        }

        public string CleanupObjectType(string type)
        {
            var res = type.ToLower().Replace('_', ' ');
            var sqlPrefix = "sql ";
            if (res.StartsWith(sqlPrefix))
            {
                res = res.Substring(sqlPrefix.Length);
            }
            return res;
        }

        public string AppendUrn(string urn, string type, string schema, string name)
        {
            if (urn == null)
            {
                return null;
            }
            if (!typeUrnMap.ContainsKey(type))
            {
                return null;
            }
            var urnType = typeUrnMap[type];
            if (urnType == null)
            {
                return null;
            }
            if (schema != null)
            {
                urn += String.Format("/{0}[@Name='{1}' and @Schema='{2}']", urnType, name, schema);
            }
            else
            {
                urn += String.Format("/{0}[@Name='{1}']", urnType, name);
            }
            return urn;
        }

        public List<ObjectInfo> GetObjects(string databaseHint = null, bool filterSchemas = true)
        {
            var result = new List<ObjectInfo>();
            string[] databases = Properties.Settings.Default.GetDatabases();
            string[] schemas = Properties.Settings.Default.GetSchemas();

            foreach (Database database in _server.Databases)
            {
                if (databases.Contains(database.Name) || (database.Name == databaseHint))
                {
                    if (!database.IsAccessible)
                    {
                        continue;
                    }
                    var dataSet = database.ExecuteWithResults(@"
select SCHEMA_NAME(o.schema_id), o.name, o.type_desc,
       SCHEMA_NAME(p.schema_id), p.name, p.type_desc
from sys.objects o
left outer join sys.objects p
  on o.parent_object_id != 0 and o.parent_object_id = p.object_id

union all

select null, i.name, i.type_desc + '_INDEX',
       SCHEMA_NAME(o.schema_id), o.name, o.type_desc
from sys.indexes i
join sys.objects o
  on i.object_id = o.object_id
where i.name is not null -- ignore heaps

union all

select SCHEMA_NAME(schema_id), name, case 
        when is_table_type = 1 then 'TABLE_TYPE'
        when is_assembly_type = 1 then 'CLR_TYPE'
        else 'DATA_TYPE'
    end,
    null, null, null
from sys.types
where is_user_defined = 1
");
                    var table = dataSet.Tables[0];
                    string baseUrn = String.Format("Server[@Name='{0}']/Database[@Name='{1}']", _server.NetName, database.Name);
                    foreach (DataRow row in table.Rows)
                    {
                        string objSchema = row[0] as string;
                        string objName = row[1] as string;
                        string objType = row[2] as string;
                        string parentSchema = row[3] as string;
                        string parentName = row[4] as string;
                        string parentType = row[5] as string;

                        if (filterSchemas && !schemas.Contains(objSchema) && !schemas.Contains(parentSchema))
                        {
                            continue;
                        }

                        string urn = baseUrn;
                        if (parentName != null)
                        {
                            urn = AppendUrn(urn, parentType, parentSchema, parentName);
                        }
                        urn = AppendUrn(urn, objType, objSchema, objName);

                        if (urn == null)
                        {
                            continue;
                        }

                        /*
                        try
                        {
                            var obj = _server.GetSmoObject(urn);
                        } 
                        catch (FailedOperationException e)
                        {
                            foreach (DataRow drow in database.EnumObjects().Rows)
                            {
                                if ((drow[2] as string) == objName)
                                {
                                    var durn = drow[3] as Urn;
                                }
                            }
                            continue;
                        }
                         */

                        result.Add(new ObjectInfo(
                            database.Name, 
                            parentSchema ?? objSchema, 
                            parentName ?? objName,
                            parentName != null ? objName : null,
                            urn != null ? new Urn(urn) : null,
                            CleanupObjectType(objType)));
                    }
                }
            }
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
