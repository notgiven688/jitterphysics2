/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.Parallelization;
using Jitter2.Unmanaged;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace Jitter2;

// The graph-colored solver. Contacts and joints are the edges of a graph whose nodes are the
// dynamic bodies. The edges of one color share no dynamic body, so they can be solved at the
// same time without locks; contacts are solved eight at once as the lanes of a SIMD bundle.
//
// The result does not depend on the number of threads or on their timing. Colors are assigned
// in an order that depends only on which contacts and joints exist, every color is solved in
// its own pass, and the edges of a pass cannot influence each other. Which contacts share a
// bundle and which worker solves which part of a pass therefore makes no difference.
//
// Nothing is kept between solves: every solve colors from scratch, moves the contact records
// that are out of place so that each color is one run of the contact buffer, and gathers the
// bundles from those runs. The rest of the engine may add, remove and move records as it likes
// in between; the next solve puts them back in order.
public sealed partial class World
{
    // A contact or joint that finds none of these colors free is solved after the colored ones,
    // one at a time and in order.
    private const int MaximumContactColors = 64;

    // A color pass ends with a barrier whose cost grows with the worker count while the work per
    // worker shrinks, so the contact count at which a color makes good use of the workers grows
    // with the square of their number.
    private const int WideColorBarrierFactor = 2;

    // Take the most workers that leave all but this fraction of the contacts in colors large
    // enough for them. Adding workers wakes parked pool threads, so the previous choice is kept
    // while it stays within the looser bound and is raised only once more workers were justified
    // for a number of solves in a row; contact churn would otherwise change the count every few
    // steps.
    private const int SmallColorFractionShift = 4, SmallColorFractionShiftKeep = 3;
    private const int WorkerRaiseDelay = 32;

    // Joints gain from every worker. Their count sets a floor under the choice made for the contacts.
    private const int JointsPerWorker = 512;

    // A color pass costs a barrier. Neighboring colors with fewer contacts than this are solved
    // one after another by a single worker in one pass instead. The colors are still solved in
    // their order, so the result is the same as with a pass for each.
    private const int SmallColorLength = 64;

    private const int MinimumBundlesPerWorker = 2;
    private const int JointsPerWorkerInPass = 64;
    private const int BodiesPerWorker = 256;

    private const int ParallelColoringThreshold = 4096;
    private const int InsertionSortLimit = 24;
    private const int RadixBits = 11;

    // The dynamic bodies of an edge by RigidBodyData._index, -1 for a body the solver only reads.
    private struct ContactEndpoints
    {
        internal int Body1, Body2;

        // An empty manifold is left out of the solve.
        internal const int Unsolved = int.MinValue;
    }

    // Per-solve scratch, indexed by the position of a contact in the active contact span.
    private ContactEndpoints[] contactEndpoints = [];
    private ulong[] contactKey1 = [], contactKey2 = [];
    private int[] contactOrder = [], contactTarget = [];
    private byte[] contactSegment = [];
    // Per-solve scratch, indexed by RigidBodyData._index.
    private ulong[] bodyColors = [];

    // The active contact records are stored in segments: one for each color, then the contacts
    // that found no color, then the empty manifolds, which are left out of the solve. Segment s
    // is [segmentStart[s], segmentStart[s + 1]).
    private const int OverflowSegment = MaximumContactColors;
    private const int UnsolvedSegment = MaximumContactColors + 1;
    private const int SegmentCount = MaximumContactColors + 2;

    private readonly int[] segmentStart = new int[SegmentCount + 1];
    private readonly int[] colorLength = new int[MaximumContactColors];
    private readonly int[] colorBundleStart = new int[MaximumContactColors];
    private int contactColorCount, coloredContactCount, solvedContactCount;
    private readonly List<ContactSolverWide> contactBundles = [];
    // The contacts without a color, in the order in which they are solved.
    private readonly List<JHandle<ContactData>> overflowContacts = [];

    // The segments are valid for this state of the buffers.
    private int coloredVersion, coloredCount = -1, coloredJointVersion, coloredSmallJointVersion;

    // Within a segment the records are kept in the order in which they were colored, so that a
    // worker's share of every color covers the same bodies. Moving single records into their
    // segment disturbs that order; it is restored once this fraction of the records has moved,
    // but not more often than every few solves: a scene whose contacts come and go all the time
    // would otherwise reorder its whole buffer on every other step.
    private const int ResortMovedFraction = 8;
    private const int ResortInterval = 16;
    private int contactsMovedSinceSort = int.MaxValue, solvesSinceSort = ResortInterval;

    // Releases the scratch memory of the solver. The next solve allocates what it needs.
    private void TrimSolverScratch()
    {
        contactEndpoints = [];
        contactKey1 = contactKey2 = bodyColors = jointKey = jointBodyColors = knownJointKey = [];
        contactOrder = contactTarget = [];
        contactSegment = jointColor = [];
        jointBody1 = jointBody2 = jointOrder = jointScratch = groupedJoints = coloredJoints = [];
        jointBodyGroup = jointGroupSize = knownJointBody1 = knownJointBody2 = [];
        jointGroupStart = new int[1];
        jointGroupCount = jointColorCount = solvedJointCount = 0;
        Array.Clear(jointColorStart);
        knownJointCount = -1;
        coloredCount = -1;
        contactsMovedSinceSort = int.MaxValue;
        contactBundles.Clear();
        contactBundles.TrimExcess();
        overflowContacts.Clear();
        overflowContacts.TrimExcess();
        overflowJoints.Clear();
        overflowJoints.TrimExcess();
    }

