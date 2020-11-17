using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.ViewModels.Masking;
using VideoScriptEditor.ViewModels.Masking.Shapes;
using SizeI = System.Drawing.Size;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views.Masking
{
    /// <summary>
    /// An <see cref="Adorner"/> for resizing the bounds of a masking shape element.
    /// </summary>
    public class MaskingShapeBoundsResizeAdorner : MaskingShapeResizeAdornerBase
    {
        private readonly Thumb _topLeftHandle;
        private readonly Thumb _topCenterHandle;
        private readonly Thumb _topRightHandle;
        private readonly Thumb _centerLeftHandle;
        private readonly Thumb _centerRightHandle;
        private readonly Thumb _bottomLeftHandle;
        private readonly Thumb _bottomCenterHandle;
        private readonly Thumb _bottomRightHandle;

        /// <summary>
        /// Identifies the <see cref="ShapeDataBounds" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="ShapeDataBounds" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty ShapeDataBoundsProperty =
                DependencyProperty.Register(
                        nameof(ShapeDataBounds),
                        typeof(Rect),
                        typeof(MaskingShapeBoundsResizeAdorner),
                        new FrameworkPropertyMetadata(Rect.Empty, new PropertyChangedCallback(OnShapeDataBoundsPropertyChanged)));

        /// <summary>
        /// Gets or sets a <see cref="Rect"/> that represents the bounding box of the masking shape data.
        /// </summary>
        public Rect ShapeDataBounds
        {
            get => (Rect)GetValue(ShapeDataBoundsProperty);
            set => SetValue(ShapeDataBoundsProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="ShapeDataBounds"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnShapeDataBoundsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MaskingShapeBoundsResizeAdorner adornerInstance = (MaskingShapeBoundsResizeAdorner)d;
            adornerInstance.InvalidateArrange();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MaskingShapeBoundsResizeAdorner"/> class.
        /// </summary>
        /// <inheritdoc cref="MaskingShapeResizeAdornerBase(FrameworkElement)"/>
        public MaskingShapeBoundsResizeAdorner(FrameworkElement adornedElement) : base(adornedElement)
        {
            _topLeftHandle = CreateHandleThumb(Cursors.SizeNWSE, RectanglePoint.TopLeft);
            _visualChildren.Add(_topLeftHandle);

            _topCenterHandle = CreateHandleThumb(Cursors.SizeNS, RectanglePoint.TopCenter);
            _visualChildren.Add(_topCenterHandle);

            _topRightHandle = CreateHandleThumb(Cursors.SizeNESW, RectanglePoint.TopRight);
            _visualChildren.Add(_topRightHandle);

            _centerLeftHandle = CreateHandleThumb(Cursors.SizeWE, RectanglePoint.CenterLeft);
            _visualChildren.Add(_centerLeftHandle);

            _centerRightHandle = CreateHandleThumb(Cursors.SizeWE, RectanglePoint.CenterRight);
            _visualChildren.Add(_centerRightHandle);

            _bottomLeftHandle = CreateHandleThumb(Cursors.SizeNESW, RectanglePoint.BottomLeft);
            _visualChildren.Add(_bottomLeftHandle);

            _bottomCenterHandle = CreateHandleThumb(Cursors.SizeNS, RectanglePoint.BottomCenter);
            _visualChildren.Add(_bottomCenterHandle);

            _bottomRightHandle = CreateHandleThumb(Cursors.SizeNWSE, RectanglePoint.BottomRight);
            _visualChildren.Add(_bottomRightHandle);
        }

        /// <inheritdoc/>
        /// <remarks>Arranges the adorner handles.</remarks>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect shapeBounds = ShapeDataBounds;
            if (shapeBounds != Rect.Empty)
            {
                SizeI videoFrameSize = MaskingViewModel.ScriptVideoContext.VideoFrameSize;
                shapeBounds.Scale(finalSize.Width / videoFrameSize.Width, finalSize.Height / videoFrameSize.Height);

                ArrangeHandleThumb(_topLeftHandle, shapeBounds.TopLeft);
                ArrangeHandleThumb(_topCenterHandle, shapeBounds.TopCenter());
                ArrangeHandleThumb(_topRightHandle, shapeBounds.TopRight);
                ArrangeHandleThumb(_centerLeftHandle, shapeBounds.CenterLeft());
                ArrangeHandleThumb(_centerRightHandle, shapeBounds.CenterRight());
                ArrangeHandleThumb(_bottomLeftHandle, shapeBounds.BottomLeft);
                ArrangeHandleThumb(_bottomCenterHandle, shapeBounds.BottomCenter());
                ArrangeHandleThumb(_bottomRightHandle, shapeBounds.BottomRight);
            }

            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc/>
        protected override void OnHandleThumbDragDelta(object sender, DragDeltaEventArgs args)
        {
            IMaskingViewModel maskingViewModel = MaskingViewModel;
            MaskShapeViewModelBase selectedMaskingShape = maskingViewModel.SelectedSegment as MaskShapeViewModelBase;
            Debug.Assert(selectedMaskingShape != null);

            Thumb handleThumb = (Thumb)sender;
            RectanglePoint handlePosition = (RectanglePoint)handleThumb.Tag;

            FrameworkElement adornedElement = (FrameworkElement)AdornedElement;
            double minSize = 0.001d;
            double elementWidth = Math.Max(minSize, adornedElement.ActualWidth);
            double elementHeight = Math.Max(minSize, adornedElement.ActualHeight);

            SizeI videoFrameSize = maskingViewModel.ScriptVideoContext.VideoFrameSize;

            double xDeltaScaleFactor = videoFrameSize.Width / elementWidth;
            double yDeltaScaleFactor = videoFrameSize.Height / elementHeight;
            Vector changeDelta = new Vector(args.HorizontalChange * xDeltaScaleFactor, args.VerticalChange * yDeltaScaleFactor);

            bool scaledResize = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            selectedMaskingShape.ResizeFromBoundingPointOffset(handlePosition, changeDelta, scaledResize);
        }
    }
}
