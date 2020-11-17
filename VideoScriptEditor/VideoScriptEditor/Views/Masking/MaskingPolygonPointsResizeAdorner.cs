using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.ViewModels.Masking;
using Debug = System.Diagnostics.Debug;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.Views.Masking
{
    /// <summary>
    /// An adorner for resizing the points of a masking polygon shape element.
    /// </summary>
    public class MaskingPolygonPointsResizeAdorner : MaskingShapeResizeAdornerBase
    {
        /// <summary>
        /// Identifies the <see cref="PolygonPoints" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="PolygonPoints" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty PolygonPointsProperty =
                DependencyProperty.Register(
                        nameof(PolygonPoints),
                        typeof(ObservableCollection<Point>),
                        typeof(MaskingPolygonPointsResizeAdorner),
                        new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnPolygonPointsPropertyChanged)));

        /// <summary>
        /// Gets or sets the collection of points that make up the polygon.
        /// </summary>
        public ObservableCollection<Point> PolygonPoints
        {
            get => (ObservableCollection<Point>)GetValue(PolygonPointsProperty);
            set => SetValue(PolygonPointsProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="PolygonPoints"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnPolygonPointsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MaskingPolygonPointsResizeAdorner adornerInstance = (MaskingPolygonPointsResizeAdorner)d;
            adornerInstance.OnPolygonPointsPropertyChanged(e.OldValue as ObservableCollection<Point>, e.NewValue as ObservableCollection<Point>);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MaskingPolygonPointsResizeAdorner"/> class.
        /// </summary>
        /// <inheritdoc cref="MaskingShapeResizeAdornerBase(FrameworkElement)"/>
        public MaskingPolygonPointsResizeAdorner(FrameworkElement adornedElement) : base(adornedElement)
        {
        }

        /// <inheritdoc/>
        /// <remarks>Arranges the adorner handles.</remarks>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Debug.Assert(_visualChildren.Count == (PolygonPoints?.Count).GetValueOrDefault(0));

            if (PolygonPoints?.Count > 0)
            {
                SizeI videoFrameSize = MaskingViewModel.ScriptVideoContext.VideoFrameSize;

                double xScaleFactor = finalSize.Width / videoFrameSize.Width;
                double yScaleFactor = finalSize.Height / videoFrameSize.Height;

                for (int i = 0; i < _visualChildren.Count; i++)
                {
                    Debug.Assert(_visualChildren[i] is Thumb);
                    ArrangeHandleThumb((Thumb)_visualChildren[i], PolygonPoints[i].Scale(xScaleFactor, yScaleFactor));
                }
            }

            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc/>
        protected override void OnHandleThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            Thumb handleThumb = (Thumb)sender;
            IMaskingViewModel maskingViewModel = MaskingViewModel;

            FrameworkElement adornedElement = (FrameworkElement)AdornedElement;
            double minSize = 0.001d;
            double elementWidth = Math.Max(minSize, adornedElement.ActualWidth);
            double elementHeight = Math.Max(minSize, adornedElement.ActualHeight);

            SizeI videoFrameSize = maskingViewModel.ScriptVideoContext.VideoFrameSize;

            double xDeltaScaleFactor = videoFrameSize.Width / elementWidth;
            double yDeltaScaleFactor = videoFrameSize.Height / elementHeight;
            Vector changeDelta = new Vector(e.HorizontalChange * xDeltaScaleFactor, e.VerticalChange * yDeltaScaleFactor);

            int pointIndex = _visualChildren.IndexOf(handleThumb);
            Point originalPoint = PolygonPoints[pointIndex];
            Point changedPoint = PointExt.ClampToBounds(originalPoint + changeDelta, videoFrameSize.Width, videoFrameSize.Height);
            if (originalPoint != changedPoint)
            {
                PolygonPoints[pointIndex] = changedPoint;
            }
        }

        /// <summary>
        /// Invoked whenever the value of the <see cref="PolygonPoints"/> property changes.
        /// </summary>
        /// <param name="oldPolygonPointsValue">The value of the <see cref="PolygonPoints"/> property before the change.</param>
        /// <param name="newPolygonPointsValue">The value of the <see cref="PolygonPoints"/> property after the change.</param>
        private void OnPolygonPointsPropertyChanged(ObservableCollection<Point> oldPolygonPointsValue, ObservableCollection<Point> newPolygonPointsValue)
        {
            if (oldPolygonPointsValue != null)
            {
                CollectionChangedEventManager.RemoveHandler(oldPolygonPointsValue, OnPolygonPointsCollectionChanged);
                RemoveAllHandleThumbs();
            }

            if (newPolygonPointsValue != null)
            {
                Debug.Assert(_visualChildren.Count == 0);
                for (int i = 0; i < newPolygonPointsValue.Count; i++)
                {
                    _visualChildren.Insert(i, CreateHandleThumb(Cursors.Hand));
                }

                CollectionChangedEventManager.AddHandler(newPolygonPointsValue, OnPolygonPointsCollectionChanged);
            }

            InvalidateArrange();
        }

        /// <summary>
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event for the <see cref="PolygonPoints"/> collection.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        private void OnPolygonPointsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1);
                    _visualChildren.Insert(e.NewStartingIndex, CreateHandleThumb(Cursors.Hand));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1);
                    RemoveHandleThumb(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveAllHandleThumbs();
                    break;
            }

            Debug.Assert(_visualChildren.Count == PolygonPoints.Count);

            InvalidateArrange();
        }

        /// <summary>
        /// Removes the handle <see cref="Thumb"/> at the specified visual child index.
        /// </summary>
        /// <param name="visualChildIndex">The visual child index of the <see cref="Thumb"/> to remove.</param>
        private void RemoveHandleThumb(int visualChildIndex)
        {
            Thumb handleThumb = _visualChildren[visualChildIndex] as Thumb;
            Debug.Assert(handleThumb != null);

            handleThumb.DragStarted -= OnHandleThumbDragStarted;
            handleThumb.DragDelta -= OnHandleThumbDragDelta;
            handleThumb.DragCompleted -= OnHandleThumbDragCompleted;

            BindingOperations.ClearAllBindings(handleThumb);

            _visualChildren.RemoveAt(visualChildIndex);
        }

        /// <summary>
        /// Removes all handle <see cref="Thumb"/>s from the adorner's visual collection.
        /// </summary>
        private void RemoveAllHandleThumbs()
        {
            for (int i = 0; _visualChildren.Count > 0;)
            {
                RemoveHandleThumb(i);
            }
        }
    }
}