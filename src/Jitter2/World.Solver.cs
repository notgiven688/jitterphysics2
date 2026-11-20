/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

// The solver. Contacts and constraints are grouped so that no two of a group share a dynamic
// body, see ContactColoring and ConstraintColoring, and each group is solved in a pass of its own,
// without locks. Contacts are solved eight at once as the lanes of a SIMD bundle.
//
// For given groups the result does not depend on the number of threads or on their timing: the
// passes run in a fixed order, and what is solved within a pass cannot influence each other.
// Which contacts share a bundle and which worker solves which part of a pass therefore makes no
// difference. The solve mode decides how the contacts get their groups: from scratch in every
// step, so that they depend only on which contacts exist, or kept from step to step, which is
// faster but makes them depend on the order in which the contacts appeared, and with several
// threads on the order in which the collision threads reported them.
public sealed partial class World
{
    // Every pass ends with a barrier that each worker has to cross, and its work is shared among
    // the workers. With w workers a solve therefore costs about work / w + passes * w barrier
    // crossings, which is least for w = sqrt(work / passes): the more work an average pass holds,
    // the more workers it is worth waiting for. Work is counted in tenths of a barrier crossing.
    private const int BundleWork = 17, ConstraintWork = 5, BodyWork = 2, RecordWork = 5, BarrierWork = 10;

    // Adding workers wakes parked pool threads, so a small raise waits until more workers were
    // justified for a number of solves in a row; contact churn would otherwise change the count
    // every few steps. A raise to more than twice the count, as when a scene wakes up, is taken
    // at once.
    private const int WorkerRaiseDelay = 32;

    // A color pass costs a barrier. Neighboring colors with fewer contacts than this are solved
    // one after another by a single worker in one pass instead. The colors are still solved in
    // their order, so the result is the same as with a pass for each.
    private const int SmallColorLength = 64;

    // Without hardware vectors the lanes of a bundle are solved one by one with the scalar
    // contact code, which rounds exactly like the bundle. The result is the same either way.
    private static readonly bool UseBundles = Vector.IsHardwareAccelerated;

    private ContactColoring contactColoring = null!;
    private ConstraintColoring constraintColoring = null!;

    // The bundles of color c start at colorBundleStart[c].
    private readonly List<ContactSolverWide> contactBundles = [];
    private readonly int[] colorBundleStart = new int[ContactColoring.MaximumColors];

    private enum SolverPassKind : byte
    {
        IntegrateForces,
        PrepareContacts,
        IterateContacts,
        WriteBackContacts,
        PrepareConstraints,
        IterateConstraints,
        IntegrateVelocities,
        UpdateRemainingContacts
    }

    private struct SolverPass
    {
        internal SolverPassKind Kind;
        // The pass covers the colors from Color up to ColorEnd.
        internal int Color, ColorEnd;
        // The number of items the workers share: bundles, constraints, bodies or records.
        internal int Total;
        // A serial pass is run by the first worker alone, in order.
        internal bool ApplyBias, WriteBack, UpdateContacts, Serial;
    }

    // One worker loop per substep runs the whole solve on the participating pool threads: force
    // integration, preparation, the biased sweeps, velocity integration and the relaxation
    // sweeps. Passes that must not overlap are separated by a spin barrier, which costs far less
    // than dispatching each pass through the task queues. Every worker owns the same share of
    // each pass in every sweep.
    private readonly List<SolverPass> solverPasses = [];
    // The workers of the current solve, and of the last one that used the thread pool.
    private int solverWorkerCount, parallelWorkerCount;
    private int solverWorkerRaiseStreak;
    private int solverArrivals;
    private int solverEpoch;
    private ExceptionDispatchInfo? solverFailure;
    private Action<int>? solverWorkerEntry;

    // Releases the scratch memory of the solver. The next solve allocates what it needs.
    private void TrimSolverScratch()
    {
        contactColoring.Trim();
        constraintColoring.Trim();
        contactBundles.Clear();
        contactBundles.TrimExcess();
    }

