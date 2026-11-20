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
using Jitter2.Dynamics.Constraints;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics;

/// <summary>
/// Colors the active constraints in the same way as <see cref="ContactColoring"/> colors the
/// contacts: a constraint takes the first color that is free on both of its dynamic bodies when it
/// is added, and gives it back when it is removed. Every body stores the colors in use by its
/// constraints as a bit mask, every constraint its color and its place in the list of that color.
/// </summary>
/// <remarks>
/// A disabled constraint keeps its place and its color. The solver skips it.
/// </remarks>
internal sealed class ConstraintColoring
{
    /// <summary>The number of colors a body mask has room for.</summary>
    internal const int MaximumColors = 32;

    /// <summary>The list of the constraints that found no color.</summary>
    internal const int OverflowColor = MaximumColors;

    /// <summary>A constraint in the list of its color.</summary>
    internal struct Entry
    {
        internal Constraint Constraint;
        // Small constraints share the header of the full ones: dispatch, id and bodies.
        internal JHandle<ConstraintData> Handle;
        internal bool Small;
    }

    // One list for each color, and a last one for the constraints without a color.
    private readonly List<Entry>[] lists = new List<Entry>[MaximumColors + 1];
    // The colors whose lists gained or lost a constraint since they were last sorted.
    private ulong changed;
    private Entry[] all = [];
    private ulong[] sortKeys = [];
    private int sortCursor;
    private bool keepColors, valid = true, dirty;

    internal ConstraintColoring()
    {
        for (int c = 0; c < lists.Length; c++) lists[c] = [];
    }

    /// <summary>The number of colors in use.</summary>
    internal int ColorCount { get; private set; }

    /// <summary>The constraints of a color.</summary>
    internal ReadOnlySpan<Entry> Constraints(int color) => CollectionsMarshal.AsSpan(lists[color]);

    /// <summary>The number of constraints of a color.</summary>
    internal int Length(int color) => lists[color].Count;

    /// <summary>
    /// Whether constraints keep their colors when others are added or removed. Without it, all
    /// constraints are colored anew in the order of their ids whenever one was added or removed;
    /// the colors then depend only on which constraints exist.
    /// </summary>
    internal bool KeepColors
    {
        get => keepColors;
        set
        {
            keepColors = value;
            // Colors that were kept depend on the past; coloring anew must not inherit them.
            dirty = true;
        }
    }

    /// <summary>
    /// The colors no longer hold, for example because a body changed its motion type. The next
    /// update colors all constraints anew.
    /// </summary>
    internal void Invalidate() => valid = false;

    internal void Trim()
    {
        all = [];
        sortKeys = [];
        foreach (var list in lists) list.TrimExcess();
    }

    /// <summary>Gives an active constraint a color. Does nothing if it already has one.</summary>
    internal void Add(Constraint constraint)
    {
        if (constraint.Color != Constraint.NotColored) return;

        ref ConstraintData data = ref constraint.Handle.Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;
        bool dynamic1 = body1.MotionType == MotionType.Dynamic;
        bool dynamic2 = body2.MotionType == MotionType.Dynamic;

        // The first color that neither dynamic body uses, or the overflow list if there is none.
        uint used = (dynamic1 ? body1.ConstraintColor : 0) | (dynamic2 ? body2.ConstraintColor : 0);
        int color = BitOperations.TrailingZeroCount(~used);

        if (color < MaximumColors)
        {
            if (dynamic1) body1.ConstraintColor |= 1u << color;
            if (dynamic2) body2.ConstraintColor |= 1u << color;
        }

        constraint.Color = color;
        constraint.ColorIndex = lists[color].Count;
        lists[color].Add(new Entry { Constraint = constraint, Handle = constraint.Handle, Small = constraint.IsSmallConstraint });
        changed |= 1UL << color;
        dirty = true;
    }

