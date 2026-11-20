/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics;

/// <summary>
/// Colors the active contacts as Box2D does: a contact takes the first color that is free on
/// both of its dynamic bodies when it is added, and gives it back when it is removed. Every body
/// stores the colors in use as a bit mask, every contact its color and its place in the list of
/// that color.
/// </summary>
internal sealed class ContactColoring
{
    /// <summary>The number of colors a body mask has room for.</summary>
    internal const int MaximumColors = 32;

    /// <summary>The list of the contacts that found no color.</summary>
    internal const int OverflowColor = MaximumColors;

    private const int RadixBits = 11;

    private readonly PartitionedBuffer<ContactData> contacts;

    // One list for each color, and a last one for the contacts without a color.
    private readonly List<JHandle<ContactData>>[] lists = new List<JHandle<ContactData>>[MaximumColors + 1];
    private bool valid = true;

    /// <summary>The number of colors in use.</summary>
    internal int ColorCount { get; private set; }

    // The colors whose lists gained or lost a contact since they were last sorted.
    private ulong changed;
    private ulong[] sortKeys = [];
    private int sortCursor;

    /// <summary>
    /// The colors no longer hold, for example because a body changed its motion type. The next
    /// update colors all active contacts anew.
    /// </summary>
    internal void Invalidate() => valid = false;

    internal ReadOnlySpan<JHandle<ContactData>> Contacts(int color) => CollectionsMarshal.AsSpan(lists[color]);

    /// <summary>The contacts without a color, in the order in which they have to be solved.</summary>
    internal ReadOnlySpan<JHandle<ContactData>> Overflow => CollectionsMarshal.AsSpan(lists[OverflowColor]);

    internal int Length(int color) => lists[color].Count;

    internal ContactColoring(PartitionedBuffer<ContactData> contacts)
    {
        this.contacts = contacts;
        for (int c = 0; c < lists.Length; c++) lists[c] = [];
    }

    /// <summary>Gives an active contact a color. Does nothing if it already has one.</summary>
    internal void Add(JHandle<ContactData> contact)
    {
        ref ContactData data = ref contact.Data;
        if (data.Color != ContactData.NotColored) return;

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;
        bool dynamic1 = body1.MotionType == MotionType.Dynamic;
        bool dynamic2 = body2.MotionType == MotionType.Dynamic;

        // The first color that neither dynamic body uses. Without one the contact goes to the
        // overflow list, which the color count stands for.
        uint used = (dynamic1 ? body1.ContactColor : 0) | (dynamic2 ? body2.ContactColor : 0);
        int color = BitOperations.TrailingZeroCount(~used);

        if (color < MaximumColors)
        {
            if (dynamic1) body1.ContactColor |= 1u << color;
            if (dynamic2) body2.ContactColor |= 1u << color;
        }

        data.Color = color;
        data.ColorIndex = lists[color].Count;
        lists[color].Add(contact);
        changed |= 1UL << color;
    }

    /// <summary>Takes the color of a contact away. Does nothing if it has none.</summary>
    internal void Remove(JHandle<ContactData> contact)
    {
        ref ContactData data = ref contact.Data;
        int color = data.Color;
        if (color == ContactData.NotColored) return;

        if (color < MaximumColors)
        {
            // A body that was not dynamic when the contact was added never got the bit.
            data.Body1.Data.ContactColor &= ~(1u << color);
            data.Body2.Data.ContactColor &= ~(1u << color);
        }

        // The last contact of the list takes the place of the removed one.
        var list = lists[color];
        int last = list.Count - 1;
        JHandle<ContactData> moved = list[last];
        list[data.ColorIndex] = moved;
        moved.Data.ColorIndex = data.ColorIndex;
        list.RemoveAt(last);
        changed |= 1UL << color;

        data.Color = ContactData.NotColored;
    }

    internal void Trim()
    {
        foreach (var list in lists) list.TrimExcess();
    }

    /// <summary>
    /// Colors all active contacts from scratch, in the order of their arbiter keys. The colors and
    /// the order within them then depend only on which contacts exist, not on what happened before.
    /// </summary>
    internal void Rebuild()
    {
        ColorAll();
        CountColors();
    }

