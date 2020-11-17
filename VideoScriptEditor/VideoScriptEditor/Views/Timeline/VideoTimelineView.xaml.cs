using Prism.Common;
using Prism.Regions;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using VideoScriptEditor.Commands;
using VideoScriptEditor.ViewModels.Timeline;
using VideoScriptEditor.Views.Common;
using SegmentViewModelWeakReference = System.WeakReference<VideoScriptEditor.ViewModels.Timeline.SegmentViewModelBase>;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views.Timeline
{
    /// <summary>
    /// The Video Timeline view.
    /// </summary>
    public partial class VideoTimelineView : UserControl
    {
        private bool _isDraggingSegment = false;
        private Point? _segmentDragOrigin;
        private TrackSegmentDragAdorner _segmentDragAdorner = null;

        private bool _isResizingSegment = false;
        private TrackSegmentResizeAdorner _segmentResizeAdorner = null;

        private IVideoTimelineViewModel ViewModel => DataContext as IVideoTimelineViewModel;

        /// <summary>
        /// Creates a new <see cref="VideoTimelineView"/> element.
        /// </summary>
        public VideoTimelineView()
        {
            InitializeComponent();

            RegionContext.GetObservableContext(this).PropertyChanged += OnPrismRegionContextChanged;
        }

        /// <summary>
        /// Handles the <see cref="ObservableObject{T}.PropertyChanged"/> event for the <see cref="RegionContext"/>.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedEventHandler"/>
        private void OnPrismRegionContextChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ObservableObject<object>.Value) && sender is ObservableObject<object> regionContext)
            {
                ViewModel.TimelineSegmentProvidingViewModel = regionContext.Value as ITimelineSegmentProvidingViewModel;
            }
        }

        /// <summary>
        /// Handles the <see cref="RangeBase.ValueChanged"/> routed event for the <see cref="timelineSlider"/>.
        /// </summary>
        /// <remarks>
        /// When the <see cref="Slider"/> thumb reaches the left or right edge of the <see cref="rootScrollViewer"/>,
        /// the timeline will scroll forward or backward in <see cref="ScrollViewer.ViewportWidth"/> amounts.
        /// The <see cref="Slider"/> thumb will appear to go from left side to right (or right side to left).
        /// </remarks>
        /// <inheritdoc cref="RoutedPropertyChangedEventHandler{T}"/>
        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;

            double pixelsPerFrame = rootScrollViewer.ExtentWidth / slider.Maximum;
            double targetScrollOffset = e.NewValue * pixelsPerFrame;

            if (targetScrollOffset < rootScrollViewer.HorizontalOffset /* Visible Left */
                || targetScrollOffset > (rootScrollViewer.HorizontalOffset + rootScrollViewer.ViewportWidth) /* Visible Right */)
            {
                if (targetScrollOffset < rootScrollViewer.HorizontalOffset)
                {
                    // Scrolling left
                    targetScrollOffset -= rootScrollViewer.ViewportWidth;
                }

                // Clamp to ScrollViewer bounds
                targetScrollOffset = Math.Clamp(targetScrollOffset, 0d, rootScrollViewer.ExtentWidth);

                // Scroll to target offset
                rootScrollViewer.ScrollToHorizontalOffset(targetScrollOffset);
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.MouseLeftButtonDown"/> routed event for a <see cref="VideoTimelineSegment"/> element.
        /// </summary>
        /// <remarks>
        /// Can't use Preview Mouse events as the <see cref="VideoTimelineSegment"/> element's border <see cref="Thumb"/>s
        /// have to have the first chance to handle the event.
        /// </remarks>
        /// <inheritdoc cref="MouseButtonEventHandler"/>
        private void OnTimelineSegmentMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _segmentDragOrigin = e.GetPosition(tracksListBox);
        }

        /// <summary>
        /// Handles the <see cref="UIElement.MouseMove"/> routed event for a <see cref="VideoTimelineSegment"/> element.
        /// </summary>
        /// <remarks>
        /// Can't use Preview Mouse events as the <see cref="VideoTimelineSegment"/> element's border <see cref="Thumb"/>s
        /// have to have the first chance to handle the event.
        /// </remarks>
        /// <inheritdoc cref="MouseEventHandler"/>
        private void OnTimelineSegmentMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingSegment && e.LeftButton == MouseButtonState.Pressed && _segmentDragOrigin.HasValue)
            {
                Point mousePosition = e.GetPosition(tracksListBox);

                if (Math.Abs(mousePosition.X - _segmentDragOrigin.Value.X) >= SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(mousePosition.Y - _segmentDragOrigin.Value.Y) >= SystemParameters.MinimumVerticalDragDistance)
                {
                    VideoTimelineSegment draggedSegmentElement = sender as VideoTimelineSegment;
                    Debug.Assert(draggedSegmentElement != null);

                    SegmentViewModelBase draggedSegmentViewModel = draggedSegmentElement.DataContext as SegmentViewModelBase;
                    Debug.Assert(draggedSegmentViewModel != null);

                    _isDraggingSegment = true;

                    DataObject dataObject = new DataObject();
                    dataObject.SetData(typeof(SegmentViewModelWeakReference), new SegmentViewModelWeakReference(draggedSegmentViewModel));
                    dataObject.SetData(typeof(Size), draggedSegmentElement.RenderSize);

                    DragDrop.DoDragDrop(tracksListBox, dataObject, DragDropEffects.Copy | DragDropEffects.Move);
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="VideoTimelineSegment.HorizontalResizeDraggingEvent"/> attached event
        /// for a <see cref="VideoTimelineSegment"/> element.
        /// </summary>
        /// <inheritdoc cref="HorizontalResizeDraggingEventHandler"/>
        private void OnTrackSegmentHorizontalResizeDragging(object sender, HorizontalResizeDragEventArgs e)
        {
            e.Handled = true;

            VideoTimelineSegment segmentElement = e.OriginalSource as VideoTimelineSegment;
            Debug.Assert(segmentElement != null);

            VideoTimelineTrack trackElement = segmentElement.ParentTrackElement;

            SegmentViewModelBase segmentViewModel = segmentElement.DataContext as SegmentViewModelBase;
            Debug.Assert(segmentViewModel != null);

            int frameDiff = (int)Math.Round(e.HorizontalChange / trackElement.PixelsPerFrame);
            if (frameDiff == 0)
            {
                return;
            }

            int proposedStartFrame = segmentViewModel.StartFrame;
            int proposedEndFrame = segmentViewModel.EndFrame;
            double framePixelOffset = trackElement.PixelsPerFrame * frameDiff;
            bool isContracting = false;
            Rect segmentElementClipRect;

            if (e.HorizontalSide == HorizontalSide.Left)
            {
                proposedStartFrame += frameDiff;
                if (!ViewModel.CanChangeTrackSegmentStartFrame(segmentViewModel, proposedStartFrame))
                {
                    return;
                }

                if (frameDiff > 0)
                {
                    isContracting = true;
                    segmentElementClipRect = new Rect(framePixelOffset, 0d, segmentElement.ActualWidth - framePixelOffset, segmentElement.ActualHeight);
                }
            }
            else // e.HorizontalSide == HorizontalSide.Right
            {
                proposedEndFrame += frameDiff;
                if (!ViewModel.CanChangeTrackSegmentEndFrame(segmentViewModel, proposedEndFrame))
                {
                    return;
                }

                if (frameDiff < 0)
                {
                    isContracting = true;

                    segmentElementClipRect = new Rect(
                        new Size(segmentElement.ActualWidth + framePixelOffset, segmentElement.ActualHeight)
                    );
                }
            }

            if (isContracting)
            {
                if (segmentElement.Clip is RectangleGeometry rectangleGeometry)
                {
                    rectangleGeometry.Rect = segmentElementClipRect;
                }
                else
                {
                    segmentElement.Clip = new RectangleGeometry(segmentElementClipRect);
                }
            }
            else
            {
                segmentElement.Clip = null;
            }

            if (_segmentResizeAdorner == null)
            {
                UIElement segmentElementContainer = segmentElement.TemplatedParent as UIElement;
                Debug.Assert(segmentElementContainer != null);

                _segmentResizeAdorner = new TrackSegmentResizeAdorner(adornedElement: trackElement, segmentElement,
                                                                      segmentElementLeftCoordinate: Canvas.GetLeft(segmentElementContainer),
                                                                      attachPosition: e.HorizontalSide);
                _segmentResizeAdorner.Attach();

                _isResizingSegment = true;
            }

            _segmentResizeAdorner.HorizontalOffset = framePixelOffset;
            _segmentResizeAdorner.ToolTipData.StartFrame = proposedStartFrame;
            _segmentResizeAdorner.ToolTipData.EndFrame = proposedEndFrame;
        }

        /// <summary>
        /// Handles the <see cref="VideoTimelineSegment.HorizontalResizeDragCompletedEvent"/> attached event
        /// for a <see cref="VideoTimelineSegment"/> element.
        /// </summary>
        /// <inheritdoc cref="HorizontalResizeDragCompletedEventHandler"/>
        private void OnTrackSegmentHorizontalResizeDragCompleted(object sender, HorizontalResizeDragCompletedEventArgs e)
        {
            if (_isResizingSegment)
            {
                VideoTimelineSegment segmentElement = e.OriginalSource as VideoTimelineSegment;
                Debug.Assert(segmentElement != null);

                VideoTimelineTrack trackElement = segmentElement.ParentTrackElement;

                SegmentViewModelBase segmentViewModel = segmentElement.DataContext as SegmentViewModelBase;
                Debug.Assert(segmentViewModel != null);

                int frameDiff = (int)Math.Round(_segmentResizeAdorner.HorizontalOffset / trackElement.PixelsPerFrame);
                if (frameDiff != 0)
                {
                    if (e.HorizontalSide == HorizontalSide.Left)
                    {
                        int newStartFrame = segmentViewModel.StartFrame + frameDiff;
                        ViewModel.ChangeTrackSegmentStartFrame(segmentViewModel, newStartFrame);
                    }
                    else // e.HorizontalSide == HorizontalSide.Right
                    {
                        int newEndFrame = segmentViewModel.EndFrame + frameDiff;
                        ViewModel.ChangeTrackSegmentEndFrame(segmentViewModel, newEndFrame);
                    }
                }

                segmentElement.Clip = null;

                _segmentResizeAdorner?.Detach();
                _segmentResizeAdorner = null;

                _isResizingSegment = false;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewMouseLeftButtonUp"/> routed event for the <see cref="tracksListBox"/>.
        /// </summary>
        /// <remarks>If a <see cref="VideoTimelineSegment"/> is being dragged, the drag operation is ended.</remarks>
        /// <inheritdoc cref="MouseButtonEventHandler"/>
        private void OnTracksListBoxPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingSegment)
            {
                EndSegmentDrag();
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewDragEnter"/> routed event for the <see cref="tracksListBox"/>.
        /// </summary>
        /// <remarks>Begins a dragging operation for a <see cref="VideoTimelineSegment"/>.</remarks>
        /// <inheritdoc cref="DragEventHandler"/>
        private void OnTracksListBoxPreviewDragEnter(object sender, DragEventArgs e)
        {
            if (_segmentDragAdorner == null)
            {
                Size? adornerRectangleSize = null;
                if (e.Data.GetDataPresent(typeof(SegmentViewModelWeakReference)) && e.Data.GetDataPresent(typeof(Size)))
                {
                    adornerRectangleSize = (Size)e.Data.GetData(typeof(Size));
                    _isDraggingSegment = true;
                }
                else if (e.Data.GetDataPresent(typeof(WeakReference<AddTrackSegmentCommandParameters>)))
                {
                    if (!(e.Data.GetData(typeof(WeakReference<AddTrackSegmentCommandParameters>)) is WeakReference<AddTrackSegmentCommandParameters> addTrackSegmentActionDataRef)
                        || !addTrackSegmentActionDataRef.TryGetTarget(out AddTrackSegmentCommandParameters addTrackSegmentActionData))
                    {
                        throw new InvalidOperationException($"Invalid reference to a {nameof(AddTrackSegmentCommandParameters)} object.");
                    }

                    Debug.Assert(tracksListBox.HasItems);

                    ListBoxItem listBoxItem = tracksListBox.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                    Debug.Assert(listBoxItem != null);

                    VideoTimelineTrack trackElement = listBoxItem.FindVisualChild<VideoTimelineTrack>();
                    Debug.Assert(trackElement != null);

                    double trackPixelsPerFrame = trackElement.PixelsPerFrame;
                    adornerRectangleSize = new Size(trackPixelsPerFrame * addTrackSegmentActionData.FrameDuration, trackElement.ActualHeight);
                    _segmentDragOrigin = trackElement.TranslatePoint(new Point((addTrackSegmentActionData.FrameDuration / 2) * trackPixelsPerFrame, 0d), tracksListBox);

                    _isDraggingSegment = true;
                }
                else
                {
                    _isDraggingSegment = false;
                }

                if (_isDraggingSegment && adornerRectangleSize.HasValue)
                {
                    _segmentDragAdorner = new TrackSegmentDragAdorner(adornedElement: tracksListBox,
                                                                      segmentElementSize: adornerRectangleSize.Value);
                    _segmentDragAdorner.Attach();
                }
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewDragLeave"/> routed event for the <see cref="tracksListBox"/>.
        /// </summary>
        /// <remarks>If a <see cref="VideoTimelineSegment"/> is being dragged, the drag operation is canceled.</remarks>
        /// <inheritdoc cref="DragEventHandler"/>
        private void OnTracksListBoxPreviewDragLeave(object sender, DragEventArgs e)
        {
            if (_isDraggingSegment)
            {
                _segmentDragAdorner?.Detach();
                _segmentDragAdorner = null;

                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewDragOver"/> routed event for the <see cref="tracksListBox"/>.
        /// </summary>
        /// <remarks>Performs validation for a <see cref="VideoTimelineSegment"/> dragging operation.</remarks>
        /// <inheritdoc cref="DragEventHandler"/>
        private void OnTracksListBoxPreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            if (_isDraggingSegment && _segmentDragOrigin.HasValue)
            {
                SegmentViewModelBase segmentViewModel = null;
                int? draggedItemStartFrame = null;
                int draggedItemFrameDuration = 0;

                if (e.Data.GetDataPresent(typeof(SegmentViewModelWeakReference)))
                {
                    if (!(e.Data.GetData(typeof(SegmentViewModelWeakReference)) is SegmentViewModelWeakReference segmentViewModelRef)
                        || !segmentViewModelRef.TryGetTarget(out segmentViewModel))
                    {
                        throw new InvalidOperationException($"Invalid reference to a {nameof(SegmentViewModelBase)} object.");
                    }

                    draggedItemStartFrame = segmentViewModel.StartFrame;
                    draggedItemFrameDuration = segmentViewModel.FrameDuration;
                }
                else if (e.Data.GetDataPresent(typeof(WeakReference<AddTrackSegmentCommandParameters>)))
                {
                    if (!(e.Data.GetData(typeof(WeakReference<AddTrackSegmentCommandParameters>)) is WeakReference<AddTrackSegmentCommandParameters> addTrackSegmentActionDataRef)
                        || !addTrackSegmentActionDataRef.TryGetTarget(out AddTrackSegmentCommandParameters addTrackSegmentActionData))
                    {
                        throw new InvalidOperationException($"Invalid reference to a {nameof(AddTrackSegmentCommandParameters)} object.");
                    }

                    draggedItemStartFrame = 0;
                    draggedItemFrameDuration = addTrackSegmentActionData.FrameDuration;
                }

                if (draggedItemStartFrame.HasValue
                    && TryFindTrackElementAndClosestFrameNumber(e, draggedItemStartFrame.Value,
                                                                out VideoTimelineTrack timelineTrackElement,
                                                                out int closestFrameNumber))
                {
                    if (e.AllowedEffects == DragDropEffects.Copy
                        || (e.AllowedEffects.HasFlag(DragDropEffects.Copy) && e.KeyStates.HasFlag(DragDropKeyStates.ControlKey)))
                    {
                        if (ViewModel.CanAddTrackSegment(timelineTrackElement.TrackNumber, closestFrameNumber, draggedItemFrameDuration))
                        {
                            e.Effects = DragDropEffects.Copy;
                        }
                    }
                    else if (e.AllowedEffects.HasFlag(DragDropEffects.Move)
                             && ViewModel.CanMoveTrackSegment(segmentViewModel, timelineTrackElement.TrackNumber, closestFrameNumber))
                    {
                        e.Effects = DragDropEffects.Move;
                    }

                    _segmentDragAdorner.PositionOffset = timelineTrackElement.TranslatePoint(new Point(closestFrameNumber * timelineTrackElement.PixelsPerFrame, 0d),
                                                                                             tracksListBox);

                    _segmentDragAdorner.ToolTipData.StartFrame = closestFrameNumber;
                    _segmentDragAdorner.ToolTipData.EndFrame = closestFrameNumber + draggedItemFrameDuration - 1;
                }

                _segmentDragAdorner.IsDragValid = e.Effects == DragDropEffects.Move || e.Effects == DragDropEffects.Copy;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the <see cref="UIElement.PreviewDrop"/> routed event for the <see cref="tracksListBox"/>.
        /// </summary>
        /// <remarks>Completes a <see cref="VideoTimelineSegment"/> dragging operation.</remarks>
        /// <inheritdoc cref="DragEventHandler"/>
        private void OnTracksListBoxPreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Effects != DragDropEffects.None)
            {
                bool isAdding = false;
                bool isCopying = false;
                bool isMoving = false;

                SegmentViewModelBase segmentViewModel = null;
                Enum addableSegmentType = null;
                int? draggedItemStartFrame = null;
                int draggedItemFrameDuration = 0;

                if (e.Data.GetDataPresent(typeof(SegmentViewModelWeakReference)))
                {
                    if (!(e.Data.GetData(typeof(SegmentViewModelWeakReference)) is SegmentViewModelWeakReference segmentViewModelRef)
                        || !segmentViewModelRef.TryGetTarget(out segmentViewModel))
                    {
                        throw new InvalidOperationException($"Invalid reference to a {nameof(SegmentViewModelBase)} object.");
                    }

                    draggedItemStartFrame = segmentViewModel.StartFrame;
                    draggedItemFrameDuration = segmentViewModel.FrameDuration;

                    if (e.Effects.HasFlag(DragDropEffects.Copy) && e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
                    {
                        isCopying = true;
                    }
                    else if (e.Effects.HasFlag(DragDropEffects.Move))
                    {
                        isMoving = true;
                    }
                }
                else if (e.Data.GetDataPresent(typeof(WeakReference<AddTrackSegmentCommandParameters>)))
                {
                    if (!(e.Data.GetData(typeof(WeakReference<AddTrackSegmentCommandParameters>)) is WeakReference<AddTrackSegmentCommandParameters> addTrackSegmentActionDataRef)
                        || !addTrackSegmentActionDataRef.TryGetTarget(out AddTrackSegmentCommandParameters addTrackSegmentActionData))
                    {
                        throw new InvalidOperationException($"Invalid reference to a {nameof(AddTrackSegmentCommandParameters)} object.");
                    }

                    addableSegmentType = addTrackSegmentActionData.SegmentTypeDescriptor;
                    draggedItemStartFrame = 0;
                    draggedItemFrameDuration = addTrackSegmentActionData.FrameDuration;
                    isAdding = true;
                }

                if (draggedItemStartFrame.HasValue
                    && TryFindTrackElementAndClosestFrameNumber(e, draggedItemStartFrame.Value,
                                                                out VideoTimelineTrack timelineTrackElement,
                                                                out int closestFrameNumber))
                {
                    if (isAdding)
                    {
                        ViewModel.AddTrackSegment(addableSegmentType, timelineTrackElement.TrackNumber,
                                                  closestFrameNumber, draggedItemFrameDuration);
                    }
                    else if (isCopying)
                    {
                        ViewModel.CopyTrackSegment(segmentViewModel, timelineTrackElement.TrackNumber, closestFrameNumber);
                    }
                    else if (isMoving)
                    {
                        ViewModel.MoveTrackSegment(segmentViewModel, timelineTrackElement.TrackNumber, closestFrameNumber);
                    }
                }
            }

            EndSegmentDrag();

            e.Handled = true;
        }

        /// <summary>
        /// Tries to find the <see cref="VideoTimelineTrack"/> element and closest frame number
        /// to the current drag location.
        /// </summary>
        /// <param name="dragEventArgs">(In) The <see cref="DragEventArgs"/> for the drag operation.</param>
        /// <param name="draggedItemStartFrame">(In) The Start Frame of the <see cref="VideoTimelineSegment"/> being dragged.</param>
        /// <param name="timelineTrackElement">(Out) If successful, the <see cref="VideoTimelineTrack"/> element.</param>
        /// <param name="closestFrameNumber">(Out) If successful, the closest frame number.</param>
        /// <returns>A <see cref="bool"/> value indicating success or failure.</returns>
        private bool TryFindTrackElementAndClosestFrameNumber(DragEventArgs dragEventArgs, int draggedItemStartFrame, out VideoTimelineTrack timelineTrackElement, out int closestFrameNumber)
        {
            closestFrameNumber = -1;

            timelineTrackElement = dragEventArgs.OriginalSource as VideoTimelineTrack;
            if (timelineTrackElement == null)
            {
                if (!(tracksListBox.ContainerFromElement((DependencyObject)dragEventArgs.OriginalSource) is ListBoxItem listBoxItem)
                    || (timelineTrackElement = listBoxItem.FindVisualChild<VideoTimelineTrack>()) == null)
                {
                    return false;
                }
            }

            Point mousePositionInListBox = dragEventArgs.GetPosition(tracksListBox);
            Vector dragPositionOffset = mousePositionInListBox - _segmentDragOrigin.Value;

            double trackPixelsPerFrame = timelineTrackElement.PixelsPerFrame;
            double segmentPixelLeftPosition = trackPixelsPerFrame * draggedItemStartFrame;
            closestFrameNumber = (int)Math.Round((segmentPixelLeftPosition + dragPositionOffset.X) / trackPixelsPerFrame);

            return true;
        }

        /// <summary>
        /// Ends a <see cref="VideoTimelineSegment"/> drag operation and cleans up resources.
        /// </summary>
        private void EndSegmentDrag()
        {
            _segmentDragAdorner?.Detach();
            _segmentDragAdorner = null;

            _isDraggingSegment = false;
            _segmentDragOrigin = null;
        }
    }
}
