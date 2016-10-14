using System;
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
                ResolveProjectRoot();
                Default.Save();
            }
            catch (Error e)
            {
                e.Show();
            }
        }

        public static string ResolveProjectRoot()
        {
            string rootPath = Environment.ExpandEnvironmentVariables(Default.ProjectRoot);
            if (!System.IO.Path.IsPathRooted(rootPath))
            {
                throw new Error("Project root path is not absolute:\n" + rootPath + "\n\nChange it in Tools->Options->Bits and Pieces->Opener", "Configuration error");
            }
            return rootPath;
        }

        protected override void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            Save();
        }
    }
}
