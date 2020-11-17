using MonitoredUndo;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Masking.Shapes;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.ViewModels.Masking.Shapes
{
    /// <summary>
    /// View Model for coordinating interaction between a view and a <see cref="RectangleMaskShapeModel">rectangle masking shape segment model</see>.
    /// </summary>
    public class RectangleMaskShapeViewModel : MaskShapeViewModelBase, IEquatable<RectangleMaskShapeViewModel>
    {
        private RectangleMaskShapeKeyFrameViewModel _activeKeyFrame = null;

        // Interpolated data fields
        private Rect _lerpedBounds;

        /// <inheritdoc cref="MaskShapeViewModelBase.ShapeType"/>
        public override MaskShapeType ShapeType => MaskShapeType.Rectangle;

        /// <inheritdoc cref="SegmentViewModelBase.ActiveKeyFrame"/>
        public override KeyFrameViewModelBase ActiveKeyFrame
        {
            get => _activeKeyFrame;
            set
            {
                if (_activeKeyFrame != null)
                {
                    _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
                    _activeKeyFrame.IsActive = false;
                }

                SetProperty(ref _activeKeyFrame, (RectangleMaskShapeKeyFrameViewModel)value, OnActiveKeyFrameChanged);
            }
        }

        /// <inheritdoc cref="MaskShapeViewModelBase.Bounds"/>
        public override Rect Bounds
        {
            get => _activeKeyFrame?.Bounds ?? _lerpedBounds;
            set
            {
                if (_activeKeyFrame != null)
                {
                    if (!value.IsEmpty)
                    {
                        ResizeBounds(value);
                    }
                }
                else
                {
                    SetProperty(ref _lerpedBounds, value);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="RectangleMaskShapeViewModel"/> instance.
        /// </summary>
        /// <param name="model">The <see cref="RectangleMaskShapeModel">rectangle masking shape segment model</see> providing data for consumption by a view.</param>
        /// <inheritdoc cref="MaskShapeViewModelBase(SegmentModelBase, Services.ScriptVideo.IScriptVideoContext, object, IUndoService, IChangeFactory, Services.IClipboardService, KeyFrameViewModelCollection)"/>
        public RectangleMaskShapeViewModel(RectangleMaskShapeModel model, Services.ScriptVideo.IScriptVideoContext scriptVideoContext, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory, Services.IClipboardService clipboardService, KeyFrameViewModelCollection keyFrameViewModels = null) : base(model, scriptVideoContext, rootUndoObject, undoService, undoChangeFactory, clipboardService, keyFrameViewModels)
        {
            // Set initial interpolated field values.
            if (model.KeyFrames.FirstOrDefault() is RectangleMaskShapeKeyFrameModel firstKeyFrame)
            {
                _lerpedBounds = firstKeyFrame.ToRect();
            }
            else
            {
                throw new ArgumentException("Must have at least one key frame", nameof(model));
            }
        }

        /// <inheritdoc/>
        public override void Flip(Axis axis)
        {
            // Do nothing as rectangle would still have the same dimensions after flipping vertically or horizontally.
        }

        /// <inheritdoc/>
        public override void Lerp(KeyFrameViewModelBase fromKeyFrame, KeyFrameViewModelBase toKeyFrame, double amount)
        {
            Debug.Assert(_activeKeyFrame == null);

            RectangleMaskShapeKeyFrameViewModel fromRectangleMaskShapeFrame = fromKeyFrame as RectangleMaskShapeKeyFrameViewModel;
            RectangleMaskShapeKeyFrameViewModel toRectangleMaskShapeFrame = toKeyFrame as RectangleMaskShapeKeyFrameViewModel;
            Debug.Assert(fromRectangleMaskShapeFrame != null && toRectangleMaskShapeFrame != null);

            Bounds = fromRectangleMaskShapeFrame.Bounds.LerpTo(toRectangleMaskShapeFrame.Bounds, amount);
        }

        /// <inheritdoc/>
        public override void OffsetBounds(Vector offsetVector)
        {
            Rect bounds = Bounds;
            if (!bounds.IsEmpty)
            {
                Bounds = Rect.Offset(bounds, offsetVector);
            }
        }

        /// <inheritdoc/>
        public override void ResizeBounds(Rect newBounds, RectanglePoint? resizeOrigin = null)
        {
            Debug.Assert(_activeKeyFrame != null);

            if (Bounds != newBounds)
            {
                // Prevent multiple Bounds PropertyChanged events being raised.
                _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;

                _undoRoot.BeginChangeSetBatch(ShapeResizedChangeSetDescription, true);

                _activeKeyFrame.Left = newBounds.Left;
                _activeKeyFrame.Top = newBounds.Top;
                _activeKeyFrame.Width = newBounds.Width;
                _activeKeyFrame.Height = newBounds.Height;

                _undoRoot.EndChangeSetBatch();

                _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
                _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

                OnDataPropertyValuesChanged();
            }
        }

        /// <inheritdoc/>
        public override bool SupportsResizeMode(MaskShapeResizeMode resizeMode) => resizeMode == MaskShapeResizeMode.Bounds;

        /// <inheritdoc/>
        public override void AddKeyFrame()
        {
            Debug.Assert(ActiveKeyFrame == null);

            // Batch undo ChangeSet for Model & ViewModel adds so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
            _undoRoot.BeginChangeSetBatch(ShapeKeyFrameAddedChangeSetDescription, false);

            var keyFrameModel = new RectangleMaskShapeKeyFrameModel(ScriptVideoContext.FrameNumber,
                                                                    _lerpedBounds.Left, _lerpedBounds.Top,
                                                                    _lerpedBounds.Width, _lerpedBounds.Height);
            Model.KeyFrames.Add(keyFrameModel);
            KeyFrameViewModels.Add(CreateKeyFrameViewModel(keyFrameModel));

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        protected internal override KeyFrameViewModelBase CreateKeyFrameViewModel(KeyFrameModelBase keyFrameModel)
        {
            return new RectangleMaskShapeKeyFrameViewModel(keyFrameModel as RectangleMaskShapeKeyFrameModel, _rootUndoObject, _undoService, _undoChangeFactory);
        }

        /// <inheritdoc/>
        protected override void OnActiveKeyFrameInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnActiveKeyFrameInstancePropertyChanged(sender, e);

            if (e.PropertyName == nameof(RectangleMaskShapeKeyFrameViewModel.Left) || e.PropertyName == nameof(RectangleMaskShapeKeyFrameViewModel.Top) || e.PropertyName == nameof(RectangleMaskShapeKeyFrameViewModel.Width) || e.PropertyName == nameof(RectangleMaskShapeKeyFrameViewModel.Height))
            {
                OnDataPropertyValuesChanged();
            }
        }

        /// <inheritdoc/>
        protected override void CopyFromKeyFrameModel(KeyFrameModelBase keyFrameModel)
        {
            RectangleMaskShapeKeyFrameModel rectangleKeyFrameModel = keyFrameModel as RectangleMaskShapeKeyFrameModel;
            Debug.Assert(_activeKeyFrame != null && rectangleKeyFrameModel != null);

            // Prevent multiple Bounds PropertyChanged events being raised.
            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;

            _undoRoot.BeginChangeSetBatch(ShapeKeyFrameCopiedChangeSetDescription, false);

            _activeKeyFrame.Left = rectangleKeyFrameModel.Left;
            _activeKeyFrame.Top = rectangleKeyFrameModel.Top;
            _activeKeyFrame.Width = rectangleKeyFrameModel.Width;
            _activeKeyFrame.Height = rectangleKeyFrameModel.Height;

            _undoRoot.EndChangeSetBatch();

            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
            _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

            OnDataPropertyValuesChanged();
        }

        /// <inheritdoc/>
        protected override bool CanCopyKeyFrameViewModelToClipboard(KeyFrameViewModelBase keyFrameViewModel)
        {
            return keyFrameViewModel is RectangleMaskShapeKeyFrameViewModel || _activeKeyFrame != null;
        }

        /// <inheritdoc/>
        protected override void CopyKeyFrameModelToClipboard(KeyFrameModelBase keyFrameModel)
        {
            Debug.Assert(keyFrameModel is RectangleMaskShapeKeyFrameModel);

            _clipboardService.SetData((RectangleMaskShapeKeyFrameModel)keyFrameModel);
        }

        /// <inheritdoc/>
        protected override bool CanPasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel)
        {
            return _clipboardService.ContainsData<RectangleMaskShapeKeyFrameModel>();
        }

        /// <inheritdoc/>
        protected override void PasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel)
        {
            PasteKeyFrameModel(_clipboardService.GetData<RectangleMaskShapeKeyFrameModel>(),
                               targetKeyFrameViewModel);
        }

        /// <inheritdoc/>
        protected override void OnDataPropertyValuesChanged()
        {
            RaiseBoundsPropertyChanged();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as RectangleMaskShapeViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] RectangleMaskShapeViewModel other)
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

            // Let base class check its own fields
            // and do the run-time type comparison.
            return base.Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode());
        }
    }
}
