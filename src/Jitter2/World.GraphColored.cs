/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Jitter2.Dynamics;
using Jitter2.Parallelization;
using Jitter2.Unmanaged;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace Jitter2;

public sealed partial class World
{
    // Limit synchronization overhead for high-degree graphs. Contacts that cannot be
    // assigned one of these colors are solved sequentially after every colored sweep.
    private const int MaximumContactColors = 64;

    // Group independent manifolds by coarse ranges of physical body slots. The
    // active/inactive body partition and the greedy color assignment stay intact.
    private const int ContactBodyBucketShift = 7;

    private sealed class ContactColor
    {
        internal readonly List<JHandle<ContactData>> Contacts = new();
        internal readonly List<ContactSolverWide> Bundles = new();
        // Keys in insertion order, consumed before the SIMD bundles are prepared.
        internal readonly List<int> BodyBuckets = new();
        internal bool NeedsLocalitySort;
        internal int BundleCount;
    }

    private readonly List<ContactColor> contactColors = new();
    private readonly List<JHandle<ContactData>> contactColorOverflow = new();
    private ulong[] bodyContactColors = Array.Empty<ulong>();
    private bool[] contactBodyWritable = Array.Empty<bool>();
    private int[] contactBucketOffsets = Array.Empty<int>();
    private JHandle<ContactData>[] contactBucketScratch = Array.Empty<JHandle<ContactData>>();
    private int contactColorCount;
    private ContactColor currentContactColor = null!;
    private bool coloredApplyBias;
    private Action<Parallel.Batch> prepareContactColor = null!;
    private Action<Parallel.Batch> iterateContactColor = null!;
    private Action<Parallel.Batch> finishContactColor = null!;

    private void BuildContactColors()
    {
        int bodyCount = memRigidBodies.Count;
        if (bodyContactColors.Length < bodyCount)
        {
            int capacity = Math.Max(bodyCount, bodyContactColors.Length * 2);
            bodyContactColors = new ulong[capacity];
            contactBodyWritable = new bool[capacity];
        }
        else
        {
            Array.Clear(bodyContactColors, 0, bodyCount);
            Array.Clear(contactBodyWritable, 0, bodyCount);
        }

        contactColorCount = 0;
        contactColorOverflow.Clear();
        foreach (ContactColor color in contactColors)
        {
            color.Contacts.Clear();
            color.BodyBuckets.Clear();
            color.NeedsLocalitySort = false;
        }

        var contacts = memContacts.Active;
        int readOnlyBucket = (bodyCount + (1 << ContactBodyBucketShift) - 1) >> ContactBodyBucketShift;

        // Usually these are precisely the dynamic bodies. Looking at every manifold's
        // write mask also covers custom contact modes: readers must not overlap a writer
        // in another lane. Static/kinematic bodies may be shared when all uses are reads.
        foreach (ref ContactData contact in contacts)
        {
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0) continue;
            if ((contact.Mode & ContactData.SolveMode.FullBody1) != 0)
                contactBodyWritable[memRigidBodies.GetIndex(contact.Body1)] = true;
            if ((contact.Mode & ContactData.SolveMode.FullBody2) != 0)
                contactBodyWritable[memRigidBodies.GetIndex(contact.Body2)] = true;
        }

