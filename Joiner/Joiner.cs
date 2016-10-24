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

            var contextDatabases = new HashSet<string>();
            foreach (TableInfo t in context.AllTables())
            {
                tableAccessor.ResolveTable(t);
                contextDatabases.Add(t.Database());
            }

            var rules = JoinParser.ParseCustomRules(Properties.Settings.Default.CustomRules);
            foreach (var rule in rules)
            {
                foreach (var t in rule.AllTables())
                {
                    tableAccessor.ResolveTable(t);
                }
            }
            foreach (var contextDatabase in contextDatabases)
            {
                rules.AddRange(tableAccessor.GetForeignKeyRules(contextDatabase));
            }
            foreach (var t1 in context.AllTables())
            {
                foreach (var t2 in context.AllTables())
                {
                    if (t1 != t2)
                    {
                        bool hasRule = false;
                        foreach (var rule in rules)
                        {
                            if (rule.Match(t1, t2))
                            {
                                hasRule = true;
                                break;
                            }
                        }
                        if (hasRule)
                        {
                            continue;
                        }
                        var generated = RuleGenerator.Create(t1, t2);
                        if (generated != null)
                        {
                            rules.AddRange(generated);
                        }
                    }
                }
            }

            var options = new List<Tuple<string, string, int>>();
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
                                applied = "\n  on " + applied;
                            }
                            options.Add(Tuple.Create(applied, rule.name, rule.priority));
                        }
                    }
                }
            }
            else
            {
                var usedAliases = new List<string>();
                foreach (var table in context.joinedTables)
                {
                    usedAliases.Add(table.Alias());
                }
                foreach (var rule in rules)
                {
                    foreach (var table in context.joinedTables)
                    {
                        var matched = rule.Match(table);
                        if (matched != null)
                        {
                            matched = matched.NewWithUniqueAlias(usedAliases);
                            string applied = context.hasGlue ? "" : "\njoin ";
                            applied += matched.Def(database) + "\n  on ";
                            applied += rule.Apply(table, matched);
                            options.Add(Tuple.Create(applied, rule.name, rule.priority));
                        }
                    }
                }
            }

            if (options.Count == 0)
            {
                return false;
            }

            var items = new List<Common.ChooseItem.Item>();
            foreach (var option in options)
            {
                var searchTerms = new List<Common.ChooseItem.SearchTerm>();
                searchTerms.Add(new Common.ChooseItem.SearchTerm(option.Item1, option.Item3 + 100));
                searchTerms.Add(new Common.ChooseItem.SearchTerm(option.Item2, option.Item3));
                items.Add(new Common.ChooseItem.Item(option.Item1, option.Item2, searchTerms.ToArray()));
            }

            Common.ChooseItem dialog = new Common.ChooseItem(items.ToArray(), null);
            PlaceFormAtCaret(application, dialog);
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return false;
            }
            string result = dialog.Result();

            if (result.StartsWith("\n") && Regex.IsMatch(body, @"\n\s*$"))
            {
                result = result.Substring(1);
            }
            if (!Regex.IsMatch(result, @"^\s") && !Regex.IsMatch(body, @"\s$"))
            {
                result = " " + result;
            }
            result = result.Replace("\n", "\n" + context.fromIndent);
            result = result.Replace("\n", "\r\n"); // back to crlf
            textDoc.Selection.Insert(result);
            return true;
        }
    }
}

