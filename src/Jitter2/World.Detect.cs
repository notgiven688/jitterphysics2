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
    /// A filter that can reject collisions or adjust contact points and normals.
    /// Defaults to <see cref="TriangleEdgeCollisionFilter"/>.
    /// </summary>
    /// <remarks>
    /// Called after a collision is detected. With multithreaded stepping, calls can run
    /// concurrently, so the filter must be thread-safe.
    /// </remarks>
    public INarrowPhaseFilter? NarrowPhaseFilter { get; set; } = new TriangleEdgeCollisionFilter();

    /// <summary>
    /// A filter that can reject shape pairs before testing them for collisions. Default is null.
    /// </summary>
    /// <remarks>
    /// Use this for collision layers or custom proxy types. With multithreaded stepping,
    /// calls can run concurrently, so the filter must be thread-safe.
    /// </remarks>
    public IBroadPhaseFilter? BroadPhaseFilter { get; set; }

    /// <summary>
    /// Generates extra contact points between touching flat surfaces to improve stability,
    /// especially when objects first come to rest. Default is true.
    /// </summary>
    public bool EnableAuxiliaryContactPoints { set; get; } = true;

    /// <summary>
    /// Reuses contact points and impulses from previous steps to improve resting stability.
    /// Default is true. When false, contacts are discarded at the end of each step,
    /// which can require more solver iterations to achieve the same stability.
    /// </summary>
    public bool PersistentContactManifold { get; set; } = true;

    /// <summary>
    /// Fraction of the predicted gap that CCD contacts allow to close during one step. Default is 0.9.
    /// </summary>
    /// <remarks>
    /// Use values from 0 (no closing motion) to 1 (allow the full gap to close).
    /// Values below 1 leave a small separation margin.
    /// </remarks>
    public Real CcdRelaxationFactor { get; set; } = (Real)0.9;

    /// <summary>
    /// Minimum relative surface speed for continuous collision detection, in world units per second.
    /// Nearly stationary pairs below this threshold are skipped. Default is 0.1.
    /// </summary>
    /// <remarks>
    /// The speed estimate includes both translation and rotation. Set to zero to include even
    /// slow-moving pairs.
    /// </remarks>
    public Real CcdVelocityThreshold { get; set; } = (Real)0.1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Detect(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        IBroadPhaseFilter? broadPhaseFilter = BroadPhaseFilter;
        if (broadPhaseFilter != null)
        {
            if (!broadPhaseFilter.Filter(proxyA, proxyB))
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

        RigidBody bodyA = sA.RigidBody;
        RigidBody bodyB = sB.RigidBody;

        Debug.Assert(bodyA != null);
        Debug.Assert(bodyB != null);
        Debug.Assert(bodyA != bodyB);
        Debug.Assert(bodyA.World == this);
        Debug.Assert(bodyB.World == this);

        ref RigidBodyData b1 = ref bodyA.Data;
        ref RigidBodyData b2 = ref bodyB.Data;

        if (!b1.IsActive && !b2.IsActive) return;

        if (b1.MotionType != MotionType.Dynamic && b2.MotionType != MotionType.Dynamic) return;

        INarrowPhaseFilter? narrowPhaseFilter = NarrowPhaseFilter;

        var colliding = NarrowPhase.MprEpa(sA, sB, b1.Orientation, b2.Orientation, b1.Position, b2.Position,
            out JVector pA, out JVector pB, out JVector normal, out var penetration);

        if (!colliding)
        {
            return;
        }

        if (narrowPhaseFilter != null)
        {
            if (!narrowPhaseFilter.Filter(sA, sB, ref pA, ref pB, ref normal, ref penetration))
            {
                return;
            }
        }

        if (EnableAuxiliaryContactPoints)
        {
            Unsafe.SkipInit(out CollisionManifold manifold);
            manifold.BuildManifold(sA, sB, pA, pB, normal);

            RegisterContact(sA.ShapeId, sB.ShapeId, bodyA, bodyB, normal, ref manifold);
        }
        else
        {
            RegisterContact(sA.ShapeId, sB.ShapeId, bodyA, bodyB, pA, pB, normal);
        }
    }

    /// <summary>
    /// Registers a single contact point into an existing <see cref="Arbiter"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contact creation and registration calls may run concurrently with each other.
    /// Finish all such calls before stepping the world, removing arbiters, or adding, removing,
    /// or changing simulation objects. Do not overlap them with <see cref="Clear"/> or <see cref="Dispose"/>.
    /// </para>
    /// <para>
    /// Do not pass an arbiter after it has been removed from the world; removed arbiters may be recycled.
    /// </para>
    /// </remarks>
    /// <param name="arbiter">The existing <see cref="Arbiter"/> instance to which the contact will be added.</param>
    /// <param name="point1">The contact point on the first body, in world space.</param>
    /// <param name="point2">The contact point on the second body, in world space.</param>
    /// <param name="normal">The contact normal, in world space. Must be normalized; a positive normal
    /// impulse pushes the arbiter's second body along this direction and its first body against it.</param>
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
                ref ContactData data = ref arbiter.Handle.Data;
                if (!PersistentContactManifold) data.UsageMask = 0;
                data.ResetMode(removeFlags);
                data.AddContact(point1, point2, normal);
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
    /// <para>
    /// Contact creation and registration calls may run concurrently with each other.
    /// Finish all such calls before stepping the world, removing arbiters, or adding, removing,
    /// or changing simulation objects. Do not overlap them with <see cref="Clear"/> or <see cref="Dispose"/>.
    /// </para>
    ///
    /// <para>The IDs identify an ordered pair: keep their order consistent between calls.</para>
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
                ref ContactData data = ref arbiter.Handle.Data;
                if (!PersistentContactManifold) data.UsageMask = 0;
                data.ResetMode(removeFlags);

                ReadOnlySpan<JVector> manifoldA = manifold.ManifoldA;
                ReadOnlySpan<JVector> manifoldB = manifold.ManifoldB;

                for (int e = 0; e < manifold.Count; e++)
                {
                    JVector mfA = manifoldA[e];
                    JVector mfB = manifoldB[e];
                    data.AddContact(mfA, mfB, normal);
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
    /// <para>
    /// Contact creation and registration calls may run concurrently with each other.
    /// Finish all such calls before stepping the world, removing arbiters, or adding, removing,
    /// or changing simulation objects. Do not overlap them with <see cref="Clear"/> or <see cref="Dispose"/>.
    /// </para>
    ///
    /// <para>The IDs identify an ordered pair: keep their order consistent between calls.</para>
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
    /// May run concurrently with contact creation and registration. Finish these calls before
    /// stepping or modifying the world, including removing arbiters, bodies, or shapes.
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
    /// The body arguments are used only when creating a new arbiter. Reuse the same IDs
    /// and body order when registering contacts for this pair.
    ///
    /// <para>
    /// Contact creation and registration calls may run concurrently with each other.
    /// Finish all such calls before stepping the world, removing arbiters, or adding, removing,
    /// or changing simulation objects. Do not overlap them with <see cref="Clear"/> or <see cref="Dispose"/>.
    /// </para>
    ///
    /// <para>The IDs identify an ordered pair: keep their order consistent between calls.</para>
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

        // Queued BeginCollide notifications may still refer to this instance.
        if (dispatchArbiters.Count > 0) retiredArbiters.Add(arbiter);
        else arbiterPool.Push(arbiter);
    }

    // The caller holds the shard lock until initialization and publication complete.
    private Arbiter CreateArbiter(ArbiterKey arbiterKey, RigidBody body1, RigidBody body2)
    {
        Debug.Assert(body1 != body2);
        Debug.Assert(body1.IsValid);
        Debug.Assert(body1.World == this);
        Debug.Assert(body2.IsValid);
        Debug.Assert(body2.World == this);

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
