using System.Windows;
using System.Windows.Controls;
using VideoScriptEditor.PrismExtensions;
using ITimelineSegmentProvidingViewModel = VideoScriptEditor.ViewModels.Timeline.ITimelineSegmentProvidingViewModel;

namespace VideoScriptEditor.Views.Cropping
{
    /// <summary>
    /// A <see cref="RegionNames.VideoDetailsRegion"/> view for displaying and precision editing properties
    /// of the <see cref="ITimelineSegmentProvidingViewModel.SelectedSegment">selected crop segment</see>
    /// in the <see cref="CroppingVideoOverlayView"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="DependentViewAttribute">dependent view</see> of and shares
    /// <see cref="FrameworkElement.DataContext">DataContext</see> with the <see cref="CroppingVideoOverlayView"/>.
    /// </remarks>
    public partial class CroppingDetailsView : UserControl, IViewSharesDataContext
    {
        /// <summary>
        /// Creates a new <see cref="CroppingDetailsView"/> instance.
        /// </summary>
        public CroppingDetailsView()
        {
            InitializeComponent();
        }
    }
}
