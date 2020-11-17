using System.Windows;
using VideoScriptEditor.PrismExtensions;

namespace VideoScriptEditor.Views.Masking
{
    /// <summary>
    /// A view providing masking-related ribbon content for the <see cref="RegionNames.RibbonGroupRegion"/>
    /// </summary>
    /// <remarks>
    /// A <see cref="DependentViewAttribute">dependent view</see> of and shares
    /// <see cref="FrameworkElement.DataContext">DataContext</see> with the <see cref="MaskingVideoOverlayView"/>.
    /// </remarks>
    public partial class MaskingRibbonGroupView : Fluent.RibbonGroupBox, IViewSharesDataContext
    {
        /// <summary>
        /// Creates a new <see cref="MaskingRibbonGroupView"/> instance.
        /// </summary>
        public MaskingRibbonGroupView()
        {
            InitializeComponent();
        }
    }
}
