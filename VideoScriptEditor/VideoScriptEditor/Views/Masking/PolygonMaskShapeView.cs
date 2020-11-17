/*
    Adapted from https://github.com/dotnet/wpf/blob/master/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shapes/Polygon.cs

    Licensed to the .NET Foundation under one or more agreements.
    The .NET Foundation licenses this file to you under the MIT license.
    See LICENSE.TXT at https://github.com/dotnet/wpf/blob/master/LICENSE.TXT for more information.
*/

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using VideoScriptEditor.Geometry;
using VideoScriptEditor.ViewModels.Masking.Shapes;
using Debug = System.Diagnostics.Debug;
using SizeI = System.Drawing.Size;

namespace VideoScriptEditor.Views.Masking
{
    /// <summary>
    /// The Polygon Mask Shape View.
    /// </summary>
    /// <remarks>A modified <see cref="Polygon"/>.</remarks>
    public class PolygonMaskShapeView : Shape
    {
        // Cached Geometry
        private System.Windows.Media.Geometry _polygonGeometry;
        private PathFigure _polygonGeometryPathFigure;
        private PolyLineSegment _polygonGeometryPolyLineSegment;

        private PolygonMaskShapeViewModel ViewModel => DataContext as PolygonMaskShapeViewModel;

        /// <summary>
        /// Creates a new <see cref="PolygonMaskShapeView"/> element.
        /// </summary>
        public PolygonMaskShapeView()
        {
            _polygonGeometry = null;
            _polygonGeometryPathFigure = null;
            _polygonGeometryPolyLineSegment = null;

            SetBinding(PointsProperty,
                       new Binding(nameof(ViewModel.Points)) { Mode = BindingMode.OneWay });
        }

        /// <summary>
        /// Identifies the <see cref="Points" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="Points" /> dependency property.
        /// </returns>
        /// <inheritdoc cref="Points"/>
        private static readonly DependencyProperty PointsProperty =
                DependencyProperty.Register(
                        nameof(Points),
                        typeof(IList<Point>),
                        typeof(PolygonMaskShapeView),
                        new FrameworkPropertyMetadata(null,
                            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                            new PropertyChangedCallback(OnPointsPropertyChanged)));

        /// <summary>
        /// The collection of points that make up the polygon.
        /// </summary>
        /// <remarks>
        /// Acts as a conversion bridge between the bound <see cref="PolygonMaskShapeViewModel.Points"/> collection
        /// and the <see cref="_polygonGeometryPathFigure"/> and <see cref="_polygonGeometryPolyLineSegment"/> Points.
        /// <para><see langword="private"/> since the property is specifically intended to be data bound to the <see cref="ViewModel"/>.</para>
        /// </remarks>
        private IList<Point> Points
        {
            get => (IList<Point>)GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="Points"/> property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnPointsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PolygonMaskShapeView polygonView = (PolygonMaskShapeView)d;

            if (e.OldValue is INotifyCollectionChanged inccOldValue)
            {
                CollectionChangedEventManager.RemoveHandler(inccOldValue, polygonView.OnPointsCollectionChanged);
            }

            if (e.NewValue is INotifyCollectionChanged inccNewValue)
            {
                CollectionChangedEventManager.AddHandler(inccNewValue, polygonView.OnPointsCollectionChanged);
            }
        }

