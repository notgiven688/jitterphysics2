/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

namespace Jitter2;

/// <summary>
/// Represents a simulation environment that holds and manages the state of all simulation objects.
/// </summary>
public sealed partial class World : IDisposable
{
    /// <summary>
    /// Controls how internal worker threads behave between calls to <see cref="Step(Real, bool)"/>.
    /// </summary>
    public enum ThreadModelType
    {
        /// <summary>
        /// Worker threads may yield when the engine is idle. Lower background CPU usage.
        /// </summary>
        Regular,

        /// <summary>
        /// Worker threads remain active between steps to reduce wake-up latency,
        /// at the cost of higher background CPU usage.
        /// </summary>
        Persistent
    }

    /// <summary>
    /// Provides access to objects in unmanaged memory. This operation is potentially unsafe.
    /// </summary>
    /// <remarks>
    /// The returned spans are backed by unmanaged memory and are only valid until the next
    /// world modification that may resize internal buffers (e.g., creating or removing bodies,
    /// constraints, contacts, or calling <see cref="Step"/>). Do not cache these spans.
    /// Not safe to use concurrently with <see cref="Step(Real, bool)"/>.
    /// </remarks>
    public readonly struct SpanData(World world)
    {
        /// <summary>
        /// Returns the total amount of unmanaged memory allocated in bytes.
        /// </summary>
        public long TotalBytesAllocated =>
            world.memRigidBodies.TotalBytesAllocated +
            world.memContacts.TotalBytesAllocated +
            world.memConstraints.TotalBytesAllocated +
            world.memSmallConstraints.TotalBytesAllocated;

        /// <summary>Span over active (awake) rigid body data.</summary>
        public Span<RigidBodyData> ActiveRigidBodies => world.memRigidBodies.Active;

        /// <summary>Span over inactive (sleeping) rigid body data.</summary>
        public Span<RigidBodyData> InactiveRigidBodies => world.memRigidBodies.Inactive;

        /// <summary>Span over all rigid body data (active and inactive).</summary>
        public Span<RigidBodyData> RigidBodies => world.memRigidBodies.Elements;

        /// <summary>Span over active contact data.</summary>
        public Span<ContactData> ActiveContacts => world.memContacts.Active;

        /// <summary>Span over inactive contact data.</summary>
        public Span<ContactData> InactiveContacts => world.memContacts.Inactive;

        /// <summary>Span over all contact data (active and inactive).</summary>
        public Span<ContactData> Contacts => world.memContacts.Elements;

        /// <summary>Span over active constraint data.</summary>
        public Span<ConstraintData> ActiveConstraints => world.memConstraints.Active;

        /// <summary>Span over inactive constraint data.</summary>
        public Span<ConstraintData> InactiveConstraints => world.memConstraints.Inactive;

        /// <summary>Span over all constraint data (active and inactive).</summary>
        public Span<ConstraintData> Constraints => world.memConstraints.Elements;

        /// <summary>Span over active small constraint data.</summary>
        public Span<SmallConstraintData> ActiveSmallConstraints => world.memSmallConstraints.Active;

        /// <summary>Span over inactive small constraint data.</summary>
        public Span<SmallConstraintData> InactiveSmallConstraints => world.memSmallConstraints.Inactive;

        /// <summary>Span over all small constraint data (active and inactive).</summary>
        public Span<SmallConstraintData> SmallConstraints => world.memSmallConstraints.Elements;
    }

    private readonly PartitionedBuffer<ContactData> memContacts;
    private readonly PartitionedBuffer<RigidBodyData> memRigidBodies;
    private readonly PartitionedBuffer<ConstraintData> memConstraints;
    private readonly PartitionedBuffer<SmallConstraintData> memSmallConstraints;