    /// <summary>
    /// Solves contacts and constraints for one substep. With <paramref name="integrate"/> set, forces
    /// are integrated before and velocities after the biased sweeps.
    /// </summary>
    /// <param name="multiThread">Whether to use the thread pool.</param>
    /// <param name="iterations">The number of biased sweeps.</param>
    /// <param name="relaxations">The number of relaxation sweeps.</param>
    /// <param name="integrate">Whether this is a full substep rather than a velocity-only solve.</param>
    /// <param name="regroup">Whether contacts or constraints may have changed since the previous solve.</param>
    /// <param name="updateContacts">Whether to finish with the contact update of the step.</param>
    private void Solve(bool multiThread, int iterations, int relaxations, bool integrate,
        bool regroup = true, bool updateContacts = false)
    {
        // The groups of the previous solve are reused only when the caller knows that nothing
        // could have added, removed or edited a contact or constraint since.
        if (regroup)
        {
            if (solveMode == SolveMode.Deterministic) contactColoring.Rebuild();
            else contactColoring.Update();
            constraintColoring.Update();
            AssignBundles();
        }

        BuildSolverPasses(iterations, relaxations, integrate, updateContacts);
        if (solverPasses.Count == 0) return;

        solverWorkerCount = multiThread ? ChooseSolverWorkers(ThreadPool.Instance.ThreadCount) : 1;
        solverArrivals = solverWorkerCount;
        solverEpoch = 0;
        solverFailure = null;

        if (solverWorkerCount == 1)
        {
            RunSolverProgram(0);
        }
        else
        {
            solverWorkerEntry ??= SolverWorkerEntry;
            ThreadPool.Instance.ExecuteWorkerLoop(solverWorkerEntry, solverWorkerCount);
        }

        solverFailure?.Throw();
    }

    private static int BundleCount(int contacts) => (contacts + ContactSolverWide.Width - 1) / ContactSolverWide.Width;

    private void AssignBundles()
    {
        int bundles = 0;
        for (int c = 0; c < contactColoring.ColorCount; c++)
        {
            colorBundleStart[c] = bundles;
            bundles += BundleCount(contactColoring.Length(c));
        }

        while (UseBundles && contactBundles.Count < bundles) contactBundles.Add(new ContactSolverWide());
    }

    // The number of workers changes how fast a solve is, never its result.
    private int ChooseSolverWorkers(int threadCount)
    {
        long work = 0;
        int barriers = 0;
        foreach (ref readonly SolverPass pass in CollectionsMarshal.AsSpan(solverPasses))
        {
            // A serial pass is one worker's, however many wait for it.
            if (pass.Serial) continue;
            barriers++;
            work += pass.Kind switch
            {
                SolverPassKind.IntegrateForces or SolverPassKind.IntegrateVelocities => (long)pass.Total * BodyWork,
                SolverPassKind.PrepareConstraints or SolverPassKind.IterateConstraints => (long)pass.Total * ConstraintWork,
                SolverPassKind.UpdateRemainingContacts => (long)pass.Total * RecordWork,
                _ => (long)pass.Total * BundleWork
            };
        }

        int workers = barriers == 0 ? 1 : Math.Clamp((int)Math.Sqrt(work / (BarrierWork * barriers)), 1, threadCount);
        int previous = Math.Min(parallelWorkerCount, threadCount);
        bool smallRaise = workers > previous && workers <= 2 * previous;
        if (smallRaise && ++solverWorkerRaiseStreak < WorkerRaiseDelay) workers = previous;
        else solverWorkerRaiseStreak = 0;

        return parallelWorkerCount = workers;
    }

    private void BuildSolverPasses(int iterations, int relaxations, bool integrate, bool updateContacts)
    {
        solverPasses.Clear();

        if (integrate) AddPass(SolverPassKind.IntegrateForces, memRigidBodies.Active.Length);

        AddContactPasses(SolverPassKind.PrepareContacts);
        AddConstraintPasses(SolverPassKind.PrepareConstraints);

        for (int iter = 0; iter < iterations; iter++)
        {
            AddContactPasses(SolverPassKind.IterateContacts, applyBias: true);
            AddConstraintPasses(SolverPassKind.IterateConstraints);
        }

        if (integrate) AddPass(SolverPassKind.IntegrateVelocities, memRigidBodies.Active.Length);

        // Relaxation solves the contacts only, on the anchors and masses prepared before the
        // bodies moved. Its last sweep also publishes the impulses and refreshes the contacts
        // while each bundle and its records are in the worker's cache.
        for (int iter = 0; iter < relaxations; iter++)
        {
            bool last = iter == relaxations - 1;
            AddContactPasses(SolverPassKind.IterateContacts, writeBack: last, updateContacts: last && updateContacts);
        }

        if (relaxations == 0) AddContactPasses(SolverPassKind.WriteBackContacts, updateContacts: updateContacts);

        // The contacts without a color.
        int remaining = contactColoring.Overflow.Length;
        if (updateContacts && remaining != 0) AddPass(SolverPassKind.UpdateRemainingContacts, remaining);
    }

