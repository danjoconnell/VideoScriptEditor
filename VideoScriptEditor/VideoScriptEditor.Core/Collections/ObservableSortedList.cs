/*
    Adapted from https://github.com/dotnet/runtime/blob/master/src/libraries/System.Collections/src/System/Collections/Generic/SortedList.cs
    and https://github.com/dotnet/runtime/blob/master/src/libraries/System.ObjectModel/src/System/Collections/ObjectModel/ObservableCollection.cs

    Licensed to the .NET Foundation under one or more agreements.
    The .NET Foundation licenses this file to you under the MIT license.
    See LICENSE.TXT at https://github.com/dotnet/runtime/blob/master/LICENSE.TXT
*/

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using SR = VideoScriptEditor.Core.SystemCollectionsSR;

namespace VideoScriptEditor.Collections
{
    // The SortedDictionary class implements a generic sorted list of keys
    // and values. Entries in a sorted list are sorted by their keys and
    // are accessible both by key and by index. The keys of a sorted dictionary
    // can be ordered either according to a specific IComparer
    // implementation given when the sorted dictionary is instantiated, or
    // according to the IComparable implementation provided by the keys
    // themselves. In either case, a sorted dictionary does not allow entries
    // with duplicate or null keys.
    //
    // A sorted list internally maintains two arrays that store the keys and
    // values of the entries. The capacity of a sorted list is the allocated
    // length of these internal arrays. As elements are added to a sorted list, the
    // capacity of the sorted list is automatically increased as required by
    // reallocating the internal arrays.  The capacity is never automatically
    // decreased, but users can call either TrimExcess or
    // Capacity explicitly.
    //
    // The GetKeyList and GetValueList methods of a sorted list
    // provides access to the keys and values of the sorted list in the form of
    // List implementations. The List objects returned by these
    // methods are aliases for the underlying sorted list, so modifications
    // made to those lists are directly reflected in the sorted list, and vice
    // versa.
    //
    // The SortedList class provides a convenient way to create a sorted
    // copy of another dictionary, such as a Hashtable. For example:
    //
    // Hashtable h = new Hashtable();
    // h.Add(...);
    // h.Add(...);
    // ...
    // SortedList s = new SortedList(h);
    //
    // The last line above creates a sorted list that contains a copy of the keys
    // and values stored in the hashtable. In this particular example, the keys
    // will be ordered according to the IComparable interface, which they
    // all must implement. To impose a different ordering, SortedList also
    // has a constructor that allows a specific IComparer implementation to
    // be specified.
    //

