using MonitoredUndo;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using VideoScriptEditor.Collections;
using VideoScriptEditor.Extensions;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.Models;
using VideoScriptEditor.Models.Cropping;
using VideoScriptEditor.Services.ScriptVideo;
using VideoScriptEditor.ViewModels.Timeline;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.ViewModels.Cropping
{
    /// <summary>
    /// View Model for coordinating interaction between a view and a <see cref="CropSegmentModel">crop segment model</see>.
    /// </summary>
    public class CropSegmentViewModel : SegmentViewModelBase, IEquatable<CropSegmentViewModel>
    {
        private CropKeyFrameViewModel _activeKeyFrame = null;

        // Visual pre-rotation crop area coordinates
        private RectanglePolygon _visualRect;

        // Interpolated post-rotation data fields
        private double _lerpedWidth = 0d;
        private double _lerpedHeight = 0d;
        private double _lerpedAngle = 0d;
        private double _lerpedDataLeft = 0d;
        private double _lerpedDataTop = 0d;

        private Size VideoFrameSize => ScriptVideoContext.VideoFrameSize.ToWpfSize();
        private Rect VideoFrameBounds => new Rect(VideoFrameSize);

        /// <summary>
        /// Absolute value of Angle rounded to correct floating-point precision errors
        /// such as when a 90 degree Angle value is expected, the actual Angle value can be a tiny fraction above or below 90.0.
        /// </summary>
        private double AbsoluteAngle => Math.Round(Math.Abs(Angle), MathExtensions.FloatingPointPrecision);

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

                SetProperty(ref _activeKeyFrame, (CropKeyFrameViewModel)value, OnActiveKeyFrameChanged);
            }
        }

        /// <summary>
        /// The visual pre-rotation top-left pixel coordinate of the area to crop.
        /// </summary>
        public Point VisualTopLeft
        {
            get => _visualRect.TopLeft;
            set => SetVisualCornerPropertyValue(RectangleCorner.TopLeft, value);
        }

        /// <summary>
        /// The visual pre-rotation top-right pixel coordinate of the area to crop.
        /// </summary>
        public Point VisualTopRight
        {
            get => _visualRect.TopRight;
            set => SetVisualCornerPropertyValue(RectangleCorner.TopRight, value);
        }

        /// <summary>
        /// The visual pre-rotation bottom-left pixel coordinate of the area to crop.
        /// </summary>
        public Point VisualBottomLeft
        {
            get => _visualRect.BottomLeft;
            set => SetVisualCornerPropertyValue(RectangleCorner.BottomLeft, value);
        }

        /// <summary>
        /// The visual pre-rotation bottom-right pixel coordinate of the area to crop.
        /// </summary>
        public Point VisualBottomRight
        {
            get => _visualRect.BottomRight;
            set => SetVisualCornerPropertyValue(RectangleCorner.BottomRight, value);
        }

        /// <inheritdoc cref="CropKeyFrameViewModel.Width"/>
        public double Width
        {
            get => _activeKeyFrame?.Width ?? _lerpedWidth;
            set
            {
                if (_activeKeyFrame != null)
                {
                    if (AbsoluteAngle == 0d)
                    {
                        _activeKeyFrame.Width = value;
                    }
                    else
                    {
                        RectanglePolygon oldVisualRect = _visualRect;

                        Line rightEdge = _visualRect.RightEdge.Offset(value - _visualRect.Width);
                        _visualRect = new RectanglePolygon(_visualRect.TopLeft, rightEdge.StartPoint, rightEdge.EndPoint, _visualRect.BottomLeft);

                        Rect dataRect = _visualRect.ToDerotatedAxisAlignedRect();
                        if (Width != dataRect.Width)
                        {
                            BatchSetActiveKeyFrameProperties(dataRect, _visualRect.Angle,
                                                             undoChangeSetDescription: $"'{Name}' segment Width changed");
                        }

                        OnVisualRectChanged(oldVisualRect);
                    }
                }
                else
                {
                    SetProperty(ref _lerpedWidth, value);
                }
            }
        }

        /// <inheritdoc cref="CropKeyFrameViewModel.Height"/>
        public double Height
        {
            get => _activeKeyFrame?.Height ?? _lerpedHeight;
            set
            {
                if (_activeKeyFrame != null)
                {
                    if (AbsoluteAngle == 0d)
                    {
                        _activeKeyFrame.Height = value;
                    }
                    else
                    {
                        RectanglePolygon oldVisualRect = _visualRect;

                        Line bottomEdge = _visualRect.BottomEdge.Offset(value - _visualRect.Height);
                        _visualRect = new RectanglePolygon(_visualRect.TopLeft, _visualRect.TopRight, bottomEdge.StartPoint, bottomEdge.EndPoint);

                        Rect dataRect = _visualRect.ToDerotatedAxisAlignedRect();
                        if (Height != dataRect.Height)
                        {
                            BatchSetActiveKeyFrameProperties(dataRect, _visualRect.Angle,
                                                             undoChangeSetDescription: $"'{Name}' segment Height changed");
                        }

                        OnVisualRectChanged(oldVisualRect);
                    }
                }
                else
                {
                    SetProperty(ref _lerpedHeight, value);
                }
            }
        }

        /// <inheritdoc cref="CropKeyFrameViewModel.Angle"/>
        public double Angle
        {
            get => _activeKeyFrame?.Angle ?? _lerpedAngle;
            set
            {
                if (_activeKeyFrame != null)
                {
                    _activeKeyFrame.Angle = value;
                }
                else
                {
                    SetProperty(ref _lerpedAngle, value);
                }
            }
        }

        /// <inheritdoc cref="CropKeyFrameViewModel.Center"/>
        public Point Center
        {
            get => _activeKeyFrame?.Center ?? _visualRect.Center;
            set
            {
                if (_activeKeyFrame != null)
                {
                    double absAngle = AbsoluteAngle;
                    if (absAngle == 0d || absAngle == 90d || absAngle == 180d)
                    {
                        Rect visualRectBounds = _visualRect.Bounds;
                        Rect offsetBounds = visualRectBounds.OffsetFromCenterWithinBounds(value - Center, VideoFrameBounds);

                        _activeKeyFrame.Center = offsetBounds.CenterPoint();
                    }
                    else
                    {
                        _activeKeyFrame.Center = value;
                    }
                }
            }
        }

        /// <summary>
        /// The serialized data value representing the post-rotation left pixel coordinate of the area to crop.
        /// </summary>
        public double DataLeft
        {
            get => _activeKeyFrame?.Left ?? _lerpedDataLeft;
            set
            {
                if (_activeKeyFrame != null)
                {
                    _activeKeyFrame.Left = value;
                }
                else
                {
                    SetProperty(ref _lerpedDataLeft, value);
                }
            }
        }

        /// <summary>
        /// The serialized data value representing the post-rotation top pixel coordinate of the area to crop.
        /// </summary>
        public double DataTop
        {
            get => _activeKeyFrame?.Top ?? _lerpedDataTop;
            set
            {
                if (_activeKeyFrame != null)
                {
                    _activeKeyFrame.Top = value;
                }
                else
                {
                    SetProperty(ref _lerpedDataTop, value);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="CropSegmentViewModel"/> instance.
        /// </summary>
        /// <param name="model">The <see cref="CropSegmentModel">crop segment model</see> providing data for consumption by a view.</param>
        /// <inheritdoc cref="SegmentViewModelBase(SegmentModelBase, IScriptVideoContext, object, IUndoService, IChangeFactory, Services.IClipboardService, KeyFrameViewModelCollection)"/>
        public CropSegmentViewModel(CropSegmentModel model, IScriptVideoContext scriptVideoContext, object rootUndoObject, IUndoService undoService, IChangeFactory undoChangeFactory, Services.IClipboardService clipboardService, KeyFrameViewModelCollection keyFrameViewModels = null) : base(model, scriptVideoContext, rootUndoObject, undoService, undoChangeFactory, clipboardService, keyFrameViewModels)
        {
            // Set initial interpolated field values.
            if (model.KeyFrames.FirstOrDefault() is CropKeyFrameModel firstKeyFrame)
            {
                _lerpedAngle = firstKeyFrame.Angle;
                _lerpedDataLeft = firstKeyFrame.Left;
                _lerpedDataTop = firstKeyFrame.Top;
                _lerpedWidth = firstKeyFrame.Width;
                _lerpedHeight = firstKeyFrame.Height;

                _visualRect = new RectanglePolygon(firstKeyFrame.BaseRect(), _lerpedAngle);
            }
            else
            {
                throw new ArgumentException("Must have at least one key frame", nameof(model));
            }
        }

        /// <inheritdoc/>
        public override void AddKeyFrame()
        {
            Debug.Assert(ActiveKeyFrame == null);

            // Batch undo ChangeSet for Model & ViewModel adds so that any subsequent property change undo/redo won't be performed on orphaned ViewModels (Undo references an existing ViewModel instance in property ChangeSets)
            _undoRoot.BeginChangeSetBatch($"'{Name}' segment key frame added", false);

            CropKeyFrameModel keyFrameModel = new CropKeyFrameModel(ScriptVideoContext.FrameNumber, DataLeft, DataTop, Width, Height, Angle);

            Model.KeyFrames.Add(keyFrameModel);
            KeyFrameViewModels.Add(CreateKeyFrameViewModel(keyFrameModel));

            _undoRoot.EndChangeSetBatch();
        }

        /// <inheritdoc/>
        protected override void CopyFromKeyFrameModel(KeyFrameModelBase keyFrameModel)
        {
            Debug.Assert(_activeKeyFrame != null);

            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
            _activeKeyFrame.CopyFromModel(keyFrameModel);
            _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;

            RectanglePolygon oldVisualRect = _visualRect;
            _visualRect = new RectanglePolygon(_activeKeyFrame.Rect, _activeKeyFrame.Angle);
            OnVisualRectChanged(oldVisualRect);

            RaisePropertyChanged(nameof(DataLeft));
            RaisePropertyChanged(nameof(DataTop));
        }

        /// <inheritdoc/>
        protected override bool CanCopyKeyFrameViewModelToClipboard(KeyFrameViewModelBase keyFrameViewModel)
        {
            return keyFrameViewModel is CropKeyFrameViewModel || _activeKeyFrame != null;
        }

        /// <inheritdoc/>
        protected override void CopyKeyFrameModelToClipboard(KeyFrameModelBase keyFrameModel)
        {
            Debug.Assert(keyFrameModel is CropKeyFrameModel);

            _clipboardService.SetData((CropKeyFrameModel)keyFrameModel);
        }

        /// <inheritdoc/>
        protected override bool CanPasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel)
        {
            return _clipboardService.ContainsData<CropKeyFrameModel>();
        }

        /// <inheritdoc/>
        protected override void PasteKeyFrameFromClipboard(KeyFrameViewModelBase targetKeyFrameViewModel)
        {
            PasteKeyFrameModel(_clipboardService.GetData<CropKeyFrameModel>(),
                               targetKeyFrameViewModel);
        }

        /// <inheritdoc/>
        public override void Lerp(KeyFrameViewModelBase fromKeyFrame, KeyFrameViewModelBase toKeyFrame, double amount)
        {
            Debug.Assert(_activeKeyFrame == null);

            CropKeyFrameViewModel fromCropKeyFrame = fromKeyFrame as CropKeyFrameViewModel;
            CropKeyFrameViewModel toCropKeyFrame = toKeyFrame as CropKeyFrameViewModel;
            Debug.Assert(fromCropKeyFrame != null && toCropKeyFrame != null);

            double lerpedAngle = fromCropKeyFrame.Angle != toCropKeyFrame.Angle ? fromCropKeyFrame.Angle.LerpTo(toCropKeyFrame.Angle, amount) : fromCropKeyFrame.Angle;
            Rect dataRect = fromCropKeyFrame.Rect.LerpTo(toCropKeyFrame.Rect, amount);

            RectanglePolygon oldVisualRect = _visualRect;
            _visualRect = new RectanglePolygon(dataRect, lerpedAngle);

            DataLeft = dataRect.Left;
            DataTop = dataRect.Top;
            Width = dataRect.Width;
            Height = dataRect.Height;
            Angle = lerpedAngle;

            OnVisualRectChanged(oldVisualRect);
        }

        /// <summary>
        /// Begins an undoable action making <see cref="UndoRoot.BeginChangeSetBatch(string, bool)">batch changes</see>
        /// to the value of the <see cref="Center"/> property.
        /// </summary>
        public void BeginBatchChangeCenterAction()
        {
            _undoRoot.BeginChangeSetBatch($"'{Name}' segment center point changed", true);
        }

        /// <summary>
        /// Begins an undoable action making <see cref="UndoRoot.BeginChangeSetBatch(string, bool)">batch changes</see>
        /// to the size of the area to be cropped.
        /// </summary>
        public void BeginBatchResizeAction()
        {
            _undoRoot.BeginChangeSetBatch($"'{Name}' segment crop area resized", true);
        }

        /// <summary>
        /// Begins an undoable action making <see cref="UndoRoot.BeginChangeSetBatch(string, bool)">batch changes</see>
        /// to the value of the <see cref="Angle"/> property.
        /// </summary>
        public void BeginBatchRotationAction()
        {
            _undoRoot.BeginChangeSetBatch($"'{Name}' segment crop area rotated", true);
        }

        /// <summary>
        /// Resizes the area to be cropped by offsetting a rectangular visual selection point,
        /// optionally using a scaled or symmetrical resize method.
        /// </summary>
        /// <param name="selectionPoint">The rectangular visual selection point.</param>
        /// <param name="selectionPointOffset">The pixel offset to apply to the visual selection point.</param>
        /// <param name="scaledOrSymmetricResize">Whether to use a scaled or symmetrical resize method.</param>
        public void ResizeFromVisualSelectionPointOffset(RectanglePoint selectionPoint, Vector selectionPointOffset, bool scaledOrSymmetricResize)
        {
            if (!CanBeEdited)
                return;

            Rect dataRect;
            double newAngle;
            RectanglePolygon oldVisualRect = _visualRect;

            double absAngle = AbsoluteAngle;
            if (absAngle == 0d)
            {
                Point dataTopLeft = new Point(_activeKeyFrame.Left, _activeKeyFrame.Top);
                Point dataBottomRight = new Point(_activeKeyFrame.Left + _activeKeyFrame.Width, _activeKeyFrame.Top + _activeKeyFrame.Height);

                switch (selectionPoint)
                {
                    case RectanglePoint.TopLeft:
                        dataTopLeft.X += selectionPointOffset.X;
                        dataTopLeft.Y += scaledOrSymmetricResize ? selectionPointOffset.X : selectionPointOffset.Y;
                        break;
                    case RectanglePoint.TopCenter:
                        dataTopLeft.Y += selectionPointOffset.Y;
                        if (scaledOrSymmetricResize)
                        {
                            dataBottomRight.Y -= selectionPointOffset.Y;
                        }
                        break;
                    case RectanglePoint.TopRight:
                        dataTopLeft.Y += scaledOrSymmetricResize ? -selectionPointOffset.X : selectionPointOffset.Y;
                        dataBottomRight.X += selectionPointOffset.X;
                        break;
                    case RectanglePoint.CenterLeft:
                        dataTopLeft.X += selectionPointOffset.X;
                        if (scaledOrSymmetricResize)
                        {
                            dataBottomRight.X -= selectionPointOffset.X;
                        }
                        break;
                    case RectanglePoint.CenterRight:
                        if (scaledOrSymmetricResize)
                        {
                            dataTopLeft.X -= selectionPointOffset.X;
                        }
                        dataBottomRight.X += selectionPointOffset.X;
                        break;
                    case RectanglePoint.BottomLeft:
                        dataTopLeft.X += selectionPointOffset.X;
                        dataBottomRight.Y += scaledOrSymmetricResize ? -selectionPointOffset.X : selectionPointOffset.Y;
                        break;
                    case RectanglePoint.BottomCenter:
                        if (scaledOrSymmetricResize)
                        {
                            dataTopLeft.Y -= selectionPointOffset.Y;
                        }
                        dataBottomRight.Y += selectionPointOffset.Y;
                        break;
                    case RectanglePoint.BottomRight:
                        dataBottomRight.X += selectionPointOffset.X;
                        dataBottomRight.Y += scaledOrSymmetricResize ? selectionPointOffset.X : selectionPointOffset.Y;
                        break;
                    default:
                        throw new InvalidEnumArgumentException(nameof(selectionPoint));
                }

                dataRect = new Rect(dataTopLeft.ClampToBounds(VideoFrameSize.Width, VideoFrameSize.Height),
                                    dataBottomRight.ClampToBounds(VideoFrameSize.Width, VideoFrameSize.Height));
                newAngle = absAngle;

                _visualRect = new RectanglePolygon(dataRect, newAngle);
            }
            else
            {
                bool constrainToContainerBounds = absAngle == 90d || absAngle == 180d;  // Already checked for zero degree angle
                Point newVisualTopLeft = VisualTopLeft;
                Point newVisualTopRight = VisualTopRight;
                Point newVisualBottomRight = VisualBottomRight;
                Point newVisualBottomLeft = VisualBottomLeft;
                Point centerPoint = Center;
                Point offsetPoint;

                Line offsetEdge, scaleDiagonalLine;

                switch (selectionPoint)
                {
                    case RectanglePoint.TopLeft:
                        newVisualTopLeft = VisualTopLeft + selectionPointOffset;
                        if (scaledOrSymmetricResize)
                        {
                            scaleDiagonalLine = new Line(VisualTopLeft, Center);
                            newVisualTopLeft = scaleDiagonalLine.Project(newVisualTopLeft);
                        }
                        VisualTopLeft = newVisualTopLeft;
                        return;
                    case RectanglePoint.TopRight:
                        newVisualTopRight = VisualTopRight + selectionPointOffset;
                        if (scaledOrSymmetricResize)
                        {
                            scaleDiagonalLine = new Line(VisualTopRight, Center);
                            newVisualTopRight = scaleDiagonalLine.Project(newVisualTopRight);
                        }
                        VisualTopRight = newVisualTopRight;
                        return;
                    case RectanglePoint.BottomLeft:
                        newVisualBottomLeft = VisualBottomLeft + selectionPointOffset;
                        if (scaledOrSymmetricResize)
                        {
                            scaleDiagonalLine = new Line(VisualBottomLeft, Center);
                            newVisualBottomLeft = scaleDiagonalLine.Project(newVisualBottomLeft);
                        }
                        VisualBottomLeft = newVisualBottomLeft;
                        return;
                    case RectanglePoint.BottomRight:
                        newVisualBottomRight = VisualBottomRight + selectionPointOffset;
                        if (scaledOrSymmetricResize)
                        {
                            scaleDiagonalLine = new Line(VisualBottomRight, Center);
                            newVisualBottomRight = scaleDiagonalLine.Project(newVisualBottomRight);
                        }
                        VisualBottomRight = newVisualBottomRight;
                        return;
                    case RectanglePoint.TopCenter:
                        Point topCenter = _visualRect.TopCenter + selectionPointOffset;
                        if (constrainToContainerBounds)
                        {
                            topCenter = VideoFrameBounds.ConstrainPoint(topCenter);
                        }
                        topCenter = _visualRect.ClosestPerpendicularPointToEdgeMidPoint(topCenter, RectangleSide.Top);
                        double topDistance = _visualRect.DistanceFromPerpendicularPointToEdgeMidPoint(topCenter, RectangleSide.Top);

                        if (scaledOrSymmetricResize)
                        {
                            if (constrainToContainerBounds)
                            {
                                offsetPoint = _visualRect.BottomCenter.WithOffset(_visualRect.BottomEdge.PerpendicularDirection, topDistance);
                                offsetPoint = VideoFrameBounds.ConstrainPoint(offsetPoint);
                                topDistance = _visualRect.BottomCenter.DistanceTo(offsetPoint);
                                topDistance = (centerPoint.DistanceTo(topCenter) > centerPoint.DistanceTo(_visualRect.TopCenter)) ? topDistance : -topDistance;
                            }

                            offsetEdge = _visualRect.BottomEdge.Offset(topDistance);
                            newVisualBottomRight = offsetEdge.StartPoint;
                            newVisualBottomLeft = offsetEdge.EndPoint;
                        }

                        offsetEdge = _visualRect.TopEdge.Offset(topDistance);
                        newVisualTopLeft = offsetEdge.StartPoint;
                        newVisualTopRight = offsetEdge.EndPoint;
                        break;
                    case RectanglePoint.CenterLeft:
                        Point centerLeft = _visualRect.CenterLeft + selectionPointOffset;
                        if (constrainToContainerBounds)
                        {
                            centerLeft = VideoFrameBounds.ConstrainPoint(centerLeft);
                        }
                        centerLeft = _visualRect.ClosestPerpendicularPointToEdgeMidPoint(centerLeft, RectangleSide.Left);
                        double leftDistance = _visualRect.DistanceFromPerpendicularPointToEdgeMidPoint(centerLeft, RectangleSide.Left);

                        if (scaledOrSymmetricResize)
                        {
                            if (constrainToContainerBounds)
                            {
                                offsetPoint = _visualRect.CenterRight.WithOffset(_visualRect.RightEdge.PerpendicularDirection, leftDistance);
                                offsetPoint = VideoFrameBounds.ConstrainPoint(offsetPoint);
                                leftDistance = _visualRect.CenterRight.DistanceTo(offsetPoint);
                                leftDistance = (centerPoint.DistanceTo(centerLeft) > centerPoint.DistanceTo(_visualRect.CenterLeft)) ? leftDistance : -leftDistance;
                            }

                            offsetEdge = _visualRect.RightEdge.Offset(leftDistance);
                            newVisualTopRight = offsetEdge.StartPoint;
                            newVisualBottomRight = offsetEdge.EndPoint;
                        }

                        offsetEdge = _visualRect.LeftEdge.Offset(leftDistance);
                        newVisualBottomLeft = offsetEdge.StartPoint;
                        newVisualTopLeft = offsetEdge.EndPoint;
                        break;
                    case RectanglePoint.CenterRight:
                        Point centerRight = _visualRect.CenterRight + selectionPointOffset;
                        if (constrainToContainerBounds)
                        {
                            centerRight = VideoFrameBounds.ConstrainPoint(centerRight);
                        }
                        centerRight = _visualRect.ClosestPerpendicularPointToEdgeMidPoint(centerRight, RectangleSide.Right);
                        double rightDistance = _visualRect.DistanceFromPerpendicularPointToEdgeMidPoint(centerRight, RectangleSide.Right);

                        if (scaledOrSymmetricResize)
                        {
                            if (constrainToContainerBounds)
                            {
                                offsetPoint = _visualRect.CenterLeft.WithOffset(_visualRect.LeftEdge.PerpendicularDirection, rightDistance);
                                offsetPoint = VideoFrameBounds.ConstrainPoint(offsetPoint);
                                rightDistance = _visualRect.CenterLeft.DistanceTo(offsetPoint);
                                rightDistance = (centerPoint.DistanceTo(centerRight) > centerPoint.DistanceTo(_visualRect.CenterRight)) ? rightDistance : -rightDistance;
                            }

                            offsetEdge = _visualRect.LeftEdge.Offset(rightDistance);
                            newVisualBottomLeft = offsetEdge.StartPoint;
                            newVisualTopLeft = offsetEdge.EndPoint;
                        }

                        offsetEdge = _visualRect.RightEdge.Offset(rightDistance);
                        newVisualTopRight = offsetEdge.StartPoint;
                        newVisualBottomRight = offsetEdge.EndPoint;
                        break;
                    case RectanglePoint.BottomCenter:
                        Point bottomCenter = _visualRect.BottomCenter + selectionPointOffset;
                        if (constrainToContainerBounds)
                        {
                            bottomCenter = VideoFrameBounds.ConstrainPoint(bottomCenter);
                        }
                        bottomCenter = _visualRect.ClosestPerpendicularPointToEdgeMidPoint(bottomCenter, RectangleSide.Bottom);
                        double bottomDistance = _visualRect.DistanceFromPerpendicularPointToEdgeMidPoint(bottomCenter, RectangleSide.Bottom);

                        if (scaledOrSymmetricResize)
                        {
                            if (constrainToContainerBounds)
                            {
                                offsetPoint = _visualRect.TopCenter.WithOffset(_visualRect.TopEdge.PerpendicularDirection, bottomDistance);
                                offsetPoint = VideoFrameBounds.ConstrainPoint(offsetPoint);
                                bottomDistance = _visualRect.TopCenter.DistanceTo(offsetPoint);
                                bottomDistance = (centerPoint.DistanceTo(bottomCenter) > centerPoint.DistanceTo(_visualRect.BottomCenter)) ? bottomDistance : -bottomDistance;
                            }

                            offsetEdge = _visualRect.TopEdge.Offset(bottomDistance);
                            newVisualTopLeft = offsetEdge.StartPoint;
                            newVisualTopRight = offsetEdge.EndPoint;
                        }

                        offsetEdge = _visualRect.BottomEdge.Offset(bottomDistance);
                        newVisualBottomRight = offsetEdge.StartPoint;
                        newVisualBottomLeft = offsetEdge.EndPoint;
                        break;
                    default:
                        throw new InvalidEnumArgumentException(nameof(selectionPoint));
                }

                _visualRect = new RectanglePolygon(newVisualTopLeft, newVisualTopRight, newVisualBottomRight, newVisualBottomLeft);
                newAngle = _visualRect.Angle;
                dataRect = _visualRect.ToDerotatedAxisAlignedRect();
            }

            BatchSetActiveKeyFrameProperties(dataRect, newAngle,
                                             undoChangeSetDescription: $"'{Name}' segment crop area resized from {selectionPoint}");

            OnVisualRectChanged(oldVisualRect);
        }

        /// <summary>
        /// Rotates the area to be cropped by offsetting a rectangular visual selection point.
        /// </summary>
        /// <param name="selectionPoint">The rectangular visual selection point.</param>
        /// <param name="selectionPointOffset">The pixel offset to apply to the visual selection point.</param>
        public void RotateFromVisualSelectionPointOffset(RectanglePoint selectionPoint, Vector selectionPointOffset)
        {
            if (!CanBeEdited)
                return;

            Point centerPoint = Center;
            Point oldPoint;

            switch (selectionPoint)
            {
                case RectanglePoint.TopLeft:
                    oldPoint = VisualTopLeft;
                    break;
                case RectanglePoint.TopRight:
                    oldPoint = VisualTopRight;
                    break;
                case RectanglePoint.BottomLeft:
                    oldPoint = VisualBottomLeft;
                    break;
                case RectanglePoint.BottomRight:
                    oldPoint = VisualBottomRight;
                    break;
                default:
                    return;
            }

            Point newPoint = oldPoint + selectionPointOffset;
            double angleDiff = centerPoint.AngleTo(newPoint) - centerPoint.AngleTo(oldPoint);
            Angle += angleDiff;
        }


        /// <summary>
        /// Horizontally centers the area to be cropped.
        /// </summary>
        public void CenterHorizontally()
        {
            Center = new Point(
                ScriptVideoContext.VideoFrameSize.Width / 2d,
                Center.Y
            );
        }

        /// <summary>
        /// Vertically centers the area to be cropped.
        /// </summary>
        public void CenterVertically()
        {
            Center = new Point(
                Center.X,
                ScriptVideoContext.VideoFrameSize.Height / 2d
            );
        }

        /// <inheritdoc/>
        protected internal override KeyFrameViewModelBase CreateKeyFrameViewModel(KeyFrameModelBase keyFrameModel)
        {
            return new CropKeyFrameViewModel(keyFrameModel as CropKeyFrameModel, _rootUndoObject, _undoService, _undoChangeFactory);
        }

        /// <inheritdoc/>
        protected override void OnActiveKeyFrameChanged()
        {
            base.OnActiveKeyFrameChanged();

            if (_activeKeyFrame != null)
            {
                RectanglePolygon oldVisualRect = _visualRect;
                _visualRect = new RectanglePolygon(_activeKeyFrame.Rect, _activeKeyFrame.Angle);

                OnVisualRectChanged(oldVisualRect);

                RaisePropertyChanged(nameof(DataLeft));
                RaisePropertyChanged(nameof(DataTop));
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Updates the <see cref="_visualRect"/> member field
        /// to reflect data property value changes occurring during an undo or redo operation.
        /// </remarks>
        protected override void OnActiveKeyFrameInstancePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnActiveKeyFrameInstancePropertyChanged(sender, e);

            /*  FIXME: PropertyChanged events arising from undoing or redoing batch changes
                cause _visualRect to be recreated for each property changed.
                There should be one consolidated change to _visualRect instead. */

            if (e.PropertyName == nameof(CropKeyFrameViewModel.Left)
                || e.PropertyName == nameof(CropKeyFrameViewModel.Top)
                || e.PropertyName == nameof(CropKeyFrameViewModel.Width)
                || e.PropertyName == nameof(CropKeyFrameViewModel.Height)
                || e.PropertyName == nameof(CropKeyFrameViewModel.Angle)
                || e.PropertyName == nameof(CropKeyFrameViewModel.Rect))
            {
                RectanglePolygon oldVisualRect = _visualRect;
                _visualRect = new RectanglePolygon(_activeKeyFrame.Rect, _activeKeyFrame.Angle);

                OnVisualRectChanged(oldVisualRect);
            }

            if (e.PropertyName == nameof(CropKeyFrameViewModel.Left))
            {
                RaisePropertyChanged(nameof(DataLeft));
            }
            else if (e.PropertyName == nameof(CropKeyFrameViewModel.Top))
            {
                RaisePropertyChanged(nameof(DataTop));
            }
        }

        /// <summary>
        /// Invoked whenever the value of the <see cref="_visualRect"/> field changes.
        /// </summary>
        /// <remarks>
        /// Invokes <see cref="INotifyPropertyChanged.PropertyChanged"/>
        /// for any visual property whose value has changed.
        /// </remarks>
        /// <param name="oldVisualRect">
        /// A reference to a <see cref="RectanglePolygon"/> representing the previous <see cref="_visualRect"/> field value.
        /// For comparing with the current <see cref="_visualRect"/> to determine what visual properties have changed values.
        /// </param>
        private void OnVisualRectChanged(in RectanglePolygon oldVisualRect)
        {
            if (_visualRect.TopLeft != oldVisualRect.TopLeft)
            {
                RaisePropertyChanged(nameof(VisualTopLeft));
            }

            if (_visualRect.TopRight != oldVisualRect.TopRight)
            {
                RaisePropertyChanged(nameof(VisualTopRight));
            }

            if (_visualRect.BottomLeft != oldVisualRect.BottomLeft)
            {
                RaisePropertyChanged(nameof(VisualBottomLeft));
            }

            if (_visualRect.BottomRight != oldVisualRect.BottomRight)
            {
                RaisePropertyChanged(nameof(VisualBottomRight));
            }

            if (_visualRect.Center != oldVisualRect.Center)
            {
                RaisePropertyChanged(nameof(Center));
            }

            if (_visualRect.Width != oldVisualRect.Width)
            {
                RaisePropertyChanged(nameof(Width));
            }

            if (_visualRect.Height != oldVisualRect.Height)
            {
                RaisePropertyChanged(nameof(Height));
            }

            if (_visualRect.Angle != oldVisualRect.Angle)
            {
                RaisePropertyChanged(nameof(Angle));
            }
        }

        /// <summary>
        /// Batch sets the data property values of the <see cref="ActiveKeyFrame"/>.
        /// </summary>
        /// <remarks>
        /// Temporarily stops handling the <see cref="INotifyPropertyChanged.PropertyChanged"/> event for
        /// the current <see cref="ActiveKeyFrame"/> instance so that changes to the <see cref="_visualRect"/>
        /// member field can be consolidated.
        /// </remarks>
        /// <param name="left">The value to set the <see cref="CropKeyFrameViewModel.Left">Left</see> property.</param>
        /// <param name="top">The value to set the <see cref="CropKeyFrameViewModel.Top">Top</see> property.</param>
        /// <param name="width">The value to set the <see cref="CropKeyFrameViewModel.Width">Width</see> property.</param>
        /// <param name="height">The value to set the <see cref="CropKeyFrameViewModel.Height">Height</see> property.</param>
        /// <param name="angle">The value to set the <see cref="CropKeyFrameViewModel.Angle">Angle</see> property.</param>
        /// <param name="undoChangeSetDescription">A description of these batch changes.</param>
        private void BatchSetActiveKeyFrameProperties(double left, double top, double width, double height, double angle, string undoChangeSetDescription)
        {
            // Prevent unnecessary _visualRect re-creation arising from multiple calls to OnActiveKeyFrameInstancePropertyChanged.
            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;

            _undoRoot.BeginChangeSetBatch(undoChangeSetDescription, true);

            if (_activeKeyFrame.Left != left)
            {
                _activeKeyFrame.Left = left;
                RaisePropertyChanged(nameof(DataLeft));
            }

            if (_activeKeyFrame.Top != top)
            {
                _activeKeyFrame.Top = top;
                RaisePropertyChanged(nameof(DataTop));
            }

            _activeKeyFrame.Width = width;
            _activeKeyFrame.Height = height;
            _activeKeyFrame.Angle = angle;

            _undoRoot.EndChangeSetBatch();

            _activeKeyFrame.PropertyChanged -= OnActiveKeyFrameInstancePropertyChanged;
            _activeKeyFrame.PropertyChanged += OnActiveKeyFrameInstancePropertyChanged;
        }

        /// <inheritdoc cref="BatchSetActiveKeyFrameProperties(double, double, double, double, double, string)"/>
        /// <param name="dataRect">
        /// A reference to a <see cref="Rect"/> structure encapsulating the
        /// <see cref="CropKeyFrameViewModel.Left">Left</see>, <see cref="CropKeyFrameViewModel.Top">Top</see>,
        /// <see cref="CropKeyFrameViewModel.Width">Width</see> and <see cref="CropKeyFrameViewModel.Height">Height</see>
        /// property values.
        /// </param>
        private void BatchSetActiveKeyFrameProperties(in Rect dataRect, double angle, string undoChangeSetDescription)
        {
            BatchSetActiveKeyFrameProperties(dataRect.Left, dataRect.Top, dataRect.Width, dataRect.Height, angle, undoChangeSetDescription);
        }

        /// <summary>
        /// Sets the value of a visual corner property and adjusts the <see cref="_visualRect"/> accordingly.
        /// </summary>
        /// <param name="visualCorner">A <see cref="RectangleCorner"/> enum value defining the visual corner property to set.</param>
        /// <param name="propertyValue">The desired value for the property.</param>
        private void SetVisualCornerPropertyValue(RectangleCorner visualCorner, Point propertyValue)
        {
            if (!_canBeEdited)
                return;

            double absAngle = AbsoluteAngle;
            bool constrainToContainerBounds = (absAngle == 0d || absAngle == 90d || absAngle == 180d);

            RectanglePolygon oldVisualRect = _visualRect;

            if (!ResizeVisualRectFromCorner(visualCorner, propertyValue, constrainToContainerBounds, VideoFrameBounds))
            {
                // No changes were made to _visualRect.
                return;
            }

            double newAngle = _visualRect.Angle;
            Rect dataRect = _visualRect.ToDerotatedAxisAlignedRect();
            if (_activeKeyFrame.Rect != dataRect || _activeKeyFrame.Angle != newAngle)
            {
                BatchSetActiveKeyFrameProperties(dataRect, newAngle,
                                                 undoChangeSetDescription: $"'{Name}' segment {visualCorner} changed");
            }

            OnVisualRectChanged(oldVisualRect);
        }

        /// <summary>
        /// Resizes the <see cref="_visualRect"/> from the specified rectangle corner.
        /// </summary>
        /// <remarks>
        /// TODO: Needs refactoring to simplify. Investigate moving this code to the <see cref="RectanglePolygon"/> structure.
        /// </remarks>
        /// <param name="rectangleCorner">
        /// A <see cref="RectangleCorner"/> enum value defining the <see cref="RectanglePolygon"/> field to resize from.
        /// </param>
        /// <param name="cornerPointValue">The desired value for the <see cref="RectanglePolygon"/> field.</param>
        /// <param name="constrainToContainerBounds">
        /// Whether to constrain the <see cref="RectanglePolygon"/> field values to the bounds defined by
        /// the <paramref name="containerBounds"/> parameter.
        /// </param>
        /// <param name="containerBounds">
        /// A <see cref="Rect"/> defining the external bounds to constrain the <see cref="RectanglePolygon"/> field values to.
        /// </param>
        /// <returns><see langword="true"/> if the <see cref="_visualRect"/> was resized; otherwise, <see langword="false"/>.</returns>
        private bool ResizeVisualRectFromCorner(RectangleCorner rectangleCorner, Point cornerPointValue, bool constrainToContainerBounds, Rect containerBounds)
        {
            if (constrainToContainerBounds && !containerBounds.Contains(cornerPointValue))
            {
                return false;
            }

            Point newTopLeft = _visualRect.TopLeft;
            Point newTopRight = _visualRect.TopRight;
            Point newBottomRight = _visualRect.BottomRight;
            Point newBottomLeft = _visualRect.BottomLeft;

            Ray pointValueXIntersectRay;
            Ray pointValueYIntersectRay;
            Point? xIntersectCornerPoint;
            Point? yIntersectCornerPoint;
            Ray xIntersectCornerPointRay;
            Ray yIntersectCornerPointRay;

            switch (rectangleCorner)
            {
                case RectangleCorner.TopLeft:
                    if (newTopLeft == cornerPointValue)
                        return false;

                    pointValueXIntersectRay = new Ray(cornerPointValue, _visualRect.TopEdge.Direction); // Top Left to Top Right
                    pointValueYIntersectRay = new Ray(cornerPointValue, _visualRect.LeftEdge.Direction.Negated()); // Top Left to Bottom Left
                    xIntersectCornerPointRay = new Ray(_visualRect.BottomRight, _visualRect.RightEdge.Direction.Negated()); // Bottom Right to Top Right
                    yIntersectCornerPointRay = new Ray(_visualRect.BottomRight, _visualRect.BottomEdge.Direction); // Bottom Right to BottomLeft
                    break;
                case RectangleCorner.TopRight:
                    if (newTopRight == cornerPointValue)
                        return false;

                    pointValueXIntersectRay = new Ray(cornerPointValue, _visualRect.TopEdge.Direction.Negated()); // Top Right to Top Left
                    pointValueYIntersectRay = new Ray(cornerPointValue, _visualRect.RightEdge.Direction); // Top Right to Bottom Right
                    xIntersectCornerPointRay = new Ray(_visualRect.BottomLeft, _visualRect.LeftEdge.Direction);   // Bottom Left to Top Left
                    yIntersectCornerPointRay = new Ray(_visualRect.BottomLeft, _visualRect.BottomEdge.Direction.Negated()); // Bottom Left to Bottom Right
                    break;
                case RectangleCorner.BottomLeft:
                    if (newBottomLeft == cornerPointValue)
                        return false;

                    pointValueXIntersectRay = new Ray(cornerPointValue, _visualRect.BottomEdge.Direction.Negated()); // Bottom Left to Bottom Right
                    pointValueYIntersectRay = new Ray(cornerPointValue, _visualRect.LeftEdge.Direction); // Bottom Left to Top Left
                    xIntersectCornerPointRay = new Ray(_visualRect.TopRight, _visualRect.RightEdge.Direction);   // Top Right to Bottom Right
                    yIntersectCornerPointRay = new Ray(_visualRect.TopRight, _visualRect.TopEdge.Direction.Negated()); // Top Right to Top Left
                    break;
                case RectangleCorner.BottomRight:
                    if (newBottomRight == cornerPointValue)
                        return false;

                    pointValueXIntersectRay = new Ray(cornerPointValue, _visualRect.BottomEdge.Direction); // Bottom Right to Bottom Left
                    pointValueYIntersectRay = new Ray(cornerPointValue, _visualRect.RightEdge.Direction.Negated()); // Bottom Right to Top Right
                    xIntersectCornerPointRay = new Ray(_visualRect.TopLeft, _visualRect.LeftEdge.Direction.Negated());   // Top Left to Bottom Left
                    yIntersectCornerPointRay = new Ray(_visualRect.TopLeft, _visualRect.TopEdge.Direction); // Top Left to Top Right
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(rectangleCorner));
            }

            if (constrainToContainerBounds)
            {
                xIntersectCornerPoint = pointValueXIntersectRay.IntersectWithinBounds(xIntersectCornerPointRay, containerBounds, out double xIntersectPointDistanceOverage);
                if (!xIntersectCornerPoint.HasValue)
                {
                    return false;
                }

                if (xIntersectPointDistanceOverage > 0d)
                {
                    cornerPointValue = cornerPointValue.WithOffset(xIntersectCornerPointRay.Direction.Negated(), -xIntersectPointDistanceOverage);
                }

                yIntersectCornerPoint = pointValueYIntersectRay.IntersectWithinBounds(yIntersectCornerPointRay, containerBounds, out double yIntersectPointDistanceOverage);
                if (!yIntersectCornerPoint.HasValue)
                {
                    return false;
                }

                if (yIntersectPointDistanceOverage > 0d)
                {
                    cornerPointValue = cornerPointValue.WithOffset(yIntersectCornerPointRay.Direction.Negated(), -yIntersectPointDistanceOverage);
                }
            }
            else
            {
                xIntersectCornerPoint = pointValueXIntersectRay.IntersectWith(xIntersectCornerPointRay);
                yIntersectCornerPoint = pointValueYIntersectRay.IntersectWith(yIntersectCornerPointRay);

                if (!xIntersectCornerPoint.HasValue || !yIntersectCornerPoint.HasValue)
                {
                    return false;
                }
            }

            switch (rectangleCorner)
            {
                case RectangleCorner.TopLeft:
                    newTopLeft = cornerPointValue;
                    newTopRight = xIntersectCornerPoint.Value;
                    newBottomLeft = yIntersectCornerPoint.Value;
                    break;
                case RectangleCorner.TopRight:
                    newTopRight = cornerPointValue;
                    newTopLeft = xIntersectCornerPoint.Value;
                    newBottomRight = yIntersectCornerPoint.Value;
                    break;
                case RectangleCorner.BottomLeft:
                    newBottomLeft = cornerPointValue;
                    newBottomRight = xIntersectCornerPoint.Value;
                    newTopLeft = yIntersectCornerPoint.Value;
                    break;
                case RectangleCorner.BottomRight:
                    newBottomRight = cornerPointValue;
                    newBottomLeft = xIntersectCornerPoint.Value;
                    newTopRight = yIntersectCornerPoint.Value;
                    break;
                default:
                    throw new InvalidEnumArgumentException(nameof(rectangleCorner));
            }

            _visualRect = new RectanglePolygon(newTopLeft, newTopRight, newBottomRight, newBottomLeft);
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as CropSegmentViewModel);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        /// <remarks>
        /// Based on sample code from https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/how-to-define-value-equality-for-a-type#class-example
        /// </remarks>
        public bool Equals([AllowNull] CropSegmentViewModel other)
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
