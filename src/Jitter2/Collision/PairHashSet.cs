/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Jitter2.Collision;

/// <summary>
/// A hash set implementation which stores unordered pairs of (int, int) values.
/// The implementation is based on open addressing with power-of-two sizing.
/// </summary>
/// <remarks>
/// <para>
/// Pairs are stored in a canonical form where the smaller ID comes first.
/// A pair with <see cref="Pair.ID"/> equal to zero is treated as an empty slot.
/// </para>
/// <para>
/// This class is not generally thread-safe. Only <see cref="ConcurrentAdd"/> may be called
/// concurrently from multiple threads. All other methods require external synchronization.
/// </para>
/// </remarks>
internal unsafe class PairHashSet : IEnumerable<PairHashSet.Pair>
{
    /// <summary>
    /// Represents an unordered pair of integer IDs stored in canonical form.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct Pair
    {
        /// <summary>Combined 64-bit identifier for the pair.</summary>
        [FieldOffset(0)] public readonly long ID;

        /// <summary>The smaller of the two IDs.</summary>
        [FieldOffset(0)] public readonly int ID1;

        /// <summary>The larger of the two IDs.</summary>
        [FieldOffset(4)] public readonly int ID2;

        /// <summary>A zero pair representing an empty slot.</summary>
        public static readonly Pair Zero = new();

        /// <summary>
        /// Creates a pair from two IDs, storing them in canonical order.
        /// </summary>
        /// <param name="id1">First ID.</param>
        /// <param name="id2">Second ID.</param>
        public Pair(int id1, int id2)
        {
            if (id1 < id2)
            {
                (ID1, ID2) = (id1, id2);
            }
            else
            {
                (ID1, ID2) = (id2, id1);
            }
        }

        /// <summary>
        /// Computes a hash code for the pair.
        /// </summary>
        /// <returns>The hash code.</returns>
        public int GetHash()
        {
            return HashCode.Combine(ID1, ID2);
        }
    }

    /// <summary>
    /// Enumerates non-empty pairs in the hash set.
    /// </summary>
    public struct Enumerator(PairHashSet hashSet) : IEnumerator<Pair>
    {
        private int index = -1;

        /// <inheritdoc/>
        public readonly Pair Current => hashSet.Slots[index];

        readonly object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public readonly void Dispose()
        {
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            var slots = hashSet.Slots;

            while (index < slots.Length - 1)
            {
                if (slots[++index].ID != 0) return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            index = -1;
        }
    }

    /// <summary>
    /// The internal storage array for pairs. Empty slots have <see cref="Pair.ID"/> equal to zero.
    /// </summary>
    public Pair[] Slots = [];
    private int count;

    /// <summary>
    /// Minimum number of slots in the hash set (128 KB at 8 bytes per slot).
    /// </summary>
    public const int MinimumSize = 16384;

    /// <summary>
    /// Factor used to determine when to shrink the hash set.
    /// </summary>
    public const int TrimFactor = 8;

    /// <summary>
    /// Gets the number of pairs in the hash set.
    /// </summary>
    public int Count => count;

    private static int PickSize(int size = -1)
    {
        int p2 = MinimumSize;
        while (p2 < size)
        {
            p2 *= 2;
        }

        return p2;
    }

