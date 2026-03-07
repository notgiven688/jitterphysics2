/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

namespace Jitter2.DataStructures;

using System;
using System.Collections.Generic;

/// <summary>
/// Append-only target for values produced by an algorithm. Designed as a minimal,
/// struct-friendly alternative to <see cref="ICollection{T}"/> for hot paths.
/// </summary>
/// <typeparam name="T">The type of value accepted by the sink.</typeparam>
public interface ISink<T>
{
    /// <summary>Appends a value to the sink.</summary>
    void Add(in T item);
}

/// <summary>
/// Adapts an <see cref="ICollection{T}"/> to the <see cref="ISink{T}"/> interface.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public readonly struct CollectionSink<T> : ISink<T>
{
    private readonly ICollection<T> collection;

    /// <summary>Creates a sink that forwards to the specified collection.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is null.</exception>
    public CollectionSink(ICollection<T> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    /// <summary>Appends a value to the wrapped collection.</summary>
    public void Add(in T item)
    {
        collection.Add(item);
    }
}
