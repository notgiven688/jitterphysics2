/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Jitter2.DataStructures;

/// <summary>
/// Provides utility methods for converting collections to spans.
/// </summary>
internal static class SpanHelper
{
    /// <summary>
    /// Converts an enumerable to a <see cref="ReadOnlySpan{T}"/>, using zero-copy when possible.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    /// <param name="elements">The collection to convert.</param>
    /// <returns>A read-only span over the elements.</returns>
    /// <remarks>
    /// Zero-copy paths are used for <see cref="Array"/> and <see cref="List{T}"/>.
    /// Other collection types are copied into a new array.
    /// </remarks>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(IEnumerable<T> elements) where T : struct
    {
        ArgumentNullException.ThrowIfNull(elements);

        switch (elements)
        {
            case T[] array:
                return array;

            case List<T> list:
                return CollectionsMarshal.AsSpan(list);

            // Count is known, copy directly. Covers HashSet, LinkedList, Queue, Stack, etc.
            case ICollection<T> collection:
                var copy = new T[collection.Count];
                collection.CopyTo(copy, 0);
                return copy;

            default:
                return new List<T>(elements).ToArray();
        }
    }
}
