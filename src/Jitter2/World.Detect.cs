/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

namespace Jitter2;

public sealed partial class World
{
    /// <summary>
    /// Thrown when the narrow phase encounters a pair of proxy types it cannot process.
    /// </summary>
    /// <remarks>
    /// This typically indicates that non-<see cref="RigidBodyShape"/> proxies were inserted into the
    /// world's <see cref="DynamicTree"/>. Use <see cref="BroadPhaseFilter"/> to filter such pairs,
    /// or ensure only supported proxy types are added.
    /// </remarks>
    public class InvalidCollisionTypeException : Exception
    {
        private static string CreateMessage(Type proxyA, Type proxyB) =>
            $"Don't know how to handle collision between {proxyA} and {proxyB}." +
            " Register a BroadPhaseFilter to handle and/or filter out these collision types.";

        public InvalidCollisionTypeException()
        {
        }

        public InvalidCollisionTypeException(string message)
            : base(message)
        {
        }

        public InvalidCollisionTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public InvalidCollisionTypeException(Type proxyA, Type proxyB)
            : base(CreateMessage(proxyA, proxyB))
        {
            ProxyA = proxyA;
            ProxyB = proxyB;
        }

        public Type? ProxyA { get; }

        public Type? ProxyB { get; }
    }

    /// <summary>
    /// Hook into the narrow-phase collision detection pipeline.
    /// The default instance is of type <see cref="TriangleEdgeCollisionFilter"/>.
    /// </summary>
    /// <remarks>
    /// Use this to intercept collisions after contact generation, modify contact data,
    /// or implement custom collision responses. When <see cref="Step(Real, bool)"/> is called
    /// with <c>multiThread=true</c>, this may be invoked concurrently. Implementations must be thread-safe.
    /// </remarks>
    public INarrowPhaseFilter? NarrowPhaseFilter { get; set; } = new TriangleEdgeCollisionFilter();

    /// <summary>
    /// Hook into the broadphase collision detection pipeline. The default value is null.
    /// </summary>
    /// <remarks>
    /// Use this to intercept shape pairs before narrow-phase detection, implement custom collision layers,
    /// or handle collisions for custom proxy types. When <see cref="Step(Real, bool)"/> is called
    /// with <c>multiThread=true</c>, this may be invoked concurrently. Implementations must be thread-safe.
    /// </remarks>
    public IBroadPhaseFilter? BroadPhaseFilter { get; set; }

    /// <summary>
    /// Enables the generation of additional contacts for flat surfaces that are in contact.
    /// Traditionally, the collision system reports the deepest collision point between two objects.
    /// A full contact manifold is then generated over several time steps using contact caching, which
    /// can be unstable. This method attempts to build a fuller or complete contact manifold within a single time step.
    /// </summary>
    public bool EnableAuxiliaryContactPoints { set; get; } = true;

    /// <summary>
    /// When enabled (the default), contact points and their accumulated impulses are cached between
    /// frames. This allows the solver to warm-start from the previous solution and enables the
    /// manifold to grow over several steps, which improves stability for resting contacts.
    /// When disabled, all contact data is discarded at the end of each frame and every contact is
    /// treated as brand-new. Disabling this removes all frame-to-frame contact memory at the cost
    /// of solver convergence speed.
    /// </summary>
    public bool PersistentContactManifold { get; set; } = true;

    /// <summary>
    /// A speculative contact slows a body down such that it does not penetrate or tunnel through
    /// an obstacle within one frame. The <see cref="SpeculativeRelaxationFactor"/> scales the
    /// slowdown, ranging from 0 (where the body stops immediately during this frame) to 1 (where the body and the
    /// obstacle just touch after the next velocity integration). A value below 1 is preferred, as the leftover velocity
    /// might be enough to trigger another speculative contact in the next frame.
    /// Default value: 0.9.
    /// </summary>
    public Real SpeculativeRelaxationFactor { get; set; } = (Real)0.9;

