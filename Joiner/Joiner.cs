using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using System.Text.RegularExpressions;

namespace Joiner
{
    class Joiner
    {
        static public bool Execute(DTE2 application)
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
            text = text.Replace("\r\n", "\n");
            int cursorPos = textDoc.Selection.ActivePoint.AbsoluteCharOffset - 1;

            string body = text.Substring(0, cursorPos);

            ContextInfo context = Parser.Parse(body);
            return context != null;
        }
    }
}
