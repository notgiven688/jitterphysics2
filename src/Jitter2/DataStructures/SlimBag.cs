/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Jitter2.DataStructures;

/// <summary>
/// A data structure based on an array, without a fixed order. Removing an element at position n
/// results in the last element of the array being moved to position n, with the <see cref="Count"/>
/// decrementing by one.
/// </summary>
/// <typeparam name="T">The type of elements in the SlimBag.</typeparam>
internal class SlimBag<T>
{
    private T[] array;
    private int counter;
    private int nullOut;
    private readonly IEqualityComparer<T> comparer = EqualityComparer<T>.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlimBag{T}"/> class with a specified initial size.
    /// </summary>
    /// <param name="initialSize">The initial size of the internal array. Defaults to 4 if not specified.</param>
    public SlimBag(int initialSize = 4)
    {
        array = new T[initialSize];
        nullOut = 0;
    }

    /// <summary>
    /// Gets the length of the internal array.
    /// </summary>
    /// <returns>The length of the internal array.</returns>
    public int InternalSize
    {
        get => array.Length;
    }

    /// <summary>
    /// Returns a span representing the valid portion of the internal array.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/> representing the valid portion of the internal array.</returns>
    public Span<T> AsSpan()
    {
        return new Span<T>(array, 0, counter);
    }

    /// <summary>
    /// Adds a range of elements to the <see cref="SlimBag{T}"/>.
    /// </summary>
    /// <param name="list">The collection of elements to add.</param>
    public void AddRange(IEnumerable<T> list)
    {
        foreach (T elem in list) Add(elem);
    }

    /// <summary>
    /// Adds an element to the <see cref="SlimBag{T}"/>.
    /// </summary>
    /// <param name="item">The element to add.</param>
    public void Add(T item)
    {
        if (counter == array.Length)
        {
            Array.Resize(ref array, array.Length * 2);
        }

        array[counter++] = item;
        nullOut = counter;
    }

    /// <summary>
    /// Removes the first occurrence of a specific element from the <see cref="SlimBag{T}"/>.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    public void Remove(T item)
    {
        int index = -1;
        for (int i = 0; i < counter; i++)
        {
            if (comparer.Equals(item, array[i]))
            {
                index = i;
                break;
            }
        }

        if (index != -1) RemoveAt(index);
    }

    /// <summary>
    /// Removes the element at the specified index from the <see cref="SlimBag{T}"/>.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        array[index] = array[--counter];
    }

    /// <summary>
    /// Gets or sets the number of elements contained in the <see cref="SlimBag{T}"/>.
    /// </summary>
    public int Count
    {
        get => counter;
        set
        {
            Debug.Assert(value <= counter);
            counter = value;
        }
    }

    /// <summary>
    /// Gets the element at the specified index. Note that index should be smaller
    /// than <see cref="Count"/>, however this is not enforced.
    /// </summary>
    /// <param name="i">The zero-based index of the element to get.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int i]
    {
        get => array[i];
    }

    /// <summary>
    /// Removes all elements from the <see cref="SlimBag{T}"/>.
    /// </summary>
    public void Clear()
    {
        counter = 0;
    }

    /// <summary>
    /// Sets unused positions in the internal array to their default values.
    /// </summary>
    public void NullOut()
    {
        Array.Clear(array, counter, nullOut - counter);
        nullOut = counter;
    }

    /// <summary>
    /// Null out a single position in the internal array.
    /// </summary>
    public void NullOutOne()
    {
        if(nullOut <= counter) return;
        array[--nullOut] = default!;
    }
}