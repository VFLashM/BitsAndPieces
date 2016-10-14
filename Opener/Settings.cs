using System;
namespace Opener.Properties {
    
    
    // This class allows you to handle specific events on the settings class:
    //  The SettingChanging event is raised before a setting's value is changed.
    //  The PropertyChanged event is raised after a setting's value is changed.
    //  The SettingsLoaded event is raised after the setting values are loaded.
    //  The SettingsSaving event is raised before the setting values are saved.
    internal sealed partial class Settings {
        
        public Settings() {
            // // To add event handlers for saving and changing settings, uncomment the lines below:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
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
        
        /*
        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // 
        }
        
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // Add code to handle the SettingsSaving event here.
        }
         */
    }
}
