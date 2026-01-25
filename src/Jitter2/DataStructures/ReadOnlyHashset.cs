/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections;
using System.Collections.Generic;

namespace Jitter2.DataStructures;

/// <summary>
/// A read-only wrapper around <see cref="HashSet{T}"/> that prevents modification while allowing enumeration and lookup.
/// </summary>
/// <typeparam name="T">The type of elements in the set.</typeparam>
public readonly struct ReadOnlyHashSet<T>(HashSet<T> hashset) : IReadOnlyCollection<T>
{
    public HashSet<T>.Enumerator GetEnumerator()
    {
        return hashset.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return hashset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return hashset.GetEnumerator();
    }

    /// <summary>Determines whether the set contains the specified element.</summary>
    public bool Contains(T item) => hashset.Contains(item);

    /// <summary>Copies the elements of the set to an array.</summary>
    public void CopyTo(T[] array) => hashset.CopyTo(array);

    /// <summary>Copies the elements of the set to an array, starting at a particular index.</summary>
    public void CopyTo(T[] array, int arrayIndex) => hashset.CopyTo(array, arrayIndex);

    /// <summary>Gets the number of elements in the set.</summary>
    public int Count => hashset.Count;
}