    // Sorts indices by their key. The sort is stable and its cost does not depend on the order
    // the indices come in, only on the spread of the keys.
    private static void SortByKey(ref int[] order, ref int[] scratch, int count, ulong[] keys)
    {
        if (count < 2) return;

        ulong min = ulong.MaxValue, max = 0;
        for (int k = 0; k < count; k++)
        {
            ulong key = keys[order[k]];
            min = Math.Min(min, key);
            max = Math.Max(max, key);
        }

        ulong range = max - min;
        Span<int> buckets = stackalloc int[1 << RadixBits];
        for (int shift = 0; shift < 64 && range >> shift != 0; shift += RadixBits)
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

    // Assigns every active contact the first color that none of its dynamic bodies uses yet.
    // The contacts are visited in the order of their arbiter keys. That order depends only on
    // which contacts exist, not on where their records are or on which thread created them, so
    // the colors are the same on every run, and a contact keeps its color from step to step
    // unless its neighborhood changes.
    private void ColorContacts(bool multiThread)
    {
        int count = memContacts.Active.Length;

        if (contactSegment.Length < count)
        {
            int capacity = Math.Max(count, contactSegment.Length * 2);
            contactEndpoints = new ContactEndpoints[capacity];
            contactKey1 = new ulong[capacity];
            contactKey2 = new ulong[capacity];
            contactOrder = new int[capacity];
            contactTarget = new int[capacity];
            contactSegment = new byte[capacity];
        }

        // Reading the endpoints touches every contact record and its bodies, which is most of
        // the cost of coloring. The workers of the last solve share it; the rest works on the
        // compact arrays filled here.
        int readers = multiThread && count >= ParallelColoringThreshold
            ? Math.Min(solverWorkerCount, ThreadPool.Instance.ThreadCount) : 1;
        if (readers > 1)
        {
            endpointReaders = readers;
            readContactEndpoints ??= worker =>
            {
                Parallel.GetBounds(memContacts.Active.Length, endpointReaders, worker, out int start, out int end);
                ReadContactEndpoints(start, end);
            };
            ThreadPool.Instance.ExecuteWorkerLoop(readContactEndpoints, readers);
        }
        else
        {
            ReadContactEndpoints(0, count);
        }

        var endpoints = contactEndpoints.AsSpan(0, count);
        int bodyLimit = 0, solved = 0;
        for (int i = 0; i < count; i++)
        {
            ref readonly ContactEndpoints e = ref endpoints[i];
            if (e.Body1 == ContactEndpoints.Unsolved)
            {
                contactSegment[i] = UnsolvedSegment;
                continue;
            }

            bodyLimit = Math.Max(bodyLimit, Math.Max(e.Body1, e.Body2) + 1);
            contactOrder[solved++] = i;
        }

        // Engine arbiters put the lower shape id first, so the first key gathers the contacts of
        // one shape; the second key orders them.
        SortByKey(ref contactOrder, ref contactTarget, solved, contactKey1);
        for (int start = 0; start < solved;)
        {
            ulong key = contactKey1[contactOrder[start]];
            int end = start + 1;
            while (end < solved && contactKey1[contactOrder[end]] == key) end++;
            SortRun(contactOrder.AsSpan(start, end - start), contactKey2);
            start = end;
        }

        if (bodyColors.Length < bodyLimit) bodyColors = new ulong[Math.Max(bodyLimit, bodyColors.Length * 2)];
        Array.Clear(bodyColors, 0, bodyLimit);
        Array.Clear(colorLength);
        overflowContacts.Clear();
        contactColorCount = coloredContactCount = 0;
        solvedContactCount = solved;

        var active = memContacts.Active;
        for (int k = 0; k < solved; k++)
        {
            int i = contactOrder[k];
            int color = TakeColor(bodyColors, endpoints[i].Body1, endpoints[i].Body2);
            if (color < MaximumContactColors)
            {
                colorLength[color]++;
                coloredContactCount++;
                contactColorCount = Math.Max(contactColorCount, color + 1);
            }
            else
            {
                overflowContacts.Add(memContacts.GetHandle(ref active[i]));
            }

            // Sixty-four stands for the overflow segment.
            contactSegment[i] = (byte)color;
        }

        int offset = 0;
        for (int c = 0; c < MaximumContactColors; c++)
        {
            segmentStart[c] = offset;
            offset += colorLength[c];
        }

        segmentStart[OverflowSegment] = offset;
        segmentStart[UnsolvedSegment] = solved;
        segmentStart[SegmentCount] = count;

        int bundles = 0;
        for (int c = 0; c < contactColorCount; c++)
        {
            colorBundleStart[c] = bundles;
            bundles += (colorLength[c] + ContactSolverWide.Width - 1) / ContactSolverWide.Width;
        }

        while (contactBundles.Count < bundles) contactBundles.Add(new ContactSolverWide());
    }

    // Returns the first color free on both bodies and takes it, or the color count if none is.
    private static int TakeColor(ulong[] colors, int body1, int body2)
    {
        ulong occupied = (body1 >= 0 ? colors[body1] : 0) | (body2 >= 0 ? colors[body2] : 0);
        int color = BitOperations.TrailingZeroCount(~occupied);
        if (color < MaximumContactColors)
        {
            if (body1 >= 0) colors[body1] |= 1UL << color;
            if (body2 >= 0) colors[body2] |= 1UL << color;
        }

        return color;
    }

    // Orders a short run of indices by key. A shape has a handful of contacts; only a large
    // static shape collects a long run.
    private static void SortRun(Span<int> run, ulong[] keys)
    {
        if (run.Length > InsertionSortLimit)
        {
            run.Sort(new KeyOrder(keys));
            return;
        }

        for (int k = 1; k < run.Length; k++)
        {
            int moving = run[k], slot = k;
            ulong key = keys[moving];
            for (; slot > 0 && keys[run[slot - 1]] > key; slot--) run[slot] = run[slot - 1];
            run[slot] = moving;
        }
    }

    private readonly struct KeyOrder(ulong[] keys) : IComparer<int>
    {
        public int Compare(int x, int y) => keys[x].CompareTo(keys[y]);
    }

    // Moves the records into their segments. Usually only a few are out of place: those that
    // changed segment, and the ones a shifted segment boundary ran over. Then a record takes any
    // free slot of its segment. When too many have moved since the order within the segments
    // was last restored, every record is put in its exact place instead.
    private void GroupContactsBySegment()
    {
        int count = memContacts.Active.Length, solved = solvedContactCount;

        Span<int> next = stackalloc int[SegmentCount];
        segmentStart.AsSpan(0, SegmentCount).CopyTo(next);

        if (++solvesSinceSort >= ResortInterval && contactsMovedSinceSort >= count / ResortMovedFraction)
        {
            // The exact slot of a record is its rank among the records of its segment.
            for (int k = 0; k < solved; k++)
            {
                int i = contactOrder[k];
                contactTarget[i] = next[contactSegment[i]]++;
            }

            for (int i = 0; i < count; i++)
            {
                if (contactSegment[i] == UnsolvedSegment) contactTarget[i] = next[UnsolvedSegment]++;
            }

            for (int i = 0; i < count; i++)
            {
                while (contactTarget[i] != i)
                {
                    int j = contactTarget[i];
                    memContacts.Swap(i, j);
                    (contactTarget[i], contactTarget[j]) = (contactTarget[j], contactTarget[i]);
                }
            }

            contactsMovedSinceSort = solvesSinceSort = 0;
            StampColors();
            return;
        }

        for (int s = 0; s < SegmentCount; s++)
        {
            int end = segmentStart[s + 1];
            for (int i = next[s]; i < end;)
            {
                int segment = contactSegment[i];
                if (segment == s)
                {
                    i++;
                    continue;
                }

                // Hand the record to the first slot of its own segment that holds a stranger.
                int j = next[segment];
                while (contactSegment[j] == segment) j++;
                next[segment] = j + 1;
                memContacts.Swap(i, j);
                (contactSegment[i], contactSegment[j]) = (contactSegment[j], contactSegment[i]);
                contactsMovedSinceSort++;
            }
        }

        StampColors();
    }

    // A body is a node of the graph only if the solver writes it. Static and kinematic bodies
    // are read by any number of edges at once and are recorded as -1.
    private void ReadContactEndpoints(int start, int end)
    {
        var active = memContacts.Active;
        for (int i = start; i < end; i++)
        {
            ref ContactData contact = ref active[i];
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0)
            {
                contactEndpoints[i].Body1 = contactEndpoints[i].Body2 = ContactEndpoints.Unsolved;
                continue;
            }

            contactKey1[i] = contact.Key.Key1;
            contactKey2[i] = contact.Key.Key2;
            ref RigidBodyData b1 = ref contact.Body1.Data;
            ref RigidBodyData b2 = ref contact.Body2.Data;
            contactEndpoints[i].Body1 = b1.MotionType == MotionType.Dynamic ? b1._index : -1;
            contactEndpoints[i].Body2 = b2.MotionType == MotionType.Dynamic ? b2._index : -1;
        }
    }

