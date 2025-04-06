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

namespace Jitter2.DataStructures;

// The System.Collections.Concurrent.ConcurrentDictionary produces too much garbage.
// Maybe we can use it in the future, but for now we use our own implementation.

internal class ShardedDictionary<T, K> where T : notnull
{
    private readonly object[] locks;
    private readonly Dictionary<T, K>[] dictionaries;

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

    public K this[T key]
    {
        get
        {
            int index = key.GetHashCode() % locks.Length;
            return dictionaries[index][key];
        }
    }

    public object GetLock(T key)
    {
        return locks[key.GetHashCode() % locks.Length];
    }

    public ShardedDictionary(int threads)
    {
        int count = ShardSuggestion(threads);

        locks = new object[count];
        dictionaries = new Dictionary<T, K>[count];

        for (int i = 0; i < count; i++)
        {
            locks[i] = new object();
            dictionaries[i] = new Dictionary<T, K>();
        }
    }

    public bool TryGetValue(T key, [MaybeNullWhen(false)] out K value)
    {
        int index = key.GetHashCode() % locks.Length;
        return dictionaries[index].TryGetValue(key, out value);
    }

    public void Add(T key, K value)
    {
        int index = key.GetHashCode() % locks.Length;
        dictionaries[index].Add(key, value);
    }

    public void Remove(T key)
    {
        int index = key.GetHashCode() % locks.Length;
        dictionaries[index].Remove(key);
    }
}
