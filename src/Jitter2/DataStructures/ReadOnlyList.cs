/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections;
using System.Collections.Generic;

namespace Jitter2.DataStructures;

/// <summary>
/// Implements a read-only wrapper for <see cref="List{T}"/>.
/// </summary>
public readonly struct ReadOnlyList<T>(List<T> list) : IReadOnlyList<T>
{
    /// <summary>Gets the element at the specified index.</summary>
    public T this[int i] => list[i];

    public List<T>.Enumerator GetEnumerator()
    {
        return list.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return list.GetEnumerator();
    }

    /// <summary>Gets the number of elements in the list.</summary>
    public int Count => list.Count;
}