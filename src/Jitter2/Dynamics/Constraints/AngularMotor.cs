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
/// Represents a motor that drives relative angular velocity between two axes fixed
/// in the reference frames of their respective bodies.
/// </summary>
public unsafe class AngularMotor : Constraint<AngularMotor.AngularMotorData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AngularMotorData
    {
        internal int _internal;
        public delegate*<ref ConstraintData, void> Iterate;
        public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

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

    protected override void Create()
    {
        Iterate = &IterateAngularMotor;
        PrepareForIteration = &PrepareForIterationAngularMotor;
        base.Create();
    }

    /// <summary>
    /// Initializes the motor with separate axes for each body.
    /// </summary>
    /// <param name="axis1">The motor axis on the first body in world space.</param>
    /// <param name="axis2">The motor axis on the second body in world space.</param>
    /// <remarks>
    /// Stores the axes in local frames. Both axes are normalized internally.
    /// Default values: <see cref="TargetVelocity"/> = 0, <see cref="MaximumForce"/> = 0.
    /// </remarks>
    public void Initialize(JVector axis1, JVector axis2)
    {
        VerifyNotZero();
        ref AngularMotorData data = ref Data;
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
    /// Initializes the motor with the same axis for both bodies.
    /// </summary>
    /// <param name="axis">The motor axis in world space, used for both bodies.</param>
    public void Initialize(JVector axis)
    {
        Initialize(axis, axis);
    }

    /// <summary>
    /// Gets or sets the target angular velocity in radians per second.
    /// </summary>
    /// <value>Default is 0.</value>
    public Real TargetVelocity
    {
        get => Data.Velocity;
        set => Data.Velocity = value;
    }

    /// <summary>
    /// Gets the motor axis on the first body in local space.
    /// </summary>
    public JVector LocalAxis1 => Data.LocalAxis1;

    /// <summary>
    /// Gets the motor axis on the second body in local space.
    /// </summary>
    public JVector LocalAxis2 => Data.LocalAxis2;

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
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            Data.MaxForce = value;
        }
    }

    public static void PrepareForIterationAngularMotor(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, AngularMotorData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector j1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector j2);

        data.EffectiveMass = JVector.Transform(j1, body1.InverseInertiaWorld) * j1 +
                             JVector.Transform(j2, body2.InverseInertiaWorld) * j2;
        data.EffectiveMass = (Real)1.0 / data.EffectiveMass;

        data.MaxLambda = (Real)1.0 / idt * data.MaxForce;

        body1.AngularVelocity -= JVector.Transform(j1 * data.AccumulatedImpulse, body1.InverseInertiaWorld);
        body2.AngularVelocity += JVector.Transform(j2 * data.AccumulatedImpulse, body2.InverseInertiaWorld);
    }

    public static void IterateAngularMotor(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, AngularMotorData>(ref constraint);

        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector j1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector j2);

        Real jv = -j1 * body1.AngularVelocity + j2 * body2.AngularVelocity;

        Real lambda = -(jv - data.Velocity) * data.EffectiveMass;

        Real oldAccumulated = data.AccumulatedImpulse;

        data.AccumulatedImpulse += lambda;

        data.AccumulatedImpulse = Math.Clamp(data.AccumulatedImpulse, -data.MaxLambda, data.MaxLambda);

        lambda = data.AccumulatedImpulse - oldAccumulated;

        body1.AngularVelocity -= JVector.Transform(j1 * lambda, body1.InverseInertiaWorld);
        body2.AngularVelocity += JVector.Transform(j2 * lambda, body2.InverseInertiaWorld);
    }

    public override void DebugDraw(IDebugDrawer drawer)
    {
        ref AngularMotorData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector axis1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector axis2);

        const Real axisLength = (Real)0.5;
        drawer.DrawSegment(body1.Position, body1.Position + axis1 * axisLength);
        drawer.DrawSegment(body2.Position, body2.Position + axis2 * axisLength);
    }
}