        /// <summary>
        /// Identifies the <see cref="VideoOverlayElementWidth" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="VideoOverlayElementWidth" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty VideoOverlayElementWidthProperty =
                DependencyProperty.Register(
                        nameof(VideoOverlayElementWidth),
                        typeof(double),
                        typeof(PolygonMaskShapeView),
                        new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The width of the parent Video Overlay element.
        /// </summary>
        /// <remarks>
        /// For dynamically scaling the <see cref="Points"/> collection items.
        /// </remarks>
        public double VideoOverlayElementWidth
        {
            get => (double)GetValue(VideoOverlayElementWidthProperty);
            set => SetValue(VideoOverlayElementWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VideoOverlayElementHeight" /> dependency property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="VideoOverlayElementHeight" /> dependency property.
        /// </returns>
        public static readonly DependencyProperty VideoOverlayElementHeightProperty =
                DependencyProperty.Register(
                        nameof(VideoOverlayElementHeight),
                        typeof(double),
                        typeof(PolygonMaskShapeView),
                        new FrameworkPropertyMetadata(default(double), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The height of the parent Video Overlay element.
        /// </summary>
        /// <remarks>
        /// For dynamically scaling the <see cref="Points"/> collection items.
        /// </remarks>
        public double VideoOverlayElementHeight
        {
            get => (double)GetValue(VideoOverlayElementHeightProperty);
            set => SetValue(VideoOverlayElementHeightProperty, value);
        }

        /// <summary>
        /// Gets the polygon that defines this shape.
        /// </summary>
        protected override System.Windows.Media.Geometry DefiningGeometry => _polygonGeometry;

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size constraint)
        {
            // Workaround for Shape.CacheDefiningGeometry() being internal and overridable only by classes in WPF's PresentationFramework assembly.
            CacheDefiningPolygonGeometry();

            // According to https://github.com/dotnet/wpf/blob/b697746306a695af80a756586cad3f84a62e366c/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Shapes/Shape.cs#L359
            // the (internal) CacheDefiningGeometry() method is executed first. Unless overridden, it is an empty method.
            return base.MeasureOverride(constraint);
        }

        /// <summary>
        /// Invoked whenever the <see cref="Points"/> collection changes.
        /// </summary>
        /// <inheritdoc cref="NotifyCollectionChangedEventHandler"/>
        private void OnPointsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.Assert(sender == ViewModel.Points); // Catch memory/event handler registration leaks.

            InvalidateMeasure();
        }

        /// <summary>
        /// Creates or updates the cached <see cref="DefiningGeometry"/>.
        /// </summary>
        /// <remarks>
        /// Would override <see cref="Shape"/>.CacheDefiningGeometry() as <see cref="Polygon"/> does,
        /// but it is <see langword="internal"/> to WPF's PresentationFramework assembly and therefore can't be overridden :(
        /// </remarks>
        private void CacheDefiningPolygonGeometry()
        {
            IList<Point> points = Points;

            // Are we degenerate?
            // Yes, if we don't have data
            if (points == null)
            {
                _polygonGeometry = System.Windows.Media.Geometry.Empty;
                _polygonGeometryPathFigure = null;
                _polygonGeometryPolyLineSegment = null;
                return;
            }

            if (_polygonGeometryPathFigure == null)
            {
                _polygonGeometryPathFigure = new PathFigure()
                {
                    IsClosed = true
                };
            }

            // Create or update the polygon PathGeometry
            if (points.Count > 0)
            {
                _polygonGeometryPathFigure.StartPoint = points[0];

                if (points.Count > 1)
                {
                    if (_polygonGeometryPolyLineSegment == null)
                    {
                        _polygonGeometryPolyLineSegment = new PolyLineSegment()
                        {
                            Points = new PointCollection(),
                            IsStroked = true
                        };

                        _polygonGeometryPathFigure.Segments.Add(_polygonGeometryPolyLineSegment);
                    }

                    SizeI videoFrameSize = ViewModel.ScriptVideoContext.VideoFrameSize;
                    double xScaleFactor = VideoOverlayElementWidth / videoFrameSize.Width;
                    double yScaleFactor = VideoOverlayElementHeight / videoFrameSize.Height;

                    PointCollection pointCollection = _polygonGeometryPolyLineSegment.Points;
                    Debug.Assert(pointCollection.Count < points.Count);

                    // Add or update _polygonGeometryPolyLineSegment.Points collection items
                    for (int i = 1; i < points.Count; i++)
                    {
                        Point scaledPoint = points[i].Scale(xScaleFactor, yScaleFactor);

                        int pointIndex = i - 1;
                        if (pointIndex < pointCollection.Count)
                        {
                            pointCollection[pointIndex] = scaledPoint;
                        }
                        else
                        {
                            pointCollection.Insert(pointIndex, scaledPoint);
                        }
                    }
                }
                else
                {
                    _polygonGeometryPathFigure.Segments.Remove(_polygonGeometryPolyLineSegment);
                    _polygonGeometryPolyLineSegment = null;
                }
            }

            if (!(_polygonGeometry is PathGeometry))
            {
                PathGeometry pathGeometry = new PathGeometry()
                {
                    FillRule = FillRule.EvenOdd
                };
                pathGeometry.Figures.Add(_polygonGeometryPathFigure);

                _polygonGeometry = pathGeometry;
            }
        }
    }
}