    private int endpointReaders;
    private Action<int>? readContactEndpoints;

    private int SegmentLength(int segment) => segmentStart[segment + 1] - segmentStart[segment];

    // ---- Joints ---------------------------------------------------------------------------------
    // Joints that are connected through dynamic bodies form a group: a ragdoll, a vehicle, a
    // chain. Groups share no dynamic body, so each can be solved on its own, joint after joint
    // in the order of the constraint ids, while other workers solve other groups. One pass
    // covers all of them. A group too large for that, such as a long rope or a net, is colored
    // like the contacts instead and solved color by color. The joint records stay where they
    // are; groups and colors are lists of references.
    private const int MaximumJointGroupSize = 256;

    // A joint is referenced by its position in the active constraints, or by the complement of
    // its position in the active small constraints. The scratch arrays are indexed by that
    // position, the small constraints following the others.
    private int[] jointBody1 = [], jointBody2 = [], jointOrder = [], jointScratch = [];
    private int[] groupedJoints = [], coloredJoints = [];
    private ulong[] jointKey = [], jointBodyColors = [];
    private byte[] jointColor = [];
    // Per-solve scratch, indexed by RigidBodyData._index: the union-find parent of a body, then
    // the size of the group rooted at it, then the slot of that group.
    private int[] jointBodyGroup = [], jointGroupSize = [];
    // Group g holds groupedJoints[jointGroupStart[g] .. jointGroupStart[g + 1]).
    private int[] jointGroupStart = new int[1];
    private int jointGroupCount;
    private readonly int[] jointColorStart = new int[MaximumContactColors + 1];
    private int jointColorCount, solvedJointCount;
    private readonly List<int> overflowJoints = [];

