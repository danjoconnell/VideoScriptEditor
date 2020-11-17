using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using VideoScriptEditor.PrismExtensions;
using VideoScriptEditor.ViewModels.Masking;
using VideoScriptEditor.ViewModels.Masking.Shapes;
using ITimelineSegmentProvidingViewModel = VideoScriptEditor.ViewModels.Timeline.ITimelineSegmentProvidingViewModel;
using Debug = System.Diagnostics.Debug;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.Views.Masking
{
    /// <summary>
    /// The Masking Video Overlay view.
    /// </summary>
    [DependentView(RegionNames.VideoDetailsRegion, typeof(MaskingDetailsView))]
    [DependentView(RegionNames.RibbonGroupRegion, typeof(MaskingRibbonGroupView))]
    public partial class MaskingVideoOverlayView : UserControl, IViewSharesDataContext
    {
        private Canvas _overlayCanvasElement = null;
        private Adorner _shapeAdorner = null;
        private Point? _maskShapeDragOrigin;
        private bool _hasShapeMoveStarted = false;

        private IMaskingViewModel ViewModel => DataContext as IMaskingViewModel;

        /// <inheritdoc cref="DesignerProperties.GetIsInDesignMode(DependencyObject)"/>
        public bool IsInDesignMode => DesignerProperties.GetIsInDesignMode(this);

        /// <summary>
        /// Creates a new <see cref="MaskingVideoOverlayView"/> instance.
        /// </summary>
        public MaskingVideoOverlayView()
        {
            InitializeComponent();

            if (!IsInDesignMode)
            {
                Debug.Assert(ViewModel != null);

                ViewModel.SelectedSegmentChanged += OnSelectedShapeChanged;
                ViewModel.ShapeResizeModeChanged += OnSelectedShapeChanged;
            }
        }

        /// <summary>
        /// Invoked whenever the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment">selected shape</see>
        /// or <see cref="IMaskingViewModel.ShapeResizeMode"/> changes.
        /// </summary>
        /// <inheritdoc cref="EventHandler"/>
        private void OnSelectedShapeChanged(object sender, EventArgs e)
        {
            if (_overlayCanvasElement == null)
            {
                // View hasn't finished loading yet.
                return;
            }

            EnsureCorrectAdorner();
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> routed event for the <see cref="_overlayCanvasElement"/>.
        /// </summary>
        /// <remarks>
        /// Displays the <see cref="_shapeAdorner"/> once the <see cref="_overlayCanvasElement"/>
        /// has finished loading.
        /// </remarks>
        /// <inheritdoc cref="RoutedEventHandler"/>
        private void OnOverlayCanvasLoaded(object sender, RoutedEventArgs e)
        {
            _overlayCanvasElement = (Canvas)sender;

            if (!IsInDesignMode)
            {
                EnsureCorrectAdorner();
            }
        }

        /// <remarks>
        /// If the left mouse button is pressed while the cursor is over a mask shape element,
        /// the mask shape is selected and prepared for a potential drag operation.
        /// </remarks>
        /// <inheritdoc/>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.OriginalSource is Shape hitShape && hitShape.DataContext is MaskShapeViewModelBase hitShapeViewModel)
                {
                    if (hitShapeViewModel.CanBeEdited)
                    {
                        // Prepare for a potential drag operation.
                        _maskShapeDragOrigin = e.GetPosition(_overlayCanvasElement);
                    }

                    ViewModel.SelectedSegment = hitShapeViewModel;
                }
            }
        }

        /// <remarks>If a mask shape is being dragged, the drag operation is ended.</remarks>
        /// <inheritdoc/>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            if (_hasShapeMoveStarted)
            {
                ViewModel.SelectedSegment?.CompleteUndoableAction();
                _hasShapeMoveStarted = false;
            }

            _maskShapeDragOrigin = null;
        }

        /// <remarks>
        /// If the left mouse button is pressed while moving the mouse over or away from a mask shape
        /// that was selected in <see cref="OnMouseDown(MouseButtonEventArgs)"/>,
        /// a dragging operation for moving the shape is started.
        /// </remarks>
        /// <inheritdoc/>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_maskShapeDragOrigin.HasValue && e.LeftButton == MouseButtonState.Pressed
                && ViewModel.SelectedSegment?.CanBeEdited == true)
            {
                Point pos = e.GetPosition(_overlayCanvasElement);
                Vector offset = new Vector(pos.X - _maskShapeDragOrigin.Value.X, pos.Y - _maskShapeDragOrigin.Value.Y);
                _maskShapeDragOrigin = pos;

                double minSize = 0.001;
                double elementWidth = Math.Max(minSize, _overlayCanvasElement.ActualWidth);
                double elementHeight = Math.Max(minSize, _overlayCanvasElement.ActualHeight);
                SizeI videoFrameSize = ViewModel.ScriptVideoContext.VideoFrameSize;

                double xDeltaScaleFactor = videoFrameSize.Width / elementWidth;
                double yDeltaScaleFactor = videoFrameSize.Height / elementHeight;
                Vector scaledOffset = new Vector(offset.X * xDeltaScaleFactor, offset.Y * yDeltaScaleFactor);

                MaskShapeViewModelBase selectedShapeViewModel = (MaskShapeViewModelBase)ViewModel.SelectedSegment;
                if (!_hasShapeMoveStarted)
                {
                    selectedShapeViewModel.BeginShapeMoveAction();
                    _hasShapeMoveStarted = true;
                }

                selectedShapeViewModel.OffsetWithinVideoFrameBounds(scaledOffset);
            }
        }

        /// <remarks>If a mask shape is being dragged, the drag operation is canceled.</remarks>
        /// <inheritdoc/>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);

            _maskShapeDragOrigin = null;
        }

        /// <summary>
        /// Displays the <see cref="_shapeAdorner"/> over the <see cref="_overlayCanvasElement"/>
        /// for resizing on-screen mask shapes.
        /// </summary>
        private void DisplayShapeAdorner()
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_overlayCanvasElement);

            Adorner[] adorners = adornerLayer.GetAdorners(_overlayCanvasElement);
            if (adorners == null || !adorners.Contains(_shapeAdorner))
            {
                adornerLayer.Add(_shapeAdorner);
            }
        }

        /// <summary>
        /// Removes the <see cref="_shapeAdorner"/> from the
        /// <see cref="_overlayCanvasElement"/>'s <see cref="AdornerLayer"/>.
        /// </summary>
        private void RemoveShapeAdorner()
        {
            if (_shapeAdorner == null)
            {
                return;
            }

            BindingOperations.ClearAllBindings(_shapeAdorner);

            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(_overlayCanvasElement);
            adornerLayer.Remove(_shapeAdorner);
            _shapeAdorner = null;
        }

        /// <summary>
        /// Ensures that the correct <see cref="Adorner"/> for the currently
        /// <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment">selected shape</see>
        /// is displayed over the <see cref="_overlayCanvasElement"/>.
        /// </summary>
        private void EnsureCorrectAdorner()
        {
            if (ViewModel.SelectedSegment == null)
            {
                return;
            }

            if (ViewModel.ShapeResizeMode == MaskShapeResizeMode.Points)
            {
                if (ViewModel.SelectedSegment is PolygonMaskShapeViewModel)
                {
                    DisplayPolygonPointsResizeAdorner();
                }
                else if (ViewModel.SelectedSegment is EllipseMaskShapeViewModel)
                {
                    DisplayEllipsePointsResizeAdorner();
                }
            }
            else
            {
                DisplayShapeBoundsResizeAdorner();
            }
        }


        /// <summary>
        /// Displays a <see cref="MaskingShapeBoundsResizeAdorner"/> over the <see cref="_overlayCanvasElement"/>
        /// for resizing the bounds of on-screen mask shapes.
        /// </summary>
        private void DisplayShapeBoundsResizeAdorner()
        {
            if (_shapeAdorner is MaskingShapeBoundsResizeAdorner)
            {
                return;
            }

            RemoveShapeAdorner();

            MaskingShapeBoundsResizeAdorner shapeBoundsResizeAdorner = new MaskingShapeBoundsResizeAdorner(_overlayCanvasElement);
            shapeBoundsResizeAdorner.SetBinding(MaskingShapeBoundsResizeAdorner.ShapeDataBoundsProperty,
                                                CreateOneWayDataContextBinding($"{nameof(IMaskingViewModel.SelectedSegment)}.{nameof(MaskShapeViewModelBase.Bounds)}"));

            _shapeAdorner = shapeBoundsResizeAdorner;
            DisplayShapeAdorner();
        }

        /// <summary>
        /// Displays a <see cref="MaskingPolygonPointsResizeAdorner"/> over the <see cref="_overlayCanvasElement"/>
        /// for resizing the points of an on-screen polygon mask shape.
        /// </summary>
        private void DisplayPolygonPointsResizeAdorner()
        {
            if (_shapeAdorner is MaskingPolygonPointsResizeAdorner)
            {
                return;
            }

            RemoveShapeAdorner();

            MaskingPolygonPointsResizeAdorner polygonPointsResizeAdorner = new MaskingPolygonPointsResizeAdorner(_overlayCanvasElement);
            polygonPointsResizeAdorner.SetBinding(MaskingPolygonPointsResizeAdorner.PolygonPointsProperty,
                                                  CreateOneWayDataContextBinding($"{nameof(IMaskingViewModel.SelectedSegment)}.{nameof(PolygonMaskShapeViewModel.Points)}"));

            _shapeAdorner = polygonPointsResizeAdorner;
            DisplayShapeAdorner();
        }

        /// <summary>
        /// Displays a <see cref="MaskingEllipsePointsResizeAdorner"/> over the <see cref="_overlayCanvasElement"/>
        /// for resizing the radius axis points of an on-screen ellipse mask shape.
        /// </summary>
        private void DisplayEllipsePointsResizeAdorner()
        {
            if (_shapeAdorner is MaskingEllipsePointsResizeAdorner)
            {
                return;
            }

            RemoveShapeAdorner();

            MaskingEllipsePointsResizeAdorner ellipsePointsResizeAdorner = new MaskingEllipsePointsResizeAdorner(_overlayCanvasElement);
            ellipsePointsResizeAdorner.SetBinding(MaskingEllipsePointsResizeAdorner.EllipseDataCenterPointProperty,
                                                  CreateOneWayDataContextBinding($"{nameof(IMaskingViewModel.SelectedSegment)}.{nameof(EllipseMaskShapeViewModel.CenterPoint)}"));
            ellipsePointsResizeAdorner.SetBinding(MaskingEllipsePointsResizeAdorner.EllipseDataRadiusXProperty,
                                                  CreateDataContextBinding($"{nameof(IMaskingViewModel.SelectedSegment)}.{nameof(EllipseMaskShapeViewModel.RadiusX)}"));
            ellipsePointsResizeAdorner.SetBinding(MaskingEllipsePointsResizeAdorner.EllipseDataRadiusYProperty,
                                                  CreateDataContextBinding($"{nameof(IMaskingViewModel.SelectedSegment)}.{nameof(EllipseMaskShapeViewModel.RadiusY)}"));

            _shapeAdorner = ellipsePointsResizeAdorner;
            DisplayShapeAdorner();
        }

        /// <summary>
        /// Creates a <see cref="Binding"/> with an initial path and <see cref="BindingMode">binding mode</see>
        /// and explicitly sets the <see cref="MaskingVideoOverlayView"/>'s <see cref="FrameworkElement.DataContext">DataContext</see>
        /// as its binding source.
        /// </summary>
        /// <param name="bindingPath">The initial <see cref="Binding.Path"/> for the binding.</param>
        /// <param name="bindingMode">
        /// The <see cref="Binding.Mode"/> for the binding. Defaults to <see cref="BindingMode.TwoWay"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="Binding"/> with the <paramref name="bindingPath"/> and <paramref name="bindingMode"/>
        /// and the <see cref="MaskingVideoOverlayView"/>'s <see cref="FrameworkElement.DataContext">DataContext</see>
        /// as its binding source.
        /// </returns>
        private Binding CreateDataContextBinding(string bindingPath, BindingMode bindingMode = BindingMode.TwoWay) => new Binding(bindingPath)
        {
            Source = DataContext,
            Mode = bindingMode
        };

        /// <summary>
        /// Creates a one-way <see cref="Binding"/> with an initial path and explicitly sets
        /// the <see cref="MaskingVideoOverlayView"/>'s <see cref="FrameworkElement.DataContext">DataContext</see>
        /// as its binding source.
        /// </summary>
        /// <param name="bindingPath">The initial <see cref="Binding.Path"/> for the binding.</param>
        /// <returns>
        /// A new one-way <see cref="Binding"/> with the initial <paramref name="bindingPath"/>
        /// and the <see cref="MaskingVideoOverlayView"/>'s <see cref="FrameworkElement.DataContext">DataContext</see>
        /// as its binding source.
        /// </returns>
        private Binding CreateOneWayDataContextBinding(string bindingPath) => CreateDataContextBinding(bindingPath, BindingMode.OneWay);
    }
}
