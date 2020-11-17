#pragma once

namespace VideoScriptEditor::Unmanaged
{
    /// <summary>
    /// Removes all inactive segments from a <see cref="std::map"/>.
    /// Inactive segments are <see cref="std::map"/> elements whose keys aren't present in the passed-in <see cref="std::vector"/>.
    /// </summary>
    /// <remarks>
    /// Function template parameters defining the <see cref="std::map"/> are referenced from https://en.cppreference.com/w/cpp/container/map.
    /// </remarks>
    /// <typeparam name="Key">The key data type stored in the <see cref="std::map"/>.</typeparam>
    /// <typeparam name="T">The element data type stored in the <see cref="std::map"/>.</typeparam>
    /// <typeparam name="Compare">The type providing a function object for comparing two element values as sort keys in the <see cref="std::map"/>.</typeparam>
    /// <typeparam name="Alloc">The type representing the stored allocator object for allocation and deallocation of memory in the <see cref="std::map"/>.</typeparam>
    /// <param name="activeSegmentsMap">A reference to the <see cref="std::map"/> from which to remove inactive segments.</param>
    /// <param name="activeSegmentKeys">
    /// A <see cref="std::vector"/> of key values to compare with key values of elements in the <see cref="std::map"/>.
    /// Any <see cref="std::map"/> element whose key value isn't contained in this collection will be removed.
    /// </param>
    /// <returns>The number of removed <see cref="std::map"/> elements.</returns>
    template<class Key, class T, class Compare, class Alloc>
    inline typename std::map<Key, T, Compare, Alloc>::size_type RemoveInactiveSegmentsFromMap(std::map<Key, T, Compare, Alloc>& activeSegmentsMap, const std::vector<int>& activeSegmentKeys)
    {
        if (activeSegmentKeys.empty())
        {
            auto oldMapSize = activeSegmentsMap.size();
            if (oldMapSize > 0)
            {
                activeSegmentsMap.clear();
            }

            // All items in the map have been removed.
            return oldMapSize;
        }

        return std::erase_if(activeSegmentsMap,
            [&](const auto& item) {
                return std::find(activeSegmentKeys.begin(), activeSegmentKeys.end(), item.first) == activeSegmentKeys.end();
            });
    }
}