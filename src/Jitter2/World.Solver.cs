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
using Jitter2.Dynamics.Solver;
using Jitter2.Parallelization;
using ThreadPool = Jitter2.Parallelization.ThreadPool;

namespace Jitter2;

// The solver. Contacts and joints are grouped so that no two of a group share a dynamic body,
// see ContactColoring and JointGrouping, and each group is solved in a pass of its own, without
// locks. Contacts are solved eight at once as the lanes of a SIMD bundle.
//
// The result does not depend on the number of threads or on their timing: the groups depend only
// on which contacts and joints exist, the passes run in a fixed order, and what is solved within
// a pass cannot influence each other. Which contacts share a bundle and which worker solves
// which part of a pass therefore makes no difference.
public sealed partial class World
{
    // Every pass ends with a barrier that each worker has to cross, and its work is shared among
    // the workers. With w workers a solve therefore costs about work / w + passes * w barrier
    // crossings, which is least for w = sqrt(work / passes): the more work an average pass holds,
    // the more workers it is worth waiting for. Work is counted in tenths of a barrier crossing.
    private const int BundleWork = 17, JointWork = 5, BodyWork = 2, RecordWork = 5, BarrierWork = 10;

    // Adding workers wakes parked pool threads, so the count is raised only once more workers
    // were justified for a number of solves in a row; contact churn would otherwise change it
    // every few steps.
    private const int WorkerRaiseDelay = 32;

    // A color pass costs a barrier. Neighboring colors with fewer contacts than this are solved
    // one after another by a single worker in one pass instead. The colors are still solved in
    // their order, so the result is the same as with a pass for each.
    private const int SmallColorLength = 64;

    // Without hardware vectors the lanes of a bundle are solved one by one with the scalar
    // contact code, which rounds exactly like the bundle. The result is the same either way.
    private static readonly bool UseBundles = Vector.IsHardwareAccelerated;

    private ContactColoring contactColoring = null!;
    private JointGrouping jointGrouping = null!;

    // The bundles of color c start at colorBundleStart[c].
    private readonly List<ContactSolverWide> contactBundles = [];
    private readonly int[] colorBundleStart = new int[ConstraintGraph.MaximumColors];

    private enum SolverPassKind : byte
    {
        IntegrateForces,
        PrepareContacts,
        IterateContacts,
        WriteBackContacts,
        PrepareJoints,
        IterateJoints,
        IntegrateVelocities,
        UpdateRemainingContacts
    }

    private struct SolverPass
    {
        internal SolverPassKind Kind;
        // A contact pass covers the colors from Color up to ColorEnd, a joint pass the pass of
        // that number of the joint grouping.
        internal int Color, ColorEnd;
        // The number of items the workers share: bundles, units of joints, bodies or records.
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
        jointGrouping.Trim();
        contactBundles.Clear();
        contactBundles.TrimExcess();
    }

    /// <summary>
    /// Solves contacts and joints for one substep. With <paramref name="integrate"/> set, forces
    /// are integrated before and velocities after the biased sweeps.
    /// </summary>
    /// <param name="multiThread">Whether to use the thread pool.</param>
    /// <param name="iterations">The number of biased sweeps.</param>
    /// <param name="relaxations">The number of relaxation sweeps.</param>
    /// <param name="integrate">Whether this is a full substep rather than a velocity-only solve.</param>
    /// <param name="regroup">Whether contacts or joints may have changed since the previous solve.</param>
    /// <param name="updateContacts">Whether to finish with the contact update of the step.</param>
    private void Solve(bool multiThread, int iterations, int relaxations, bool integrate,
        bool regroup = true, bool updateContacts = false)
    {
        // The groups of the previous solve are reused only when the caller knows that nothing
        // could have added, removed or edited a contact or joint since.
        if (regroup)
        {
            // Reading the records is shared among the workers of the last solve.
            int readers = multiThread ? Math.Min(parallelWorkerCount, ThreadPool.Instance.ThreadCount) : 1;
            contactColoring.Update(readers);
            jointGrouping.Update(readers);
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
                SolverPassKind.PrepareJoints or SolverPassKind.IterateJoints =>
                    (long)jointGrouping.Joints(pass.Color) * JointWork,
                SolverPassKind.UpdateRemainingContacts => (long)pass.Total * RecordWork,
                _ => (long)pass.Total * BundleWork
            };
        }

        int workers = barriers == 0 ? 1 : Math.Clamp((int)Math.Sqrt(work / (BarrierWork * barriers)), 1, threadCount);
        int previous = Math.Min(parallelWorkerCount, threadCount);
        if (workers > previous && previous != 0 && ++solverWorkerRaiseStreak < WorkerRaiseDelay) workers = previous;
        else solverWorkerRaiseStreak = 0;

