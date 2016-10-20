using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;

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

            return false;
        }
    }
}
