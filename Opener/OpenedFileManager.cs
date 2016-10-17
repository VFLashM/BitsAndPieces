using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using EnvDTE80;
using Microsoft.SqlServer.Management.UI.VSIntegration.Editors;
using EnvDTE;

namespace Opener
{
    class OpenedFileManager
    {
        private DTE2 _applicationObject;
        private Dictionary<string, Document> _openedDocuments = new Dictionary<string,Document>();

        public OpenedFileManager(DTE2 applicationObject)
        {
            _applicationObject = applicationObject;
            // does not work for some reason: _applicationObject.Events.DocumentEvents.DocumentClosing
            _applicationObject.Events.WindowEvents.WindowClosing += new _dispWindowEvents_WindowClosingEventHandler(WindowEvents_WindowClosing); 
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

        public void Open(string key, string text)
        {
            Document existingDocument;
            if (_openedDocuments.TryGetValue(key, out existingDocument))
            {
                existingDocument.Activate();
            }
            else
            {
                var script = ServiceCache.ScriptFactory.CreateNewBlankScript(ScriptType.Sql) as SqlScriptEditorControl;
                script.EditorText = text;
                _openedDocuments[key] = _applicationObject.ActiveDocument;

                string fullPath = Properties.Settings.Default.ResolveProjectRoot();
                if (!fullPath.EndsWith("\\")) 
                {
                    fullPath += '\\';
                }
                fullPath += key.Replace('.', '\\').Replace(':', '_') + ".sql";
                _applicationObject.ActiveDocument.Save(fullPath);
            }
        }
    }
}
