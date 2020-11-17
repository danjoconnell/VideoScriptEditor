using CodeBits;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VideoScriptEditor.ViewModels.Timeline;
using Debug = System.Diagnostics.Debug;

namespace VideoScriptEditor.Collections
{
    /// <summary>
    /// A sorted observable collection of <see cref="KeyFrameViewModelBase">key frame</see> view models.
    /// </summary>
    public class KeyFrameViewModelCollection : OrderedObservableCollection<KeyFrameViewModelBase>
    {
        /// <summary>
        /// Creates a new <see cref="KeyFrameViewModelCollection"/> instance.
        /// </summary>
        public KeyFrameViewModelCollection() : base()
        {
        }

        /// <summary>
        /// Performs a binary search for a <see cref="KeyFrameViewModelBase">key frame</see> element with the specified <see cref="KeyFrameViewModelBase.FrameNumber"/> property value.
        /// </summary>
        /// <param name="frameNumber">The <see cref="KeyFrameViewModelBase.FrameNumber"/> property value of the <see cref="KeyFrameViewModelBase">key frame</see> element to search for.</param>
        /// <returns>
        /// The zero-based index of the matching element in the collection, if a matching element is found;
        /// otherwise, a negative number that is the bitwise complement of the index of the next element that is larger
        /// or, if there is no larger element, the bitwise complement of <see cref="Collection{T}.Count"/>.
        /// </returns>
        public int BinarySearch(int frameNumber)
        {
            // The current implementation of Collection<T> uses a List<T> as its backing collection, exposed via its protected Items property.
            // So, we'll cast the Items IList<T> back to a List<T> to utilize its built-in BinarySearch method.
            List<KeyFrameViewModelBase> items = Items as List<KeyFrameViewModelBase>;
            Debug.Assert(items != null, "Items is not a generic List"); // Verify that the backing collection implementation is still a List<T>.

            return items.BinarySearchByItemPropertyValue(kf => kf.FrameNumber.CompareTo(frameNumber));
        }

        /// <summary>
        /// Finds the index of the first <see cref="KeyFrameViewModelBase">key frame</see> element
        /// that has a <see cref="KeyFrameViewModelBase.FrameNumber"/> property value greater than or equal to
        /// the specified <see cref="KeyFrameViewModelBase.FrameNumber"/> value.
        /// </summary>
        /// <param name="frameNumber">
        /// The <see cref="KeyFrameViewModelBase.FrameNumber"/> value of the <see cref="KeyFrameViewModelBase">key frame</see> element to search for.
        /// </param>
        /// <returns>
        /// The index of the first element that has a <see cref="KeyFrameViewModelBase.FrameNumber"/> property value greater than or equal to
        /// the specified <see cref="KeyFrameViewModelBase.FrameNumber"/> value or <see cref="Collection{T}.Count"/> if no such element is found.
        /// </returns>
        public int LowerBoundIndex(int frameNumber)
        {
            int index = BinarySearch(frameNumber);
            return index < 0 ? ~index : index;
        }

        /// <inheritdoc/>
        protected override int Compare(KeyFrameViewModelBase x, KeyFrameViewModelBase y)
        {
            return x.FrameNumber.CompareTo(y.FrameNumber);
        }
    }
}
