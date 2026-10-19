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
/// Drives body 2 toward a target orientation relative to body 1 using limited torque.
/// Controls three angular degrees of freedom; translation remains free.
/// </summary>
public unsafe class AngularServo : Constraint<AngularServo.AngularServoData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AngularServoData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JQuaternion TargetRelativeOrientation;
        public Real MaximumTorque;
        public Real Softness;
        public Real BiasFactor;
        public JVector AccumulatedImpulse;
        public JVector Bias;
        public JMatrix EffectiveMass;
        public JMatrix Jacobian;
        public short Hemisphere;
    }

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationAngularServo, &IterateAngularServo);

    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = JVector.Zero;

    /// <summary>Initializes the servo with the current relative orientation as its target.</summary>
    public void Initialize()
    {
        VerifyNotZero();
        Initialize(Body1.Orientation.Conjugate() * Body2.Orientation);
    }

    /// <summary>Initializes a target orientation for body 2 relative to body 1.</summary>
    /// <param name="targetRelativeOrientation">A normalized orientation in body 1's frame.</param>
    /// <remarks>The bodies may start outside the target pose. Maximum torque defaults to zero.</remarks>
    public void Initialize(JQuaternion targetRelativeOrientation)
    {
        VerifyNotZero();
        ArgumentCheck.UnitQuaternion(targetRelativeOrientation, nameof(targetRelativeOrientation));
        ref var data = ref Data;
        data.TargetRelativeOrientation = targetRelativeOrientation;
        data.MaximumTorque = 0;
        data.Softness = Constraint.DefaultAngularSoftness;
        data.BiasFactor = Constraint.DefaultAngularBias;
        data.Hemisphere = 0;
        ResetWarmStart();
    }

    /// <summary>
    /// Gets or sets body 2's target orientation relative to body 1. Identity aligns their local frames.
    /// The value must be normalized. Changing it does not move either body.
    /// </summary>
    /// <remarks>The servo uses the shortest rotation to the target orientation. Revolution counts are not supported.</remarks>
    public JQuaternion TargetRelativeOrientation
    {
        get => Data.TargetRelativeOrientation;
        set
        {
            Data.TargetRelativeOrientation = ArgumentCheck.UnitQuaternion(value, nameof(value));
            Data.Hemisphere = 0;
            ResetWarmStart();
            ServoMath.WakeBodies(this);
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

    /// <summary>Gets the world-space angular impulse applied to body 1 in the last solved substep.</summary>
    public JVector Impulse => JVector.TransposedTransform(Data.AccumulatedImpulse, Data.Jacobian);

    public static void PrepareForIterationAngularServo(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, AngularServoData>(ref constraint);
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        data.Jacobian = -(Real)2 * AngularConstraintMath.CalculateJacobian(
            data.TargetRelativeOrientation.Conjugate() * body1.Orientation.Conjugate(),
            body2.Orientation, out JVector error, out short hemisphere);
        if (data.Hemisphere != 0 && data.Hemisphere != hemisphere) data.AccumulatedImpulse = JVector.Zero;
        data.Hemisphere = hemisphere;

        data.EffectiveMass = JSymmetricMatrix.Transform(body1.InverseInertiaWorld + body2.InverseInertiaWorld,
            data.Jacobian).ToMatrix();
        Real softness = data.Softness * timeStep.InverseSubstepDt;
        data.EffectiveMass.M11 += softness;
        data.EffectiveMass.M22 += softness;
        data.EffectiveMass.M33 += softness;
        data.EffectiveMass = MathHelper.InverseSymmetric(data.EffectiveMass);
        data.Bias = -error * data.BiasFactor * timeStep.InverseStepDt;

        ServoMath.ClampAngular(ref data.AccumulatedImpulse, data.Jacobian,
            ServoMath.MaximumImpulse(data.MaximumTorque, timeStep.SubstepDt));
        ApplyImpulse(ref data, JVector.TransposedTransform(data.AccumulatedImpulse, data.Jacobian));
    }

    public static void IterateAngularServo(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, AngularServoData>(ref constraint);
        JVector jv = JVector.Transform(data.Body1.Data.AngularVelocity - data.Body2.Data.AngularVelocity, data.Jacobian);
        JVector softness = data.AccumulatedImpulse * (data.Softness * timeStep.InverseSubstepDt);
        JVector lambda = -JVector.Transform(jv + data.Bias + softness, data.EffectiveMass);
        JVector previous = data.AccumulatedImpulse;
        data.AccumulatedImpulse += lambda;
        ServoMath.ClampAngular(ref data.AccumulatedImpulse, data.Jacobian,
            ServoMath.MaximumImpulse(data.MaximumTorque, timeStep.SubstepDt));
        ApplyImpulse(ref data, JVector.TransposedTransform(data.AccumulatedImpulse - previous, data.Jacobian));
    }

    private static void ApplyImpulse(ref AngularServoData data, in JVector impulse)
    {
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;
        if (body1.MotionType == MotionType.Dynamic)
            body1.AngularVelocity += JVector.Transform(impulse, body1.InverseInertiaWorld);
        if (body2.MotionType == MotionType.Dynamic)
            body2.AngularVelocity -= JVector.Transform(impulse, body2.InverseInertiaWorld);
    }

    public override void DebugDraw(IDebugDrawer drawer) => DebugDraw(drawer, DebugDrawSettings.Default);

    /// <inheritdoc />
    public override void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        VerifyNotZero();
        JVector origin = GetDebugDrawOrigin(settings);
        drawer.DrawFrame(origin, Body1.Orientation * TargetRelativeOrientation, settings with
        {
            AxisXColor = settings.ReferenceColor,
            AxisYColor = settings.ReferenceColor,
            AxisZColor = settings.ReferenceColor
        });
        drawer.DrawFrame(origin, Body2.Orientation, settings);
    }
}