    /// <summary>
    /// Delegate for per-step and per-substep callbacks.
    /// </summary>
    /// <param name="dt">
    /// The duration in seconds: the full step duration for <see cref="PreStep"/>/<see cref="PostStep"/>,
    /// or the substep duration for <see cref="PreSubStep"/>/<see cref="PostSubStep"/>.
    /// </param>
    public delegate void WorldStep(Real dt);

    // Post- and Pre-step

    /// <summary>
    /// Raised at the beginning of a simulation step, before any collision detection,
    /// constraint solving, or integration is performed.
    /// </summary>
    /// <remarks>
    /// This event is invoked once per call to <see cref="Step"/> and receives the full
    /// step time <c>dt</c>. It can be used to apply external forces, modify bodies,
    /// or gather per-step diagnostics before the simulation advances.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event WorldStep? PreStep;

    /// <summary>
    /// Raised at the end of a simulation step, after all substeps, collision handling,
    /// and integration have completed.
    /// </summary>
    /// <remarks>
    /// This event is invoked once per call to <see cref="Step"/> and receives the full
    /// step time <c>dt</c>. At this point, all body states represent the final results
    /// of the step.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event WorldStep? PostStep;

    /// <summary>
    /// Raised at the beginning of each substep during a simulation step.
    /// </summary>
    /// <remarks>
    /// A simulation step may be divided into multiple substeps for stability.
    /// This event is invoked once per substep and receives the substep duration
    /// (<c>dt / substepCount</c>). It is called immediately before force integration
    /// and constraint solving for the substep.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event WorldStep? PreSubStep;

    /// <summary>
    /// Raised at the end of each substep during a simulation step.
    /// </summary>
    /// <remarks>
    /// This event is invoked once per substep and receives the substep duration.
    /// It is called after integration and constraint solving for the substep
    /// have completed.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event WorldStep? PostSubStep;

    /// <summary>
    /// Grants access to objects residing in unmanaged memory. This operation can be potentially unsafe. Use
    /// the corresponding managed properties where possible to mitigate risk.
    /// </summary>
    public SpanData RawData => new(this);

    private readonly ShardedDictionary<ArbiterKey, Arbiter> arbiters =
        new(Parallelization.ThreadPool.ThreadCountSuggestion);

    private readonly PartitionedSet<Island> islands = [];
    private readonly PartitionedSet<RigidBody> bodies = [];

    private static ulong _idCounter;

    /// <summary>
    /// Generates a unique ID.
    /// </summary>
    /// <returns>A monotonically increasing unique identifier.</returns>
    public static ulong RequestId()
    {
        return Interlocked.Increment(ref _idCounter);
    }

    /// <summary>
    /// Generates a range of unique IDs.
    /// </summary>
    /// <param name="count">The number of IDs to generate.</param>
    /// <returns>A tuple containing the minimum and maximum request IDs in the generated range. The upper
    /// bound is exclusive.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is less than 1.</exception>
    public static (ulong min, ulong max) RequestId(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater zero.");
        ulong count64 = (ulong)count;
        ulong max = Interlocked.Add(ref _idCounter, count64) + 1;
        return (max - count64, max);
    }

    /// <summary>
    /// Defines the two available thread models. The <see cref="ThreadModelType.Persistent"/> model keeps the worker
    /// threads active continuously, even when the <see cref="World.Step(Real, bool)"/> is not in operation, which might
    /// consume more CPU cycles and possibly affect the performance of other operations such as rendering. However, it ensures that the threads
    /// remain 'warm' for the next invocation of <see cref="World.Step(Real, bool)"/>. Conversely, the <see cref="ThreadModelType.Regular"/> model allows
    /// the worker threads to yield and undertake other tasks.
    /// </summary>
    public ThreadModelType ThreadModel { get; set; } = ThreadModelType.Regular;

    /// <summary>
    /// All collision islands in this world.
    /// </summary>
    public ReadOnlyPartitionedSet<Island> Islands => new(islands);

    /// <summary>
    /// All rigid bodies in this world.
    /// </summary>
    public ReadOnlyPartitionedSet<RigidBody> RigidBodies => new(bodies);

