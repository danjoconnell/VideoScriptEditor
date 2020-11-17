using System.ComponentModel;

namespace VideoScriptEditor.Settings
{
    /// <summary>
    /// Interface abstracting a model that encapsulates application settings and related I/O operations.
    /// </summary>
    public interface IApplicationSettings : INotifyPropertyChanged
    {
        /// <summary>
        /// The default frame duration for new segments added to the timeline.
        /// </summary>
        int NewSegmentFrameDuration { get; set; }

        /// <summary>
        /// Whether to create a backup before overwriting an existing file when saving a project.
        /// </summary>
        bool CreateProjectBackupWhenSaving { get; set; }
    }
}
