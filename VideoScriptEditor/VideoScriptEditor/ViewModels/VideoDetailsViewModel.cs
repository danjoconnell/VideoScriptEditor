using MonitoredUndo;
using Prism;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Extensions;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Primitives;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using Debug = System.Diagnostics.Debug;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.ViewModels
{
    /// <summary>
    /// View Model encapsulating presentation logic for the Video Details view.
    /// </summary>
    public class VideoDetailsViewModel : BindableBase, IActiveAware, ISupportsUndo
    {
        private readonly IScriptVideoService _scriptVideoService;
        private readonly IProjectService _projectService;
        private readonly IDialogService _dialogService;
        private readonly IUndoService _undoService;
        private readonly IChangeFactory _undoChangeFactory;
        private readonly IApplicationCommands _applicationCommands;
        private readonly UndoRoot _undoRoot;
        private bool _isActive;
        private bool _areEventsAndCommandsSubscribed = false;
        private VideoProcessingOptionsViewModel _videoProcessingOptions;

        private ProjectModel Project => _projectService?.Project;

        /// <inheritdoc cref="IActiveAware.IsActiveChanged"/>
        public event EventHandler IsActiveChanged;

        /// <inheritdoc cref="IActiveAware.IsActive"/>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value, OnIsActiveChanged);
        }

        /// <summary>
        /// Gets the runtime context of the <see cref="IScriptVideoService"/> instance.
        /// </summary>
        public IScriptVideoContext ScriptVideoContext { get; }

        /// <inheritdoc cref="VideoProcessingOptionsViewModel"/>
        public VideoProcessingOptionsViewModel VideoProcessingOptions
        {
            get => _videoProcessingOptions;
            private set => SetProperty(ref _videoProcessingOptions, value);
        }

        /// <inheritdoc cref="UndoRoot.UndoStack"/>
        public IEnumerable<ChangeSet> UndoStack => _undoRoot.UndoStack;

        /// <inheritdoc cref="UndoRoot.RedoStack"/>
        public IEnumerable<ChangeSet> RedoStack => _undoRoot.RedoStack;

        /// <inheritdoc cref="IApplicationCommands.UndoCommand"/>
        public DelegateCommand<ChangeSet> UndoCommand { get; }

        /// <inheritdoc cref="IApplicationCommands.RedoCommand"/>
        public DelegateCommand<ChangeSet> RedoCommand { get; }

        /// <summary>
        /// Command for showing the Output Video Properties dialog
        /// for making changes to <see cref="VideoProcessingOptionsModel">video processing options</see>.
        /// </summary>
        public DelegateCommand ChangeOutputVideoPropertiesCommand { get; set; }

        /// <summary>
        /// Creates a new <see cref="VideoDetailsViewModel"/> instance.
        /// </summary>
        /// <param name="scriptVideoService">The <see cref="IScriptVideoService"/> instance for processing and previewing edited video.</param>
        /// <param name="projectService">
        /// The <see cref="IProjectService"/> instance providing access to a <see cref="ProjectModel">project</see>
        /// and performing related I/O operations.
        /// </param>
        /// <param name="dialogService">The Prism <see cref="IDialogService"/> instance for showing modal and non-modal dialogs.</param>
        /// <param name="undoService">The <see cref="IUndoService"/> instance providing undo/redo support.</param>
        /// <param name="undoChangeFactory">The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</param>
        /// <param name="applicationCommands">The <see cref="IApplicationCommands"/> instance providing a common set of application related commands.</param>
        public VideoDetailsViewModel(IScriptVideoService scriptVideoService, IProjectService projectService, IDialogService dialogService, IUndoService undoService, IChangeFactory undoChangeFactory, IApplicationCommands applicationCommands)
        {
            _scriptVideoService = scriptVideoService;
            ScriptVideoContext = scriptVideoService.GetContextReference();
            _projectService = projectService;
            _dialogService = dialogService;
            _undoService = undoService;
            _undoChangeFactory = undoChangeFactory;
            _applicationCommands = applicationCommands;
            _undoRoot = undoService[this];

            UndoCommand = new DelegateCommand<ChangeSet>(
                executeMethod: ExecuteUndoCommand,
                canExecuteMethod: (changeSet) => _undoRoot.CanUndo
            ).ObservesProperty(() => UndoStack);

            RedoCommand = new DelegateCommand<ChangeSet>(
                executeMethod: ExecuteRedoCommand,
                canExecuteMethod: (changeSet) => _undoRoot.CanRedo
            ).ObservesProperty(() => RedoStack);

            ChangeOutputVideoPropertiesCommand = new DelegateCommand(
                executeMethod: ExecuteChangeOutputVideoPropertiesCommand,
                canExecuteMethod: () => ScriptVideoContext.HasVideo && Project != null
            );
            ChangeOutputVideoPropertiesCommand.ObservesProperty(() => ScriptVideoContext.HasVideo)
                                              .ObservesProperty(() => Project);

            if (Project != null)
            {
                _videoProcessingOptions = new VideoProcessingOptionsViewModel(Project.VideoProcessingOptions);
            }
        }

        /// <inheritdoc cref="ISupportsUndo.GetUndoRoot"/>
        public object GetUndoRoot() => this;

        /// <summary>
        /// Invoked whenever the value of the <see cref="IsActive"/> property changes.
        /// </summary>
        /// <remarks>Raises the <see cref="IsActiveChanged"/> event.</remarks>
        private void OnIsActiveChanged()
        {
            if (_isActive)
            {
                //
                // Setup
                //

                if (!_areEventsAndCommandsSubscribed)
                {
                    SubscribeEventsAndCommands();
                    _areEventsAndCommandsSubscribed = true;
                }

                if (ScriptVideoContext.HasVideo)
                {
                    RaisePropertyChanged(nameof(VideoProcessingOptions));
                }

                RaisePropertyChanged(nameof(UndoStack));
                RaisePropertyChanged(nameof(RedoStack));
            }
            else
            {
                //
                // Cleanup
                //

                if (_areEventsAndCommandsSubscribed)
                {
                    UnsubscribeEventsAndCommands();
                    _areEventsAndCommandsSubscribed = false;
                }
            }

            IsActiveChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Subscribes to events and registers commands.
        /// </summary>
        private void SubscribeEventsAndCommands()
        {
            _projectService.ProjectOpened -= OnProjectOpened;
            _projectService.ProjectOpened += OnProjectOpened;

            _projectService.ProjectClosing -= OnProjectClosing;
            _projectService.ProjectClosing += OnProjectClosing;

            _undoRoot.UndoStackChanged -= OnUndoStackChanged;
            _undoRoot.UndoStackChanged += OnUndoStackChanged;

            _undoRoot.RedoStackChanged -= OnRedoStackChanged;
            _undoRoot.RedoStackChanged += OnRedoStackChanged;

            _applicationCommands.UndoCommand.RegisterCommand(UndoCommand);
            _applicationCommands.RedoCommand.RegisterCommand(RedoCommand);
        }

        /// <summary>
        /// Unsubscribes from events and unregisters commands.
        /// </summary>
        private void UnsubscribeEventsAndCommands()
        {
            _projectService.ProjectOpened -= OnProjectOpened;
            _projectService.ProjectClosing -= OnProjectClosing;

            _undoRoot.UndoStackChanged -= OnUndoStackChanged;
            _undoRoot.RedoStackChanged -= OnRedoStackChanged;

            _applicationCommands.UndoCommand.UnregisterCommand(UndoCommand);
            _applicationCommands.RedoCommand.UnregisterCommand(RedoCommand);
        }

        /// <summary>
        /// Handles the <see cref="IProjectService.ProjectOpened"/> event for the <see cref="_projectService"/> instance.
        /// </summary>
        /// <inheritdoc cref="EventHandler"/>
        private void OnProjectOpened(object sender, EventArgs e)
        {
            VideoProcessingOptions = new VideoProcessingOptionsViewModel(Project.VideoProcessingOptions);
        }

        /// <summary>
        /// Handles the <see cref="IProjectService.ProjectClosing"/> event for the <see cref="_projectService"/> instance.
        /// </summary>
        /// <inheritdoc cref="EventHandler"/>
        private void OnProjectClosing(object sender, EventArgs e)
        {
            _undoRoot.Clear();
            VideoProcessingOptions = null;
        }

        /// <summary>
        /// Undoes all <see cref="ChangeSet"/>s up to and including <paramref name="changeSet"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="changeSet"/> is null, the first available <see cref="ChangeSet"/>
        /// in the <see cref="UndoStack"/> is undone.
        /// </remarks>
        /// <param name="changeSet">
        /// The last <see cref="ChangeSet"/> to undo.
        /// If null, the first available <see cref="ChangeSet"/> in the <see cref="UndoStack"/>.
        /// </param>
        private void ExecuteUndoCommand(ChangeSet changeSet)
        {
            if (changeSet != null)
            {
                _undoRoot.Undo(changeSet);
            }
            else
            {
                _undoRoot.Undo();
            }
        }

        /// <summary>
        /// Redoes <see cref="ChangeSet"/>s up to and including <paramref name="changeSet"/>.
        /// </summary>
        /// <remarks>
        /// If <paramref name="changeSet"/> is null, the first available <see cref="ChangeSet"/>
        /// in the <see cref="RedoStack"/> is redone.
        /// </remarks>
        /// <param name="changeSet">
        /// The last <see cref="ChangeSet"/> to redo.
        /// If null, the first available <see cref="ChangeSet"/> in the <see cref="RedoStack"/>.
        /// </param>
        private void ExecuteRedoCommand(ChangeSet changeSet)
        {
            if (changeSet != null)
            {
                _undoRoot.Redo(changeSet);
            }
            else
            {
                _undoRoot.Redo();
            }
        }

        /// <summary>
        /// Handles the <see cref="UndoRoot.UndoStackChanged"/> event for the <see cref="_undoRoot"/>.
        /// </summary>
        /// <remarks>
        /// Raises the <see cref="DelegateCommandBase.CanExecuteChanged"/> event on the <see cref="UndoCommand"/>.
        /// </remarks>
        /// <inheritdoc cref="EventHandler"/>
        private void OnUndoStackChanged(object sender, EventArgs e)
        {
            UndoCommand.RaiseCanExecuteChanged();

            if (_undoRoot.CanUndo)
            {
                Project.HasChanges = true;
            }
        }

        /// <summary>
        /// Handles the <see cref="UndoRoot.RedoStackChanged"/> event for the <see cref="_undoRoot"/>.
        /// </summary>
        /// <remarks>
        /// Raises the <see cref="DelegateCommandBase.CanExecuteChanged"/> event on the <see cref="RedoCommand"/>.
        /// </remarks>
        /// <inheritdoc cref="EventHandler"/>
        private void OnRedoStackChanged(object sender, EventArgs e)
        {
            RedoCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Shows the Output Video Properties dialog for making changes
        /// to <see cref="VideoProcessingOptionsModel">video processing options</see>.
        /// </summary>
        private void ExecuteChangeOutputVideoPropertiesCommand()
        {
            SizeI sourceVideoFrameSize = ScriptVideoContext.VideoFrameSize;

            _dialogService.ShowDialog(
                nameof(Views.Dialogs.OutputVideoPropertiesDialog),
                new DialogParameters
                {
                    { nameof(IScriptVideoContext.VideoFrameSize), sourceVideoFrameSize },
                    { nameof(IScriptVideoContext.AspectRatio), ScriptVideoContext.AspectRatio },
                    { nameof(VideoProcessingOptionsModel.OutputVideoResizeMode), VideoProcessingOptions?.OutputVideoResizeMode ?? VideoResizeMode.None },
                    { nameof(VideoProcessingOptionsModel.OutputVideoSize), VideoProcessingOptions?.OutputVideoSize },
                    { nameof(VideoProcessingOptionsModel.OutputVideoAspectRatio), VideoProcessingOptions?.OutputVideoAspectRatio }
                },
                dialogResult =>
                {
                    if (dialogResult.Result != ButtonResult.OK)
                    {
                        // Dialog canceled.
                        return;
                    }

                    VideoResizeMode newOutputVideoResizeMode = dialogResult.Parameters.GetValue<VideoResizeMode>(nameof(VideoProcessingOptionsModel.OutputVideoResizeMode));
                    SizeI? newOutputVideoSize = null;
                    Ratio? newOutputVideoAspectRatio = null;

                    switch (newOutputVideoResizeMode)
                    {
                        case VideoResizeMode.None:
                            newOutputVideoSize = sourceVideoFrameSize;
                            break;
                        case VideoResizeMode.LetterboxToSize:
                            Debug.Assert(dialogResult.Parameters.ContainsKey(nameof(VideoProcessingOptionsModel.OutputVideoSize)));

                            newOutputVideoSize = dialogResult.Parameters.GetValue<SizeI>(nameof(VideoProcessingOptionsModel.OutputVideoSize));
                            break;
                        case VideoResizeMode.LetterboxToAspectRatio:
                            Debug.Assert(dialogResult.Parameters.ContainsKey(nameof(VideoProcessingOptionsModel.OutputVideoAspectRatio)));

                            newOutputVideoAspectRatio = dialogResult.Parameters.GetValue<Ratio>(nameof(VideoProcessingOptionsModel.OutputVideoAspectRatio));
                            newOutputVideoSize = sourceVideoFrameSize.ExpandToAspectRatio(newOutputVideoAspectRatio.Value);
                            break;
                    }

                    VideoResizeMode currentOutputVideoResizeMode = VideoProcessingOptions.OutputVideoResizeMode;
                    SizeI? currentOutputVideoSize = VideoProcessingOptions.OutputVideoSize;
                    Ratio? currentOutputVideoAspectRatio = VideoProcessingOptions.OutputVideoAspectRatio;

                    if (currentOutputVideoResizeMode != newOutputVideoResizeMode || currentOutputVideoSize != newOutputVideoSize || currentOutputVideoAspectRatio != newOutputVideoAspectRatio)
                    {
                        _undoRoot.AddChange(
                            new DelegateChange(this,
                                () => SetVideoProcessingValues(currentOutputVideoResizeMode, currentOutputVideoSize, currentOutputVideoAspectRatio),
                                () => SetVideoProcessingValues(newOutputVideoResizeMode, newOutputVideoSize, newOutputVideoAspectRatio),
                                (newOutputVideoResizeMode, nameof(newOutputVideoResizeMode), newOutputVideoSize, nameof(newOutputVideoSize), newOutputVideoAspectRatio, nameof(newOutputVideoAspectRatio))
                            ),
                            "Output video properties changed"
                        );
                    }

                    SetVideoProcessingValues(newOutputVideoResizeMode, newOutputVideoSize, newOutputVideoAspectRatio);
                }
            );
        }

        /// <summary>
        /// Sets <see cref="VideoProcessingOptions"/> property values
        /// and updates the <see cref="IScriptVideoContext.OutputPreviewSize"/>.
        /// </summary>
        /// <param name="outputVideoResizeMode">The new value for the <see cref="VideoProcessingOptionsViewModel.OutputVideoResizeMode"/> property.</param>
        /// <param name="outputVideoSize">The new value for the <see cref="VideoProcessingOptionsViewModel.OutputVideoSize"/> property.</param>
        /// <param name="outputVideoAspectRatio">The new value for the <see cref="VideoProcessingOptionsViewModel.OutputVideoAspectRatio"/> property.</param>
        private void SetVideoProcessingValues(VideoResizeMode outputVideoResizeMode, SizeI? outputVideoSize, Ratio? outputVideoAspectRatio)
        {
            VideoProcessingOptions.OutputVideoResizeMode = outputVideoResizeMode;
            VideoProcessingOptions.OutputVideoSize = outputVideoSize;
            VideoProcessingOptions.OutputVideoAspectRatio = outputVideoAspectRatio;

            if (!outputVideoSize.HasValue)
            {
                outputVideoSize = ScriptVideoContext.VideoFrameSize;
            }

            ScriptVideoContext.OutputPreviewSize = new VideoSizeOptions()
            {
                ResizeMode = outputVideoResizeMode,
                AspectRatio = outputVideoAspectRatio,
                PixelWidth = outputVideoSize.Value.Width,
                PixelHeight = outputVideoSize.Value.Height
            };
        }
    }
}