    /// <summary>
    /// Speculative contacts are generated when the relative velocity between two bodies exceeds
    /// the threshold value. To prevent bodies with a diameter of D from tunneling through thin walls, this
    /// threshold should be set to approximately D / timestep, e.g., 100 for a unit cube and a
    /// timestep of 0.01.
    /// Default value: 10.0.
    /// </summary>
    public Real SpeculativeVelocityThreshold { get; set; } = (Real)10.0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Detect(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        if (BroadPhaseFilter != null)
        {
            if (!BroadPhaseFilter.Filter(proxyA, proxyB))
            {
                return;
            }
        }

        if (proxyA is not RigidBodyShape sA || proxyB is not RigidBodyShape sB)
        {
            throw new InvalidCollisionTypeException(proxyA.GetType(), proxyB.GetType());
        }

        if (sB.ShapeId < sA.ShapeId)
        {
            (sA, sB) = (sB, sA);
        }

        Debug.Assert(sA.RigidBody != sB.RigidBody);
        Debug.Assert(sA.RigidBody.World == this);
        Debug.Assert(sB.RigidBody.World == this);

        Debug.Assert(sA.RigidBody != null);
        Debug.Assert(sB.RigidBody != null);

        if (!sA.RigidBody.Data.IsActive && !sB.RigidBody.Data.IsActive) return;

        if ((sA.RigidBody.Data.MotionType != MotionType.Dynamic) &&
            (sB.RigidBody.Data.MotionType != MotionType.Dynamic)) return;

        ref RigidBodyData b1 = ref sA.RigidBody.Data;
        ref RigidBodyData b2 = ref sB.RigidBody.Data;

        bool speculative = sA.RigidBody.EnableSpeculativeContacts || sB.RigidBody.EnableSpeculativeContacts;

        var colliding = NarrowPhase.MprEpa(sA, sB, b1.Orientation, b2.Orientation, b1.Position, b2.Position,
            out JVector pA, out JVector pB, out JVector normal, out var penetration);

        if (!colliding)
        {
            if (!speculative) return;

            JVector dv = sB.RigidBody.Velocity - sA.RigidBody.Velocity;

            if (dv.LengthSquared() < SpeculativeVelocityThreshold * SpeculativeVelocityThreshold) return;

            bool success = NarrowPhase.Sweep(sA, sB, b1.Orientation, b2.Orientation,
                b1.Position, b2.Position, b1.Velocity, b2.Velocity,
                out pA, out pB, out normal, out Real toi);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (!success || toi > stepDt || toi == (Real)0.0) return;

            penetration = normal * (pA - pB) * SpeculativeRelaxationFactor;

            if (NarrowPhaseFilter != null)
            {
                if (!NarrowPhaseFilter.Filter(sA, sB, ref pA, ref pB, ref normal, ref penetration))
                {
                    return;
                }
            }

            RegisterContact(sA.ShapeId, sB.ShapeId, sA.RigidBody, sB.RigidBody,
                pA, pB, normal, ContactData.SolveMode.Angular);

            return;
        }

        if (NarrowPhaseFilter != null)
        {
            if (!NarrowPhaseFilter.Filter(sA, sB, ref pA, ref pB, ref normal, ref penetration))
            {
                return;
            }
        }

        if (EnableAuxiliaryContactPoints)
        {
            Unsafe.SkipInit(out CollisionManifold manifold);
            manifold.BuildManifold(sA, sB, pA, pB, normal);

            RegisterContact(sA.ShapeId, sB.ShapeId, sA.RigidBody, sB.RigidBody, normal, ref manifold);
        }
        else
        {
            RegisterContact(sA.ShapeId, sB.ShapeId, sA.RigidBody, sB.RigidBody, pA, pB, normal);
        }
    }

