using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using VideoScriptEditor.Geometry;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.Views.Masking
{
    /// <summary>
    /// An adorner for resizing the points of a masking ellipse shape element.
    /// </summary>
    public class MaskingEllipsePointsResizeAdorner : MaskingShapeResizeAdornerBase
    {
        private readonly Thumb _topCenterHandle;
        private readonly Thumb _centerLeftHandle;
        private readonly Thumb _centerRightHandle;
        private readonly Thumb _bottomCenterHandle;

        /// <summary>
        /// Identifies the <see cref="EllipseDataRadiusX" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="EllipseDataRadiusX" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty EllipseDataRadiusXProperty =
                DependencyProperty.Register(
                        nameof(EllipseDataRadiusX),
                        typeof(double),
                        typeof(MaskingEllipsePointsResizeAdorner),
                        new FrameworkPropertyMetadata(0d, new PropertyChangedCallback(OnEllipseRadiusDataPropertyChanged)));

        /// <summary>
        /// Gets or sets the x-radius data value of the ellipse.
        /// </summary>
        public double EllipseDataRadiusX
        {
            get => (double)GetValue(EllipseDataRadiusXProperty);
            set => SetValue(EllipseDataRadiusXProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EllipseDataRadiusY" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="EllipseDataRadiusY" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty EllipseDataRadiusYProperty =
                DependencyProperty.Register(
                        nameof(EllipseDataRadiusY),
                        typeof(double),
                        typeof(MaskingEllipsePointsResizeAdorner),
                        new FrameworkPropertyMetadata(0d, new PropertyChangedCallback(OnEllipseRadiusDataPropertyChanged)));

        /// <summary>
        /// Gets or sets the y-radius data value of the ellipse.
        /// </summary>
        public double EllipseDataRadiusY
        {
            get => (double)GetValue(EllipseDataRadiusYProperty);
            set => SetValue(EllipseDataRadiusYProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="EllipseDataCenterPoint" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="EllipseDataCenterPoint" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty EllipseDataCenterPointProperty =
                DependencyProperty.Register(
                        nameof(EllipseDataCenterPoint),
                        typeof(Point),
                        typeof(MaskingEllipsePointsResizeAdorner),
                        new FrameworkPropertyMetadata(new Point()));

        /// <summary>
        /// Gets or sets the center point data value of the ellipse.
        /// </summary>
        public Point EllipseDataCenterPoint
        {
            get => (Point)GetValue(EllipseDataCenterPointProperty);
            set => SetValue(EllipseDataCenterPointProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for ellipse radius data properties.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnEllipseRadiusDataPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MaskingEllipsePointsResizeAdorner adornerInstance = (MaskingEllipsePointsResizeAdorner)d;
            adornerInstance.InvalidateArrange();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MaskingEllipsePointsResizeAdorner"/> class.
        /// </summary>
        /// <inheritdoc cref="MaskingShapeResizeAdornerBase(FrameworkElement)"/>
        public MaskingEllipsePointsResizeAdorner(FrameworkElement adornedElement) : base(adornedElement)
        {
            // RadiusX
            _centerLeftHandle = CreateHandleThumb(Cursors.SizeWE, RectanglePoint.CenterLeft);
            _visualChildren.Add(_centerLeftHandle);

            _centerRightHandle = CreateHandleThumb(Cursors.SizeWE, RectanglePoint.CenterRight);
            _visualChildren.Add(_centerRightHandle);

            // RadiusY
            _topCenterHandle = CreateHandleThumb(Cursors.SizeNS, RectanglePoint.TopCenter);
            _visualChildren.Add(_topCenterHandle);

            _bottomCenterHandle = CreateHandleThumb(Cursors.SizeNS, RectanglePoint.BottomCenter);
            _visualChildren.Add(_bottomCenterHandle);
        }

        /// <inheritdoc/>
        /// <remarks>Arranges the adorner handles.</remarks>
        protected override Size ArrangeOverride(Size finalSize)
        {
            SizeI videoFrameSize = MaskingViewModel.ScriptVideoContext.VideoFrameSize;

            Rect ellipseBounds = Ellipse.CalculateBounds(EllipseDataCenterPoint, EllipseDataRadiusX, EllipseDataRadiusY);
            ellipseBounds.Scale(finalSize.Width / videoFrameSize.Width, finalSize.Height / videoFrameSize.Height);

            ArrangeHandleThumb(_centerLeftHandle, ellipseBounds.CenterLeft());
            ArrangeHandleThumb(_centerRightHandle, ellipseBounds.CenterRight());
            ArrangeHandleThumb(_topCenterHandle, ellipseBounds.TopCenter());
            ArrangeHandleThumb(_bottomCenterHandle, ellipseBounds.BottomCenter());

            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc/>
        protected override void OnHandleThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb handleThumb = (Thumb)sender;
            RectanglePoint handlePosition = (RectanglePoint)handleThumb.Tag;

            FrameworkElement adornedElement = (FrameworkElement)AdornedElement;
            double minSize = 0.001d;
            double elementWidth = Math.Max(minSize, adornedElement.ActualWidth);
            double elementHeight = Math.Max(minSize, adornedElement.ActualHeight);

            SizeI videoFrameSize = MaskingViewModel.ScriptVideoContext.VideoFrameSize;

            double xDeltaScaleFactor = videoFrameSize.Width / elementWidth;
            double yDeltaScaleFactor = videoFrameSize.Height / elementHeight;
            Vector changeDelta = new Vector(e.HorizontalChange * xDeltaScaleFactor, e.VerticalChange * yDeltaScaleFactor);

            double clampRadiusXToBounds(double radiusXVal)
            {
                double centerPointX = EllipseDataCenterPoint.X;
                return Math.Clamp(radiusXVal, 0d, Math.Min(centerPointX, videoFrameSize.Width - centerPointX));
            }

            double clampRadiusYToBounds(double radiusYVal)
            {
                double centerPointY = EllipseDataCenterPoint.Y;
                return Math.Clamp(radiusYVal, 0d, Math.Min(centerPointY, videoFrameSize.Height - centerPointY));
            }

            switch (handlePosition)
            {
                case RectanglePoint.CenterLeft:
                    EllipseDataRadiusX = clampRadiusXToBounds(EllipseDataRadiusX - changeDelta.X);
                    break;
                case RectanglePoint.CenterRight:
                    EllipseDataRadiusX = clampRadiusXToBounds(EllipseDataRadiusX + changeDelta.X);
                    break;
                case RectanglePoint.TopCenter:
                    EllipseDataRadiusY = clampRadiusYToBounds(EllipseDataRadiusY - changeDelta.Y);
                    break;
                case RectanglePoint.BottomCenter:
                    EllipseDataRadiusY = clampRadiusYToBounds(EllipseDataRadiusY + changeDelta.Y);
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Unexpected {nameof(RectanglePoint)} value for handle thumb: {handlePosition}");
            }
        }
    }
}
