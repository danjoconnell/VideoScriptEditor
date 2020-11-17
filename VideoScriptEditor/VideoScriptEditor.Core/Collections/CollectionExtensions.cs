using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace VideoScriptEditor.Collections
{
    /// <summary>
    /// Extension methods and helpers for collections.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Move item at <paramref name="oldIndex"/> to <paramref name="newIndex"/>.
        /// </summary>
        /// <remarks>
        /// Adapted from <see cref="ObservableCollection{T}.MoveItem(int, int)"/>
        /// at https://github.com/dotnet/runtime/blob/master/src/libraries/System.ObjectModel/src/System/Collections/ObjectModel/ObservableCollection.cs.
        /// Licensed to the .NET Foundation under one or more agreements.
        /// The .NET Foundation licenses this file to you under the MIT license.
        /// See https://github.com/dotnet/runtime/blob/master/LICENSE.TXT for more information.
        /// </remarks>
        public static void MoveItem<T>(this List<T> list, int oldIndex, int newIndex)
        {
            T removedItem = list[oldIndex];

            list.RemoveAt(oldIndex);
            list.Insert(newIndex, removedItem);
        }

        /// <summary>
        /// Searches the sorted <see cref="List{T}"/> for an element
        /// whose property value matches the conditions defined by the specified comparison delegate,
        /// and returns the zero-based index of the element.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="list">The sorted <see cref="List{T}"/> to search.</param>
        /// <param name="itemPropertyValueComparisonDelegate">
        /// The delegate that compares property values of the elements in the sorted <see cref="List{T}"/>.
        /// </param>
        /// <returns>
        /// The zero-based index of the matching element in the sorted <see cref="List{T}"/>, if a matching element is found;
        /// otherwise, a negative number that is the bitwise complement of the index of the next element that is larger
        /// or, if there is no larger element, the bitwise complement of <see cref="List{T}.Count"/>.
        /// </returns>
        public static int BinarySearchByItemPropertyValue<T>(this List<T> list, Func<T, int> itemPropertyValueComparisonDelegate) where T : class
        {
            return list.BinarySearch(null,  // Searching by item property value, not item
                                     new ItemPropertyValueDelegateComparer<T>(itemPropertyValueComparisonDelegate));
        }

        /// <summary>
        /// Compares property values in a sorted collection using a delegate to perform the comparison logic.
        /// </summary>
        /// <typeparam name="TItem">The type of the elements in the collection.</typeparam>
        private class ItemPropertyValueDelegateComparer<TItem> : IComparer<TItem>
        {
            // The delegate performing the comparison logic.
            private readonly Func<TItem, int> _itemPropertyValueComparisonDelegate;

            /// <summary>
            /// Creates a new instance of the <see cref="ItemPropertyValueDelegateComparer{TItem}"/>.
            /// </summary>
            /// <param name="itemPropertyValueComparisonDelegate">The delegate to perform the comparison logic.</param>
            public ItemPropertyValueDelegateComparer(Func<TItem, int> itemPropertyValueComparisonDelegate)
            {
                _itemPropertyValueComparisonDelegate = itemPropertyValueComparisonDelegate;
            }

            /// <inheritdoc/>
            public int Compare([AllowNull] TItem x, [AllowNull] TItem y)
                => _itemPropertyValueComparisonDelegate(x);
        }
    }
}
