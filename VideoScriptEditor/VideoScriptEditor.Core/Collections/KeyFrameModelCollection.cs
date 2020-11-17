using CodeBits;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VideoScriptEditor.Models;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Collections
{
    /// <summary>
    /// A sorted observable collection of <see cref="KeyFrameModelBase">key frame</see> models.
    /// </summary>
    public class KeyFrameModelCollection : OrderedObservableCollection<KeyFrameModelBase>
    {
        /// <summary>
        /// Creates a new <see cref="KeyFrameModelCollection"/> instance.
        /// </summary>
        public KeyFrameModelCollection() : base()
        {
        }

        /// <summary>
        /// Performs a binary search for a <see cref="KeyFrameModelBase">key frame</see> element with the specified <see cref="KeyFrameModelBase.FrameNumber"/> property value.
        /// </summary>
        /// <param name="frameNumber">The <see cref="KeyFrameModelBase.FrameNumber"/> property value of the <see cref="KeyFrameModelBase">key frame</see> element to search for.</param>
        /// <returns>
        /// The zero-based index of the matching element in the collection, if a matching element is found;
        /// otherwise, a negative number that is the bitwise complement of the index of the next element that is larger
        /// or, if there is no larger element, the bitwise complement of <see cref="Collection{T}.Count"/>.
        /// </returns>
        public int BinarySearch(int frameNumber)
        {
            // The current implementation of Collection<T> uses a List<T> as its backing collection, exposed via its protected Items property.
            // So, we'll cast the Items IList<T> back to a List<T> to utilize its built-in BinarySearch method.
            List<KeyFrameModelBase> items = Items as List<KeyFrameModelBase>;
            Debug.Assert(items != null, "Items is not a generic List"); // Verify that the backing collection implementation is still a List<T>.

            return items.BinarySearchByItemPropertyValue(kf => kf.FrameNumber.CompareTo(frameNumber));
        }

        /// <inheritdoc/>
        protected override int Compare(KeyFrameModelBase x, KeyFrameModelBase y)
        {
            return x.FrameNumber.CompareTo(y.FrameNumber);
        }
    }
}
