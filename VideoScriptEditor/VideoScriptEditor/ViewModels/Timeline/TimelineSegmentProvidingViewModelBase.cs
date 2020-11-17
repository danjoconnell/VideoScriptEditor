using MonitoredUndo;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Models;
using VideoScriptEditor.Services.ScriptVideo;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// Base class abstracting common logic for view models implementing <see cref="ITimelineSegmentProvidingViewModel"/>.
    /// </summary>
    /// <remarks>Implements <see cref="ITimelineSegmentProvidingViewModel"/>.</remarks>
    public abstract class TimelineSegmentProvidingViewModelBase : BindableBase, ITimelineSegmentProvidingViewModel
    {
        /// <summary>The <see cref="IScriptVideoService"/> instance for processing and previewing edited video.</summary>
        protected readonly IScriptVideoService _scriptVideoService;

        /// <summary>The <see cref="IUndoService"/> instance providing undo/redo support.</summary>
        protected readonly IUndoService _undoService;

        /// <summary>The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</summary>
        protected readonly IChangeFactory _undoChangeFactory;

        /// <summary>The <see cref="IApplicationCommands"/> instance providing a common set of application related commands.</summary>
        protected readonly IApplicationCommands _applicationCommands;

        /// <summary>The <see cref="UndoRoot"/> instance for the <see cref="RootUndoObject"/>.</summary>
        protected UndoRoot _undoRoot;

        /// <summary>A dictionary of 'Active' segment view models sorted and keyed by zero-based track number.</summary>
        protected ObservableSortedList<int, SegmentViewModelBase> _activeSegmentDictionary;

        /// <summary>The zero-based track number of the active (currently selected) track in the timeline.</summary>
        protected int _activeTrackNumber;

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.SelectedSegmentChanged"/>
        public event EventHandler SelectedSegmentChanged;

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.ScriptVideoContext"/>
        public IScriptVideoContext ScriptVideoContext { get; }

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.SegmentModels"/>
        public abstract SegmentModelCollection SegmentModels { get; }

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.SegmentViewModels"/>
        public SegmentViewModelCollection SegmentViewModels { get; }

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.SegmentViewModelFactory"/>
        public abstract ISegmentViewModelFactory SegmentViewModelFactory { get; }

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.SelectedSegment"/>
        public abstract SegmentViewModelBase SelectedSegment { get; set; }

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.ActiveTrackNumber"/>
        public int ActiveTrackNumber
        {
            get => _activeTrackNumber;
            set => SetProperty(ref _activeTrackNumber, value, OnActiveTrackNumberChanged);
        }

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.ActiveSegments"/>
        public IList<SegmentViewModelBase> ActiveSegments => _activeSegmentDictionary.Values;

        /// <inheritdoc cref="UndoRoot.UndoStack"/>
        public IEnumerable<ChangeSet> UndoStack => _undoRoot.UndoStack;

        /// <inheritdoc cref="UndoRoot.RedoStack"/>
        public IEnumerable<ChangeSet> RedoStack => _undoRoot.RedoStack;

        /// <inheritdoc cref="IApplicationCommands.UndoCommand"/>
        public DelegateCommand<ChangeSet> UndoCommand { get; }

        /// <inheritdoc cref="IApplicationCommands.RedoCommand"/>
        public DelegateCommand<ChangeSet> RedoCommand { get; }

        /// <summary>
        /// Base constructor for view models derived from the <see cref="TimelineSegmentProvidingViewModelBase"/> class.
        /// </summary>
        /// <param name="scriptVideoService">The <see cref="IScriptVideoService"/> instance for processing and previewing edited video.</param>
        /// <param name="undoService">The <see cref="IUndoService"/> instance providing undo/redo support.</param>
        /// <param name="undoChangeFactory">The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</param>
        /// <param name="applicationCommands">The <see cref="IApplicationCommands"/> instance providing a common set of application related commands.</param>
        protected TimelineSegmentProvidingViewModelBase(IScriptVideoService scriptVideoService, IUndoService undoService, IChangeFactory undoChangeFactory, IApplicationCommands applicationCommands)
        {
            _scriptVideoService = scriptVideoService;
            ScriptVideoContext = scriptVideoService.GetContextReference();
            _undoService = undoService;
            _undoChangeFactory = undoChangeFactory;
            _undoRoot = undoService[this];
            _applicationCommands = applicationCommands;

            SegmentViewModels = new SegmentViewModelCollection();

            _activeSegmentDictionary = new ObservableSortedList<int, SegmentViewModelBase>();

            UndoCommand = new DelegateCommand<ChangeSet>(
                executeMethod: OnUndoCommandExecuted,
                canExecuteMethod: (changeSet) => _undoRoot.CanUndo
            ).ObservesProperty(() => UndoStack);

            RedoCommand = new DelegateCommand<ChangeSet>(
                executeMethod: OnRedoCommandExecuted,
                canExecuteMethod: (changeSet) => _undoRoot.CanRedo
            ).ObservesProperty(() => RedoStack);
        }

        /// <inheritdoc cref="ISupportsUndo.GetUndoRoot"/>
        public object GetUndoRoot() => this;

        /// <inheritdoc cref="ITimelineSegmentProvidingViewModel.RefreshActiveSegments"/>
        public virtual void RefreshActiveSegments()
        {
            RefreshActiveSegmentsForFrame(ScriptVideoContext.FrameNumber);
        }

        /// <summary>
        /// Handles the <see cref="UndoRoot.UndoStackChanged"/> event for the <see cref="_undoRoot"/>.
        /// </summary>
        /// <remarks>
        /// Raises the <see cref="DelegateCommandBase.CanExecuteChanged"/> event on the <see cref="UndoCommand"/>.
        /// </remarks>
        /// <inheritdoc cref="EventHandler"/>
        protected virtual void OnUndoStackChanged(object sender, EventArgs e)
        {
            UndoCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Handles the <see cref="UndoRoot.RedoStackChanged"/> event for the <see cref="_undoRoot"/>.
        /// </summary>
        /// <remarks>
        /// Raises the <see cref="DelegateCommandBase.CanExecuteChanged"/> event on the <see cref="RedoCommand"/>.
        /// </remarks>
        /// <inheritdoc cref="EventHandler"/>
        protected virtual void OnRedoStackChanged(object sender, EventArgs e)
        {
            RedoCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Subscribes to events and registers commands common to view models deriving
        /// from the <see cref="TimelineSegmentProvidingViewModelBase"/> class.
        /// </summary>
        /// <remarks>
        /// These include <see cref="IScriptVideoService.FrameChanged"/>,
        /// <see cref="UndoRoot.UndoStackChanged"/>,
        /// <see cref="UndoRoot.RedoStackChanged"/>,
        /// <see cref="IApplicationCommands.UndoCommand"/>
        /// and <see cref="IApplicationCommands.RedoCommand"/>.
        /// </remarks>
        protected virtual void SubscribeCommonEventsAndCommands()
        {
            _scriptVideoService.FrameChanged -= OnScriptVideoFrameNumberChanged;
            _scriptVideoService.FrameChanged += OnScriptVideoFrameNumberChanged;

            _undoRoot.UndoStackChanged -= OnUndoStackChanged;
            _undoRoot.UndoStackChanged += OnUndoStackChanged;

            _undoRoot.RedoStackChanged -= OnRedoStackChanged;
            _undoRoot.RedoStackChanged += OnRedoStackChanged;

            _applicationCommands.UndoCommand.RegisterCommand(UndoCommand);
            _applicationCommands.RedoCommand.RegisterCommand(RedoCommand);
        }

        /// <summary>
        /// Unsubscribes from events and unregisters commands common to view models deriving
        /// from the <see cref="TimelineSegmentProvidingViewModelBase"/> class.
        /// </summary>
        /// <remarks>
        /// These include <see cref="IScriptVideoService.FrameChanged"/>,
        /// <see cref="UndoRoot.UndoStackChanged"/>,
        /// <see cref="UndoRoot.RedoStackChanged"/>,
        /// <see cref="INotifyCollectionChanged.CollectionChanged"/> for <see cref="SegmentModels"/> and <see cref="SegmentViewModels"/>,
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> for <see cref="SelectedSegment"/>,
        /// <see cref="IApplicationCommands.UndoCommand"/>
        /// and <see cref="IApplicationCommands.RedoCommand"/>.
        /// </remarks>
        protected virtual void UnsubscribeCommonEventsAndCommands()
        {
            if (SegmentModels != null)
            {
                SegmentModels.CollectionChanged -= OnSegmentModelsCollectionChanged;
            }

            SegmentViewModels.CollectionChanged -= OnSegmentViewModelsCollectionChanged;

            if (SelectedSegment != null)
            {
                SelectedSegment.PropertyChanged -= OnSelectedSegmentInstancePropertyChanged;
            }

            _scriptVideoService.FrameChanged -= OnScriptVideoFrameNumberChanged;

            _undoRoot.UndoStackChanged -= OnUndoStackChanged;
            _undoRoot.RedoStackChanged -= OnRedoStackChanged;

            _applicationCommands.UndoCommand.UnregisterCommand(UndoCommand);
            _applicationCommands.RedoCommand.UnregisterCommand(RedoCommand);
        }

        /// <summary>
        /// Handles the <see cref="IScriptVideoService.FrameChanged"/> event for the <see cref="_scriptVideoService"/>.
        /// </summary>
        /// <remarks>
        /// Refreshes the <see cref="ActiveSegments"/> collection for the <see cref="FrameChangedEventArgs.CurrentFrameNumber"/>.
        /// </remarks>
        /// <inheritdoc cref="EventHandler{FrameChangedEventArgs}"/>
        protected virtual void OnScriptVideoFrameNumberChanged(object sender, FrameChangedEventArgs e)
        {
            RefreshActiveSegmentsForFrame(e.CurrentFrameNumber);
        }

        /// <summary>
        /// Undoes all <see cref="ChangeSet"/>s up to and including <paramref name="changeSet"/>.
        /// </summary>
        /// <remarks>
        /// <para>Execute delegate method for the <see cref="UndoCommand"/>.</para>
        /// If <paramref name="changeSet"/> is null, the first available <see cref="ChangeSet"/>
        /// in the <see cref="UndoStack"/> is undone.
        /// </remarks>
        /// <param name="changeSet">
        /// The last <see cref="ChangeSet"/> to undo.
        /// If null, the first available <see cref="ChangeSet"/> in the <see cref="UndoStack"/>.
        /// </param>
        protected virtual void OnUndoCommandExecuted(ChangeSet changeSet)
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
        /// <para>Execute delegate method for the <see cref="RedoCommand"/>.</para>
        /// If <paramref name="changeSet"/> is null, the first available <see cref="ChangeSet"/>
        /// in the <see cref="RedoStack"/> is redone.
        /// </remarks>
        /// <param name="changeSet">
        /// The last <see cref="ChangeSet"/> to redo.
        /// If null, the first available <see cref="ChangeSet"/> in the <see cref="RedoStack"/>.
        /// </param>
        protected virtual void OnRedoCommandExecuted(ChangeSet changeSet)
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
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event for the <see cref="SegmentModels"/> collection.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        protected virtual void OnSegmentModelsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            string changeDescription;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1);
                    changeDescription = $"'{((SegmentModelBase)e.NewItems[0]).Name}' segment model added";
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1);
                    changeDescription = $"'{((SegmentModelBase)e.OldItems[0]).Name}' segment model removed";
                    break;
                case NotifyCollectionChangedAction.Move:
                    Debug.Assert(e.OldItems.Count == 1);
                    changeDescription = $"'{((SegmentModelBase)e.OldItems[0]).Name}' segment model moved";
                    break;
                default:
                    changeDescription = "Timeline segment model collection changed";
                    break;
            }

            // log the collection changes with the undo framework
            _undoChangeFactory.OnCollectionChanged(this, nameof(SegmentModels), SegmentModels, e, changeDescription);
        }

        /// <summary>
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event for the <see cref="SegmentViewModels"/> collection.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        protected virtual void OnSegmentViewModelsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SegmentViewModelBase changedSegment;
            string changeDescription;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1);
                    changedSegment = (SegmentViewModelBase)e.NewItems[0];
                    changeDescription = $"'{changedSegment.Name}' segment added";
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1);
                    changedSegment = (SegmentViewModelBase)e.OldItems[0];
                    changeDescription = $"'{changedSegment.Name}' segment removed";
                    break;
                case NotifyCollectionChangedAction.Move:
                    Debug.Assert(e.OldItems.Count == 1);
                    changedSegment = (SegmentViewModelBase)e.OldItems[0];
                    changeDescription = $"'{changedSegment.Name}' segment moved";
                    break;
                default:
                    changedSegment = null;
                    changeDescription = "Timeline segment view model collection changed";
                    break;
            }

            // log the collection changes with the undo framework
            _undoChangeFactory.OnCollectionChanged(this, nameof(SegmentViewModels), SegmentViewModels, e, changeDescription);

            if (changedSegment?.IsFrameWithin(ScriptVideoContext.FrameNumber) == true)
            {
                RefreshActiveSegmentsForFrame(ScriptVideoContext.FrameNumber);
            }

            RaisePropertyChanged(nameof(SegmentViewModels));
        }

        /// <summary>
        /// Invoked whenever the value of the <see cref="SelectedSegment"/> property changes.
        /// </summary>
        protected virtual void OnSelectedSegmentChanged()
        {
            if (SelectedSegment != null)
            {
                ActiveTrackNumber = SelectedSegment.TrackNumber;
                SelectedSegment.IsSelected = true;

                SelectedSegment.PropertyChanged -= OnSelectedSegmentInstancePropertyChanged;
                SelectedSegment.PropertyChanged += OnSelectedSegmentInstancePropertyChanged;
            }

            SelectedSegmentChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Invoked whenever the value of the <see cref="ActiveTrackNumber"/> property changes.
        /// </summary>
        protected virtual void OnActiveTrackNumberChanged()
        {
            if (SelectedSegment == null || SelectedSegment.TrackNumber != _activeTrackNumber)
            {
                SelectedSegment = _activeSegmentDictionary.TryGetValue(_activeTrackNumber, out SegmentViewModelBase segment) ? segment : null;
            }
        }

        /// <summary>
        /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// for the current <see cref="SelectedSegment"/> instance.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedEventHandler"/>
        protected virtual void OnSelectedSegmentInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.Assert(sender == SelectedSegment); // Catch memory/event handler registration leaks.
        }


        /// <summary>
        /// Refreshes the <see cref="ActiveSegments"/> collection
        /// to only include segments whose frame range includes the specified zero-based frame number.
        /// </summary>
        /// <param name="frameNumber">The zero-based frame number to filter the <see cref="SegmentViewModels"/> collection by.</param>
        protected virtual void RefreshActiveSegmentsForFrame(int frameNumber)
        {
            if (SelectedSegment != null)
            {
                SelectedSegment.PropertyChanged -= OnSelectedSegmentInstancePropertyChanged;
            }

            List<int> activeTrackNumbers = new List<int>();

            var segmentsForFrame = SegmentViewModels.Where(segment => segment.IsFrameWithin(frameNumber));
            foreach (SegmentViewModelBase activeSegment in segmentsForFrame)
            {
                int trackNumber = activeSegment.TrackNumber;
                activeTrackNumbers.Add(trackNumber);

                if (_activeSegmentDictionary.TryGetValue(trackNumber, out SegmentViewModelBase segment))
                {
                    if (segment != activeSegment)
                    {
                        segment.ActiveKeyFrame = null;
                        _activeSegmentDictionary[trackNumber] = activeSegment;
                    }
                }
                else
                {
                    _activeSegmentDictionary.Add(trackNumber, activeSegment);
                }

                // Find key frames, lerp etc.
                int keyFrameIndex = activeSegment.KeyFrameViewModels.BinarySearch(frameNumber);
                if (keyFrameIndex >= 0)
                {
                    Debug.Assert(keyFrameIndex < activeSegment.KeyFrameViewModels.Count);

                    KeyFrameViewModelBase matchingKeyFrame = activeSegment.KeyFrameViewModels[keyFrameIndex];
                    if (!ScriptVideoContext.IsVideoPlaying)
                    {
                        activeSegment.ActiveKeyFrame = matchingKeyFrame;
                    }
                    else
                    {
                        activeSegment.ActiveKeyFrame = null;
                        activeSegment.Lerp(matchingKeyFrame, matchingKeyFrame, 0d);
                    }
                }
                else
                {
                    activeSegment.ActiveKeyFrame = null;

                    keyFrameIndex = ~keyFrameIndex;
                    Debug.Assert(keyFrameIndex >= 0);

                    KeyFrameViewModelBase previousKeyFrame = (keyFrameIndex > 0) ? activeSegment.KeyFrameViewModels[keyFrameIndex - 1] : activeSegment.KeyFrameViewModels[keyFrameIndex];
                    KeyFrameViewModelBase nextKeyFrame = (keyFrameIndex < activeSegment.KeyFrameViewModels.Count) ? activeSegment.KeyFrameViewModels[keyFrameIndex] : previousKeyFrame;
                    Debug.Assert(previousKeyFrame != null && nextKeyFrame != null);

                    int frameRange = nextKeyFrame.FrameNumber - previousKeyFrame.FrameNumber;
                    double lerpAmount = (frameRange > 0) ? (double)(frameNumber - previousKeyFrame.FrameNumber) / frameRange : 0d; // prevent double.NaN value as a result of division by zero
                    activeSegment.Lerp(previousKeyFrame, nextKeyFrame, lerpAmount);
                }
            }

            if (activeTrackNumbers.Count == 0)
            {
                if (_activeSegmentDictionary.Count != 0)
                {
                    SelectedSegment = null;

                    foreach (SegmentViewModelBase inactiveSegment in _activeSegmentDictionary.Values)
                    {
                        inactiveSegment.ActiveKeyFrame = null;
                    }

                    _activeSegmentDictionary.Clear();
                }
            }
            else
            {
                // Remove excess items not keyed to an active Track number
                int[] inactiveTrackNumbers = _activeSegmentDictionary.Keys.Except(activeTrackNumbers).ToArray(); // using ToArray() to force immediate query evaluation
                                                                                                                 // so that _activeSegmentDictionary can be modified.
                foreach (int inactiveTrackNumber in inactiveTrackNumbers)
                {
                    int inactiveSegmentIndex = _activeSegmentDictionary.IndexOfKey(inactiveTrackNumber);
                    Debug.Assert(inactiveSegmentIndex != -1);

                    _activeSegmentDictionary.Values[inactiveSegmentIndex].ActiveKeyFrame = null;
                    _activeSegmentDictionary.RemoveAt(inactiveSegmentIndex);
                }

                // (Re)set selected segment
                if (SelectedSegment != null && _activeSegmentDictionary.ContainsValue(SelectedSegment))
                {
                    SelectedSegment.PropertyChanged -= OnSelectedSegmentInstancePropertyChanged;
                    SelectedSegment.PropertyChanged += OnSelectedSegmentInstancePropertyChanged;
                }
                else
                {
                    SelectedSegment = _activeSegmentDictionary.TryGetValue(_activeTrackNumber, out SegmentViewModelBase segment) ? segment : null;
                }
            }
        }
    }
}