    private const byte GroupedJoint = byte.MaxValue;

    // The joints the current groups and colors were built from.
    private int[] knownJointBody1 = [], knownJointBody2 = [];
    private ulong[] knownJointKey = [];
    private int knownJointCount = -1;

    private void ColorJoints(bool multiThread)
    {
        int constraintCount = memConstraints.Active.Length;
        int total = constraintCount + memSmallConstraints.Active.Length;
        if (jointBody1.Length < total)
        {
            int capacity = Math.Max(total, jointBody1.Length * 2);
            jointBody1 = new int[capacity];
            jointBody2 = new int[capacity];
            jointOrder = new int[capacity];
            jointScratch = new int[capacity];
            groupedJoints = new int[capacity];
            coloredJoints = new int[capacity];
            jointGroupStart = new int[capacity + 1];
            jointKey = new ulong[capacity];
            jointColor = new byte[capacity];
        }

        // As for the contacts, reading the joint records and their bodies is most of the work.
        int readers = multiThread && total >= ParallelColoringThreshold
            ? Math.Min(solverWorkerCount, ThreadPool.Instance.ThreadCount) : 1;
        if (readers > 1)
        {
            endpointReaders = readers;
            readJointEndpoints ??= worker =>
            {
                int joints = memConstraints.Active.Length + memSmallConstraints.Active.Length;
                Parallel.GetBounds(joints, endpointReaders, worker, out int start, out int end);
                ReadJointEndpoints(start, end);
            };
            ThreadPool.Instance.ExecuteWorkerLoop(readJointEndpoints, readers);
        }
        else
        {
            ReadJointEndpoints(0, total);
        }

        // Joints rarely change. When every joint still connects the same bodies as in the last
        // solve, its groups and colors still hold.
        if (total == knownJointCount &&
            jointBody1.AsSpan(0, total).SequenceEqual(knownJointBody1.AsSpan(0, total)) &&
            jointBody2.AsSpan(0, total).SequenceEqual(knownJointBody2.AsSpan(0, total)) &&
            jointKey.AsSpan(0, total).SequenceEqual(knownJointKey.AsSpan(0, total)))
        {
            return;
        }

        if (knownJointKey.Length < total)
        {
            knownJointBody1 = new int[jointBody1.Length];
            knownJointBody2 = new int[jointBody1.Length];
            knownJointKey = new ulong[jointBody1.Length];
        }

        jointBody1.AsSpan(0, total).CopyTo(knownJointBody1);
        jointBody2.AsSpan(0, total).CopyTo(knownJointBody2);
        jointKey.AsSpan(0, total).CopyTo(knownJointKey);
        knownJointCount = total;

        int count = 0, bodyLimit = 0;
        for (int j = 0; j < total; j++)
        {
            if (jointBody1[j] == ContactEndpoints.Unsolved) continue;
            bodyLimit = Math.Max(bodyLimit, Math.Max(jointBody1[j], jointBody2[j]) + 1);
            jointOrder[count++] = j;
        }

        solvedJointCount = count;
        SortJoints(count);

        if (jointBodyColors.Length < bodyLimit)
        {
            int size = Math.Max(bodyLimit, jointBodyColors.Length * 2);
            jointBodyColors = new ulong[size];
            jointBodyGroup = new int[size];
            jointGroupSize = new int[size];
        }

        // Connect the dynamic bodies of every joint and count the joints of each group.
        for (int b = 0; b < bodyLimit; b++) jointBodyGroup[b] = b;
        for (int k = 0; k < count; k++)
        {
            int j = jointOrder[k];
            if (jointBody1[j] >= 0 && jointBody2[j] >= 0) JoinJointGroups(jointBody1[j], jointBody2[j]);
        }

        // Point every body straight at its root, so that the passes below need no search.
        for (int b = 0; b < bodyLimit; b++) jointBodyGroup[b] = FindJointGroup(b);

        Array.Clear(jointGroupSize, 0, bodyLimit);
        for (int k = 0; k < count; k++)
        {
            int root = JointGroupOf(jointOrder[k]);
            if (root >= 0) jointGroupSize[root]++;
        }

        // Number the small groups in the order in which the sorted joints reach them. A joint
        // between two bodies that the solver only reads is a group of its own.
        Array.Clear(jointBodyColors, 0, bodyLimit);
        overflowJoints.Clear();
        jointGroupCount = jointColorCount = 0;
        Span<int> colorSize = stackalloc int[MaximumContactColors];
        colorSize.Clear();

        // Sizes become slots: a negative entry -(slot + 1) marks a numbered group.
        for (int k = 0; k < count; k++)
        {
            int j = jointOrder[k];
            int root = JointGroupOf(j);
            if (root < 0)
            {
                jointColor[j] = GroupedJoint;
                jointScratch[j] = jointGroupCount;
                jointGroupStart[jointGroupCount++] = 1;
                continue;
            }

            int size = jointGroupSize[root];
            if (size > MaximumJointGroupSize)
            {
                int color = TakeColor(jointBodyColors, jointBody1[j], jointBody2[j]);
                jointColor[j] = (byte)color;
                if (color < MaximumContactColors)
                {
                    colorSize[color]++;
                    jointColorCount = Math.Max(jointColorCount, color + 1);
                }
                else
                {
                    overflowJoints.Add(JointReference(j, constraintCount));
                }

                continue;
            }

            if (size > 0)
            {
                jointGroupStart[jointGroupCount] = size;
                jointGroupSize[root] = -(jointGroupCount++ + 1);
            }

            jointColor[j] = GroupedJoint;
            jointScratch[j] = -jointGroupSize[root] - 1;
        }

        // Sizes to offsets.
        int offset = 0;
        for (int g = 0; g < jointGroupCount; g++)
        {
            int size = jointGroupStart[g];
            jointGroupStart[g] = offset;
            offset += size;
        }

        jointGroupStart[jointGroupCount] = offset;

        offset = 0;
        for (int c = 0; c < MaximumContactColors; c++)
        {
            jointColorStart[c] = offset;
            offset += colorSize[c];
            colorSize[c] = jointColorStart[c];
        }

        jointColorStart[MaximumContactColors] = offset;

        // Fill the groups and colors in the order of the constraint ids. The start of a group
        // serves as its cursor and is restored afterwards.
        for (int k = 0; k < count; k++)
        {
            int j = jointOrder[k];
            if (jointColor[j] == GroupedJoint) groupedJoints[jointGroupStart[jointScratch[j]]++] = JointReference(j, constraintCount);
            else if (jointColor[j] < MaximumContactColors) coloredJoints[colorSize[jointColor[j]]++] = JointReference(j, constraintCount);
        }

        for (int g = jointGroupCount; g > 0; g--) jointGroupStart[g] = jointGroupStart[g - 1];
        jointGroupStart[0] = 0;
    }

