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
public readonly struct ReadOnlyList<T>(List<T> list) : IReadOnlyCollection<T>
{
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

    public int Count => list.Count;
}