using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// An <see cref="Adorner"/> for visually previewing the <see cref="Fluent.GalleryItem"/>
    /// being dragged by the <see cref="DragRibbonGalleryItemsToTimelineBehavior"/>.
    /// </summary>
    public class RibbonGalleryBehaviorDragAdorner : Adorner
    {
        private readonly Image _previewImage;
        private Point _positionOffset;

        /// <summary>
        /// Gets or sets the distance between the top-left coordinate of the <see cref="Adorner.AdornedElement">adorned element</see>
        /// and the top-left coordinate of the <see cref="Image"/> providing a preview of the <see cref="Fluent.GalleryItem"/> being dragged.
        /// </summary>
        public Point PositionOffset
        {
            get => _positionOffset;
            set
            {
                if (_positionOffset != value)
                {
                    _positionOffset = value;

                    // Update position
                    AdornerLayer.GetAdornerLayer(AdornedElement)?.Update();
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="RibbonGalleryBehaviorDragAdorner"/> instance.
        /// </summary>
        /// <inheritdoc cref="Adorner(UIElement)"/>
        /// <param name="previewBitmap">
        /// A <see cref="BitmapSource"/> representation of the <see cref="Fluent.GalleryItem"/> being dragged.
        /// </param>
        /// <param name="positionOffset">The initial value of the <see cref="PositionOffset"/> property.</param>
        public RibbonGalleryBehaviorDragAdorner(UIElement adornedElement, BitmapSource previewBitmap, Point positionOffset = default) : base(adornedElement)
        {
            _positionOffset = positionOffset;

            _previewImage = new Image()
            {
                Width = previewBitmap.Width,
                Height = previewBitmap.Height,
                Source = previewBitmap
            };
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size constraint)
        {
            _previewImage.Measure(constraint);
            return _previewImage.DesiredSize;
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            _previewImage.Arrange(new Rect(finalSize));
            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc/>
        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _previewImage;
        }

        /// <inheritdoc/>
        protected override int VisualChildrenCount => 1;

        /// <inheritdoc/>
        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            GeneralTransformGroup result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(_positionOffset.X, _positionOffset.Y));
            return result;
        }
    }
}
