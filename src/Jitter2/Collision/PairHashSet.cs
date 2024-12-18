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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Jitter2.Collision;

/// <summary>
/// A hash set implementation optimized for thread-safe additions of (int,int)-pairs.
/// </summary>
/// <remarks>
/// - The <see cref="Add(Pair)"/> method is thread-safe and can be called concurrently
///   from multiple threads without additional synchronization.
/// - Other operations, such as <see cref="Remove(Pair)"/> or enumeration, are NOT thread-safe
///   and require external synchronization if used concurrently.
/// </remarks>
public unsafe class PairHashSet : IEnumerable<PairHashSet.Pair>
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct Pair
    {
        [FieldOffset(0)] public readonly long ID;

        [FieldOffset(0)] public readonly int ID1;

        [FieldOffset(4)] public readonly int ID2;

        public static Pair Zero = new Pair();

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

        public int GetHash()
        {
            return (ID1 + 2281 * ID2) & 0x7FFFFFFF;
        }
    }

    public struct Enumerator : IEnumerator<Pair>
    {
        private readonly PairHashSet hashSet;
        private int index = -1;

        public Enumerator(PairHashSet hashSet)
        {
            this.hashSet = hashSet;
        }

        public readonly Pair Current => hashSet.Slots[index];

        readonly object IEnumerator.Current => Current;

        public readonly void Dispose()
        {
        }

        public bool MoveNext()
        {
            var slots = hashSet.Slots;

            while (index < slots.Length - 1)
            {
                if (slots[++index].ID != 0) return true;
            }

            return false;
        }

        public void Reset()
        {
            index = -1;
        }
    }

    public Pair[] Slots = Array.Empty<Pair>();

    private int modder = 1;

    // 16384*8/1024 KB = 128 KB
    public const int MinimumSize = 16384;
    public const int TrimFactor = 8;

    private int count;

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

    public void Clear()
    {
        Array.Clear(Slots, 0, Slots.Length);
    }

    public PairHashSet()
    {
        Resize(PickSize());
    }

    private void Resize(int size)
    {
        if (Slots.Length == size) return;
        Trace.WriteLine($"PairHashSet: Resizing {Slots.Length} -> {size}");

        var tmp = Slots;
        count = 0;

        Slots = new Pair[size];

        Interlocked.MemoryBarrier();

        modder = size - 1;

        for (int i = 0; i < tmp.Length; i++)
        {
            Pair pair = tmp[i];
            if (pair.ID != 0)
            {
                Add(pair);
            }
        }
    }

    /// <summary>
    /// Adds a pair to the hash set if it does not already exist.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe and can be called concurrently from multiple threads.
    /// However, it does NOT provide thread safety for other operations like <see cref="Remove(Pair)"/>.
    /// Ensure external synchronization if other operations are used concurrently.
    /// </remarks>
    /// <param name="pair">The pair to add.</param>
    /// <returns>
    /// <c>true</c> if the pair was added successfully; <c>false</c> if it already exists.
    /// </returns>
    public bool Add(Pair pair)
    {
        int hash = pair.GetHash();

        try_again:

        var originalSlots = Slots;

        fixed (Pair* slotsPtr = Slots)
        {
            while (true)
            {
                int hash_i = FindSlot(hash, pair.ID);
                Pair* slotPtr = &slotsPtr[hash_i];

                if (slotPtr->ID == pair.ID)
                {
                    return false;
                }

                if (slotPtr->ID == 0)
                {
                    if (Interlocked.CompareExchange(ref *(long*)slotPtr,
                            *(long*)&pair, 0) == 0)
                    {
                        if (originalSlots != Slots)
                        {
                            // Item was added to the wrong array.
                            goto try_again;
                        }

                        Interlocked.Increment(ref count);

                        if (Slots.Length < 2 * Count)
                        {
                            lock (Slots)
                            {
                                // check if another thread already did the work for us
                                if (Slots.Length < 2 * Count)
                                {
                                    Resize(PickSize(Slots.Length * 2));
                                }
                            }
                            return true;
                        }

                        return true;  // Successfully added the pair
                    }
                }

            } // while
        } // fixed
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FindSlot(int hash, long id)
    {
        hash &= modder;

        while (true)
        {
            if (Slots[hash].ID == 0 || Slots[hash].ID == id) return hash;
            hash = (hash + 1) & modder;
        }
    }

    public bool Remove(int slot)
    {
        if (Slots[slot].ID == 0)
        {
            return false;
        }

        int hash_j = slot;

        while (true)
        {
            hash_j = (hash_j + 1) & modder;

            if (Slots[hash_j].ID == 0)
            {
                break;
            }

            int hash_k = Slots[hash_j].GetHash() & modder;

            // https://en.wikipedia.org/wiki/Open_addressing
            if ((hash_j > slot && (hash_k <= slot || hash_k > hash_j)) ||
                (hash_j < slot && hash_k <= slot && hash_k > hash_j))
            {
                Slots[slot] = Slots[hash_j];
                slot = hash_j;
            }
        }

        Slots[slot] = Pair.Zero;
        count -= 1;

        if (Slots.Length > MinimumSize && Count * TrimFactor < Slots.Length)
        {
            Resize(PickSize(Count * 2));
        }

        return true;
    }

    public bool Remove(Pair pair)
    {
        int hash = pair.GetHash();
        int hash_i = FindSlot(hash, pair.ID);
        return Remove(hash_i);
    }

    public IEnumerator<Pair> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}