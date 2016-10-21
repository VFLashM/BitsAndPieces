using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Opener
{
    class QuickOpen
    {
        static Common.ChooseItem.SearchTerm[] CreateSearchTerms(ObjectAccessor.ObjectInfo obj)
        {
            var searchTerms = new List<Common.ChooseItem.SearchTerm>();
            searchTerms.Add(new Common.ChooseItem.SearchTerm(obj.type, 0));
            searchTerms.Add(new Common.ChooseItem.SearchTerm(obj.fullName, 1));
            searchTerms.Add(new Common.ChooseItem.SearchTerm(obj.database, 2));
            if (obj.schema != null)
            {
                searchTerms.Add(new Common.ChooseItem.SearchTerm(obj.schema, 2));
            }
            searchTerms.Add(new Common.ChooseItem.SearchTerm(obj.name, 3));
            if (obj.subname != null)
            {
                searchTerms.Add(new Common.ChooseItem.SearchTerm(obj.subname, 3));
            }
            return searchTerms.ToArray();
        }

        public static bool Execute(OpenedFileManager openedFileManager)
        {
            var accessor = new ObjectAccessor();
            var objects = accessor.GetObjects();

            var strToUrn = new Dictionary<string, Urn>();
            var items = new List<Common.ChooseItem.Item>();
            foreach (var obj in objects)
            {
                strToUrn[obj.fullName] = obj.urn;
                var searchTerms = CreateSearchTerms(obj);
                items.Add(new Common.ChooseItem.Item(obj.fullName, obj.type, searchTerms));
            }

            string title = "Choose object on " + accessor.ServerName();
            var choose = new Common.ChooseItem(items.ToArray(), title);
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
