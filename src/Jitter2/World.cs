/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.UnmanagedMemory;

namespace Jitter2;

/// <summary>
/// Represents a simulation environment that holds and manages the state of all simulation objects.
/// </summary>
public sealed partial class World : IDisposable
{
    public enum ThreadModelType
    {
        Regular,
        Persistent
    }

    /// <summary>
    /// Provides access to objects in unmanaged memory. This operation is potentially unsafe.
    /// </summary>
    public readonly struct SpanData
    {
        private readonly World world;

        public SpanData(World world)
        {
            this.world = world;
        }

        /// <summary>
        /// Returns the total amount of unmanaged memory allocated in bytes.
        /// </summary>
        public long TotalBytesAllocated =>
            world.memRigidBodies.TotalBytesAllocated +
            world.memContacts.TotalBytesAllocated +
            world.memConstraints.TotalBytesAllocated +
            world.memSmallConstraints.TotalBytesAllocated;

        public readonly Span<RigidBodyData> ActiveRigidBodies => world.memRigidBodies.Active;
        public readonly Span<RigidBodyData> InactiveRigidBodies => world.memRigidBodies.Inactive;
        public readonly Span<RigidBodyData> RigidBodies => world.memRigidBodies.Elements;

        public readonly Span<ContactData> ActiveContacts => world.memContacts.Active;
        public readonly Span<ContactData> InactiveContacts => world.memContacts.Inactive;
        public readonly Span<ContactData> Contacts => world.memContacts.Elements;

        public readonly Span<ConstraintData> ActiveConstraints => world.memConstraints.Active;
        public readonly Span<ConstraintData> InactiveConstraints => world.memConstraints.Inactive;
        public readonly Span<ConstraintData> Constraints => world.memConstraints.Elements;

        public readonly Span<SmallConstraintData> ActiveSmallConstraints => world.memSmallConstraints.Active;
        public readonly Span<SmallConstraintData> InactiveSmallConstraints => world.memSmallConstraints.Inactive;
        public readonly Span<SmallConstraintData> SmallConstraints => world.memSmallConstraints.Elements;
    }

    private readonly UnmanagedActiveList<ContactData> memContacts;
    private readonly UnmanagedActiveList<RigidBodyData> memRigidBodies;
    private readonly UnmanagedActiveList<ConstraintData> memConstraints;
    private readonly UnmanagedActiveList<SmallConstraintData> memSmallConstraints;

    public delegate void WorldStep(Real dt);

    // Post- and Pre-step
    public event WorldStep? PreStep;
    public event WorldStep? PostStep;

    /// <summary>
    /// Grants access to objects residing in unmanaged memory. This operation can be potentially unsafe. Utilize
    /// the corresponding native properties where possible to mitigate risk.
    /// </summary>
    public SpanData RawData => new(this);

    private readonly ConcurrentDictionary<ArbiterKey, Arbiter> arbiters = new();

    private readonly ActiveList<Island> islands = new();
    private readonly ActiveList<RigidBody> bodies = new();

    private static ulong _idCounter;

    /// <summary>
    /// Generates a unique ID.
    /// </summary>
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
    public ReadOnlyActiveList<Island> Islands => new(islands);

    /// <summary>
    /// All rigid bodies in this world.
    /// </summary>
    public ReadOnlyActiveList<RigidBody> RigidBodies => new ReadOnlyActiveList<RigidBody>(bodies);

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
    /// Number of iterations (solver and relaxation) per substep (see <see cref="SubstepCount"/>).
    /// </summary>
    /// <remarks>Default value: (solver: 6, relaxation: 4)</remarks>
    /// <value></value>
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
    /// Substepping is deactivated when set to one.
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

    private JVector gravity = new(0, -(Real)9.81, 0);

    /// <summary>
    /// Default gravity, see also <see cref="RigidBody.AffectedByGravity"/>.
    /// </summary>
    public JVector Gravity
    {
        get => gravity;
        set => gravity = value;
    }

    // Make this global since it is used by nearly every method called
    // in World.Step.
    private volatile int solverIterations = 6;
    private volatile int velocityRelaxations = 4;
    private volatile int substeps = 1;

    private Real substep_dt = (Real)(1.0 / 100.0);
    private Real step_dt = (Real)(1.0 / 100.0);

