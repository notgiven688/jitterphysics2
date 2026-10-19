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
/// Drives relative twist toward a target angle using limited torque.
/// Controls one angular degree of freedom; translation and swing remain free.
/// </summary>
/// <remarks>
/// Uses signed joint angles in [-pi, pi], without tracking revolutions.
/// Combine with mechanical limits that keep motion inside this angular interval.
/// </remarks>
public unsafe class TwistServo : Constraint<TwistServo.TwistServoData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TwistServoData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAxis2;
        public JQuaternion ReferenceOrientation;
        public Real TargetAngle;
        public Real MaximumTorque;
        public Real Softness;
        public Real BiasFactor;
        public Real EffectiveMass;
        public Real AccumulatedImpulse;
        public Real Bias;
        public JVector Jacobian;
        public short Hemisphere;
    }

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationTwistServo, &IterateTwistServo);

    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = 0;

    /// <summary>Initializes both twist axes from the same world-space direction.</summary>
    public void Initialize(JVector axis, JAngle targetAngle = default) => Initialize(axis, axis, targetAngle);

    /// <summary>Initializes separate world-space twist axes and a target relative to the initial pose.</summary>
    /// <remarks>
    /// The shortest rotation aligning the two axes defines zero twist. Maximum torque defaults to zero.
    /// A nonzero target angle starts the servo outside its target configuration.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The target angle is not finite or is outside [-pi, pi].</exception>
    public void Initialize(JVector axis1, JVector axis2, JAngle targetAngle = default)
    {
        VerifyNotZero();
        ArgumentCheck.NonZero(axis1, nameof(axis1));
        ArgumentCheck.NonZero(axis2, nameof(axis2));
        Real target = ArgumentCheck.InRange((Real)targetAngle, -MathR.PI, MathR.PI, nameof(targetAngle));
        JVector.NormalizeInPlace(ref axis1);
        JVector.NormalizeInPlace(ref axis2);
        ref var data = ref Data;
        data.LocalAxis2 = JVector.ConjugatedTransform(axis2, Body2.Orientation);
        JVector firstInBody2 = JVector.ConjugatedTransform(axis1, Body2.Orientation);
        data.ReferenceOrientation = JQuaternion.CreateFromToRotation(firstInBody2, data.LocalAxis2) *
                                    (Body2.Orientation.Conjugate() * Body1.Orientation);
        data.TargetAngle = target;
        data.MaximumTorque = 0;
        data.Softness = Constraint.DefaultAngularSoftness;
        data.BiasFactor = Constraint.DefaultAngularBias;
        data.Hemisphere = 0;
        ResetWarmStart();
    }

    /// <summary>Gets or sets the desired twist angle relative to the initial zero-twist pose.</summary>
    /// <remarks>
    /// The target must be in [-pi, pi]. The signed angle error is not wrapped: moving from
    /// -170 to +170 degrees travels through zero. Revolution counts are not supported.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite or is outside [-pi, pi].</exception>
    public JAngle TargetAngle
    {
        get => (JAngle)Data.TargetAngle;
        set
        {
            Data.TargetAngle = ArgumentCheck.InRange((Real)value, -MathR.PI, MathR.PI, nameof(value));
            ResetWarmStart();
            ServoMath.WakeBodies(this);
        }
    }

    /// <summary>Gets the current relative twist angle in [-pi, pi]. Revolutions are not tracked.</summary>
    public JAngle Angle
    {
        get
        {
            ref var data = ref Data;
            return (JAngle)AngularConstraintMath.CalculateTwist(
                data.ReferenceOrientation * Body1.Orientation.Conjugate(), Body2.Orientation,
                data.LocalAxis2, out _, out _);
        }
    }

    /// <summary>Gets or sets the maximum total torque the servo can apply.</summary>
    /// <value>Default is zero. Positive infinity allows unlimited torque.</value>
    public Real MaximumTorque
    {
        get => Data.MaximumTorque;
        set
        {
            Data.MaximumTorque = ServoMath.CheckMaximum(value);
            ServoMath.WakeBodies(this);
        }
    }

    /// <summary>Gets or sets the softness (compliance) of the servo.</summary>
    /// <value>Default is <see cref="Constraint.DefaultAngularSoftness"/>.</value>
    public Real Softness
    {
        get => Data.Softness;
        set => Data.Softness = ArgumentCheck.NonNegative(value, nameof(value));
    }

    /// <summary>Gets or sets how aggressively the servo corrects its position error.</summary>
    /// <value>Default is <see cref="Constraint.DefaultAngularBias"/>.</value>
    public Real Bias
    {
        get => Data.BiasFactor;
        set => Data.BiasFactor = ArgumentCheck.NonNegative(value, nameof(value));
    }

    /// <summary>Gets the signed angular impulse in the last solved substep.</summary>
    public Real Impulse => Data.AccumulatedImpulse;

    public static void PrepareForIterationTwistServo(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, TwistServoData>(ref constraint);
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;
        Real angle = AngularConstraintMath.CalculateTwist(data.ReferenceOrientation * body1.Orientation.Conjugate(),
            body2.Orientation, data.LocalAxis2, out data.Jacobian, out short hemisphere);
        if (data.Hemisphere != 0 && data.Hemisphere != hemisphere) data.AccumulatedImpulse = 0;
        data.Hemisphere = hemisphere;
        Real mass = JVector.Transform(data.Jacobian, body1.InverseInertiaWorld + body2.InverseInertiaWorld) * data.Jacobian +
                    data.Softness * timeStep.InverseSubstepDt;
        data.EffectiveMass = mass > 0 ? (Real)1 / mass : 0;
        if (data.EffectiveMass == 0) data.AccumulatedImpulse = 0;
        // A joint-angle target must be reached through its angular interval, even when
        // crossing the -pi/pi boundary would give a shorter rotation.
        Real error = angle - data.TargetAngle;
        data.Bias = error * data.BiasFactor * timeStep.InverseStepDt;
        Real maximum = ServoMath.MaximumImpulse(data.MaximumTorque, timeStep.SubstepDt);
        data.AccumulatedImpulse = Math.Clamp(data.AccumulatedImpulse, -maximum, maximum);
        ApplyImpulse(ref data, data.AccumulatedImpulse);
    }

    public static void IterateTwistServo(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, TwistServoData>(ref constraint);
        Real jv = (data.Body1.Data.AngularVelocity - data.Body2.Data.AngularVelocity) * data.Jacobian;
        Real lambda = -data.EffectiveMass * (jv + data.Bias +
            data.AccumulatedImpulse * data.Softness * timeStep.InverseSubstepDt);
        Real previous = data.AccumulatedImpulse;
        Real maximum = ServoMath.MaximumImpulse(data.MaximumTorque, timeStep.SubstepDt);
        data.AccumulatedImpulse = Math.Clamp(previous + lambda, -maximum, maximum);
        ApplyImpulse(ref data, data.AccumulatedImpulse - previous);
    }

    private static void ApplyImpulse(ref TwistServoData data, Real impulse)
    {
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;
        if (body1.MotionType == MotionType.Dynamic)
            body1.AngularVelocity += JVector.Transform(data.Jacobian * impulse, body1.InverseInertiaWorld);
        if (body2.MotionType == MotionType.Dynamic)
            body2.AngularVelocity -= JVector.Transform(data.Jacobian * impulse, body2.InverseInertiaWorld);
    }

    public override void DebugDraw(IDebugDrawer drawer) => DebugDraw(drawer, DebugDrawSettings.Default);

    /// <inheritdoc />
    public override void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        VerifyNotZero();
        ref var data = ref Data;
        JQuaternion reference = Body1.Orientation * data.ReferenceOrientation.Conjugate();
        JVector axis = JVector.Transform(data.LocalAxis2, reference);
        JVector direction = JVector.Transform(MathHelper.CreateOrthonormal(data.LocalAxis2), reference);
        DebugDrawGeometry.AngularRange(drawer, GetDebugDrawOrigin(settings), axis, direction,
            data.TargetAngle, data.TargetAngle, Angle.Radian, settings);
    }
}
