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
/// Constrains two bodies to rotate relative to each other around a single axis,
/// removing two angular degrees of freedom. Optionally enforces angular limits.
/// </summary>
public unsafe class HingeAngle : Constraint<HingeAngle.HingeAngleData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HingeAngleData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public Real MinAngle;
        public Real MaxAngle;
        public Real FixedAngle;

        public Real BiasFactor;
        public Real LimitBias;

        public Real LimitSoftness;
        public Real Softness;

        public JVector Axis;
        public JQuaternion Q0;

        public JVector AccumulatedImpulse;
        public JVector Bias;

        public JMatrix EffectiveMass;
        public JMatrix Jacobian;

        public JVector BilateralMass;
        public Real CouplingX, CouplingY;

        public ushort Clamp;
        public short Hemisphere;
    }

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationHingeAngle, &IterateHingeAngle);

    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = JVector.Zero;

    /// <summary>
    /// Initializes the constraint with a rotation axis and angular limits.
    /// </summary>
    /// <param name="axis">The hinge axis in world space around which rotation is allowed.</param>
    /// <param name="limit">The angular limits defining the allowed rotation range.</param>
    /// <remarks>
    /// Stores the axis in the local frame of body 2 and records the initial relative orientation.
    /// Equal or reversed limits fix the hinge at their midpoint.
    /// Default values: <see cref="Softness"/> = <see cref="Constraint.DefaultAngularSoftness"/>, <see cref="LimitSoftness"/> = <see cref="Constraint.DefaultAngularLimitSoftness"/>,
    /// <see cref="Bias"/> = <see cref="Constraint.DefaultAngularBias"/>, <see cref="LimitBias"/> = <see cref="Constraint.DefaultAngularLimitBias"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="axis"/> is zero or contains a non-finite value, or when
    /// either value in <paramref name="limit"/> is not finite.
    /// </exception>
    public void Initialize(JVector axis, AngularLimit limit)
    {
        VerifyNotZero();
        ArgumentCheck.NonZero(axis, nameof(axis));
        ArgumentCheck.Finite(limit.From, nameof(limit.From));
        ArgumentCheck.Finite(limit.To, nameof(limit.To));

        ref HingeAngleData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        data.Softness = Constraint.DefaultAngularSoftness;
        data.LimitSoftness = Constraint.DefaultAngularLimitSoftness;
        data.BiasFactor = Constraint.DefaultAngularBias;
        data.LimitBias = Constraint.DefaultAngularLimitBias;

        data.MinAngle = (Real)limit.From / (Real)2.0;
        data.MaxAngle = (Real)limit.To / (Real)2.0;
        data.FixedAngle = (Real)0.25 * (Real)limit.From + (Real)0.25 * (Real)limit.To;

        JVector.NormalizeInPlace(ref axis);
        data.Axis = JVector.ConjugatedTransform(axis, body2.Orientation);

        JQuaternion q1 = body1.Orientation;
        JQuaternion q2 = body2.Orientation;

        data.Q0 = q2.Conjugate() * q1;
    }

    /// <summary>
    /// Sets the angular limits for the hinge rotation.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when either assigned limit value is not finite.
    /// </exception>
    public AngularLimit Limit
    {
        set
        {
            ArgumentCheck.Finite(value.From, nameof(value.From));
            ArgumentCheck.Finite(value.To, nameof(value.To));

            ref HingeAngleData data = ref Data;
            data.MinAngle = (Real)value.From / (Real)2.0;
            data.MaxAngle = (Real)value.To / (Real)2.0;
            data.FixedAngle = (Real)0.25 * (Real)value.From + (Real)0.25 * (Real)value.To;
        }
    }

    public static void PrepareForIterationHingeAngle(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, HingeAngleData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JQuaternion q1 = body1.Orientation;
        JQuaternion q2 = body2.Orientation;

        JVector p0 = MathHelper.CreateOrthonormal(data.Axis);
        JVector p1 = data.Axis % p0;

        JMatrix m0 = AngularConstraintMath.CalculateJacobian(
            data.Q0 * q1.Conjugate(), q2, out JVector rotation, out short hemisphere);
        if (data.Hemisphere != 0 && data.Hemisphere != hemisphere)
            data.AccumulatedImpulse = JVector.Zero;
        data.Hemisphere = hemisphere;
        JVector error = new(JVector.Dot(p0, rotation), JVector.Dot(p1, rotation),
            JVector.Dot(data.Axis, rotation));

        data.Clamp = 0;

        data.Jacobian.UnsafeGet(0) = JVector.TransposedTransform(p0, m0);
        data.Jacobian.UnsafeGet(1) = JVector.TransposedTransform(p1, m0);
        data.Jacobian.UnsafeGet(2) = JVector.TransposedTransform(data.Axis, m0);

        data.EffectiveMass = JSymmetricMatrix.Transform(body1.InverseInertiaWorld + body2.InverseInertiaWorld,
            JMatrix.Transpose(data.Jacobian)).ToMatrix();

        data.EffectiveMass.M11 += data.Softness * timeStep.InverseSubstepDt;
        data.EffectiveMass.M22 += data.Softness * timeStep.InverseSubstepDt;
        data.EffectiveMass.M33 += data.LimitSoftness * timeStep.InverseSubstepDt;

        Real maxA = data.MaxAngle;
        Real minA = data.MinAngle;
        bool fullRange = minA <= -MathR.PI / 2 && maxA >= MathR.PI / 2;

        if (minA >= maxA)
        {
            data.Clamp = 3;
            error.Z = AngularConstraintMath.ShortestAngle(error.Z - data.FixedAngle, MathR.PI / 2);
        }
        else if (!fullRange && error.Z >= maxA)
        {
            data.Clamp = 1;
            error.Z -= maxA;
        }
        else if (!fullRange && error.Z <= minA)
        {
            data.Clamp = 2;
            error.Z -= minA;
        }
        else
        {
            data.AccumulatedImpulse.Z = 0;
            data.EffectiveMass.M33 = 0;
            data.EffectiveMass.M31 = data.EffectiveMass.M13 = 0;
            data.EffectiveMass.M32 = data.EffectiveMass.M23 = 0;

            data.Jacobian.M13 = data.Jacobian.M23 = data.Jacobian.M33 = 0;
        }

        if (data.Clamp == 1 || data.Clamp == 2)
        {
            data.AccumulatedImpulse.Z = data.Clamp == 1 ?
                MathR.Min(data.AccumulatedImpulse.Z, 0) : MathR.Max(data.AccumulatedImpulse.Z, 0);
            data.BilateralMass = MathHelper.InverseBilateralBlock(data.EffectiveMass);
            data.CouplingX = data.EffectiveMass.M13;
            data.CouplingY = data.EffectiveMass.M23;
        }

        data.EffectiveMass = MathHelper.InverseSymmetric(data.EffectiveMass);

        data.Bias = error * timeStep.InverseStepDt;
        data.Bias.X *= data.BiasFactor;
        data.Bias.Y *= data.BiasFactor;
        data.Bias.Z *= data.LimitBias;

        body1.AngularVelocity += JVector.Transform(JVector.Transform(data.AccumulatedImpulse, data.Jacobian), body1.InverseInertiaWorld);
        body2.AngularVelocity -= JVector.Transform(JVector.Transform(data.AccumulatedImpulse, data.Jacobian), body2.InverseInertiaWorld);
    }

    /// <summary>
    /// Gets the current angle of rotation around the hinge axis relative to the initial pose.
    /// </summary>
    /// <remarks>
    /// When the hinge axes are misaligned, this is the projection of the shortest
    /// relative rotation vector onto the hinge axis, matching the limit coordinate.
    /// </remarks>
    public JAngle Angle
    {
        get
        {
            ref HingeAngleData data = ref Data;
            JQuaternion q1 = data.Body1.Data.Orientation;
            JQuaternion q2 = data.Body2.Data.Orientation;

            JQuaternion quat0 = data.Q0 * q1.Conjugate() * q2;

            return (JAngle)((Real)2.0 * JVector.Dot(data.Axis,
                AngularConstraintMath.RotationLog(quat0)));
        }
    }

    /// <summary>
    /// Gets or sets the softness (compliance) of the angular constraint.
    /// </summary>
    /// <value>
    /// Default is 0.001. Higher values allow more angular error but improve stability.
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
    /// Gets or sets the softness (compliance) applied when angular limits are active.
    /// </summary>
    /// <value>
    /// Default is 0.001. Higher values allow more limit violation but improve stability.
    /// </value>
    public Real LimitSoftness
    {
        get => Data.LimitSoftness;
        set
        {
            DebugCheck.IsNonNegative(value, nameof(value));
            Data.LimitSoftness = value;
        }
    }

    /// <summary>
    /// Gets or sets the bias factor controlling how aggressively angular error is corrected.
    /// </summary>
    /// <value>
    /// Default is 0.2. Higher values correct errors faster but may cause instability.
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
    /// Gets or sets the bias factor for angular limit correction.
    /// </summary>
    /// <value>
    /// Default is 0.1. Higher values correct limit violations faster.
    /// </value>
    public Real LimitBias
    {
        get => Data.LimitBias;
        set
        {
            DebugCheck.IsNonNegative(value, nameof(value));
            Data.LimitBias = value;
        }
    }

    /// <summary>
    /// Gets the accumulated solver impulse from the last solved substep.
    /// </summary>
    public JVector Impulse => Data.AccumulatedImpulse;

    public static void IterateHingeAngle(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, HingeAngleData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        JVector jv = JVector.TransposedTransform(body1.AngularVelocity - body2.AngularVelocity, data.Jacobian);

        JVector softness = data.AccumulatedImpulse * timeStep.InverseSubstepDt;
        softness.X *= data.Softness;
        softness.Y *= data.Softness;
        softness.Z *= data.LimitSoftness;

        JVector residual = jv + data.Bias + softness;
        JVector lambda = -(Real)1.0 * JVector.Transform(residual, data.EffectiveMass);

        JVector origAcc = data.AccumulatedImpulse;

        data.AccumulatedImpulse += lambda;

        if ((data.Clamp == 1 && data.AccumulatedImpulse.Z > 0) ||
            (data.Clamp == 2 && data.AccumulatedImpulse.Z < 0))
        {
            // The coupled block assumed its limit impulse would be applied.
            // At the unilateral bound, solve the bilateral rows again with that
            // impulse fixed, including any removal of the cached limit impulse.
            lambda.Z = -origAcc.Z;
            Real x = residual.X + data.CouplingX * lambda.Z;
            Real y = residual.Y + data.CouplingY * lambda.Z;
            lambda.X = -(data.BilateralMass.X * x + data.BilateralMass.Y * y);
            lambda.Y = -(data.BilateralMass.Y * x + data.BilateralMass.Z * y);
            data.AccumulatedImpulse.X = origAcc.X + lambda.X;
            data.AccumulatedImpulse.Y = origAcc.Y + lambda.Y;
            data.AccumulatedImpulse.Z = 0;
        }
        else if (data.Clamp == 0)
        {
            origAcc.Z = 0;
            data.AccumulatedImpulse.Z = 0;
        }

        lambda = data.AccumulatedImpulse - origAcc;

        body1.AngularVelocity += JVector.Transform(JVector.Transform(lambda, data.Jacobian), body1.InverseInertiaWorld);
        body2.AngularVelocity -= JVector.Transform(JVector.Transform(lambda, data.Jacobian), body2.InverseInertiaWorld);
    }

    public override void DebugDraw(IDebugDrawer drawer)
    {
        ref HingeAngleData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.Axis, body2.Orientation, out JVector axis);

        const Real axisLength = (Real)0.5;
        drawer.DrawSegment(body1.Position, body1.Position + axis * axisLength);
        drawer.DrawSegment(body2.Position, body2.Position + axis * axisLength);
    }
}