    // One pass for each color, the neighboring small colors sharing a serial pass, and a serial
    // pass for the contacts without a color.
    private void AddContactPasses(SolverPassKind kind, bool applyBias = false, bool writeBack = false,
        bool updateContacts = false)
    {
        var pass = new SolverPass { Kind = kind, ApplyBias = applyBias, WriteBack = writeBack, UpdateContacts = updateContacts };

        for (int c = 0; c < contactColoring.ColorCount;)
        {
            pass.Color = c;
            if (contactColoring.Length(c) >= SmallColorLength)
            {
                (pass.ColorEnd, pass.Total, pass.Serial) = (++c, BundleCount(contactColoring.Length(pass.Color)), false);
            }
            else
            {
                while (c < contactColoring.ColorCount && contactColoring.Length(c) < SmallColorLength) c++;
                (pass.ColorEnd, pass.Total, pass.Serial) = (c, 1, true);
            }

            solverPasses.Add(pass);
        }

        // Their impulses live in the records, so there is nothing to write back.
        if (contactColoring.Overflow.Length != 0 && kind != SolverPassKind.WriteBackContacts)
        {
            (pass.Color, pass.ColorEnd, pass.Total, pass.Serial) = (ContactColoring.OverflowColor, ContactColoring.OverflowColor, 1, true);
            pass.UpdateContacts = false;
            solverPasses.Add(pass);
        }
    }

