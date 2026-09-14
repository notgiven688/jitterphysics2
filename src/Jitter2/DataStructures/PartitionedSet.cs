/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jitter2.DataStructures;

/// <summary>
/// Defines an index property used by <see cref="PartitionedSet{T}"/> to track element positions.
/// </summary>
public interface IPartitionedSetIndex
{
    /// <summary>
    /// Gets or sets the index of the element within the <see cref="PartitionedSet{T}"/>.
    /// A value of -1 indicates the element is not part of any set.
    /// </summary>
    int SetIndex { get; set; }
}

/// <summary>
/// A read-only wrapper around <see cref="PartitionedSet{T}"/>.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public readonly struct ReadOnlyPartitionedSet<T>(PartitionedSet<T> partitionedSet) : IEnumerable<T>
    where T : class, IPartitionedSetIndex
{
    /// <summary>Gets the number of active elements.</summary>
    public int ActiveCount => partitionedSet.ActiveCount;

    /// <summary>Gets the total number of elements.</summary>
    public int Count => partitionedSet.Count;

    /// <summary>
    /// Returns a read-only span of all elements in the set.
    /// </summary>
    public ReadOnlySpan<T> Elements => partitionedSet.Elements;

    /// <summary>
    /// Returns a read-only span of the active elements in the set.
    /// </summary>
    public ReadOnlySpan<T> Active => partitionedSet.Active;

    /// <summary>
    /// Returns a read-only span of the inactive elements in the set.
    /// </summary>
    public ReadOnlySpan<T> Inactive => partitionedSet.Inactive;

    /// <summary>Gets the element at the specified index.</summary>
    public T this[int i] => partitionedSet[i];

    /// <summary>Determines whether the set contains the specified element.</summary>
    public bool Contains(T element) => partitionedSet.Contains(element);

    /// <summary>Determines whether the specified element is in the active partition.</summary>
    public bool IsActive(T element) => partitionedSet.IsActive(element);

    public PartitionedSet<T>.Enumerator GetEnumerator()
    {
        return new PartitionedSet<T>.Enumerator(partitionedSet);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Represents a collection of objects that can be partitioned into active and inactive subsets.
/// </summary>
/// <typeparam name="T">The type of elements in the set, which must implement <see cref="IPartitionedSetIndex"/>.</typeparam>
/// <remarks>
/// The methods <see cref="Add(T, bool)"/>, <see cref="Remove(T)"/>, <see cref="Contains(T)"/>, <see cref="IsActive(T)"/>,
/// <see cref="MoveToActive(T)"/>, and <see cref="MoveToInactive(T)"/> all operate in O(1) time complexity.
/// </remarks>
public class PartitionedSet<T> : IEnumerable<T> where T : class, IPartitionedSetIndex
{
    public struct Enumerator(PartitionedSet<T> partitionedSet) : IEnumerator<T>
    {
        private int index = -1;

        public readonly T Current => (index >= 0 ? partitionedSet[index] : null)!;

        readonly object IEnumerator.Current => Current;

        public readonly void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (index < partitionedSet.Count - 1)
            {
                index++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            index = -1;
        }
    }

    private readonly int initialSize;
    private T[] elements;

    /// <summary>Gets the number of active elements in the set.</summary>
    public int ActiveCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PartitionedSet{T}"/> class.
    /// </summary>
    /// <param name="initialSize">The initial capacity of the internal array.</param>
    public PartitionedSet(int initialSize = 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialSize, 1);

        this.initialSize = initialSize;
        elements = new T[initialSize];
    }

    /// <summary>Gets the number of elements the set can hold before the next resize.</summary>
    public int Capacity => elements.Length;

    /// <summary>Gets the element at the specified index.</summary>
    public T this[int i] => elements[i];

    /// <summary>Returns a span of all elements in the set.</summary>
    public ReadOnlySpan<T> Elements => elements.AsSpan()[..Count];

    /// <summary>Returns a span of active elements in the set.</summary>
    public ReadOnlySpan<T> Active => elements.AsSpan()[..ActiveCount];

    /// <summary>Returns a span of inactive elements in the set.</summary>
    public ReadOnlySpan<T> Inactive => elements.AsSpan()[ActiveCount..Count];

    /// <summary>Removes all elements from the set.</summary>
    public void Clear()
    {
        for (int i = 0; i < Count; i++)
        {
            elements[i].SetIndex = -1;
            elements[i] = null!;
        }

        Count = 0;
        ActiveCount = 0;
    }

    /// <summary>Gets the total number of elements in the set.</summary>
    public int Count { get; private set; }

    /// <summary>
    /// Shrinks the internal storage to the smallest retained capacity that can hold the current elements.
    /// </summary>
    /// <returns><see langword="true"/> if the set was resized; otherwise, <see langword="false"/>.</returns>
    public bool Trim()
    {
        int newSize = initialSize;

        while (newSize < Count)
        {
            newSize = checked(newSize * 2);
        }

        if (newSize >= elements.Length) return false;

        Array.Resize(ref elements, newSize);
        return true;
    }

    /// <summary>Returns a span of all elements in the set.</summary>
    public Span<T> AsSpan() => this.elements.AsSpan(0, Count);

    /// <summary>
    /// Adds an element to the set.
    /// </summary>
    /// <param name="element">The element to add.</param>
    /// <param name="active">If <see langword="true"/>, the element is added to the active partition.</param>
    public void Add(T element, bool active = false)
    {
        Debug.Assert(element.SetIndex == -1);

        if (Count == elements.Length)
        {
            Array.Resize(ref elements, elements.Length * 2);
        }

        element.SetIndex = Count;
        elements[Count++] = element;

        if (active) MoveToActive(element);
    }

    private void Swap(int index0, int index1)
    {
        (elements[index0], elements[index1]) =
            (elements[index1], elements[index0]);

        elements[index0].SetIndex = index0;
        elements[index1].SetIndex = index1;
    }

    /// <summary>
    /// Determines whether the specified element is in the active partition.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <returns><see langword="true"/> if the element is active; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsActive(T element)
    {
        Debug.Assert(element.SetIndex != -1);
        Debug.Assert(elements[element.SetIndex] == element);

        return (element.SetIndex < ActiveCount);
    }

    /// <summary>
    /// Moves an element to the active partition.
    /// </summary>
    /// <param name="element">The element to move.</param>
    /// <returns><see langword="true"/> if the element was moved; <see langword="false"/> if it was already active.</returns>
    public bool MoveToActive(T element)
    {
        Debug.Assert(element.SetIndex != -1);
        Debug.Assert(elements[element.SetIndex] == element);

        int index = element.SetIndex;
        if (index < ActiveCount) return false;
        if (index != ActiveCount) Swap(ActiveCount, index);
        ActiveCount += 1;
        return true;
    }

    /// <summary>
    /// Moves an element to the inactive partition.
    /// </summary>
    /// <param name="element">The element to move.</param>
    /// <returns><see langword="true"/> if the element was moved; <see langword="false"/> if it was already inactive.</returns>
    public bool MoveToInactive(T element)
    {
        Debug.Assert(element.SetIndex != -1);
        Debug.Assert(elements[element.SetIndex] == element);

        int index = element.SetIndex;
        if (index >= ActiveCount) return false;
        ActiveCount -= 1;
        if (index != ActiveCount) Swap(ActiveCount, index);
        return true;
    }

    /// <summary>
    /// Determines whether the set contains the specified element.
    /// </summary>
    /// <param name="element">The element to locate.</param>
    /// <returns><see langword="true"/> if the element is found; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T element)
    {
        if (element.SetIndex >= Count || element.SetIndex < 0) return false;
        return (elements[element.SetIndex] == element);
    }

    /// <summary>
    /// Removes the specified element from the set.
    /// </summary>
    /// <param name="element">The element to remove.</param>
    public void Remove(T element)
    {
        Debug.Assert(element.SetIndex != -1);
        Debug.Assert(elements[element.SetIndex] == element);

        MoveToInactive(element);

        int index = element.SetIndex;
        int lastIndex = Count - 1;

        Count = lastIndex;

        if (index != lastIndex)
        {
            T moved = elements[lastIndex];
            elements[index] = moved;
            moved.SetIndex = index;
        }

        elements[lastIndex] = null!;

        element.SetIndex = -1;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
