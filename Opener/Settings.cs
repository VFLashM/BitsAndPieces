using System;
using System.Collections.Generic;
using System.Linq;

namespace Opener.Properties {
    
    
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings {

        public static void CheckAndSave()
        {
            try
            {
                Default.ResolveProjectRoot();
                Default.GetDatabases();
                Default.GetSchemas();
                Default.Save();
            }
            catch (Common.Error e)
            {
                e.Show();
            }
        }

        public string ResolveProjectRoot()
        {
            string rootPath = Environment.ExpandEnvironmentVariables(ProjectRoot);
            if (!System.IO.Path.IsPathRooted(rootPath))
            {
                throw new Common.Error("Project root path is not absolute:\n" + rootPath + "\n\nChange it in Tools->Options->Bits and Pieces->Opener", "Configuration error");
            }
            return rootPath;
        }

        static string[] ParseCommaSeparated(string text, string helpName)
        {
            char[] separators = { ',' };
            var parts = text.Split(separators).ToList();
            var result = parts.Select(db => db.Trim()).Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
            if (result.Count == 0)
            {
                throw new Common.Error("No " + helpName + " specified\nList " + helpName + " in Tools->Options->Bits and Pieces->Opener", "Configuration error");
            }
            return result.ToArray();
        }

        public string[] GetDatabases()
        {
            return ParseCommaSeparated(Databases, "databases");
        }

        public string[] GetSchemas()
        {
            return ParseCommaSeparated(Schemas, "schemas");
        }

        protected override void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            Save();
        }
    }
}
