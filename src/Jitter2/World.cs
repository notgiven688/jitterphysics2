/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
/// Thrown when two body arguments are required to reference different bodies.
/// </summary>
public sealed class SameBodyException : ArgumentException
{
    private const string DefaultMessage = "Body arguments must be different.";
    private const string DefaultParamName = "body2";

    public SameBodyException()
        : base(DefaultMessage, DefaultParamName)
    {
    }

    public SameBodyException(string message)
        : base(message)
    {
    }

    public SameBodyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SameBodyException(string message, string paramName)
        : base(message, paramName)
    {
    }

    public SameBodyException(string message, string paramName, Exception innerException)
        : base(message, paramName, innerException)
    {
    }
}

/// <summary>
/// Contains the bodies, contacts, and constraints of a physics simulation.
/// </summary>
/// <remarks>
/// Do not step or modify the same world from multiple threads at once. Different worlds can
/// run concurrently with <c>multiThread: false</c>. Multithreaded steps share a worker pool
/// and wait for each other, even when they belong to different worlds.
/// </remarks>
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
    /// Provides direct access to body, contact, and constraint data stored in unmanaged memory.
    /// </summary>
    /// <remarks>
    /// Do not retain spans across calls to <see cref="Step(Real, bool)"/> or changes to bodies,
    /// contacts, or constraints: these operations can move or resize the underlying storage.
    /// Do not access the spans while the world is being stepped.
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
    /// <param name="timeStep">The full step and substep timing information.</param>
    public delegate void WorldStep(TimeStep timeStep);

    // Step callbacks.

    /// <summary>
    /// Raised at the beginning of a simulation step, before any collision detection,
    /// constraint solving, or integration is performed.
    /// </summary>
    /// <remarks>
    /// Use this event to apply forces or update bodies before the simulation advances.
    /// It runs once per call to <see cref="Step"/>, on the calling thread.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event WorldStep? PreStep;

    /// <summary>
    /// Raised at the end of a simulation step, after all substeps, collision handling,
    /// and integration have completed.
    /// </summary>
    /// <remarks>
    /// Body states contain the completed step's results. This event runs once per call
    /// to <see cref="Step"/>, on the calling thread.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event WorldStep? PostStep;

    /// <summary>
    /// Raised at the beginning of each substep during a simulation step.
    /// </summary>
    /// <remarks>
    /// Runs on the calling thread, before forces are applied and constraints are solved.
    /// Do not add or remove bodies, shapes, constraints, or contacts, or change body motion types
    /// in this callback.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event WorldStep? PreSubStep;

    /// <summary>
    /// Raised at the end of each substep during a simulation step.
    /// </summary>
    /// <remarks>
    /// Runs on the calling thread, after bodies have moved and constraints have been solved.
    /// Do not add or remove bodies, shapes, constraints, or contacts, or change body motion types
    /// in this callback.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event WorldStep? PostSubStep;

    /// <summary>
    /// Raised when a group of connected bodies wakes up.
    /// </summary>
    /// <remarks>
    /// The bodies are already awake when this callback runs. Islands can split, merge, or be
    /// reused, so copy any data you need instead of retaining the island reference.
    /// Do not add or remove bodies, shapes, constraints, or contacts, or change body motion types
    /// in this callback.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event Action<Island>? IslandActivated;

    /// <summary>
    /// Raised when a group of connected bodies goes to sleep.
    /// </summary>
    /// <remarks>
    /// The bodies are already asleep when this callback runs. Islands can split, merge, or be
    /// reused, so copy any data you need instead of retaining the island reference.
    /// Do not add or remove bodies, shapes, constraints, or contacts, or change body motion types
    /// in this callback.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event Action<Island>? IslandDeactivated;

    /// <summary>
    /// Raised after a constraint has been permanently removed from the world.
    /// </summary>
    /// <remarks>
    /// The event is raised for explicit removal as well as automatic removal caused by removing a
    /// body or clearing the world. It is not raised when a constraint is disabled, when a body's
    /// motion type changes, or when the world is disposed.
    /// The constraint is invalid when the callback runs, but its <see cref="Constraint.Body1"/> and
    /// <see cref="Constraint.Body2"/> references remain available.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
    public event Action<Constraint>? ConstraintRemoved;

    /// <summary>
    /// Provides direct access to the world's unmanaged simulation data. Prefer the properties on
    /// <see cref="RigidBody"/>, <see cref="Constraint"/>, and <see cref="Arbiter"/> for normal use.
    /// </summary>
    /// <remarks>
    /// Spans must not be retained across world changes or accessed while the world is being stepped.
    /// See <see cref="SpanData"/> for lifetime restrictions.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if this world has been disposed.</exception>
    public SpanData RawData
    {
        get
        {
            ThrowIfDisposed();
            return new SpanData(this);
        }
    }

    private readonly ShardedDictionary<ArbiterKey, Arbiter> arbiters =
        new(Parallelization.ThreadPool.ThreadCountSuggestion);

    // Accessed under lock (memContacts) during creation/rollback. Removal is exclusive:
    // either during Step after collision workers finish, or between simulation steps.
    private readonly Stack<Arbiter> arbiterPool = [];

    private readonly PartitionedSet<Island> islands = [];
    private readonly PartitionedSet<RigidBody> bodies = [];
    private readonly Stack<Island> islandPool = [];

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
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        ulong count64 = (ulong)count;
        ulong max = Interlocked.Add(ref _idCounter, count64) + 1;
        return (max - count64, max);
    }

    /// <summary>
    /// Gets or sets the threading model used between calls to <see cref="Step(Real, bool)"/>.
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
    /// Access to the <see cref="DynamicTree"/> instance. Callers may manage their own proxies and
    /// update shapes after changing their geometry. Jitter manages proxies for attached rigid body shapes.
    /// </summary>
    public DynamicTree DynamicTree { get; }

    /// <summary>
    /// A static body to use as one endpoint when attaching a constraint to the world.
    /// </summary>
    public RigidBody NullBody { get; }

    /// <summary>
    /// Whether bodies can go to sleep when they stop moving. Default is true.
    /// Setting this to false does not wake bodies that are already asleep.
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <c>solver</c> is less than 1 or <c>relaxation</c> is negative.
    /// </exception>
    public (int solver, int relaxation) SolverIterations
    {
        get => (solverIterations, velocityRelaxations);
        set
        {
            if (value.solver < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value.solver), value.solver,
                    "Solver iterations cannot be smaller than one.");
            }

            if (value.relaxation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value.relaxation), value.relaxation,
                    "Relaxation iterations cannot be smaller than zero.");
            }

            solverIterations = value.solver;
            velocityRelaxations = value.relaxation;
        }
    }

    /// <summary>
    /// The number of substeps for each call to <see cref="World.Step(Real, bool)"/>.
    /// Sub-stepping is deactivated when set to one.
    /// Default value: 1.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is less than one.
    /// </exception>
    public int SubstepCount
    {
        get => substeps;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    "The number of substeps must be greater than zero.");
            }

            substeps = value;
        }
    }

    /// <summary>
    /// Default gravity, see also <see cref="RigidBody.AffectedByGravity"/>.
    /// Default value: (0, -9.81, 0).
    /// </summary>
    public JVector Gravity
    {
        get => gravity;
        set
        {
            DebugCheck.IsFinite(value, nameof(value));
            gravity = value;
        }
    }

    // Make this global since it is used by nearly every method called
    // in World.Step.
    private JVector gravity = new(0, -(Real)9.81, 0);
    private int solverIterations = 6;
    private int velocityRelaxations = 4;
    private int substeps = 1;

    private TimeStep timeStep = new((Real)0.01);
    // The dt used by the last broadphase update; before the first Step, allow a conservative sweep.
    private Real broadphaseStepDt = (Real)0.1;

    internal Real BroadphaseStepDt => broadphaseStepDt;

    /// <summary>
    /// Creates an instance of the World class.
    /// </summary>
    public World()
    {
        Debug.Assert(Unsafe.SizeOf<RigidBodyData>() == SolverPrefetchCacheLineSize * (Precision.IsDoublePrecision ? 4 : 2),
            "RigidBodyData size should match the cache lines prefetched by the solver.");

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
    /// Default filter function for the DynamicTree. Rejects two <see cref="RigidBodyShape"/> proxies
    /// attached to the same body; accepts other proxy pairs.
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
    /// Removes all entities from the simulation world. Also clears all proxies from the dynamic tree,
    /// including any user-added proxies not owned by the world.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();

        // create a copy, since we are going to modify the list
        Stack<RigidBody> bodyStack = new(bodies);
        while (bodyStack.Count > 0) Remove(bodyStack.Pop());

        if (NullBody.Shapes.Count > 0)
        {
            NullBody.RemoveShapes(NullBody.Shapes, MassInertiaUpdateMode.Preserve);
        }

        // Left-over shapes not associated with a rigid body.
        Stack<IDynamicTreeProxy> proxies = new(DynamicTree.Proxies);
        while (proxies.Count > 0) DynamicTree.RemoveProxy(proxies.Pop());

    }

    /// <summary>
    /// Reduces excess capacity in internal world storage.
    /// </summary>
    /// <remarks>
    /// Use this after removing many objects to release unused collection capacity.
    /// Existing bodies, constraints, contacts, and arbiters remain valid.
    /// Unmanaged simulation buffers are not compacted.
    /// <para>Do not use or modify this world from other threads while trimming, including queries
    /// through <see cref="DynamicTree"/>.</para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if this world has been disposed.</exception>
    public void Trim()
    {
        ThrowIfDisposed();

        bodies.Trim();
        islands.Trim();
        ccdBodies.TrimExcess();
        ccdBodySet.TrimExcess();
        ccdQueries.Clear();
        ccdQueries.TrimExcess();

        deferredArbiters.Trim();
        brokenArbiters.Trim();
        dispatchArbiters.TrimExcess();
        retiredArbiters.TrimExcess();

        arbiters.TrimExcess();

        foreach (Island island in islandPool)
        {
            island.TrimLists();
        }

        islandPool.TrimExcess();

        DynamicTree.Trim();

        handleToIsland.TrimExcess();
        islandRanges.TrimExcess();
        sortedContacts.TrimExcess();
        sortedSmallConstraints.TrimExcess();
        sortedConstraints.TrimExcess();

        foreach (RigidBody body in bodies)
        {
            body.TrimLists();
        }

        foreach (Island island in islands)
        {
            island.TrimLists();
        }
    }

    /// <summary>
    /// Removes the specified body from the world. This operation also automatically discards any associated contacts
    /// and constraints.
    /// </summary>
    /// <param name="body">The rigid body to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="body"/> does not belong to this world.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <paramref name="body"/> has already been removed.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if this world has been disposed.</exception>
    public void Remove(RigidBody body)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(body);

        if (body.World != this)
            throw new ArgumentException("The body does not belong to this world.", nameof(body));

        if (body == NullBody) return;

        if (!body.IsValid)
            throw new InvalidOperationException("The body has already been removed from this world.");

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

        for (int i = deferredArbiters.Count - 1; i >= 0; i--)
        {
            Arbiter arbiter = deferredArbiters[i];
            if (arbiter.Body1 == body || arbiter.Body2 == body) Remove(arbiter);
        }

        foreach (var contact in body.InternalContacts)
        {
            Remove(contact);
        }

        RemoveCcdBody(body);
        memRigidBodies.Free(body.Handle);

        // We must be our own island.
        Debug.Assert(body.InternalIsland is { InternalBodies.Count: 1 });

        body.Handle = JHandle<RigidBodyData>.Zero;

        IslandHelper.BodyRemoved(islands, islandPool, body);

        body.InternalIsland = null!;

        bodies.Remove(body);
    }

    /// <summary>
    /// Removes a specific constraint from the world. For temporary deactivation of constraints, consider using the
    /// <see cref="Constraint.IsEnabled"/> property.
    /// </summary>
    /// <param name="constraint">The constraint to be removed.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="constraint"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="constraint"/> does not belong to this world.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown if the constraint has already been removed.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if this world has been disposed.</exception>
    public void Remove(Constraint constraint)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(constraint);

        if (constraint.Body1.World != this)
            throw new ArgumentException("The constraint does not belong to this world.", nameof(constraint));

        if (!constraint.IsValid)
            throw new InvalidOperationException("The constraint has already been removed from this world.");

        ActivateBodyNextStep(constraint.Body1);
        ActivateBodyNextStep(constraint.Body2);

        IslandHelper.ConstraintRemoved(islands, islandPool, constraint);

        if (constraint.IsSmallConstraint)
        {
            memSmallConstraints.Free(constraint.SmallHandle);
        }
        else
        {
            memConstraints.Free(constraint.Handle);
        }

        constraint.Handle = JHandle<ConstraintData>.Zero;
        ConstraintRemoved?.Invoke(constraint);
    }

    /// <summary>
    /// Removes a particular arbiter from the world.
    /// </summary>
    /// <param name="arbiter">The arbiter to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="arbiter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="arbiter"/> does not belong to this world.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown if this world has been disposed.</exception>
    public void Remove(Arbiter arbiter)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(arbiter);

        if (arbiter.Body1.World != this)
            throw new ArgumentException("The arbiter does not belong to this world.", nameof(arbiter));

        ActivateBodyNextStep(arbiter.Body1);
        ActivateBodyNextStep(arbiter.Body2);

        bool pending = false;
        for (int i = 0; i < deferredArbiters.Count; i++)
        {
            if (deferredArbiters[i] != arbiter) continue;
            deferredArbiters.RemoveAt(i);
            pending = true;
            break;
        }

        if (!pending) IslandHelper.ArbiterRemoved(islands, islandPool, arbiter);
        arbiters.Remove(arbiter.Handle.Data.Key);

        brokenArbiters.Remove(arbiter.Handle);
        memContacts.Free(arbiter.Handle);

        ReturnArbiter(arbiter);
    }

    internal void RemoveDeferredArbitersForShape(RigidBody body, ulong shapeId)
    {
        for (int i = deferredArbiters.Count - 1; i >= 0; i--)
        {
            Arbiter arbiter = deferredArbiters[i];
            if (arbiter.Body1 != body && arbiter.Body2 != body) continue;

            ArbiterKey key = arbiter.Handle.Data.Key;
            if (key.Key1 == shapeId || key.Key2 == shapeId) Remove(arbiter);
        }
    }

    /// <summary>
    /// Marks a body to be activated at the start of the next step.
    /// </summary>
    /// <param name="body">The body to activate.</param>
    /// <param name="wakeUpStatic">Set to true when the intention is to move a static
    /// body or to switch from another MotionType to static. This then activates all connected bodies.</param>
    internal void ActivateBodyNextStep(RigidBody body, bool wakeUpStatic = false)
    {
        body.InternalSleepTime = 0;

        if (body.IsActive) return;

        if (body.MotionType == MotionType.Static && !wakeUpStatic) return;

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

    /// <summary>
    /// Removes contacts that connect two non-dynamic bodies.
    /// </summary>
    /// <param name="body">The body whose non-dynamic contacts should be removed.</param>
    internal void RemoveStaticStaticContacts(RigidBody body)
    {
        foreach (var arbiter in body.InternalContacts)
        {
            if (arbiter.Body1.Data.MotionType != MotionType.Dynamic &&
                arbiter.Body2.Data.MotionType != MotionType.Dynamic)
            {
                Remove(arbiter);
            }
        }
    }

    /// <summary>
    /// Keeps a constraint in the solver's active partition only while a dynamic endpoint is awake.
    /// This does not change the constraint's enabled state or its island connections.
    /// </summary>
    internal void UpdateConstraintActivation(Constraint constraint)
    {
        RigidBody body1 = constraint.Body1;
        RigidBody body2 = constraint.Body2;
        bool active = (body1.MotionType == MotionType.Dynamic && body1.IsActive) ||
                      (body2.MotionType == MotionType.Dynamic && body2.IsActive);

        if (constraint.IsSmallConstraint)
        {
            if (active) memSmallConstraints.MoveToActive(constraint.SmallHandle);
            else memSmallConstraints.MoveToInactive(constraint.SmallHandle);
        }
        else
        {
            if (active) memConstraints.MoveToActive(constraint.Handle);
            else memConstraints.MoveToInactive(constraint.Handle);
        }
    }

    /// <summary>
    /// Rebuilds island connections from the body's existing contacts and constraints.
    /// </summary>
    /// <param name="body">The body whose connections should be rebuilt.</param>
    internal void BuildConnectionsFromExistingContacts(RigidBody body)
    {
        foreach (var constraint in body.InternalConstraints)
        {
            IslandHelper.AddConnection(islands, islandPool, constraint.Body1, constraint.Body2);
        }

        foreach (var contact in body.InternalContacts)
        {
            IslandHelper.AddConnection(islands, islandPool, contact.Body1, contact.Body2);
        }
    }

    /// <summary>
    /// Removes all island connections for a body.
    /// </summary>
    /// <param name="body">The body whose connections should be removed.</param>
    internal void RemoveConnections(RigidBody body)
    {
        while (body.InternalConnections.Count > 0)
        {
            IslandHelper.RemoveConnection(islands, islandPool, body, body.InternalConnections[^1]);
        }

        Debug.Assert(body.InternalConnections.Count == 0);
        Debug.Assert(body.InternalIsland.InternalBodies.Count == 1);
    }

    /// <summary>
    /// Marks a body to be deactivated at the start of the next step.
    /// </summary>
    /// <param name="body">The body to deactivate.</param>
    internal void DeactivateBodyNextStep(RigidBody body)
    {
        body.InternalSleepTime = Real.PositiveInfinity;
    }

    /// <summary>
    /// Immediately puts a group of connected bodies to sleep.
    /// </summary>
    /// <remarks>
    /// Works even when <see cref="AllowDeactivation"/> is false. Clears velocities and pending
    /// forces and torques on the island's dynamic and kinematic bodies.
    /// </remarks>
    /// <param name="island">The island to deactivate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="island"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="island"/> does not belong to this world.</exception>
    public void ForceSleepIsland(Island island)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(island);

        if (!islands.Contains(island))
        {
            throw new ArgumentException("The island does not belong to this world.", nameof(island));
        }

        foreach (RigidBody body in island.InternalBodies)
        {
            ClearMotionForSleep(body);
        }

        island.MarkedAsActive = false;
        island.NeedsUpdate = false;

        if (!islands.IsActive(island)) return;

        bool deactivatedBody = false;

        foreach (RigidBody body in island.InternalBodies)
        {
            deactivatedBody |= DeactivateBodyForSleep(body);
        }

        islands.MoveToInactive(island);

        if (deactivatedBody)
        {
            IslandDeactivated?.Invoke(island);
        }
    }

    /// <summary>
    /// Constructs a constraint of the specified type. After creation, initialize the constraint
    /// by calling its <c>Initialize</c> method.
    /// </summary>
    /// <typeparam name="T">The specific type of constraint to create.</typeparam>
    /// <param name="body1">The first rigid body involved in the constraint.</param>
    /// <param name="body2">The second rigid body involved in the constraint.</param>
    /// <returns>A new instance of the specified constraint type, already registered with the world.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown if either body does not belong to this world.
    /// </exception>
    /// <exception cref="SameBodyException">
    /// Thrown if <paramref name="body1"/> and <paramref name="body2"/> are the same.
    /// </exception>
    /// <exception cref="PartitionedBuffer{T}.MaximumSizeException">Raised when the constraint buffer maximum size is exceeded.</exception>
    public T CreateConstraint<T>(RigidBody body1, RigidBody body2) where T : Constraint, new()
    {
        ThrowIfDisposed();

        if (body1.World != this)
            throw new ArgumentException("The body does not belong to this world.", nameof(body1));
        if (body2.World != this)
            throw new ArgumentException("The body does not belong to this world.", nameof(body2));
        if (!body1.IsValid || !body2.IsValid)
            throw new InvalidOperationException("The body has been removed from this world.");
        if (ReferenceEquals(body1, body2))
            throw new SameBodyException();

        T constraint = new();
        ulong constraintId = RequestId();

        if (constraint.IsSmallConstraint)
        {
            constraint.Create(memSmallConstraints.Allocate(false, true), body1, body2);
            constraint.SmallHandle.Data.ConstraintId = constraintId;
        }
        else
        {
            constraint.Create(memConstraints.Allocate(false, true), body1, body2);
            constraint.Handle.Data.ConstraintId = constraintId;
        }

        IslandHelper.ConstraintCreated(islands, islandPool, constraint);

        AddToActiveList(body1.InternalIsland);
        AddToActiveList(body2.InternalIsland);

        UpdateConstraintActivation(constraint);

        return constraint;
    }

    /// <summary>
    /// Marks an island as active and moves it to the active partition.
    /// </summary>
    /// <param name="island">The island to activate.</param>
    private void AddToActiveList(Island island)
    {
        bool wasActive = islands.IsActive(island);
        island.MarkedAsActive = true;
        if (!wasActive) island.NeedsUpdate = true;

        islands.MoveToActive(island);
    }

    [Conditional("DEBUG")]
    private void AssertIslandActivationInvariant(Island island)
    {
        if (island.NeedsUpdate) return;

        bool shouldBeActive = islands.IsActive(island);

        foreach (RigidBody body in island.InternalBodies)
        {
            if (body.Data.MotionType == MotionType.Static) continue;

            Debug.Assert(body.Data.IsActive == shouldBeActive,
                "Islands without pending updates must not contain mixed dynamic body activation state.");
        }
    }

    [Conditional("DEBUG")]
    private void AssertIslandActivationInvariants()
    {
        for (int i = 0; i < islands.Count; i++)
        {
            AssertIslandActivationInvariant(islands[i]);
        }
    }

    /// <summary>
    /// Creates and adds a new rigid body to the simulation world.
    /// </summary>
    /// <returns>A newly created instance of <see cref="RigidBody"/>.</returns>
    /// <exception cref="PartitionedBuffer{T}.MaximumSizeException">Raised when the rigid-body buffer maximum size is exceeded.</exception>
    public RigidBody CreateRigidBody()
    {
        ThrowIfDisposed();
        RigidBody body = new(memRigidBodies.Allocate(true, true), this);
        body.Data.IsActive = true;

        bodies.Add(body, true);

        IslandHelper.BodyAdded(islands, islandPool, body);

        AddToActiveList(body.InternalIsland);

        return body;
    }

    private bool disposed;

    /// <summary>
    /// Gets a value indicating whether this world has been disposed.
    /// </summary>
    public bool IsDisposed => disposed;

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    /// <summary>
    /// Releases all unmanaged memory buffers used by this simulation world.
    /// </summary>
    /// <remarks>
    /// After disposal, the world instance is unusable. All bodies, constraints, and contacts
    /// become invalid. Calling <see cref="Dispose"/> multiple times is safe.
    /// </remarks>
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        ccdBodies.Clear();
        ccdBodySet.Clear();
        ccdQueries.Clear();

        memContacts.Dispose();
        memRigidBodies.Dispose();
        memConstraints.Dispose();
        memSmallConstraints.Dispose();

        deferredContacts.Dispose();
        deferredConstraints.Dispose();
        deferredSmallConstraints.Dispose();
    }
}