    /// <summary>
    /// Removes all pairs from the hash set.
    /// </summary>
    /// <remarks>
    /// This method is not thread-safe. Do not call concurrently with any other method.
    /// </remarks>
    public void Clear()
    {
        Array.Clear(Slots, 0, Slots.Length);
        count = 0;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PairHashSet"/> class.
    /// </summary>
    public PairHashSet()
    {
        Resize(PickSize());
    }

    private void Resize(int size)
    {
        if (Slots.Length == size) return;

        Logger.Information("{0}: Resizing from {1} to {2} elements.", nameof(PairHashSet), Slots.Length, size);

        var newSlots = new Pair[size];

        for (int i = 0; i < Slots.Length; i++)
        {
            Pair pair = Slots[i];
            if (pair.ID != 0)
            {
                int hash = pair.GetHash();
                int hashIndex = FindSlot(newSlots, hash, pair.ID);
                newSlots[hashIndex] = pair;
            }
        }

        Slots = newSlots;
    }

    private static int FindSlot(Pair[] slots, int hash, long id)
    {
        int modder = slots.Length - 1;

        hash &= modder;

        while (true)
        {
            if (slots[hash].ID == 0 || slots[hash].ID == id) return hash;
            hash = (hash + 1) & modder;
        }
    }

    /// <summary>
    /// Checks whether the hash set contains the specified pair.
    /// </summary>
    /// <param name="pair">The pair to check.</param>
    /// <returns><c>true</c> if the pair exists; otherwise, <c>false</c>.</returns>
    public bool Contains(Pair pair)
    {
        int hash = pair.GetHash();
        int hashIndex = FindSlot(Slots, hash, pair.ID);
        return Slots[hashIndex].ID != 0;
    }

    /// <summary>
    /// Adds a pair to the hash set.
    /// </summary>
    /// <param name="pair">The pair to add.</param>
    /// <returns><c>true</c> if the pair was added; <c>false</c> if it already exists.</returns>
    /// <remarks>
    /// This method is not thread-safe.
    /// </remarks>
    public bool Add(Pair pair)
    {
        int hash = pair.GetHash();
        int hashIndex = FindSlot(Slots, hash, pair.ID);

        if (Slots[hashIndex].ID == 0)
        {
            Slots[hashIndex] = pair;
            Interlocked.Increment(ref count);

            if (Slots.Length < 2 * count)
            {
                Resize(PickSize(Slots.Length * 2));
            }

            return true;
        }

        return false;
    }

    private Jitter2.Parallelization.ReaderWriterLock rwLock;

    /// <summary>
    /// Attempts to add a pair to the hash set in a thread-safe manner.
    /// </summary>
    /// <remarks>
    /// Multiple threads may call <see cref="ConcurrentAdd"/> concurrently.
    /// However, no other methods (including <see cref="Add"/>, <see cref="Remove(Pair)"/>,
    /// <see cref="Clear"/>, or enumeration) may be called concurrently with this method.
    /// </remarks>
    /// <param name="pair">The pair to add.</param>
    /// <returns><c>true</c> if the pair was added; <c>false</c> if it already exists.</returns>
    public bool ConcurrentAdd(Pair pair)
    {
        int hash = pair.GetHash();

        // Fast path: This is a *huge* optimization in case of frequent additions
        // of already existing entries. Entirely bypassing any locks or synchronization.
        int fpHashIndex = FindSlot(Slots, hash, pair.ID);
        if (Slots[fpHashIndex].ID != 0) return false;

        rwLock.EnterReadLock();

        fixed (Pair* slotsPtr = Slots)
        {
            while (true)
            {
                var hashIndex = FindSlot(Slots, hash, pair.ID);
                var slotPtr = &slotsPtr[hashIndex];

                if (slotPtr->ID == pair.ID)
                {
                    rwLock.ExitReadLock();
                    return false;
                }

                if (Interlocked.CompareExchange(ref *(long*)slotPtr, pair.ID, 0) != 0)
                {
                    continue;
                }

                Interlocked.Increment(ref count);
                rwLock.ExitReadLock();

                if (Slots.Length < 2 * count)
                {
                    rwLock.EnterWriteLock();

                    // check if another thread already performed a resize.
                    if (Slots.Length < 2 * count)
                    {
                        Resize(PickSize(Slots.Length * 2));
                    }

                    rwLock.ExitWriteLock();
                }

                return true;
            } // while
        } // fixed
    }

    /// <summary>
    /// Removes the pair at the specified slot index.
    /// </summary>
    /// <param name="slot">The slot index.</param>
    /// <returns><c>true</c> if a pair was removed; <c>false</c> if the slot was empty.</returns>
    public bool Remove(int slot)
    {
        int modder = Slots.Length - 1;

        if (Slots[slot].ID == 0)
        {
            return false;
        }

        int hashJ = slot;

        while (true)
        {
            hashJ = (hashJ + 1) & modder;

            if (Slots[hashJ].ID == 0)
            {
                break;
            }

            int hashK = Slots[hashJ].GetHash() & modder;

            // https://en.wikipedia.org/wiki/Open_addressing
            if ((hashJ > slot && (hashK <= slot || hashK > hashJ)) ||
                (hashJ < slot && hashK <= slot && hashK > hashJ))
            {
                Slots[slot] = Slots[hashJ];
                slot = hashJ;
            }
        }

        Slots[slot] = Pair.Zero;
        Interlocked.Decrement(ref count);

        if (Slots.Length > MinimumSize && count * TrimFactor < Slots.Length)
        {
            Resize(PickSize(count * 2));
        }

        return true;
    }

    /// <summary>
    /// Removes the specified pair from the hash set.
    /// </summary>
    /// <param name="pair">The pair to remove.</param>
    /// <returns><c>true</c> if the pair was removed; <c>false</c> if it was not found.</returns>
    public bool Remove(Pair pair)
    {
        int hash = pair.GetHash();
        int hashIndex = FindSlot(Slots, hash, pair.ID);
        return Remove(hashIndex);
    }

    /// <inheritdoc/>
    public IEnumerator<Pair> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}