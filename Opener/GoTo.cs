using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using System.Windows.Forms;

namespace Opener
{
    class GoTo
    {
        static bool IsId(char c)
        {
            return Char.IsLetter(c) 
                || Char.IsDigit(c) 
                || c == '.' 
                || c == '_'
                || c == '@'
                || c == '#'
                || c == '$';
        }

        static string WordUnderCursor(DTE2 application)
        {
            Document doc = application.ActiveDocument;
            if (doc == null) 
            {
                return null;
            }
            TextDocument textDoc = doc.Object("TextDocument") as TextDocument;
            if (textDoc == null)
            {
                return null;
            }

            var editPoint = textDoc.StartPoint.CreateEditPoint();
            string text = editPoint.GetText(textDoc.EndPoint);
            text = text.Replace("\r\n", "\n");

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
                return null;
            }
            return text.Substring(from, to - from);
        }

        static public bool Execute(DTE2 application, OpenedFileManager openedFileManager)
        {
            string name = WordUnderCursor(application);
            if (name == null)
            {
                return false;
            }

            var accessor = new ObjectAccessor();
            ObjectAccessor.ObjectInfo info = accessor.FindObject(name);
            if (info == null)
            {
                return false;
            }

            string body = accessor.GetObjectText(info.urn);
            openedFileManager.Open(info.name, body);
            return true;
        }
    }
}
