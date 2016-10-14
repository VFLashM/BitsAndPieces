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
                Default.Save();
            }
            catch (Error e)
            {
                e.Show();
            }
        }

        public string ResolveProjectRoot()
        {
            string rootPath = Environment.ExpandEnvironmentVariables(ProjectRoot);
            if (!System.IO.Path.IsPathRooted(rootPath))
            {
                throw new Error("Project root path is not absolute:\n" + rootPath + "\n\nChange it in Tools->Options->Bits and Pieces->Opener", "Configuration error");
            }
            return rootPath;
        }

        public string[] GetDatabases()
        {
            char[] separators = { ',' };
            var parts = Databases.Split(separators).ToList();
            var result = parts.Select(db => db.Trim()).Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
            if (result.Count == 0)
            {
                throw new Error("No databases specified\nList databases in Tools->Options->Bits and Pieces->Opener", "Configuration error");
            }
            return result.ToArray();
        }

        protected override void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            Save();
        }
    }
}
