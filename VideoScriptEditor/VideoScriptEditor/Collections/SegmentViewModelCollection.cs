using CodeBits;
using VideoScriptEditor.ViewModels.Timeline;

namespace VideoScriptEditor.Collections
{
    /// <summary>
    /// A sorted observable collection of <see cref="SegmentViewModelBase">segment</see> view models.
    /// </summary>
    public class SegmentViewModelCollection : OrderedObservableCollection<SegmentViewModelBase>
    {
        /// <summary>
        /// Creates a new <see cref="SegmentViewModelCollection"/> instance.
        /// </summary>
        public SegmentViewModelCollection() : base()
        {
        }
    }
}
