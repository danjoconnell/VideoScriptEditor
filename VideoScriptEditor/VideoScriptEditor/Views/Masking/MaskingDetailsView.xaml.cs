using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VideoScriptEditor.PrismExtensions;
using VideoScriptEditor.ViewModels.Masking.Shapes;
using VideoScriptEditor.Views.Common;
using ITimelineSegmentProvidingViewModel = VideoScriptEditor.ViewModels.Timeline.ITimelineSegmentProvidingViewModel;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Views.Masking
{
    /// <summary>
    /// A <see cref="RegionNames.VideoDetailsRegion"/> view for displaying and precision editing properties
    /// of the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment">selected mask shape</see>
    /// in the <see cref="MaskingVideoOverlayView"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="DependentViewAttribute">dependent view</see> of and shares
    /// <see cref="FrameworkElement.DataContext">DataContext</see> with the <see cref="MaskingVideoOverlayView"/>.
    /// </remarks>
    public partial class MaskingDetailsView : UserControl, IViewSharesDataContext
    {
        /// <summary>
        /// Creates a new <see cref="MaskingDetailsView"/> instance.
        /// </summary>
        public MaskingDetailsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Loaded"/> routed event for the root <see cref="Grid"/>
        /// in a <see cref="ItemsControl.ItemTemplate"/> for a <see cref="Point"/> within the
        /// <see cref="DataTemplate">MaskShapePolygonPointsTemplate</see>.
        /// </summary>
        /// <remarks>
        /// Obtains the index of the <see cref="Point"/> data object within the <see cref="PolygonMaskShapeViewModel.Points"/>
        /// collection and sets the <see cref="PointEditBox.BoundPoint"/> binding and <see cref="Label"/> content accordingly.
        /// </remarks>
        /// <inheritdoc cref="RoutedEventHandler"/>
        private void OnPolygonMaskShapePointGridLoaded(object sender, RoutedEventArgs e)
        {
            Grid pointGrid = (Grid)sender;
            Label pointNumberLabel = (Label)pointGrid.FindName("PART_PointNumberLabel");
            PointEditBox pointEditBox = (PointEditBox)pointGrid.FindName("PART_PointEditBox");

            Point point = (Point)pointGrid.DataContext;
            PolygonMaskShapeViewModel polygonMaskShapeViewModel = (PolygonMaskShapeViewModel)pointGrid.Tag;

            int pointIndex = polygonMaskShapeViewModel.Points.IndexOf(point);
            Debug.Assert(pointIndex != -1);

            pointNumberLabel.Content = $"Point {pointIndex + 1}:";

            pointEditBox.SetBinding(
                PointEditBox.BoundPointProperty,
                new Binding($"{nameof(PolygonMaskShapeViewModel.Points)}[{pointIndex}]")
                {
                    Source = polygonMaskShapeViewModel,
                    Mode = BindingMode.TwoWay
                }
            );
        }
    }
}
