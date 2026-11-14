/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.InteropServices;
using Jitter2.Dynamics.Constraints;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics.Solver;

/// <summary>
/// Arranges the active joints into passes of units. A unit is a run of joints that one worker
/// solves in order; the units of a pass share no dynamic body, so workers can solve them at the
/// same time without locks.
/// </summary>
/// <remarks>
/// <para>
/// Joints that are connected through dynamic bodies form a group: a ragdoll, a vehicle, a chain.
/// Groups share no dynamic body, so every group is a unit of the first pass, its joints in the
/// order of their constraint ids. A group too large to be one unit, such as a rope, a net or a
/// soft body, is colored like the contacts instead: each of its joints is a unit of the pass of
/// its color. Joints that find no color form the last pass, which is solved by a single worker.
/// </para>
/// <para>
/// The joint records stay where they are. A joint is referenced by its position in the active
/// constraints, or by the complement of its position in the active small constraints.
/// </para>
/// </remarks>
internal sealed class JointGrouping(
    PartitionedBuffer<ConstraintData> constraints, PartitionedBuffer<SmallConstraintData> smallConstraints)
    : ConstraintGraph
{
    /// <summary>The groups, then one pass for each color, then the joints without a color.</summary>
    internal const int PassCount = MaximumColors + 2;

    private const int GroupPass = 0, OverflowPass = MaximumColors + 1;
    private const int MaximumGroupSize = 256;

    // Scratch, indexed by the position of a joint: the constraints, followed by the small constraints.
    private Endpoints[] endpoints = [], knownEndpoints = [];
    private ulong[] key = [], knownKey = [];
    private int[] order = [], scratch = [], unitOf = [];
    private byte[] passOf = [];
    private int knownCount = -1;
    // Scratch, indexed by RigidBodyData._index.
    private ulong[] bodyColors = [];
    private int[] bodyGroup = [], groupSize = [], groupUnit = [];

    // Unit u holds joints[unitStart[u] .. unitStart[u + 1]); pass p holds the units
    // passStart[p] .. passStart[p + 1].
    private int[] joints = [], unitStart = new int[1];
    private readonly int[] passStart = new int[PassCount + 1];

    /// <summary>The number of units in a pass.</summary>
    internal int Units(int pass) => passStart[pass + 1] - passStart[pass];

    /// <summary>The number of joints in a pass.</summary>
    internal int Joints(int pass) => unitStart[passStart[pass + 1]] - unitStart[passStart[pass]];

    /// <summary>Whether a single worker has to solve the pass, unit after unit.</summary>
    internal static bool IsSerial(int pass) => pass == OverflowPass;

    /// <summary>The joints of a range of units of a pass, in the order in which they are solved.</summary>
    internal ReadOnlySpan<int> Joints(int pass, int firstUnit, int endUnit)
    {
        int first = unitStart[passStart[pass] + firstUnit];
        return joints.AsSpan(first, unitStart[passStart[pass] + endUnit] - first);
    }

    /// <summary>Releases the scratch memory. The next update allocates what it needs.</summary>
    internal void Trim()
    {
        endpoints = knownEndpoints = [];
        key = knownKey = bodyColors = [];
        order = scratch = unitOf = bodyGroup = groupSize = groupUnit = joints = [];
        passOf = [];
        unitStart = new int[1];
        Array.Clear(passStart);
        knownCount = -1;
    }

    /// <summary>Brings the passes up to date with the active joints.</summary>
    /// <param name="workers">The number of pool workers that may share reading the records.</param>
    internal void Update(int workers)
    {
        int constraintCount = constraints.Active.Length;
        int total = constraintCount + smallConstraints.Active.Length;
        if (endpoints.Length < total)
        {
            int capacity = Math.Max(total, endpoints.Length * 2);
            endpoints = new Endpoints[capacity];
            knownEndpoints = new Endpoints[capacity];
            key = new ulong[capacity];
            knownKey = new ulong[capacity];
            order = new int[capacity];
            scratch = new int[capacity];
            unitOf = new int[capacity];
            passOf = new byte[capacity];
            joints = new int[capacity];
            unitStart = new int[capacity + 1];
            knownCount = -1;
        }

        ReadAllEdges(total, workers);

        // Joints rarely change. When every joint still connects the same bodies as in the last
        // update, the passes still hold.
        var current = MemoryMarshal.AsBytes(endpoints.AsSpan(0, total));
        if (total == knownCount &&
            current.SequenceEqual(MemoryMarshal.AsBytes(knownEndpoints.AsSpan(0, total))) &&
            key.AsSpan(0, total).SequenceEqual(knownKey.AsSpan(0, total)))
        {
            return;
        }

        endpoints.AsSpan(0, total).CopyTo(knownEndpoints);
        key.AsSpan(0, total).CopyTo(knownKey);
        knownCount = total;

        int count = 0, bodyLimit = 0;
        for (int j = 0; j < total; j++)
        {
            Endpoints edge = endpoints[j];
            if (!edge.IsSolved) continue;
            bodyLimit = Math.Max(bodyLimit, Math.Max(edge.Body1, edge.Body2) + 1);
            order[count++] = j;
        }

        SortByKey(ref order, ref scratch, count, key);
        FindGroups(count, bodyLimit);

        // Give every joint its pass, in the order of the constraint ids. Groups are numbered as
        // the sorted joints reach them; a joint between two bodies that the solver only reads is
        // a group of its own.
        Array.Clear(bodyColors, 0, bodyLimit);
        Array.Fill(groupUnit, -1, 0, bodyLimit);
        Span<int> units = stackalloc int[PassCount];
        units.Clear();

        for (int k = 0; k < count; k++)
        {
            int j = order[k];
            int root = GroupOf(endpoints[j]);
            int size = root < 0 ? 1 : groupSize[root];

            if (size > MaximumGroupSize)
            {
                // The color count stands for the pass of the joints without a color.
                passOf[j] = (byte)(1 + TakeColor(bodyColors, endpoints[j]));
                units[passOf[j]]++;
                continue;
            }

            int unit = root < 0 ? -1 : groupUnit[root];
            if (unit < 0)
            {
                unit = units[GroupPass]++;
                unitStart[unit] = size;
                if (root >= 0) groupUnit[root] = unit;
            }

            passOf[j] = GroupPass;
            unitOf[j] = unit;
        }

        // Sizes to offsets: the groups first, then one unit for each remaining joint.
        int groups = units[GroupPass], offset = 0;
        for (int u = 0; u < groups; u++)
        {
            int size = unitStart[u];
            unitStart[u] = scratch[u] = offset;
            offset += size;
        }

        for (int u = groups; u <= count; u++) unitStart[u] = offset++;

        passStart[0] = 0;
        for (int p = 0; p < PassCount; p++)
        {
            passStart[p + 1] = passStart[p] + units[p];
            units[p] = passStart[p];
        }

        // Fill the units in the order of the constraint ids.
        for (int k = 0; k < count; k++)
        {
            int j = order[k];
            int slot = passOf[j] == GroupPass ? scratch[unitOf[j]]++ : unitStart[units[passOf[j]]++];
            joints[slot] = j < constraintCount ? j : ~(j - constraintCount);
        }
    }

    protected override void ReadEdges(int start, int end)
    {
        var large = constraints.Active;
        var small = smallConstraints.Active;
        for (int j = start; j < end; j++)
        {
            bool enabled;
            JHandle<RigidBodyData> body1, body2;
            if (j < large.Length)
            {
                ref ConstraintData joint = ref large[j];
                (enabled, body1, body2, key[j]) = (joint.IsEnabled, joint.Body1, joint.Body2, joint.ConstraintId);
            }
            else
            {
                ref SmallConstraintData joint = ref small[j - large.Length];
                (enabled, body1, body2, key[j]) = (joint.IsEnabled, joint.Body1, joint.Body2, joint.ConstraintId);
            }

            endpoints[j] = enabled ? Endpoints.Of(ref body1.Data, ref body2.Data) : Endpoints.None;
        }
    }

    // Connects the dynamic bodies of every joint and counts the joints of each group. Afterwards
    // bodyGroup points every body at the root of its group and groupSize holds the joint count
    // of each root.
    private void FindGroups(int count, int bodyLimit)
    {
        if (bodyGroup.Length < bodyLimit)
        {
            int size = Math.Max(bodyLimit, bodyGroup.Length * 2);
            bodyColors = new ulong[size];
            bodyGroup = new int[size];
            groupSize = new int[size];
            groupUnit = new int[size];
        }

        for (int b = 0; b < bodyLimit; b++) bodyGroup[b] = b;
        for (int k = 0; k < count; k++)
        {
            Endpoints edge = endpoints[order[k]];
            if (edge.Body1 < 0 || edge.Body2 < 0) continue;

            // The lower index becomes the root, whatever the order of the joints.
            int root1 = FindRoot(edge.Body1), root2 = FindRoot(edge.Body2);
            if (root1 < root2) bodyGroup[root2] = root1;
            else bodyGroup[root1] = root2;
        }

        for (int b = 0; b < bodyLimit; b++) bodyGroup[b] = FindRoot(b);

        Array.Clear(groupSize, 0, bodyLimit);
        for (int k = 0; k < count; k++)
        {
            int root = GroupOf(endpoints[order[k]]);
            if (root >= 0) groupSize[root]++;
        }
    }

    private int FindRoot(int body)
    {
        while (bodyGroup[body] != body)
        {
            bodyGroup[body] = bodyGroup[bodyGroup[body]];
            body = bodyGroup[body];
        }

        return body;
    }

    private int GroupOf(Endpoints edge)
    {
        int body = edge.Body1 >= 0 ? edge.Body1 : edge.Body2;
        return body >= 0 ? bodyGroup[body] : -1;
    }
}
