using MonitoredUndo;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Models;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.ViewModels.Timeline;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels
{
    /// <summary>
    /// Base class for view models encapsulating presentation logic for views in the <see cref="RegionNames.VideoOverlayRegion"/>.
    /// </summary>
    /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase"/>
    public abstract class VideoOverlayViewModelBase : TimelineSegmentProvidingViewModelBase, INavigationAware
    {
        /// <summary>
        /// The <see cref="IProjectService"/> instance providing access to a <see cref="ProjectModel">project</see>
        /// and performing related I/O operations.
        /// </summary>
        protected readonly IProjectService _projectService;

        /// <summary>Indicates whether events and commands common to derived view models are subscribed.</summary>
        protected bool _areCommonEventsAndCommandsSubscribed = false;

        /// <inheritdoc cref="IProjectService.Project"/>
        protected ProjectModel Project => _projectService.Project;

        /// <summary>
        /// Gets a read-only collection of <see cref="Enum"/> values
        /// describing the type of segments that can be added to the timeline.
        /// </summary>
        public abstract ReadOnlyObservableCollection<Enum> AddableSegmentTypes { get; }

        /// <summary>
        /// Base constructor for view models derived from the <see cref="VideoOverlayViewModelBase"/> class.
        /// </summary>
        /// <inheritdoc cref="TimelineSegmentProvidingViewModelBase(IScriptVideoService, IUndoService, IChangeFactory, IApplicationCommands)"/>
        /// <param name="projectService">
        /// The <see cref="IProjectService"/> instance providing access to a <see cref="ProjectModel">project</see>
        /// and performing related I/O operations.
        /// </param>
        protected VideoOverlayViewModelBase(IScriptVideoService scriptVideoService, IUndoService undoService, IChangeFactory undoChangeFactory, IApplicationCommands applicationCommands, IProjectService projectService) : base(scriptVideoService, undoService, undoChangeFactory, applicationCommands)
        {
            _projectService = projectService;
            _projectService.ProjectClosing += OnProjectClosing;
        }

        /// <inheritdoc cref="INavigationAware.OnNavigatedTo(NavigationContext)"/>
        public virtual void OnNavigatedTo(NavigationContext navigationContext)
        {
            RefreshSegmentViewModels();

            if (!_areCommonEventsAndCommandsSubscribed)
            {
                SubscribeCommonEventsAndCommands();
                _areCommonEventsAndCommandsSubscribed = true;
            }

            RefreshActiveSegmentsForFrame(ScriptVideoContext.FrameNumber);

            RaisePropertyChanged(nameof(SegmentViewModels));
            RaisePropertyChanged(nameof(UndoStack));
            RaisePropertyChanged(nameof(RedoStack));
        }

        /// <inheritdoc cref="INavigationAware.OnNavigatedFrom(NavigationContext)"/>
        public virtual void OnNavigatedFrom(NavigationContext navigationContext)
        {
            if (_areCommonEventsAndCommandsSubscribed)
            {
                UnsubscribeCommonEventsAndCommands();
                _areCommonEventsAndCommandsSubscribed = false;
            }
        }

        /// <inheritdoc cref="INavigationAware.IsNavigationTarget(NavigationContext)"/>
        public abstract bool IsNavigationTarget(NavigationContext navigationContext);

        /// <remarks>
        /// These include <see cref="IScriptVideoService.FrameChanged"/>,
        /// <see cref="UndoRoot.UndoStackChanged"/>,
        /// <see cref="UndoRoot.RedoStackChanged"/>,
        /// <see cref="IProjectService.ProjectOpened"/>,
        /// <see cref="IApplicationCommands.UndoCommand"/>
        /// and <see cref="IApplicationCommands.RedoCommand"/>.
        /// </remarks>
        /// <inheritdoc/>
        protected override void SubscribeCommonEventsAndCommands()
        {
            base.SubscribeCommonEventsAndCommands();

            _projectService.ProjectOpened -= OnProjectOpened;
            _projectService.ProjectOpened += OnProjectOpened;
        }

        /// <remarks>
        /// These include <see cref="IScriptVideoService.FrameChanged"/>,
        /// <see cref="UndoRoot.UndoStackChanged"/>,
        /// <see cref="UndoRoot.RedoStackChanged"/>,
        /// <see cref="INotifyCollectionChanged.CollectionChanged"/> for <see cref="TimelineSegmentProvidingViewModelBase.SegmentModels"/> and <see cref="TimelineSegmentProvidingViewModelBase.SegmentViewModels"/>,
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> for <see cref="SelectedSegment"/>,
        /// <see cref="IProjectService.ProjectOpened"/>,
        /// <see cref="IApplicationCommands.UndoCommand"/>
        /// and <see cref="IApplicationCommands.RedoCommand"/>.
        /// </remarks>
        /// <inheritdoc/>
        protected override void UnsubscribeCommonEventsAndCommands()
        {
            _projectService.ProjectOpened -= OnProjectOpened;

            base.UnsubscribeCommonEventsAndCommands();
        }

        /// <inheritdoc/>
        protected override void OnUndoStackChanged(object sender, EventArgs e)
        {
            base.OnUndoStackChanged(sender, e);

            if (_undoRoot.CanUndo)
            {
                Project.HasChanges = true;
            }
        }

        /// <summary>
        /// Refreshes the <see cref="TimelineSegmentProvidingViewModelBase.SegmentViewModels"/> collection
        /// so that each model in the <see cref="TimelineSegmentProvidingViewModelBase.SegmentModels"/> collection
        /// has a corresponding view model for interacting with the timeline and video overlay views.
        /// </summary>
        protected virtual void RefreshSegmentViewModels()
        {
            SegmentViewModels.CollectionChanged -= OnSegmentViewModelsCollectionChanged;

            if (SegmentModels != null)
            {
                SegmentModels.CollectionChanged -= OnSegmentModelsCollectionChanged;

                // Remove excess items
                if (SegmentViewModels.Count > SegmentModels.Count)
                {
                    for (int i = SegmentViewModels.Count - 1; i >= SegmentModels.Count; i--)
                    {
                        SegmentViewModels.RemoveAt(i);
                    }
                    Debug.Assert(SegmentViewModels.Count == SegmentModels.Count);
                }

                for (int i = 0; i < SegmentModels.Count; i++)
                {
                    if (i < SegmentViewModels.Count)
                    {
                        if (!SegmentViewModels[i].Model.Equals(SegmentModels[i]))
                        {
                            SegmentViewModels[i] = SegmentViewModelFactory.CreateSegmentViewModel(SegmentModels[i]);
                        }
                    }
                    else
                    {
                        SegmentViewModels.Add(SegmentViewModelFactory.CreateSegmentViewModel(SegmentModels[i]));
                    }
                }

                SegmentModels.CollectionChanged += OnSegmentModelsCollectionChanged;
            }
            else
            {
                SegmentViewModels.Clear();
            }

            SegmentViewModels.CollectionChanged += OnSegmentViewModelsCollectionChanged;
        }

        /// <summary>
        /// Handles the <see cref="IProjectService.ProjectOpened"/> event for the <see cref="_projectService"/> instance.
        /// </summary>
        /// <inheritdoc cref="EventHandler"/>
        protected virtual void OnProjectOpened(object sender, EventArgs e)
        {
            RefreshSegmentViewModels();

            _scriptVideoService.FrameChanged -= OnScriptVideoFrameNumberChanged;
            _scriptVideoService.FrameChanged += OnScriptVideoFrameNumberChanged;

            RefreshActiveSegmentsForFrame(ScriptVideoContext.FrameNumber);

            RaisePropertyChanged(nameof(ActiveSegments));
            RaisePropertyChanged(nameof(SegmentViewModels));
            RaisePropertyChanged(nameof(UndoStack));
            RaisePropertyChanged(nameof(RedoStack));
        }

        /// <summary>
        /// Handles the <see cref="IProjectService.ProjectClosing"/> event for the <see cref="_projectService"/> instance.
        /// </summary>
        /// <inheritdoc cref="EventHandler"/>
        protected virtual void OnProjectClosing(object sender, EventArgs e)
        {
            _scriptVideoService.FrameChanged -= OnScriptVideoFrameNumberChanged;
            SegmentModels.CollectionChanged -= OnSegmentModelsCollectionChanged;
            SegmentViewModels.CollectionChanged -= OnSegmentViewModelsCollectionChanged;

            if (SelectedSegment != null)
            {
                SelectedSegment.PropertyChanged -= OnSelectedSegmentInstancePropertyChanged;
                SelectedSegment = null;
            }

            _undoRoot.Clear();
            _activeSegmentDictionary.Clear();
            SegmentViewModels.Clear();

            RaisePropertyChanged(nameof(ActiveSegments));
            RaisePropertyChanged(nameof(SegmentViewModels));
            RaisePropertyChanged(nameof(UndoStack));
            RaisePropertyChanged(nameof(RedoStack));
        }
    }
}
