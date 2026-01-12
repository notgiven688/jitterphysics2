using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitter2.Internal;

internal static class GeometryInput
{
    /// <summary>
    /// Normalizes geometry input into a ReadOnlySpan.
    /// Allocates a backing array if necessary.
    /// </summary>
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(IEnumerable<T> elements, out T[]? backingArray)  where T : struct
    {
        backingArray = null;

        switch (elements)
        {
            case T[] array:
                return array;
            case List<T> list:
                return CollectionsMarshal.AsSpan(list);
            default:
                backingArray = elements.ToArray();
                return backingArray;
        }
    }
}