    /// <summary>
    /// Access to the <see cref="DynamicTree"/> instance. The instance
    /// should only be modified by Jitter.
    /// </summary>
    public DynamicTree DynamicTree { get; }

    /// <summary>
    /// A fixed body, pinned to the world. Can be used to create constraints with.
    /// </summary>
    public RigidBody NullBody { get; }

    /// <summary>
    /// Specifies whether the deactivation mechanism of Jitter is enabled.
    /// Does not activate inactive objects if set to false.
    /// </summary>
    public bool AllowDeactivation { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of iterations per substep for the constraint solver and velocity relaxation.
    /// </summary>
    /// <remarks>
    /// Higher solver iterations improve constraint accuracy at the cost of performance.
    /// Relaxation iterations help reduce velocity errors after solving.
    /// Default value: (solver: 6, relaxation: 4).
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown if <c>solver</c> is less than 1 or <c>relaxation</c> is negative.
    /// </exception>
    public (int solver, int relaxation) SolverIterations
    {
        get => (solverIterations, velocityRelaxations);
        set
        {
            if (value.solver < 1)
            {
                throw new ArgumentException("Solver iterations can not be smaller than one.",
                    nameof(SolverIterations));
            }

            if (value.relaxation < 0)
            {
                throw new ArgumentException("Relaxation iterations can not be smaller than zero.",
                    nameof(SolverIterations));
            }

            solverIterations = value.solver;
            velocityRelaxations = value.relaxation;
        }
    }

    /// <summary>
    /// The number of substeps for each call to <see cref="World.Step(Real, bool)"/>.
    /// Sub-stepping is deactivated when set to one.
    /// </summary>
    public int SubstepCount
    {
        get => substeps;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    "The number of substeps has to be larger than zero.");
            }

