using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using EnvDTE80;
using Microsoft.SqlServer.Management.UI.VSIntegration.Editors;
using EnvDTE;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Opener
{
    class OpenedFileManager
    {
        private DTE2 _applicationObject;
        private Dictionary<string, Document> _openedDocuments = new Dictionary<string,Document>();
        CommandEvents _executeEvents;

        public OpenedFileManager(DTE2 applicationObject)
        {
            _applicationObject = applicationObject;
            // does not work for some reason: _applicationObject.Events.DocumentEvents.DocumentClosing
            _applicationObject.Events.WindowEvents.WindowClosing += new _dispWindowEvents_WindowClosingEventHandler(WindowEvents_WindowClosing);

            var executeCommand = _applicationObject.Commands.Item("Query.Execute");
            _executeEvents = _applicationObject.Events.CommandEvents[executeCommand.Guid, executeCommand.ID];
            _executeEvents.AfterExecute += new _dispCommandEvents_AfterExecuteEventHandler(executeSqlEvents_AfterExecute);
        }

        void executeSqlEvents_AfterExecute(string Guid, int ID, object CustomIn, object CustomOut)
        {
            if (_openedDocuments.ContainsValue(_applicationObject.ActiveDocument))
            {
                var textSelection = _applicationObject.ActiveDocument.Selection as TextSelection;
                if (textSelection.IsEmpty)
                {
                    _applicationObject.ActiveDocument.Save();
                }
            }
        }

        void WindowEvents_WindowClosing(Window Window)
        {
            var keysToPurge = new List<string>();
            foreach (var pair in _openedDocuments)
            {
                bool found = false;
                foreach (var doc in _applicationObject.Documents)
                {
                    if (doc == pair.Value)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    keysToPurge.Add(pair.Key);
                }
            }
            foreach (var key in keysToPurge)
            {
                _openedDocuments.Remove(key);
            }
        }

        public void Open(string name, Urn urn, string text)
        {
            string key = urn.ToString();
            IObjectExplorerService objExplorer = ServiceCache.ServiceProvider.GetService(typeof(IObjectExplorerService)) as IObjectExplorerService;
            var node = objExplorer.FindNode(key);
            if (node != null)
            {
                objExplorer.SynchronizeTree(node);
            }

            Document existingDocument;
            if (_openedDocuments.TryGetValue(key, out existingDocument))
            {
                existingDocument.Activate();
            }
            else if (text != null)
            {
                var script = ServiceCache.ScriptFactory.CreateNewBlankScript(ScriptType.Sql) as SqlScriptEditorControl;
                script.EditorText = text;
                _openedDocuments[key] = _applicationObject.ActiveDocument;

                string fullPath = Properties.Settings.Default.ResolveProjectRoot();
                if (!fullPath.EndsWith("\\"))
                {
                    fullPath += '\\';
                }
                fullPath += name.Replace('.', '\\').Replace(':', '_') + ".sql";
                _applicationObject.ActiveDocument.Save(fullPath);
            }
        }
    }
}
