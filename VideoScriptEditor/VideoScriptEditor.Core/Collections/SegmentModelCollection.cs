using CodeBits;
using VideoScriptEditor.Models;

namespace VideoScriptEditor.Collections
{
    /// <summary>
    /// A sorted observable collection of <see cref="SegmentModelBase">segment</see> models.
    /// </summary>
    public class SegmentModelCollection : OrderedObservableCollection<SegmentModelBase>
    {
        /// <summary>
        /// Creates a new <see cref="SegmentModelCollection"/> instance.
        /// </summary>
        public SegmentModelCollection() : base()
        {
        }
    }
}
