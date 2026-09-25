/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// A motor constraint that drives relative translational velocity along an axis fixed
/// in the reference frame of each body.
/// </summary>
public unsafe class LinearMotor : Constraint<LinearMotor.LinearMotorData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LinearMotorData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAxis1;
        public JVector LocalAxis2;

        public Real Velocity;
        public Real MaxForce;
        public Real MaxLambda;

        public Real EffectiveMass;

        public Real AccumulatedImpulse;
    }

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationLinearMotor, &IterateLinearMotor);

    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = (Real)0.0;

    /// <summary>
    /// Gets or sets the motor axis on the first body in local space.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the assigned value is not finite or normalized.
    /// </exception>
    public JVector LocalAxis1
    {
        get => Data.LocalAxis1;
        set
        {
            Data.LocalAxis1 = ArgumentCheck.UnitVector(value, nameof(value));
        }
    }

    /// <summary>
    /// Gets or sets the motor axis on the second body in local space.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the assigned value is not finite or normalized.
    /// </exception>
    public JVector LocalAxis2
    {
        get => Data.LocalAxis2;
        set
        {
            Data.LocalAxis2 = ArgumentCheck.UnitVector(value, nameof(value));
        }
    }

    /// <summary>
    /// Initializes the motor with axes for each body.
    /// </summary>
    /// <param name="axis1">Motor axis on the first body in world space.</param>
    /// <param name="axis2">Motor axis on the second body in world space.</param>
    /// <remarks>
    /// Stores the axes in local frames. Both axes are normalized internally.
    /// Default values: <see cref="TargetVelocity"/> = 0, <see cref="MaximumForce"/> = 0.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="axis1"/> or <paramref name="axis2"/> is zero or contains a non-finite value.
    /// </exception>
    public void Initialize(JVector axis1, JVector axis2)
    {
        VerifyNotZero();
        ArgumentCheck.NonZero(axis1, nameof(axis1));
        ArgumentCheck.NonZero(axis2, nameof(axis2));

        ref LinearMotorData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.NormalizeInPlace(ref axis1);
        JVector.NormalizeInPlace(ref axis2);

        JVector.ConjugatedTransform(axis1, body1.Orientation, out data.LocalAxis1);
        JVector.ConjugatedTransform(axis2, body2.Orientation, out data.LocalAxis2);

        data.MaxForce = 0;
        data.Velocity = 0;
    }

    /// <summary>
    /// Gets or sets the target linear velocity in units per second.
    /// </summary>
    /// <value>Default is 0.</value>
    public Real TargetVelocity
    {
        get => Data.Velocity;
        set
        {
            DebugCheck.IsFinite(value, nameof(value));
            Data.Velocity = value;
        }
    }

    /// <summary>
    /// Gets or sets the maximum force the motor can apply.
    /// </summary>
    /// <value>Default is 0. Must be non-negative.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is negative.
    /// </exception>
    public Real MaximumForce
    {
        get => Data.MaxForce;
        set
        {
            Data.MaxForce = ArgumentCheck.NonNegative(value, nameof(value));
        }
    }

    /// <summary>
    /// Gets the accumulated impulse applied by this motor during the last step.
    /// </summary>
    public Real Impulse => Data.AccumulatedImpulse;

    public static void PrepareForIterationLinearMotor(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, LinearMotorData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector j1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector j2);

        data.EffectiveMass = body1.GetInverseMass(j1) + body2.GetInverseMass(j2);
        if (data.EffectiveMass <= 0)
        {
            data.EffectiveMass = 0;
            data.AccumulatedImpulse = 0;
            return;
        }

        data.EffectiveMass = (Real)1.0 / data.EffectiveMass;
        data.MaxLambda = ((Real)1.0 / idt) * data.MaxForce;

        body1.Velocity -= JVector.Multiply(body1.InverseMass, j1 * data.AccumulatedImpulse);
        body2.Velocity += JVector.Multiply(body2.InverseMass, j2 * data.AccumulatedImpulse);
    }

    public override void DebugDraw(IDebugDrawer drawer)
    {
        ref LinearMotorData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector axis1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector axis2);

        const Real axisLength = (Real)0.5;
        drawer.DrawSegment(body1.Position, body1.Position + axis1 * axisLength);
        drawer.DrawSegment(body2.Position, body2.Position + axis2 * axisLength);
    }

    public static void IterateLinearMotor(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, LinearMotorData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector j1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector j2);

        Real jv = -j1 * body1.Velocity + j2 * body2.Velocity;

        Real lambda = -(jv - data.Velocity) * data.EffectiveMass;

        Real oldAccumulated = data.AccumulatedImpulse;

        data.AccumulatedImpulse += lambda;

        data.AccumulatedImpulse = Math.Clamp(data.AccumulatedImpulse, -data.MaxLambda, data.MaxLambda);

        lambda = data.AccumulatedImpulse - oldAccumulated;

        body1.Velocity -= JVector.Multiply(body1.InverseMass, j1 * lambda);
        body2.Velocity += JVector.Multiply(body2.InverseMass, j2 * lambda);
    }
}
