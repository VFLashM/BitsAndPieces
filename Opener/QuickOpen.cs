using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Opener
{
    class QuickOpen
    {
        public static bool Execute(OpenedFileManager openedFileManager)
        {
            var accessor = new ObjectAccessor();
            var objects = accessor.GetObjects();

            var strToUrn = new Dictionary<string, Urn>();
            var items = new List<ChooseItem.Item>();
            foreach (var obj in objects)
            {
                strToUrn[obj.name] = obj.urn;
                items.Add(new ChooseItem.Item(obj.name, obj.type, obj.name.Replace(':', '.').Split('.')));
            }

            string title = "Choose object on " + accessor.ServerName();
            var choose = new ChooseItem(items.ToArray(), title);
            if (choose.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return false;
            }

            string result = choose.Result();
            Urn resultUrn = strToUrn[result];
            string body = accessor.GetObjectText(resultUrn);
            openedFileManager.Open(result, resultUrn, body);
            return true;
        }
    }
}
