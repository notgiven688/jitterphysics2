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
/// Drives an anchor on body 2 toward a target position attached to body 1 using limited force.
/// Controls three translational degrees of freedom; relative orientation remains free.
/// </summary>
public unsafe class LinearServo : Constraint<LinearServo.LinearServoData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LinearServoData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAnchor1;
        public JVector LocalAnchor2;
        public JVector TargetPosition;
        public Real MaximumForce;
        public Real Softness;
        public Real BiasFactor;
        public JVector R1, R2;
        public JVector AccumulatedImpulse;
        public JVector Bias;
        public JMatrix EffectiveMass;
    }

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationLinearServo, &IterateLinearServo);

    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = JVector.Zero;

    /// <summary>Initializes both body anchors at the same world-space point.</summary>
    public void Initialize(JVector anchor) => Initialize(anchor, anchor);

    /// <summary>Initializes separate world-space anchors that the servo drives toward coincidence.</summary>
    /// <remarks>Target position and maximum force default to zero. The anchors may start apart.</remarks>
    public void Initialize(JVector anchor1, JVector anchor2)
    {
        VerifyNotZero();
        ArgumentCheck.Finite(anchor1, nameof(anchor1));
        ArgumentCheck.Finite(anchor2, nameof(anchor2));
        ref var data = ref Data;
        data.LocalAnchor1 = JVector.ConjugatedTransform(anchor1 - Body1.Position, Body1.Orientation);
        data.LocalAnchor2 = JVector.ConjugatedTransform(anchor2 - Body2.Position, Body2.Orientation);
        data.TargetPosition = JVector.Zero;
        data.MaximumForce = 0;
        data.Softness = Constraint.DefaultLinearSoftness;
        data.BiasFactor = Constraint.DefaultLinearBias;
        ResetWarmStart();
    }

    /// <summary>Gets or sets the world-space anchor attached to body 1.</summary>
    [ReferenceFrame(ReferenceFrame.World)]
    public JVector Anchor1
    {
        get => Body1.Position + JVector.Transform(Data.LocalAnchor1, Body1.Orientation);
        set
        {
            ArgumentCheck.Finite(value, nameof(value));
            Data.LocalAnchor1 = JVector.ConjugatedTransform(value - Body1.Position, Body1.Orientation);
            ResetWarmStart();
            ServoMath.WakeBodies(this);
        }
    }

    /// <summary>Gets or sets the world-space anchor attached to body 2.</summary>
    [ReferenceFrame(ReferenceFrame.World)]
    public JVector Anchor2
    {
        get => Body2.Position + JVector.Transform(Data.LocalAnchor2, Body2.Orientation);
        set
        {
            ArgumentCheck.Finite(value, nameof(value));
            Data.LocalAnchor2 = JVector.ConjugatedTransform(value - Body2.Position, Body2.Orientation);
            ResetWarmStart();
            ServoMath.WakeBodies(this);
        }
    }

    /// <summary>
    /// Gets or sets the desired offset from anchor 1 to anchor 2, expressed in body 1's local frame.
    /// Zero drives the anchors toward coincidence. Changing it does not move either body.
    /// </summary>
    public JVector TargetPosition
    {
        get => Data.TargetPosition;
        set
        {
            Data.TargetPosition = ArgumentCheck.Finite(value, nameof(value));
            ResetWarmStart();
            ServoMath.WakeBodies(this);
        }
    }

    /// <summary>Gets or sets the maximum total force the servo can apply.</summary>
    /// <value>Default is zero. Positive infinity allows unlimited force.</value>
    public Real MaximumForce
    {
        get => Data.MaximumForce;
        set
        {
            Data.MaximumForce = ServoMath.CheckMaximum(value);
            ServoMath.WakeBodies(this);
        }
    }

    /// <summary>Gets or sets the softness (compliance) of the servo.</summary>
    /// <value>Default is <see cref="Constraint.DefaultLinearSoftness"/>.</value>
    public Real Softness
    {
        get => Data.Softness;
        set => Data.Softness = ArgumentCheck.NonNegative(value, nameof(value));
    }

    /// <summary>Gets or sets how aggressively the servo corrects its position error.</summary>
    /// <value>Default is <see cref="Constraint.DefaultLinearBias"/>.</value>
    public Real Bias
    {
        get => Data.BiasFactor;
        set => Data.BiasFactor = ArgumentCheck.NonNegative(value, nameof(value));
    }

    /// <summary>Gets the world-space impulse applied to body 2 in the last solved substep.</summary>
    public JVector Impulse => Data.AccumulatedImpulse;

    public static void PrepareForIterationLinearServo(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, LinearServoData>(ref constraint);
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;
        data.R1 = JVector.Transform(data.LocalAnchor1 + data.TargetPosition, body1.Orientation);
        data.R2 = JVector.Transform(data.LocalAnchor2, body2.Orientation);
        JMatrix cr1 = JMatrix.CreateCrossProduct(data.R1), cr2 = JMatrix.CreateCrossProduct(data.R2);
        data.EffectiveMass = (JSymmetricMatrix.CreateScale(body1.InverseMassVector) +
                              JSymmetricMatrix.Transform(body1.InverseInertiaWorld, cr1) +
                              JSymmetricMatrix.CreateScale(body2.InverseMassVector) +
                              JSymmetricMatrix.Transform(body2.InverseInertiaWorld, cr2)).ToMatrix();
        Real softness = data.Softness * timeStep.InverseSubstepDt;
        data.EffectiveMass.M11 += softness;
        data.EffectiveMass.M22 += softness;
        data.EffectiveMass.M33 += softness;
        data.EffectiveMass = MathHelper.InverseSymmetric(data.EffectiveMass);
        data.Bias = (body2.Position + data.R2 - body1.Position - data.R1) *
                    (data.BiasFactor * timeStep.InverseStepDt);
        ServoMath.ClampMagnitude(ref data.AccumulatedImpulse,
            ServoMath.MaximumImpulse(data.MaximumForce, timeStep.SubstepDt));
        ApplyImpulse(ref data, data.AccumulatedImpulse);
    }

    public static void IterateLinearServo(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, LinearServoData>(ref constraint);
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;
        JVector jv = -body1.Velocity - (body1.AngularVelocity % data.R1) +
                     body2.Velocity + (body2.AngularVelocity % data.R2);
        JVector softness = data.AccumulatedImpulse * (data.Softness * timeStep.InverseSubstepDt);
        JVector lambda = -JVector.Transform(jv + data.Bias + softness, data.EffectiveMass);
        JVector previous = data.AccumulatedImpulse;
        data.AccumulatedImpulse += lambda;
        ServoMath.ClampMagnitude(ref data.AccumulatedImpulse,
            ServoMath.MaximumImpulse(data.MaximumForce, timeStep.SubstepDt));
        ApplyImpulse(ref data, data.AccumulatedImpulse - previous);
    }

    private static void ApplyImpulse(ref LinearServoData data, in JVector impulse)
    {
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;
        if (body1.MotionType == MotionType.Dynamic)
        {
            body1.Velocity -= JVector.Multiply(impulse, body1.InverseMassVector);
            body1.AngularVelocity -= JVector.Transform(data.R1 % impulse, body1.InverseInertiaWorld);
        }
        if (body2.MotionType == MotionType.Dynamic)
        {
            body2.Velocity += JVector.Multiply(impulse, body2.InverseMassVector);
            body2.AngularVelocity += JVector.Transform(data.R2 % impulse, body2.InverseInertiaWorld);
        }
    }

    public override void DebugDraw(IDebugDrawer drawer) => DebugDraw(drawer, DebugDrawSettings.Default);

    /// <inheritdoc />
    public override void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        VerifyNotZero();
        JVector target = Anchor1 + JVector.Transform(TargetPosition, Body1.Orientation);
        JVector current = Anchor2;
        drawer.DrawSegment(target, current, settings.CurrentColor);
        drawer.DrawPoint(target, settings.PointRadius, settings.ReferenceColor);
        drawer.DrawPoint(current, settings.PointRadius, settings.CurrentColor);
        if (Body2 != Body2.World.NullBody) drawer.DrawSegment(Body2.Position, current, settings.CurrentColor);
    }
}
