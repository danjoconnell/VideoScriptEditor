using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// An <see cref="Adorner"/> for visually previewing the new location of a <see cref="VideoTimelineSegment"/>
    /// during a drag operation.
    /// </summary>
    public class TrackSegmentDragAdorner : TrackSegmentAdornerBase
    {
        private Size _previewBorderSize;

        /// <summary>
        /// Identifies the <see cref="PositionOffset" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="PositionOffset" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty PositionOffsetProperty =
                DependencyProperty.Register(
                        nameof(PositionOffset),
                        typeof(Point),
                        typeof(TrackSegmentDragAdorner),
                        new FrameworkPropertyMetadata(default(Point), new PropertyChangedCallback(OnPositionOffsetPropertyChanged)));

        /// <summary>
        /// Gets or sets the distance between the top-left coordinate of the <see cref="Adorner.AdornedElement">adorned element</see>
        /// and the top-left coordinate of the element providing a preview of the new location of the <see cref="VideoTimelineSegment"/>.
        /// </summary>
        public Point PositionOffset
        {
            get => (Point)GetValue(PositionOffsetProperty);
            set => SetValue(PositionOffsetProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="PositionOffset"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnPositionOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TrackSegmentDragAdorner dragAdorner = (TrackSegmentDragAdorner)d;
            Point positionOffset = (Point)e.NewValue;

            dragAdorner._previewToolTip.HorizontalOffset = positionOffset.X + (dragAdorner._previewBorderSize.Width / 4d);
            dragAdorner._previewToolTip.VerticalOffset = positionOffset.Y + (dragAdorner._previewBorderSize.Height / 4d);

            // Update position
            dragAdorner._adornerLayer.Update();
        }

        /// <summary>
        /// Identifies the <see cref="IsDragValid" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="IsDragValid" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty IsDragValidProperty =
                DependencyProperty.Register(
                        nameof(IsDragValid),
                        typeof(bool),
                        typeof(TrackSegmentDragAdorner),
                        new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnIsDragValidPropertyChanged)));

        /// <summary>
        /// Gets or sets a value indicating whether the location the <see cref="VideoTimelineSegment"/>
        /// is being dragged to is valid.
        /// </summary>
        public bool IsDragValid
        {
            get => (bool)GetValue(IsDragValidProperty);
            set => SetValue(IsDragValidProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="IsDragValid"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnIsDragValidPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TrackSegmentDragAdorner dragAdorner = (TrackSegmentDragAdorner)d;
            dragAdorner._previewBorder.Background = (bool)e.NewValue ? Brushes.Green : Brushes.Red;

            // Update visual
            dragAdorner._adornerLayer.Update();
        }

        /// <summary>
        /// Creates a new <see cref="TrackSegmentDragAdorner"/> instance.
        /// </summary>
        /// <inheritdoc cref="TrackSegmentAdornerBase(UIElement)"/>
        /// <param name="segmentElementSize">The size of the <see cref="VideoTimelineSegment"/> being dragged.</param>
        public TrackSegmentDragAdorner(UIElement adornedElement, Size segmentElementSize) : base(adornedElement)
        {
            _previewBorderSize = segmentElementSize;

            _previewBorder.Width = segmentElementSize.Width;
            _previewBorder.Height = segmentElementSize.Height;
            _previewBorder.Background = Brushes.Green;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _previewBorder.Arrange(new Rect(finalSize));
            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc/>
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            Point positionOffset = PositionOffset;
            GeneralTransformGroup result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(positionOffset.X, positionOffset.Y));
            return result;
        }
    }
}