    /// <summary>
    /// Registers a single contact point into an existing <see cref="Arbiter"/>.
    /// </summary>
    /// <remarks>
    /// This method adds a contact point to the specified <paramref name="arbiter"/>, using the provided contact points
    /// and normal. All input vectors must be in world space. The <paramref name="normal"/> vector must be normalized.
    /// This method assumes that the <paramref name="arbiter"/> is already valid and mapped to the correct pair of bodies.
    /// <para>
    /// Calls that create or register contacts may run concurrently with each other. They must not run
    /// concurrently with <see cref="Step(Real, bool)"/>, <see cref="Remove(Arbiter)"/>, body or shape
    /// removal, <see cref="Clear"/>, <see cref="Dispose"/>, or other topology-changing operations.
    /// If custom contact generation uses external worker threads, all such work must finish before the
    /// world is stepped or modified.
    /// </para>
    /// <para>
    /// Do not pass an arbiter after it has been removed from the world; removed arbiters may be recycled.
    /// </para>
    /// </remarks>
    /// <param name="arbiter">The existing <see cref="Arbiter"/> instance to which the contact will be added.</param>
    /// <param name="point1">The contact point on the first body, in world space.</param>
    /// <param name="point2">The contact point on the second body, in world space.</param>
    /// <param name="normal">The contact normal, in world space. Must be normalized.</param>
    /// <param name="removeFlags">A bitmask of <see cref="ContactData.SolveMode"/> flags to be removed from the full
    /// contact solution (see <see cref="ContactData.SolveMode.Full"/>).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterContact(Arbiter arbiter, in JVector point1, in JVector point2,
        in JVector normal, ContactData.SolveMode removeFlags = ContactData.SolveMode.None)
    {
        lock (arbiter)
        {
            memContacts.ResizeLock.EnterReadLock();
            try
            {
                if (!PersistentContactManifold) arbiter.Handle.Data.UsageMask = 0;
                arbiter.Handle.Data.AddContact(point1, point2, normal);
                arbiter.Handle.Data.ResetMode(removeFlags);
            }
            finally
            {
                memContacts.ResizeLock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Registers one or more contact points between two rigid bodies using a <see cref="CollisionManifold"/>,
    /// creating an <see cref="Arbiter"/> if one does not already exist.
    /// </summary>
    /// <remarks>
    /// This method ensures that contact information between the specified ID pair is tracked by an <see cref="Arbiter"/>.
    /// If no arbiter exists for the given IDs, one is created using <paramref name="body1"/> and <paramref name="body2"/>.
    ///
    /// <para>
    /// Calls that create or register contacts may run concurrently with each other. They must not run
    /// concurrently with <see cref="Step(Real, bool)"/>, <see cref="Remove(Arbiter)"/>, body or shape
    /// removal, <see cref="Clear"/>, <see cref="Dispose"/>, or other topology-changing operations.
    /// If custom contact generation uses external worker threads, all such work must finish before the
    /// world is stepped or modified.
    /// </para>
    ///
    /// <para><b>Note:</b> The order of <paramref name="id0"/> and <paramref name="id1"/> <i>does matter</i>.</para>
    /// </remarks>
    /// <param name="id0">The first identifier associated with the contact (e.g., shape or feature ID).</param>
    /// <param name="id1">The second identifier associated with the contact.</param>
    /// <param name="body1">The first rigid body involved in the contact.</param>
    /// <param name="body2">The second rigid body involved in the contact.</param>
    /// <param name="normal">
    /// The contact normal, in world space. Must be a unit vector pointing from <paramref name="body1"/> toward <paramref name="body2"/>.
    /// </param>
    /// <param name="manifold">A <see cref="CollisionManifold"/> containing contact point pairs in world space.</param>
    /// <param name="removeFlags">A bitmask of <see cref="ContactData.SolveMode"/> flags to be removed from the full
    /// contact solution (see <see cref="ContactData.SolveMode.Full"/>).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterContact(ulong id0, ulong id1, RigidBody body1, RigidBody body2, in JVector normal,
        ref CollisionManifold manifold, ContactData.SolveMode removeFlags = ContactData.SolveMode.None)
    {
        GetOrCreateArbiter(id0, id1, body1, body2, out Arbiter arbiter);

        lock (arbiter)
        {
            // Do not add contacts while contacts might be resized
            memContacts.ResizeLock.EnterReadLock();

            try
            {
                if (!PersistentContactManifold) arbiter.Handle.Data.UsageMask = 0;
                arbiter.Handle.Data.ResetMode(removeFlags);

                ReadOnlySpan<JVector> manifoldA = manifold.ManifoldA;
                ReadOnlySpan<JVector> manifoldB = manifold.ManifoldB;

                for (int e = 0; e < manifold.Count; e++)
                {
                    JVector mfA = manifoldA[e];
                    JVector mfB = manifoldB[e];
                    arbiter.Handle.Data.AddContact(mfA, mfB, normal);
                }
            }
            finally
            {
                memContacts.ResizeLock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Registers a contact point between two rigid bodies, creating an <see cref="Arbiter"/> if one does not already exist.
    /// </summary>
    /// <remarks>
    /// This method ensures that contact information between the specified ID pair is tracked by an <see cref="Arbiter"/>.
    /// If no arbiter exists for the given IDs, one is created using <paramref name="body1"/> and <paramref name="body2"/>.
    /// The provided contact points and normal must be in world space. The <paramref name="normal"/> vector must be normalized.
    ///
    /// <para>
    /// Calls that create or register contacts may run concurrently with each other. They must not run
    /// concurrently with <see cref="Step(Real, bool)"/>, <see cref="Remove(Arbiter)"/>, body or shape
    /// removal, <see cref="Clear"/>, <see cref="Dispose"/>, or other topology-changing operations.
    /// If custom contact generation uses external worker threads, all such work must finish before the
    /// world is stepped or modified.
    /// </para>
    ///
    /// <para><b>Note:</b> The order of <paramref name="id0"/> and <paramref name="id1"/> <i>does matter</i>.</para>
    /// </remarks>
    /// <param name="id0">The first identifier associated with the contact (e.g., shape or feature ID).</param>
    /// <param name="id1">The second identifier associated with the contact.</param>
    /// <param name="body1">The first rigid body involved in the contact.</param>
    /// <param name="body2">The second rigid body involved in the contact.</param>
    /// <param name="point1">The contact point on <paramref name="body1"/>, in world space.</param>
    /// <param name="point2">The contact point on <paramref name="body2"/>, in world space.</param>
    /// <param name="normal">
    /// The contact normal, in world space. Must be a unit vector pointing from <paramref name="body1"/> toward <paramref name="body2"/>.
    /// </param>
    /// <param name="removeFlags">A bitmask of <see cref="ContactData.SolveMode"/> flags to be removed from the full
    /// contact solution (see <see cref="ContactData.SolveMode.Full"/>).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterContact(ulong id0, ulong id1, RigidBody body1, RigidBody body2,
        in JVector point1, in JVector point2, in JVector normal,
        ContactData.SolveMode removeFlags = ContactData.SolveMode.None)
    {
        GetOrCreateArbiter(id0, id1, body1, body2, out Arbiter arbiter);
        RegisterContact(arbiter, point1, point2, normal, removeFlags);
    }

    /// <summary>
    /// Gets an existing <see cref="Arbiter"/> instance for the given pair of IDs.
    /// </summary>
    /// <param name="id0">The first identifier (e.g., shape ID).</param>
    /// <param name="id1">The second identifier.</param>
    /// <param name="arbiter">The arbiter for the ordered ID pair, or <see langword="null"/> if none exists.</param>
    /// <returns><see langword="true"/> if an arbiter exists for the ordered ID pair; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The order of <paramref name="id0"/> and <paramref name="id1"/> matters.
    /// For arbiters created by the engine, <paramref name="id0"/> &lt; <paramref name="id1"/> holds
    /// for <see cref="RigidBodyShape"/>s.
    /// <para>
    /// This method may run concurrently with contact creation and registration. It must not run
    /// concurrently with <see cref="Step(Real, bool)"/>, <see cref="Remove(Arbiter)"/>, body or shape
    /// removal, <see cref="Clear"/>, <see cref="Dispose"/>, or other topology-changing operations.
    /// </para>
    /// </remarks>
    public bool GetArbiter(ulong id0, ulong id1, [MaybeNullWhen(false)] out Arbiter arbiter)
    {
        ArbiterKey arbiterKey = new(id0, id1);

        lock (arbiters.GetLock(arbiterKey))
        {
            return arbiters.TryGetValue(arbiterKey, out arbiter!);
        }
    }

    /// <summary>
    /// Gets an existing <see cref="Arbiter"/> instance for the given pair of IDs,
    /// or creates a new one if none exists.
    /// </summary>
    /// <remarks>
    /// This method ensures there is a unique <see cref="Arbiter"/> for each ordered pair of IDs.
    /// If an arbiter already exists, it is returned via the <paramref name="arbiter"/> out parameter.
    /// Otherwise, a new arbiter is allocated, initialized with the provided <paramref name="body1"/> and <paramref name="body2"/>,
    /// and registered internally. The body arguments are used only when a new arbiter is created.
    ///
    /// <para>
    /// Calls that create or register contacts may run concurrently with each other. They must not run
    /// concurrently with <see cref="Step(Real, bool)"/>, <see cref="Remove(Arbiter)"/>, body or shape
    /// removal, <see cref="Clear"/>, <see cref="Dispose"/>, or other topology-changing operations.
    /// If custom contact generation uses external worker threads, all such work must finish before the
    /// world is stepped or modified.
    /// </para>
    ///
    /// <para><b>Note:</b> The order of <paramref name="id0"/> and <paramref name="id1"/> <i>does matter</i>.</para>
    /// </remarks>
    /// <param name="id0">The first identifier associated with the contact (e.g., shape or feature ID).</param>
    /// <param name="id1">The second identifier associated with the contact.</param>
    /// <param name="body1">The first rigid body. Used only if a new arbiter is created.</param>
    /// <param name="body2">The second rigid body. Used only if a new arbiter is created.</param>
    /// <param name="arbiter">The resulting <see cref="Arbiter"/> instance associated with the ID pair.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetOrCreateArbiter(ulong id0, ulong id1, RigidBody body1, RigidBody body2, out Arbiter arbiter)
    {
        ArbiterKey arbiterKey = new(id0, id1);

        lock (arbiters.GetLock(arbiterKey))
        {
            if (arbiters.TryGetValue(arbiterKey, out arbiter!))
            {
                Debug.Assert(arbiter.Body1 == body1 && arbiter.Body2 == body2);
                return;
            }

            arbiter = CreateArbiter(arbiterKey, body1, body2);
        }
    }

    private Arbiter RentArbiter()
    {
        return arbiterPool.TryPop(out Arbiter? arbiter) ? arbiter : new Arbiter();
    }

    private void ReturnArbiter(Arbiter arbiter)
    {
        arbiter.Handle = JHandle<ContactData>.Zero;
        arbiter.Body1 = null!;
        arbiter.Body2 = null!;
        arbiterPool.Push(arbiter);
    }

    // The caller holds the shard lock until initialization and publication complete.
    private Arbiter CreateArbiter(ArbiterKey arbiterKey, RigidBody body1, RigidBody body2)
    {
        Arbiter? arbiter = null;

        try
        {
            lock (memContacts)
            {
                arbiter = RentArbiter();

                var handle = memContacts.Allocate(true);
                arbiter.Handle = handle;
                handle.Data.Init(body1, body2);
                handle.Data.Key = arbiterKey;
                arbiter.Body1 = body1;
                arbiter.Body2 = body2;

                deferredArbiters.Add(arbiter);

                Debug.Assert(memContacts.IsActive(arbiter.Handle));
            }

            // Dictionary growth only blocks this shard, not every contact creator.
            arbiters.Add(arbiterKey, arbiter);
            return arbiter;
        }
        catch
        {
            if (arbiter != null)
            {
                lock (memContacts)
                {
                    deferredArbiters.Remove(arbiter);

                    if (!arbiter.Handle.IsZero)
                    {
                        // Free can move another, already published contact. Exclude
                        // concurrent RegisterContact calls while its data is moved.
                        memContacts.ResizeLock.EnterWriteLock();
                        try
                        {
                            memContacts.Free(arbiter.Handle);
                        }
                        finally
                        {
                            memContacts.ResizeLock.ExitWriteLock();
                        }
                    }

                    ReturnArbiter(arbiter);
                }
            }

            throw;
        }
    }
}
