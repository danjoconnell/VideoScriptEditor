using System.Windows;
using System.Windows.Media;
using VideoScriptEditor.ViewModels.Cropping;

namespace VideoScriptEditor.Views.Cropping
{
    /// <summary>
    /// Attached properties for <see cref="System.Windows.Media.Geometry"/> and child <see cref="PolyLineSegment"/>
    /// objects in the <see cref="CroppingVideoOverlayView.CropGeometries"/> collection.
    /// </summary>
    /// <remarks>
    /// Allows individual <see cref="Point"/>s in a <see cref="PolyLineSegment.Points"/> collection to be
    /// data bound directly to <see cref="Point"/>s in a <see cref="CropSegmentViewModel"/>.
    /// </remarks>
    public class CropGeometryAttachedProp
    {
        // The compiler _should_ inline these const values:
        private const int TopRightPolyLinePointIndex = 0;
        private const int BottomRightPolyLinePointIndex = 1;
        private const int BottomLeftPolyLinePointIndex = 2;

        /// <summary>
        /// Identifies the <see cref="CropGeometryAttachedProp.ViewModel"/> attached property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="CropGeometryAttachedProp.ViewModel"/> attached property.
        /// </returns>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.RegisterAttached(
            "ViewModel",
            typeof(CropSegmentViewModel),
            typeof(CropGeometryAttachedProp),
            new PropertyMetadata(null)
        );

        /// <summary>
        /// Gets the value of the <see cref="CropGeometryAttachedProp.ViewModel"/> attached property
        /// for a given <see cref="System.Windows.Media.Geometry"/> object.
        /// </summary>
        /// <param name="geometryObj">
        /// The <see cref="System.Windows.Media.Geometry"/> object from which the property value is read.
        /// </param>
        /// <returns>
        /// The <see cref="CropGeometryAttachedProp.ViewModel"/> of the specified <see cref="System.Windows.Media.Geometry"/> object
        /// for data binding.
        /// </returns>
        [AttachedPropertyBrowsableForType(typeof(System.Windows.Media.Geometry))]
        public static CropSegmentViewModel GetViewModel(System.Windows.Media.Geometry geometryObj) => (CropSegmentViewModel)geometryObj.GetValue(ViewModelProperty);

        /// <summary>
        /// Sets the value of the <see cref="CropGeometryAttachedProp.ViewModel"/> attached property
        /// for a given <see cref="System.Windows.Media.Geometry"/> object.
        /// </summary>
        /// <param name="geometryObj">
        /// The <see cref="System.Windows.Media.Geometry"/> object to which the property value is written.
        /// </param>
        /// <param name="viewModel">
        /// Sets the <see cref="CropGeometryAttachedProp.ViewModel"/> of the specified <see cref="System.Windows.Media.Geometry"/> object
        /// for data binding.
        /// </param>
        public static void SetViewModel(System.Windows.Media.Geometry geometryObj, CropSegmentViewModel viewModel) => geometryObj.SetValue(ViewModelProperty, viewModel);

        /// <summary>
        /// Identifies the <see cref="CropGeometryAttachedProp.TopRight"/> attached property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="CropGeometryAttachedProp.TopRight"/> attached property.
        /// </returns>
        public static readonly DependencyProperty TopRightProperty =
                DependencyProperty.RegisterAttached(
                        "TopRight",
                        typeof(Point),
                        typeof(CropGeometryAttachedProp),
                        new FrameworkPropertyMetadata(default(Point), new PropertyChangedCallback(OnTopRightPropertyChanged)));


        /// <summary>
        /// Gets the value of the <see cref="CropGeometryAttachedProp.TopRight"/> attached property
        /// for a given <see cref="PolyLineSegment"/> object.
        /// </summary>
        /// <param name="polyLineSegment">The <see cref="PolyLineSegment"/> object from which the property value is read.</param>
        /// <returns>
        /// The <see cref="CropGeometryAttachedProp.TopRight"/> <see cref="Point"/> in the <see cref="PolyLineSegment.Points"/> collection.
        /// </returns>
        [AttachedPropertyBrowsableForType(typeof(PolyLineSegment))]
        public static Point GetTopRight(PolyLineSegment polyLineSegment) => (Point)polyLineSegment.GetValue(TopRightProperty);

