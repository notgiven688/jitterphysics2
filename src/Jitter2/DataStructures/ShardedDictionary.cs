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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Jitter2.DataStructures;

// The System.Collections.Concurrent.ConcurrentDictionary produces too much garbage.
// Maybe we can use it in the future, but for now we use our own implementation.

internal class ShardedDictionary<TKey, TValue> where TKey : notnull
{
    private readonly object[] locks;
    private readonly Dictionary<TKey, TValue>[] dictionaries;

    private static int ShardSuggestion(int threads)
    {
        int[] primes = { 3, 5, 7, 11, 17, 23, 29, 37, 47, 59, 71 };

        foreach (int prime in primes)
        {
            if (prime >= threads)
                return prime;
        }

        return primes[^1];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetShardIndex(TKey key)
    {
        return (key.GetHashCode() & 0x7FFFFFFF) % locks.Length;
    }

    public TValue this[TKey key]
    {
        get
        {
            int index = GetShardIndex(key);
            return dictionaries[index][key];
        }
    }

    public object GetLock(TKey key)
    {
        return locks[GetShardIndex(key)];
    }

    public ShardedDictionary(int threads)
    {
        int count = ShardSuggestion(threads);

        locks = new object[count];
        dictionaries = new Dictionary<TKey, TValue>[count];

        for (int i = 0; i < count; i++)
        {
            locks[i] = new object();
            dictionaries[i] = new Dictionary<TKey, TValue>();
        }
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return dictionaries[GetShardIndex(key)].TryGetValue(key, out value);
    }

    public void Add(TKey key, TValue value)
    {
        dictionaries[GetShardIndex(key)].Add(key, value);
    }

    public void Remove(TKey key)
    {
        dictionaries[GetShardIndex(key)].Remove(key);
    }
}