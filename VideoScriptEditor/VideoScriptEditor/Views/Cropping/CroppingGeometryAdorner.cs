using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.ViewModels.Cropping;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.Views.Cropping
{
    /// <summary>
    /// An adorner for resizing and rotating cropping <see cref="System.Windows.Media.Geometry"/> shapes.
    /// </summary>
    public class CroppingGeometryAdorner : Adorner
    {
        // Uses Thumbs for visual elements.
        // The Thumbs have built-in mouse input handling.
        private readonly CropAdjustmentHandleThumb _topLeftHandle;
        private readonly CropAdjustmentHandleThumb _topCenterHandle;
        private readonly CropAdjustmentHandleThumb _topRightHandle;
        private readonly CropAdjustmentHandleThumb _centerLeftHandle;
        private readonly CropAdjustmentHandleThumb _centerRightHandle;
        private readonly CropAdjustmentHandleThumb _bottomLeftHandle;
        private readonly CropAdjustmentHandleThumb _bottomCenterHandle;
        private readonly CropAdjustmentHandleThumb _bottomRightHandle;

        // Stores and manages the adorner's visual children.
        private readonly VisualCollection _visualChildren;

        /// <inheritdoc cref="ICroppingViewModel"/>
        private ICroppingViewModel CroppingViewModel => ((FrameworkElement)AdornedElement).DataContext as ICroppingViewModel;

        /// <summary>
        /// Identifies the <see cref="TopLeftHandleDataCoordinate" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="TopLeftHandleDataCoordinate" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty TopLeftHandleDataCoordinateProperty =
                DependencyProperty.Register(
                        nameof(TopLeftHandleDataCoordinate),
                        typeof(Point),
                        typeof(CroppingGeometryAdorner),
                        new FrameworkPropertyMetadata(new Point(), new PropertyChangedCallback(OnHandleDataCoordinatePropertyChanged)));

        /// <summary>
        /// Gets or sets the data coordinate for positioning the top-left handle.
        /// </summary>
        public Point TopLeftHandleDataCoordinate
        {
            get => (Point)GetValue(TopLeftHandleDataCoordinateProperty);
            set => SetValue(TopLeftHandleDataCoordinateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TopRightHandleDataCoordinate" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="TopRightHandleDataCoordinate" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty TopRightHandleDataCoordinateProperty =
                DependencyProperty.Register(
                        nameof(TopRightHandleDataCoordinate),
                        typeof(Point),
                        typeof(CroppingGeometryAdorner),
                        new FrameworkPropertyMetadata(new Point(), new PropertyChangedCallback(OnHandleDataCoordinatePropertyChanged)));

        /// <summary>
        /// Gets or sets the data coordinate for positioning the top-right handle.
        /// </summary>
        public Point TopRightHandleDataCoordinate
        {
            get => (Point)GetValue(TopRightHandleDataCoordinateProperty);
            set => SetValue(TopRightHandleDataCoordinateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BottomLeftHandleDataCoordinate" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="BottomLeftHandleDataCoordinate" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty BottomLeftHandleDataCoordinateProperty =
                DependencyProperty.Register(
                        nameof(BottomLeftHandleDataCoordinate),
                        typeof(Point),
                        typeof(CroppingGeometryAdorner),
                        new FrameworkPropertyMetadata(new Point(), new PropertyChangedCallback(OnHandleDataCoordinatePropertyChanged)));

        /// <summary>
        /// Gets or sets the data coordinate for positioning the bottom-left handle.
        /// </summary>
        public Point BottomLeftHandleDataCoordinate
        {
            get => (Point)GetValue(BottomLeftHandleDataCoordinateProperty);
            set => SetValue(BottomLeftHandleDataCoordinateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BottomRightHandleDataCoordinate" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="BottomRightHandleDataCoordinate" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty BottomRightHandleDataCoordinateProperty =
                DependencyProperty.Register(
                        nameof(BottomRightHandleDataCoordinate),
                        typeof(Point),
                        typeof(CroppingGeometryAdorner),
                        new FrameworkPropertyMetadata(new Point(), new PropertyChangedCallback(OnHandleDataCoordinatePropertyChanged)));

        /// <summary>
        /// Gets or sets the data coordinate for positioning the bottom-right handle.
        /// </summary>
        public Point BottomRightHandleDataCoordinate
        {
            get => (Point)GetValue(BottomRightHandleDataCoordinateProperty);
            set => SetValue(BottomRightHandleDataCoordinateProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for handle data coordinate properties.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnHandleDataCoordinatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CroppingGeometryAdorner adornerInstance = (CroppingGeometryAdorner)d;
            adornerInstance.InvalidateArrange();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CroppingGeometryAdorner"/> class.
        /// </summary>
        /// <inheritdoc cref="Adorner(UIElement)"/>
        public CroppingGeometryAdorner(FrameworkElement adornedElement)
            : base(adornedElement)
        {
            Debug.Assert(adornedElement.DataContext is ICroppingViewModel, "ICroppingViewModel instance was null!");

            // Call a helper method to initialize the Thumbs
            // with a customized cursor and handler for resizing.
            _topLeftHandle = CreateHandleThumb(RectanglePoint.TopLeft);
            _topCenterHandle = CreateHandleThumb(RectanglePoint.TopCenter);
            _topRightHandle = CreateHandleThumb(RectanglePoint.TopRight);
            _centerLeftHandle = CreateHandleThumb(RectanglePoint.CenterLeft);
            _centerRightHandle = CreateHandleThumb(RectanglePoint.CenterRight);
            _bottomLeftHandle = CreateHandleThumb(RectanglePoint.BottomLeft);
            _bottomCenterHandle = CreateHandleThumb(RectanglePoint.BottomCenter);
            _bottomRightHandle = CreateHandleThumb(RectanglePoint.BottomRight);

            _visualChildren = new VisualCollection(this)
            {
                _topLeftHandle,
                _topCenterHandle,
                _topRightHandle,
                _centerLeftHandle,
                _centerRightHandle,
                _bottomLeftHandle,
                _bottomCenterHandle,
                _bottomRightHandle
            };
        }

        /// <inheritdoc/>
        /// <remarks>Overridden to interface with the adorner's visual collection.</remarks>
        protected override int VisualChildrenCount => _visualChildren.Count;

        /// <inheritdoc/>
        /// <remarks>Overridden to interface with the adorner's visual collection.</remarks>
        protected override Visual GetVisualChild(int index) => _visualChildren[index];

        /// <inheritdoc/>
        /// <remarks>Arranges the adorner handles.</remarks>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (CroppingViewModel.ScriptVideoContext.HasVideo)
            {
                SizeI videoFrameSize = CroppingViewModel.ScriptVideoContext.VideoFrameSize;

                double xScaleFactor = finalSize.Width / videoFrameSize.Width;
                double yScaleFactor = finalSize.Height / videoFrameSize.Height;

                ArrangeHandleThumb(_topLeftHandle, TopLeftHandleDataCoordinate.Scale(xScaleFactor, yScaleFactor));
                ArrangeHandleThumb(_topCenterHandle, PointExt.MidPoint(TopLeftHandleDataCoordinate, TopRightHandleDataCoordinate).Scale(xScaleFactor, yScaleFactor));
                ArrangeHandleThumb(_topRightHandle, TopRightHandleDataCoordinate.Scale(xScaleFactor, yScaleFactor));
                ArrangeHandleThumb(_centerLeftHandle, PointExt.MidPoint(TopLeftHandleDataCoordinate, BottomLeftHandleDataCoordinate).Scale(xScaleFactor, yScaleFactor));
                ArrangeHandleThumb(_centerRightHandle, PointExt.MidPoint(TopRightHandleDataCoordinate, BottomRightHandleDataCoordinate).Scale(xScaleFactor, yScaleFactor));
                ArrangeHandleThumb(_bottomLeftHandle, BottomLeftHandleDataCoordinate.Scale(xScaleFactor, yScaleFactor));
                ArrangeHandleThumb(_bottomCenterHandle, PointExt.MidPoint(BottomLeftHandleDataCoordinate, BottomRightHandleDataCoordinate).Scale(xScaleFactor, yScaleFactor));
                ArrangeHandleThumb(_bottomRightHandle, BottomRightHandleDataCoordinate.Scale(xScaleFactor, yScaleFactor));
            }

            // Return the final size.
            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Creates a <see cref="CropAdjustmentHandleThumb"/> and sets the <see cref="RectanglePoint">handle position</see>,
        /// data bindings and event handlers.
        /// </summary>
        /// <param name="handlePosition">A <see cref="RectanglePoint"/> value specifying the handle position.</param>
        /// <returns>A new <see cref="CropAdjustmentHandleThumb"/> instance.</returns>
        private CropAdjustmentHandleThumb CreateHandleThumb(RectanglePoint handlePosition)
        {
            ICroppingViewModel croppingViewModel = CroppingViewModel;

            CropAdjustmentHandleThumb handleThumb = new CropAdjustmentHandleThumb()
            {
                HandlePosition = handlePosition,
                Cursor = Cursors.Hand
            };

            handleThumb.SetBinding(
                CropAdjustmentHandleThumb.AdjustmentModeProperty,
                new Binding(nameof(ICroppingViewModel.AdjustmentHandleMode))
                {
                    Source = croppingViewModel,
                    Mode = BindingMode.OneWay,
                }
            );

            handleThumb.SetBinding(
                CropAdjustmentHandleThumb.ShowHandleProperty,
                new Binding($"{nameof(ICroppingViewModel.SelectedSegment)}.{nameof(CropSegmentViewModel.CanBeEdited)}")
                {
                    Source = croppingViewModel,
                    Mode = BindingMode.OneWay,
                    FallbackValue = false
                }
            );

            handleThumb.SetBinding(
                CropAdjustmentHandleThumb.HandleAngleProperty,
                new Binding($"{nameof(ICroppingViewModel.SelectedSegment)}.{nameof(CropSegmentViewModel.Angle)}")
                {
                    Source = croppingViewModel,
                    Mode = BindingMode.OneWay,
                    FallbackValue = 0d
                }
            );

            handleThumb.DragStarted += OnHandleThumbDragStarted;
            handleThumb.DragDelta += OnHandleThumbDragDelta;
            handleThumb.DragCompleted += OnHandleThumbDragCompleted;

            return handleThumb;
        }

        /// <summary>
        /// Arranges a <see cref="CropAdjustmentHandleThumb"/>
        /// so that it is centered in the specified <see cref="Point">position</see>.
        /// </summary>
        /// <param name="handleThumb">The <see cref="CropAdjustmentHandleThumb"/> to arrange.</param>
        /// <param name="position">
        /// A <see cref="Point"/> specifying the center position to arrange the <see cref="CropAdjustmentHandleThumb"/>.
        /// </param>
        private void ArrangeHandleThumb(CropAdjustmentHandleThumb handleThumb, Point position)
        {
            Vector halfSize = (Vector)handleThumb.DesiredSize / 2;
            handleThumb.Arrange(new Rect(position - halfSize, position + halfSize));
        }

        /// <summary>
        /// Handles the <see cref="Thumb.DragStarted"/> event for a <see cref="CropAdjustmentHandleThumb"/>.
        /// </summary>
        /// <inheritdoc cref="DragStartedEventHandler"/>
        private void OnHandleThumbDragStarted(object sender, DragStartedEventArgs e)
        {
            ICroppingViewModel croppingViewModel = CroppingViewModel;

            if (croppingViewModel.SelectedSegment is CropSegmentViewModel selectedCropSegment)
            {
                switch (croppingViewModel.AdjustmentHandleMode)
                {
                    case CropAdjustmentHandleMode.Resize:
                        selectedCropSegment.BeginBatchResizeAction();
                        break;
                    case CropAdjustmentHandleMode.Rotate:
                        selectedCropSegment.BeginBatchRotationAction();
                        break;
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="Thumb.DragDelta"/> event for a <see cref="CropAdjustmentHandleThumb"/>.
        /// </summary>
        /// <inheritdoc cref="DragDeltaEventHandler"/>
        private void OnHandleThumbDragDelta(object sender, DragDeltaEventArgs args)
        {
            ICroppingViewModel croppingViewModel = CroppingViewModel;

            CropSegmentViewModel selectedCropSegment = croppingViewModel.SelectedSegment as CropSegmentViewModel;
            RectanglePoint? handlePosition = (sender as CropAdjustmentHandleThumb)?.HandlePosition;
            Debug.Assert(selectedCropSegment != null && handlePosition.HasValue);

            FrameworkElement adornedElement = (FrameworkElement)AdornedElement;
            double minSize = 0.001d;
            double elementWidth = Math.Max(minSize, adornedElement.ActualWidth);
            double elementHeight = Math.Max(minSize, adornedElement.ActualHeight);

            // Change the size by the amount the user drags the mouse, as long as it's larger 
            // than the width or height of an adorner, respectively.

            SizeI videoFrameSize = croppingViewModel.ScriptVideoContext.VideoFrameSize;

            double xDeltaScaleFactor = videoFrameSize.Width / elementWidth;
            double yDeltaScaleFactor = videoFrameSize.Height / elementHeight;
            Vector changeDelta = new Vector(args.HorizontalChange * xDeltaScaleFactor, args.VerticalChange * yDeltaScaleFactor);

            switch (croppingViewModel.AdjustmentHandleMode)
            {
                case CropAdjustmentHandleMode.Resize:
                    bool scaledOrSymmetricResize = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    selectedCropSegment.ResizeFromVisualSelectionPointOffset(handlePosition.Value, changeDelta, scaledOrSymmetricResize);
                    break;
                case CropAdjustmentHandleMode.Rotate:
                    selectedCropSegment.RotateFromVisualSelectionPointOffset(handlePosition.Value, changeDelta);
                    break;
            }
        }

        /// <summary>
        /// Handles the <see cref="Thumb.DragCompleted"/> event for a <see cref="CropAdjustmentHandleThumb"/>.
        /// </summary>
        /// <inheritdoc cref="DragCompletedEventHandler"/>
        private void OnHandleThumbDragCompleted(object sender, DragCompletedEventArgs e)
        {
            CroppingViewModel.SelectedSegment?.CompleteUndoableAction();
        }
    }
}