        foreach (ref ContactData contact in contacts)
        {
            if ((contact.UsageMask & ContactData.MaskContactAll) == 0) continue;
            int body1 = memRigidBodies.GetIndex(contact.Body1);
            int body2 = memRigidBodies.GetIndex(contact.Body2);
            bool writable1 = contactBodyWritable[body1], writable2 = contactBodyWritable[body2];
            ulong occupied = (writable1 ? bodyContactColors[body1] : 0) |
                             (writable2 ? bodyContactColors[body2] : 0);
            var handle = memContacts.GetHandle(ref contact);
            int index = BitOperations.TrailingZeroCount(~occupied);
            if (index >= MaximumContactColors)
            {
                contactColorOverflow.Add(handle);
                continue;
            }

            if (index == contactColors.Count) contactColors.Add(new ContactColor());
            ContactColor color = contactColors[index];
            color.Contacts.Add(handle);

            // Both indices are already available for coloring. Cache the bucket now
            // rather than chasing contact/body handles again during the ordering pass.
            // A shared read-only floor must not pull every manifold into its bucket.
            int bucket = writable1
                ? (writable2 ? Math.Min(body1, body2) : body1) >> ContactBodyBucketShift
                : writable2 ? body2 >> ContactBodyBucketShift : readOnlyBucket;
            if (!color.NeedsLocalitySort && color.BodyBuckets.Count != 0 && bucket < color.BodyBuckets[^1])
                color.NeedsLocalitySort = true;
            color.BodyBuckets.Add(bucket);

            contactColorCount = Math.Max(contactColorCount, index + 1);
            ulong bit = 1UL << index;
            if (writable1) bodyContactColors[body1] |= bit;
            if (writable2) bodyContactColors[body2] |= bit;
        }

