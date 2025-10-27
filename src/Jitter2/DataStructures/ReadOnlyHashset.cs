/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections;
using System.Collections.Generic;

namespace Jitter2.DataStructures;

/// <summary>
/// Implements a read-only wrapper for <see cref="HashSet{T}"/>.
/// </summary>
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

    public void CopyTo(T[] array) => hashset.CopyTo(array);

    public void CopyTo(T[] array, int arrayIndex) => hashset.CopyTo(array, arrayIndex);

    public int Count => hashset.Count;
}