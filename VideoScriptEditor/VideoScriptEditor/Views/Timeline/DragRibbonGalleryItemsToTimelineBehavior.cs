using Fluent;
using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VideoScriptEditor.Commands;
using VideoScriptEditor.Views.Common;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// Behavior allowing <see cref="InRibbonGallery"/> <see cref="GalleryItem"/>s
    /// representing segment types to be dragged by the mouse to the Timeline.
    /// </summary>
    /// <remarks>
    /// Expects the <see cref="InRibbonGallery"/> to be bound to a collection of <see cref="Enum"/> values
    /// describing the type of segments that can be added to the Timeline. 
    /// </remarks>
    public class DragRibbonGalleryItemsToTimelineBehavior : Behavior<InRibbonGallery>
    {
        private bool _isDraggingItem = false;
        private Point? _itemDragOrigin;
        private RibbonGalleryBehaviorDragAdorner _itemDragAdorner = null;

        /// <summary>
        /// Identifies the <see cref="DragItemActionData" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="DragItemActionData" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty DragItemActionDataProperty = DependencyProperty.Register(
            nameof(DragItemActionData),
            typeof(AddTrackSegmentCommandParameters),
            typeof(DragRibbonGalleryItemsToTimelineBehavior),
            new FrameworkPropertyMetadata(null)
        );

        /// <summary>
        /// Encapsulated data parameters for dragging an item to the Timeline.
        /// </summary>
        public AddTrackSegmentCommandParameters DragItemActionData
        {
            get => (AddTrackSegmentCommandParameters)GetValue(DragItemActionDataProperty);
            set => SetValue(DragItemActionDataProperty, value);
        }

        /// <summary>
        /// Creates a new <see cref="DragRibbonGalleryItemsToTimelineBehavior"/> instance.
        /// </summary>
        public DragRibbonGalleryItemsToTimelineBehavior() : base()
        {
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(OnRibbonGalleryPreviewMouseDown), true);
            AssociatedObject.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnRibbonGalleryPreviewMouseLeftButtonUp), true);
            AssociatedObject.AddHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(OnRibbonGalleryPreviewMouseMove), true);
            AssociatedObject.AddHandler(UIElement.PreviewDragEnterEvent, new DragEventHandler(OnRibbonGalleryPreviewDragEnter), true);
            AssociatedObject.AddHandler(UIElement.PreviewDragOverEvent, new DragEventHandler(OnRibbonGalleryPreviewDragOver), true);
            AssociatedObject.AddHandler(UIElement.PreviewDragLeaveEvent, new DragEventHandler(OnRibbonGalleryPreviewDragLeave), true);
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(UIElement.PreviewMouseDownEvent, new MouseButtonEventHandler(OnRibbonGalleryPreviewMouseDown));
            AssociatedObject.RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnRibbonGalleryPreviewMouseLeftButtonUp));
            AssociatedObject.RemoveHandler(UIElement.PreviewMouseMoveEvent, new MouseEventHandler(OnRibbonGalleryPreviewMouseMove));
            AssociatedObject.RemoveHandler(UIElement.PreviewDragEnterEvent, new DragEventHandler(OnRibbonGalleryPreviewDragEnter));
            AssociatedObject.RemoveHandler(UIElement.PreviewDragOverEvent, new DragEventHandler(OnRibbonGalleryPreviewDragOver));
            AssociatedObject.RemoveHandler(UIElement.PreviewDragLeaveEvent, new DragEventHandler(OnRibbonGalleryPreviewDragLeave));
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewMouseDown"/> routed event for the <see cref="Behavior{InRibbonGallery}.AssociatedObject"/>.
        /// </summary>
        /// <remarks>
        /// If the left mouse button is pressed while the cursor is over a <see cref="GalleryItem"/>,
        /// the current mouse position is the drag origin point.
        /// </remarks>
        /// <inheritdoc cref="MouseButtonEventHandler"/>
        private void OnRibbonGalleryPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && AssociatedObject.ContainerFromElement((DependencyObject)e.OriginalSource) is GalleryItem)
            {
                _itemDragOrigin = e.GetPosition(AssociatedObject);
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewMouseLeftButtonUp"/> routed event for the <see cref="Behavior{InRibbonGallery}.AssociatedObject"/>.
        /// </summary>
        /// <remarks>
        /// If a <see cref="GalleryItem"/> representing a segment type is being dragged, the drag operation is ended.
        /// </remarks>
        /// <inheritdoc cref="MouseButtonEventHandler"/>
        private void OnRibbonGalleryPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingItem)
            {
                EndItemDrag();
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewMouseMove"/> routed event for the <see cref="Behavior{InRibbonGallery}.AssociatedObject"/>.
        /// </summary>
        /// <remarks>
        /// If the left mouse button is pressed while moving the mouse over or away from a <see cref="GalleryItem"/>,
        /// a dragging operation is started.
        /// </remarks>
        /// <inheritdoc cref="MouseEventHandler"/>
        private void OnRibbonGalleryPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingItem && e.LeftButton == MouseButtonState.Pressed && _itemDragOrigin.HasValue)
            {
                if (AssociatedObject.ContainerFromElement((DependencyObject)e.OriginalSource) is GalleryItem galleryItem)
                {
                    Point position = e.GetPosition(AssociatedObject);
                    if (Math.Abs(position.X - _itemDragOrigin.Value.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(position.Y - _itemDragOrigin.Value.Y) >= SystemParameters.MinimumVerticalDragDistance)
                    {
                        _isDraggingItem = true;

                        AddTrackSegmentCommandParameters dragItemActionData = DragItemActionData;
                        Debug.Assert(dragItemActionData != null);

                        Debug.Assert(galleryItem.DataContext is Enum);
                        dragItemActionData.SegmentTypeDescriptor = (Enum)galleryItem.DataContext;

                        DataObject dataObject = new DataObject(typeof(WeakReference<AddTrackSegmentCommandParameters>), new WeakReference<AddTrackSegmentCommandParameters>(dragItemActionData));

                        FrameworkElement bitmapSourceElement = galleryItem.FindVisualChild<ContentPresenter>();
                        if (bitmapSourceElement == null)
                        {
                            bitmapSourceElement = galleryItem;
                        }

                        RenderTargetBitmap bitmap = new RenderTargetBitmap((int)Math.Round(bitmapSourceElement.ActualWidth + 0.5), (int)Math.Round(bitmapSourceElement.ActualHeight + 0.5), 96, 96, PixelFormats.Pbgra32);
                        bitmap.Render(bitmapSourceElement);

                        dataObject.SetImage(bitmap);

                        DragDrop.DoDragDrop(AssociatedObject, dataObject, DragDropEffects.Copy);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewDragEnter"/> routed event for the <see cref="Behavior{InRibbonGallery}.AssociatedObject"/>.
        /// </summary>
        /// <remarks>
        /// Initializes and displays the <see cref="_itemDragAdorner"/>.
        /// </remarks>
        /// <inheritdoc cref="DragEventHandler"/>
        private void OnRibbonGalleryPreviewDragEnter(object sender, DragEventArgs e)
        {
            if (_isDraggingItem && _itemDragAdorner == null && e.Data.GetDataPresent(DataFormats.Bitmap, false))
            {
                BitmapSource previewBitmap = (BitmapSource)e.Data.GetData(DataFormats.Bitmap, true);
                _itemDragAdorner = new RibbonGalleryBehaviorDragAdorner(AssociatedObject, previewBitmap, e.GetPosition(AssociatedObject));

                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(AssociatedObject);
                adornerLayer.Add(_itemDragAdorner);
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewDragOver"/> routed event for the <see cref="Behavior{InRibbonGallery}.AssociatedObject"/>.
        /// </summary>
        /// <remarks>Updates the position of the <see cref="_itemDragAdorner"/>.</remarks>
        /// <inheritdoc cref="DragEventHandler"/>
        private void OnRibbonGalleryPreviewDragOver(object sender, DragEventArgs e)
        {
            if (_isDraggingItem)
            {
                _itemDragAdorner.PositionOffset = e.GetPosition(AssociatedObject);

                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewDragLeave"/> routed event for the <see cref="Behavior{InRibbonGallery}.AssociatedObject"/>.
        /// </summary>
        /// <remarks>If a <see cref="GalleryItem"/> representing a segment type is being dragged, the drag operation is canceled.</remarks>
        /// <inheritdoc cref="DragEventHandler"/>
        private void OnRibbonGalleryPreviewDragLeave(object sender, DragEventArgs e)
        {
            if (_isDraggingItem)
            {
                RemoveItemDragAdorner();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Removes the <see cref="_itemDragAdorner"/> from the
        /// <see cref="Behavior{InRibbonGallery}.AssociatedObject"/>'s <see cref="AdornerLayer"/>.
        /// </summary>
        private void RemoveItemDragAdorner()
        {
            if (_itemDragAdorner != null)
            {
                AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(AssociatedObject);
                adornerLayer.Remove(_itemDragAdorner);

                _itemDragAdorner = null;
            }
        }

        /// <summary>
        /// Ends a <see cref="GalleryItem"/> drag operation and cleans up resources.
        /// </summary>
        private void EndItemDrag()
        {
            RemoveItemDragAdorner();

            _isDraggingItem = false;
            _itemDragOrigin = null;
        }
    }
}