    internal void Update()
    {
        if (!valid) ColorAll();

        // Sort one changed color in every update, taking turns. A resting scene ends up with all
        // of its colors in order and then costs nothing here.
        for (int n = 0; n < MaximumColors; n++)
        {
            int color = (sortCursor + n) % MaximumColors;
            if ((changed & (1UL << color)) == 0) continue;
            Sort(color);
            changed &= ~(1UL << color);
            sortCursor = color + 1;
            break;
        }

        CountColors();
    }

    private void CountColors()
    {
        ColorCount = 0;
        for (int c = 0; c < MaximumColors; c++)
        {
            if (lists[c].Count != 0) ColorCount = c + 1;
        }
    }

    // Puts the contacts of a color in the order in which their bodies lie in memory, so that a
    // sweep through the color walks through the body records instead of jumping between them.
    private unsafe void Sort(int color)
    {
        var list = CollectionsMarshal.AsSpan(lists[color]);
        if (sortKeys.Length < list.Length) sortKeys = new ulong[Math.Max(list.Length, sortKeys.Length * 2)];
        var keys = sortKeys.AsSpan(0, list.Length);

        for (int i = 0; i < list.Length; i++)
        {
            ref ContactData data = ref list[i].Data;

            // The lower address of the two body records; a body that is not dynamic does not count.
            ref RigidBodyData body1 = ref data.Body1.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            ulong address1 = body1.MotionType == MotionType.Dynamic ? (ulong)Unsafe.AsPointer(ref body1) : ulong.MaxValue;
            ulong address2 = body2.MotionType == MotionType.Dynamic ? (ulong)Unsafe.AsPointer(ref body2) : ulong.MaxValue;
            keys[i] = Math.Min(address1, address2);
        }

        keys.Sort(list);
        for (int i = 0; i < list.Length; i++) list[i].Data.ColorIndex = i;
    }

    // Scratch for coloring from scratch, indexed by the position of a contact in the buffer.
    private ulong[] key1 = [], key2 = [];
    private int[] order = [], scratch = [];

    // Forgets every color and colors the active contacts anew, in the order of their arbiter keys.
    private void ColorAll()
    {
        foreach (var list in lists) list.Clear();

        var active = contacts.Active;
        int count = active.Length;
        if (order.Length < count)
        {
            int capacity = Math.Max(count, order.Length * 2);
            (key1, key2, order, scratch) = (new ulong[capacity], new ulong[capacity], new int[capacity], new int[capacity]);
        }

        for (int i = 0; i < count; i++)
        {
            ref ContactData data = ref active[i];
            data.Color = ContactData.NotColored;
            data.Body1.Data.ContactColor = 0;
            data.Body2.Data.ContactColor = 0;
            (key1[i], key2[i], order[i]) = (data.Key.Key1, data.Key.Key2, i);
        }

        // The sort is stable, so sorting by the second key and then by the first orders by both.
        SortByKey(key2, count);
        SortByKey(key1, count);

        for (int k = 0; k < count; k++) Add(contacts.GetHandle(ref active[order[k]]));

        valid = true;
    }

    // Sorts the indices in order by their key with a radix sort. The sort is stable.
    private void SortByKey(ulong[] keys, int count)
    {
        ulong min = ulong.MaxValue, max = 0;
        for (int k = 0; k < count; k++)
        {
            min = Math.Min(min, keys[k]);
            max = Math.Max(max, keys[k]);
        }

        Span<int> buckets = stackalloc int[1 << RadixBits];
        for (int shift = 0; shift < 64 && (max - min) >> shift != 0; shift += RadixBits)
        {
            buckets.Clear();
            for (int k = 0; k < count; k++) buckets[Digit(keys[order[k]] - min, shift)]++;

            int offset = 0;
            for (int b = 0; b < buckets.Length; b++)
            {
                int size = buckets[b];
                buckets[b] = offset;
                offset += size;
            }

            for (int k = 0; k < count; k++)
            {
                int index = order[k];
                scratch[buckets[Digit(keys[index] - min, shift)]++] = index;
            }

            (order, scratch) = (scratch, order);
        }
    }

    private static int Digit(ulong key, int shift) => (int)(key >> shift) & ((1 << RadixBits) - 1);
}