    // Constraint ids grow as joints are created, and each of the two buffers mostly keeps that
    // order, so the joints usually arrive as two sorted runs that only need merging.
    private void SortJoints(int count)
    {
        int breaks = 0, split = count;
        for (int k = 1; k < count; k++)
        {
            if (jointKey[jointOrder[k - 1]] <= jointKey[jointOrder[k]]) continue;
            if (++breaks > 1) break;
            split = k;
        }

        if (breaks == 0) return;
        if (breaks > 1)
        {
            SortByKey(ref jointOrder, ref jointScratch, count, jointKey);
            return;
        }

        int a = 0, b = split;
        for (int k = 0; k < count; k++)
        {
            bool first = b >= count || (a < split && jointKey[jointOrder[a]] <= jointKey[jointOrder[b]]);
            jointScratch[k] = first ? jointOrder[a++] : jointOrder[b++];
        }

        (jointOrder, jointScratch) = (jointScratch, jointOrder);
    }

    private int JointGroupOf(int joint)
    {
        int body = jointBody1[joint] >= 0 ? jointBody1[joint] : jointBody2[joint];
        return body >= 0 ? jointBodyGroup[body] : -1;
    }

    private int FindJointGroup(int body)
    {
        while (jointBodyGroup[body] != body)
        {
            jointBodyGroup[body] = jointBodyGroup[jointBodyGroup[body]];
            body = jointBodyGroup[body];
        }

        return body;
    }

    private void JoinJointGroups(int body1, int body2)
    {
        int root1 = FindJointGroup(body1), root2 = FindJointGroup(body2);
        // The lower index becomes the root, whatever the order of the joints.
        if (root1 < root2) jointBodyGroup[root2] = root1;
        else jointBodyGroup[root1] = root2;
    }

    private static int JointReference(int joint, int constraintCount) =>
        joint < constraintCount ? joint : ~(joint - constraintCount);

    // Disabled joints are left out; the bodies of the others are recorded like those of contacts.
    private void ReadJointEndpoints(int start, int end)
    {
        var constraints = memConstraints.Active;
        var small = memSmallConstraints.Active;
        for (int j = start; j < end; j++)
        {
            bool enabled;
            JHandle<RigidBodyData> body1, body2;
            if (j < constraints.Length)
            {
                ref ConstraintData joint = ref constraints[j];
                (enabled, body1, body2, jointKey[j]) = (joint.IsEnabled, joint.Body1, joint.Body2, joint.ConstraintId);
            }
            else
            {
                ref SmallConstraintData joint = ref small[j - constraints.Length];
                (enabled, body1, body2, jointKey[j]) = (joint.IsEnabled, joint.Body1, joint.Body2, joint.ConstraintId);
            }

            if (!enabled)
            {
                jointBody1[j] = jointBody2[j] = ContactEndpoints.Unsolved;
                continue;
            }

            ref RigidBodyData b1 = ref body1.Data;
            ref RigidBodyData b2 = ref body2.Data;
            jointBody1[j] = b1.MotionType == MotionType.Dynamic ? b1._index : -1;
            jointBody2[j] = b2.MotionType == MotionType.Dynamic ? b2._index : -1;
        }
    }

    private Action<int>? readJointEndpoints;

    private void SolveJoint(int reference, bool prepare)
    {
        if (reference >= 0)
        {
            ref ConstraintData joint = ref memConstraints.Active[reference];
            if (prepare) joint.PrepareForIteration(ref joint, timeStep);
            else joint.Iterate(ref joint, timeStep);
        }
        else
        {
            ref SmallConstraintData joint = ref memSmallConstraints.Active[~reference];
            if (prepare) joint.PrepareForIteration(ref joint, timeStep);
            else joint.Iterate(ref joint, timeStep);
        }
    }

    private void SolveOverflowContacts(bool prepare, bool applyBias)
    {
        foreach (var handle in overflowContacts)
        {
            ref ContactData contact = ref handle.Data;
            if (prepare) contact.PrepareForIteration(timeStep);
            else contact.Iterate(applyBias);
        }
    }

