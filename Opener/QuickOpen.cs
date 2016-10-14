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
            foreach (var obj in objects)
            {
                strToUrn[obj.name] = obj.urn;
            }

            var choose = new ChooseItem(strToUrn.Keys.ToArray());
            if (choose.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return false;
            }

            string result = choose.Result();
            Urn resultUrn = strToUrn[result];
            string body = accessor.GetObjectText(resultUrn);
            openedFileManager.Open(result, body);
            return true;
        }
    }
}
