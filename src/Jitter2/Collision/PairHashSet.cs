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

    public volatile Pair[] Slots = Array.Empty<Pair>();
    private readonly object locker = new object();
    private volatile int count;

    private uint generation = 0;

    // 16384*8/1024 KB = 128 KB
    public const int MinimumSize = 16384;
    public const int TrimFactor = 8;

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
        Trace.WriteLine($"PairHashSet: Resizing {Slots.Length} -> {size}");

        var tmp = Slots;

        Slots = new Pair[size];

        for (int i = 0; i < tmp.Length; i++)
        {
            Pair pair = tmp[i];
            if (pair.ID != 0)
            {
                int hash = pair.GetHash();
                int hash_i = FindSlot(Slots, hash, pair.ID);

                if (Slots[hash_i].ID == 0)
                {
                    Slots[hash_i] = pair;
                }
            }
        }
    }

    private int FindSlot(Pair[] slots, int hash, long id)
    {
        int lmodder = slots.Length - 1;

        hash &= lmodder;

        while (true)
        {
            if (slots[hash].ID == 0 || slots[hash].ID == id) return hash;
            hash = (hash + 1) & lmodder;
        }
    }

    public bool Add(Pair pair)
    {
        int hash = pair.GetHash();

        int hash_i = FindSlot(Slots, hash, pair.ID);

        if (Slots[hash_i].ID == 0)
        {
            Slots[hash_i] = pair;
            count += 1;

            if (Slots.Length < 2 * count)
            {
                Resize(PickSize(Slots.Length * 2));
            }

            return true;
        }

        return false;
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
    public void ConcurrentAdd(Pair pair)
    {
        // One could improve this further by making the resize lock-free as well
        // (using the new array as soon as it is initialized, i.e. making use of
        // "double buffering"). However, getting this right without having subtle
        // bugs is not worth the effort.

        SpinWait sw = new SpinWait();

        int hash = pair.GetHash();

        try_again:

        uint cgen1 = Volatile.Read(ref generation);

        var currentSlots = Slots;

        fixed (Pair* slotsPtr = currentSlots)
        {
            while (true)
            {
                int hash_i = FindSlot(currentSlots, hash, pair.ID);
                Pair* slotPtr = &slotsPtr[hash_i];

                if (slotPtr->ID == pair.ID)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref *(long*)slotPtr,
                        *(long*)&pair, 0) == 0)
                {
                    uint cgen2 = Volatile.Read(ref generation);

                    if (cgen1 != cgen2 || cgen1 % 2 != 0)
                    {
                        sw.SpinOnce();
                        goto try_again;
                    }

                    Interlocked.Increment(ref count);

                    if (Slots.Length < 2 * count)
                    {
                        lock (locker)
                        {
                            Interlocked.Increment(ref generation);

                            // check if another thread already did the work for us
                            if (Slots.Length < 2 * count)
                            {
                                Resize(PickSize(Slots.Length * 2));
                            }

                            Interlocked.Increment(ref generation);
                        }
                    }

                    return;
                }

            } // while
        } // fixed
    }

    public bool Remove(int slot)
    {
        int lmodder = Slots.Length - 1;

        if (Slots[slot].ID == 0)
        {
            return false;
        }

        int hash_j = slot;

        while (true)
        {
            hash_j = (hash_j + 1) & lmodder;

            if (Slots[hash_j].ID == 0)
            {
                break;
            }

            int hash_k = Slots[hash_j].GetHash() & lmodder;

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

        if (Slots.Length > MinimumSize && count * TrimFactor < Slots.Length)
        {
            Resize(PickSize(count * 2));
        }

        return true;
    }

    public bool Remove(Pair pair)
    {
        int hash = pair.GetHash();
        int hash_i = FindSlot(Slots, hash, pair.ID);
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