    private void AddConstraintPasses(SolverPassKind kind)
    {
        var pass = new SolverPass { Kind = kind };

        // One pass for each color, the neighboring small colors sharing a serial pass.
        for (int c = 0; c < constraintColoring.ColorCount;)
        {
            pass.Color = c;
            if (constraintColoring.Length(c) >= SmallColorLength)
            {
                (pass.ColorEnd, pass.Total, pass.Serial) = (++c, constraintColoring.Length(pass.Color), false);
            }
            else
            {
                while (c < constraintColoring.ColorCount && constraintColoring.Length(c) < SmallColorLength) c++;
                (pass.ColorEnd, pass.Total, pass.Serial) = (c, 1, true);
            }

            solverPasses.Add(pass);
        }

        // The constraints without a color, in the order of their constraint ids.
        if (constraintColoring.Length(ConstraintColoring.OverflowColor) != 0)
        {
            (pass.Color, pass.ColorEnd, pass.Total, pass.Serial) =
                (ConstraintColoring.OverflowColor, ConstraintColoring.OverflowColor + 1, 1, true);
            solverPasses.Add(pass);
        }
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

            TraceBegin(passes, k);
            if (start < end)
            {
                Tracer.ProfileBegin(TraceName.Pass);
                RunPass(in pass, start, end);
                Tracer.ProfileEnd(TraceName.Pass);
            }

            bool proceed = SolverBarrier();
            TraceEnd(passes, k, !proceed);
            if (!proceed) return;
        }
    }

    // The trace shows one slice for the consecutive passes of a name, with their barriers, and
    // inside it one slice for each pass the worker has a share of.
    private static TraceName TraceNameOf(in SolverPass pass)
    {
        if (pass.Kind != SolverPassKind.IterateContacts || pass.ApplyBias) return TraceName.IntegrateForces + (int)pass.Kind;
        return pass.WriteBack ? TraceName.RelaxAndWriteBack : TraceName.RelaxContacts;
    }

    [Conditional("PROFILE")]
    private static void TraceBegin(ReadOnlySpan<SolverPass> passes, int k)
    {
        TraceName name = TraceNameOf(passes[k]);
        if (k == 0 || TraceNameOf(passes[k - 1]) != name) Tracer.ProfileBegin(name);
    }

    [Conditional("PROFILE")]
    private static void TraceEnd(ReadOnlySpan<SolverPass> passes, int k, bool stopped)
    {
        TraceName name = TraceNameOf(passes[k]);
        if (stopped || k == passes.Length - 1 || TraceNameOf(passes[k + 1]) != name) Tracer.ProfileEnd(name);
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

    private void RunPass(in SolverPass pass, int start, int end)
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
                if (pass.Color == ContactColoring.OverflowColor) SolveOverflowContacts(in pass);
                else if (!pass.Serial) SolveContactColor(in pass, pass.Color, start, end);
                else
                {
                    for (int c = pass.Color; c < pass.ColorEnd; c++)
                        SolveContactColor(in pass, c, 0, BundleCount(contactColoring.Length(c)));
                }

                break;
            case SolverPassKind.PrepareConstraints:
            case SolverPassKind.IterateConstraints:
                bool prepare = pass.Kind == SolverPassKind.PrepareConstraints;
                if (!pass.Serial)
                {
                    var share = constraintColoring.Constraints(pass.Color).Slice(start, end - start);
                    foreach (ref readonly ConstraintColoring.Entry entry in share) SolveConstraint(in entry, prepare);
                }
                else
                {
                    for (int c = pass.Color; c < pass.ColorEnd; c++)
                    {
                        foreach (ref readonly ConstraintColoring.Entry entry in constraintColoring.Constraints(c))
                            SolveConstraint(in entry, prepare);
                    }
                }

                break;
            case SolverPassKind.UpdateRemainingContacts:
                var overflow = contactColoring.Overflow;
                for (int i = start; i < end; i++) UpdateSolvedContact(overflow[i]);

                break;
        }
    }

    private void SolveContactColor(in SolverPass pass, int color, int start, int end)
    {
        var contacts = contactColoring.Contacts(color);
        int first = colorBundleStart[color];
        for (int i = start; i < end; i++)
        {
            int from = i * ContactSolverWide.Width;
            var handles = contacts.Slice(from, Math.Min(ContactSolverWide.Width, contacts.Length - from));

            if (UseBundles)
            {
                ContactSolverWide bundle = contactBundles[first + i];
                switch (pass.Kind)
                {
                    case SolverPassKind.PrepareContacts:
                        bundle.Prepare(handles, timeStep);
                        break;
                    case SolverPassKind.IterateContacts:
                        bundle.Iterate(pass.ApplyBias, pass.WriteBack);
                        break;
                    default:
                        bundle.WriteBack();
                        break;
                }
            }
            else
            {
                foreach (var handle in handles) SolveContact(ref handle.Data, in pass);
            }

            if (!pass.UpdateContacts) continue;
            foreach (var handle in handles) UpdateSolvedContact(handle);
        }
    }

    private void SolveOverflowContacts(in SolverPass pass)
    {
        foreach (var handle in contactColoring.Overflow) SolveContact(ref handle.Data, in pass);
    }

    // The scalar contact code keeps its impulses in the record, so a write-back has nothing to do.
    private void SolveContact(ref ContactData contact, in SolverPass pass)
    {
        if ((contact.UsageMask & ContactData.MaskContactAll) == 0) return;
        if (pass.Kind == SolverPassKind.PrepareContacts) contact.PrepareForIteration(timeStep);
        else if (pass.Kind == SolverPassKind.IterateContacts) contact.Iterate(pass.ApplyBias);
    }

    // A substep callback may have disabled the constraint since the passes were built.
    private void SolveConstraint(in ConstraintColoring.Entry entry, bool prepare)
    {
        if (!entry.Small)
        {
            ref ConstraintData constraint = ref entry.Handle.Data;
            if (!constraint.IsEnabled) return;
            if (prepare) constraint.PrepareForIteration(ref constraint, timeStep);
            else constraint.Iterate(ref constraint, timeStep);
        }
        else
        {
            ref SmallConstraintData constraint = ref JHandle<ConstraintData>.AsHandle<SmallConstraintData>(entry.Handle).Data;
            if (!constraint.IsEnabled) return;
            if (prepare) constraint.PrepareForIteration(ref constraint, timeStep);
            else constraint.Iterate(ref constraint, timeStep);
        }
    }

    // The contact update of the step, run by the solver on the records it has just written while
    // they and their bodies are still in the worker's cache.
    private void UpdateSolvedContact(JHandle<ContactData> handle)
    {
        ref ContactData contact = ref handle.Data;
        contact.UpdatePosition();
        if ((contact.UsageMask & ContactData.MaskContactAll) == 0) brokenArbiters.ConcurrentAdd(handle);
    }
}
