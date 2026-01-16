/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Jitter2.Internal;

public static class GeometryInput
{
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(IEnumerable<T> elements, out T[]? backingArray) where T : struct
    {
        backingArray = null;

        switch (elements)
        {
            // 1. Array: Zero-copy, just return it.
            case T[] array:
                return array;

            // 2. List<T>: Zero-copy via Marshal.
            case List<T> list:
                return CollectionsMarshal.AsSpan(list);

            // 3. ICollection<T>: Fast copy (Count is known).
            // Covers HashSet, LinkedList, Queue, Stack, etc.
            case ICollection<T> collection:
                backingArray = new T[collection.Count];
                collection.CopyTo(backingArray, 0);
                return backingArray;

            // 4. IReadOnlyCollection<T>: Optimized allocation (Count is known).
            // Covers custom collections that only expose read-only interfaces.
            case IReadOnlyCollection<T> readOnlyCol:
                backingArray = new T[readOnlyCol.Count];
                int i = 0;
                foreach (var item in readOnlyCol)
                {
                    backingArray[i++] = item;
                }

                return backingArray;

            // 5. General Fallback: Iteration required (Size unknown).
            default:
                var buffer = new List<T>();

                foreach (var item in elements)
                {
                    buffer.Add(item);
                }

                backingArray = buffer.ToArray();
                return backingArray;
        }
    }

}