/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

namespace Jitter2.SoftBodies;

/// <summary>
/// A constraint that acts like a spring, connecting two bodies.
/// It applies forces to maintain a target distance between two anchor points.
/// </summary>
public unsafe class SpringConstraint : Constraint
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SpringData
    {
        internal int _internal;
        private readonly delegate*<ref ConstraintData, void> iterate;
        private readonly delegate*<ref ConstraintData, Real, void> prepareForIteration;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAnchor1;
        public JVector LocalAnchor2;

        public Real BiasFactor;
        public Real Softness;
        public Real Distance;

        public Real EffectiveMass;
        public Real AccumulatedImpulse;
        public Real Bias;

        public JVector Jacobian;
    }

    private JHandle<SpringData> handle;

    /// <inheritdoc/>
    protected override void Create()
    {
        CheckDataSize<SpringData>();

        Iterate = &IterateSpringConstraint;
        PrepareForIteration = &PrepareForIterationSpringConstraint;
        handle = JHandle<ConstraintData>.AsHandle<SpringData>(Handle);
    }

    /// <inheritdoc/>
    public override bool IsSmallConstraint { get; } = sizeof(SpringData) <= sizeof(SmallConstraintData);

    /// <summary>
    /// Initializes the constraint.
    /// </summary>
    /// <param name="anchor1">Anchor point on the first rigid body, in world space.</param>
    /// <param name="anchor2">Anchor point on the second rigid body, in world space.</param>
    public void Initialize(JVector anchor1, JVector anchor2)
    {
        ref SpringData data = ref handle.Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Subtract(anchor1, body1.Position, out data.LocalAnchor1);
        JVector.Subtract(anchor2, body2.Position, out data.LocalAnchor2);

        data.Softness = (Real)0.001;
        data.BiasFactor = (Real)0.2;
        data.Distance = (anchor2 - anchor1).Length();
    }

    /// <summary>
    /// Sets the spring parameters using physical properties. This method calculates and sets
    /// the <see cref="Softness"/> and <see cref="Bias"/> properties. It assumes that the mass
    /// of the involved bodies and the timestep size does not change.
    /// </summary>
    /// <param name="frequency">The frequency in Hz.</param>
    /// <param name="damping">The damping ratio (0 = no damping, 1 = critical damping).</param>
    /// <param name="dt">The timestep of the simulation.</param>
    public void SetSpringParameters(Real frequency, Real damping, Real dt)
    {
        ref SpringData data = ref handle.Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        Real effectiveMass = (Real)1.0 / (body1.InverseMass + body2.InverseMass);

        Real omega = (Real)2.0 * MathR.PI * frequency;
        Real d = (Real)2.0 * effectiveMass * damping * omega;
        Real k = effectiveMass * omega * omega;

        Real h = dt;
        data.Softness = (Real)1.0 / (d + h * k);
        data.BiasFactor = h * k * data.Softness;
    }

    /// <summary>
    /// Gets the accumulated impulse applied by the spring.
    /// </summary>
    public Real Impulse
    {
        get
        {
            ref SpringData data = ref handle.Data;
            return data.AccumulatedImpulse;
        }
    }

    /// <summary>
    /// Gets or sets the anchor point on the first body in world space.
    /// </summary>
    public JVector Anchor1
    {
        set
        {
            ref SpringData data = ref handle.Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            JVector.Subtract(value, body1.Position, out data.LocalAnchor1);
        }
        get
        {
            ref SpringData data = ref handle.Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            JVector.Add(data.LocalAnchor1, body1.Position, out JVector result);
            return result;
        }
    }

    /// <summary>
    /// Gets or sets the anchor point on the second body in world space.
    /// </summary>
    public JVector Anchor2
    {
        set
        {
            ref SpringData data = ref handle.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            JVector.Subtract(value, body2.Position, out data.LocalAnchor2);
        }
        get
        {
            ref SpringData data = ref handle.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            JVector.Add(data.LocalAnchor2, body2.Position, out JVector result);
            return result;
        }
    }

    /// <summary>
    /// Gets or sets the target resting distance of the spring.
    /// </summary>
    public Real TargetDistance
    {
        set
        {
            ref SpringData data = ref handle.Data;
            data.Distance = value;
        }
        get => handle.Data.Distance;
    }

    /// <summary>
    /// Gets the current distance between the anchor points.
    /// </summary>
    public Real Distance
    {
        get
        {
            ref SpringData data = ref handle.Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;

            JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector r1);
            JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector r2);

            JVector.Add(body1.Position, r1, out JVector p1);
            JVector.Add(body2.Position, r2, out JVector p2);

            JVector.Subtract(p2, p1, out JVector dp);

            return dp.Length();
        }
    }

    public static void PrepareForIterationSpringConstraint(ref ConstraintData constraint, Real idt)
    {
        ref SpringData data = ref Unsafe.AsRef<SpringData>(Unsafe.AsPointer(ref constraint));
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector r1 = data.LocalAnchor1;
        JVector r2 = data.LocalAnchor2;

        JVector.Add(body1.Position, r1, out JVector p1);
        JVector.Add(body2.Position, r2, out JVector p2);

        JVector.Subtract(p2, p1, out JVector dp);

        Real error = dp.Length() - data.Distance;

        JVector n = p2 - p1;
        if (n.LengthSquared() > (Real)1e-12) JVector.NormalizeInPlace(ref n);

        data.Jacobian = n;
        data.EffectiveMass = body1.InverseMass + body2.InverseMass;
        data.EffectiveMass += data.Softness * idt;
        data.EffectiveMass = (Real)1.0 / data.EffectiveMass;

        data.Bias = error * data.BiasFactor * idt;

        body1.Velocity -= body1.InverseMass * data.AccumulatedImpulse * data.Jacobian;
        body2.Velocity += body2.InverseMass * data.AccumulatedImpulse * data.Jacobian;
    }

    /// <summary>
    /// Gets or sets the softness of the spring.
    /// </summary>
    public Real Softness
    {
        get => handle.Data.Softness;
        set => handle.Data.Softness = value;
    }

    /// <summary>
    /// Gets or sets the bias factor of the spring.
    /// </summary>
    public Real Bias
    {
        get => handle.Data.BiasFactor;
        set => handle.Data.BiasFactor = value;
    }

    public static void IterateSpringConstraint(ref ConstraintData constraint, Real idt)
    {
        ref SpringData data = ref Unsafe.AsRef<SpringData>(Unsafe.AsPointer(ref constraint));
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        Real jv = (body2.Velocity - body1.Velocity) * data.Jacobian;

        Real softnessScalar = data.AccumulatedImpulse * data.Softness * idt;

        Real lambda = -data.EffectiveMass * (jv + data.Bias + softnessScalar);

        Real oldAccumulatedImpulse = data.AccumulatedImpulse;
        data.AccumulatedImpulse += lambda;
        lambda = data.AccumulatedImpulse - oldAccumulatedImpulse;

        body1.Velocity -= body1.InverseMass * lambda * data.Jacobian;
        body2.Velocity += body2.InverseMass * lambda * data.Jacobian;
    }
}