    /// <summary>
    /// Takes the color of a constraint away, while its record and its bodies are still intact. Does
    /// nothing if it has none.
    /// </summary>
    internal void Remove(Constraint constraint)
    {
        int color = constraint.Color;
        if (color == Constraint.NotColored) return;

        if (color < MaximumColors)
        {
            // A body that was not dynamic when the constraint was added never got the bit.
            ref ConstraintData data = ref constraint.Handle.Data;
            data.Body1.Data.ConstraintColor &= ~(1u << color);
            data.Body2.Data.ConstraintColor &= ~(1u << color);
        }

        // The last constraint of the list takes the place of the removed one.
        var list = lists[color];
        int last = list.Count - 1;
        Entry moved = list[last];
        list[constraint.ColorIndex] = moved;
        moved.Constraint.ColorIndex = constraint.ColorIndex;
        list.RemoveAt(last);

        constraint.Color = Constraint.NotColored;
        changed |= 1UL << color;
        dirty = true;
    }

    /// <summary>Brings the colors up to date with what was added and removed since the last update.</summary>
    internal void Update()
    {
        if (!valid || (!keepColors && dirty))
        {
            ColorAll();
        }
        else if (keepColors)
        {
            // Sort one changed color in every update, taking turns.
            for (int n = 0; n <= MaximumColors; n++)
            {
                int color = (sortCursor + n) % (MaximumColors + 1);
                if ((changed & (1UL << color)) == 0) continue;
                Sort(color);
                changed &= ~(1UL << color);
                sortCursor = color + 1;
                break;
            }
        }

        dirty = false;
        ColorCount = 0;
        for (int c = 0; c < MaximumColors; c++)
        {
            if (lists[c].Count != 0) ColorCount = c + 1;
        }
    }

    // Puts the constraints of a color in the order in which their bodies lie in memory.
    private unsafe void Sort(int color)
    {
        var list = CollectionsMarshal.AsSpan(lists[color]);
        if (sortKeys.Length < list.Length) sortKeys = new ulong[Math.Max(list.Length, sortKeys.Length * 2)];
        var keys = sortKeys.AsSpan(0, list.Length);

        for (int i = 0; i < list.Length; i++)
        {
            // The lower address of the two body records; a body that is not dynamic does not count.
            ref ConstraintData data = ref list[i].Handle.Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            ulong address1 = body1.MotionType == MotionType.Dynamic ? (ulong)Unsafe.AsPointer(ref body1) : ulong.MaxValue;
            ulong address2 = body2.MotionType == MotionType.Dynamic ? (ulong)Unsafe.AsPointer(ref body2) : ulong.MaxValue;
            keys[i] = Math.Min(address1, address2);
        }

        keys.Sort(list);
        for (int i = 0; i < list.Length; i++) list[i].Constraint.ColorIndex = i;
    }

    // Forgets every color and colors the constraints anew, in the order of their constraint ids.
    private void ColorAll()
    {
        int count = 0;
        foreach (var list in lists) count += list.Count;
        if (all.Length < count) all = new Entry[Math.Max(count, all.Length * 2)];
        if (sortKeys.Length < count) sortKeys = new ulong[Math.Max(count, sortKeys.Length * 2)];

        count = 0;
        foreach (var list in lists)
        {
            foreach (ref Entry entry in CollectionsMarshal.AsSpan(list))
            {
                ref ConstraintData data = ref entry.Handle.Data;
                data.Body1.Data.ConstraintColor = 0;
                data.Body2.Data.ConstraintColor = 0;
                entry.Constraint.Color = Constraint.NotColored;
                sortKeys[count] = data.ConstraintId;
                all[count++] = entry;
            }

            list.Clear();
        }

        sortKeys.AsSpan(0, count).Sort(all.AsSpan(0, count));
        for (int i = 0; i < count; i++) Add(all[i].Constraint);

        // Do not keep the constraints alive through the scratch array.
        Array.Clear(all, 0, count);
        changed = 0;
        valid = true;
    }
}
