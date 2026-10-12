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

    private sealed class ContactColor
    {
        internal readonly List<JHandle<ContactData>> Contacts = new();
        internal readonly List<ContactSolverWide> Bundles = new();
        internal int BundleCount;
    }

    private readonly List<ContactColor> contactColors = new();
    private readonly List<JHandle<ContactData>> contactColorOverflow = new();
    private ulong[] bodyContactColors = Array.Empty<ulong>();
    private bool[] contactBodyWritable = Array.Empty<bool>();
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
        foreach (ContactColor color in contactColors) color.Contacts.Clear();

        var contacts = memContacts.Active;

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
            ulong occupied = (contactBodyWritable[body1] ? bodyContactColors[body1] : 0) |
                             (contactBodyWritable[body2] ? bodyContactColors[body2] : 0);
            var handle = memContacts.GetHandle(ref contact);
            int index = BitOperations.TrailingZeroCount(~occupied);
            if (index >= MaximumContactColors)
            {
                contactColorOverflow.Add(handle);
                continue;
            }

            if (index == contactColors.Count) contactColors.Add(new ContactColor());
            contactColors[index].Contacts.Add(handle);
            contactColorCount = Math.Max(contactColorCount, index + 1);
            ulong bit = 1UL << index;
            if (contactBodyWritable[body1]) bodyContactColors[body1] |= bit;
            if (contactBodyWritable[body2]) bodyContactColors[body2] |= bit;
        }

        for (int i = 0; i < contactColorCount; i++)
        {
            ContactColor color = contactColors[i];
            color.BundleCount = System.Numerics.Vector.IsHardwareAccelerated
                ? color.Contacts.Count / ContactSolverWide.Width : 0;
            while (color.Bundles.Count < color.BundleCount) color.Bundles.Add(new ContactSolverWide());
        }
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
            foreach (var handle in lanes) handle.Data.PrepareForIteration(timeStep);
            if (i < color.BundleCount) color.Bundles[i].Prepare(lanes);
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
        // by UpdateBodies at the end of Step. Prepared angular responses remain valid.
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