        for (int i = 0; i < contactColorCount; i++)
        {
            ContactColor color = contactColors[i];
            OrderContactColor(color, readOnlyBucket + 1);
            color.BundleCount = System.Numerics.Vector.IsHardwareAccelerated
                ? color.Contacts.Count / ContactSolverWide.Width : 0;
            while (color.Bundles.Count < color.BundleCount) color.Bundles.Add(new ContactSolverWide());
        }
    }

    private void OrderContactColor(ContactColor color, int bucketCount)
    {
        if (color.NeedsLocalitySort)
        {
            var contacts = CollectionsMarshal.AsSpan(color.Contacts);
            var keys = CollectionsMarshal.AsSpan(color.BodyBuckets);
            if (contactBucketOffsets.Length < bucketCount) contactBucketOffsets = new int[bucketCount];
            else Array.Clear(contactBucketOffsets, 0, bucketCount);
            if (contactBucketScratch.Length < contacts.Length)
                contactBucketScratch = new JHandle<ContactData>[Math.Max(contacts.Length, contactBucketScratch.Length * 2)];

            foreach (int bucket in keys) contactBucketOffsets[bucket]++;
            int offset = 0;
            for (int bucket = 0; bucket < bucketCount; bucket++)
            {
                int count = contactBucketOffsets[bucket];
                contactBucketOffsets[bucket] = offset;
                offset += count;
            }

            // Stable counting scatter: preserve the original order inside each bucket.
            // Only independent contacts within one completed color can move here.
            for (int i = 0; i < contacts.Length; i++)
                contactBucketScratch[contactBucketOffsets[keys[i]]++] = contacts[i];
            contactBucketScratch.AsSpan(0, contacts.Length).CopyTo(contacts);
        }

        color.BodyBuckets.Clear();
        color.NeedsLocalitySort = false;
    }

    private void DispatchContactColors(bool multiThread, Action<Parallel.Batch> action)
    {
        for (int i = 0; i < contactColorCount; i++)
        {
            currentContactColor = contactColors[i];
            int count = currentContactColor.Contacts.Count;
            int jobs = (count + ContactSolverWide.Width - 1) / ContactSolverWide.Width;
            int tasks = multiThread ? Math.Min(count / 128 + 1, ThreadPool.Instance.ThreadCount) : 1;
            if (tasks == 1) action(new Parallel.Batch(0, jobs));
            else Parallel.ForBatch(0, jobs, Math.Min(tasks, jobs), action);
            // ForBatch completes here: no body shared by adjacent colors is in flight.
        }
    }

    private void PrepareContactColor(Parallel.Batch batch)
    {
        ContactColor color = currentContactColor;
        var contacts = CollectionsMarshal.AsSpan(color.Contacts);
        int width = ContactSolverWide.Width;
        for (int i = batch.Start; i < batch.End; i++)
        {
            var lanes = contacts.Slice(i * width, Math.Min(width, contacts.Length - i * width));
            if (i < color.BundleCount) color.Bundles[i].Prepare(lanes, timeStep);
            else foreach (var handle in lanes) handle.Data.PrepareForIteration(timeStep);
        }
    }

    private void IterateContactColor(Parallel.Batch batch)
    {
        ContactColor color = currentContactColor;
        var contacts = CollectionsMarshal.AsSpan(color.Contacts);
        int width = ContactSolverWide.Width;
        for (int i = batch.Start; i < batch.End; i++)
        {
            if (i < color.BundleCount)
            {
                color.Bundles[i].Iterate(coloredApplyBias);
            }
            else
            {
                int end = Math.Min((i + 1) * width, contacts.Length);
                for (int j = i * width; j < end; j++) contacts[j].Data.Iterate(coloredApplyBias);
            }
        }
    }

    private void FinishContactColor(Parallel.Batch batch)
    {
        ContactColor color = currentContactColor;
        int end = Math.Min(batch.End, color.BundleCount);
        for (int i = batch.Start; i < end; i++)
        {
            color.Bundles[i].WriteBack();
        }
    }

    private void IterateColoredContacts(bool multiThread, bool applyBias)
    {
        coloredApplyBias = applyBias;
        DispatchContactColors(multiThread, iterateContactColor);
        foreach (var handle in contactColorOverflow) handle.Data.Iterate(applyBias);
    }

    private void SolveColoredContacts(bool multiThread, int iterations)
    {
        // Build after PreSubStep: callbacks may change contact modes or topology. Rebuild
        // on each substep so removals, activation and switching solvers cannot stale handles.
        BuildContactColors();
        DispatchContactColors(multiThread, prepareContactColor);
        foreach (var handle in contactColorOverflow) handle.Data.PrepareForIteration(timeStep);

        // Joint tasks may share bodies with any color. They retain their existing locks
        // and must complete before starting the next lock-free contact pass.
        if (multiThread)
        {
            memConstraints.ParallelForBatch(64, prepareConstraints, false);
            memSmallConstraints.ParallelForBatch(64, prepareSmallConstraints, false);
            ThreadPool.Instance.Execute();
        }
        else
        {
            PrepareConstraints(new Parallel.Batch(0, memConstraints.Active.Length));
            PrepareSmallConstraints(new Parallel.Batch(0, memSmallConstraints.Active.Length));
        }

        for (int iter = 0; iter < iterations; iter++)
        {
            IterateColoredContacts(multiThread, applyBias: true);
            if (multiThread)
            {
                memConstraints.ParallelForBatch(64, iterateConstraints, false);
                memSmallConstraints.ParallelForBatch(64, iterateSmallConstraints, false);
                ThreadPool.Instance.Execute();
            }
            else
            {
                IterateConstraints(new Parallel.Batch(0, memConstraints.Active.Length));
                IterateSmallConstraints(new Parallel.Batch(0, memSmallConstraints.Active.Length));
            }
        }
    }

    private void RelaxColoredContacts(bool multiThread, int iterations)
    {
        // Integration changes velocities and transforms, but world inertia is refreshed
        // by UpdateBodies at the end of Step. Keep the prepared anchors and inertia
        // fixed so velocity relaxation operates on the same constraints.
        for (int iter = 0; iter < iterations; iter++) IterateColoredContacts(multiThread, applyBias: false);

        // Persist warm-start state even when there are no relaxation iterations.
        DispatchContactColors(multiThread, finishContactColor);
    }

    private void ClearContactColors()
    {
        contactColors.Clear();
        contactColorOverflow.Clear();
        contactColorCount = 0;
        currentContactColor = null!;
        bodyContactColors = Array.Empty<ulong>();
        contactBodyWritable = Array.Empty<bool>();
        contactBucketOffsets = Array.Empty<int>();
        contactBucketScratch = Array.Empty<JHandle<ContactData>>();
    }

    private void TrimContactColors()
    {
        // Scratch is rebuilt on the next solve. Releasing it avoids retaining bundles
        // and handle arrays belonging to a formerly much larger scene.
        ClearContactColors();
        contactColors.TrimExcess();
        contactColorOverflow.TrimExcess();
    }
}
