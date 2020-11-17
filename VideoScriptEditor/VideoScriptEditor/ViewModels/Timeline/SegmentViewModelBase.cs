using MonitoredUndo;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Models;
using VideoScriptEditor.Services;
using VideoScriptEditor.Services.ScriptVideo;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels.Timeline
{
    /// <summary>
    /// Base segment view model class for coordinating interaction between a view and a <see cref="SegmentModelBase">segment model</see>.
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IDisposable"/> to ensure that the <see cref="INotifyCollectionChanged.CollectionChanged"/> event handler for
    /// <see cref="KeyFrameModels"/> is unsubscribed before garbage collection of a class instance.
    /// </remarks>
    public abstract class SegmentViewModelBase : BindableBase, IEquatable<SegmentViewModelBase>, IComparable<SegmentViewModelBase>, ISupportsUndo, IDisposable
    {
        /// <summary>For detecting redundant calls to Dispose</summary>
        protected bool _disposedValue;

        /// <summary>The <see cref="IClipboardService"/> instance providing access to the system clipboard.</summary>
        protected readonly IClipboardService _clipboardService;

        /// <summary>The <see cref="IUndoService"/> instance providing undo/redo support.</summary>
        protected readonly IUndoService _undoService;

        /// <summary>The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</summary>
        protected readonly IChangeFactory _undoChangeFactory;

        /// <summary>The undo "root document" or "root object" for this view model.</summary>
        protected object _rootUndoObject;

        /// <summary>The <see cref="UndoRoot"/> instance for the <see cref="_rootUndoObject"/>.</summary>
        protected UndoRoot _undoRoot;

        /// <summary>Indicates whether or not this segment is selected.</summary>
        protected bool _isSelected = false;

        /// <summary>Whether the data property values in this segment can be externally modified.</summary>
        protected bool _canBeEdited = false;

        /// <summary>
        /// The <see cref="SegmentModelBase">segment model</see> providing data for consumption by a view.
        /// </summary>
        public SegmentModelBase Model { get; }

        /// <inheritdoc cref="SegmentModelBase.Name"/>
        public virtual string Name
        {
            get => Model.Name;
            set
            {
                if (!string.IsNullOrWhiteSpace(value) && Model.Name != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(Name), Model.Name, value, $"'{Model.Name}' segment renamed to '{value}'");

                    Model.Name = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <inheritdoc cref="SegmentModelBase.StartFrame"/>
        public virtual int StartFrame
        {
            get => Model.StartFrame;
            set
            {
                if (Model.StartFrame != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(StartFrame), Model.StartFrame, value);

                    Model.StartFrame = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(FrameDuration));
                }
            }
        }

        /// <inheritdoc cref="SegmentModelBase.EndFrame"/>
        public virtual int EndFrame
        {
            get => Model.EndFrame;
            set
            {
                if (Model.EndFrame != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(EndFrame), Model.EndFrame, value);

                    Model.EndFrame = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(FrameDuration));
                }
            }
        }

        /// <inheritdoc cref="SegmentModelBase.TrackNumber"/>
        public virtual int TrackNumber
        {
            get => Model.TrackNumber;
            set
            {
                if (Model.TrackNumber != value)
                {
                    _undoChangeFactory.OnChanging(this, nameof(TrackNumber), Model.TrackNumber, value);

                    Model.TrackNumber = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the inclusive frame duration of the segment.
        /// </summary>
        /// <remarks>
        /// Note that Start/End Frames are zero-based and inclusive.
        /// For example, with Start Frame 0 and End Frame 9, the Duration is 10 frames.
        /// </remarks>
        public int FrameDuration => EndFrame - StartFrame + 1;

        /// <summary>
        /// A sorted collection of <see cref="KeyFrameViewModelBase">key frame view models</see> in this segment.
        /// </summary>
        public KeyFrameViewModelCollection KeyFrameViewModels { get; }

        /// <summary>
        /// The active key frame for this segment.
        /// </summary>
        /// <remarks>
        /// The key frame at <see cref="IScriptVideoContext.FrameNumber"/> (the current zero-based frame number of the video)
        /// or <see langword="null"/> if the current frame isn't a key frame in this segment.
        /// </remarks>
        public abstract KeyFrameViewModelBase ActiveKeyFrame { get; set; }

        /// <summary>
        /// Indicates whether or not this segment is selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// Gets whether the data property values in this segment can be externally modified.
        /// </summary>
        public virtual bool CanBeEdited
        {
            get => _canBeEdited;
            protected set => SetProperty(ref _canBeEdited, value);
        }

        /// <summary>
        /// The runtime context of the <see cref="IScriptVideoService"/> instance.
        /// </summary>
        public IScriptVideoContext ScriptVideoContext { get; }

        /// <summary>
        /// Gets a command for removing a <see cref="KeyFrameViewModelBase">key frame view model</see> from the segment.
        /// </summary>
        /// <remarks>The key frame at the beginning of the segment can't be removed.</remarks>
        public DelegateCommand<KeyFrameViewModelBase> RemoveKeyFrameCommand { get; }

        /// <summary>
        /// Gets a command for copying a <see cref="KeyFrameViewModelBase">key frame view model</see>
        /// to the system clipboard.
        /// </summary>
        public DelegateCommand<KeyFrameViewModelBase> CopyKeyFrameToClipboardCommand { get; }

        /// <summary>
        /// Gets a command for pasting <see cref="KeyFrameViewModelBase">key frame view model</see> data property values
        /// from a <see cref="KeyFrameModelBase">key frame model</see> in the system clipboard.
        /// </summary>
        public DelegateCommand<KeyFrameViewModelBase> PasteKeyFrameCommand { get; }

        /// <summary>
        /// A sorted collection of key frame models in this segment.
        /// </summary>
        /// <inheritdoc cref="SegmentModelBase.KeyFrames"/>
        protected KeyFrameModelCollection KeyFrameModels => Model.KeyFrames;

        /// <summary>
        /// Base constructor for segment view models derived from the <see cref="SegmentViewModelBase"/> class.
        /// </summary>
        /// <param name="model">The <see cref="SegmentModelBase">segment model</see> providing data for consumption by a view.</param>
        /// <param name="scriptVideoContext">The runtime context of the <see cref="IScriptVideoService"/> instance.</param>
        /// <param name="rootUndoObject">The undo "root document" or "root object" for this view model.</param>
        /// <param name="undoService">The <see cref="IUndoService"/> instance providing undo/redo support.</param>
        /// <param name="undoChangeFactory">The <see cref="IChangeFactory"/> instance for undo <see cref="Change"/> creation.</param>
        /// <param name="clipboardService">The <see cref="IClipboardService"/> instance providing access to the system clipboard.</param>
        /// <param name="keyFrameViewModels">
        /// A sorted collection of key frame view models in this segment.
        /// Defaults to a null <see cref="KeyFrameViewModelCollection"/>.
        /// </param>
        protected SegmentViewModelBase(SegmentModelBase model, IScriptVideoContext scriptVideoContext, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory, IClipboardService clipboardService, KeyFrameViewModelCollection keyFrameViewModels = null)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            _rootUndoObject = rootUndoObject;
            ScriptVideoContext = scriptVideoContext;
            _undoService = undoService;
            _undoChangeFactory = undoChangeFactory;
            _undoRoot = undoService[rootUndoObject];
            _clipboardService = clipboardService;

            RemoveKeyFrameCommand = new DelegateCommand<KeyFrameViewModelBase>(RemoveKeyFrame, CanRemoveKeyFrame);
            CopyKeyFrameToClipboardCommand = new DelegateCommand<KeyFrameViewModelBase>(CopyKeyFrameViewModelToClipboard, CanCopyKeyFrameViewModelToClipboard);
            PasteKeyFrameCommand = new DelegateCommand<KeyFrameViewModelBase>(PasteKeyFrameFromClipboard, CanPasteKeyFrameFromClipboard);

            KeyFrameViewModels = keyFrameViewModels ?? new KeyFrameViewModelCollection();
            if (KeyFrameViewModels.Count == 0)
            {
                foreach (KeyFrameModelBase keyFrameModel in model.KeyFrames)
                {
                    KeyFrameViewModels.Add(
                        CreateKeyFrameViewModel(keyFrameModel)
                    );
                }
            }

            KeyFrameViewModels.CollectionChanged += OnKeyFrameViewModelsCollectionChanged;
            Model.KeyFrames.CollectionChanged += OnKeyFrameModelsCollectionChanged;
        }

        /// <inheritdoc cref="ISupportsUndo.GetUndoRoot"/>
        public object GetUndoRoot() => _rootUndoObject;

        /// <inheritdoc cref="SegmentModelBase.IsFrameWithin(int)"/>
        public bool IsFrameWithin(int frameNumber) => Model.IsFrameWithin(frameNumber);

        /// <summary>
        /// Moves this segment to a different <see cref="TrackNumber"/> and/or <see cref="StartFrame"/> position.
        /// </summary>
        /// <param name="trackNumber">The new <see cref="TrackNumber"/> value.</param>
        /// <param name="startFrame">The new <see cref="StartFrame"/> value.</param>
        public virtual void MoveTo(int trackNumber, int startFrame)
        {
            if (TrackNumber == trackNumber && StartFrame == startFrame)
                return;

            bool batchUndoChanges = !_undoRoot.IsInBatch;
            if (batchUndoChanges)
            {
                _undoRoot.BeginChangeSetBatch($"'{Name}' segment moved", false);
            }

            int originalTrackNumber = TrackNumber;
            int originalStartFrame = StartFrame;

            _undoRoot.AddChange(
                new DelegateChange(this,
                    () => MoveTo(originalTrackNumber, originalStartFrame),
                    () => MoveTo(trackNumber, startFrame),
                    (originalTrackNumber, nameof(TrackNumber), originalStartFrame, nameof(StartFrame))
                ),
                $"'{Name}' segment moved"
            );

            int newEndFrame = startFrame + (EndFrame - StartFrame);

            TrackNumber = trackNumber;
            StartFrame = startFrame;
            EndFrame = newEndFrame;

            int startFrameOffset = startFrame - originalStartFrame;
            if (KeyFrameViewModels.Count > 0 && startFrameOffset != 0)
            {
                if (startFrameOffset > 0)
                {
                    for (int i = KeyFrameViewModels.Count - 1; i >= 0; i--)
                    {
                        KeyFrameViewModels[i].FrameNumber += startFrameOffset;
                    }
                }
                else if (startFrameOffset < 0)
                {
                    for (int i = 0; i < KeyFrameViewModels.Count; i++)
                    {
                        KeyFrameViewModels[i].FrameNumber += startFrameOffset;
                    }
                }

                RaisePropertyChanged(nameof(KeyFrameViewModels));
            }

            if (batchUndoChanges)
            {
                _undoRoot.EndChangeSetBatch();
            }
        }

        /// <summary>
        /// Expands or contracts the start of the segment to the specified frame number.
        /// </summary>
        /// <param name="newStartFrameNumber">The new start frame number.</param>
        public virtual void MoveStartFrame(int newStartFrameNumber)
        {
            Debug.Assert(KeyFrameViewModels.Count == KeyFrameModels.Count);

            if (newStartFrameNumber < StartFrame)
            {
                // Expanding

                Debug.Assert(KeyFrameModels.Count > 0 && KeyFrameModels[0].FrameNumber == StartFrame);

                if (KeyFrameViewModels.Count > 1)
                {
                    KeyFrameModelBase newStartKeyFrameModel = KeyFrameModels[0].DeepCopy();
                    newStartKeyFrameModel.FrameNumber = newStartFrameNumber;

                    KeyFrameModels.Insert(0, newStartKeyFrameModel);
                    KeyFrameViewModels.Insert(0, CreateKeyFrameViewModel(newStartKeyFrameModel));
                }
                else
                {
                    KeyFrameViewModels[0].FrameNumber = newStartFrameNumber;
                }

                StartFrame = newStartFrameNumber;
            }
            else
            {
                // Contracting

                StartFrame = newStartFrameNumber;

                if (ActiveKeyFrame != null && ActiveKeyFrame.FrameNumber < newStartFrameNumber)
                {
                    ActiveKeyFrame = null;
                }

                int lastKeyFrameIndexToRemove;

                int keyFrameAtOrAfterIndex = KeyFrameViewModels.BinarySearch(newStartFrameNumber);
                if (keyFrameAtOrAfterIndex >= 0)
                {
                    // Contracting to matching key frame - remove any key frames before
                    lastKeyFrameIndexToRemove = keyFrameAtOrAfterIndex - 1;
                }
                else
                {
                    // The index of the first key frame that has a frame number greater than or equal to newStartFrameNumber
                    // or the value of KeyFrameViewModels.Count if no such key frame was found.
                    keyFrameAtOrAfterIndex = ~keyFrameAtOrAfterIndex;

                    int keyFrameBeforeIndex = Math.Max(keyFrameAtOrAfterIndex - 1, 0);  // Addresses the possibility that keyFrameAtOrAfterIndex may be zero.
                    KeyFrameViewModelBase keyFrameViewModelBefore = KeyFrameViewModels[keyFrameBeforeIndex];

                    if (keyFrameAtOrAfterIndex < KeyFrameViewModels.Count)
                    {
                        // Lerp and add new start key frame
                        KeyFrameViewModelBase keyFrameViewModelAfter = KeyFrameViewModels[keyFrameAtOrAfterIndex];
                        KeyFrameViewModelBase lerpedKeyFrameViewModel = keyFrameViewModelBefore.Lerp(newStartFrameNumber, keyFrameViewModelAfter);
                        KeyFrameModels.Insert(keyFrameAtOrAfterIndex, lerpedKeyFrameViewModel.Model);
                        KeyFrameViewModels.Insert(keyFrameAtOrAfterIndex, lerpedKeyFrameViewModel);

                        // Remove any existing key frames before
                        lastKeyFrameIndexToRemove = keyFrameBeforeIndex;
                    }
                    else
                    {
                        // No key frame at or greater than newStartFrame
                        // 'Move' the last key frame in the collection by changing its frame number to the value of newStartFrameNumber
                        keyFrameViewModelBefore.FrameNumber = newStartFrameNumber;

                        // Remove any existing key frames before this one.
                        lastKeyFrameIndexToRemove = keyFrameBeforeIndex - 1;
                    }
                }

                for (int i = lastKeyFrameIndexToRemove; i >= 0; i--)
                {
                    KeyFrameViewModels.RemoveAt(i);
                    KeyFrameModels.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Expands or contracts the end of the segment to the specified frame number.
        /// </summary>
        /// <param name="newEndFrameNumber">The new end frame number.</param>
        public virtual void MoveEndFrame(int newEndFrameNumber)
        {
            Debug.Assert(KeyFrameViewModels.Count == KeyFrameModels.Count);

            if (newEndFrameNumber < EndFrame && KeyFrameViewModels.Count > 1)
            {
                // Contracting

                if (ActiveKeyFrame != null && ActiveKeyFrame.FrameNumber > newEndFrameNumber)
                {
                    ActiveKeyFrame = null;
                }

                int keyFrameAtOrAfterIndex = KeyFrameViewModels.BinarySearch(newEndFrameNumber);
                if (keyFrameAtOrAfterIndex < 0)
                {
                    // The index of the first key frame that has a frame number greater than or equal to newEndFrameNumber
                    // or the value of KeyFrameViewModels.Count if no such key frame was found.
                    keyFrameAtOrAfterIndex = ~keyFrameAtOrAfterIndex;
                    Debug.Assert(keyFrameAtOrAfterIndex > 0, "Trying to remove the first key frame");

                    while (keyFrameAtOrAfterIndex < KeyFrameViewModels.Count)
                    {
                        KeyFrameViewModels.RemoveAt(keyFrameAtOrAfterIndex);
                        KeyFrameModels.RemoveAt(keyFrameAtOrAfterIndex);
                    }
                }
            }

            EndFrame = newEndFrameNumber;
        }

        /// <summary>
        /// Completes an undoable action in which changes are being batch collected.
        /// </summary>
        /// <remarks>
        /// Calls <see cref="UndoRoot.EndChangeSetBatch"/> to stop collecting and combining undoable changes.
        /// </remarks>
        public virtual void CompleteUndoableAction()
        {
            _undoRoot.EndChangeSetBatch();
        }

        /// <summary>
        /// Linearly interpolates data property values between two <see cref="KeyFrameViewModelBase">key frame view models</see>
        /// based on the given weighting.
        /// </summary>
        /// <param name="fromKeyFrame">The first source <see cref="KeyFrameViewModelBase">key frame view model</see>.</param>
        /// <param name="toKeyFrame">The second source <see cref="KeyFrameViewModelBase">key frame view model</see>.</param>
        /// <param name="amount">Value indicating the weight of <paramref name="toKeyFrame"/>.</param>
        public abstract void Lerp(KeyFrameViewModelBase fromKeyFrame, KeyFrameViewModelBase toKeyFrame, double amount);

        /// <summary>
        /// Adds a key frame to the segment at <see cref="IScriptVideoContext.FrameNumber"/> (the current zero-based frame number of the video).
        /// </summary>
        public abstract void AddKeyFrame();

        /// <summary>
        /// Determines whether there is a key frame located before the current <see cref="ActiveKeyFrame"/>
        /// to copy data property values from.
        /// </summary>
        /// <returns><see langword="true"/> if there is a key frame located before the current <see cref="ActiveKeyFrame"/>; otherwise, <see langword="false"/>.</returns>
        public bool CanCopyFromPreviousKeyFrame()
        {
            return ActiveKeyFrame?.Model != null && KeyFrameModels.IndexOf(ActiveKeyFrame.Model) > 0;
        }

        /// <summary>
        /// Sets data properties to values copied from the key frame located before the current <see cref="ActiveKeyFrame"/>.
        /// </summary>
        public void CopyFromPreviousKeyFrame()
        {
            Debug.Assert(CanCopyFromPreviousKeyFrame() == true);

            int activeKeyFrameModelIndex = KeyFrameModels.IndexOf(ActiveKeyFrame.Model);
            CopyFromKeyFrameModel(KeyFrameModels[activeKeyFrameModelIndex - 1]);
        }

        /// <summary>
        /// Determines whether there is a key frame located after the current <see cref="ActiveKeyFrame"/>
        /// to copy data property values from.
        /// </summary>
        /// <returns><see langword="true"/> if there is a key frame located after the current <see cref="ActiveKeyFrame"/>; otherwise, <see langword="false"/>.</returns>
        public bool CanCopyFromNextKeyFrame()
        {
            if (ActiveKeyFrame?.Model != null && KeyFrameModels.Count > 1)
            {
                int activeKeyFrameModelIndex = KeyFrameModels.IndexOf(ActiveKeyFrame.Model);
                return activeKeyFrameModelIndex >= 0 && activeKeyFrameModelIndex < (KeyFrameModels.Count - 1);
            }

            return false;
        }

        /// <summary>
        /// Sets data properties to values copied from the key frame located after the current <see cref="ActiveKeyFrame"/>.
        /// </summary>
        public void CopyFromNextKeyFrame()
        {
            Debug.Assert(CanCopyFromNextKeyFrame() == true);

            int activeKeyFrameModelIndex = KeyFrameModels.IndexOf(ActiveKeyFrame.Model);
            CopyFromKeyFrameModel(KeyFrameModels[activeKeyFrameModelIndex + 1]);
        }

        /// <summary>
        /// Sets data properties to values copied from the specified <see cref="KeyFrameModelBase">key frame model</see>
        /// </summary>
        /// <param name="keyFrameModel">The <see cref="KeyFrameModelBase">key frame model</see> containing the data values to copy.</param>
        protected abstract void CopyFromKeyFrameModel(KeyFrameModelBase keyFrameModel);

        /// <summary>
        /// Determines whether the specified <see cref="KeyFrameViewModelBase">key frame view model</see>
        /// can be removed from the segment.
        /// </summary>
        /// <remarks>
        /// The key frame at the beginning of the segment can't be removed.
        /// If <paramref name="keyFrameViewModel"/> is <see langword="null"/>, the <see cref="ActiveKeyFrame"/> is checked for suitability.
        /// </remarks>
        /// <param name="keyFrameViewModel">The key frame view model to check. If <see langword="null"/>, the <see cref="ActiveKeyFrame"/>.</param>
        /// <returns><see langword="true"/> if the key frame view model can be removed; otherwise, <see langword="false"/>.</returns>
        protected bool CanRemoveKeyFrame(KeyFrameViewModelBase keyFrameViewModel)
        {
            if (keyFrameViewModel == null)
            {
                // Query removing the ActiveKeyFrame
                keyFrameViewModel = ActiveKeyFrame;
            }

            return keyFrameViewModel != null && keyFrameViewModel.FrameNumber != StartFrame;
        }

        /// <summary>
        /// Removes the specified <see cref="KeyFrameViewModelBase">key frame view model</see> from the segment.
        /// </summary>
        /// <remarks>If <paramref name="keyFrameViewModel"/> is <see langword="null"/>, the <see cref="ActiveKeyFrame"/> is removed.</remarks>
        /// <param name="keyFrameViewModel">The key frame view model to remove. If <see langword="null"/>, the <see cref="ActiveKeyFrame"/>.</param>
        protected void RemoveKeyFrame(KeyFrameViewModelBase keyFrameViewModel)
        {
            if (keyFrameViewModel == null)
            {
                // Removing the ActiveKeyFrame
                keyFrameViewModel = ActiveKeyFrame;
            }
            Debug.Assert(keyFrameViewModel != null);

            // Batch undo ChangeSet for Model & ViewModel removes so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
            _undoRoot.BeginChangeSetBatch($"'{Name}' segment key frame removed", false);

            KeyFrameViewModels.Remove(keyFrameViewModel);
            KeyFrameModels.Remove(keyFrameViewModel.Model);

            _undoRoot.EndChangeSetBatch();
        }

        /// <summary>
        /// Determines whether a key frame view model can be copied to the system clipboard.
        /// </summary>
        /// <remarks>If <paramref name="keyFrameViewModel"/> is <see langword="null"/>, the <see cref="ActiveKeyFrame"/> is checked for suitability.</remarks>
        /// <param name="keyFrameViewModel">The key frame view model to check. If <see langword="null"/>, the <see cref="ActiveKeyFrame"/>.</param>
        /// <returns><see langword="true"/> if the key frame view model can be copied to the system clipboard; otherwise, <see langword="false"/>.</returns>
        protected abstract bool CanCopyKeyFrameViewModelToClipboard(KeyFrameViewModelBase keyFrameViewModel);

        /// <summary>
        /// Copies a <see cref="KeyFrameViewModelBase">key frame view model</see> to the system clipboard.
        /// </summary>
        /// <remarks>If <paramref name="keyFrameViewModel"/> is <see langword="null"/>, the <see cref="ActiveKeyFrame"/> is copied.</remarks>
        /// <param name="keyFrameViewModel">The key frame view model to copy. If <see langword="null"/>, the <see cref="ActiveKeyFrame"/>.</param>
        protected void CopyKeyFrameViewModelToClipboard(KeyFrameViewModelBase keyFrameViewModel)
        {
            if (keyFrameViewModel == null)
            {
                // Copying the ActiveKeyFrame
                keyFrameViewModel = ActiveKeyFrame;
            }
            Debug.Assert(keyFrameViewModel != null);

            CopyKeyFrameModelToClipboard(keyFrameViewModel.Model);
        }

        /// <summary>
        /// Copies the specified <see cref="KeyFrameModelBase">key frame model</see> to the system clipboard.
        /// </summary>
        /// <param name="keyFrameModel">The <see cref="KeyFrameModelBase">key frame model</see> to copy to the system clipboard.</param>
        protected abstract void CopyKeyFrameModelToClipboard(KeyFrameModelBase keyFrameModel);

        /// <summary>
        /// Determines whether the system clipboard contains a <see cref="KeyFrameModelBase">key frame model</see>
        /// with data values that can be pasted to the target <see cref="KeyFrameViewModelBase">key frame view model</see>.
        /// </summary>
        /// <param name="targetKeyFrameViewModel">The target <see cref="KeyFrameViewModelBase">key frame view model</see>. If <see langword="null"/>, the <see cref="ActiveKeyFrame"/>.</param>
        /// <returns><see langword="true"/> if the system clipboard contains a key frame model with data values that can be pasted to the target key frame view model; otherwise, <see langword="false"/>.</returns>
        protected abstract bool CanPasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel);

        /// <summary>
        /// Pastes data property values from a <see cref="KeyFrameModelBase">key frame model</see> in the system clipboard
        /// to a target <see cref="KeyFrameViewModelBase">key frame view model</see>.
        /// </summary>
        /// <remarks>If <paramref name="targetKeyFrameViewModel"/> is <see langword="null"/>, the <see cref="ActiveKeyFrame"/> is the paste target.</remarks>
        /// <param name="targetKeyFrameViewModel">The target <see cref="KeyFrameViewModelBase">key frame view model</see>. If <see langword="null"/>, the <see cref="ActiveKeyFrame"/>.</param>
        protected abstract void PasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel);

        /// <summary>
        /// Pastes data property values from a <see cref="KeyFrameModelBase">key frame model</see>
        /// to a target <see cref="KeyFrameViewModelBase">key frame view model</see>.
        /// </summary>
        /// <remarks>If <paramref name="targetKeyFrameViewModel"/> is <see langword="null"/>, the <see cref="ActiveKeyFrame"/> is the paste target.</remarks>
        /// <param name="keyFrameModel">The <see cref="KeyFrameModelBase">key frame model</see> containing the data values to paste.</param>
        /// <param name="targetKeyFrameViewModel">The target <see cref="KeyFrameViewModelBase">key frame view model</see>. If <see langword="null"/>, the <see cref="ActiveKeyFrame"/>.</param>
        protected void PasteKeyFrameModel(KeyFrameModelBase keyFrameModel, KeyFrameViewModelBase targetKeyFrameViewModel)
        {
            if (ActiveKeyFrame != null && (targetKeyFrameViewModel == null || targetKeyFrameViewModel == ActiveKeyFrame))
            {
                // Paste into ActiveKeyFrame
                CopyFromKeyFrameModel(keyFrameModel);
            }
            else
            {
                Debug.Assert(targetKeyFrameViewModel != null);

                targetKeyFrameViewModel.CopyFromModel(keyFrameModel);
            }
        }

        /// <summary>
        /// Creates a new <see cref="KeyFrameViewModelBase"/> derived view model
        /// for coordinating interaction between a view and the specified key frame model.
        /// </summary>
        /// <param name="keyFrameModel">The key frame model providing the data model for the view model.</param>
        /// <returns>
        /// A new <see cref="KeyFrameViewModelBase"/> derived view model for coordinating interaction
        /// between a view and the key frame model specified by the <paramref name="keyFrameModel"/> parameter.
        /// </returns>
        protected internal abstract KeyFrameViewModelBase CreateKeyFrameViewModel(KeyFrameModelBase keyFrameModel);

        /// <summary>
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event for the <see cref="KeyFrameModels"/> collection.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        protected virtual void OnKeyFrameModelsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // log the collection changes with the undo framework
            _undoChangeFactory.OnCollectionChanged(this, nameof(KeyFrameModels), KeyFrameModels, e, GetKeyFrameCollectionChangeDescription(e));

            RaisePropertyChanged(nameof(KeyFrameModels));
        }

        /// <summary>
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event for the <see cref="KeyFrameViewModels"/> collection.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        protected virtual void OnKeyFrameViewModelsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // log the collection changes with the undo framework
            _undoChangeFactory.OnCollectionChanged(this, nameof(KeyFrameViewModels), KeyFrameViewModels, e, GetKeyFrameCollectionChangeDescription(e));

            RaisePropertyChanged(nameof(KeyFrameViewModels));

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (KeyFrameViewModelBase newKeyFrame in e.NewItems)
                {
                    if (newKeyFrame.FrameNumber == ScriptVideoContext.FrameNumber)
                    {
                        ActiveKeyFrame = newKeyFrame;
                        break;
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                int frameNumber = ScriptVideoContext.FrameNumber;

                bool removedActiveKeyFrame = e.OldItems.Cast<KeyFrameViewModelBase>().Any(kf => kf.FrameNumber == frameNumber);
                if (removedActiveKeyFrame)
                {
                    ActiveKeyFrame = null;

                    if (KeyFrameViewModels.Count > 0) // When joining segments, the right side segment will have zero key frames before being removed
                    {
                        // Find key frame at or after the one that was removed
                        int keyFrameAtOrAfterIndex = KeyFrameViewModels.LowerBoundIndex(frameNumber);

                        KeyFrameViewModelBase keyFrameViewModelBefore = keyFrameAtOrAfterIndex > 0 ? KeyFrameViewModels[keyFrameAtOrAfterIndex - 1] : KeyFrameViewModels[keyFrameAtOrAfterIndex];
                        KeyFrameViewModelBase keyFrameViewModelAfter = keyFrameAtOrAfterIndex < KeyFrameViewModels.Count ? KeyFrameViewModels[keyFrameAtOrAfterIndex] : keyFrameViewModelBefore;
                        Debug.Assert(keyFrameViewModelBefore != null && keyFrameViewModelAfter != null);

                        int frameRange = keyFrameViewModelAfter.FrameNumber - keyFrameViewModelBefore.FrameNumber;
                        double lerpAmount = frameRange > 0 ? (double)(frameNumber - keyFrameViewModelBefore.FrameNumber) / frameRange : 0d; // prevent double.NaN value as a result of division by zero
                        Lerp(keyFrameViewModelBefore, keyFrameViewModelAfter, lerpAmount);
                    }
                }
            }
        }

        /// <summary>
        /// Invoked whenever the value of the <see cref="ActiveKeyFrame"/> property changes.
        /// </summary>
        protected virtual void OnActiveKeyFrameChanged()
        {
            if (ActiveKeyFrame != null)
            {
                ActiveKeyFrame.IsActive = true;

                ActiveKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
                ActiveKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

                CanBeEdited = true;
            }
            else
            {
                CanBeEdited = false;
            }
        }

        /// <summary>
        /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// for the current <see cref="ActiveKeyFrame"/> instance.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedEventHandler"/>
        protected virtual void OnActiveKeyFrameInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.Assert(sender == ActiveKeyFrame); // Catch memory/event handler registration leaks.
        }

        /// <summary>
        /// Gets a human-readable description of changes made to a key frame collection.
        /// </summary>
        /// <param name="collectionChangedEventArgs">
        /// A <see cref="NotifyCollectionChangedEventArgs"/> instance providing data
        /// about the changes made to the collection.
        /// </param>
        /// <returns>A <see cref="string"/> describing the changes made to the key frame collection.</returns>
        protected virtual string GetKeyFrameCollectionChangeDescription(NotifyCollectionChangedEventArgs collectionChangedEventArgs)
        {
            StringBuilder changeDescription = new StringBuilder($"'{Name}' segment key frame ");
            switch (collectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    changeDescription.Append("added");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    changeDescription.Append("removed");
                    break;
                case NotifyCollectionChangedAction.Move:
                    changeDescription.Append("moved");
                    break;
                default:
                    changeDescription.Append("collection changed");
                    break;
            }

            return changeDescription.ToString();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as SegmentViewModelBase);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] SegmentViewModelBase other)
        {
            // If parameter is null, return false.
            if (other is null)
            {
                return false;
            }

            // Optimization for a common success case.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // If run-time types are not exactly the same, return false.
            if (GetType() != other.GetType())
            {
                return false;
            }

            // Return true if the fields match.
            // Note that the base class is not invoked because it is
            // System.Object, which defines Equals as reference equality.
            return Model.Equals(other.Model);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Model);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)"/>
        public virtual int CompareTo(SegmentViewModelBase other)
        {
            // If other is not a valid object reference, this instance is greater. 
            if (other == null) return 1;

            return Model.CompareTo(other.Model);
        }

        /// <summary>
        /// Releases all resources used by the current instance.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (Model?.KeyFrames != null)
                    {
                        Model.KeyFrames.CollectionChanged -= OnKeyFrameModelsCollectionChanged;
                    }
                }

                _disposedValue = true;
            }
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
