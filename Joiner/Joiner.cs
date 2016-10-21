using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Shell;
using System.Windows.Forms;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.Internal.VisualStudio.PlatformUI;
using System.Drawing;

namespace Joiner
{
    class Joiner
    {
        class CaretLocation
        {
            public readonly int left;
            public readonly int top;
            public readonly int bottom;

            public CaretLocation(int left, int top, int bottom)
            {
                this.left = left;
                this.top = top;
                this.bottom = bottom;
            }
        }

        static CaretLocation GetCaretLocation(DTE2 application)
        {
            // dark magic goes here

            IVsUIHierarchy uiHierarchy;
            uint itemID;
            IVsWindowFrame windowFrame;
            if (!VsShellUtilities.IsDocumentOpen(ServiceCache.ServiceProvider, application.ActiveDocument.FullName, Guid.Empty,
                                    out uiHierarchy, out itemID, out windowFrame))
            {
                return null;
            }

            IVsTextView vsTextView = Microsoft.VisualStudio.Shell.VsShellUtilities.GetTextView(windowFrame);
            IVsUserData userData = vsTextView as IVsUserData;
            if (userData == null) return null;

            object holder;
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guidViewHost, out holder);
            IWpfTextViewHost viewHost = holder as IWpfTextViewHost;
            if (viewHost == null) return null;

            IWpfTextView wpfTextView = viewHost.TextView;
            System.Windows.UIElement uiElement = wpfTextView as System.Windows.UIElement;
            if (uiElement == null) return null;

            var caretPos = wpfTextView.Caret.Position.BufferPosition;
            var bounds = wpfTextView.GetTextViewLineContainingBufferPosition(caretPos).GetCharacterBounds(caretPos);

            double zoomMultiplier = wpfTextView.ZoomLevel / 100.0;
            double left = (bounds.Left - wpfTextView.ViewportLeft) * zoomMultiplier;
            double top = (bounds.Top - wpfTextView.ViewportTop) * zoomMultiplier;
            double bottom = (bounds.Bottom - wpfTextView.ViewportTop) * zoomMultiplier;

            System.Windows.Point topPoint = new System.Windows.Point(left, top);
            System.Windows.Point bottomPoint = new System.Windows.Point(left, bottom);
            topPoint = uiElement.PointToScreen(topPoint);
            bottomPoint = uiElement.PointToScreen(bottomPoint);

            return new CaretLocation(
                Convert.ToInt32(topPoint.X),
                Convert.ToInt32(topPoint.Y),
                Convert.ToInt32(bottomPoint.Y)
            );
        }

        static void PlaceFormAtCaret(DTE2 application, Form form)
        {
            var location = GetCaretLocation(application);
            if (location == null)
            {
                return;
            }
            int center = (location.top + location.bottom) / 2;
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.Contains(new Point(location.left, center)))
                {
                    form.StartPosition = FormStartPosition.Manual;
                    int x = Math.Min(location.left, screen.WorkingArea.Right - form.Width);
                    x = Math.Max(screen.WorkingArea.Left, x);

                    int offset = 5;
                    int y = location.bottom + offset;
                    if ((y + form.Height) > screen.WorkingArea.Bottom)
                    {
                        y = location.top - offset - form.Height;
                    }
                    y = Math.Max(screen.WorkingArea.Top, y);

                    form.Location = new Point(x, y);
                }
            }
        }

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
            ContextInfo context = JoinParser.ParseContext(body);
            if (context == null)
            {
                return false;
            }

            string database = Common.Connection.GetActiveDatabase(body);
            var tableAccessor = new TableAccessor(database);
            var rules = new List<Rule>();
            foreach (TableInfo t in context.joinedTables)
            {
                var trules = tableAccessor.ResolveTable(t);
                if (trules != null)
                {
                    rules.AddRange(trules);
                }
            }
            if (context.newTable != null)
            {
                var trules = tableAccessor.ResolveTable(context.newTable);
                if (trules != null)
                {
                    rules.AddRange(trules);
                }
            }

            var options = new List<string>();
            if (context.newTable != null)
            {
                foreach (var rule in rules)
                {
                    foreach (var table in context.joinedTables)
                    {
                        var applied = rule.Apply(table, context.newTable);
                        if (applied != null)
                        {
                            if (!context.hasGlue)
                            {
                                applied = "on " + applied;
                            }
                            options.Add(applied);
                        }
                    }
                }
            }
            else
            {
                foreach (var rule in rules)
                {
                    foreach (var table in context.joinedTables)
                    {
                        var matched = rule.Match(table);
                        if (matched != null)
                        {
                            string applied = context.hasGlue ? "" : "join ";
                            applied += matched.Def() + " on ";
                            applied += rule.Apply(table, matched);
                            options.Add(applied);
                        }
                    }
                }
            }

            var items = new List<Common.ChooseItem.Item>();
            foreach (var option in options)
            {
                items.Add(new Common.ChooseItem.Item(option, "rule"));
            }

            Common.ChooseItem dialog = new Common.ChooseItem(items.ToArray(), null);
            PlaceFormAtCaret(application, dialog);
            dialog.ShowDialog();

            return context != null;
        }
    }
}

