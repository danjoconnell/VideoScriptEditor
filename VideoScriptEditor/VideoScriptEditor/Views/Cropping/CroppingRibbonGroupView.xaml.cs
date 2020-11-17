using System.Windows;
using VideoScriptEditor.PrismExtensions;

namespace VideoScriptEditor.Views.Cropping
{
    /// <summary>
    /// A view providing cropping-related ribbon content for the <see cref="RegionNames.RibbonGroupRegion"/>
    /// </summary>
    /// <remarks>
    /// A <see cref="DependentViewAttribute">dependent view</see> of and shares
    /// <see cref="FrameworkElement.DataContext">DataContext</see> with the <see cref="CroppingVideoOverlayView"/>.
    /// </remarks>
    public partial class CroppingRibbonGroupView : Fluent.RibbonGroupBox, IViewSharesDataContext
    {
        /// <summary>
        /// Creates a new <see cref="CroppingRibbonGroupView"/> instance.
        /// </summary>
        public CroppingRibbonGroupView()
        {
            InitializeComponent();
        }
    }
}
