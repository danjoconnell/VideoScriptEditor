using MonitoredUndo;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.Extensions;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Masking.Shapes;
using VideoScriptEditor.ViewModels.Timeline;
using System.Diagnostics.CodeAnalysis;

namespace VideoScriptEditor.ViewModels.Masking.Shapes
{
    /// <summary>
    /// View Model for coordinating interaction between a view and an <see cref="EllipseMaskShapeModel">ellipse masking shape segment model</see>.
    /// </summary>
    public class EllipseMaskShapeViewModel : MaskShapeViewModelBase, IEquatable<EllipseMaskShapeViewModel>
    {
        private EllipseMaskShapeKeyFrameViewModel _activeKeyFrame = null;

        // Interpolated data fields
        private Point _lerpedCenterPoint;
        private double _lerpedRadiusX;
        private double _lerpedRadiusY;

        /// <inheritdoc cref="MaskShapeViewModelBase.ShapeType"/>
        public override MaskShapeType ShapeType => MaskShapeType.Ellipse;

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

                SetProperty(ref _activeKeyFrame, (EllipseMaskShapeKeyFrameViewModel)value, OnActiveKeyFrameChanged);
            }
        }

        /// <inheritdoc cref="MaskShapeViewModelBase.Bounds"/>
        public override Rect Bounds
        {
            get => Ellipse.CalculateBounds(CenterPoint, RadiusX, RadiusY);
            set
            {
                if (!value.IsEmpty)
                {
                    ResizeBounds(value);
                }
            }
        }

        /// <inheritdoc cref="EllipseMaskShapeKeyFrameViewModel.CenterPoint"/>
        public Point CenterPoint
        {
            get => _activeKeyFrame?.CenterPoint ?? _lerpedCenterPoint;
            set
            {
                if (_activeKeyFrame != null)
                {
                    _activeKeyFrame.CenterPoint = value;
                }
                else
                {
                    if (SetProperty(ref _lerpedCenterPoint, value))
                    {
                        RaiseBoundsPropertyChanged();
                    }
                }
            }
        }

        /// <inheritdoc cref="EllipseMaskShapeKeyFrameViewModel.RadiusX"/>
        public double RadiusX
        {
            get => _activeKeyFrame?.RadiusX ?? _lerpedRadiusX;
            set
            {
                if (_activeKeyFrame != null)
                {
                    _activeKeyFrame.RadiusX = value;
                }
                else
                {
                    if (SetProperty(ref _lerpedRadiusX, value))
                    {
                        RaiseBoundsPropertyChanged();
                    }
                }
            }
        }

        /// <inheritdoc cref="EllipseMaskShapeKeyFrameViewModel.RadiusY"/>
        public double RadiusY
        {
            get => _activeKeyFrame?.RadiusY ?? _lerpedRadiusY;
            set
            {
                if (_activeKeyFrame != null)
                {
                    _activeKeyFrame.RadiusY = value;
                }
                else
                {
                    if (SetProperty(ref _lerpedRadiusY, value))
                    {
                        RaiseBoundsPropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="EllipseMaskShapeViewModel"/> instance.
        /// </summary>
        /// <param name="model">The <see cref="EllipseMaskShapeModel">ellipse masking shape segment model</see> providing data for consumption by a view.</param>
        /// <inheritdoc cref="MaskShapeViewModelBase(SegmentModelBase, Services.ScriptVideo.IScriptVideoContext, object, IUndoService, IChangeFactory, Services.IClipboardService, KeyFrameViewModelCollection)"/>
        public EllipseMaskShapeViewModel(EllipseMaskShapeModel model, Services.ScriptVideo.IScriptVideoContext scriptVideoContext, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory, Services.IClipboardService clipboardService, KeyFrameViewModelCollection keyFrameViewModels = null) : base(model, scriptVideoContext, rootUndoObject, undoService, undoChangeFactory, clipboardService, keyFrameViewModels)
        {
            // Set initial interpolated field values.
            if (model.KeyFrames.FirstOrDefault() is EllipseMaskShapeKeyFrameModel firstKeyFrame)
            {
                _lerpedCenterPoint = firstKeyFrame.CenterPoint.ToWpfPoint();
                _lerpedRadiusX = firstKeyFrame.RadiusX;
                _lerpedRadiusY = firstKeyFrame.RadiusY;
            }
            else
            {
                throw new ArgumentException("Must have at least one key frame", nameof(model));
            }
        }

        /// <inheritdoc/>
        public override void Flip(Axis axis)
        {
            // Do nothing as ellipse would still have the same dimensions after flipping vertically or horizontally.
        }

        /// <inheritdoc/>
        public override void Lerp(KeyFrameViewModelBase fromKeyFrame, KeyFrameViewModelBase toKeyFrame, double amount)
        {
            Debug.Assert(_activeKeyFrame == null);

            EllipseMaskShapeKeyFrameViewModel fromEllipseMaskShapeFrame = fromKeyFrame as EllipseMaskShapeKeyFrameViewModel;
            EllipseMaskShapeKeyFrameViewModel toEllipseMaskShapeFrame = toKeyFrame as EllipseMaskShapeKeyFrameViewModel;
            Debug.Assert(fromEllipseMaskShapeFrame != null && toEllipseMaskShapeFrame != null);

            _lerpedCenterPoint = fromEllipseMaskShapeFrame.CenterPoint.LerpTo(toEllipseMaskShapeFrame.CenterPoint, amount);
            _lerpedRadiusX = fromEllipseMaskShapeFrame.RadiusX.LerpTo(toEllipseMaskShapeFrame.RadiusX, amount);
            _lerpedRadiusY = fromEllipseMaskShapeFrame.RadiusY.LerpTo(toEllipseMaskShapeFrame.RadiusY, amount);

            OnDataPropertyValuesChanged();
        }

        /// <inheritdoc/>
        public override void OffsetBounds(Vector offsetVector)
        {
            CenterPoint += offsetVector;
        }

        /// <inheritdoc/>
        public override void ResizeBounds(Rect newBounds, RectanglePoint? resizeOrigin = null)
        {
            Debug.Assert(_activeKeyFrame != null);

            Rect bounds = Bounds;
            if (bounds != newBounds)
            {
                Ellipse ellipse = new Ellipse(newBounds);

                // Prevent unnecessarily recalculating Bounds due to PropertyChanged events coming from OnActiveKeyFrameInstancePropertyChanged.
                _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;

                _undoRoot.BeginChangeSetBatch(ShapeResizedChangeSetDescription, true);

                _activeKeyFrame.CenterPoint = ellipse.CenterPoint;
                _activeKeyFrame.RadiusX = ellipse.RadiusX;
                _activeKeyFrame.RadiusY = ellipse.RadiusY;

                _undoRoot.EndChangeSetBatch();

                _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
                _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

                OnDataPropertyValuesChanged();
            }
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

            var keyFrameModel = new EllipseMaskShapeKeyFrameModel(ScriptVideoContext.FrameNumber,
                                                                  CenterPoint.ToPointD(), RadiusX, RadiusY);
            Model.KeyFrames.Add(keyFrameModel);
            KeyFrameViewModels.Add(CreateKeyFrameViewModel(keyFrameModel));

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        protected internal override KeyFrameViewModelBase CreateKeyFrameViewModel(KeyFrameModelBase keyFrameModel)
        {
            return new EllipseMaskShapeKeyFrameViewModel(keyFrameModel as EllipseMaskShapeKeyFrameModel, _rootUndoObject, _undoService, _undoChangeFactory);
        }

        /// <inheritdoc/>
        protected override void OnActiveKeyFrameInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnActiveKeyFrameInstancePropertyChanged(sender, e);

            if (e.PropertyName == nameof(EllipseMaskShapeKeyFrameViewModel.CenterPoint) || e.PropertyName == nameof(EllipseMaskShapeKeyFrameViewModel.RadiusX) || e.PropertyName == nameof(EllipseMaskShapeKeyFrameViewModel.RadiusY))
            {
                RaisePropertyChanged(e.PropertyName);
                RaiseBoundsPropertyChanged();
            }
        }

        /// <inheritdoc/>
        protected override void CopyFromKeyFrameModel(KeyFrameModelBase keyFrameModel)
        {
            EllipseMaskShapeKeyFrameModel ellipseKeyFrameModel = keyFrameModel as EllipseMaskShapeKeyFrameModel;
            Debug.Assert(_activeKeyFrame != null && ellipseKeyFrameModel != null);

            // Prevent unnecessarily recalculating Bounds due to PropertyChanged events coming from OnActiveKeyFrameInstancePropertyChanged.
            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
            
            _undoRoot.BeginChangeSetBatch(ShapeKeyFrameCopiedChangeSetDescription, false);

            _activeKeyFrame.CenterPoint = ellipseKeyFrameModel.CenterPoint.ToWpfPoint();
            _activeKeyFrame.RadiusX = ellipseKeyFrameModel.RadiusX;
            _activeKeyFrame.RadiusY = ellipseKeyFrameModel.RadiusY;

            _undoRoot.EndChangeSetBatch();

            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
            _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

            OnDataPropertyValuesChanged();
        }

        /// <inheritdoc/>
        protected override bool CanCopyKeyFrameViewModelToClipboard(KeyFrameViewModelBase keyFrameViewModel)
        {
            return keyFrameViewModel is EllipseMaskShapeKeyFrameViewModel || _activeKeyFrame != null;
        }

        /// <inheritdoc/>
        protected override void CopyKeyFrameModelToClipboard(KeyFrameModelBase keyFrameModel)
        {
            Debug.Assert(keyFrameModel is EllipseMaskShapeKeyFrameModel);

            _clipboardService.SetData((EllipseMaskShapeKeyFrameModel)keyFrameModel);
        }

        /// <inheritdoc/>
        protected override bool CanPasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel)
        {
            return _clipboardService.ContainsData<EllipseMaskShapeKeyFrameModel>();
        }

        /// <inheritdoc/>
        protected override void PasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel)
        {
            PasteKeyFrameModel(_clipboardService.GetData<EllipseMaskShapeKeyFrameModel>(),
                               targetKeyFrameViewModel);
        }

        /// <inheritdoc/>
        protected override void OnDataPropertyValuesChanged()
        {
            RaisePropertyChanged(nameof(CenterPoint));
            RaisePropertyChanged(nameof(RadiusX));
            RaisePropertyChanged(nameof(RadiusY));
            RaiseBoundsPropertyChanged();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as EllipseMaskShapeViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] EllipseMaskShapeViewModel other)
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
