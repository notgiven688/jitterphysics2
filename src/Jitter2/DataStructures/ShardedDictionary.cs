/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if NET9_0_OR_GREATER
using Lock = System.Threading.Lock;
#else
using Lock = System.Object;
#endif

namespace Jitter2.DataStructures;

/// <summary>
/// A thread-safe dictionary that partitions entries across multiple shards to reduce lock contention.
/// Each shard has its own lock, allowing concurrent access to different shards.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <remarks>
/// <para>
/// This implementation avoids the GC overhead of <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/>.
/// </para>
/// <para>
/// <b>Threading:</b> Individual operations are not thread-safe. Callers must acquire the shard lock
/// via <see cref="GetLock"/> before calling <see cref="Add"/>, <see cref="Remove"/>, or the indexer.
/// </para>
/// </remarks>
internal class ShardedDictionary<TKey, TValue> where TKey : notnull
{
    private readonly Lock[] locks;
    private readonly Dictionary<TKey, TValue>[] dictionaries;

    private static int ShardSuggestion(int threads)
    {
        int[] primes = [3, 5, 7, 11, 17, 23, 29, 37, 47, 59, 71];

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

    /// <summary>Gets the value associated with the specified key.</summary>
    public TValue this[TKey key]
    {
        get
        {
            int index = GetShardIndex(key);
            return dictionaries[index][key];
        }
    }

    /// <summary>
    /// Gets the lock object for the shard containing the specified key.
    /// </summary>
    /// <param name="key">The key to locate the shard for.</param>
    /// <returns>The lock object for the shard.</returns>
    public Lock GetLock(TKey key)
    {
        return locks[GetShardIndex(key)];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="threads">The expected number of concurrent threads, used to determine shard count.</param>
    public ShardedDictionary(int threads)
    {
        int count = ShardSuggestion(threads);

        locks = new Lock[count];
        dictionaries = new Dictionary<TKey, TValue>[count];

        for (int i = 0; i < count; i++)
        {
            locks[i] = new Lock();
            dictionaries[i] = new Dictionary<TKey, TValue>();
        }
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return dictionaries[GetShardIndex(key)].TryGetValue(key, out value);
    }

    /// <summary>
    /// Adds a key-value pair to the dictionary.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to associate with the key.</param>
    public void Add(TKey key, TValue value)
    {
        dictionaries[GetShardIndex(key)].Add(key, value);
    }

    /// <summary>
    /// Removes the entry with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    public void Remove(TKey key)
    {
        dictionaries[GetShardIndex(key)].Remove(key);
    }
}