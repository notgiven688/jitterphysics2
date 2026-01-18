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
    public class InvalidCollisionTypeException(Type proxyA, Type proxyB) : Exception(
        $"Don't know how to handle collision between {proxyA} and {proxyB}." +
        $" Register a BroadPhaseFilter to handle and/or filter out these collision types.");

    /// <summary>
    /// Specifies an implementation of the <see cref="INarrowPhaseFilter"/> to be used in collision detection.
    /// The default instance is of type <see cref="TriangleEdgeCollisionFilter"/>.
    /// </summary>
    /// <remarks>
    /// When <see cref="Step(Real, bool)"/> is called with <c>multiThread=true</c>, this filter may be
    /// invoked concurrently from worker threads. Implementations must be thread-safe.
    /// </remarks>
    public INarrowPhaseFilter? NarrowPhaseFilter { get; set; } = new TriangleEdgeCollisionFilter();

    /// <summary>
    /// Specifies an implementation of the <see cref="IBroadPhaseFilter"/> to be used in collision detection.
    /// The default value is null.
    /// </summary>
    /// <remarks>
    /// When <see cref="Step(Real, bool)"/> is called with <c>multiThread=true</c>, this filter may be
    /// invoked concurrently from worker threads. Implementations must be thread-safe.
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
    /// A speculative contact slows a body down such that it does not penetrate or tunnel through
    /// an obstacle within one frame. The <see cref="SpeculativeRelaxationFactor"/> scales the
    /// slowdown, ranging from 0 (where the body stops immediately during this frame) to 1 (where the body and the
    /// obstacle just touch after the next velocity integration). A value below 1 is preferred, as the leftover velocity
    /// might be enough to trigger another speculative contact in the next frame.
    /// </summary>
    public Real SpeculativeRelaxationFactor { get; set; } = (Real)0.9;

    /// <summary>
    /// Speculative contacts are generated when the velocity towards an obstacle exceeds
    /// the threshold value. To prevent bodies with a diameter of D from tunneling through thin walls, this
    /// threshold should be set to approximately D / timestep, e.g., 100 for a unit cube and a
    /// timestep of 0.01.
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
            arbiter.Handle.Data.AddContact(point1, point2, normal);
            arbiter.Handle.Data.ResetMode(removeFlags);
            memContacts.ResizeLock.ExitReadLock();
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
    /// This method is thread-safe.
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

            arbiter.Handle.Data.ResetMode(removeFlags);

            for (int e = 0; e < manifold.Count; e++)
            {
                JVector mfA = manifold.ManifoldA[e];
                JVector mfB = manifold.ManifoldB[e];

                Real nd = JVector.Dot(mfA - mfB, normal);
                if (nd < (Real)0.0) continue;

                arbiter.Handle.Data.AddContact(mfA, mfB, normal);
            }

            memContacts.ResizeLock.ExitReadLock();
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
    /// This method is thread-safe.
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
    /// Retrieves an existing <see cref="Arbiter"/> instance for the given pair of IDs.
    /// </summary>
    /// <param name="id0">The first identifier (e.g., shape ID).</param>
    /// <param name="id1">The second identifier.</param>
    /// <param name="arbiter">When this method returns true, contains the arbiter; otherwise, null.</param>
    /// <returns><see langword="true"/> if an arbiter exists for the ordered ID pair; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The order of <paramref name="id0"/> and <paramref name="id1"/> matters.
    /// For arbiters created by the engine, <paramref name="id0"/> &lt; <paramref name="id1"/> holds
    /// for <see cref="RigidBodyShape"/>s.
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
    /// Retrieves an existing <see cref="Arbiter"/> instance for the given pair of IDs,
    /// or creates a new one if none exists.
    /// </summary>
    /// <remarks>
    /// This method ensures there is a unique <see cref="Arbiter"/> for each ordered pair of IDs.
    /// If an arbiter already exists, it is returned via the <paramref name="arbiter"/> out parameter.
    /// Otherwise, a new arbiter is allocated, initialized with the provided <paramref name="body1"/> and <paramref name="body2"/>,
    /// and registered internally. The body arguments are used only when a new arbiter is created.
    ///
    /// This method is thread-safe.
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

            lock (memContacts)
            {
                if (!Arbiter.Pool.TryPop(out arbiter!))
                {
                    arbiter = new Arbiter();
                }

                var handle = memContacts.Allocate(true);
                arbiter.Handle = handle;
                handle.Data.Init(body1, body2);
                handle.Data.Key = arbiterKey;
                arbiter.Body1 = body1;
                arbiter.Body2 = body2;

                arbiters.Add(arbiterKey, arbiter);
                deferredArbiters.Add(arbiter);

                Debug.Assert(memContacts.IsActive(arbiter.Handle));
            }
        }
    }
}