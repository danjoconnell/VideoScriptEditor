using MonitoredUndo;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Masking.Shapes;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.ViewModels.Masking.Shapes
{
    /// <summary>
    /// View Model for coordinating interaction between a view and a <see cref="PolygonMaskShapeModel">polygon masking shape segment model</see>.
    /// </summary>
    public class PolygonMaskShapeViewModel : MaskShapeViewModelBase, IEquatable<PolygonMaskShapeViewModel>
    {
        private PolygonMaskShapeKeyFrameViewModel _activeKeyFrame = null;

        // Interpolated data fields
        private readonly ObservableCollection<Point> _lerpedPoints;

        /// <inheritdoc cref="MaskShapeViewModelBase.ShapeType"/>
        public override MaskShapeType ShapeType => MaskShapeType.Polygon;

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

                SetProperty(ref _activeKeyFrame, (PolygonMaskShapeKeyFrameViewModel)value, OnActiveKeyFrameChanged);
            }
        }

        /// <inheritdoc cref="PolygonMaskShapeKeyFrameViewModel.Points"/>
        public ObservableCollection<Point> Points => _activeKeyFrame?.Points ?? _lerpedPoints;

        /// <inheritdoc cref="MaskShapeViewModelBase.Bounds"/>
        public override Rect Bounds
        {
            get => PolygonHelpers.GetBounds(Points);
            set
            {
                Rect currentBounds = Bounds;
                if (!value.IsEmpty && !currentBounds.IsEmpty)
                {
                    if (currentBounds.Location != value.Location)
                    {
                        OffsetBounds(value.Location - currentBounds.Location);
                    }

                    if (currentBounds.Size != value.Size)
                    {
                        ResizeBounds(value);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="PolygonMaskShapeViewModel"/> instance.
        /// </summary>
        /// <param name="model">The <see cref="PolygonMaskShapeModel">polygon masking shape segment model</see> providing data for consumption by a view.</param>
        /// <inheritdoc cref="MaskShapeViewModelBase(SegmentModelBase, Services.ScriptVideo.IScriptVideoContext, object, IUndoService, IChangeFactory, Services.IClipboardService, KeyFrameViewModelCollection)"/>
        public PolygonMaskShapeViewModel(PolygonMaskShapeModel model, Services.ScriptVideo.IScriptVideoContext scriptVideoContext, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory, Services.IClipboardService clipboardService, KeyFrameViewModelCollection keyFrameViewModels = null) : base(model, scriptVideoContext, rootUndoObject, undoService, undoChangeFactory, clipboardService, keyFrameViewModels)
        {
            // Set initial interpolated field values.
            if (model.KeyFrames.FirstOrDefault() is PolygonMaskShapeKeyFrameModel firstKeyFrame)
            {
                _lerpedPoints = new ObservableCollection<Point>(
                    firstKeyFrame.Points.Select(pointD => pointD.ToWpfPoint())
                );

                _lerpedPoints.CollectionChanged += OnLerpedPointsCollectionChanged;
            }
            else
            {
                throw new ArgumentException("Must have at least one key frame", nameof(model));
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Inspired by image flipping techniques discussed on Stack Overflow
        /// at https://stackoverflow.com/questions/3005219/how-to-flip-image-in-wpf
        /// </remarks>
        public override void Flip(Axis axis)
        {
            Debug.Assert(CanBeEdited == true);
            Debug.Assert(axis == Axis.X || axis == Axis.Y);

            double xScale, yScale;
            if (axis == Axis.X)
            {
                xScale = -1d;
                yScale = 1d;
            }
            else // axis == Axis.Y
            {
                xScale = 1d;
                yScale = -1d;
            }
            Matrix scalingMatrix = MatrixFactory.CreateScalingMatrix(xScale, yScale, Bounds.CenterPoint());

            Point[] flippedPoints = Points.ToArray();
            scalingMatrix.Transform(flippedPoints);

            // Prevent unnecessarily recalculating Bounds due to PropertyChanged events coming from OnActiveKeyFrameInstancePropertyChanged.
            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;

            _undoRoot.BeginChangeSetBatch($"'{Name}' masking shape flipped", true);

            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = flippedPoints[i];
            }

            _undoRoot.EndChangeSetBatch();

            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
            _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

            RaiseBoundsPropertyChanged();
        }

        /// <inheritdoc/>
        public override void Lerp(KeyFrameViewModelBase fromKeyFrame, KeyFrameViewModelBase toKeyFrame, double amount)
        {
            Debug.Assert(_activeKeyFrame == null);

            PolygonMaskShapeKeyFrameViewModel fromPolygonMaskShapeFrame = fromKeyFrame as PolygonMaskShapeKeyFrameViewModel;
            PolygonMaskShapeKeyFrameViewModel toPolygonMaskShapeFrame = toKeyFrame as PolygonMaskShapeKeyFrameViewModel;
            Debug.Assert(fromPolygonMaskShapeFrame != null && toPolygonMaskShapeFrame != null);

            Debug.Assert(fromPolygonMaskShapeFrame.Points.Count == toPolygonMaskShapeFrame.Points.Count, "The two polygon points counts are NOT equal!");

            _lerpedPoints.CollectionChanged -= OnLerpedPointsCollectionChanged;

            for (int i = 0; i < _lerpedPoints.Count; i++)
            {
                Point fromPoint = fromPolygonMaskShapeFrame.Points[i];
                Point toPoint = toPolygonMaskShapeFrame.Points[i];
                _lerpedPoints[i] = fromPoint.LerpTo(toPoint, amount);
            }

            _lerpedPoints.CollectionChanged -= OnLerpedPointsCollectionChanged;
            _lerpedPoints.CollectionChanged += OnLerpedPointsCollectionChanged;

            RaiseBoundsPropertyChanged();
        }

        /// <inheritdoc/>
        public override void OffsetBounds(Vector offsetVector)
        {
            Debug.Assert(_activeKeyFrame != null);

            // Prevent unnecessarily recalculating Bounds due to PropertyChanged events coming from OnActiveKeyFrameInstancePropertyChanged.
            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;

            _undoRoot.BeginChangeSetBatch($"'{Name}' masking shape offset", true);

            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] += offsetVector;
            }

            _undoRoot.EndChangeSetBatch();

            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
            _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

            RaiseBoundsPropertyChanged();
        }

        /// <inheritdoc/>
        public override void ResizeBounds(Rect newBounds, RectanglePoint? resizeOrigin = null)
        {
            Debug.Assert(_activeKeyFrame != null);

            Rect bounds = Bounds;
            if (newBounds == bounds)
            {
                return;
            }

            double xScaleFactor = newBounds.Width / bounds.Width;
            double yScaleFactor = newBounds.Height / bounds.Height;
            Point centerPoint = resizeOrigin switch
            {
                RectanglePoint.TopLeft => bounds.BottomRight,
                RectanglePoint.TopCenter => bounds.BottomCenter(),
                RectanglePoint.TopRight => bounds.BottomLeft,
                RectanglePoint.CenterLeft => bounds.CenterRight(),
                RectanglePoint.CenterRight => bounds.CenterLeft(),
                RectanglePoint.BottomLeft => bounds.TopRight,
                RectanglePoint.BottomCenter => bounds.TopCenter(),
                _ => bounds.TopLeft,    // default
            };

            Matrix scalingMatrix = MatrixFactory.CreateScalingMatrix(xScaleFactor, yScaleFactor, centerPoint);
            Point[] scaledPoints = Points.ToArray();
            scalingMatrix.Transform(scaledPoints);

            // Prevent unnecessarily recalculating Bounds due to PropertyChanged events coming from OnActiveKeyFrameInstancePropertyChanged.
            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;

            _undoRoot.BeginChangeSetBatch(ShapeResizedChangeSetDescription, true);

            for (int i = 0; i < Points.Count; i++)
            {
                Point scaledPoint = scaledPoints[i];
                Points[i] = new Point(
                    Math.Max(0d, scaledPoint.X),
                    Math.Max(0d, scaledPoint.Y)
                );
            }

            _undoRoot.EndChangeSetBatch();

            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
            _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

            RaiseBoundsPropertyChanged();
        }

        /// <inheritdoc/>
        public override bool SupportsResizeMode(MaskShapeResizeMode resizeMode)
            => resizeMode == MaskShapeResizeMode.Bounds || resizeMode == MaskShapeResizeMode.Points;

        /// <inheritdoc/>
        public override void AddKeyFrame()
        {
            Debug.Assert(ActiveKeyFrame == null);

            // Batch undo ChangeSet for Model & ViewModel adds so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
            _undoRoot.BeginChangeSetBatch(ShapeKeyFrameAddedChangeSetDescription, false);

            var keyFrameModel = new PolygonMaskShapeKeyFrameModel(ScriptVideoContext.FrameNumber,
                                                                  _lerpedPoints.Select(wpfPoint => wpfPoint.ToPointD()).ToList());

            Model.KeyFrames.Add(keyFrameModel);
            KeyFrameViewModels.Add(CreateKeyFrameViewModel(keyFrameModel));

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        protected internal override KeyFrameViewModelBase CreateKeyFrameViewModel(KeyFrameModelBase keyFrameModel)
        {
            return new PolygonMaskShapeKeyFrameViewModel(keyFrameModel as PolygonMaskShapeKeyFrameModel, _rootUndoObject, _undoService, _undoChangeFactory);
        }

        /// <inheritdoc/>
        protected override void OnActiveKeyFrameInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnActiveKeyFrameInstancePropertyChanged(sender, e);

            if (e.PropertyName == nameof(PolygonMaskShapeKeyFrameViewModel.Points))
            {
                OnDataPropertyValuesChanged();
            }
        }

        /// <inheritdoc/>
        protected override void CopyFromKeyFrameModel(KeyFrameModelBase keyFrameModel)
        {
            PolygonMaskShapeKeyFrameModel polygonKeyFrameModel = keyFrameModel as PolygonMaskShapeKeyFrameModel;
            Debug.Assert(_activeKeyFrame != null && polygonKeyFrameModel != null);
            Debug.Assert(polygonKeyFrameModel.Points.Count == _activeKeyFrame.Points.Count);

            // Prevent unnecessarily recalculating Bounds due to PropertyChanged events coming from OnActiveKeyFrameInstancePropertyChanged.
            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;

            _undoRoot.BeginChangeSetBatch(ShapeKeyFrameCopiedChangeSetDescription, false);

            for (int i = 0; i < _activeKeyFrame.Points.Count; i++)
            {
                if (!_activeKeyFrame.Points[i].IsEqualTo(polygonKeyFrameModel.Points[i]))
                {
                    _activeKeyFrame.Points[i] = polygonKeyFrameModel.Points[i].ToWpfPoint();
                }
            }

            _undoRoot.EndChangeSetBatch();

            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
            _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

            RaiseBoundsPropertyChanged();
        }

        /// <inheritdoc/>
        protected override bool CanCopyKeyFrameViewModelToClipboard(KeyFrameViewModelBase keyFrameViewModel)
        {
            return keyFrameViewModel is PolygonMaskShapeKeyFrameViewModel || _activeKeyFrame != null;
        }

        /// <inheritdoc/>
        protected override void CopyKeyFrameModelToClipboard(KeyFrameModelBase keyFrameModel)
        {
            Debug.Assert(keyFrameModel is PolygonMaskShapeKeyFrameModel);

            _clipboardService.SetData((PolygonMaskShapeKeyFrameModel)keyFrameModel);
        }

        /// <inheritdoc/>
        protected override bool CanPasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel)
        {
            return _clipboardService.ContainsData<PolygonMaskShapeKeyFrameModel>();
        }

        /// <inheritdoc/>
        protected override void PasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel)
        {
            PasteKeyFrameModel(_clipboardService.GetData<PolygonMaskShapeKeyFrameModel>(),
                               targetKeyFrameViewModel);
        }

        /// <inheritdoc/>
        protected override void OnDataPropertyValuesChanged()
        {
            RaisePropertyChanged(nameof(Points));
            RaiseBoundsPropertyChanged();
        }

        /// <summary>
        /// Invoked whenever the <see cref="_lerpedPoints"/> collection changes.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        private void OnLerpedPointsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(sender == _lerpedPoints); // Catch memory/event handler registration leaks.

            RaiseBoundsPropertyChanged();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as PolygonMaskShapeViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] PolygonMaskShapeViewModel other)
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