    // ---- Workers --------------------------------------------------------------------------------

    // The number of workers changes how fast a solve is, never its result.
    private int ChooseSolverWorkerLimit(int threadCount)
    {
        int colored = coloredContactCount;
        int previous = solverWorkerLimit > 0 ? Math.Min(solverWorkerLimit, threadCount) : threadCount;
        int workers = previous;

        if (ContactsInSmallColors(workers) <= colored >> SmallColorFractionShiftKeep)
        {
            while (workers < threadCount && ContactsInSmallColors(workers + 1) <= colored >> SmallColorFractionShift) workers++;
        }
        else
        {
            while (workers > 1 && ContactsInSmallColors(workers) > colored >> SmallColorFractionShift) workers--;
        }

        if (workers > previous && solverWorkerLimit != 0 && ++solverWorkerRaiseStreak < WorkerRaiseDelay) workers = previous;
        else solverWorkerRaiseStreak = 0;

        return Math.Max(workers, Math.Min(threadCount, solvedJointCount / JointsPerWorker));
    }

    // Contacts of colors too small to keep the given number of workers busy.
    private int ContactsInSmallColors(int workers)
    {
        int threshold = WideColorBarrierFactor * workers * workers, small = 0;
        for (int c = 0; c < contactColorCount; c++)
        {
            if (colorLength[c] < threshold) small += colorLength[c];
        }

        return small;
    }

    // ---- Solver program ---------------------------------------------------------------------
    // One worker loop per substep runs the whole solve on the participating pool threads: force
    // integration, preparation, the biased sweeps, velocity integration and the relaxation
    // sweeps. Passes that must not overlap are separated by a spin barrier, which costs far less
    // than dispatching each pass through the task queues. Every worker owns the same share of
    // each pass in every sweep.

    private enum SolverPassKind : byte
    {
        IntegrateForces,
        PrepareContacts,
        IterateContacts,
        WriteBackContacts,
        PrepareOverflowContacts,
        IterateOverflowContacts,
        PrepareJointGroups,
        IterateJointGroups,
        PrepareJoints,
        IterateJoints,
        IntegrateVelocities,
        UpdateRemainingContacts
    }

    private struct SolverPass
    {
        internal SolverPassKind Kind;
        // The color of the pass, or the color count for the joints without a color. A serial
        // contact pass covers the colors up to ColorEnd.
        internal int Color, ColorEnd, Total;
        // A serial pass is run by the first worker alone, in order.
        internal bool ApplyBias, WriteBack, UpdateContacts, Serial;
    }

    private readonly List<SolverPass> solverPasses = [];
    // The most workers the current plan may use, and the count it does use.
    private int solverWorkerLimit;
    private int solverWorkerCount;
    private int solverWorkerRaiseStreak;
    private int solverArrivals;
    private int solverEpoch;
    private ExceptionDispatchInfo? solverFailure;
    private Action<int>? solverWorkerEntry;

    /// <summary>
    /// Solves contacts and joints for one substep. With <paramref name="integrate"/> set, forces
    /// are integrated before and velocities after the biased sweeps.
    /// </summary>
    /// <param name="multiThread">Whether to use the thread pool.</param>
    /// <param name="iterations">The number of biased sweeps.</param>
    /// <param name="relaxations">The number of relaxation sweeps.</param>
    /// <param name="integrate">Whether this is a full substep rather than a velocity-only solve.</param>
    /// <param name="recolor">Whether contacts or joints may have changed since the previous solve.</param>
    /// <param name="updateContacts">
    /// Whether to finish with the contact update of the step, see <see cref="RemoveBrokenArbiters"/>.
    /// </param>
    private void SolveColored(bool multiThread, int iterations, int relaxations, bool integrate,
        bool recolor = true, bool updateContacts = false)
    {
        // The colors of the previous substep are reused only when the caller knows that nothing
        // could have added, removed or edited a contact or joint since.
        if (recolor || !ColorsAreCurrent)
        {
            ColorContacts(multiThread);
            ColorJoints(multiThread);
            GroupContactsBySegment();
        }

        int workers = 1;
        if (multiThread)
        {
            solverWorkerLimit = ChooseSolverWorkerLimit(ThreadPool.Instance.ThreadCount);

            // Let the largest pass decide how many workers are worth synchronizing.
            int demand = 0;
            for (int c = 0; c < contactColorCount; c++)
                demand = Math.Max(demand, colorLength[c] / (ContactSolverWide.Width * MinimumBundlesPerWorker));
            demand = Math.Max(demand, jointGroupStart[jointGroupCount] / JointsPerWorkerInPass);
            for (int c = 0; c < jointColorCount; c++)
                demand = Math.Max(demand, (jointColorStart[c + 1] - jointColorStart[c]) / JointsPerWorkerInPass);
            if (integrate) demand = Math.Max(demand, memRigidBodies.Active.Length / BodiesPerWorker);
            workers = Math.Clamp(demand, 1, solverWorkerLimit);
        }

        solverWorkerCount = workers;

        BuildSolverPasses(iterations, relaxations, integrate, updateContacts);
        if (solverPasses.Count == 0) return;
        if (updateContacts) FlagRetiringArbiters();

        solverArrivals = workers;
        solverEpoch = 0;
        solverFailure = null;

        if (workers == 1)
        {
            RunSolverProgram(0);
        }
        else
        {
            solverWorkerEntry ??= SolverWorkerEntry;
            ThreadPool.Instance.ExecuteWorkerLoop(solverWorkerEntry, workers);
        }

        solverFailure?.Throw();
    }

