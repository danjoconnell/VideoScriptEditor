using Prism.Mvvm;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VideoScriptEditor.Settings
{
    /// <summary>
    /// Model encapsulating application settings and related I/O operations.
    /// </summary>
    public class ApplicationSettings : BindableBase, IApplicationSettings
    {
        private int _newSegmentFrameDuration;
        private bool _createProjectBackupWhenSaving;

        /// <inheritdoc cref="IApplicationSettings.NewSegmentFrameDuration"/>
        public int NewSegmentFrameDuration
        {
            get => _newSegmentFrameDuration;
            set => SetProperty(ref _newSegmentFrameDuration, Math.Max(value, 1), OnSettingValueChanged);
        }

        /// <inheritdoc cref="IApplicationSettings.CreateProjectBackupWhenSaving"/>
        public bool CreateProjectBackupWhenSaving
        {
            get => _createProjectBackupWhenSaving;
            set => SetProperty(ref _createProjectBackupWhenSaving, value, OnSettingValueChanged);
        }

        /// <summary>
        /// Whether any of the settings have changed since the application was loaded.
        /// </summary>
        [JsonIgnore]
        public bool SettingValuesChangedSinceLoad { get; private set; } = false;

        /// <summary>
        /// Creates a new <see cref="ApplicationSettings"/> instance.
        /// </summary>
        public ApplicationSettings()
        {
            // Defaults
            _newSegmentFrameDuration = 10;
            _createProjectBackupWhenSaving = true;
        }

        /// <summary>
        /// Invoked whenever a setting property value changes.
        /// </summary>
        private void OnSettingValueChanged()
        {
            SettingValuesChangedSinceLoad = true;
        }

        /// <summary>
        /// Serializes and saves the settings to the specified file.
        /// </summary>
        /// <param name="fileName">The file name to save the serialized settings to.</param>
        public void SaveToFile(string fileName)
        {
            if (SettingValuesChangedSinceLoad || !File.Exists(fileName))
            {
                string jsonString = JsonSerializer.Serialize(this,
                                                             new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(fileName, jsonString);
            }
        }

        /// <summary>
        /// Loads and deserializes the settings from the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to deserialize the settings from.</param>
        /// <returns>An <see cref="ApplicationSettings"/> instance containing the deserialized settings.</returns>
        public static ApplicationSettings LoadFromFile(string fileName)
        {
            ApplicationSettings applicationSettings = null;

            if (File.Exists(fileName))
            {
                string jsonString = File.ReadAllText(fileName);
                applicationSettings = JsonSerializer.Deserialize<ApplicationSettings>(jsonString);
            }

            return applicationSettings ?? new ApplicationSettings();
        }
    }
}