        /// <summary>
        /// Sets the value of the <see cref="CropGeometryAttachedProp.TopRight"/> attached property
        /// for a given <see cref="PolyLineSegment"/> object.
        /// </summary>
        /// <param name="polyLineSegment">The <see cref="PolyLineSegment"/> object to which the property value is written.</param>
        /// <param name="value">
        /// Sets the <see cref="CropGeometryAttachedProp.TopRight"/> <see cref="Point"/> in the <see cref="PolyLineSegment.Points"/> collection.
        /// </param>
        public static void SetTopRight(PolyLineSegment polyLineSegment, Point value) => polyLineSegment.SetValue(TopRightProperty, value);

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="CropGeometryAttachedProp.TopRight"/> attached property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnTopRightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Assumes PointCollection already has an item at this index
            ((PolyLineSegment)d).Points[TopRightPolyLinePointIndex] = (Point)e.NewValue;
        }

        /// <summary>
        /// Identifies the <see cref="CropGeometryAttachedProp.BottomLeft"/> attached property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="CropGeometryAttachedProp.BottomLeft"/> attached property.
        /// </returns>
        public static readonly DependencyProperty BottomLeftProperty =
                DependencyProperty.RegisterAttached(
                        "BottomLeft",
                        typeof(Point),
                        typeof(CropGeometryAttachedProp),
                        new FrameworkPropertyMetadata(default(Point), new PropertyChangedCallback(OnBottomLeftPropertyChanged)));

        /// <summary>
        /// Gets the value of the <see cref="CropGeometryAttachedProp.BottomLeft"/> attached property
        /// for a given <see cref="PolyLineSegment"/> object.
        /// </summary>
        /// <param name="polyLineSegment">The <see cref="PolyLineSegment"/> object from which the property value is read.</param>
        /// <returns>
        /// The <see cref="CropGeometryAttachedProp.BottomLeft"/> <see cref="Point"/> in the <see cref="PolyLineSegment.Points"/> collection.
        /// </returns>
        [AttachedPropertyBrowsableForType(typeof(PolyLineSegment))]
        public static Point GetBottomLeft(PolyLineSegment polyLineSegment) => (Point)polyLineSegment.GetValue(BottomLeftProperty);

        /// <summary>
        /// Sets the value of the <see cref="CropGeometryAttachedProp.BottomLeft"/> attached property
        /// for a given <see cref="PolyLineSegment"/> object.
        /// </summary>
        /// <param name="polyLineSegment">The <see cref="PolyLineSegment"/> object to which the property value is written.</param>
        /// <param name="value">
        /// Sets the <see cref="CropGeometryAttachedProp.BottomLeft"/> <see cref="Point"/> in the <see cref="PolyLineSegment.Points"/> collection.
        /// </param>
        public static void SetBottomLeft(PolyLineSegment polyLineSegment, Point value) => polyLineSegment.SetValue(BottomLeftProperty, value);

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="CropGeometryAttachedProp.BottomLeft"/> attached property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnBottomLeftPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Assumes PointCollection already has an item at this index
            ((PolyLineSegment)d).Points[BottomLeftPolyLinePointIndex] = (Point)e.NewValue;
        }

        /// <summary>
        /// Identifies the <see cref="CropGeometryAttachedProp.BottomRight"/> attached property.
        /// </summary>
        /// <returns>
        /// The identifier for the <see cref="CropGeometryAttachedProp.BottomRight"/> attached property.
        /// </returns>
        public static readonly DependencyProperty BottomRightProperty =
                DependencyProperty.RegisterAttached(
                        "BottomRight",
                        typeof(Point),
                        typeof(CropGeometryAttachedProp),
                        new FrameworkPropertyMetadata(default(Point), new PropertyChangedCallback(OnBottomRightPropertyChanged)));

        /// <summary>
        /// Gets the value of the <see cref="CropGeometryAttachedProp.BottomRight"/> attached property
        /// for a given <see cref="PolyLineSegment"/> object.
        /// </summary>
        /// <param name="polyLineSegment">The <see cref="PolyLineSegment"/> object from which the property value is read.</param>
        /// <returns>
        /// The <see cref="CropGeometryAttachedProp.BottomRight"/> <see cref="Point"/> in the <see cref="PolyLineSegment.Points"/> collection.
        /// </returns>
        [AttachedPropertyBrowsableForType(typeof(PolyLineSegment))]
        public static Point GetBottomRight(PolyLineSegment polyLineSegment) => (Point)polyLineSegment.GetValue(BottomRightProperty);

        /// <summary>
        /// Sets the value of the <see cref="CropGeometryAttachedProp.BottomRight"/> attached property
        /// for a given <see cref="PolyLineSegment"/> object.
        /// </summary>
        /// <param name="polyLineSegment">The <see cref="PolyLineSegment"/> object to which the property value is written.</param>
        /// <param name="value">
        /// Sets the <see cref="CropGeometryAttachedProp.BottomRight"/> <see cref="Point"/> in the <see cref="PolyLineSegment.Points"/> collection.
        /// </param>
        public static void SetBottomRight(PolyLineSegment polyLineSegment, Point value) => polyLineSegment.SetValue(BottomRightProperty, value);

        /// <summary>
        /// <see cref="PropertyChangedCallback"/> for the <see cref="CropGeometryAttachedProp.BottomRight"/> attached property.
        /// </summary>
        /// <inheritdoc cref="PropertyChangedCallback"/>
        private static void OnBottomRightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Assumes PointCollection already has an item at this index
            ((PolyLineSegment)d).Points[BottomRightPolyLinePointIndex] = (Point)e.NewValue;
        }
    }
}
