/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics.Solver;

/// <summary>
/// Colors the active contacts and keeps their records grouped by color.
/// </summary>
/// <remarks>
/// <para>
/// The active contact records are stored in segments: one for each color, then the contacts that
/// found no color, then the empty manifolds, which are left out of the solve. A color is thus
/// one run of the contact buffer, and the solver gathers its SIMD bundles from consecutive
/// records.
/// </para>
/// <para>
/// Nothing is kept between updates. Every update colors from scratch and moves the records that
/// are out of place. The rest of the engine may add, remove and move records as it likes in
/// between; the next update puts them back in order.
/// </para>
/// </remarks>
internal sealed class ContactColoring(PartitionedBuffer<ContactData> contacts) : ConstraintGraph
{
    internal const int OverflowSegment = MaximumColors;
    internal const int UnsolvedSegment = MaximumColors + 1;
    private const int SegmentCount = MaximumColors + 2;

    // Within a segment the records are kept in the order in which they were colored, so that a
    // worker's share of every color covers the same bodies. Moving single records into their
    // segment disturbs that order; it is restored once this fraction of the records has moved,
    // but not more often than every few updates: a scene whose contacts come and go all the time
    // would otherwise reorder its whole buffer on every other step.
    private const int ResortMovedFraction = 8;
    private const int ResortInterval = 16;
    private int movedSinceSort = int.MaxValue, updatesSinceSort = ResortInterval;

    // Scratch, indexed by the position of a contact in the active contact span.
    private Endpoints[] endpoints = [];
    private ulong[] key1 = [], key2 = [];
    private int[] order = [], target = [];
    private byte[] segment = [];
    // Scratch, indexed by RigidBodyData._index.
    private ulong[] bodyColors = [];

    private readonly int[] segmentStart = new int[SegmentCount + 1];
    private readonly List<JHandle<ContactData>> overflow = [];

    /// <summary>The number of colors in use.</summary>
    internal int ColorCount { get; private set; }

    /// <summary>The contacts without a color, in the order in which they have to be solved.</summary>
    internal List<JHandle<ContactData>> Overflow => overflow;

    /// <summary>The position of the first record of a segment in the active contact span.</summary>
    internal int Start(int segment) => segmentStart[segment];

    /// <summary>The number of records in a segment.</summary>
    internal int Length(int segment) => segmentStart[segment + 1] - segmentStart[segment];

    /// <summary>Releases the scratch memory. The next update allocates what it needs.</summary>
    internal void Trim()
    {
        endpoints = [];
        key1 = key2 = bodyColors = [];
        order = target = [];
        segment = [];
        overflow.Clear();
        overflow.TrimExcess();
        movedSinceSort = int.MaxValue;
    }

    /// <summary>
    /// Assigns every active contact the first color that none of its dynamic bodies uses yet and
    /// moves the records into their segments.
    /// </summary>
    /// <param name="workers">The number of pool workers that may share reading the records.</param>
    internal void Update(int workers)
    {
        int count = contacts.Active.Length;
        if (segment.Length < count)
        {
            int capacity = Math.Max(count, segment.Length * 2);
            endpoints = new Endpoints[capacity];
            key1 = new ulong[capacity];
            key2 = new ulong[capacity];
            order = new int[capacity];
            target = new int[capacity];
            segment = new byte[capacity];
        }

        ReadAllEdges(count, workers);

        int bodyLimit = 0, solved = 0;
        for (int i = 0; i < count; i++)
        {
            Endpoints edge = endpoints[i];
            if (!edge.IsSolved)
            {
                segment[i] = UnsolvedSegment;
                continue;
            }

            bodyLimit = Math.Max(bodyLimit, Math.Max(edge.Body1, edge.Body2) + 1);
            order[solved++] = i;
        }

        // The contacts are visited in the order of their arbiter keys. Engine arbiters put the
        // lower shape id first, so the first key gathers the contacts of one shape; the second
        // key orders them. A contact keeps its color from step to step unless its neighborhood
        // changes, and few records have to move.
        SortByKey(ref order, ref target, solved, key1);
        for (int start = 0; start < solved;)
        {
            ulong key = key1[order[start]];
            int end = start + 1;
            while (end < solved && key1[order[end]] == key) end++;
            SortRun(order.AsSpan(start, end - start), key2);
            start = end;
        }

        if (bodyColors.Length < bodyLimit) bodyColors = new ulong[Math.Max(bodyLimit, bodyColors.Length * 2)];
        Array.Clear(bodyColors, 0, bodyLimit);
        overflow.Clear();
        ColorCount = 0;

        Span<int> length = stackalloc int[SegmentCount];
        length.Clear();
        var active = contacts.Active;
        for (int k = 0; k < solved; k++)
        {
            int i = order[k];
            // The color count stands for the overflow segment.
            int color = TakeColor(bodyColors, endpoints[i]);
            if (color < MaximumColors) ColorCount = Math.Max(ColorCount, color + 1);
            else overflow.Add(contacts.GetHandle(ref active[i]));

            segment[i] = (byte)color;
            length[color]++;
        }

        length[UnsolvedSegment] = count - solved;
        int offset = 0;
        for (int s = 0; s < SegmentCount; s++)
        {
            segmentStart[s] = offset;
            offset += length[s];
        }

        segmentStart[SegmentCount] = offset;
        MoveRecordsIntoSegments(count, solved);
    }

    protected override void ReadEdges(int start, int end)
    {
        var active = contacts.Active;
        for (int i = start; i < end; i++)
        {
            ref ContactData contact = ref active[i];
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0)
            {
                endpoints[i] = Endpoints.None;
                continue;
            }

            key1[i] = contact.Key.Key1;
            key2[i] = contact.Key.Key2;
            endpoints[i] = Endpoints.Of(ref contact.Body1.Data, ref contact.Body2.Data);
        }
    }

    // Usually only a few records are out of place: those that changed segment, and the ones a
    // shifted segment boundary ran over. Then a record takes any free slot of its segment. When
    // too many have moved since the order within the segments was last restored, every record is
    // put in its exact place instead.
    private void MoveRecordsIntoSegments(int count, int solved)
    {
        Span<int> next = stackalloc int[SegmentCount];
        segmentStart.AsSpan(0, SegmentCount).CopyTo(next);

        if (++updatesSinceSort >= ResortInterval && movedSinceSort >= count / ResortMovedFraction)
        {
            // The exact slot of a record is its rank among the records of its segment.
            for (int k = 0; k < solved; k++)
            {
                int i = order[k];
                target[i] = next[segment[i]]++;
            }

            for (int i = 0; i < count; i++)
            {
                if (segment[i] == UnsolvedSegment) target[i] = next[UnsolvedSegment]++;
            }

            for (int i = 0; i < count; i++)
            {
                while (target[i] != i)
                {
                    int j = target[i];
                    contacts.Swap(i, j);
                    (target[i], target[j]) = (target[j], target[i]);
                }
            }

            movedSinceSort = updatesSinceSort = 0;
            return;
        }

        for (int s = 0; s < SegmentCount; s++)
        {
            int end = segmentStart[s + 1];
            for (int i = next[s]; i < end;)
            {
                int own = segment[i];
                if (own == s)
                {
                    i++;
                    continue;
                }

                // Hand the record to the first slot of its own segment that holds a stranger.
                int j = next[own];
                while (segment[j] == own) j++;
                next[own] = j + 1;
                contacts.Swap(i, j);
                (segment[i], segment[j]) = (segment[j], segment[i]);
                movedSinceSort++;
            }
        }
    }
}