        return parallelWorkerCount = workers;
    }

    private void BuildSolverPasses(int iterations, int relaxations, bool integrate, bool updateContacts)
    {
        solverPasses.Clear();

        if (integrate) AddPass(SolverPassKind.IntegrateForces, memRigidBodies.Active.Length);

        AddContactPasses(SolverPassKind.PrepareContacts);
        AddJointPasses(SolverPassKind.PrepareJoints);

        for (int iter = 0; iter < iterations; iter++)
        {
            AddContactPasses(SolverPassKind.IterateContacts, applyBias: true);
            AddJointPasses(SolverPassKind.IterateJoints);
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

        // The contacts without a color and the empty manifolds lie side by side.
        int remaining = contactColoring.Length(ContactColoring.OverflowSegment) +
                        contactColoring.Length(ContactColoring.UnsolvedSegment);
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
        if (contactColoring.Overflow.Count != 0 && kind != SolverPassKind.WriteBackContacts)
        {
            (pass.Color, pass.ColorEnd, pass.Total, pass.Serial) = (ContactColoring.OverflowSegment, ContactColoring.OverflowSegment, 1, true);
            pass.UpdateContacts = false;
            solverPasses.Add(pass);
        }
    }

    private void AddJointPasses(SolverPassKind kind)
    {
        for (int p = 0; p < JointGrouping.PassCount; p++)
        {
            int units = jointGrouping.Units(p);
            if (units == 0) continue;
            solverPasses.Add(new SolverPass { Kind = kind, Color = p, Total = units, Serial = JointGrouping.IsSerial(p) });
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

            if (start < end) RunPass(in pass, start, end);
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
                if (pass.Color == ContactColoring.OverflowSegment) SolveOverflowContacts(in pass);
                else if (!pass.Serial) SolveContactColor(in pass, pass.Color, start, end);
                else
                {
                    for (int c = pass.Color; c < pass.ColorEnd; c++)
                        SolveContactColor(in pass, c, 0, BundleCount(contactColoring.Length(c)));
                }

                break;
            case SolverPassKind.PrepareJoints:
            case SolverPassKind.IterateJoints:
                foreach (int joint in jointGrouping.Joints(pass.Color, start, end))
                    SolveJoint(joint, pass.Kind == SolverPassKind.PrepareJoints);
                break;
            case SolverPassKind.UpdateRemainingContacts:
                UpdateSolvedContacts(memContacts.Active.Slice(contactColoring.Start(ContactColoring.OverflowSegment) + start, end - start));
                break;
        }
    }

    private void SolveContactColor(in SolverPass pass, int color, int start, int end)
    {
        var lanes = memContacts.Active.Slice(contactColoring.Start(color), contactColoring.Length(color));
        int first = colorBundleStart[color];
        for (int i = start; i < end; i++)
        {
            int from = i * ContactSolverWide.Width;
            var records = lanes.Slice(from, Math.Min(ContactSolverWide.Width, lanes.Length - from));

            if (UseBundles)
            {
                ContactSolverWide bundle = contactBundles[first + i];
                switch (pass.Kind)
                {
                    case SolverPassKind.PrepareContacts:
                        bundle.Prepare(records, timeStep);
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
                foreach (ref ContactData contact in records) SolveContact(ref contact, in pass);
            }

            if (pass.UpdateContacts) UpdateSolvedContacts(records);
        }
    }

    private void SolveOverflowContacts(in SolverPass pass)
    {
        foreach (var handle in contactColoring.Overflow) SolveContact(ref handle.Data, in pass);
    }

    // The scalar contact code keeps its impulses in the record, so a write-back has nothing to do.
    private void SolveContact(ref ContactData contact, in SolverPass pass)
    {
        if (pass.Kind == SolverPassKind.PrepareContacts) contact.PrepareForIteration(timeStep);
        else if (pass.Kind == SolverPassKind.IterateContacts) contact.Iterate(pass.ApplyBias);
    }

    // A substep callback may have disabled the joint since the passes were built.
    private void SolveJoint(int reference, bool prepare)
    {
        if (reference >= 0)
        {
            ref ConstraintData joint = ref memConstraints.Active[reference];
            if (!joint.IsEnabled) return;
            if (prepare) joint.PrepareForIteration(ref joint, timeStep);
            else joint.Iterate(ref joint, timeStep);
        }
        else
        {
            ref SmallConstraintData joint = ref memSmallConstraints.Active[~reference];
            if (!joint.IsEnabled) return;
            if (prepare) joint.PrepareForIteration(ref joint, timeStep);
            else joint.Iterate(ref joint, timeStep);
        }
    }

    // The contact update of the step, run by the solver on the records it has just written while
    // they and their bodies are still in the worker's cache.
    private void UpdateSolvedContacts(Span<ContactData> records)
    {
        foreach (ref ContactData contact in records)
        {
            contact.UpdatePosition();

            if ((contact.UsageMask & ContactData.MaskContactAll) == 0)
                brokenArbiters.ConcurrentAdd(memContacts.GetHandle(ref contact));
        }
    }
}