    /// <summary>
    /// Creates an instance of the <see cref="World"/> class with the default capacity.
    /// This initializes the world using default values for the number of bodies, contacts,
    /// constraints, and small constraints as defined in <see cref="Capacity.Default"/>.
    /// </summary>
    /// <seealso cref="World(Capacity)"/>
    public World() : this(Capacity.Default) { }

    /// <summary>
    /// Creates an instance of the World class. As Jitter utilizes a distinct memory model, it is necessary to specify
    /// the capacity of the world in advance.
    /// </summary>
    public World(Capacity capacity)
    {
        memRigidBodies = new UnmanagedActiveList<RigidBodyData>(capacity.BodyCount);
        memContacts = new UnmanagedActiveList<ContactData>(capacity.ContactCount);
        memConstraints = new UnmanagedActiveList<ConstraintData>(capacity.ConstraintCount);
        memSmallConstraints = new UnmanagedActiveList<SmallConstraintData>(capacity.SmallConstraintCount);

        NullBody = CreateRigidBody();
        NullBody.IsStatic = true;

        DynamicTree = new DynamicTree(DefaultDynamicTreeFilter);
    }

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
    public void Remove(RigidBody body)
    {
        // No need to copy the hashset content first. Removing while iterating does not invalidate
        // the enumerator any longer, see https://github.com/dotnet/runtime/pull/37180
        // This comes in very handy for us.

        foreach (var constraint in body.constraints)
        {
            Remove(constraint);
        }

        foreach (var shape in body.Shapes)
        {
            DynamicTree.RemoveProxy(shape);
            shape.RigidBody = null!;
        }

        foreach (var contact in body.contacts)
        {
            Remove(contact);
        }

        if (body == NullBody) return;

        memRigidBodies.Free(body.handle);

        // we must be our own island..
        Debug.Assert(body.island is { bodies.Count: 1 });

        body.handle = JHandle<RigidBodyData>.Zero;

        IslandHelper.BodyRemoved(islands, body);

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
        arbiters.TryRemove(arbiter.Handle.Data.Key, out _);

        brokenArbiters.Remove(arbiter.Handle);
        memContacts.Free(arbiter.Handle);

        Arbiter.Pool.Push(arbiter);

        arbiter.Handle = JHandle<ContactData>.Zero;
    }

    internal void UpdateShape(RigidBodyShape shape)
    {
        shape.UpdateWorldBoundingBox();
        DynamicTree.Update(shape);
    }

    internal void ActivateBodyNextStep(RigidBody body)
    {
        body.sleepTime = 0;
        AddToActiveList(body.island);
    }

    internal void DeactivateBodyNextStep(RigidBody body)
    {
        body.sleepTime = Real.PositiveInfinity;
    }

    /// <summary>
    /// Constructs a constraint of the specified type. After creation, it is mandatory to initialize the constraint using the Constraint.Initialize method.
    /// </summary>
    /// <typeparam name="T">The specific type of constraint to create.</typeparam>
    /// <param name="body1">The first rigid body involved in the constraint.</param>
    /// <param name="body2">The second rigid body involved in the constraint.</param>
    /// <returns>A new instance of the specified constraint type.</returns>
    public T CreateConstraint<T>(RigidBody body1, RigidBody body2) where T : Constraint, new()
    {
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

        AddToActiveList(body1.island);
        AddToActiveList(body2.island);

        return constraint;
    }

    private void AddToActiveList(Island island)
    {
        island.MarkedAsActive = true;
        island.NeedsUpdate = true;
        islands.MoveToActive(island);
    }

    /// <summary>
    /// Creates and adds a new rigid body to the simulation world.
    /// </summary>
    /// <returns>A newly created instance of <see cref="RigidBody"/>.</returns>
    public RigidBody CreateRigidBody()
    {
        RigidBody body = new(memRigidBodies.Allocate(true, true), this);
        body.Data.IsActive = true;

        bodies.Add(body, true);

        IslandHelper.BodyAdded(islands, body);

        AddToActiveList(body.island);

        return body;
    }

    public void Dispose()
    {
        memContacts.Dispose();
        memRigidBodies.Dispose();
        memConstraints.Dispose();
        memSmallConstraints.Dispose();
    }
}