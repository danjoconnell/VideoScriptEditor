using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;
using VideoScriptEditor.ViewModels.Timeline;
using VideoScriptEditor.Views.Common;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// The Video Timeline Segment view.
    /// </summary>
    public partial class VideoTimelineSegment : Control
    {
        private SegmentViewModelBase ViewModel => DataContext as SegmentViewModelBase;

        /// <summary>
        /// Identifies the <see cref="ParentTrackElement" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="ParentTrackElement" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty ParentTrackElementProperty = DependencyProperty.Register(
            nameof(ParentTrackElement),
            typeof(VideoTimelineTrack),
            typeof(VideoTimelineSegment),
            new PropertyMetadata(null));

        /// <summary>
        /// The parent Video Timeline Track element.
        /// </summary>
        public VideoTimelineTrack ParentTrackElement
        {
            get => (VideoTimelineTrack)GetValue(ParentTrackElementProperty);
            set => SetValue(ParentTrackElementProperty, value);
        }

        /// <summary>
        /// Event fires when the user clicks and drags the <see cref="Thumb"/> on the left or right side of the <see cref="VideoTimelineSegment"/>.
        /// </summary>
        public static readonly RoutedEvent HorizontalResizeDraggingEvent = EventManager.RegisterRoutedEvent("HorizontalResizeDragging", RoutingStrategy.Bubble, typeof(HorizontalResizeDraggingEventHandler), typeof(VideoTimelineSegment));

        /// <summary>
        /// Adds a handler for the <see cref="HorizontalResizeDraggingEvent"/> attached event.
        /// </summary>
        /// <param name="element">Element that listens to this event.</param>
        /// <param name="handler">Event handler to add.</param>
        public static void AddHorizontalResizeDraggingHandler(DependencyObject element, HorizontalResizeDraggingEventHandler handler)
        {
            (element as UIElement)?.AddHandler(HorizontalResizeDraggingEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the <see cref="HorizontalResizeDraggingEvent"/> attached event.
        /// </summary>
        /// <param name="element">Element that listens to this event.</param>
        /// <param name="handler">Event handler to remove.</param>
        public static void RemoveHorizontalResizeDraggingHandler(DependencyObject element, HorizontalResizeDraggingEventHandler handler)
        {
            (element as UIElement)?.RemoveHandler(HorizontalResizeDraggingEvent, handler);
        }

        /// <summary>
        /// Event fires when user the user releases the mouse button after dragging the <see cref="Thumb"/> on the left or right side of the <see cref="VideoTimelineSegment"/>.
        /// </summary>
        public static readonly RoutedEvent HorizontalResizeDragCompletedEvent = EventManager.RegisterRoutedEvent("HorizontalResizeDragCompleted", RoutingStrategy.Bubble, typeof(HorizontalResizeDragCompletedEventHandler), typeof(VideoTimelineSegment));

        /// <summary>
        /// Adds a handler for the <see cref="HorizontalResizeDragCompletedEvent"/> attached event.
        /// </summary>
        /// <param name="element">Element that listens to this event.</param>
        /// <param name="handler">Event handler to add.</param>
        public static void AddHorizontalResizeDragCompletedHandler(DependencyObject element, HorizontalResizeDragCompletedEventHandler handler)
        {
            (element as UIElement)?.AddHandler(HorizontalResizeDragCompletedEvent, handler);
        }

        /// <summary>
        /// Removes a handler for the <see cref="HorizontalResizeDragCompletedEvent"/> attached event.
        /// </summary>
        /// <param name="element">Element that listens to this event.</param>
        /// <param name="handler">Event handler to remove.</param>
        public static void RemoveHorizontalResizeDragCompletedHandler(DependencyObject element, HorizontalResizeDragCompletedEventHandler handler)
        {
            (element as UIElement)?.RemoveHandler(HorizontalResizeDragCompletedEvent, handler);
        }

        /// <summary>
        /// Creates a new <see cref="VideoTimelineSegment"/> element.
        /// </summary>
        public VideoTimelineSegment()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the <see cref="Thumb.DragDelta"/> routed event for a border <see cref="Thumb"/> element.
        /// </summary>
        /// <remarks>
        /// Forwards the event data to the <see cref="VideoTimelineView"/>
        /// via the <see cref="HorizontalResizeDraggingEvent"/> attached event.
        /// </remarks>
        /// <inheritdoc cref="DragDeltaEventHandler"/>
        private void OnBorderThumbElementDragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb borderThumb = (Thumb)sender;
            HorizontalSide borderSide = ConvertDockValueToHorizontalSide(DockPanel.GetDock(borderThumb));
            RaiseEvent(new HorizontalResizeDragEventArgs(borderSide, e.HorizontalChange));

            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="Thumb.DragCompleted"/> routed event for a border <see cref="Thumb"/> element.
        /// </summary>
        /// <remarks>
        /// Forwards the event data to the <see cref="VideoTimelineView"/>
        /// via the <see cref="HorizontalResizeDragCompletedEvent"/> attached event.
        /// </remarks>
        /// <inheritdoc cref="DragCompletedEventHandler"/>
        private void OnBorderThumbElementDragCompleted(object sender, DragCompletedEventArgs e)
        {
            Thumb borderThumb = (Thumb)sender;
            HorizontalSide borderSide = ConvertDockValueToHorizontalSide(DockPanel.GetDock(borderThumb));
            RaiseEvent(new HorizontalResizeDragCompletedEventArgs(borderSide, e.HorizontalChange, e.Canceled));

            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewMouseLeftButtonDown"/> routed event
        /// for a key frame <see cref="Rectangle"/> element.
        /// </summary>
        /// <remarks>Sets the current zero-based frame number of the video to the frame number of the key frame.</remarks>
        /// <inheritdoc cref="MouseButtonEventHandler"/>
        private void OnKeyFrameRectanglePreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                e.Handled = true;

                if ((sender as Rectangle)?.DataContext is KeyFrameViewModelBase keyFrameViewModel)
                {
                    ViewModel.ScriptVideoContext.FrameNumber = keyFrameViewModel.FrameNumber;
                }
            }
        }

        /// <summary>
        /// Converts a <see cref="DockPanel.DockProperty"/> value
        /// to a corresponding <see cref="HorizontalSide"/> enum value.
        /// </summary>
        /// <param name="dockValue">The <see cref="Dock"/> value to convert to a <see cref="HorizontalSide"/>.</param>
        /// <returns>The corresponding <see cref="HorizontalSide"/> enum value.</returns>
        private HorizontalSide ConvertDockValueToHorizontalSide(Dock dockValue) => dockValue switch
        {
            Dock.Left => HorizontalSide.Left,
            Dock.Right => HorizontalSide.Right,
            _ // default
                => throw new InvalidEnumArgumentException($"Unsupported '{dockValue}' DockPanel.Dock enum value."),
        };
    }
}