    private bool ColorsAreCurrent =>
        coloredVersion == memContacts.Version && coloredCount == memContacts.Active.Length &&
        coloredJointVersion == memConstraints.Version && coloredSmallJointVersion == memSmallConstraints.Version;

    private void StampColors()
    {
        coloredVersion = memContacts.Version;
        coloredCount = memContacts.Active.Length;
        coloredJointVersion = memConstraints.Version;
        coloredSmallJointVersion = memSmallConstraints.Version;
    }

    // The contact update of the step, run by the solver on the records it has just written while
    // they and their bodies are still in the worker's cache. It runs before the arbiters that
    // broke in the previous step are removed, so those are flagged first: their empty manifolds
    // must not be reported as broken a second time.
    private void FlagRetiringArbiters()
    {
        foreach (var handle in brokenArbiters)
        {
            ref ContactData contact = ref handle.Data;
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0) contact.UsageMask = ContactData.MaskRetiring;
        }
    }

    private void UpdateSolvedContacts(Span<ContactData> records)
    {
        for (int i = 0; i < records.Length; i++)
        {
            ref ContactData contact = ref records[i];
            if (contact.UsageMask == ContactData.MaskRetiring) continue;
            contact.UpdatePosition();

            if ((contact.UsageMask & ContactData.MaskContactAll) == 0)
                pendingBrokenArbiters.ConcurrentAdd(memContacts.GetHandle(ref contact));
        }
    }

    private void BuildSolverPasses(int iterations, int relaxations, bool integrate, bool updateContacts)
    {
        solverPasses.Clear();

        if (integrate) AddPass(SolverPassKind.IntegrateForces, memRigidBodies.Active.Length);

        AddContactPasses(SolverPassKind.PrepareContacts, SolverPassKind.PrepareOverflowContacts);
        AddJointPasses(SolverPassKind.PrepareJoints);

        for (int iter = 0; iter < iterations; iter++)
        {
            AddContactPasses(SolverPassKind.IterateContacts, SolverPassKind.IterateOverflowContacts, applyBias: true);
            AddJointPasses(SolverPassKind.IterateJoints);
        }

        if (integrate) AddPass(SolverPassKind.IntegrateVelocities, memRigidBodies.Active.Length);

        // Relaxation solves the contacts only, on the anchors and masses prepared before the
        // bodies moved. Its last sweep also publishes the impulses while each bundle is in cache.
        for (int iter = 0; iter < relaxations; iter++)
        {
            bool last = iter == relaxations - 1;
            AddContactPasses(SolverPassKind.IterateContacts, SolverPassKind.IterateOverflowContacts,
                writeBack: last, updateContacts: last && updateContacts);
        }

        if (relaxations == 0)
        {
            AddContactPasses(SolverPassKind.WriteBackContacts, SolverPassKind.WriteBackContacts,
                updateContacts: updateContacts, overflow: false);
        }

        if (updateContacts && SegmentLength(OverflowSegment) + SegmentLength(UnsolvedSegment) != 0)
            AddPass(SolverPassKind.UpdateRemainingContacts, solverWorkerCount);
    }

    private void AddContactPasses(SolverPassKind kind, SolverPassKind overflowKind, bool applyBias = false,
        bool writeBack = false, bool updateContacts = false, bool overflow = true)
    {
        for (int c = 0; c < contactColorCount;)
        {
            if (colorLength[c] >= SmallColorLength)
            {
                AddColorPass(kind, c++, applyBias, writeBack, updateContacts);
                continue;
            }

            int first = c;
            while (c < contactColorCount && colorLength[c] < SmallColorLength) c++;
            solverPasses.Add(new SolverPass
            {
                Kind = kind, Color = first, ColorEnd = c, Total = 1, ApplyBias = applyBias, WriteBack = writeBack,
                UpdateContacts = updateContacts, Serial = true
            });
        }

        if (overflow && overflowContacts.Count != 0)
        {
            solverPasses.Add(new SolverPass
            {
                Kind = overflowKind, Total = overflowContacts.Count, ApplyBias = applyBias, Serial = true
            });
        }
    }

    private void AddJointPasses(SolverPassKind kind)
    {
        if (jointGroupCount != 0)
        {
            solverPasses.Add(new SolverPass
            {
                Kind = kind == SolverPassKind.PrepareJoints ? SolverPassKind.PrepareJointGroups : SolverPassKind.IterateJointGroups,
                Total = jointGroupCount
            });
        }

        for (int c = 0; c < jointColorCount; c++)
        {
            int length = jointColorStart[c + 1] - jointColorStart[c];
            if (length != 0) solverPasses.Add(new SolverPass { Kind = kind, Color = c, Total = length });
        }

        if (overflowJoints.Count != 0)
        {
            solverPasses.Add(new SolverPass
            {
                Kind = kind, Color = MaximumContactColors, Total = overflowJoints.Count, Serial = true
            });
        }
    }

    private void AddColorPass(SolverPassKind kind, int color, bool applyBias = false, bool writeBack = false,
        bool updateContacts = false)
    {
        int bundles = (colorLength[color] + ContactSolverWide.Width - 1) / ContactSolverWide.Width;
        solverPasses.Add(new SolverPass
        {
            Kind = kind, Color = color, Total = bundles, ApplyBias = applyBias, WriteBack = writeBack,
            UpdateContacts = updateContacts
        });
    }

    private void AddPass(SolverPassKind kind, int total)
    {
        solverPasses.Add(new SolverPass { Kind = kind, Total = total });
    }

    private void SolverWorkerEntry(int worker)
    {
        try
        {
            RunSolverProgram(worker);
        }
        catch (Exception exception)
        {
            // The others see the failure at their next barrier and leave. The caller rethrows
            // once every worker has returned.
            Interlocked.CompareExchange(ref solverFailure, ExceptionDispatchInfo.Capture(exception), null);
        }
    }

    // Every participating worker runs the same pass list. A single worker passes each barrier at once.
    private void RunSolverProgram(int worker)
    {
        var passes = CollectionsMarshal.AsSpan(solverPasses);
        for (int k = 0; k < passes.Length; k++)
        {
            ref readonly SolverPass pass = ref passes[k];
            int start = 0, end = pass.Total;
            if (pass.Serial) end = worker == 0 ? end : 0;
            else Parallel.GetBounds(pass.Total, solverWorkerCount, worker, out start, out end);

            if (start < end) RunPass(in pass, worker, start, end);
            if (!SolverBarrier()) return;
        }
    }

    private bool SolverBarrier()
    {
        int epoch = Volatile.Read(ref solverEpoch);
        if (Volatile.Read(ref solverFailure) != null) return false;
        if (Interlocked.Decrement(ref solverArrivals) == 0)
        {
            // Last to arrive: rearm, then release the others.
            solverArrivals = solverWorkerCount;
            Volatile.Write(ref solverEpoch, epoch + 1);
            return true;
        }

        var spinner = new SpinWait();
        while (Volatile.Read(ref solverEpoch) == epoch)
        {
            if (Volatile.Read(ref solverFailure) != null) return false;
            // Yield when the machine is oversubscribed, but never sleep for a millisecond.
            spinner.SpinOnce(sleep1Threshold: -1);
        }

        return true;
    }

    private void SolveContactColor(in SolverPass pass, int color, int start, int end)
    {
        var lanes = memContacts.Active.Slice(segmentStart[color], colorLength[color]);
        int first = colorBundleStart[color];
        for (int i = start; i < end; i++)
        {
            ContactSolverWide bundle = contactBundles[first + i];
            int from = i * ContactSolverWide.Width;
            switch (pass.Kind)
            {
                case SolverPassKind.PrepareContacts:
                    bundle.Prepare(lanes.Slice(from, Math.Min(ContactSolverWide.Width, lanes.Length - from)), timeStep);
                    break;
                case SolverPassKind.IterateContacts:
                    bundle.Iterate(pass.ApplyBias, pass.WriteBack);
                    break;
                default:
                    bundle.WriteBack();
                    break;
            }

            if (pass.UpdateContacts) UpdateSolvedContacts(lanes.Slice(from, bundle.Count));
        }
    }

    private void RunPass(in SolverPass pass, int worker, int start, int end)
    {
        switch (pass.Kind)
        {
            case SolverPassKind.IntegrateForces:
                IntegrateForces(new Parallel.Batch(start, end));
                break;
            case SolverPassKind.IntegrateVelocities:
                IntegrateVelocities(new Parallel.Batch(start, end));
                break;
            case SolverPassKind.PrepareContacts:
            case SolverPassKind.IterateContacts:
            case SolverPassKind.WriteBackContacts:
            {
                if (!pass.Serial)
                {
                    SolveContactColor(in pass, pass.Color, start, end);
                    break;
                }

                for (int c = pass.Color; c < pass.ColorEnd; c++)
                    SolveContactColor(in pass, c, 0, (colorLength[c] + ContactSolverWide.Width - 1) / ContactSolverWide.Width);

                break;
            }
            case SolverPassKind.PrepareOverflowContacts:
                SolveOverflowContacts(prepare: true, applyBias: false);
                break;
            case SolverPassKind.IterateOverflowContacts:
                SolveOverflowContacts(prepare: false, pass.ApplyBias);
                break;
            case SolverPassKind.PrepareJointGroups:
            case SolverPassKind.IterateJointGroups:
            {
                bool prepare = pass.Kind == SolverPassKind.PrepareJointGroups;
                for (int i = jointGroupStart[start]; i < jointGroupStart[end]; i++) SolveJoint(groupedJoints[i], prepare);
                break;
            }
            case SolverPassKind.PrepareJoints:
            case SolverPassKind.IterateJoints:
            {
                bool prepare = pass.Kind == SolverPassKind.PrepareJoints;
                ReadOnlySpan<int> joints = pass.Color == MaximumContactColors
                    ? CollectionsMarshal.AsSpan(overflowJoints)
                    : coloredJoints.AsSpan(jointColorStart[pass.Color], pass.Total);
                for (int i = start; i < end; i++) SolveJoint(joints[i], prepare);
                break;
            }
            case SolverPassKind.UpdateRemainingContacts:
            {
                // The overflow and the unsolved segment are adjacent.
                int remaining = SegmentLength(OverflowSegment) + SegmentLength(UnsolvedSegment);
                Parallel.GetBounds(remaining, solverWorkerCount, worker, out int first, out int last);
                UpdateSolvedContacts(memContacts.Active.Slice(segmentStart[OverflowSegment] + first, last - first));
                break;
            }
        }
    }
}
