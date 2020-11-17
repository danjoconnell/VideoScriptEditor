using CodeBits;
using MonitoredUndo;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Services.Dialogs;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// View Model encapsulating presentation logic for the Video Timeline view.
    /// </summary>
    /// <remarks>
    /// <para>Implementation of <see cref="IVideoTimelineViewModel"/>.</para>
    /// Implements <see cref="IDisposable"/> to ensure that the <see cref="INotifyCollectionChanged.CollectionChanged"/> handler for <see cref="ITimelineSegmentProvidingViewModel.SegmentViewModels"/>
    /// and the <see cref="IClipboardService.ClipboardUpdated"/> event handler are unsubscribed before garbage collection of a class instance.
    /// </remarks>
    public class VideoTimelineViewModel : BindableBase, IVideoTimelineViewModel, ISupportsUndo, IDestructible
    {
        private readonly IScriptVideoService _scriptVideoService;
        private readonly IUndoService _undoService;
        private readonly IChangeFactory _undoChangeFactory;
        private UndoRoot _undoRoot;
        private readonly IClipboardService _clipboardService;
        private readonly IDialogService _dialogService;
        private readonly ITimelineCommands _timelineCommands;

        private ITimelineSegmentProvidingViewModel _segmentProvidingViewModel = null;
        private double _zoomLevel = 0d;
        private IVideoTimelineTrackViewModel _selectedTrack = null;

        /// <inheritdoc cref="IVideoTimelineViewModel.TimelineSegmentProvidingViewModel"/>
        public ITimelineSegmentProvidingViewModel TimelineSegmentProvidingViewModel
        {
            get => _segmentProvidingViewModel;
            set
            {
                if (_segmentProvidingViewModel != null)
                {
                    _segmentProvidingViewModel.SegmentViewModels.CollectionChanged -= OnTimelineSegmentsCollectionChanged;
                    _segmentProvidingViewModel.PropertyChanged -= OnTimelineSegmentProvidingViewModelInstancePropertyChanged;
                }

                SetProperty(ref _segmentProvidingViewModel, value, OnTimelineSegmentProvidingViewModelChanged);
            }
        }

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.SegmentViewModels"/>
        public SegmentViewModelCollection TimelineSegments => _segmentProvidingViewModel?.SegmentViewModels;

        /// <inheritdoc cref="IVideoTimelineViewModel.TimelineTrackCollection"/>
        public OrderedObservableCollection<IVideoTimelineTrackViewModel> TimelineTrackCollection { get; }

        /// <inheritdoc cref="IVideoTimelineViewModel.ScriptVideoContext"/>
        public IScriptVideoContext ScriptVideoContext { get; }

        /// <inheritdoc cref="IVideoTimelineViewModel.ZoomLevel"/>
        public double ZoomLevel
        {
            get => _zoomLevel;
            set => SetProperty(ref _zoomLevel, Math.Clamp(value, 0d, 100d));
        }

        /// <inheritdoc cref="IVideoTimelineViewModel.SelectedTrack"/>
        public IVideoTimelineTrackViewModel SelectedTrack
        {
            get => _selectedTrack;
            set
            {
                if (_selectedTrack != value)
                {
                    SetProperty(ref _selectedTrack, value, OnSelectedTrackChanged);
                }
            }
        }

        /// <inheritdoc cref="ITimelineCommands.SetTimelineZoomLevelCommand"/>
        public DelegateCommand<double?> SetTimelineZoomLevelCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.AddTrackCommand"/>
        public DelegateCommand AddTrackCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.RemoveTrackCommand"/>
        public DelegateCommand<IVideoTimelineTrackViewModel> RemoveTrackCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.AddTrackSegmentCommand"/>
        public DelegateCommand AddTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.AddTrackSegmentCommandParameters"/>
        public AddTrackSegmentCommandParameters AddTrackSegmentCommandParameters => _timelineCommands.AddTrackSegmentCommandParameters;

        /// <inheritdoc cref="ITimelineCommands.RemoveTrackSegmentCommand"/>
        public DelegateCommand<SegmentViewModelBase> RemoveTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.SplitSelectedTrackSegmentCommand"/>
        public DelegateCommand SplitSelectedTrackSegmentCommand { get; }

        /// <inheritdoc cref="IVideoTimelineViewModel.MergeTrackSegmentLeftCommand"/>
        public DelegateCommand<SegmentViewModelBase> MergeTrackSegmentLeftCommand { get; }

        /// <inheritdoc cref="IVideoTimelineViewModel.MergeTrackSegmentRightCommand"/>
        public DelegateCommand<SegmentViewModelBase> MergeTrackSegmentRightCommand { get; }

        /// <inheritdoc cref="IVideoTimelineViewModel.RenameTrackSegmentCommand"/>
        public DelegateCommand<SegmentViewModelBase> RenameTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.SeekPreviousKeyFrameInTrackCommand"/>
        public DelegateCommand SeekPreviousKeyFrameInTrackCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.SeekNextKeyFrameInTrackCommand"/>
        public DelegateCommand SeekNextKeyFrameInTrackCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.AddTrackSegmentKeyFrameCommand"/>
        public DelegateCommand AddTrackSegmentKeyFrameCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.RemoveTrackSegmentKeyFrameCommand"/>
        public DelegateCommand RemoveTrackSegmentKeyFrameCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.CopyKeyFrameFromPreviousInTrackSegmentCommand"/>
        public DelegateCommand CopyKeyFrameFromPreviousInTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.CopyKeyFrameFromNextInTrackSegmentCommand"/>
        public DelegateCommand CopyKeyFrameFromNextInTrackSegmentCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.CopyTrackSegmentKeyFrameToClipboardCommand"/>
        public DelegateCommand CopyTrackSegmentKeyFrameToClipboardCommand { get; }

        /// <inheritdoc cref="ITimelineCommands.PasteTrackSegmentKeyFrameCommand"/>
        public DelegateCommand PasteTrackSegmentKeyFrameCommand { get; }

        /// <summary>
        /// Creates a new <see cref="VideoTimelineViewModel"/> instance.
        /// </summary>
        /// <param name="scriptVideoService">The <see cref="IScriptVideoService"/> instance for processing and previewing edited video.</param>
        /// <param name="undoService">The <see cref="IUndoService"/> instance providing undo/redo support.</param>
        /// <param name="undoChangeFactory">The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</param>
        /// <param name="clipboardService">The <see cref="IClipboardService"/> instance providing access to the system clipboard.</param>
        /// <param name="dialogService">The Prism <see cref="IDialogService"/> instance for showing modal and non-modal dialogs.</param>
        /// <param name="timelineCommands">The <see cref="ITimelineCommands"/> instance providing a set of timeline related commands.</param>
        public VideoTimelineViewModel(IScriptVideoService scriptVideoService, IUndoService undoService, IChangeFactory undoChangeFactory, IClipboardService clipboardService, IDialogService dialogService, ITimelineCommands timelineCommands)
        {
            _scriptVideoService = scriptVideoService;
            ScriptVideoContext = scriptVideoService.GetContextReference();
            _undoService = undoService;
            _undoChangeFactory = undoChangeFactory;
            _clipboardService = clipboardService;
            _dialogService = dialogService;
            _timelineCommands = timelineCommands;

            TimelineTrackCollection = new OrderedObservableCollection<IVideoTimelineTrackViewModel>();
            TimelineTrackCollection.CollectionChanged += OnTimelineTrackCollectionCollectionChanged;

            SetTimelineZoomLevelCommand = new DelegateCommand<double?>(
                executeMethod: (zoomLevelParam) => ZoomLevel = zoomLevelParam.GetValueOrDefault(0d)
            );

            AddTrackCommand = new DelegateCommand(
                executeMethod: AddTrack,
                canExecuteMethod: () => TimelineSegments != null && ScriptVideoContext.HasVideo
            );
            AddTrackCommand.ObservesProperty(() => TimelineSegments)
                           .ObservesProperty(() => ScriptVideoContext.HasVideo);

            RemoveTrackCommand = new DelegateCommand<IVideoTimelineTrackViewModel>(RemoveTrack, CanRemoveTrack)
                              .ObservesProperty(() => TimelineSegments)
                              .ObservesProperty(() => ScriptVideoContext.HasVideo)
                              .ObservesProperty(() => SelectedTrack);

            AddTrackSegmentCommand = new DelegateCommand(
                executeMethod: () => AddTrackSegment(AddTrackSegmentCommandParameters.SegmentTypeDescriptor,
                                                     SelectedTrack.TrackNumber, ScriptVideoContext.FrameNumber,
                                                     AddTrackSegmentCommandParameters.FrameDuration),
                canExecuteMethod: CanAddTrackSegmentCommandExecute
            );
            AddTrackSegmentCommand.ObservesProperty(() => AddTrackSegmentCommandParameters.SegmentTypeDescriptor)
                                  .ObservesProperty(() => AddTrackSegmentCommandParameters.FrameDuration)
                                  .ObservesProperty(() => TimelineSegments)
                                  .ObservesProperty(() => ScriptVideoContext.FrameNumber)
                                  .ObservesProperty(() => SelectedTrack)
                                  .ObservesProperty(() => ScriptVideoContext.HasVideo)
                                  .ObservesProperty(() => ScriptVideoContext.VideoFrameCount);

            RemoveTrackSegmentCommand = new DelegateCommand<SegmentViewModelBase>(RemoveTrackSegment, CanRemoveTrackSegment)
                                     .ObservesProperty(() => TimelineSegments)
                                     .ObservesProperty(() => ScriptVideoContext.HasVideo)
                                     .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment);

            SplitSelectedTrackSegmentCommand = new DelegateCommand(SplitSelectedTrackSegment, CanSplitSelectedTrackSegment)
                                            .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment)
                                            .ObservesProperty(() => ScriptVideoContext.FrameNumber);

            MergeTrackSegmentLeftCommand = new DelegateCommand<SegmentViewModelBase>(MergeTrackSegmentLeft, CanMergeTrackSegmentLeft)
                                        .ObservesProperty(() => TimelineSegments);

            MergeTrackSegmentRightCommand = new DelegateCommand<SegmentViewModelBase>(MergeTrackSegmentRight, CanMergeTrackSegmentRight)
                                         .ObservesProperty(() => TimelineSegments);

            RenameTrackSegmentCommand = new DelegateCommand<SegmentViewModelBase>(RenameTrackSegment);

            SeekPreviousKeyFrameInTrackCommand = new DelegateCommand(SeekPreviousKeyFrameInTrack, CanSeekPreviousKeyFrameInTrack)
                                              .ObservesProperty(() => TimelineSegmentProvidingViewModel)
                                              .ObservesProperty(() => ScriptVideoContext.HasVideo)
                                              .ObservesProperty(() => ScriptVideoContext.FrameNumber)
                                              .ObservesProperty(() => SelectedTrack)
                                              .ObservesProperty(() => TimelineSegments.Count);

            SeekNextKeyFrameInTrackCommand = new DelegateCommand(SeekNextKeyFrameInTrack, CanSeekNextKeyFrameInTrack)
                                          .ObservesProperty(() => TimelineSegmentProvidingViewModel)
                                          .ObservesProperty(() => ScriptVideoContext.HasVideo)
                                          .ObservesProperty(() => ScriptVideoContext.FrameNumber)
                                          .ObservesProperty(() => SelectedTrack)
                                          .ObservesProperty(() => TimelineSegments.Count);

            AddTrackSegmentKeyFrameCommand = new DelegateCommand(
                executeMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.AddKeyFrame(),
                canExecuteMethod: CanAddKeyFrameToSelectedSegment
            );
            AddTrackSegmentKeyFrameCommand.ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment)
                                          .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.ActiveKeyFrame);

            RemoveTrackSegmentKeyFrameCommand = new DelegateCommand(
                // Passing a null parameter to the command to remove/query removal of the selected segment's ActiveKeyFrame.
                executeMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.RemoveKeyFrameCommand?.Execute(null),
                canExecuteMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.RemoveKeyFrameCommand?.CanExecute(null) == true
            );
            RemoveTrackSegmentKeyFrameCommand.ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment)
                                             .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.ActiveKeyFrame)
                                             .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.ActiveKeyFrame.FrameNumber)
                                             .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.StartFrame);

            CopyKeyFrameFromPreviousInTrackSegmentCommand = new DelegateCommand(
                executeMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.CopyFromPreviousKeyFrame(),
                canExecuteMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.CanCopyFromPreviousKeyFrame() == true
            );
            CopyKeyFrameFromPreviousInTrackSegmentCommand.ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment)
                                                         .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.ActiveKeyFrame)
                                                         .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.KeyFrameViewModels.Count);

            CopyKeyFrameFromNextInTrackSegmentCommand = new DelegateCommand(
                executeMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.CopyFromNextKeyFrame(),
                canExecuteMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.CanCopyFromNextKeyFrame() == true
            );
            CopyKeyFrameFromNextInTrackSegmentCommand.ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment)
                                                     .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.ActiveKeyFrame)
                                                     .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.KeyFrameViewModels.Count);

            CopyTrackSegmentKeyFrameToClipboardCommand = new DelegateCommand(
                // Passing a null parameter to the command to copy/query copying the selected segment's ActiveKeyFrame.
                executeMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.CopyKeyFrameToClipboardCommand?.Execute(null),
                canExecuteMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.CopyKeyFrameToClipboardCommand?.CanExecute(null) == true
            );
            CopyTrackSegmentKeyFrameToClipboardCommand.ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment)
                                                      .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.ActiveKeyFrame);

            PasteTrackSegmentKeyFrameCommand = new DelegateCommand(
                // Passing a null parameter to the command to paste to/query pasting to the selected segment's ActiveKeyFrame.
                executeMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.PasteKeyFrameCommand?.Execute(null),
                canExecuteMethod: () => TimelineSegmentProvidingViewModel?.SelectedSegment?.PasteKeyFrameCommand?.CanExecute(null) == true
            );
            PasteTrackSegmentKeyFrameCommand.ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment)
                                            .ObservesProperty(() => TimelineSegmentProvidingViewModel.SelectedSegment.ActiveKeyFrame);

            _timelineCommands.SetTimelineZoomLevelCommand.RegisterCommand(SetTimelineZoomLevelCommand);
            _timelineCommands.AddTrackCommand.RegisterCommand(AddTrackCommand);
            _timelineCommands.RemoveTrackCommand.RegisterCommand(RemoveTrackCommand);
            _timelineCommands.AddTrackSegmentCommand.RegisterCommand(AddTrackSegmentCommand);
            _timelineCommands.RemoveTrackSegmentCommand.RegisterCommand(RemoveTrackSegmentCommand);
            _timelineCommands.SplitSelectedTrackSegmentCommand.RegisterCommand(SplitSelectedTrackSegmentCommand);
            _timelineCommands.SeekPreviousKeyFrameInTrackCommand.RegisterCommand(SeekPreviousKeyFrameInTrackCommand);
            _timelineCommands.SeekNextKeyFrameInTrackCommand.RegisterCommand(SeekNextKeyFrameInTrackCommand);
            _timelineCommands.AddTrackSegmentKeyFrameCommand.RegisterCommand(AddTrackSegmentKeyFrameCommand);
            _timelineCommands.RemoveTrackSegmentKeyFrameCommand.RegisterCommand(RemoveTrackSegmentKeyFrameCommand);
            _timelineCommands.CopyKeyFrameFromNextInTrackSegmentCommand.RegisterCommand(CopyKeyFrameFromNextInTrackSegmentCommand);
            _timelineCommands.CopyKeyFrameFromPreviousInTrackSegmentCommand.RegisterCommand(CopyKeyFrameFromPreviousInTrackSegmentCommand);
            _timelineCommands.CopyTrackSegmentKeyFrameToClipboardCommand.RegisterCommand(CopyTrackSegmentKeyFrameToClipboardCommand);
            _timelineCommands.PasteTrackSegmentKeyFrameCommand.RegisterCommand(PasteTrackSegmentKeyFrameCommand);

            _clipboardService.ClipboardUpdated += OnClipboardUpdated;
        }

        /// <inheritdoc cref="IDestructible.Destroy"/>
        public void Destroy()
        {
            if (_segmentProvidingViewModel?.SegmentViewModels != null)
            {
                _segmentProvidingViewModel.SegmentViewModels.CollectionChanged -= OnTimelineSegmentsCollectionChanged;
            }

            if (_clipboardService != null)
            {
                _clipboardService.ClipboardUpdated -= OnClipboardUpdated;
            }
        }

        /// <inheritdoc cref="ISupportsUndo.GetUndoRoot"/>
        public object GetUndoRoot() => _segmentProvidingViewModel?.GetUndoRoot();

        /// <summary>
        /// Invoked whenever the value of the <see cref="TimelineSegmentProvidingViewModel"/> property changes.
        /// </summary>
        private void OnTimelineSegmentProvidingViewModelChanged()
        {
            // Temporarily disable undo monitoring for TimelineTrackColllection
            TimelineTrackCollection.CollectionChanged -= OnTimelineTrackCollectionCollectionChanged;

            _undoRoot = null;
            _selectedTrack = null;
            TimelineTrackCollection.Clear();

            if (_segmentProvidingViewModel != null)
            {
                Debug.Assert(_segmentProvidingViewModel.SegmentViewModels != null && _segmentProvidingViewModel.GetUndoRoot() != null);

                _undoRoot = _undoService[_segmentProvidingViewModel.GetUndoRoot()];

                // Must have at least one track in order to add new segments
                int newTrackCollectionCount = TimelineSegments.Count > 0 ? TimelineSegments.Max(segment => segment.TrackNumber) + 1 : 1;

                for (int i = 0; i < newTrackCollectionCount; i++)
                {
                    TimelineTrackCollection.Add(
                        new VideoTimelineTrackViewModel(i, GetUndoRoot(), _undoChangeFactory, ScriptVideoContext)
                    );
                }

                foreach (SegmentViewModelBase segmentViewModel in TimelineSegments)
                {
                    TimelineTrackCollection[segmentViewModel.TrackNumber].TrackSegments.Add(segmentViewModel);
                }

                SelectedTrack = _segmentProvidingViewModel.SelectedSegment != null
                                ? TimelineTrackCollection[TimelineSegmentProvidingViewModel.SelectedSegment.TrackNumber]
                                : TimelineTrackCollection.FirstOrDefault();


                _segmentProvidingViewModel.SegmentViewModels.CollectionChanged -= OnTimelineSegmentsCollectionChanged;
                _segmentProvidingViewModel.SegmentViewModels.CollectionChanged += OnTimelineSegmentsCollectionChanged;

                _segmentProvidingViewModel.PropertyChanged -= OnTimelineSegmentProvidingViewModelInstancePropertyChanged;
                _segmentProvidingViewModel.PropertyChanged += OnTimelineSegmentProvidingViewModelInstancePropertyChanged;
            }

            RaisePropertyChanged(nameof(TimelineSegments));

            // Re-enable undo monitoring
            TimelineTrackCollection.CollectionChanged -= OnTimelineTrackCollectionCollectionChanged;
            TimelineTrackCollection.CollectionChanged += OnTimelineTrackCollectionCollectionChanged;

            RaisePropertyChanged(nameof(TimelineTrackCollection));
        }

        /// <summary>
        /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// for the current <see cref="TimelineSegmentProvidingViewModel"/> instance.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedEventHandler"/>
        private void OnTimelineSegmentProvidingViewModelInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ITimelineSegmentProvidingViewModel.SegmentViewModels))
            {
                RaisePropertyChanged(nameof(TimelineSegments));
            }
            else if (e.PropertyName == nameof(ITimelineSegmentProvidingViewModel.ActiveTrackNumber))
            {
                SelectedTrack = TimelineTrackCollection[TimelineSegmentProvidingViewModel.ActiveTrackNumber];
            }
        }

        /// <summary>
        /// Handles the <see cref="IClipboardService.ClipboardUpdated"/> event.
        /// </summary>
        /// <remarks>
        /// Raises the <see cref="DelegateCommandBase.CanExecuteChanged"/> event on the <see cref="PasteTrackSegmentKeyFrameCommand"/>.
        /// </remarks>
        /// <inheritdoc cref="EventHandler"/>
        private void OnClipboardUpdated(object sender, EventArgs e)
        {
            PasteTrackSegmentKeyFrameCommand.RaiseCanExecuteChanged();
        }

        #region Track related methods

        /// <summary>
        /// Invoked whenever the value of the <see cref="SelectedTrack"/> property changes.
        /// </summary>
        private void OnSelectedTrackChanged()
        {
            if (SelectedTrack != null)
            {
                TimelineSegmentProvidingViewModel.ActiveTrackNumber = SelectedTrack.TrackNumber;
            }
        }

        /// <summary>
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event for the <see cref="TimelineTrackCollection"/>.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        private void OnTimelineTrackCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (GetUndoRoot() != null)
            {
                StringBuilder changeDescription = new StringBuilder("Timeline track ");
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        changeDescription.AppendFormat("{0} added", e.NewStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        changeDescription.AppendFormat("{0} removed", e.OldStartingIndex);
                        break;
                    case NotifyCollectionChangedAction.Move:
                        changeDescription.AppendFormat("{0} moved", e.OldStartingIndex);
                        break;
                    default:
                        changeDescription.Append("collection changed");
                        break;
                }

                // log the collection changes with the undo framework
                _undoChangeFactory.OnCollectionChanged(this, nameof(TimelineTrackCollection), TimelineTrackCollection, e, changeDescription.ToString());
            }

            RaisePropertyChanged(nameof(TimelineTrackCollection));
        }

        /// <summary>
        /// Adds a new track to the timeline.
        /// </summary>
        /// <remarks>Execute delegate method for the <see cref="AddTrackCommand"/>.</remarks>
        private void AddTrack()
        {
            var newTrack = new VideoTimelineTrackViewModel(TimelineTrackCollection.Count, GetUndoRoot(), _undoChangeFactory, ScriptVideoContext);
            TimelineTrackCollection.Add(newTrack);

            SelectedTrack = newTrack;
        }

        /// <summary>
        /// Determines whether the specified track can be removed from the timeline.
        /// </summary>
        /// <remarks>
        /// <para>CanExecute delegate method for the <see cref="RemoveTrackCommand"/>.</para>
        /// If <paramref name="track"/> is null, the <see cref="SelectedTrack"/> is checked for suitability.
        /// </remarks>
        /// <param name="track">The track to check. If null, the <see cref="SelectedTrack"/>.</param>
        /// <returns>True if the track can be removed, otherwise False.</returns>
        private bool CanRemoveTrack(IVideoTimelineTrackViewModel track)
        {
            return TimelineSegments != null && ScriptVideoContext.HasVideo && (track != null || SelectedTrack != null);
        }

        /// <summary>
        /// Removes the specified track and all segments on it from the timeline.
        /// </summary>
        /// <remarks>
        /// <para>Execute delegate method for the <see cref="RemoveTrackCommand"/>.</para>
        /// If <paramref name="track"/> is null, the <see cref="SelectedTrack"/> is removed.
        /// </remarks>
        /// <param name="track">The track to remove. If null, the <see cref="SelectedTrack"/>.</param>
        private void RemoveTrack(IVideoTimelineTrackViewModel track)
        {
            if (track == null)
            {
                track = SelectedTrack ?? throw new NullReferenceException(nameof(SelectedTrack));
            }

            _undoRoot.BeginChangeSetBatch($"Timeline track {track.TrackNumber} removed", false);

            if (track.TrackSegments.Count > 0)
            {
                for (int i = track.TrackSegments.Count - 1; i >= 0; i--)
                {
                    RemoveTrackSegment(track.TrackSegments[i]);
                }
            }
            TimelineTrackCollection.Remove(track);

            if (TimelineTrackCollection.Count > 0)
            {
                if (track.TrackNumber != TimelineTrackCollection.Count)
                {
                    // Track wasn't the last track in the collection
                    for (int i = track.TrackNumber; i < TimelineTrackCollection.Count; i++)
                    {
                        IVideoTimelineTrackViewModel timelineTrack = TimelineTrackCollection[i];
                        timelineTrack.TrackNumber--;

                        if (timelineTrack.TrackSegments.Count > 0)
                        {
                            // Reorder TrackNumber on segment view models
                            for (int j = 0; j < timelineTrack.TrackSegments.Count; j++)
                            {
                                SegmentViewModelBase trackSegment = timelineTrack.TrackSegments[j];
                                trackSegment.TrackNumber = timelineTrack.TrackNumber;
                            }
                        }
                    }
                }

                if (track == SelectedTrack || SelectedTrack == null)
                {
                    SelectedTrack = TimelineTrackCollection.FirstOrDefault();
                }
            }
            else
            {
                // Must have at least one track in order to add new segments
                AddTrack();
            }

            _undoRoot.EndChangeSetBatch();
        }

        #endregion Track related methods

        #region Segment related methods

        /// <summary>
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event for the <see cref="TimelineSegments"/> collection.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        private void OnTimelineSegmentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Debug.Assert(e.NewItems.Count == 1);

                SegmentViewModelBase addedSegmentViewModel = e.NewItems[0] as SegmentViewModelBase;
                Debug.Assert(addedSegmentViewModel != null);

                // Add new track if required.
                int requiredTrackCount = addedSegmentViewModel.TrackNumber + 1;
                if (requiredTrackCount > TimelineTrackCollection.Count)
                {
                    if (!_undoRoot.IsInBatch)
                    {
                        // Temporarily disable undo monitoring for TimelineTrackColllection
                        TimelineTrackCollection.CollectionChanged -= OnTimelineTrackCollectionCollectionChanged;
                    }

                    for (int i = TimelineTrackCollection.Count; i < requiredTrackCount; i++)
                    {
                        TimelineTrackCollection.Add(
                            new VideoTimelineTrackViewModel(i, GetUndoRoot(), _undoChangeFactory, ScriptVideoContext)
                        );
                    }

                    if (!_undoRoot.IsInBatch)
                    {
                        // Re-enable undo monitoring
                        TimelineTrackCollection.CollectionChanged -= OnTimelineTrackCollectionCollectionChanged;
                        TimelineTrackCollection.CollectionChanged += OnTimelineTrackCollectionCollectionChanged;

                        RaisePropertyChanged(nameof(TimelineTrackCollection));
                    }
                }

                // Synchronize the track's TrackSegments collection with the TimelineSegments collection changes
                TimelineTrackCollection[addedSegmentViewModel.TrackNumber].TrackSegments.Add(addedSegmentViewModel);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                Debug.Assert(e.OldItems.Count == 1);

                SegmentViewModelBase removedSegmentViewModel = e.OldItems[0] as SegmentViewModelBase;
                Debug.Assert(removedSegmentViewModel != null);

                // Synchronize the track's TrackSegments collection with the TimelineSegments collection changes
                if (removedSegmentViewModel.TrackNumber < TimelineTrackCollection.Count)
                {
                    TimelineTrackCollection[removedSegmentViewModel.TrackNumber].TrackSegments.Remove(removedSegmentViewModel);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (IVideoTimelineTrackViewModel trackViewModel in TimelineTrackCollection)
                {
                    trackViewModel.TrackSegments.Clear();
                }
            }
        }

        /// <inheritdoc/>
        public bool CanAddTrackSegment(int targetTrackNumber, int targetStartFrameNumber, int segmentDuration)
        {
            if (_segmentProvidingViewModel == null)
            {
                return false;
            }

            int targetEndFrameNumber = targetStartFrameNumber + segmentDuration - 1;
            if (targetStartFrameNumber < 0 || targetEndFrameNumber <= targetStartFrameNumber || targetEndFrameNumber >= ScriptVideoContext.VideoFrameCount)
            {
                return false;
            }

            if (targetTrackNumber < 0 || targetTrackNumber >= TimelineTrackCollection.Count)
            {
                return false;
            }

            // https://stackoverflow.com/questions/3269434/whats-the-most-efficient-way-to-test-two-integer-ranges-for-overlap/3269471#3269471
            return !TimelineTrackCollection[targetTrackNumber].TrackSegments.Any(segmentVM => segmentVM.StartFrame <= targetEndFrameNumber && targetStartFrameNumber <= segmentVM.EndFrame);
        }

        /// <inheritdoc/>
        public void AddTrackSegment(Enum segmentTypeDescriptor, int targetTrackNumber, int targetStartFrameNumber, int segmentFrameDuration)
        {
            if (segmentTypeDescriptor is null)
            {
                throw new ArgumentNullException(nameof(segmentTypeDescriptor));
            }

            if (!CanAddTrackSegment(targetTrackNumber, targetStartFrameNumber, segmentFrameDuration))
            {
                throw new InvalidOperationException($"Can't add a segment starting at frame {targetStartFrameNumber} with a duration of {segmentFrameDuration} frames to timeline track {targetTrackNumber}.");
            }

            var segmentViewModelFactory = _segmentProvidingViewModel.SegmentViewModelFactory;

            var segmentViewModel = segmentViewModelFactory.CreateSegmentModelViewModel(segmentTypeDescriptor,
                                                                                       targetTrackNumber,
                                                                                       targetStartFrameNumber,
                                                                                       targetStartFrameNumber + segmentFrameDuration - 1);

            // Batch undo ChangeSet for Model & ViewModel adds so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
            _undoRoot.BeginChangeSetBatch($"{segmentTypeDescriptor} segment added", false);

            _segmentProvidingViewModel.SegmentModels.Add(segmentViewModel.Model);
            _segmentProvidingViewModel.SegmentViewModels.Add(segmentViewModel);

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        public void CopyTrackSegment(SegmentViewModelBase trackSegmentToCopy, int destinationTrackNumber, int destinationStartFrameNumber)
        {
            if (trackSegmentToCopy is null)
            {
                throw new ArgumentNullException(nameof(trackSegmentToCopy));
            }

            if (!CanAddTrackSegment(destinationTrackNumber, destinationStartFrameNumber, trackSegmentToCopy.FrameDuration))
            {
                throw new InvalidOperationException($"Can't add a segment starting at frame {destinationStartFrameNumber} with a duration of {trackSegmentToCopy.FrameDuration} frames to timeline track {destinationTrackNumber}.");
            }

            var segmentViewModelFactory = _segmentProvidingViewModel.SegmentViewModelFactory;

            var copiedSegmentViewModel = segmentViewModelFactory.CreateSegmentModelViewModel(trackSegmentToCopy,
                                                                                             destinationTrackNumber,
                                                                                             destinationStartFrameNumber,
                                                                                             destinationStartFrameNumber + trackSegmentToCopy.FrameDuration - 1);

            // Batch undo ChangeSet for Model & ViewModel adds so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
            _undoRoot.BeginChangeSetBatch($"'{trackSegmentToCopy.Name}' segment copied", false);

            _segmentProvidingViewModel.SegmentModels.Add(copiedSegmentViewModel.Model);
            _segmentProvidingViewModel.SegmentViewModels.Add(copiedSegmentViewModel);

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        public bool CanMoveTrackSegment(SegmentViewModelBase trackSegmentToMove, int destinationTrackNumber, int destinationStartFrameNumber)
        {
            if (trackSegmentToMove is null)
            {
                return false;
            }

            if (trackSegmentToMove.TrackNumber == destinationTrackNumber && trackSegmentToMove.StartFrame == destinationStartFrameNumber)
            {
                return false;
            }

            int destinationEndFrameNumber = destinationStartFrameNumber + trackSegmentToMove.FrameDuration - 1;
            if (destinationStartFrameNumber < 0 || destinationEndFrameNumber >= ScriptVideoContext.VideoFrameCount)
            {
                return false;
            }

            if (destinationTrackNumber < 0 || destinationTrackNumber >= TimelineTrackCollection.Count)
            {
                return false;
            }

            if (trackSegmentToMove.TrackNumber == destinationTrackNumber)
            {
                return !TimelineTrackCollection[destinationTrackNumber].TrackSegments.Any(segmentVM => segmentVM != trackSegmentToMove && segmentVM.StartFrame <= destinationEndFrameNumber && destinationStartFrameNumber <= segmentVM.EndFrame);
            }

            // https://stackoverflow.com/questions/3269434/whats-the-most-efficient-way-to-test-two-integer-ranges-for-overlap/3269471#3269471
            return !TimelineTrackCollection[destinationTrackNumber].TrackSegments.Any(segmentVM => segmentVM.StartFrame <= destinationEndFrameNumber && destinationStartFrameNumber <= segmentVM.EndFrame);
        }

        /// <inheritdoc/>
        public void MoveTrackSegment(SegmentViewModelBase trackSegmentToMove, int destinationTrackNumber, int destinationStartFrameNumber)
        {
            if (trackSegmentToMove is null)
            {
                throw new ArgumentNullException(nameof(trackSegmentToMove));
            }

            if (!CanMoveTrackSegment(trackSegmentToMove, destinationTrackNumber, destinationStartFrameNumber))
            {
                Debug.Fail($"Can't move '{trackSegmentToMove.Name}' segment to starting frame {destinationStartFrameNumber} and timeline track {destinationTrackNumber}.");
                return;
            }

            // Batch undo ChangeSet for Model & ViewModel removes so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
            _undoRoot.BeginChangeSetBatch($"'{trackSegmentToMove.Name}' segment moved", false);

            // Prepare for collection re-sorting
            _segmentProvidingViewModel.SegmentModels.Remove(trackSegmentToMove.Model);
            _segmentProvidingViewModel.SegmentViewModels.Remove(trackSegmentToMove);

            trackSegmentToMove.MoveTo(destinationTrackNumber, destinationStartFrameNumber);

            // Perform collection re-sorting
            _segmentProvidingViewModel.SegmentModels.Add(trackSegmentToMove.Model);
            _segmentProvidingViewModel.SegmentViewModels.Add(trackSegmentToMove);

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        public bool CanChangeTrackSegmentStartFrame(SegmentViewModelBase trackSegmentViewModel, int newStartFrameNumber)
        {
            // Validate segment change
            if (trackSegmentViewModel is null || newStartFrameNumber >= trackSegmentViewModel.EndFrame || newStartFrameNumber < 0)
            {
                return false;
            }

            // Check to see if track segments would overlap after change
            IVideoTimelineTrackViewModel trackViewModel = TimelineTrackCollection[trackSegmentViewModel.TrackNumber];
            int segmentIndex = trackViewModel.TrackSegments.IndexOf(trackSegmentViewModel);
            if (segmentIndex > 0 && trackViewModel.TrackSegments[segmentIndex - 1].EndFrame > newStartFrameNumber)
            {
                return false;
            }

            // Change is valid
            return true;
        }

        /// <inheritdoc/>
        public bool CanChangeTrackSegmentEndFrame(SegmentViewModelBase trackSegmentViewModel, int newEndFrameNumber)
        {
            // Validate segment change
            if (trackSegmentViewModel is null || newEndFrameNumber <= trackSegmentViewModel.StartFrame || newEndFrameNumber >= ScriptVideoContext.VideoFrameCount)
            {
                return false;
            }

            // Check to see if track segments would overlap after change
            IVideoTimelineTrackViewModel trackViewModel = TimelineTrackCollection[trackSegmentViewModel.TrackNumber];
            if (trackViewModel.TrackSegments.Count > 1)
            {
                int segmentIndex = trackViewModel.TrackSegments.IndexOf(trackSegmentViewModel);
                if (segmentIndex < (trackViewModel.TrackSegments.Count - 1) && trackViewModel.TrackSegments[segmentIndex + 1].StartFrame < newEndFrameNumber)
                {
                    return false;
                }
            }

            // Change is valid
            return true;
        }

        /// <inheritdoc/>
        public void ChangeTrackSegmentStartFrame(SegmentViewModelBase trackSegmentViewModel, int newStartFrameNumber)
        {
            if (trackSegmentViewModel is null)
            {
                throw new ArgumentNullException(nameof(trackSegmentViewModel));
            }

            if (!CanChangeTrackSegmentStartFrame(trackSegmentViewModel, newStartFrameNumber))
            {
                throw new InvalidOperationException($"Can't change the start frame of the '{trackSegmentViewModel.Name}' segment to {newStartFrameNumber}.");
            }

            WeakReference<SegmentViewModelBase> trackSegmentViewModelRef = new WeakReference<SegmentViewModelBase>(trackSegmentViewModel);

            _undoRoot.BeginChangeSetBatch($"'{trackSegmentViewModel.Name}' segment start frame changed", false);

            // On Undo
            _undoRoot.AddChange(
                new DelegateChange(this,
                    () => RefreshActiveSegmentsUndoDelegate(trackSegmentViewModelRef),
                    null,
                    (trackSegmentViewModel.StartFrame, nameof(SegmentViewModelBase.StartFrame), trackSegmentViewModel.EndFrame, nameof(SegmentViewModelBase.EndFrame), trackSegmentViewModel.TrackNumber, nameof(SegmentViewModelBase.TrackNumber), trackSegmentViewModel.Name)
                ),
                $"Active segments refreshed after undoing the '{trackSegmentViewModel.Name}' segment start frame change"
            );

            trackSegmentViewModel.MoveStartFrame(newStartFrameNumber);

            // On Redo
            _undoRoot.AddChange(
                new DelegateChange(this,
                    null,
                    () => RefreshActiveSegmentsUndoDelegate(trackSegmentViewModelRef),
                    (trackSegmentViewModel.StartFrame, nameof(SegmentViewModelBase.StartFrame), trackSegmentViewModel.EndFrame, nameof(SegmentViewModelBase.EndFrame), trackSegmentViewModel.TrackNumber, nameof(SegmentViewModelBase.TrackNumber), trackSegmentViewModel.Name)
                ),
                $"Active segments refreshed after redoing the '{trackSegmentViewModel.Name}' segment start frame change"
            );

            _undoRoot.EndChangeSetBatch();

            RefreshActiveSegmentsIfCurrentFrameIsWithinSegment(trackSegmentViewModel);
        }

        /// <inheritdoc/>
        public void ChangeTrackSegmentEndFrame(SegmentViewModelBase trackSegmentViewModel, int newEndFrameNumber)
        {
            if (trackSegmentViewModel is null)
            {
                throw new ArgumentNullException(nameof(trackSegmentViewModel));
            }

            if (!CanChangeTrackSegmentEndFrame(trackSegmentViewModel, newEndFrameNumber))
            {
                throw new InvalidOperationException($"Can't change the end frame of the '{trackSegmentViewModel.Name}' segment to {newEndFrameNumber}.");
            }

            WeakReference<SegmentViewModelBase> trackSegmentViewModelRef = new WeakReference<SegmentViewModelBase>(trackSegmentViewModel);

            _undoRoot.BeginChangeSetBatch($"'{trackSegmentViewModel.Name}' segment end frame changed", false);

            // On Undo
            _undoRoot.AddChange(
                new DelegateChange(this,
                    () => RefreshActiveSegmentsUndoDelegate(trackSegmentViewModelRef),
                    null,
                    (trackSegmentViewModel.StartFrame, nameof(SegmentViewModelBase.StartFrame), trackSegmentViewModel.EndFrame, nameof(SegmentViewModelBase.EndFrame), trackSegmentViewModel.TrackNumber, nameof(SegmentViewModelBase.TrackNumber), trackSegmentViewModel.Name)
                ),
                $"Active segments refreshed after undoing the '{trackSegmentViewModel.Name}' segment end frame change"
            );

            trackSegmentViewModel.MoveEndFrame(newEndFrameNumber);

            // On Redo
            _undoRoot.AddChange(
                new DelegateChange(this,
                    null,
                    () => RefreshActiveSegmentsUndoDelegate(trackSegmentViewModelRef),
                    (trackSegmentViewModel.StartFrame, nameof(SegmentViewModelBase.StartFrame), trackSegmentViewModel.EndFrame, nameof(SegmentViewModelBase.EndFrame), trackSegmentViewModel.TrackNumber, nameof(SegmentViewModelBase.TrackNumber), trackSegmentViewModel.Name)
                ),
                $"Active segments refreshed after redoing the '{trackSegmentViewModel.Name}' segment end frame change"
            );

            _undoRoot.EndChangeSetBatch();

            RefreshActiveSegmentsIfCurrentFrameIsWithinSegment(trackSegmentViewModel);
        }

        /// <summary>
        /// Determines whether the <see cref="AddTrackSegmentCommandParameters"/> instance describes a segment of a type and frame duration
        /// that can be added to the <see cref="SelectedTrack"/> at the current <see cref="IScriptVideoContext.FrameNumber"/>
        /// without overlapping any existing segments on the track.
        /// </summary>
        /// <remarks>CanExecute delegate method for the <see cref="AddTrackSegmentCommand"/>.</remarks>
        /// <returns><see langword="true"/> if the segment can be added to the <see cref="SelectedTrack"/> at the current <see cref="IScriptVideoContext.FrameNumber"/>, otherwise <see langword="false"/>.</returns>
        private bool CanAddTrackSegmentCommandExecute()
        {
            return AddTrackSegmentCommandParameters?.SegmentTypeDescriptor != null && TimelineSegments != null && ScriptVideoContext.HasVideo && SelectedTrack != null
                   && CanAddTrackSegment(SelectedTrack.TrackNumber, ScriptVideoContext.FrameNumber, AddTrackSegmentCommandParameters.FrameDuration);
        }

        /// <summary>
        /// Determines whether the specified track segment can be removed from the timeline.
        /// </summary>
        /// <remarks>
        /// <para>CanExecute delegate method for the <see cref="RemoveTrackSegmentCommand"/>.</para>
        /// If <paramref name="trackSegment"/> is null, the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/> is checked for suitability.
        /// </remarks>
        /// <param name="trackSegment">
        /// The track segment to check. If null, the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/>.
        /// </param>
        /// <returns>True if the track segment can be removed, otherwise False.</returns>
        private bool CanRemoveTrackSegment(SegmentViewModelBase trackSegment)
        {
            return TimelineSegments != null && ScriptVideoContext.HasVideo
                    && (trackSegment != null || TimelineSegmentProvidingViewModel?.SelectedSegment != null);
        }

        /// <summary>
        /// Removes the specified track segment from the timeline.
        /// </summary>
        /// <remarks>
        /// <para>Execute delegate method for the <see cref="RemoveTrackSegmentCommand"/>.</para>
        /// If <paramref name="trackSegment"/> is null, the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/> is removed.
        /// </remarks>
        /// <param name="trackSegment">
        /// The track segment to remove. If null, the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/>.
        /// </param>
        private void RemoveTrackSegment(SegmentViewModelBase trackSegment)
        {
            bool trackSegmentWasSelected = trackSegment == TimelineSegmentProvidingViewModel.SelectedSegment;

            if (trackSegment == null)
            {
                trackSegment = TimelineSegmentProvidingViewModel.SelectedSegment;
                trackSegmentWasSelected = true;
            }

            if (trackSegment != null)
            {
                if (trackSegmentWasSelected)
                    TimelineSegmentProvidingViewModel.SelectedSegment = null;

                // Batch undo ChangeSet for Model & ViewModel removes so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
                _undoRoot.BeginChangeSetBatch($"'{trackSegment.Name}' segment removed", false);

                TimelineSegmentProvidingViewModel.SegmentViewModels.Remove(trackSegment);
                TimelineSegmentProvidingViewModel.SegmentModels.Remove(trackSegment.Model);

                _undoRoot.EndChangeSetBatch();
            }
        }

        /// <summary>
        /// Determines whether the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/>
        /// can be split at the current <see cref="IScriptVideoContext.FrameNumber"/>.
        /// </summary>
        /// <remarks>CanExecute delegate method for the <see cref="SplitSelectedTrackSegmentCommand"/>.</remarks>
        /// <returns>
        /// True if the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/> can be split
        /// at the current <see cref="IScriptVideoContext.FrameNumber"/>, otherwise False.
        /// </returns>
        private bool CanSplitSelectedTrackSegment()
        {
            if (TimelineSegmentProvidingViewModel?.SelectedSegment is SegmentViewModelBase selectedSegment)
            {
                int frameNumberToSplitAt = ScriptVideoContext.FrameNumber;
                return frameNumberToSplitAt > selectedSegment.StartFrame && frameNumberToSplitAt < selectedSegment.EndFrame;
            }

            return false;
        }

        /// <summary>
        /// Splits the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/>
        /// at the current <see cref="IScriptVideoContext.FrameNumber"/>.
        /// </summary>
        /// <remarks>Execute delegate method for the <see cref="SplitSelectedTrackSegmentCommand"/>.</remarks>
        private void SplitSelectedTrackSegment()
        {
            SegmentViewModelBase segmentToSplit = TimelineSegmentProvidingViewModel.SelectedSegment;
            int frameNumberToSplitAt = ScriptVideoContext.FrameNumber;

            // Batch undo ChangeSet for Model & ViewModel collection changes so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
            _undoRoot.BeginChangeSetBatch($"'{segmentToSplit.Name}' segment split", false);

            // On Undo
            WeakReference<SegmentViewModelBase> segmentToSplitRef = new WeakReference<SegmentViewModelBase>(segmentToSplit);
            _undoRoot.AddChange(
                new DelegateChange(this,
                    () => RefreshActiveSegmentsUndoDelegate(segmentToSplitRef),
                    null,
                    (segmentToSplit.StartFrame, nameof(SegmentViewModelBase.StartFrame), segmentToSplit.EndFrame, nameof(SegmentViewModelBase.EndFrame), segmentToSplit.TrackNumber, nameof(SegmentViewModelBase.TrackNumber), segmentToSplit.Name)
                ),
                "Active segments refreshed"
            );

            SegmentViewModelBase splitSegment = _segmentProvidingViewModel.SegmentViewModelFactory.CreateSplitSegmentModelViewModel(segmentToSplit, frameNumberToSplitAt);

            _segmentProvidingViewModel.SegmentModels.Add(splitSegment.Model);
            _segmentProvidingViewModel.SegmentViewModels.Add(splitSegment);

            _undoRoot.EndChangeSetBatch();

            RefreshActiveSegmentsIfCurrentFrameIsWithinSegment(splitSegment);
        }

        /// <summary>
        /// Determines whether the specified track segment can be merged
        /// with the segment on the track to its immediate left.
        /// </summary>
        /// <remarks>CanExecute delegate method for the <see cref="MergeTrackSegmentLeftCommand"/>.</remarks>
        /// <param name="rightTrackSegment">The track segment to check.</param>
        /// <returns>
        /// True if the track segment can be merged with the segment on the track to its immediate left,
        /// otherwise False.
        /// </returns>
        private bool CanMergeTrackSegmentLeft(SegmentViewModelBase rightTrackSegment)
        {
            if (rightTrackSegment == null)
            {
                return false;
            }

            IVideoTimelineTrackViewModel trackViewModel = TimelineTrackCollection[rightTrackSegment.TrackNumber];
            if (trackViewModel.TrackSegments.Count <= 1)
            {
                return false;
            }

            int rightTrackSegmentIndex = trackViewModel.TrackSegments.IndexOf(rightTrackSegment);
            return rightTrackSegmentIndex > 0 && trackViewModel.TrackSegments[rightTrackSegmentIndex - 1].EndFrame == rightTrackSegment.StartFrame - 1;
        }

        /// <summary>
        /// Merges a track segment with the segment on the track to its immediate left.
        /// </summary>
        /// <remarks>Execute delegate method for the <see cref="MergeTrackSegmentLeftCommand"/>.</remarks>
        /// <param name="rightTrackSegment">
        /// The track segment to merge with the segment on the track to its immediate left.
        /// </param>
        private void MergeTrackSegmentLeft(SegmentViewModelBase rightTrackSegment)
        {
            IVideoTimelineTrackViewModel trackViewModel = TimelineTrackCollection[rightTrackSegment.TrackNumber];
            int rightTrackSegmentIndex = trackViewModel.TrackSegments.IndexOf(rightTrackSegment);
            SegmentViewModelBase leftTrackSegment = trackViewModel.TrackSegments[rightTrackSegmentIndex - 1];

            MergeTrackSegments(leftTrackSegment, rightTrackSegment, $"'{rightTrackSegment.Name}' segment merged with '{leftTrackSegment.Name}'");
        }

        /// <summary>
        /// Determines whether the specified track segment can be merged
        /// with the segment on the track to its immediate right.
        /// </summary>
        /// <remarks>CanExecute delegate method for the <see cref="MergeTrackSegmentRightCommand"/>.</remarks>
        /// <param name="leftTrackSegment">The track segment to check.</param>
        /// <returns>
        /// True if the track segment can be merged with the segment on the track to its immediate right,
        /// otherwise False.
        /// </returns>
        private bool CanMergeTrackSegmentRight(SegmentViewModelBase leftTrackSegment)
        {
            if (leftTrackSegment == null)
            {
                return false;
            }

            IVideoTimelineTrackViewModel trackViewModel = TimelineTrackCollection[leftTrackSegment.TrackNumber];
            if (trackViewModel.TrackSegments.Count <= 1)
            {
                return false;
            }

            int leftTrackSegmentIndex = trackViewModel.TrackSegments.IndexOf(leftTrackSegment);
            return leftTrackSegmentIndex < trackViewModel.TrackSegments.Count - 1 && trackViewModel.TrackSegments[leftTrackSegmentIndex + 1].StartFrame == leftTrackSegment.EndFrame + 1;
        }

        /// <summary>
        /// Merges a track segment with the segment on the track to its immediate right.
        /// </summary>
        /// <remarks>Execute delegate method for the <see cref="MergeTrackSegmentRightCommand"/>.</remarks>
        /// <param name="leftTrackSegment">
        /// The track segment to merge with the segment on the track to its immediate right.
        /// </param>
        private void MergeTrackSegmentRight(SegmentViewModelBase leftTrackSegment)
        {
            IVideoTimelineTrackViewModel trackViewModel = TimelineTrackCollection[leftTrackSegment.TrackNumber];
            int leftTrackSegmentIndex = trackViewModel.TrackSegments.IndexOf(leftTrackSegment);
            SegmentViewModelBase rightTrackSegment = trackViewModel.TrackSegments[leftTrackSegmentIndex + 1];

            MergeTrackSegments(leftTrackSegment, rightTrackSegment, $"'{leftTrackSegment.Name}' segment merged with '{rightTrackSegment.Name}'");
        }

        /// <summary>
        /// Prompts the user for a new name for the specified track segment, then renames the segment.
        /// </summary>
        /// <remarks>Execute delegate method for the <see cref="RenameTrackSegmentCommand"/>.</remarks>
        /// <param name="trackSegment">The track segment to rename.</param>
        private void RenameTrackSegment(SegmentViewModelBase trackSegment)
        {
            Debug.Assert(trackSegment != null);

            _dialogService.ShowDialog(
                nameof(Views.Dialogs.InputValuePromptDialog),
                new DialogParameters
                {
                    { nameof(IDialogAware.Title), "Please enter a new name for the segment" },
                    { nameof(Dialogs.InputValuePromptDialogViewModel.InputValue), trackSegment.Name }
                },
                dialogResult =>
                {
                    if (dialogResult.Result == ButtonResult.OK)
                    {
                        trackSegment.Name = dialogResult.Parameters.GetValue<string>(nameof(Dialogs.InputValuePromptDialogViewModel.InputValue));
                    }
                }
            );
        }

        /// <summary>
        /// Merges two adjacent track segments.
        /// </summary>
        /// <param name="leftSegment">The track segment that is to the immediate left of the <paramref name="rightSegment"/>.</param>
        /// <param name="rightSegment">The track segment that is to the immediate right of the <paramref name="leftSegment"/>.</param>
        /// <param name="undoChangeDescription">A human-readable description of the undoable changes.</param>
        private void MergeTrackSegments(SegmentViewModelBase leftSegment, SegmentViewModelBase rightSegment, string undoChangeDescription)
        {
            _undoRoot.BeginChangeSetBatch(undoChangeDescription, false);

            // On Undo
            WeakReference<SegmentViewModelBase> rightSegmentRef = new WeakReference<SegmentViewModelBase>(rightSegment);
            _undoRoot.AddChange(
                new DelegateChange(this,
                    () => RefreshActiveSegmentsUndoDelegate(rightSegmentRef),
                    null,
                    (rightSegment.StartFrame, nameof(SegmentViewModelBase.StartFrame), rightSegment.EndFrame, nameof(SegmentViewModelBase.EndFrame), rightSegment.TrackNumber, nameof(SegmentViewModelBase.TrackNumber), rightSegment.Name)
                ),
                "Active segments refreshed"
            );

            TimelineSegmentProvidingViewModel.SegmentViewModels.Remove(rightSegment);
            TimelineSegmentProvidingViewModel.SegmentModels.Remove(rightSegment.Model);

            for (int i = 0; i < rightSegment.KeyFrameViewModels.Count; /* don't increment i */)
            {
                KeyFrameViewModelBase keyFrameViewModel = rightSegment.KeyFrameViewModels[i];

                rightSegment.KeyFrameViewModels.RemoveAt(i);
                rightSegment.Model.KeyFrames.RemoveAt(i);

                leftSegment.Model.KeyFrames.Add(keyFrameViewModel.Model);
                leftSegment.KeyFrameViewModels.Add(keyFrameViewModel);
            }

            leftSegment.EndFrame = rightSegment.EndFrame;

            // On Redo
            WeakReference<SegmentViewModelBase> leftSegmentRef = new WeakReference<SegmentViewModelBase>(leftSegment);
            _undoRoot.AddChange(
                new DelegateChange(this,
                    null,
                    () => RefreshActiveSegmentsUndoDelegate(leftSegmentRef),
                    (leftSegment.StartFrame, nameof(SegmentViewModelBase.StartFrame), leftSegment.EndFrame, nameof(SegmentViewModelBase.EndFrame), leftSegment.TrackNumber, nameof(SegmentViewModelBase.TrackNumber), leftSegment.Name)
                ),
                "Active segments refreshed"
            );

            _undoRoot.EndChangeSetBatch();

            RefreshActiveSegmentsIfCurrentFrameIsWithinSegment(leftSegment);
        }

        /// <summary>
        /// Refreshes the <see cref="ITimelineSegmentProvidingViewModel.ActiveSegments"/> collection
        /// if the current <see cref="IScriptVideoContext.FrameNumber"/> is within <paramref name="segmentViewModel"/>.
        /// </summary>
        /// <param name="segmentViewModel">
        /// The track segment to check if <see cref="IScriptVideoContext.FrameNumber"/> is within its inclusive
        /// start and end frame range.
        /// </param>
        private void RefreshActiveSegmentsIfCurrentFrameIsWithinSegment(SegmentViewModelBase segmentViewModel)
        {
            if (segmentViewModel.IsFrameWithin(ScriptVideoContext.FrameNumber))
            {
                TimelineSegmentProvidingViewModel.RefreshActiveSegments();
            }
        }

        /// <summary>
        /// Delegate for refreshing the <see cref="ITimelineSegmentProvidingViewModel.ActiveSegments"/> collection
        /// during an undo or redo action.
        /// </summary>
        /// <remarks>
        /// Calls <see cref="RefreshActiveSegmentsIfCurrentFrameIsWithinSegment(SegmentViewModelBase)"/>
        /// if the <paramref name="segmentViewModelRef"/> weak reference is alive.
        /// </remarks>
        /// <param name="segmentViewModelRef">
        /// A weak reference to the <see cref="SegmentViewModelBase"/> to pass to <see cref="RefreshActiveSegmentsIfCurrentFrameIsWithinSegment(SegmentViewModelBase)"/>.
        /// </param>
        private void RefreshActiveSegmentsUndoDelegate(WeakReference<SegmentViewModelBase> segmentViewModelRef)
        {
            if (segmentViewModelRef.TryGetTarget(out SegmentViewModelBase segmentViewModel))
            {
                RefreshActiveSegmentsIfCurrentFrameIsWithinSegment(segmentViewModel);
            }
        }

        #endregion Segment related methods

        #region Key frame related methods

        /// <summary>
        /// Determines whether a key frame can be added to the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/>
        /// at the current <see cref="IScriptVideoContext.FrameNumber"/>.
        /// </summary>
        /// <remarks>CanExecute delegate method for the <see cref="AddTrackSegmentKeyFrameCommand"/>.</remarks>
        /// <returns>
        /// True if a key frame can be added to the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/>
        /// at the current <see cref="IScriptVideoContext.FrameNumber"/>, otherwise False.
        /// </returns>
        private bool CanAddKeyFrameToSelectedSegment()
        {
            SegmentViewModelBase selectedSegment = TimelineSegmentProvidingViewModel?.SelectedSegment;
            return selectedSegment != null && selectedSegment?.ActiveKeyFrame == null;
        }

        /// <summary>
        /// Determines whether the timeline can <see cref="IScriptVideoService.SeekFrame(int)">seek</see> the key frame
        /// in the <see cref="SelectedTrack"/> located before the current <see cref="IScriptVideoContext.FrameNumber"/>.
        /// </summary>
        /// <remarks>CanExecute delegate method for the <see cref="SeekPreviousKeyFrameInTrackCommand"/>.</remarks>
        /// <returns>
        /// True if the first (or only) key frame of the first segment (if any) in the selected track (if there is one selected)
        /// has a <see cref="KeyFrameViewModelBase.FrameNumber"/> less than the current <see cref="IScriptVideoContext.FrameNumber"/>,
        /// otherwise False.
        /// </returns>
        private bool CanSeekPreviousKeyFrameInTrack()
        {
            if (TimelineSegmentProvidingViewModel == null)
            {
                return false;
            }

            int currentFrameNumber = ScriptVideoContext.FrameNumber;

            // Check the first (or only) key frame of the first segment (if any) in the selected track (if there is one selected)
            // to see if the current frame number is greater than its FrameNumber
            return ScriptVideoContext.HasVideo && currentFrameNumber > 0
                   && SelectedTrack?.TrackSegments?.FirstOrDefault()?.KeyFrameViewModels?.FirstOrDefault() is KeyFrameViewModelBase firstKeyFrame
                   && currentFrameNumber > firstKeyFrame.FrameNumber;
        }

        /// <summary>
        /// <see cref="IScriptVideoService.SeekFrame(int)">Seeks</see> the key frame in the <see cref="SelectedTrack"/>
        /// located before the current <see cref="IScriptVideoContext.FrameNumber"/>.
        /// </summary>
        /// <remarks>Execute delegate method for the <see cref="SeekPreviousKeyFrameInTrackCommand"/>.</remarks>
        private void SeekPreviousKeyFrameInTrack()
        {
            int currentFrameNumber = ScriptVideoContext.FrameNumber;
            int targetFrameNumber;

            if (_segmentProvidingViewModel.SelectedSegment is SegmentViewModelBase selectedSegment && selectedSegment.TrackNumber == SelectedTrack.TrackNumber)
            {
                int keyFrameIndex = selectedSegment.KeyFrameViewModels.IndexOf(selectedSegment.ActiveKeyFrame);
                if (keyFrameIndex > 0)
                {
                    // At a key frame after the first in the segment - go to previous key frame
                    targetFrameNumber = selectedSegment.KeyFrameViewModels[keyFrameIndex - 1].FrameNumber;
                }
                else if (currentFrameNumber > selectedSegment.KeyFrameViewModels.First().FrameNumber)
                {
                    // In-between key frames - search next key frame in the segment
                    keyFrameIndex = selectedSegment.KeyFrameViewModels.LowerBoundIndex(currentFrameNumber);

                    // Binary search finds equal to or next, so go to the key frame before the returned index
                    targetFrameNumber = selectedSegment.KeyFrameViewModels[keyFrameIndex - 1].FrameNumber;
                }
                else
                {
                    // At the first key frame in the segment - go to previous segment in track
                    int segmentIndex = SelectedTrack.TrackSegments.IndexOf(selectedSegment);
                    Debug.Assert(segmentIndex > 0);

                    targetFrameNumber = SelectedTrack.TrackSegments[segmentIndex - 1].KeyFrameViewModels.Last().FrameNumber;
                }
            }
            else
            {
                // In-between segments - go to segment in track less than current frame number

                SegmentViewModelBase trackSegment = SelectedTrack.TrackSegments.Last(segment => segment.EndFrame < currentFrameNumber);
                targetFrameNumber = trackSegment.KeyFrameViewModels.Last().FrameNumber;
            }

            ScriptVideoContext.FrameNumber = targetFrameNumber;
        }

        /// <summary>
        /// Determines whether the timeline can <see cref="IScriptVideoService.SeekFrame(int)">seek</see> the key frame
        /// in the <see cref="SelectedTrack"/> located after the current <see cref="IScriptVideoContext.FrameNumber"/>.
        /// </summary>
        /// <remarks>CanExecute delegate method for the <see cref="SeekNextKeyFrameInTrackCommand"/>.</remarks>
        /// <returns>
        /// True if the last (or only) key frame of the last segment (if any) in the selected track (if there is one selected)
        /// has a <see cref="KeyFrameViewModelBase.FrameNumber"/> greater than the current <see cref="IScriptVideoContext.FrameNumber"/>,
        /// otherwise False.
        /// </returns>
        private bool CanSeekNextKeyFrameInTrack()
        {
            if (TimelineSegmentProvidingViewModel == null)
            {
                return false;
            }

            int currentFrameNumber = ScriptVideoContext.FrameNumber;

            // Check the last (or only) key frame of the last segment (if any) in the selected track (if there is one selected)
            // to see if the current frame number is less than its FrameNumber
            return ScriptVideoContext.HasVideo && currentFrameNumber < ScriptVideoContext.SeekableVideoFrameCount
                   && SelectedTrack?.TrackSegments?.LastOrDefault()?.KeyFrameViewModels?.LastOrDefault() is KeyFrameViewModelBase lastKeyFrame
                   && currentFrameNumber < lastKeyFrame.FrameNumber;
        }

        /// <summary>
        /// <see cref="IScriptVideoService.SeekFrame(int)">Seeks</see> the key frame in the <see cref="SelectedTrack"/>
        /// located after the current <see cref="IScriptVideoContext.FrameNumber"/>.
        /// </summary>
        /// <remarks>Execute delegate method for the <see cref="SeekNextKeyFrameInTrackCommand"/>.</remarks>
        private void SeekNextKeyFrameInTrack()
        {
            int currentFrameNumber = ScriptVideoContext.FrameNumber;
            int targetFrameNumber;

            if (_segmentProvidingViewModel.SelectedSegment is SegmentViewModelBase selectedSegment && selectedSegment.TrackNumber == SelectedTrack.TrackNumber)
            {
                int keyFrameIndex = selectedSegment.KeyFrameViewModels.IndexOf(selectedSegment.ActiveKeyFrame);
                if (keyFrameIndex >= 0 && keyFrameIndex < selectedSegment.KeyFrameViewModels.Count - 1)
                {
                    // At a key frame before the last in the segment - go to next key frame
                    targetFrameNumber = selectedSegment.KeyFrameViewModels[keyFrameIndex + 1].FrameNumber;
                }
                else if (currentFrameNumber < selectedSegment.KeyFrameViewModels.Last().FrameNumber)
                {
                    // In-between key frames - search next key frame in the segment
                    keyFrameIndex = selectedSegment.KeyFrameViewModels.LowerBoundIndex(currentFrameNumber);
                    Debug.Assert(keyFrameIndex < selectedSegment.KeyFrameViewModels.Count);

                    targetFrameNumber = selectedSegment.KeyFrameViewModels[keyFrameIndex].FrameNumber;
                }
                else
                {
                    // At or beyond the last key frame in the segment - go to next segment in track
                    int segmentIndex = SelectedTrack.TrackSegments.IndexOf(selectedSegment);
                    Debug.Assert(segmentIndex < SelectedTrack.TrackSegments.Count - 1);

                    targetFrameNumber = SelectedTrack.TrackSegments[segmentIndex + 1].KeyFrameViewModels.First().FrameNumber;
                }
            }
            else
            {
                // In-between segments - go to segment in track greater than current frame number

                SegmentViewModelBase trackSegment = SelectedTrack.TrackSegments.First(segment => segment.StartFrame > currentFrameNumber);  // TODO: Could binary search work better here?
                targetFrameNumber = trackSegment.KeyFrameViewModels.First().FrameNumber;
            }

            ScriptVideoContext.FrameNumber = targetFrameNumber;
        }

        #endregion Key frame related methods
    }
}
