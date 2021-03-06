﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Precomplete.Properties {
    
    
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

        static string[] ParseCommaSeparated(string text, string helpName)
        {
            char[] separators = { ',' };
            var parts = text.Split(separators).ToList();
            var result = parts.Select(db => db.Trim()).Where(s => !String.IsNullOrWhiteSpace(s)).ToList();
            return result.ToArray();
        }

        public string[] GetDatabases()
        {
            return ParseCommaSeparated(Databases, "databases");
        }

        protected override void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            Save();
        }
    }
}
