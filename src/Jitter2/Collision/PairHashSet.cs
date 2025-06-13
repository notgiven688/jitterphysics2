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
using System.Runtime.InteropServices;
using System.Threading;

namespace Jitter2.Collision;

/// <summary>
/// A hash set implementation which stores pairs of (int, int) values.
/// The implementation is based on open addressing.
/// </summary>
internal unsafe class PairHashSet : IEnumerable<PairHashSet.Pair>
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly struct Pair
    {
        [FieldOffset(0)] public readonly long ID;

        [FieldOffset(0)] public readonly int ID1;

        [FieldOffset(4)] public readonly int ID2;

        public static Pair Zero = new();

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
            return HashCode.Combine(ID1, ID2);
        }
    }

    public struct Enumerator(PairHashSet hashSet) : IEnumerator<Pair>
    {
        private int index = -1;

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

    public Pair[] Slots = [];
    private int count;

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

    public bool Contains(Pair pair)
    {
        int hash = pair.GetHash();
        int hashIndex = FindSlot(Slots, hash, pair.ID);
        return Slots[hashIndex].ID != 0;
    }

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

    public bool ConcurrentAdd(Pair pair)
    {
        int hash = pair.GetHash();

        // Fast path: This is a *huge* optimization for the case of frequent additions
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

    public bool Remove(Pair pair)
    {
        int hash = pair.GetHash();
        int hashIndex = FindSlot(Slots, hash, pair.ID);
        return Remove(hashIndex);
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