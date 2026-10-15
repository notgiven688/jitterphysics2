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

    // These entries are indexed by the stable PartitionedBuffer handle ID, never
    // the physical slot: activation and contact reordering can move that slot.
    private struct ContactGraphEntry
    {
        internal JHandle<ContactData> Handle;
        internal JHandle<RigidBodyData> Body1, Body2;
        internal ContactData.SolveMode Mode;
        internal int BodyId1, BodyId2;
        internal int Color, Slot;
    }

    private ContactGraphEntry[] contactGraph = Array.Empty<ContactGraphEntry>();
    private int[] contactBodyReferences = Array.Empty<int>();
    private readonly List<JHandle<ContactData>> pendingColoredContacts = new();
    private bool contactGraphValid;
    private bool contactGraphChanged;
    // Diagnostic counts used by the focused benchmark and lifecycle checks.
    private int contactGraphRebuildCount, contactGraphReuseCount, contactGraphIncrementalCount;

    private void BuildContactColors()
    {
        if (!contactGraphValid)
        {
            RebuildContactColors();
            return;
        }

        pendingColoredContacts.Clear();
        bool rebuild = false;
        // Public contact fields can be edited directly, including in PreSubStep.
        // Validate once in physical contact order. An unchanged edge does not touch
        // body masks, color lists, or SIMD bundle allocation.
        foreach (ref ContactData contact in memContacts.Active)
        {
            EnsureContactGraphCapacity(contact._internal);
            ref ContactGraphEntry entry = ref contactGraph[contact._internal];
            bool used = (contact.UsageMask & ContactData.MaskContactAll) != 0;
            if (!entry.Handle.IsZero && used && entry.Body1 == contact.Body1 &&
                entry.Body2 == contact.Body2 && entry.Mode == contact.Mode) continue;
            if (!entry.Handle.IsZero) RemoveColoredContact(entry.Handle);
            if (!used) continue;
            var handle = memContacts.GetHandle(ref contact);
            InitializeContactGraphEntry(handle, ref rebuild);
            pendingColoredContacts.Add(handle);
            contactGraphChanged = true;
        }

        // Only a reader-to-writer promotion invalidates colors already assigned to
        // other contacts. Ordinary contact churn can stay incremental.
        if (rebuild)
        {
            RebuildContactColors();
            return;
        }

        foreach (var handle in pendingColoredContacts) AddColoredContact(handle);
        if (contactGraphChanged)
        {
            // Retry serial overflow only when the graph has changed.
            for (int i = contactColorOverflow.Count - 1; i >= 0; i--)
            {
                var handle = contactColorOverflow[i];
                ref ContactGraphEntry entry = ref contactGraph[handle.Data._internal];
                RemoveContactColorSlot(ref entry);
                AddColoredContact(handle);
            }
            RefreshContactColorBundles();
            contactGraphIncrementalCount++;
        }
        else contactGraphReuseCount++;
        contactGraphChanged = false;
    }

    private void EnsureContactGraphCapacity(int id)
    {
        if (id >= contactGraph.Length)
            Array.Resize(ref contactGraph, Math.Max(id + 1, Math.Max(128, contactGraph.Length * 2)));
    }

    private void EnsureContactBodyCapacity(int id)
    {
        if (id < bodyContactColors.Length) return;
        int capacity = Math.Max(id + 1, Math.Max(128, bodyContactColors.Length * 2));
        Array.Resize(ref bodyContactColors, capacity);
        Array.Resize(ref contactBodyWritable, capacity);
        Array.Resize(ref contactBodyReferences, capacity);
    }

    private void AddContactBodyReference(int id, bool writable, ref bool rebuild)
    {
        EnsureContactBodyCapacity(id);
        if (writable && !contactBodyWritable[id])
        {
            // Previously shared readers must now exclude each other too. Updating
            // only this edge would permit a reader/writer race in an existing color.
            rebuild |= contactBodyReferences[id] != 0;
            contactBodyWritable[id] = true;
        }
        contactBodyReferences[id]++;
    }

    private void InitializeContactGraphEntry(JHandle<ContactData> handle, ref bool rebuild)
    {
        ref ContactData contact = ref handle.Data;
        EnsureContactGraphCapacity(contact._internal);
        int first = contact.Body1.Data._index, second = contact.Body2.Data._index;
        contactGraph[contact._internal] = new ContactGraphEntry
        {
            Handle = handle, Body1 = contact.Body1, Body2 = contact.Body2, Mode = contact.Mode,
            BodyId1 = first, BodyId2 = second, Color = -1
        };
        AddContactBodyReference(first, (contact.Mode & ContactData.SolveMode.FullBody1) != 0, ref rebuild);
        AddContactBodyReference(second, (contact.Mode & ContactData.SolveMode.FullBody2) != 0, ref rebuild);
    }

    private void RebuildContactColors()
    {
        Array.Clear(contactGraph);
        Array.Clear(bodyContactColors);
        Array.Clear(contactBodyWritable);
        Array.Clear(contactBodyReferences);
        contactColorOverflow.Clear();
        pendingColoredContacts.Clear();
        contactColorCount = 0;
        foreach (ContactColor color in contactColors) color.Contacts.Clear();
        bool ignored = false;
        // Classify global writers before assigning any edges, including custom modes.
        foreach (ref ContactData contact in memContacts.Active)
            if ((contact.UsageMask & ContactData.MaskContactAll) != 0)
                InitializeContactGraphEntry(memContacts.GetHandle(ref contact), ref ignored);
        foreach (ref ContactData contact in memContacts.Active)
            if ((contact.UsageMask & ContactData.MaskContactAll) != 0)
                AddColoredContact(memContacts.GetHandle(ref contact));
        RefreshContactColorBundles();
        contactGraphValid = true;
        contactGraphChanged = false;
        contactGraphRebuildCount++;
    }

    private void AddColoredContact(JHandle<ContactData> handle)
    {
        ref ContactGraphEntry entry = ref contactGraph[handle.Data._internal];
        bool writable1 = contactBodyWritable[entry.BodyId1], writable2 = contactBodyWritable[entry.BodyId2];
        ulong occupied = (writable1 ? bodyContactColors[entry.BodyId1] : 0) |
                         (writable2 ? bodyContactColors[entry.BodyId2] : 0);
        int index = BitOperations.TrailingZeroCount(~occupied);
        entry.Color = index;
        if (index >= MaximumContactColors)
        {
            entry.Slot = contactColorOverflow.Count;
            contactColorOverflow.Add(handle);
            return;
        }
        if (index == contactColors.Count) contactColors.Add(new ContactColor());
        ContactColor color = contactColors[index];
        entry.Slot = color.Contacts.Count;
        color.Contacts.Add(handle);
        contactColorCount = Math.Max(contactColorCount, index + 1);
        ulong bit = 1UL << index;
        if (writable1) bodyContactColors[entry.BodyId1] |= bit;
        if (writable2) bodyContactColors[entry.BodyId2] |= bit;
    }

    private void RemoveContactColorSlot(ref ContactGraphEntry entry)
    {
        if (entry.Color < 0) return;
        List<JHandle<ContactData>> contacts;
        if (entry.Color == MaximumContactColors) contacts = contactColorOverflow;
        else
        {
            contacts = contactColors[entry.Color].Contacts;
            ulong bit = ~(1UL << entry.Color);
            if (contactBodyWritable[entry.BodyId1]) bodyContactColors[entry.BodyId1] &= bit;
            if (contactBodyWritable[entry.BodyId2]) bodyContactColors[entry.BodyId2] &= bit;
        }
        int last = contacts.Count - 1;
        if (entry.Slot != last)
        {
            var moved = contacts[last];
            contacts[entry.Slot] = moved;
            contactGraph[moved.Data._internal].Slot = entry.Slot;
        }
        contacts.RemoveAt(last);
        entry.Color = -1;
    }

    private void RemoveContactBodyReference(int id)
    {
        if (--contactBodyReferences[id] != 0) return;
        bodyContactColors[id] = 0;
        contactBodyWritable[id] = false;
    }

    // Remove before freeing/recycling a handle or moving it into sleeping storage.
    // Use cached endpoints: public fields may already refer to different bodies.
    private void RemoveColoredContact(JHandle<ContactData> handle)
    {
        int id = handle.Data._internal;
        if (id >= contactGraph.Length || contactGraph[id].Handle.IsZero) return;
        ref ContactGraphEntry entry = ref contactGraph[id];
        RemoveContactColorSlot(ref entry);
        RemoveContactBodyReference(entry.BodyId1);
        RemoveContactBodyReference(entry.BodyId2);
        entry = default;
        contactGraphChanged = true;
    }

    private void RefreshContactColorBundles()
    {
        while (contactColorCount > 0 && contactColors[contactColorCount - 1].Contacts.Count == 0) contactColorCount--;
        for (int c = 0; c < contactColorCount; c++)
        {
            ContactColor color = contactColors[c];
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
        // Validate after PreSubStep; retain occupancy masks and color lists
        // when endpoints, participation and write modes are unchanged.
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
        contactGraphValid = false;
        contactGraphChanged = false;
        contactGraph = Array.Empty<ContactGraphEntry>();
        contactBodyReferences = Array.Empty<int>();
        pendingColoredContacts.Clear();
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
        pendingColoredContacts.TrimExcess();
    }
}