    /// <summary>
    /// Represents an observable collection of key/value pairs that are sorted by key based on the
    /// associated <see cref="IComparer{T}"/> implementation.
    /// </summary>
    /// <inheritdoc cref="SortedList{TKey, TValue}"/>
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class ObservableSortedList<TKey, TValue>
        : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged where TKey : notnull
    {
        private TKey[] keys; // Do not rename (binary serialization)
        private TValue[] values; // Do not rename (binary serialization)
        private int _size; // Do not rename (binary serialization)
        private int version; // Do not rename (binary serialization)
        private readonly IComparer<TKey> comparer; // Do not rename (binary serialization)
        private KeyList? keyList; // Do not rename (binary serialization)
        private ValueList? valueList; // Do not rename (binary serialization)

        private const int DefaultCapacity = 4;

        // Constructs a new sorted list. The sorted list is initially empty and has
        // a capacity of zero. Upon adding the first element to the sorted list the
        // capacity is increased to DefaultCapacity, and then increased in multiples of two as
        // required. The elements of the sorted list are ordered according to the
        // IComparable interface, which must be implemented by the keys of
        // all entries added to the sorted list.
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSortedList{TKey, TValue}"/> class
        /// that is empty, has the default initial capacity, and uses the default <see cref="IComparer{T}"/>.
        /// </summary>
        public ObservableSortedList()
        {
            keys = Array.Empty<TKey>();
            values = Array.Empty<TValue>();
            _size = 0;
            comparer = Comparer<TKey>.Default;
        }

        // Constructs a new sorted list. The sorted list is initially empty and has
        // a capacity of zero. Upon adding the first element to the sorted list the
        // capacity is increased to 16, and then increased in multiples of two as
        // required. The elements of the sorted list are ordered according to the
        // IComparable interface, which must be implemented by the keys of
        // all entries added to the sorted list.
        //
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSortedList{TKey, TValue}"/> class
        /// that is empty, has the specified initial capacity, and uses the default <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of elements that the <see cref="ObservableSortedList{TKey, TValue}"/> can contain.
        /// </param>
        /// <inheritdoc cref="SortedList{TKey, TValue}.SortedList(int)"/>
        public ObservableSortedList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, SR.ArgumentOutOfRange_NeedNonNegNum);
            keys = new TKey[capacity];
            values = new TValue[capacity];
            comparer = Comparer<TKey>.Default;
        }

        // Constructs a new sorted list with a given IComparer
        // implementation. The sorted list is initially empty and has a capacity of
        // zero. Upon adding the first element to the sorted list the capacity is
        // increased to 16, and then increased in multiples of two as required. The
        // elements of the sorted list are ordered according to the given
        // IComparer implementation. If comparer is null, the
        // elements are compared to each other using the IComparable
        // interface, which in that case must be implemented by the keys of all
        // entries added to the sorted list.
        //
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSortedList{TKey, TValue}"/> class
        /// that is empty, has the default initial capacity, and uses the specified <see cref="IComparer{T}"/>.
        /// </summary>
        /// <inheritdoc cref="SortedList{TKey, TValue}.SortedList(IComparer{TKey}?)"/>
        public ObservableSortedList(IComparer<TKey>? comparer)
            : this()
        {
            if (comparer != null)
            {
                this.comparer = comparer;
            }
        }

        // Constructs a new sorted dictionary with a given IComparer
        // implementation and a given initial capacity. The sorted list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required. The elements of the sorted list
        // are ordered according to the given IComparer implementation. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented
        // by the keys of all entries added to the sorted list.
        //
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSortedList{TKey, TValue}"/> class
        /// that is empty, has the default initial capacity, and uses the specified <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of elements that the <see cref="ObservableSortedList{TKey, TValue}"/> can contain.
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing keys.
        /// -or-
        /// <see langword="null" /> to use the default <see cref="IComparer{T}"/> for the type of the key.
        /// </param>
        /// <inheritdoc cref="SortedList{TKey, TValue}.SortedList(int, IComparer{TKey}?)"/>
        public ObservableSortedList(int capacity, IComparer<TKey>? comparer)
            : this(comparer)
        {
            Capacity = capacity;
        }

        // Constructs a new sorted list containing a copy of the entries in the
        // given dictionary. The elements of the sorted list are ordered according
        // to the IComparable interface, which must be implemented by the
        // keys of all entries in the given dictionary as well as keys
        // subsequently added to the sorted list.
        //
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSortedList{TKey, TValue}"/> class
        /// that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>,
        /// has sufficient capacity to accommodate the number of elements copied, and uses
        /// the default <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="dictionary">
        /// The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the
        /// new <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </param>
        /// <inheritdoc cref="SortedList{TKey, TValue}.SortedList(IDictionary{TKey, TValue})"/>
        public ObservableSortedList(IDictionary<TKey, TValue> dictionary)
            : this(dictionary, null)
        {
        }

        // Constructs a new sorted list containing a copy of the entries in the
        // given dictionary. The elements of the sorted list are ordered according
        // to the given IComparer implementation. If comparer is
        // null, the elements are compared to each other using the
        // IComparable interface, which in that case must be implemented
        // by the keys of all entries in the given dictionary as well as keys
        // subsequently added to the sorted list.
        //
        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSortedList{TKey, TValue}"/> class
        /// that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>,
        /// has sufficient capacity to accommodate the number of elements copied, and uses
        /// the specified <see cref="IComparer{T}"/>.
        /// </summary>
        /// <param name="dictionary">
        /// The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied to the
        /// new <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </param>
        /// <param name="comparer">
        /// The <see cref="IComparer{T}"/> implementation to use when comparing keys.
        /// -or-
        /// <see langword="null" /> to use the default <see cref="IComparer{T}"/> for the type of the key.
        /// </param>
        /// <inheritdoc cref="SortedList{TKey, TValue}.SortedList(IDictionary{TKey, TValue}, IComparer{TKey}?)"/>
        public ObservableSortedList(IDictionary<TKey, TValue> dictionary, IComparer<TKey>? comparer)
            : this((dictionary != null ? dictionary.Count : 0), comparer)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            int count = dictionary.Count;
            if (count != 0)
            {
                TKey[] keys = this.keys;
                dictionary.Keys.CopyTo(keys, 0);
                dictionary.Values.CopyTo(values, 0);
                Debug.Assert(count == this.keys.Length);
                if (count > 1)
                {
                    comparer = Comparer; // obtain default if this is null.
                    Array.Sort<TKey, TValue>(keys, values, comparer);
                    for (int i = 1; i != keys.Length; ++i)
                    {
                        if (comparer.Compare(keys[i - 1], keys[i]) == 0)
                        {
                            throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate, keys[i]));
                        }
                    }
                }
            }

            _size = count;
        }

        // Adds an entry with the given key and value to this sorted list. An
        // ArgumentException is thrown if the key is already present in the sorted list.
        //
        /// <summary>
        /// Adds an element with the specified key and value into the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </summary>
        /// <inheritdoc cref="SortedList{TKey, TValue}.Add(TKey, TValue)"/>
        public void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            int i = Array.BinarySearch<TKey>(keys, 0, _size, key, comparer);
            if (i >= 0)
                throw new ArgumentException(SR.Format(SR.Argument_AddingDuplicate, key), nameof(key));
            Insert(~i, key, value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            Add(keyValuePair.Key, keyValuePair.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int index = IndexOfKey(keyValuePair.Key);
            if (index >= 0 && EqualityComparer<TValue>.Default.Equals(values[index], keyValuePair.Value))
            {
                return true;
            }
            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            int index = IndexOfKey(keyValuePair.Key);
            if (index >= 0 && EqualityComparer<TValue>.Default.Equals(values[index], keyValuePair.Value))
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        // Returns the capacity of this sorted list. The capacity of a sorted list
        // represents the allocated length of the internal arrays used to store the
        // keys and values of the list, and thus also indicates the maximum number
        // of entries the list can contain before a reallocation of the internal
        // arrays is required.
        //
        /// <summary>
        /// Gets or sets the number of elements that the <see cref="ObservableSortedList{TKey, TValue}"/>
        /// can contain.
        /// </summary>
        /// <returns>
        /// The number of elements that the <see cref="ObservableSortedList{TKey, TValue}"/> can contain.
        /// </returns>
        /// <inheritdoc cref="SortedList{TKey, TValue}.Capacity"/>
        public int Capacity
        {
            get
            {
                return keys.Length;
            }
            set
            {
                if (value != keys.Length)
                {
                    if (value < _size)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value, SR.ArgumentOutOfRange_SmallCapacity);
                    }

                    if (value > 0)
                    {
                        TKey[] newKeys = new TKey[value];
                        TValue[] newValues = new TValue[value];
                        if (_size > 0)
                        {
                            Array.Copy(keys, newKeys, _size);
                            Array.Copy(values, newValues, _size);
                        }
                        keys = newKeys;
                        values = newValues;
                    }
                    else
                    {
                        keys = Array.Empty<TKey>();
                        values = Array.Empty<TValue>();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IComparer{T}"/> for the sorted list.
        /// </summary>
        /// <returns>
        /// The <see cref="IComparer{T}"/> for the current <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </returns>
        public IComparer<TKey> Comparer
        {
            get
            {
                return comparer;
            }
        }

        void IDictionary.Add(object key, object? value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null && !(default(TValue) == null))    // null is an invalid value for Value types
                throw new ArgumentNullException(nameof(value));

            if (!(key is TKey))
                throw new ArgumentException(SR.Format(SR.Arg_WrongType, key, typeof(TKey)), nameof(key));

            if (!(value is TValue) && value != null)            // null is a valid value for Reference Types
                throw new ArgumentException(SR.Format(SR.Arg_WrongType, value, typeof(TValue)), nameof(value));

            Add((TKey)key, (TValue)value!);
        }

        // Returns the number of entries in this sorted list.
        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </summary>
        /// <returns>The number of key/value pairs contained in the <see cref="ObservableSortedList{TKey, TValue}"/>.</returns>
        public int Count
        {
            get
            {
                return _size;
            }
        }

        // Returns a collection representing the keys of this sorted list. This
        // method returns the same object as GetKeyList, but typed as an
        // ICollection instead of an IList.
        /// <summary>
        /// Gets a collection containing the keys in the <see cref="ObservableSortedList{TKey, TValue}"/>,
        /// in sorted order.
        /// </summary>
        /// <returns>
        /// A <see cref="IList{T}"/> containing the keys in the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </returns>
        public IList<TKey> Keys
        {
            get
            {
                return GetKeyListHelper();
            }
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get
            {
                return GetKeyListHelper();
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return GetKeyListHelper();
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
        {
            get
            {
                return GetKeyListHelper();
            }
        }

        // Returns a collection representing the values of this sorted list. This
        // method returns the same object as GetValueList, but typed as an
        // ICollection instead of an IList.
        //
        /// <summary>
        /// Gets a collection containing the values in the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="IList{T}"/> containing the values in the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </returns>
        public IList<TValue> Values
        {
            get
            {
                return GetValueListHelper();
            }
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get
            {
                return GetValueListHelper();
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                return GetValueListHelper();
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
        {
            get
            {
                return GetValueListHelper();
            }
        }

        private KeyList GetKeyListHelper()
        {
            if (keyList == null)
                keyList = new KeyList(this);
            return keyList;
        }

        private ValueList GetValueListHelper()
        {
            if (valueList == null)
                valueList = new ValueList(this);
            return valueList;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return false; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return false; }
        }

        bool IDictionary.IsFixedSize
        {
            get { return false; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        // Synchronization root for this object.
        object ICollection.SyncRoot => this;

        // Removes all entries from this sorted list.
        /// <summary>
        /// Removes all elements from the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </summary>
        public void Clear()
        {
            // clear does not change the capacity
            version++;
            // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
            {
                Array.Clear(keys, 0, _size);
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                Array.Clear(values, 0, _size);
            }
            _size = 0;

            OnListCleared();
        }

        bool IDictionary.Contains(object key)
        {
            if (IsCompatibleKey(key))
            {
                return ContainsKey((TKey)key);
            }
            return false;
        }

        // Checks if this sorted list contains an entry with the given key.
        /// <summary>
        /// Determines whether the <see cref="ObservableSortedList{TKey, TValue}"/> contains a specific key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ObservableSortedList{TKey, TValue}"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="ObservableSortedList{TKey, TValue}"/> contains an element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        /// <inheritdoc cref="SortedList{TKey, TValue}.ContainsKey(TKey)"/>
        public bool ContainsKey(TKey key)
        {
            return IndexOfKey(key) >= 0;
        }

        // Checks if this sorted list contains an entry with the given value. The
        // values of the entries of the sorted list are compared to the given value
        // using the Object.Equals method. This method performs a linear
        // search and is substantially slower than the Contains
        // method.
        /// <summary>
        /// Determines whether the <see cref="ObservableSortedList{TKey, TValue}"/> contains a specific value.
        /// </summary>
        /// <param name="value">
        /// The value to locate in the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// The value can be <see langword="null"/> for reference types.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="ObservableSortedList{TKey, TValue}"/> contains an element with the specified value; otherwise, <see langword="false"/>.
        /// </returns>
        public bool ContainsValue(TValue value)
        {
            return IndexOfValue(value) >= 0;
        }

        // Copies the values in this SortedList to an array.
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0 || arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_Index);
            }

            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            for (int i = 0; i < Count; i++)
            {
                KeyValuePair<TKey, TValue> entry = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
                array[arrayIndex + i] = entry;
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }

            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException(SR.Arg_NonZeroLowerBound, nameof(array));
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            }

            if (array.Length - index < Count)
            {
                throw new ArgumentException(SR.Arg_ArrayPlusOffTooSmall);
            }

            KeyValuePair<TKey, TValue>[]? keyValuePairArray = array as KeyValuePair<TKey, TValue>[];
            if (keyValuePairArray != null)
            {
                for (int i = 0; i < Count; i++)
                {
                    keyValuePairArray[i + index] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
                }
            }
            else
            {
                object[]? objects = array as object[];
                if (objects == null)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }

                try
                {
                    for (int i = 0; i < Count; i++)
                    {
                        objects[i + index] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }
        }

        private const int MaxArrayLength = 0X7FEFFFFF;

        // Ensures that the capacity of this sorted list is at least the given
        // minimum value. The capacity is increased to twice the current capacity
        // or to min, whichever is larger.
        private void EnsureCapacity(int min)
        {
            int newCapacity = keys.Length == 0 ? DefaultCapacity : keys.Length * 2;
            // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newCapacity > MaxArrayLength) newCapacity = MaxArrayLength;
            if (newCapacity < min) newCapacity = min;
            Capacity = newCapacity;
        }

        // Returns the value of the entry at the given index.
        private TValue GetByIndex(int index)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            return values[index];
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator{T}"/> of type <see cref="KeyValuePair{TKey, TValue}"/> for the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.DictEntry);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this, Enumerator.KeyValuePair);
        }

        // Returns the key of the entry at the given index.
        private TKey GetKey(int index)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            return keys[index];
        }

        // Returns the value associated with the given key. If an entry with the
        // given key is not found, the returned value is null.
        /// <inheritdoc cref="P:System.Collections.Generic.SortedList`2.Item(`0)"/>
        public TValue this[TKey key]
        {
            get
            {
                int i = IndexOfKey(key);
                if (i >= 0)
                    return values[i];

                throw new KeyNotFoundException(SR.Format(SR.Arg_KeyNotFoundWithKey, key.ToString()));
            }
            set
            {
                if (((object)key) == null) throw new ArgumentNullException(nameof(key));
                int i = Array.BinarySearch<TKey>(keys, 0, _size, key, comparer);
                if (i >= 0)
                {
                    TValue oldValue = values[i];

                    values[i] = value;
                    version++;

                    OnItemReplaced(key, value, oldValue, i);
                    return;
                }
                Insert(~i, key, value);
            }
        }

        object? IDictionary.this[object key]
        {
            get
            {
                if (IsCompatibleKey(key))
                {
                    int i = IndexOfKey((TKey)key);
                    if (i >= 0)
                    {
                        return values[i];
                    }
                }

                return null;
            }
            set
            {
                if (!IsCompatibleKey(key))
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (value == null && !(default(TValue) == null))
                    throw new ArgumentNullException(nameof(value));

                TKey tempKey = (TKey)key;
                try
                {
                    this[tempKey] = (TValue)value!;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_WrongType, value, typeof(TValue)), nameof(value));
                }
            }
        }

        // Returns the index of the entry with a given key in this sorted list. The
        // key is located through a binary search, and thus the average execution
        // time of this method is proportional to Log2(size), where
        // size is the size of this sorted list. The returned value is -1 if
        // the given key does not occur in this sorted list. Null is an invalid
        // key value.
        /// <summary>
        /// Searches for the specified key and returns the zero-based index within the entire <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ObservableSortedList{TKey, TValue}"/>.</param>
        /// <returns>
        /// The zero-based index of <paramref name="key"/> within the entire <see cref="ObservableSortedList{TKey, TValue}"/>, if found; otherwise, -1.
        /// </returns>
        /// <inheritdoc cref="SortedList{TKey, TValue}.IndexOfKey(TKey)"/>
        public int IndexOfKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            int ret = Array.BinarySearch<TKey>(keys, 0, _size, key, comparer);
            return ret >= 0 ? ret : -1;
        }

        // Returns the index of the first occurrence of an entry with a given value
        // in this sorted list. The entry is located through a linear search, and
        // thus the average execution time of this method is proportional to the
        // size of this sorted list. The elements of the list are compared to the
        // given value using the Object.Equals method.
        /// <summary>
        /// Searches for the specified value and returns the zero-based index of the first occurrence within the entire <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </summary>
        /// <param name="value">
        /// The value to locate in the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// The value can be <see langword="null"/> for reference types.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of <paramref name="value"/> within the entire <see cref="ObservableSortedList{TKey, TValue}"/>, if found; otherwise, -1.
        /// </returns>
        public int IndexOfValue(TValue value)
        {
            return Array.IndexOf(values, value, 0, _size);
        }

        // Inserts an entry with a given key and value at a given index.
        private void Insert(int index, TKey key, TValue value)
        {
            if (_size == keys.Length) EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(keys, index, keys, index + 1, _size - index);
                Array.Copy(values, index, values, index + 1, _size - index);
            }
            keys[index] = key;
            values[index] = value;
            _size++;
            version++;

            OnItemAdded(key, value, index);
        }

        /// <returns>
        /// <see langword="true"/> if the <see cref="ObservableSortedList{TKey, TValue}"/> contains an element with the specified key; otherwise, <see langword="false"/>.
        /// </returns>
        /// <inheritdoc cref="SortedList{TKey, TValue}.TryGetValue(TKey, out TValue)"/>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            int i = IndexOfKey(key);
            if (i >= 0)
            {
                value = values[i];
                return true;
            }

            value = default;
            return false;
        }

        // Removes the entry at the given index. The size of the sorted list is
        // decreased by one.
        /// <summary>
        /// Removes the element at the specified index of the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is less than zero.
        /// -or-
        /// <paramref name="index"/> is equal to or greater than <see cref="ObservableSortedList{TKey, TValue}.Count"/>.
        /// </exception>
        /// <inheritdoc cref="SortedList{TKey, TValue}.RemoveAt(int)"/>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);

            KeyValuePair<TKey, TValue> removedItem = new KeyValuePair<TKey, TValue>(keys[index], values[index]);

            _size--;
            if (index < _size)
            {
                Array.Copy(keys, index + 1, keys, index, _size - index);
                Array.Copy(values, index + 1, values, index, _size - index);
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
            {
                keys[_size] = default(TKey)!;
            }
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                values[_size] = default(TValue)!;
            }
            version++;

            OnItemRemoved(removedItem, index);
        }

        // Removes an entry from this sorted list. If an entry with the specified
        // key exists in the sorted list, it is removed. An ArgumentException is
        // thrown if the key is null.
        /// <summary>
        /// Removes the element with the specified key from the <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>.
        /// This method also returns <see langword="false"/> if <paramref name="key"/> was not found in the original <see cref="ObservableSortedList{TKey, TValue}"/>.
        /// </returns>
        /// <inheritdoc cref="SortedList{TKey, TValue}.Remove(TKey)"/>
        public bool Remove(TKey key)
        {
            int i = IndexOfKey(key);
            if (i >= 0)
                RemoveAt(i);
            return i >= 0;
        }

        void IDictionary.Remove(object key)
        {
            if (IsCompatibleKey(key))
            {
                Remove((TKey)key);
            }
        }

        // Sets the capacity of this sorted list to the size of the sorted list.
        // This method can be used to minimize a sorted list's memory overhead once
        // it is known that no new elements will be added to the sorted list. To
        // completely clear a sorted list and release all memory referenced by the
        // sorted list, execute the following statements:
        //
        // SortedList.Clear();
        // SortedList.TrimExcess();
        /// <summary>
        /// Sets the capacity to the actual number of elements in the <see cref="ObservableSortedList{TKey, TValue}"/>,
        /// if that number is less than 90 percent of current capacity.
        /// </summary>
        public void TrimExcess()
        {
            int threshold = (int)(((double)keys.Length) * 0.9);
            if (_size < threshold)
            {
                Capacity = _size;
            }
        }

        private static bool IsCompatibleKey(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return (key is TKey);
        }

        private struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly ObservableSortedList<TKey, TValue> _sortedList;
            private TKey? _key;
            private TValue? _value;
            private int _index;
            private readonly int _version;
            private readonly int _getEnumeratorRetType;  // What should Enumerator.Current return?

            internal const int KeyValuePair = 1;
            internal const int DictEntry = 2;

            internal Enumerator(ObservableSortedList<TKey, TValue> sortedList, int getEnumeratorRetType)
            {
                _sortedList = sortedList;
                _index = 0;
                _version = _sortedList.version;
                _getEnumeratorRetType = getEnumeratorRetType;
                _key = default;
                _value = default;
            }

            public void Dispose()
            {
                _index = 0;
                _key = default;
                _value = default;
            }

            object IDictionaryEnumerator.Key
            {
                get
                {
                    if (_index == 0 || (_index == _sortedList.Count + 1))
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }

                    return _key!;
                }
            }

            public bool MoveNext()
            {
                if (_version != _sortedList.version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);

                if ((uint)_index < (uint)_sortedList.Count)
                {
                    _key = _sortedList.keys[_index];
                    _value = _sortedList.values[_index];
                    _index++;
                    return true;
                }

                _index = _sortedList.Count + 1;
                _key = default;
                _value = default;
                return false;
            }

            DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    if (_index == 0 || (_index == _sortedList.Count + 1))
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }

                    return new DictionaryEntry(_key!, _value);
                }
            }

            public KeyValuePair<TKey, TValue> Current => new KeyValuePair<TKey, TValue>(_key!, _value!);

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _sortedList.Count + 1))
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }

                    if (_getEnumeratorRetType == DictEntry)
                    {
                        return new DictionaryEntry(_key!, _value);
                    }
                    else
                    {
                        return new KeyValuePair<TKey, TValue>(_key!, _value!);
                    }
                }
            }

            object? IDictionaryEnumerator.Value
            {
                get
                {
                    if (_index == 0 || (_index == _sortedList.Count + 1))
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }

                    return _value;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _sortedList.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                _index = 0;
                _key = default;
                _value = default;
            }
        }

        private sealed class ObservableSortedListKeyEnumerator : IEnumerator<TKey>, IEnumerator
        {
            private readonly ObservableSortedList<TKey, TValue> _sortedList;
            private int _index;
            private readonly int _version;
            private TKey? _currentKey;

            internal ObservableSortedListKeyEnumerator(ObservableSortedList<TKey, TValue> sortedList)
            {
                _sortedList = sortedList;
                _version = sortedList.version;
            }

            public void Dispose()
            {
                _index = 0;
                _currentKey = default;
            }

            public bool MoveNext()
            {
                if (_version != _sortedList.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                if ((uint)_index < (uint)_sortedList.Count)
                {
                    _currentKey = _sortedList.keys[_index];
                    _index++;
                    return true;
                }

                _index = _sortedList.Count + 1;
                _currentKey = default;
                return false;
            }

            public TKey Current => _currentKey!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _sortedList.Count + 1))
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }

                    return _currentKey;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _sortedList.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
                _index = 0;
                _currentKey = default;
            }
        }

        private sealed class ObservableSortedListValueEnumerator : IEnumerator<TValue>, IEnumerator
        {
            private readonly ObservableSortedList<TKey, TValue> _sortedList;
            private int _index;
            private readonly int _version;
            private TValue? _currentValue;

            internal ObservableSortedListValueEnumerator(ObservableSortedList<TKey, TValue> sortedList)
            {
                _sortedList = sortedList;
                _version = sortedList.version;
            }

            public void Dispose()
            {
                _index = 0;
                _currentValue = default;
            }

            public bool MoveNext()
            {
                if (_version != _sortedList.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                if ((uint)_index < (uint)_sortedList.Count)
                {
                    _currentValue = _sortedList.values[_index];
                    _index++;
                    return true;
                }

                _index = _sortedList.Count + 1;
                _currentValue = default;
                return false;
            }

            public TValue Current => _currentValue!;

            object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _sortedList.Count + 1))
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_EnumOpCantHappen);
                    }

                    return _currentValue;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _sortedList.version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }
                _index = 0;
                _currentValue = default;
            }
        }

        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class KeyList : IList<TKey>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
        {
            private readonly ObservableSortedList<TKey, TValue> _dict; // Do not rename (binary serialization)

            internal KeyList(ObservableSortedList<TKey, TValue> dictionary)
            {
                _dict = dictionary;
            }

            public int Count
            {
                get { return _dict._size; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection)_dict).SyncRoot; }
            }

            public void Add(TKey key)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public void Clear()
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public bool Contains(TKey key)
            {
                return _dict.ContainsKey(key);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                // defer error checking to Array.Copy
                Array.Copy(_dict.keys, 0, array, arrayIndex, _dict.Count);
            }

            void ICollection.CopyTo(Array array, int arrayIndex)
            {
                if (array != null && array.Rank != 1)
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));

                try
                {
                    // defer error checking to Array.Copy
                    Array.Copy(_dict.keys, 0, array!, arrayIndex, _dict.Count);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }

            public void Insert(int index, TKey value)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public TKey this[int index]
            {
                get
                {
                    return _dict.GetKey(index);
                }
                set
                {
                    throw new NotSupportedException(SR.NotSupported_KeyCollectionSet);
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return new ObservableSortedListKeyEnumerator(_dict);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new ObservableSortedListKeyEnumerator(_dict);
            }

            public int IndexOf(TKey key)
            {
                if (((object)key) == null)
                    throw new ArgumentNullException(nameof(key));

                int i = Array.BinarySearch<TKey>(_dict.keys, 0,
                                          _dict.Count, key, _dict.comparer);
                if (i >= 0) return i;
                return -1;
            }

            public bool Remove(TKey key)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
                // return false;
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            /// <inheritdoc cref="INotifyCollectionChanged.CollectionChanged"/>
            [field: NonSerialized]
            public event NotifyCollectionChangedEventHandler? CollectionChanged;

            /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
            [field: NonSerialized]
            public event PropertyChangedEventHandler? PropertyChanged;

            /// <summary>
            /// Raises the <see cref="PropertyChanged"/> event with the provided arguments.
            /// </summary>
            /// <param name="e">Arguments of the event being raised.</param>
            private void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                PropertyChanged?.Invoke(this, e);
            }

            /// <summary>
            /// Raises the <see cref="CollectionChanged"/> event with the provided arguments.
            /// </summary>
            /// <param name="e">Arguments of the event being raised.</param>
            internal void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                if (e.Action != NotifyCollectionChangedAction.Replace)
                {
                    OnPropertyChanged(EventArgsCache.CountPropertyChanged);
                }
                OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

                CollectionChanged?.Invoke(this, e);
            }
        }

        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class ValueList : IList<TValue>, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
        {
            private readonly ObservableSortedList<TKey, TValue> _dict; // Do not rename (binary serialization)

            internal ValueList(ObservableSortedList<TKey, TValue> dictionary)
            {
                _dict = dictionary;
            }

            public int Count
            {
                get { return _dict._size; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            bool ICollection.IsSynchronized
            {
                get { return false; }
            }

            object ICollection.SyncRoot
            {
                get { return ((ICollection)_dict).SyncRoot; }
            }

            public void Add(TValue key)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public void Clear()
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public bool Contains(TValue value)
            {
                return _dict.ContainsValue(value);
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                // defer error checking to Array.Copy
                Array.Copy(_dict.values, 0, array, arrayIndex, _dict.Count);
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (array != null && array.Rank != 1)
                    throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));

                try
                {
                    // defer error checking to Array.Copy
                    Array.Copy(_dict.values, 0, array!, index, _dict.Count);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException(SR.Argument_InvalidArrayType, nameof(array));
                }
            }

            public void Insert(int index, TValue value)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            public TValue this[int index]
            {
                get
                {
                    return _dict.GetByIndex(index);
                }
                set
                {
                    throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return new ObservableSortedListValueEnumerator(_dict);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new ObservableSortedListValueEnumerator(_dict);
            }

            public int IndexOf(TValue value)
            {
                return Array.IndexOf(_dict.values, value, 0, _dict.Count);
            }

            public bool Remove(TValue value)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
                // return false;
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException(SR.NotSupported_SortedListNestedWrite);
            }

            /// <inheritdoc cref="INotifyCollectionChanged.CollectionChanged"/>
            [field: NonSerialized]
            public event NotifyCollectionChangedEventHandler? CollectionChanged;

            /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
            [field: NonSerialized]
            public event PropertyChangedEventHandler? PropertyChanged;

            /// <summary>
            /// Raises the <see cref="PropertyChanged"/> event with the provided arguments.
            /// </summary>
            /// <param name="e">Arguments of the event being raised.</param>
            private void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                PropertyChanged?.Invoke(this, e);
            }

            /// <summary>
            /// Raises the <see cref="CollectionChanged"/> event with the provided arguments.
            /// </summary>
            /// <param name="e">Arguments of the event being raised.</param>
            internal void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                if (e.Action != NotifyCollectionChangedAction.Replace)
                {
                    OnPropertyChanged(EventArgsCache.CountPropertyChanged);
                }
                OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

                CollectionChanged?.Invoke(this, e);
            }
        }

        /// <summary>
        /// Occurs when an item is added, removed, changed, moved, or the entire list is refreshed.
        /// </summary>
        [field: NonSerialized]
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
        [field: NonSerialized]
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.
        /// </summary>
        private void OnCountPropertyChanged() => OnPropertyChanged(EventArgsCache.CountPropertyChanged);

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the <see cref="this[TKey]">Indexer</see> property
        /// </summary>
        private void OnIndexerPropertyChanged() => OnPropertyChanged(EventArgsCache.IndexerPropertyChanged);

        /// <summary>
        /// Invoked whenever an item is added to the list.
        /// </summary>
        /// <remarks>
        /// Raises <see cref="PropertyChanged"/> events for the <see cref="Count"/> and <see cref="this[TKey]">Indexer</see> properties
        /// and the <see cref="CollectionChanged"/> event for the added item.
        /// </remarks>
        /// <param name="key">The key of the added item.</param>
        /// <param name="value">The value of the added item.</param>
        /// <param name="index">The index of the added item.</param>
        protected virtual void OnItemAdded(TKey key, TValue value, int index)
        {
            OnCountPropertyChanged();
            OnIndexerPropertyChanged();

            NotifyCollectionChangedAction action = NotifyCollectionChangedAction.Add;
            keyList?.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, key, index));
            valueList?.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, value, index));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, new KeyValuePair<TKey, TValue>(key, value), index));
        }

        /// <summary>
        /// Invoked whenever an item is removed from the list.
        /// </summary>
        /// <remarks>
        /// Raises <see cref="PropertyChanged"/> events for the <see cref="Count"/> and <see cref="this[TKey]">Indexer</see> properties
        /// and the <see cref="CollectionChanged"/> event for the removed item.
        /// </remarks>
        /// <param name="removedItem">>The item that has been removed.</param>
        /// <param name="index">The index where the item has been removed.</param>
        protected virtual void OnItemRemoved(KeyValuePair<TKey, TValue> removedItem, int index)
        {
            OnCountPropertyChanged();
            OnIndexerPropertyChanged();

            NotifyCollectionChangedAction action = NotifyCollectionChangedAction.Remove;
            keyList?.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, removedItem.Key, index));
            valueList?.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, removedItem.Value, index));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, removedItem, index));
        }

        /// <summary>
        /// Invoked whenever an item is replaced in the list.
        /// </summary>
        /// <remarks>
        /// Raises the <see cref="PropertyChanged"/> event for the <see cref="this[TKey]">Indexer</see> property
        /// and the <see cref="CollectionChanged"/> event for the replaced item.
        /// </remarks>
        /// <param name="key">The key of the item that has been replaced.</param>
        /// <param name="newValue">The new item value that has replaced the original item value.</param>
        /// <param name="oldValue">The original item value that has been replaced.</param>
        /// <param name="index">The index of the item that has been replaced.</param>
        protected virtual void OnItemReplaced(TKey key, TValue newValue, TValue oldValue, int index)
        {
            OnIndexerPropertyChanged();

            NotifyCollectionChangedAction action = NotifyCollectionChangedAction.Replace;
            valueList?.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newValue, oldValue, index));
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, new KeyValuePair<TKey, TValue>(key, oldValue),
                                                                                 new KeyValuePair<TKey, TValue>(key, newValue), index));
        }

        /// <summary>
        /// Invoked whenever the list is cleared.
        /// </summary>
        /// <remarks>
        /// Raises the <see cref="CollectionChanged"/> event to notify listeners
        /// that the list has been cleared.
        /// </remarks>
        protected virtual void OnListCleared()
        {
            keyList?.OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
            valueList?.OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
            CollectionChanged?.Invoke(this, EventArgsCache.ResetCollectionChanged);
        }
    }

    internal static class EventArgsCache
    {
        internal static readonly PropertyChangedEventArgs CountPropertyChanged = new PropertyChangedEventArgs("Count");
        internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new PropertyChangedEventArgs("Item[]");
        internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    }
}
