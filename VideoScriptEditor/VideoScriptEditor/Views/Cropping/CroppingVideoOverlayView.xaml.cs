using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VideoScriptEditor.PrismExtensions;
using VideoScriptEditor.ViewModels.Cropping;
using VideoScriptEditor.Views.Common;
using ITimelineSegmentProvidingViewModel = VideoScriptEditor.ViewModels.Timeline.ITimelineSegmentProvidingViewModel;
using SizeI = System.Drawing.Size;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views.Cropping
{
    /// <summary>
    /// The Cropping Video Overlay view.
    /// </summary>
    /// <remarks>
    /// Uses a <see cref="GeometryGroup"/> as a pseudo-<see cref="ItemsControl"/>
    /// in the child <see cref="Rectangle"/>'s <see cref="UIElement.OpacityMask"/>.
    /// </remarks>
    [DependentView(RegionNames.VideoDetailsRegion, typeof(CroppingDetailsView))]
    [DependentView(RegionNames.RibbonGroupRegion, typeof(CroppingRibbonGroupView))]
    public partial class CroppingVideoOverlayView : UserControl, IViewSharesDataContext
    {
        private readonly VideoOverlayGeometrySizeConverter _videoOverlayGeometrySizeConverter;
        private CroppingGeometryAdorner _geometryAdorner = null;
        private Point? _cropGeometryDragOrigin;
        private bool _hasGeometryMoveStarted = false;

        private ICroppingViewModel ViewModel => DataContext as ICroppingViewModel;

        /// <inheritdoc cref="DesignerProperties.GetIsInDesignMode(DependencyObject)"/>
        public bool IsInDesignMode => DesignerProperties.GetIsInDesignMode(this);

        /// <summary>
        /// Identifies the <see cref="CropGeometries" /> dependency property key.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="CropGeometries" /> dependency property key.
        /// </returns>
        protected static readonly DependencyPropertyKey CropGeometriesPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(CropGeometries),
            typeof(GeometryCollection),
            typeof(CroppingVideoOverlayView),
            new PropertyMetadata());

        /// <summary>
        /// Identifies the <see cref="CropGeometries" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="CropGeometries" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty CropGeometriesProperty =
                CropGeometriesPropertyKey.DependencyProperty;

        /// <summary>
        /// <see cref="GeometryCollection"/> visualizing <see cref="CropSegmentViewModel"/>s in the data bound
        /// <see cref="ITimelineSegmentProvidingViewModel.ActiveSegments">ActiveSegments</see> collection.
        /// </summary>
        /// <remarks>
        /// Bound to the exclusionary <see cref="GeometryGroup.Children"/> in the <see cref="_overlayRectangleElement"/>'s <see cref="UIElement.OpacityMask"/>
        /// as part of a pseudo-<see cref="ItemsControl"/>.
        /// </remarks>
        public GeometryCollection CropGeometries
        {
            get => (GeometryCollection)GetValue(CropGeometriesProperty);
            private set => SetValue(CropGeometriesPropertyKey, value);
        }

        /// <summary>
        /// Creates a new <see cref="CroppingVideoOverlayView"/> instance.
        /// </summary>
        public CroppingVideoOverlayView()
        {
            InitializeComponent();

            _videoOverlayGeometrySizeConverter = new VideoOverlayGeometrySizeConverter();

            // Creating the GeometryCollection here rather than in the CropGeometries dependency property metadata
            // to ensure that IsFrozen is set to false and the collection can be modified.
            CropGeometries = new GeometryCollection();

            if (!IsInDesignMode)
            {
                CollectionChangedEventManager.AddHandler((INotifyCollectionChanged)ViewModel.ActiveSegments, OnActiveSegmentsCollectionChanged);
            }
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> routed event for the <see cref="_overlayRectangleElement"/>.
        /// </summary>
        /// <remarks>
        /// Displays the <see cref="_geometryAdorner"/> once the <see cref="_overlayRectangleElement"/>
        /// has finished loading.
        /// </remarks>
        /// <inheritdoc cref="RoutedEventHandler"/>
        private void OnOverlayRectangleLoaded(object sender, RoutedEventArgs e)
        {
            if (!IsInDesignMode)
            {
                DisplayGeometryAdorner();
            }
        }

        /// <summary>
        /// Invoked whenever the <see cref="ITimelineSegmentProvidingViewModel.ActiveSegments">ActiveSegments</see>
        /// collection changes.
        /// </summary>
        /// <remarks>
        /// Adds, removes or updates <see cref="PathGeometry"/> objects in the <see cref="CropGeometries"/> collection
        /// as part of a pseudo-<see cref="ItemsControl"/> for the <see cref="_overlayRectangleElement"/>'s <see cref="UIElement.OpacityMask"/>.
        /// </remarks>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        private void OnActiveSegmentsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems.Count == 1);

                    AddCropGeometry((CropSegmentViewModel)e.NewItems[0]);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems.Count == 1);

                    RemoveCropGeometry(
                        (PathGeometry)CropGeometries.First(geometry => CropGeometryAttachedProp.GetViewModel(geometry) == (CropSegmentViewModel)e.OldItems[0])
                    );
                    break;

                case NotifyCollectionChangedAction.Replace:
                    Debug.Assert(e.OldItems.Count == 1 && e.NewItems.Count == 1);

                    // Reuse the existing PathGeometry by resetting its bindings rather than removing and recreating it.
                    PathGeometry cropGeometry = (PathGeometry)CropGeometries.First(geometry => CropGeometryAttachedProp.GetViewModel(geometry) == (CropSegmentViewModel)e.OldItems[0]);
                    SetCropGeometryDataBindings(cropGeometry, (CropSegmentViewModel)e.NewItems[0]);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    for (int i = CropGeometries.Count - 1; i >= 0; i--)
                    {
                        if (CropGeometries[i] is PathGeometry pathGeometry)
                        {
                            RemoveCropGeometry(pathGeometry);
                        }
                        else
                        {
                            // Execution should never reach here...
                            Debug.Fail($"{nameof(CropGeometries)} collection item at index {i} isn't a {nameof(PathGeometry)} instance");

                            CropGeometries.RemoveAt(i);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Creates a <see cref="PathGeometry"/> object bound to the specified <paramref name="cropSegmentViewModel"/>
        /// and adds it to the <see cref="CropGeometries"/> collection.
        /// </summary>
        /// <param name="cropSegmentViewModel">The <see cref="CropSegmentViewModel"/> to bind the created <see cref="PathGeometry"/> to.</param>
        private void AddCropGeometry(CropSegmentViewModel cropSegmentViewModel)
        {
            if (CropGeometries.FirstOrDefault(geometry => CropGeometryAttachedProp.GetViewModel(geometry) == cropSegmentViewModel) is PathGeometry cropGeometry)
            {
                // A PathGeometry object already exists for the cropSegmentViewModel.
                return;
            }

            PolyLineSegment polyLineSegment = new PolyLineSegment(
                points: new Point[]
                {
                    default,    // Placeholder for VisualTopRight binding
                    default,    // Placeholder for VisualBottomRight binding
                    default     // Placeholder for VisualBottomLeft binding
                },
                isStroked: true
            );

            PathFigure pathFigure = new PathFigure();
            pathFigure.Segments.Add(polyLineSegment);
            pathFigure.IsClosed = true;

            cropGeometry = new PathGeometry(
                new PathFigure[]
                {
                    pathFigure
                }
            );

            SetCropGeometryDataBindings(cropGeometry, cropSegmentViewModel);

            CropGeometries.Add(cropGeometry);
        }

        /// <summary>
        /// Clears bindings and removes the specified <see cref="PathGeometry"/>
        /// from the <see cref="CropGeometries"/> collection.
        /// </summary>
        /// <param name="cropGeometry">
        /// The <see cref="PathGeometry"/> to remove from the <see cref="CropGeometries"/> collection.
        /// </param>
        private void RemoveCropGeometry(PathGeometry cropGeometry)
        {
            CropGeometries.Remove(cropGeometry);
            CropGeometryAttachedProp.SetViewModel(cropGeometry, null);

            PathFigure pathFigure = cropGeometry.Figures.FirstOrDefault();
            if (pathFigure != null)
            {
                if (pathFigure.Segments.FirstOrDefault() is PolyLineSegment polyLineSegment)
                {
                    BindingOperations.ClearBinding(polyLineSegment, CropGeometryAttachedProp.BottomLeftProperty);
                    BindingOperations.ClearBinding(polyLineSegment, CropGeometryAttachedProp.BottomRightProperty);
                    BindingOperations.ClearBinding(polyLineSegment, CropGeometryAttachedProp.TopRightProperty);
                }

                BindingOperations.ClearBinding(pathFigure, PathFigure.StartPointProperty);  // TopLeft
            }
        }

        /// <summary>
        /// Data binds a target <see cref="PathGeometry"/> to a source <see cref="CropSegmentViewModel"/>.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="CropGeometryAttachedProp">attached properties</see> to bind individual <see cref="PolyLineSegment.Points"/>
        /// to the <paramref name="cropSegmentViewModel"/>.
        /// </remarks>
        /// <param name="cropGeometry">The binding target <see cref="PathGeometry"/>.</param>
        /// <param name="cropSegmentViewModel">The binding source <see cref="CropSegmentViewModel"/>.</param>
        private void SetCropGeometryDataBindings(PathGeometry cropGeometry, CropSegmentViewModel cropSegmentViewModel)
        {
            CropGeometryAttachedProp.SetViewModel(cropGeometry, cropSegmentViewModel);

            PathFigure pathFigure = cropGeometry.Figures[0];
            BindingOperations.SetBinding(
                pathFigure, PathFigure.StartPointProperty,
                CreatePolyLineSegmentPointBinding(nameof(CropSegmentViewModel.VisualTopLeft), cropSegmentViewModel)
            );

            PolyLineSegment polyLineSegment = (PolyLineSegment)pathFigure.Segments[0];

            BindingOperations.SetBinding(
                polyLineSegment, CropGeometryAttachedProp.TopRightProperty,
                CreatePolyLineSegmentPointBinding(nameof(CropSegmentViewModel.VisualTopRight), cropSegmentViewModel)
            );
            BindingOperations.SetBinding(
                polyLineSegment, CropGeometryAttachedProp.BottomRightProperty,
                CreatePolyLineSegmentPointBinding(nameof(CropSegmentViewModel.VisualBottomRight), cropSegmentViewModel)
            );
            BindingOperations.SetBinding(
                polyLineSegment, CropGeometryAttachedProp.BottomLeftProperty,
                CreatePolyLineSegmentPointBinding(nameof(CropSegmentViewModel.VisualBottomLeft), cropSegmentViewModel)
            );
        }

        /// <summary>
        /// Creates a one-way <see cref="Binding"/> with an initial path and explicitly sets
        /// the <see cref="CroppingVideoOverlayView"/>'s <see cref="FrameworkElement.DataContext">DataContext</see>
        /// as its binding source.
        /// </summary>
        /// <param name="bindingPath">The initial <see cref="Binding.Path"/> for the binding.</param>
        /// <returns>
        /// A new one-way <see cref="Binding"/> with the initial <paramref name="bindingPath"/>
        /// and the <see cref="CroppingVideoOverlayView"/>'s <see cref="FrameworkElement.DataContext">DataContext</see>
        /// as its binding source.
        /// </returns>
        private Binding CreateOneWayDataContextBinding(string bindingPath) => new Binding(bindingPath)
        {
            Source = DataContext,
            Mode = BindingMode.OneWay
        };

        /// <summary>
        /// Creates a one-way <see cref="MultiBinding"/> with a binding path and source
        /// for an individual <see cref="Point"/> in a <see cref="PolyLineSegment.Points"/> collection.
        /// </summary>
        /// <remarks>
        /// Multi-binds the <see cref="ICroppingViewModel.ScriptVideoContext.VideoFrameSize"/>, and <see cref="_overlayRectangleElement"/>'s
        /// <see cref="FrameworkElement.ActualWidth">AcutalWidth</see> and <see cref="FrameworkElement.ActualHeight">ActualHeight</see>
        /// as input values for the <see cref="VideoOverlayGeometrySizeConverter"/> so that the bound <see cref="Point"/> is positioned
        /// according to the video frame ratio whenever the <see cref="CroppingVideoOverlayView"/> is resized.
        /// </remarks>
        /// <param name="bindingPath">The initial <see cref="Binding.Path"/> for the binding.</param>
        /// <param name="bindingSource">The <see cref="object"/> to use as the binding source.</param>
        /// <returns>
        /// A new one-way <see cref="MultiBinding"/> with the <paramref name="bindingPath"/> and <paramref name="bindingSource"/>
        /// for an individual <see cref="Point"/> in a <see cref="PolyLineSegment.Points"/> collection.
        /// </returns>
        private MultiBinding CreatePolyLineSegmentPointBinding(string bindingPath, object bindingSource)
        {
            MultiBinding multiBinding = new MultiBinding()
            {
                Mode = BindingMode.OneWay,
                Converter = _videoOverlayGeometrySizeConverter
            };
            multiBinding.Bindings.Add(
                new Binding(bindingPath)
                {
                    Source = bindingSource,
                    Mode = BindingMode.OneWay
                }
            );
            multiBinding.Bindings.Add(
                CreateOneWayDataContextBinding(
                    $"{nameof(ICroppingViewModel.ScriptVideoContext)}.{nameof(ICroppingViewModel.ScriptVideoContext.VideoFrameSize)}"
                )
            );
            multiBinding.Bindings.Add(
                new Binding(nameof(ActualWidth))
                {
                    Source = _overlayRectangleElement,
                    Mode = BindingMode.OneWay
                }
            );
            multiBinding.Bindings.Add(
                new Binding(nameof(ActualHeight))
                {
                    Source = _overlayRectangleElement,
                    Mode = BindingMode.OneWay
                }
            );

            return multiBinding;
        }

        /// <summary>
        /// Displays the <see cref="_geometryAdorner"/> over the <see cref="_overlayRectangleElement"/>
        /// for resizing and/or rotating on-screen crop segments.
        /// </summary>
        /// <remarks>
        /// Creates and data binds a new <see cref="CroppingGeometryAdorner"/> instance
        /// if the <see cref="_geometryAdorner"/> is being displayed for the first time.
        /// </remarks>
        private void DisplayGeometryAdorner()
        {
            if (_geometryAdorner == null)
            {
                _geometryAdorner = new CroppingGeometryAdorner(_overlayRectangleElement);
                _geometryAdorner.SetBinding(CroppingGeometryAdorner.TopLeftHandleDataCoordinateProperty,
                                            CreateOneWayDataContextBinding($"{nameof(ICroppingViewModel.SelectedSegment)}.{nameof(CropSegmentViewModel.VisualTopLeft)}"));
                _geometryAdorner.SetBinding(CroppingGeometryAdorner.TopRightHandleDataCoordinateProperty,
                                            CreateOneWayDataContextBinding($"{nameof(ICroppingViewModel.SelectedSegment)}.{nameof(CropSegmentViewModel.VisualTopRight)}"));
                _geometryAdorner.SetBinding(CroppingGeometryAdorner.BottomLeftHandleDataCoordinateProperty,
                                            CreateOneWayDataContextBinding($"{nameof(ICroppingViewModel.SelectedSegment)}.{nameof(CropSegmentViewModel.VisualBottomLeft)}"));
                _geometryAdorner.SetBinding(CroppingGeometryAdorner.BottomRightHandleDataCoordinateProperty,
                                            CreateOneWayDataContextBinding($"{ nameof(ICroppingViewModel.SelectedSegment)}.{ nameof(CropSegmentViewModel.VisualBottomRight)}"));
            }

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_overlayRectangleElement);
            Adorner[] adorners = adornerLayer.GetAdorners(_overlayRectangleElement);
            if (adorners == null || !adorners.Contains(_geometryAdorner))
            {
                adornerLayer.Add(_geometryAdorner);
            }
        }

        /// <remarks>
        /// If the left mouse button is pressed while the cursor is over a <see cref="CropGeometries"/>
        /// <see cref="PathGeometry"/> item, the associated crop segment is selected and prepared for a potential drag operation.
        /// </remarks>
        /// <inheritdoc/>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.ChangedButton == MouseButton.Left)
            {
                Point hitPosition = e.GetPosition(_overlayRectangleElement);

                var hitGeometry = CropGeometries.FirstOrDefault(geometry => geometry.FillContains(hitPosition));
                if (hitGeometry != null && CropGeometryAttachedProp.GetViewModel(hitGeometry) is CropSegmentViewModel hitGeometryViewModel)
                {
                    if (hitGeometryViewModel.CanBeEdited)
                    {
                        // Prepare for a potential drag operation.
                        _cropGeometryDragOrigin = hitPosition;
                    }

                    ViewModel.SelectedSegment = hitGeometryViewModel;
                }
            }
        }

        /// <remarks>If a crop segment is being dragged, the drag operation is ended.</remarks>
        /// <inheritdoc/>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (_hasGeometryMoveStarted)
            {
                ViewModel.SelectedSegment?.CompleteUndoableAction();
                _hasGeometryMoveStarted = false;
            }

            _cropGeometryDragOrigin = null;
        }

        /// <remarks>
        /// If the left mouse button is pressed while moving the mouse over or away from a crop segment
        /// that was selected in <see cref="OnMouseDown(MouseButtonEventArgs)"/>,
        /// a dragging operation for moving the segment's center position is started.
        /// </remarks>
        /// <inheritdoc/>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_cropGeometryDragOrigin.HasValue && e.LeftButton == MouseButtonState.Pressed
                && ViewModel.SelectedSegment?.CanBeEdited == true)
            {
                Point pos = e.GetPosition(_overlayRectangleElement);
                Vector offset = new Vector(pos.X - _cropGeometryDragOrigin.Value.X, pos.Y - _cropGeometryDragOrigin.Value.Y);
                _cropGeometryDragOrigin = pos;

                double minSize = 0.001;
                double elementWidth = Math.Max(minSize, _overlayRectangleElement.ActualWidth);
                double elementHeight = Math.Max(minSize, _overlayRectangleElement.ActualHeight);
                SizeI videoFrameSize = ViewModel.ScriptVideoContext.VideoFrameSize;

                double xDeltaScaleFactor = videoFrameSize.Width / elementWidth;
                double yDeltaScaleFactor = videoFrameSize.Height / elementHeight;
                Vector scaledOffset = new Vector(offset.X * xDeltaScaleFactor, offset.Y * yDeltaScaleFactor);

                CropSegmentViewModel selectedSegmentViewModel = (CropSegmentViewModel)ViewModel.SelectedSegment;

                if (!_hasGeometryMoveStarted)
                {
                    selectedSegmentViewModel.BeginBatchChangeCenterAction();
                    _hasGeometryMoveStarted = true;
                }

                selectedSegmentViewModel.Center += scaledOffset;
            }
        }

        /// <remarks>If a crop segment is being dragged, the drag operation is canceled.</remarks>
        /// <inheritdoc/>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            _cropGeometryDragOrigin = null;
        }

        /// <remarks>
        /// If the left mouse button is double-clicked on the currently selected crop segment,
        /// and it is editable, the <see cref="ICroppingViewModel.AdjustmentHandleMode"/> is toggled between
        /// <see cref="CropAdjustmentHandleMode.Resize">resize</see> and <see cref="CropAdjustmentHandleMode.Rotate">rotate</see>.
        /// </remarks>
        /// <inheritdoc/>
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (e.ChangedButton == MouseButton.Left && ViewModel.SelectedSegment?.CanBeEdited == true)
            {
                Point hitPosition = e.GetPosition(_overlayRectangleElement);
                if (CropGeometries.Any(geometry => CropGeometryAttachedProp.GetViewModel(geometry) == ViewModel.SelectedSegment
                                                   && geometry.FillContains(hitPosition)))
                {
                    CropAdjustmentHandleMode currentMode = ViewModel.AdjustmentHandleMode;
                    ViewModel.AdjustmentHandleMode = (currentMode == CropAdjustmentHandleMode.Resize) ? CropAdjustmentHandleMode.Rotate : CropAdjustmentHandleMode.Resize;
                }
            }
        }
    }
}
