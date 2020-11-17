using System.Windows;
using System.Windows.Controls.Primitives;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.ViewModels.Cropping;

namespace VideoScriptEditor.Views.Cropping
{
    /// <summary>
    /// A handle <see cref="Thumb"/> for adjusting the size or rotation angle of a crop segment
    /// being adorned by the <see cref="CroppingGeometryAdorner"/>.
    /// </summary>
    public class CropAdjustmentHandleThumb : Thumb
    {
        /// <summary>
        /// Identifies the <see cref="HandlePosition" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="HandlePosition" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty HandlePositionProperty = DependencyProperty.Register(
            nameof(HandlePosition),
            typeof(RectanglePoint?),
            typeof(CropAdjustmentHandleThumb),
            new PropertyMetadata(null, OnHandlePositionPropertyChanged));

        /// <summary>
        /// The rectangular position of the <see cref="CropAdjustmentHandleThumb"/>
        /// around the crop segment.
        /// </summary>
        public RectanglePoint? HandlePosition
        {
            get => (RectanglePoint?)GetValue(HandlePositionProperty);
            set => SetValue(HandlePositionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HandleAngle" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="HandleAngle" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty HandleAngleProperty = DependencyProperty.Register(
            nameof(HandleAngle),
            typeof(double),
            typeof(CropAdjustmentHandleThumb),
            new PropertyMetadata(0d, null, CoerceHandleAngleValue));

        /// <summary>
        /// The handle angle.
        /// </summary>
        public double HandleAngle
        {
            get => (double)GetValue(HandleAngleProperty);
            set => SetValue(HandleAngleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowHandle" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="ShowHandle" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty ShowHandleProperty = DependencyProperty.Register(
            nameof(ShowHandle),
            typeof(bool),
            typeof(CropAdjustmentHandleThumb),
            new PropertyMetadata(false, OnShowHandlePropertyChanged, CoerceShowHandleValue));

        /// <summary>
        /// Whether to display the handle.
        /// </summary>
        public bool ShowHandle
        {
            get => (bool)GetValue(ShowHandleProperty);
            set => SetValue(ShowHandleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AdjustmentMode" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="AdjustmentMode" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty AdjustmentModeProperty = DependencyProperty.Register(
            nameof(AdjustmentMode),
            typeof(CropAdjustmentHandleMode),
            typeof(CropAdjustmentHandleThumb),
            new PropertyMetadata(CropAdjustmentHandleMode.Resize, OnAdjustmentModePropertyChanged));

        /// <summary>
        /// Whether the <see cref="CropAdjustmentHandleThumb"/> is adjusting the size or rotation angle of the crop segment.
        /// </summary>
        public CropAdjustmentHandleMode AdjustmentMode
        {
            get => (CropAdjustmentHandleMode)GetValue(AdjustmentModeProperty);
            set => SetValue(AdjustmentModeProperty, value);
        }

        /// <summary>
        /// Creates a new <see cref="CropAdjustmentHandleThumb"/> instance.
        /// </summary>
        public CropAdjustmentHandleThumb() : base()
        {
        }

        /// <summary>
        /// Static constructor for the <see cref="CropAdjustmentHandleThumb"/> class.
        /// It simply defines a default style.
        /// </summary>
        static CropAdjustmentHandleThumb()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CropAdjustmentHandleThumb), new FrameworkPropertyMetadata(typeof(CropAdjustmentHandleThumb)));
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="HandlePosition"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnHandlePositionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ShowHandleProperty);
            d.CoerceValue(HandleAngleProperty);
        }

        /// <summary>
        /// <see cref="CoerceValueCallback"/> for the <see cref="HandleAngle"/> property.
        /// </summary>
        /// <inheritdoc cref="CoerceValueCallback"/>
        private static object CoerceHandleAngleValue(DependencyObject d, object baseValue)
        {
            CropAdjustmentHandleThumb thumbInstance = (CropAdjustmentHandleThumb)d;
            double angleValue = (double)baseValue;

            double baseHandleAngle;
            switch (thumbInstance.HandlePosition)
            {
                case RectanglePoint.TopLeft:
                    baseHandleAngle = 45d;
                    break;
                case RectanglePoint.TopRight:
                    baseHandleAngle = -225d;
                    break;
                case RectanglePoint.BottomLeft:
                    baseHandleAngle = -45d;
                    break;
                case RectanglePoint.BottomRight:
                    baseHandleAngle = 225d;
                    break;
                case RectanglePoint.TopCenter:
                case RectanglePoint.BottomCenter:
                    baseHandleAngle = -90d;
                    break;
                case RectanglePoint.CenterLeft:
                case RectanglePoint.CenterRight:
                    baseHandleAngle = 0d;
                    break;
                default:
                    baseHandleAngle = 0d;
                    break;
            }

            return angleValue == 0d ? baseHandleAngle : angleValue + baseHandleAngle;
        }

        /// <summary>
        /// <see cref="CoerceValueCallback"/> for the <see cref="ShowHandle"/> property.
        /// </summary>
        /// <inheritdoc cref="CoerceValueCallback"/>
        private static object CoerceShowHandleValue(DependencyObject d, object baseValue)
        {
            CropAdjustmentHandleThumb thumbInstance = (CropAdjustmentHandleThumb)d;
            bool showHandle = (bool)baseValue;

            if (showHandle == true && thumbInstance.AdjustmentMode == CropAdjustmentHandleMode.Rotate)
            {
                RectanglePoint? handlePosition = thumbInstance.HandlePosition;
                if (handlePosition == RectanglePoint.TopCenter
                    || handlePosition == RectanglePoint.CenterLeft
                    || handlePosition == RectanglePoint.CenterRight
                    || handlePosition == RectanglePoint.BottomCenter)
                {
                    showHandle = false;
                }
            }

            return showHandle;
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="ShowHandle"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnShowHandlePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CropAdjustmentHandleThumb thumbInstance = (CropAdjustmentHandleThumb)d;
            bool showHandle = (bool)e.NewValue;

            thumbInstance.Visibility = showHandle ? Visibility.Visible : Visibility.Collapsed;
            thumbInstance.IsEnabled = showHandle;
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="AdjustmentMode"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnAdjustmentModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ShowHandleProperty);
        }
    }
}