            substeps = value;
        }
    }

    /// <summary>
    /// Default gravity, see also <see cref="RigidBody.AffectedByGravity"/>.
    /// </summary>
    public JVector Gravity { get; set; } = new(0, -(Real)9.81, 0);

    // Make this global since it is used by nearly every method called
    // in World.Step.
    private int solverIterations = 6;
    private int velocityRelaxations = 4;
    private int substeps = 1;

    private Real stepDt = (Real)0.01;
    private Real substepDt = (Real)0.01;
    private Real invStepDt = (Real)100.0;

    /// <summary>
    /// Creates an instance of the World class.
    /// </summary>
    public World()
    {
        Logger.Information($"Creating new world.");

        memRigidBodies = new PartitionedBuffer<RigidBodyData>(aligned64: true);
        memContacts = new PartitionedBuffer<ContactData>();
        memConstraints = new PartitionedBuffer<ConstraintData>();
        memSmallConstraints = new PartitionedBuffer<SmallConstraintData>();

        NullBody = CreateRigidBody();
        NullBody.MotionType = MotionType.Static;

        DynamicTree = new DynamicTree(DefaultDynamicTreeFilter);

        InitParallelCallbacks();
    }

    /// <summary>
    /// Default filter function for the DynamicTree. Returns true if both proxies are of type RigidBodyShape
    /// and belong to different RigidBody instances.
    /// </summary>
    public static bool DefaultDynamicTreeFilter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        if (proxyA is RigidBodyShape rbsA && proxyB is RigidBodyShape rbsB)
        {
            return rbsA.RigidBody != rbsB.RigidBody;
        }

        return true;
    }

    /// <summary>
    /// Removes all entities from the simulation world.
    /// </summary>
    public void Clear()
    {
        // create a copy, since we are going to modify the list
        Stack<RigidBody> bodyStack = new(bodies);
        while (bodyStack.Count > 0) Remove(bodyStack.Pop());

        // Left-over shapes not associated with a rigid body.
        Stack<IDynamicTreeProxy> proxies = new(DynamicTree.Proxies);
        while (proxies.Count > 0) DynamicTree.RemoveProxy(proxies.Pop());
    }

    /// <summary>
    /// Removes the specified body from the world. This operation also automatically discards any associated contacts
    /// and constraints.
    /// </summary>
    /// <param name="body">The rigid body to remove.</param>
    public void Remove(RigidBody body)
    {
        if (body == NullBody) return;

        // No need to copy the hashset content first. Removing while iterating does not invalidate
        // the enumerator any longer, see https://github.com/dotnet/runtime/pull/37180
        // This comes in very handy for us.

        foreach (var constraint in body.InternalConstraints)
        {
            Remove(constraint);
        }

        foreach (var shape in body.Shapes)
        {
            DynamicTree.RemoveProxy(shape);
            shape.RigidBody = null!;
        }

        foreach (var contact in body.InternalContacts)
        {
            Remove(contact);
        }

        memRigidBodies.Free(body.Handle);

        // We must be our own island.
        Debug.Assert(body.InternalIsland is { InternalBodies.Count: 1 });

        body.Handle = JHandle<RigidBodyData>.Zero;

        IslandHelper.BodyRemoved(islands, body);

        body.InternalIsland = null!;

        bodies.Remove(body);
    }

    /// <summary>
    /// Removes a specific constraint from the world. For temporary deactivation of constraints, consider using the
    /// <see cref="Constraint.IsEnabled"/> property.
    /// </summary>
    /// <param name="constraint">The constraint to be removed.</param>
    public void Remove(Constraint constraint)
    {
        ActivateBodyNextStep(constraint.Body1);
        ActivateBodyNextStep(constraint.Body2);

        IslandHelper.ConstraintRemoved(islands, constraint);

        if (constraint.IsSmallConstraint)
        {
            memSmallConstraints.Free(constraint.SmallHandle);
        }
        else
        {
            memConstraints.Free(constraint.Handle);
        }

        constraint.Handle = JHandle<ConstraintData>.Zero;
    }

    /// <summary>
    /// Removes a particular arbiter from the world.
    /// </summary>
    public void Remove(Arbiter arbiter)
    {
        ActivateBodyNextStep(arbiter.Body1);
        ActivateBodyNextStep(arbiter.Body2);

        IslandHelper.ArbiterRemoved(islands, arbiter);
        arbiters.Remove(arbiter.Handle.Data.Key);

        brokenArbiters.Remove(arbiter.Handle);
        memContacts.Free(arbiter.Handle);

        arbiter.Handle = JHandle<ContactData>.Zero;
        arbiter.Body1 = null!;
        arbiter.Body2 = null!;

        Arbiter.Pool.Push(arbiter);
    }

    internal void ActivateBodyNextStep(RigidBody body)
    {
        body.InternalSleepTime = 0;

        if (body.IsActive) return;

        AddToActiveList(body.InternalIsland);

        if (body.MotionType == MotionType.Static)
        {
            foreach (var c in body.Constraints)
            {
                ActivateBodyNextStep(c.Body1 == body ? c.Body2 : c.Body1);
            }

            foreach (var c in body.Contacts)
            {
                ActivateBodyNextStep(c.Body1 == body ? c.Body2 : c.Body1);
            }
        }

        body.Island.NeedsUpdate = true;
    }

    internal void RemoveStaticStaticConstraints(RigidBody body)
    {
        foreach (var constraint in body.InternalConstraints)
        {
            if (constraint.Body1.Data.MotionType != MotionType.Dynamic &&
                constraint.Body2.Data.MotionType != MotionType.Dynamic)
            {
                Remove(constraint);
            }
        }

        foreach (var arbiter in body.InternalContacts)
        {
            if (arbiter.Body1.Data.MotionType != MotionType.Dynamic &&
                arbiter.Body2.Data.MotionType != MotionType.Dynamic)
            {
                Remove(arbiter);
            }
        }
    }

    internal void BuildConnectionsFromExistingContacts(RigidBody body)
    {
        foreach (var constraint in body.InternalConstraints)
        {
            IslandHelper.AddConnection(islands, constraint.Body1, constraint.Body2);
        }

        foreach (var contact in body.InternalContacts)
        {
            IslandHelper.AddConnection(islands, contact.Body1, contact.Body2);
        }
    }

    internal void RemoveConnections(RigidBody body)
    {
        if (body.InternalConnections.Count > 0)
        {
            int count = body.InternalConnections.Count;
            var connections = ArrayPool<RigidBody>.Shared.Rent(count);

            try
            {
                body.InternalConnections.CopyTo(connections);

                for (int i = 0; i < count; i++)
                {
                    IslandHelper.RemoveConnection(islands, body, connections[i]);
                }
            }
            finally
            {
                ArrayPool<RigidBody>.Shared.Return(connections, clearArray: true);
            }
        }

        Debug.Assert(body.InternalConnections.Count == 0);
        Debug.Assert(body.InternalIsland.InternalBodies.Count == 1);
    }

    internal void DeactivateBodyNextStep(RigidBody body)
    {
        body.InternalSleepTime = Real.PositiveInfinity;
    }

    /// <summary>
    /// Constructs a constraint of the specified type. After creation, initialize the constraint
    /// by calling its <c>Initialize</c> method.
    /// </summary>
    /// <typeparam name="T">The specific type of constraint to create.</typeparam>
    /// <param name="body1">The first rigid body involved in the constraint.</param>
    /// <param name="body2">The second rigid body involved in the constraint.</param>
    /// <returns>A new instance of the specified constraint type, already registered with the world.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="body1"/> and <paramref name="body2"/> are the same.</exception>
    /// <exception cref="PartitionedBuffer{T}.MaximumSizeException">Raised when the maximum size limit is exceeded.</exception>
    public T CreateConstraint<T>(RigidBody body1, RigidBody body2) where T : Constraint, new()
    {
        if (ReferenceEquals(body1, body2))
            throw new ArgumentException($"{nameof(body1)} and {nameof(body2)} must be different.");

        T constraint = new();

        if (constraint.IsSmallConstraint)
        {
            constraint.Create(memSmallConstraints.Allocate(true, true), body1, body2);
        }
        else
        {
            constraint.Create(memConstraints.Allocate(true, true), body1, body2);
        }

        IslandHelper.ConstraintCreated(islands, constraint);

        AddToActiveList(body1.InternalIsland);
        AddToActiveList(body2.InternalIsland);

        return constraint;
    }

    private void AddToActiveList(Island island)
    {
        island.MarkedAsActive = true;
        islands.MoveToActive(island);
    }

    /// <summary>
    /// Creates and adds a new rigid body to the simulation world.
    /// </summary>
    /// <returns>A newly created instance of <see cref="RigidBody"/>.</returns>
    /// <exception cref="PartitionedBuffer{T}.MaximumSizeException">Raised when the maximum size limit is exceeded.</exception>
    public RigidBody CreateRigidBody()
    {
        RigidBody body = new(memRigidBodies.Allocate(true, true), this);
        body.Data.IsActive = true;

        bodies.Add(body, true);

        IslandHelper.BodyAdded(islands, body);

        AddToActiveList(body.InternalIsland);

        return body;
    }

    /// <summary>
    /// Releases all unmanaged memory buffers used by this simulation world.
    /// </summary>
    /// <remarks>
    /// After disposal, the world instance is unusable. All bodies, constraints, and contacts
    /// become invalid.
    /// </remarks>
    public void Dispose()
    {
        memContacts.Dispose();
        memRigidBodies.Dispose();
        memConstraints.Dispose();
        memSmallConstraints.Dispose();

        deferredContacts.Dispose();
        deferredConstraints.Dispose();
        deferredSmallConstraints.Dispose();
    }
}