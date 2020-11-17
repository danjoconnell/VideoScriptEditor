using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Models;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.Dialog;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.Settings;
using VideoScriptEditor.ViewModels.Common;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.ViewModels
{
    /// <summary>
    /// View Model encapsulating presentation logic for the Main Window of the application.
    /// </summary>
    public class MainWindowViewModel : BindableBase, IDestructible
    {
        private const string BASE_TITLE_STRING = "Video Script Editor";
        private const string NEW_PROJECT_TITLE_STRING = BASE_TITLE_STRING + " - Untitled";

        private readonly IScriptVideoService _scriptVideoService;
        private readonly IProjectService _projectService;
        private readonly ISystemDialogService _systemDialogService;

        private string _title = NEW_PROJECT_TITLE_STRING;
        private bool _hasProject = false;
        private LabelledZoomLevel _sourceVideoZoomLevel = null;
        private double _sourceVideoPresentationWidth = double.NaN;  // Fit
        private double _sourceVideoPresentationHeight = double.NaN; // Fit

        /// <summary>
        /// Gets the runtime context of the <see cref="IScriptVideoService"/> instance.
        /// </summary>
        public IScriptVideoContext ScriptVideoContext { get; }

        /// <summary>
        /// Gets or sets the title that will appear in the Window title bar.
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// The <see cref="IApplicationSettings"/> model instance encapsulating application settings and related I/O operations.
        /// </summary>
        public IApplicationSettings Settings { get; }

        /// <summary>
        /// The <see cref="IApplicationCommands"/> instance providing a common set of application related commands.
        /// </summary>
        public IApplicationCommands ApplicationCommands { get; }

        /// <summary>
        /// The <see cref="ITimelineCommands"/> instance providing a set of timeline related commands.
        /// </summary>
        public ITimelineCommands TimelineCommands { get; }

        /// <summary>
        /// Gets a collection of <see cref="LabelledZoomLevel">zoom levels</see>
        /// for the source video.
        /// </summary>
        public ObservableCollection<LabelledZoomLevel> SourceVideoZoomLevels { get; }

        /// <summary>
        /// Gets or sets the current source video <see cref="LabelledZoomLevel">zoom level</see>.
        /// </summary>
        public LabelledZoomLevel SourceVideoZoomLevel
        {
            get => _sourceVideoZoomLevel;
            set => SetProperty(ref _sourceVideoZoomLevel, value, OnSourceVideoZoomLevelPropertyChanged);
        }

        /// <summary>
        /// Gets the pixel width to use when presenting the source video.
        /// </summary>
        public double SourceVideoPresentationWidth
        {
            get => _sourceVideoPresentationWidth;
            private set => SetProperty(ref _sourceVideoPresentationWidth, value);
        }

        /// <summary>
        /// Gets the pixel height to use when presenting the source video.
        /// </summary>
        public double SourceVideoPresentationHeight
        {
            get => _sourceVideoPresentationHeight;
            private set => SetProperty(ref _sourceVideoPresentationHeight, value);
        }

        /// <summary>
        /// Gets whether there is a <see cref="ProjectModel">project</see> currently open.
        /// </summary>
        public bool HasProject
        {
            get => _hasProject;
            private set => SetProperty(ref _hasProject, value);
        }

        /// <summary>
        /// Command for creating a new <see cref="ProjectModel">project</see>.
        /// </summary>
        public DelegateCommand NewProjectCommand { get; }

        /// <summary>
        /// Command for opening a <see cref="ProjectModel">project</see> from a file.
        /// </summary>
        public DelegateCommand OpenProjectCommand { get; }

        /// <summary>
        /// Command for saving the current <see cref="ProjectModel">project</see> to a file.
        /// </summary>
        public DelegateCommand<SaveCommandOptions?> SaveProjectCommand { get; }

        /// <summary>
        /// Command for closing the current <see cref="ProjectModel">project</see>.
        /// </summary>
        public DelegateCommand CloseProjectCommand { get; }

        /// <summary>
        /// Command for importing an AviSynth script from a file into the current <see cref="ProjectModel">project</see>
        /// and <see cref="IScriptVideoService"/> instance.
        /// </summary>
        public DelegateCommand ImportScriptCommand { get; }

        /// <summary>
        /// Command for toggling between starting and pausing video playback.
        /// </summary>
        /// <remarks>
        /// On execution, <see cref="IScriptVideoService.StartVideoPlayback"/>
        /// or <see cref="IScriptVideoService.PauseVideoPlayback"/> is called depending on the value of <see cref="IScriptVideoContext.IsVideoPlaying"/>.
        /// </remarks>
        public DelegateCommand ToggleStartPauseVideoPlaybackCommand { get; }

        /// <summary>
        /// Command for binding to the <see cref="IScriptVideoService.StartVideoPlayback"/> service method.
        /// </summary>
        public DelegateCommand StartVideoPlaybackCommand { get; }

        /// <summary>
        /// Command for binding to the <see cref="IScriptVideoService.PauseVideoPlayback"/> service method.
        /// </summary>
        public DelegateCommand PauseVideoPlaybackCommand { get; }

        /// <summary>
        /// Gets a command for binding to the <see cref="IScriptVideoService.StopVideoPlayback"/> service method.
        /// </summary>
        public DelegateCommand StopVideoPlaybackCommand { get; }

        /// <summary>
        /// Command for stepping the current video frame forward.
        /// </summary>
        /// <remarks>Equivalent to incrementing <see cref="IScriptVideoContext.FrameNumber"/> by one.</remarks>
        public DelegateCommand VideoFrameStepForwardCommand { get; }

        /// <summary>
        /// Command for stepping the current video frame backward.
        /// </summary>
        /// <remarks>Equivalent to decrementing <see cref="IScriptVideoContext.FrameNumber"/> by one.</remarks>
        public DelegateCommand VideoFrameStepBackwardCommand { get; }

        /// <summary>
        /// Creates a new <see cref="MainWindowViewModel"/> instance.
        /// </summary>
        /// <param name="applicationSettings">The <see cref="IApplicationSettings"/> model instance encapsulating application settings and related I/O operations.</param>
        /// <param name="scriptVideoService">The <see cref="IScriptVideoService"/> instance for processing and previewing edited video.</param>
        /// <param name="projectService">
        /// The <see cref="IProjectService"/> instance providing access to a <see cref="ProjectModel">project</see>
        /// and performing related I/O operations.
        /// </param>
        /// <param name="systemDialogService">The <see cref="ISystemDialogService"/> instance for forwarding messages to the UI.</param>
        /// <param name="applicationCommands">The <see cref="IApplicationCommands"/> instance providing a common set of application related commands.</param>
        /// <param name="timelineCommands">The <see cref="ITimelineCommands"/> instance providing a set of timeline related commands.</param>
        public MainWindowViewModel(IApplicationSettings applicationSettings, IScriptVideoService scriptVideoService, IProjectService projectService, ISystemDialogService systemDialogService, IApplicationCommands applicationCommands, ITimelineCommands timelineCommands)
        {
            Settings = applicationSettings;

            _scriptVideoService = scriptVideoService;
            ScriptVideoContext = scriptVideoService.GetContextReference();

            _projectService = projectService;
            _projectService.ProjectOpened += OnProjectOpened;
            _projectService.ProjectClosed += OnProjectClosed;

            _systemDialogService = systemDialogService;

            ApplicationCommands = applicationCommands;

            TimelineCommands = timelineCommands;
            TimelineCommands.AddTrackSegmentCommandParameters.FrameDuration = Settings.NewSegmentFrameDuration;
            // For updating Settings.NewSegmentFrameDuration value from a AddTrackSegmentCommandParameters.FrameDuration property change.
            TimelineCommands.AddTrackSegmentCommandParameters.PropertyChanged += OnAddTrackSegmentCommandParametersPropertyChanged;

            NewProjectCommand = new DelegateCommand(NewProject);
            OpenProjectCommand = new DelegateCommand(OpenProject);
            SaveProjectCommand = new DelegateCommand<SaveCommandOptions?>(SaveProject).ObservesCanExecute(() => HasProject);
            CloseProjectCommand = new DelegateCommand(() => CloseProject()).ObservesCanExecute(() => HasProject);
            ImportScriptCommand = new DelegateCommand(ImportScript).ObservesCanExecute(() => HasProject);

            SourceVideoZoomLevels = new ObservableCollection<LabelledZoomLevel>(GetStandardVideoZoomLevels());
            SourceVideoZoomLevel = SourceVideoZoomLevels[0];  // 'Fit'

            ToggleStartPauseVideoPlaybackCommand = new DelegateCommand(ToggleStartPauseVideoPlayback)
                                                .ObservesCanExecute(() => ScriptVideoContext.HasVideo);

            StartVideoPlaybackCommand = new DelegateCommand(
                executeMethod: StartVideoPlayback,
                canExecuteMethod: () => ScriptVideoContext.HasVideo && !ScriptVideoContext.IsVideoPlaying
            );
            StartVideoPlaybackCommand.ObservesProperty(() => ScriptVideoContext.HasVideo)
                                     .ObservesProperty(() => ScriptVideoContext.IsVideoPlaying);

            PauseVideoPlaybackCommand = new DelegateCommand(PauseVideoPlayback)
                                     .ObservesCanExecute(() => ScriptVideoContext.IsVideoPlaying);

            StopVideoPlaybackCommand = new DelegateCommand(StopVideoPlayback)
                                    .ObservesCanExecute(() => ScriptVideoContext.HasVideo);

            VideoFrameStepForwardCommand = new DelegateCommand(
                executeMethod: () => ScriptVideoContext.FrameNumber++,
                canExecuteMethod: () => ScriptVideoContext.HasVideo && ScriptVideoContext.FrameNumber < ScriptVideoContext.SeekableVideoFrameCount
            );
            VideoFrameStepForwardCommand.ObservesProperty(() => ScriptVideoContext.HasVideo)
                                        .ObservesProperty(() => ScriptVideoContext.FrameNumber);

            VideoFrameStepBackwardCommand = new DelegateCommand(
                executeMethod: () => ScriptVideoContext.FrameNumber--,
                canExecuteMethod: () => ScriptVideoContext.HasVideo && ScriptVideoContext.FrameNumber > 0
            );
            VideoFrameStepBackwardCommand.ObservesProperty(() => ScriptVideoContext.HasVideo)
                                         .ObservesProperty(() => ScriptVideoContext.FrameNumber);

            NewProject();
        }

        /// <inheritdoc cref="IDestructible.Destroy"/>
        public void Destroy()
        {
            if (_projectService != null)
            {
                _projectService.ProjectOpened -= OnProjectOpened;
                _projectService.ProjectClosed -= OnProjectClosed;
            }

            if (TimelineCommands?.AddTrackSegmentCommandParameters != null)
            {
                TimelineCommands.AddTrackSegmentCommandParameters.PropertyChanged -= OnAddTrackSegmentCommandParametersPropertyChanged;
            }
        }

        /// <summary>
        /// Handles the <see cref="IProjectService.ProjectOpened"/> event for the <see cref="IProjectService"/> instance.
        /// </summary>
        /// <inheritdoc cref="EventHandler"/>
        private void OnProjectOpened(object sender, EventArgs e)
        {
            HasProject = true;
        }

        /// <summary>
        /// Handles the <see cref="IProjectService.ProjectClosed"/> event for the <see cref="IProjectService"/> instance.
        /// </summary>
        /// <inheritdoc cref="EventHandler"/>
        private void OnProjectClosed(object sender, EventArgs e)
        {
            HasProject = false;

            ScriptVideoContext.Project = null;

            Title = BASE_TITLE_STRING;
        }


        /// <summary>
        /// Creates a new <see cref="ProjectModel">project</see>.
        /// </summary>
        /// <remarks>
        /// <para>If a project is currently open, it is closed before the new project is created.</para>
        /// Invoked on execution of the <see cref="NewProjectCommand"/>.
        /// </remarks>
        private void NewProject()
        {
            if (_hasProject && !CloseProject())
            {
                return;
            }

            _projectService.CreateNewProject();

            Title = NEW_PROJECT_TITLE_STRING;
        }

        /// <summary>
        /// Opens a <see cref="ProjectModel">project</see> from a file selected
        /// from an <see cref="ISystemDialogService.ShowOpenFileDialog(SystemOpenFileDialogSettings)">Open file dialog</see>.
        /// </summary>
        /// <remarks>Invoked on execution of the <see cref="OpenProjectCommand"/>.</remarks>
        private void OpenProject()
        {
            SystemOpenFileDialogSettings dialogSettings = new SystemOpenFileDialogSettings
            {
                Title = "Open Project",
                DefaultExt = ".vseproj",
                Filter = "Video Script Editor Project (.vseproj)|*.vseproj"
            };
            if ((_hasProject && !CloseProject()) || !_systemDialogService.ShowOpenFileDialog(dialogSettings).GetValueOrDefault(false))
            {
                return;
            }

            ProjectModel project;
            try
            {
                project = _projectService.OpenProject(dialogSettings.FileName);
            }
            catch (Exception ex)
            {
                _systemDialogService.ShowErrorDialog($"Failed to load a project from {dialogSettings.FileName}", exception: ex);
                project = null;
            }

            if (project != null)
            {
                Title = $"{BASE_TITLE_STRING} - {Path.GetFileNameWithoutExtension(dialogSettings.FileName)}";

                ScriptVideoContext.Project = project;
            }
        }

        /// <summary>
        /// Saves the current <see cref="ProjectModel">project</see> to the existing project file or to a file selected
        /// from a <see cref="ISystemDialogService.ShowSaveFileDialog(SystemSaveFileDialogSettings)">Save file dialog</see>
        /// depending on the value of the <paramref name="saveOptions"/> parameter.
        /// </summary>
        /// <remarks>Invoked on execution of the <see cref="SaveProjectCommand"/>.</remarks>
        /// <param name="saveOptions">
        /// A <see cref="SaveCommandOptions"/> value specifying whether to save to the existing project file or a new project file.
        /// </param>
        private void SaveProject(SaveCommandOptions? saveOptions)
        {
            ProjectModel project = _projectService.Project;
            string projectFilePath = project.ProjectFilePath;

            if (saveOptions == SaveCommandOptions.SaveAs || project.IsNew || projectFilePath == null)
            {
                SystemSaveFileDialogSettings dialogSettings = new SystemSaveFileDialogSettings()
                {
                    Title = "Save Project",
                    DefaultExt = ".vseproj",
                    Filter = "Video Script Editor Project (.vseproj)|*.vseproj",
                    CheckFileExists = false,
                    OverwritePrompt = true
                };
                if (!_systemDialogService.ShowSaveFileDialog(dialogSettings).GetValueOrDefault(false))
                {
                    return;
                }

                projectFilePath = dialogSettings.FileName;
            }

            bool projectSaved;
            try
            {
                _projectService.SaveProject(projectFilePath, Settings.CreateProjectBackupWhenSaving);
                projectSaved = true;
            }
            catch (Exception ex)
            {
                _systemDialogService.ShowErrorDialog($"Failed to save the project to {projectFilePath}", exception: ex);
                projectSaved = false;
            }
            
            if (projectSaved)
            {
                _systemDialogService.ShowMessageBox("Project saved successfully.");

                project.HasChanges = false;
                Title = $"{BASE_TITLE_STRING} - {Path.GetFileNameWithoutExtension(projectFilePath)}";
            }
        }

        /// <summary>
        /// Closes the current <see cref="ProjectModel">project</see>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the current project has unsaved changes,
        /// a <see cref="ISystemDialogService.ShowConfirmationDialog(string, string)">confirmation dialog</see>
        /// is shown before closing the project.
        /// </para>
        /// Invoked on execution of the <see cref="CloseProjectCommand"/>.
        /// </remarks>
        /// <returns>True if the current project was closed, otherwise False.</returns>
        private bool CloseProject()
        {
            if (_projectService.Project?.HasChanges == true
                && !_systemDialogService.ShowConfirmationDialog("The current project may have unsaved changes. Are you sure you wish to close it?"))
            {
                return false;
            }

            _projectService.CloseProject();

            return true;
        }

        /// <summary>
        /// Imports an AviSynth script from a file selected from
        /// an <see cref="ISystemDialogService.ShowOpenFileDialog(SystemOpenFileDialogSettings)">Open file dialog</see>
        /// into the current <see cref="ProjectModel">project</see> and <see cref="IScriptVideoService"/> instance.
        /// </summary>
        /// <remarks>Invoked on execution of the <see cref="ImportScriptCommand"/>.</remarks>
        private void ImportScript()
        {
            SystemOpenFileDialogSettings dialogSettings = new SystemOpenFileDialogSettings
            {
                Title = "Import Script",
                DefaultExt = ".avs",
                Filter = "AviSynth script (.avs, .avsi)|*.avs;*.avsi"
            };

            if (_systemDialogService.ShowOpenFileDialog(dialogSettings) == true)
            {
                ProjectModel project = _projectService.Project;

                project.ScriptFileSource = dialogSettings.FileName;

                if (ScriptVideoContext.Project != project)
                {
                    ScriptVideoContext.Project = project;
                }
                else
                {
                    ScriptVideoContext.ScriptFileSource = dialogSettings.FileName;
                }
            }
        }

        /// <summary>
        /// Invoked whenever the value of the <see cref="SourceVideoZoomLevel"/> property changes.
        /// </summary>
        private void OnSourceVideoZoomLevelPropertyChanged()
        {
            if (SourceVideoZoomLevel == null || SourceVideoZoomLevel.Value == 0)
            {
                // Fit
                SourceVideoPresentationWidth = double.NaN;
                SourceVideoPresentationHeight = double.NaN;
            }
            else
            {
                SizeI videoFrameSize = ScriptVideoContext.VideoFrameSize;

                // Scale
                SourceVideoPresentationWidth = videoFrameSize.Width * SourceVideoZoomLevel.Value;
                SourceVideoPresentationHeight = videoFrameSize.Height * SourceVideoZoomLevel.Value;
            }
        }

        /// <summary>
        /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// for the <see cref="ITimelineCommands.AddTrackSegmentCommandParameters"/> instance.
        /// </summary>
        /// <remarks>
        /// Updates the <see cref="IApplicationSettings.NewSegmentFrameDuration"/> value
        /// whenever the <see cref="AddTrackSegmentCommandParameters.FrameDuration"/> property changes.
        /// </remarks>
        /// <inheritdoc cref="PropertyChangedEventHandler"/>
        private void OnAddTrackSegmentCommandParametersPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AddTrackSegmentCommandParameters.FrameDuration))
            {
                Settings.NewSegmentFrameDuration = TimelineCommands.AddTrackSegmentCommandParameters.FrameDuration;
            }
        }

        /// <summary>
        /// Invoked on execution of the <see cref="ToggleStartPauseVideoPlaybackCommand"/>.
        /// </summary>
        private void ToggleStartPauseVideoPlayback()
        {
            if (ScriptVideoContext.IsVideoPlaying)
            {
                _scriptVideoService.PauseVideoPlayback();
            }
            else
            {
                _scriptVideoService.StartVideoPlayback();
            }
        }

        /// <summary>
        /// Invoked on execution of the <see cref="StartVideoPlaybackCommand"/>.
        /// </summary>
        private void StartVideoPlayback()
        {
            _scriptVideoService.StartVideoPlayback();
        }

        /// <summary>
        /// Invoked on execution of the <see cref="PauseVideoPlaybackCommand"/>.
        /// </summary>
        private void PauseVideoPlayback()
        {
            _scriptVideoService.PauseVideoPlayback();
        }

        /// <summary>
        /// Invoked on execution of the <see cref="StopVideoPlaybackCommand"/>.
        /// </summary>
        private void StopVideoPlayback()
        {
            if (ScriptVideoContext.IsVideoPlaying)
            {
                try
                {
                    _scriptVideoService.StopVideoPlayback();
                }
                catch (Exception ex)
                {
                    _systemDialogService.ShowErrorDialog("An exception occurred while attempting to stop video playback", exception: ex);
                }
            }
            else
            {
                ScriptVideoContext.FrameNumber = 0;
            }
        }

        /// <summary>
        /// Creates a standard set of <see cref="LabelledZoomLevel">zoom levels</see>
        /// for the <see cref="SourceVideoZoomLevels"/> collection.
        /// </summary>
        /// <returns>A collection containing a standard set of <see cref="LabelledZoomLevel">zoom levels</see>.</returns>
        private IEnumerable<LabelledZoomLevel> GetStandardVideoZoomLevels()
        {
            double[] levels = { 0, 10, 25, 50, 75, 100, 200, 300, 400, 800, 1600 };

            yield return new LabelledZoomLevel(levels[0], "Fit");

            for (int i = 1; i < levels.Length; i++)
            {
                yield return new LabelledZoomLevel(levels[i] / 100, $"{levels[i]}%");
            }
        }
    }
}
