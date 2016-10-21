using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Opener
{
    class QuickOpen
    {
        static Common.ChooseItem.SearchTerm[] CreateSearchTerms(string name, string type)
        {
            var searchTerms = new List<Common.ChooseItem.SearchTerm>();
            searchTerms.Add(new Common.ChooseItem.SearchTerm(type, 0));
            searchTerms.Add(new Common.ChooseItem.SearchTerm(name, 1));
            int priority = 2;
            foreach (var part in name.Split('.'))
            {
                foreach (var term in part.Split(':'))
                {
                    searchTerms.Add(new Common.ChooseItem.SearchTerm(term, priority));
                }
                ++priority;
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
                strToUrn[obj.name] = obj.urn;
                var searchTerms = CreateSearchTerms(obj.name, obj.type);
                items.Add(new Common.ChooseItem.Item(obj.name, obj.type, searchTerms));
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
