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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2;

public sealed partial class World
{
    public class InvalidCollisionTypeException : Exception
    {
        public InvalidCollisionTypeException(Type proxyA, Type proxyB)
            : base($"Don't know how to handle collision between {proxyA} and {proxyB}." +
                   $" Register a BroadPhaseFilter to handle and/or filter out these collision types.")
        {
        }
    }

    /// <summary>
    /// Specifies an implementation of the <see cref="INarrowPhaseFilter"/> to be used in collision detection.
    /// The default instance is of type <see cref="TriangleEdgeCollisionFilter"/>.
    /// </summary>
    public INarrowPhaseFilter? NarrowPhaseFilter { get; set; } = new TriangleEdgeCollisionFilter();

    /// <summary>
    /// Specifies an implementation of the <see cref="IBroadPhaseFilter"/> to be used in collision detection.
    /// The default value is null.
    /// </summary>
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
    /// might be sufficient to trigger another speculative contact in the next frame.
    /// </summary>
    public Real SpeculativeRelaxationFactor { get; set; } = (Real)0.9;

    /// <summary>
    /// Speculative contacts are generated when the velocity towards an obstacle exceeds
    /// the threshold value. To prevent bodies with a diameter of D from tunneling through thin walls, this
    /// threshold should be set to approximately D / timestep, e.g., 100 for a unit cube and a
    /// timestep of 0.01.
    /// </summary>
    public Real SpeculativeVelocityThreshold { get; set; } =(Real)10;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterContact(Arbiter arbiter, in JVector point1, in JVector point2,
        in JVector normal, Real penetration, bool speculative = false)
    {
        lock (arbiter)
        {
            memContacts.ResizeLock.EnterReadLock();
            arbiter.Handle.Data.IsSpeculative = speculative;
            arbiter.Handle.Data.AddContact(point1, point2, normal, penetration);
            memContacts.ResizeLock.ExitReadLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterContact(ulong id0, ulong id1, RigidBody body1, RigidBody body2, in JVector normal,
        ref CollisionManifold manifold, bool speculative = false)
    {
        GetArbiter(id0, id1, body1, body2, out Arbiter arbiter);

        lock (arbiter)
        {
            // Do no add contacts while contacts might be resized
            memContacts.ResizeLock.EnterReadLock();

            arbiter.Handle.Data.IsSpeculative = speculative;

            for (int e = 0; e < manifold.Count; e++)
            {
                JVector mfA = manifold.ManifoldA[e];
                JVector mfB = manifold.ManifoldB[e];

                Real nd = JVector.Dot(mfA - mfB, normal);
                if (nd < (Real)0.0) continue;

                arbiter.Handle.Data.AddContact(mfA, mfB, normal, nd);
            }

            memContacts.ResizeLock.ExitReadLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RegisterContact(ulong id0, ulong id1, RigidBody body1, RigidBody body2,
        in JVector point1, in JVector point2, in JVector normal, Real penetration, bool speculative = false)
    {
        GetArbiter(id0, id1, body1, body2, out Arbiter arbiter);
        RegisterContact(arbiter, point1, point2, normal, penetration, speculative);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DetectCallback(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
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

        Unsafe.SkipInit(out JVector normal);
        Unsafe.SkipInit(out JVector pA);
        Unsafe.SkipInit(out JVector pB);

        Debug.Assert(sA.RigidBody != sB.RigidBody);
        Debug.Assert(sA.RigidBody.World == this);
        Debug.Assert(sB.RigidBody.World == this);

        Debug.Assert(sA.RigidBody != null);
        Debug.Assert(sB.RigidBody != null);

        if (!sA.RigidBody.Data.IsActive && !sB.RigidBody.Data.IsActive) return;
        if (sA.RigidBody.Data.IsStatic && sB.RigidBody.Data.IsStatic) return;

        ref RigidBodyData b1 = ref sA.RigidBody.Data;
        ref RigidBodyData b2 = ref sB.RigidBody.Data;

        bool speculative = sA.RigidBody.EnableSpeculativeContacts || sB.RigidBody.EnableSpeculativeContacts;

        var colliding = NarrowPhase.MPREPA(sA, sB, b1.Orientation, b2.Orientation, b1.Position, b2.Position,
            out pA, out pB, out normal, out var penetration);

        if (!colliding)
        {
            if (!speculative) return;

            JVector dv = sB.RigidBody.Velocity - sA.RigidBody.Velocity;

            if (dv.LengthSquared() < SpeculativeVelocityThreshold * SpeculativeVelocityThreshold) return;

            bool success = NarrowPhase.Sweep(sA, sB, b1.Orientation, b2.Orientation,
                b1.Position, b2.Position, b1.Velocity, b2.Velocity,
                out pA, out pB, out normal, out Real toi);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (!success || toi > step_dt || toi == (Real)0.0) return;

            penetration = normal * (pA - pB) * SpeculativeRelaxationFactor;

            if (NarrowPhaseFilter != null)
            {
                if (!NarrowPhaseFilter.Filter(sA, sB, ref pA, ref pB, ref normal, ref penetration))
                {
                    return;
                }
            }

            RegisterContact(sA.ShapeId, sB.ShapeId, sA.RigidBody, sB.RigidBody,
                pA, pB, normal, penetration, speculative: true);

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
            manifold.BuildManifold<RigidBodyShape,RigidBodyShape>(sA, sB, pA, pB, normal);

            RegisterContact(sA.ShapeId, sB.ShapeId, sA.RigidBody, sB.RigidBody,
                normal, ref manifold, speculative: false);
        }
        else
        {
            RegisterContact(sA.ShapeId, sB.ShapeId, sA.RigidBody, sB.RigidBody,
                pA, pB, normal, penetration, speculative: false);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetArbiter(ulong id0, ulong id1, RigidBody b0, RigidBody b1, out Arbiter arbiter)
    {
        ArbiterKey arbiterKey = new(id0, id1);

        lock (arbiters.GetLock(arbiterKey))
        {
            if (arbiters.TryGetValue(arbiterKey, out arbiter!)) return;

            lock (memContacts)
            {
                if (!Arbiter.Pool.TryPop(out arbiter!))
                {
                    arbiter = new Arbiter();
                }

                var handle = memContacts.Allocate(true);
                arbiter.Handle = handle;
                handle.Data.Init(b0, b1);
                handle.Data.Key = arbiterKey;
                arbiter.Body1 = b0;
                arbiter.Body2 = b1;

                arbiters.Add(arbiterKey, arbiter);
                deferredArbiters.Add(arbiter);

                Debug.Assert(memContacts.IsActive(arbiter.Handle));
            }
        }
    }
}