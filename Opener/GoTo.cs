using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.UI.VSIntegration;

namespace Opener
{
    class GoTo
    {
        static bool IsId(char c)
        {
            return Char.IsLetter(c) 
                || Char.IsDigit(c) 
                || "._@#$".Contains(c);
        }

        static public bool Execute(DTE2 application, OpenedFileManager openedFileManager)
        {
            Document doc = application.ActiveDocument;
            if (doc == null)
            {
                return false;
            }
            TextDocument textDoc = doc.Object("TextDocument") as TextDocument;
            if (textDoc == null)
            {
                return false;
            }

            var editPoint = textDoc.StartPoint.CreateEditPoint();
            string text = editPoint.GetText(textDoc.EndPoint);
            text = text.Replace(Environment.NewLine, "\n");

            int from = textDoc.Selection.ActivePoint.AbsoluteCharOffset - 1;
            while (from > 0 && IsId(text[from - 1]))
            {
                from -= 1;
            }
            int to = from;
            while (to < text.Length && IsId(text[to]))
            {
                to += 1;
            }
            if (from == to)
            {
                return false;
            }
            string name = text.Substring(from, to - from);

            string database = Common.Connection.GetActiveDatabase(text, from);

            var accessor = new ObjectAccessor();
            ObjectAccessor.ObjectInfo info = accessor.FindObject(name, database);
            if (info == null)
            {
                return false;
            }

            string body = accessor.GetObjectText(info.urn);
            openedFileManager.Open(accessor.ServerName(), info.fullName, info.urn, body);
            return true;
        }
    }
}
