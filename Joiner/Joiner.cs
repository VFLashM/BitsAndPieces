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
            new Common.ChooseItem(items.ToArray(), null).ShowDialog();

            return context != null;
        }
    }
}
