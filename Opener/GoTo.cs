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
        static string WordUnderCursor(DTE2 application)
        {
            Document doc = application.ActiveDocument;
            if (doc == null) {
                return null;
            }
            TextDocument textDoc = doc.Object("TextDocument") as TextDocument;

            var ep = textDoc.StartPoint.CreateEditPoint();
            string s = ep.GetText(textDoc.EndPoint);
            int offset = textDoc.Selection.ActivePoint.AbsoluteCharOffset;
            while (offset > 0 && (Char.IsLetter(s[offset-1]) || Char.IsDigit(s[offset-1])))
            {
                offset -= 1;
            }
            int to = offset;
            while (to < s.Length && (Char.IsLetter(s[to+1]) || Char.IsDigit(s[to+1])))
            {
                to += 1;
            }
            string ssss = s.Substring(offset, to - offset + 1);
            return ssss;
        }

        static public bool Execute(DTE2 application)
        {
            MessageBox.Show(WordUnderCursor(application));
            return true;
        }
    }
}
