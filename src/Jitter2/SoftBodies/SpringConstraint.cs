/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

namespace Jitter2.SoftBodies;

/// <summary>
/// Constrains two bodies to maintain a target distance between anchor points,
/// applying spring-like forces. Removes one translational degree of freedom along
/// the line connecting the anchors.
/// </summary>
/// <remarks>
/// This constraint is designed for soft body vertices, which act as mass points.
/// Angular velocities of the connected bodies are not taken into account.
/// The spring behavior is controlled by <see cref="Softness"/> and <see cref="Bias"/>,
/// which can be set directly or computed from physical parameters using
/// <see cref="SetSpringParameters"/>.
/// </remarks>
public unsafe class SpringConstraint : Constraint<SpringConstraint.SpringData>
{
    /// <summary>
    /// Low-level data for the spring constraint, stored in unmanaged memory.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SpringData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

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

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationSpringConstraint, &IterateSpringConstraint);

    /// <inheritdoc/>
    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = (Real)0.0;

    /// <summary>
    /// Initializes the constraint from world-space anchor points.
    /// </summary>
    /// <param name="anchor1">Anchor point on the first rigid body, in world space.</param>
    /// <param name="anchor2">Anchor point on the second rigid body, in world space.</param>
    /// <remarks>
    /// Computes local anchor offsets from the current body positions.
    /// Default values: <see cref="Softness"/> = <see cref="Constraint.DefaultLinearSoftness"/>, <see cref="Bias"/> = <see cref="Constraint.DefaultLinearBias"/>.
    /// The <see cref="TargetDistance"/> is set to the initial distance between the anchors.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="anchor1"/> or <paramref name="anchor2"/> contains a non-finite value.
    /// </exception>
    public void Initialize(JVector anchor1, JVector anchor2)
    {
        VerifyNotZero();
        ArgumentCheck.Finite(anchor1, nameof(anchor1));
        ArgumentCheck.Finite(anchor2, nameof(anchor2));

        ref SpringData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Subtract(anchor1, body1.Position, out data.LocalAnchor1);
        JVector.Subtract(anchor2, body2.Position, out data.LocalAnchor2);

        data.Softness = Constraint.DefaultLinearSoftness;
        data.BiasFactor = Constraint.DefaultLinearBias;
        data.Distance = (anchor2 - anchor1).Length();
    }

    /// <summary>
    /// Sets the spring parameters using physical properties. This method calculates and sets
    /// the <see cref="Softness"/> and <see cref="Bias"/> properties. It assumes that the mass
    /// of the involved bodies and the timestep size do not change. With restricted motion,
    /// the effective mass is measured along the current spring direction; call this again
    /// if that direction or the permitted axes change.
    /// </summary>
    /// <param name="frequency">The frequency in Hz.</param>
    /// <param name="damping">The damping ratio (0 = no damping, 1 = critical damping).</param>
    /// <param name="dt">The timestep of the simulation.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="frequency"/> or <paramref name="dt"/> is less than or equal to zero,
    /// when <paramref name="damping"/> is negative, or when any value is not finite.
    /// </exception>
    public void SetSpringParameters(Real frequency, Real damping, Real dt)
    {
        ArgumentCheck.Positive(frequency, nameof(frequency));
        ArgumentCheck.NonNegative(damping, nameof(damping));
        ArgumentCheck.Positive(dt, nameof(dt));

        ref SpringData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector direction = body2.Position + data.LocalAnchor2 - body1.Position - data.LocalAnchor1;
        if (direction.LengthSquared() > (Real)1e-12) JVector.NormalizeInPlace(ref direction);
        Real inverseEffectiveMass = body1.GetInverseMass(direction) + body2.GetInverseMass(direction);
        if (inverseEffectiveMass <= 0)
        {
            data.Softness = 0;
            data.BiasFactor = 0;
            return;
        }
        Real effectiveMass = (Real)1.0 / inverseEffectiveMass;

        Real omega = (Real)2.0 * MathR.PI * frequency;
        Real dampingCoefficient = (Real)2.0 * effectiveMass * damping * omega;
        Real springStiffness = effectiveMass * omega * omega;

        Real h = dt;
        data.Softness = (Real)1.0 / (dampingCoefficient + h * springStiffness);
        data.BiasFactor = h * springStiffness * data.Softness;
    }

    /// <summary>
    /// Gets the accumulated impulse applied by the spring.
    /// </summary>
    public Real Impulse
    {
        get
        {
            ref SpringData data = ref Data;
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
            DebugCheck.IsFinite(value, nameof(value));

            ref SpringData data = ref Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            JVector.Subtract(value, body1.Position, out data.LocalAnchor1);
        }
        get
        {
            ref SpringData data = ref Data;
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
            DebugCheck.IsFinite(value, nameof(value));

            ref SpringData data = ref Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            JVector.Subtract(value, body2.Position, out data.LocalAnchor2);
        }
        get
        {
            ref SpringData data = ref Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            JVector.Add(data.LocalAnchor2, body2.Position, out JVector result);
            return result;
        }
    }

    /// <summary>
    /// Gets or sets the target resting distance of the spring.
    /// </summary>
    /// <value>Units: meters. Default is the initial distance between anchors at initialization.</value>
    public Real TargetDistance
    {
        set
        {
            DebugCheck.IsNonNegative(value, nameof(value));

            ref SpringData data = ref Data;
            data.Distance = value;
        }
        get => Data.Distance;
    }

    /// <summary>
    /// Gets the current distance between the anchor points.
    /// </summary>
    public Real Distance
    {
        get
        {
            ref SpringData data = ref Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;

            JVector.Add(body1.Position, data.LocalAnchor1, out JVector p1);
            JVector.Add(body2.Position, data.LocalAnchor2, out JVector p2);

            JVector.Subtract(p2, p1, out JVector dp);

            return dp.Length();
        }
    }

    /// <summary>
    /// Prepares the spring constraint for iteration.
    /// </summary>
    /// <param name="constraint">The constraint data reference.</param>
    /// <param name="idt">The inverse substep duration (1/dt).</param>
    public static void PrepareForIterationSpringConstraint(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, SpringData>(ref constraint);
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector r1 = data.LocalAnchor1;
        JVector r2 = data.LocalAnchor2;

        JVector.Add(body1.Position, r1, out JVector p1);
        JVector.Add(body2.Position, r2, out JVector p2);

        JVector.Subtract(p2, p1, out JVector dp);

        Real error = dp.Length() - data.Distance;

        JVector n = dp;
        if (n.LengthSquared() > (Real)1e-12) JVector.NormalizeInPlace(ref n);

        data.Jacobian = n;
        data.EffectiveMass = body1.GetInverseMass(n) + body2.GetInverseMass(n);
        if (data.EffectiveMass <= 0)
        {
            data.EffectiveMass = 0;
            data.AccumulatedImpulse = 0;
            return;
        }
        data.EffectiveMass += data.Softness * idt;
        data.EffectiveMass = (Real)1.0 / data.EffectiveMass;

        data.Bias = error * data.BiasFactor * idt;

        body1.Velocity -= JVector.Multiply(body1.InverseMass, data.AccumulatedImpulse * data.Jacobian);
        body2.Velocity += JVector.Multiply(body2.InverseMass, data.AccumulatedImpulse * data.Jacobian);
    }

    /// <summary>
    /// Gets or sets the softness (compliance) of the spring constraint.
    /// </summary>
    /// <value>
    /// Default is <see cref="Constraint.DefaultLinearSoftness"/>. Higher values allow more positional error but produce a softer spring.
    /// Scaled by inverse timestep during solving.
    /// </value>
    public Real Softness
    {
        get => Data.Softness;
        set
        {
            DebugCheck.IsNonNegative(value, nameof(value));
            Data.Softness = value;
        }
    }

    /// <summary>
    /// Gets or sets the bias factor (error correction strength) of the spring constraint.
    /// </summary>
    /// <value>
    /// Default is 0.2. Higher values correct distance errors more aggressively.
    /// </value>
    public Real Bias
    {
        get => Data.BiasFactor;
        set
        {
            DebugCheck.IsNonNegative(value, nameof(value));
            Data.BiasFactor = value;
        }
    }

    /// <summary>
    /// Performs one iteration of the spring constraint solver.
    /// </summary>
    /// <param name="constraint">The constraint data reference.</param>
    /// <param name="idt">The inverse substep duration (1/dt).</param>
    public static void IterateSpringConstraint(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, SpringData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        Real jv = (body2.Velocity - body1.Velocity) * data.Jacobian;

        Real softnessScalar = data.AccumulatedImpulse * data.Softness * idt;

        Real lambda = -data.EffectiveMass * (jv + data.Bias + softnessScalar);

        Real oldAccumulatedImpulse = data.AccumulatedImpulse;
        data.AccumulatedImpulse += lambda;
        lambda = data.AccumulatedImpulse - oldAccumulatedImpulse;

        body1.Velocity -= JVector.Multiply(body1.InverseMass, lambda * data.Jacobian);
        body2.Velocity += JVector.Multiply(body2.InverseMass, lambda * data.Jacobian);
    }

    /// <inheritdoc/>
    public override void DebugDraw(IDebugDrawer drawer)
    {
        ref SpringData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector p1 = body1.Position + data.LocalAnchor1;
        JVector p2 = body2.Position + data.LocalAnchor2;

        drawer.DrawSegment(body1.Position, p1);
        drawer.DrawSegment(body2.Position, p2);
        drawer.DrawSegment(p1, p2);
        drawer.DrawPoint(p1);
        drawer.DrawPoint(p2);
    }
}
