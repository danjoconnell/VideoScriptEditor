using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using VideoScriptEditor.Views.Common;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// An <see cref="Adorner"/> for visually previewing changes to the left and right bounds of a <see cref="VideoTimelineSegment"/>.
    /// </summary>
    public class TrackSegmentResizeAdorner : TrackSegmentAdornerBase
    {
        private readonly HorizontalSide _attachPosition;
        private readonly Brush _expandBackgroundBrush;
        private readonly Brush _contractBackgroundBrush;
        private readonly VideoTimelineSegment _segmentElement;
        private readonly double _segmentElementLeftCoordinate;
        private Rect _previewBorderArrangeRect;

        /// <summary>
        /// Identifies the <see cref="HorizontalOffset" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="HorizontalOffset" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty HorizontalOffsetProperty =
                DependencyProperty.Register(
                        nameof(HorizontalOffset),
                        typeof(double),
                        typeof(TrackSegmentResizeAdorner),
                        new FrameworkPropertyMetadata(0d, new PropertyChangedCallback(OnHorizontalOffsetPropertyChanged)));

        /// <summary>
        /// Gets or sets the horizontal distance between the left edge of the adorned <see cref="VideoTimelineTrack"/>
        /// and the left edge of the element providing a preview of the resized bounds of the <see cref="VideoTimelineSegment"/>.
        /// </summary>
        public double HorizontalOffset
        {
            get => (double)GetValue(HorizontalOffsetProperty);
            set => SetValue(HorizontalOffsetProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="HorizontalOffset"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnHorizontalOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TrackSegmentResizeAdorner)d).OnHorizontalOffsetChanged();
        }

        /// <summary>
        /// Creates a new <see cref="TrackSegmentResizeAdorner"/> instance.
        /// </summary>
        /// <inheritdoc cref="TrackSegmentAdornerBase(UIElement)"/>
        /// <param name="segmentElement">The <see cref="VideoTimelineSegment"/> whose bounds are being changed.</param>
        /// <param name="segmentElementLeftCoordinate">
        /// The horizontal distance between the left edge of the adorned <see cref="VideoTimelineTrack"/> and the left edge of the <paramref name="segmentElement"/>.
        /// </param>
        /// <param name="attachPosition">
        /// The <see cref="HorizontalSide"/> of the <paramref name="segmentElement"/> to attach the element providing a preview of the resized bounds.
        /// </param>
        public TrackSegmentResizeAdorner(VideoTimelineTrack adornedElement, VideoTimelineSegment segmentElement, double segmentElementLeftCoordinate, HorizontalSide attachPosition) : base(adornedElement)
        {
            _segmentElement = segmentElement;
            _segmentElementLeftCoordinate = segmentElementLeftCoordinate;
            _expandBackgroundBrush = segmentElement.Background;
            _contractBackgroundBrush = new SolidColorBrush(Colors.Transparent);
            _attachPosition = attachPosition;

            _previewBorder.Height = segmentElement.ActualHeight;
            _previewBorder.BorderBrush = segmentElement.BorderBrush;
            _previewBorder.Opacity = segmentElement.Opacity;
            _previewBorder.Cursor = Cursors.SizeWE;

            _previewBorderArrangeRect = new Rect(new Size(0d, _segmentElement.ActualHeight));
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _previewBorder.Arrange(_previewBorderArrangeRect);
            return base.ArrangeOverride(finalSize);
        }

        /// <summary>
        /// Invoked whenever the value of the <see cref="HorizontalOffset"/> property changes.
        /// </summary>
        private void OnHorizontalOffsetChanged()
        {
            double horizontalOffset = HorizontalOffset;

            if (_attachPosition == HorizontalSide.Left)
            {
                if (horizontalOffset < 0d)
                {
                    // Expanding

                    _previewBorderArrangeRect.X = _segmentElementLeftCoordinate + horizontalOffset;
                    _previewBorderArrangeRect.Width = Math.Abs(horizontalOffset) + _segmentElement.BorderThickness.Left;

                    _previewBorder.Background = _expandBackgroundBrush;
                    
                    Thickness borderThickness = _segmentElement.BorderThickness;
                    borderThickness.Right = 0d;
                    _previewBorder.BorderThickness = borderThickness;
                }
                else
                {
                    // Contracting

                    _previewBorderArrangeRect.X = _segmentElementLeftCoordinate;
                    _previewBorderArrangeRect.Width = horizontalOffset;

                    _previewBorder.Background = _contractBackgroundBrush;
                    _previewBorder.BorderThickness = new Thickness(0d, 0d, _segmentElement.BorderThickness.Left, 0d);
                }

                _previewToolTip.HorizontalOffset = _segmentElementLeftCoordinate + horizontalOffset;
            }
            else // _attachPosition == HorizontalSide.Right
            {
                if (horizontalOffset < 0d)
                {
                    // Contracting

                    _previewBorderArrangeRect.X = _segmentElementLeftCoordinate + _segmentElement.ActualWidth + horizontalOffset;
                    _previewBorderArrangeRect.Width = Math.Abs(horizontalOffset);

                    _previewBorder.Background = _contractBackgroundBrush;
                    _previewBorder.BorderThickness = new Thickness(_segmentElement.BorderThickness.Right, 0d, 0d, 0d);
                }
                else
                {
                    // Expanding

                    _previewBorderArrangeRect.X = _segmentElementLeftCoordinate + _segmentElement.ActualWidth - _segmentElement.BorderThickness.Right;
                    _previewBorderArrangeRect.Width = horizontalOffset + _segmentElement.BorderThickness.Right;

                    _previewBorder.Background = _expandBackgroundBrush;

                    Thickness borderThickness = _segmentElement.BorderThickness;
                    borderThickness.Left = 0d;
                    _previewBorder.BorderThickness = borderThickness;
                }

                _previewToolTip.HorizontalOffset = _segmentElementLeftCoordinate + _segmentElement.ActualWidth + horizontalOffset;
            }

            _previewToolTip.VerticalOffset = Mouse.GetPosition(this).Y;

            _previewBorder.Width = _previewBorderArrangeRect.Width;

            InvalidateMeasure();
        }
    }
}
