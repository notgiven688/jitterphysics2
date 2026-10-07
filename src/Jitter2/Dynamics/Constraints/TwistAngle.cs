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
/// Constrains the relative twist of two bodies. This constraint removes one angular
/// degree of freedom when the limit is enforced.
/// </summary>
public unsafe class TwistAngle : Constraint<TwistAngle.TwistLimitData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TwistLimitData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector B;

        public JQuaternion Q0;

        public Real Angle1, Angle2, FixedAngle;
        public ushort Clamp;
        public short Hemisphere;

        public Real BiasFactor;
        public Real Softness;

        public Real EffectiveMass;
        public Real AccumulatedImpulse;
        public Real Bias;

        public JVector Jacobian;
    }

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationTwistAngle, &IterateTwistAngle);

    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = (Real)0.0;

    /// <summary>
    /// Initializes the constraint from world-space axes and angular limits.
    /// </summary>
    /// <param name="axis1">The twist axis for body 1 in world space.</param>
    /// <param name="axis2">The twist axis for body 2 in world space.</param>
    /// <param name="limit">The allowed relative twist angle range.</param>
    /// <remarks>
    /// Stores each axis in the local frame of its body and records the initial relative orientation.
    /// Equal or reversed limits fix the twist at their midpoint.
    /// Default values: <see cref="Softness"/> = <see cref="Constraint.DefaultAngularSoftness"/>, <see cref="Bias"/> = <see cref="Constraint.DefaultAngularBias"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="axis1"/> or <paramref name="axis2"/> is zero or contains a non-finite value,
    /// or when either value in <paramref name="limit"/> is not finite.
    /// </exception>
    public void Initialize(JVector axis1, JVector axis2, AngularLimit limit)
    {
        VerifyNotZero();
        ArgumentCheck.NonZero(axis1, nameof(axis1));
        ArgumentCheck.NonZero(axis2, nameof(axis2));
        ArgumentCheck.Finite(limit.From, nameof(limit.From));
        ArgumentCheck.Finite(limit.To, nameof(limit.To));

        ref TwistLimitData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        data.Softness = Constraint.DefaultAngularSoftness;
        data.BiasFactor = Constraint.DefaultAngularBias;

        JVector.NormalizeInPlace(ref axis1);
        JVector.NormalizeInPlace(ref axis2);

        data.Angle1 = (Real)limit.From;
        data.Angle2 = (Real)limit.To;
        data.FixedAngle = (Real)0.5 * (Real)limit.From + (Real)0.5 * (Real)limit.To;

        // Calculate local axes
        JVector u1 = JVector.ConjugatedTransform(axis1, body1.Orientation);
        data.B = JVector.ConjugatedTransform(axis2, body2.Orientation);

        // 1. Calculate the initial relative orientation (Body1 -> Body2)
        JQuaternion q1 = body1.Orientation;
        JQuaternion q2 = body2.Orientation;
        JQuaternion qRel = q2.Conjugate() * q1;

        // 2. Map u1 into Body2 space using the initial orientation
        JVector u1InB2 = JVector.Transform(u1, qRel);

        // 3. Calculate the correction rotation to align u1_in_B2 to data.B (u2)
        // This calculates the 'swing' offset between the two axes
        JQuaternion qCorrection = JQuaternion.CreateFromToRotation(u1InB2, data.B);

        // 4. Apply correction to Q0.
        // Q0 now represents a reference orientation where the axes are perfectly aligned.
        data.Q0 = qCorrection * qRel;
    }

    /// <summary>
    /// Sets the angular limits for the twist rotation.
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

            ref TwistLimitData data = ref Data;
            data.Angle1 = (Real)value.From;
            data.Angle2 = (Real)value.To;
            data.FixedAngle = (Real)0.5 * (Real)value.From + (Real)0.5 * (Real)value.To;
        }
    }

    /// <summary>
    /// Initializes the constraint with a fixed twist angle (no rotation allowed).
    /// </summary>
    /// <param name="axis1">The twist axis for body 1 in world space.</param>
    /// <param name="axis2">The twist axis for body 2 in world space.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="axis1"/> or <paramref name="axis2"/> is zero or contains a non-finite value.
    /// </exception>
    public void Initialize(JVector axis1, JVector axis2)
    {
        Initialize(axis1, axis2, AngularLimit.Fixed);
    }

    public static void PrepareForIterationTwistAngle(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, TwistLimitData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JQuaternion q1 = body1.Orientation;
        JQuaternion q2 = body2.Orientation;

        JMatrix m = (-(Real)(1.0 / 2.0)) * QMatrix.ProjectMultiplyLeftRight(data.Q0 * q1.Conjugate(), q2);

        JQuaternion q = data.Q0 * q1.Conjugate() * q2;

        data.Jacobian = JVector.TransposedTransform(data.B, m);

        Real error = JVector.Dot(data.B, new JVector(q.X, q.Y, q.Z));
        Real lower = data.Angle1, upper = data.Angle2, target = data.FixedAngle;

        short hemisphere = q.W < 0 ? (short)-1 : (short)1;
        if (data.Hemisphere != 0 && data.Hemisphere != hemisphere) data.AccumulatedImpulse = 0;
        data.Hemisphere = hemisphere;

        if (q.W < (Real)0.0)
        {
            q *= -1;
            error *= -(Real)1.0;
            data.Jacobian *= -1;
        }

        // Use the projection angle throughout: its angular derivative has unit length.
        // Mixing this coordinate with sin(angle / 2) changes both softness and impulse units.
        Real derivativeSquared = data.Jacobian.LengthSquared();
        if (derivativeSquared > 0)
        {
            data.Jacobian *= (Real)1.0 / MathR.Sqrt(derivativeSquared);
        }
        else
        {
            // The projection angle has a cusp at exactly a half turn. Choose its
            // twist direction, which is a valid one-sided derivative at this pose.
            data.Jacobian = -JVector.Transform(data.B, q2);
        }

        JVector perpendicular = q.Vector - data.B * error;
        Real cosine = MathR.Sqrt(q.W * q.W + perpendicular.LengthSquared());
        error = (Real)2.0 * StableMath.Atan2(error, cosine);

        data.Clamp = 0;
        bool fullRange = lower <= -MathR.PI && upper >= MathR.PI;

        if (lower >= upper)
        {
            data.Clamp = 3;
            error = AngularConstraintMath.ShortestAngle(error - target, MathR.PI);
        }
        else if (!fullRange && error >= upper)
        {
            data.Clamp = 1;
            error -= upper;
        }
        else if (!fullRange && error <= lower)
        {
            data.Clamp = 2;
            error -= lower;
        }
        else
        {
            data.AccumulatedImpulse = (Real)0.0;
            return;
        }

        if (data.Clamp == 1) data.AccumulatedImpulse = MathR.Min(data.AccumulatedImpulse, 0);
        else if (data.Clamp == 2) data.AccumulatedImpulse = MathR.Max(data.AccumulatedImpulse, 0);

        data.EffectiveMass = JVector.Transform(data.Jacobian, body1.InverseInertiaWorld + body2.InverseInertiaWorld) * data.Jacobian;
        data.EffectiveMass += data.Softness * timeStep.InverseSubstepDt;
        data.EffectiveMass = data.EffectiveMass > 0 ? (Real)1.0 / data.EffectiveMass : 0;
        if (data.EffectiveMass == 0) data.AccumulatedImpulse = 0;

        data.Bias = error * data.BiasFactor * timeStep.InverseStepDt;

        body1.AngularVelocity += JVector.Transform(data.AccumulatedImpulse * data.Jacobian, body1.InverseInertiaWorld);
        body2.AngularVelocity -= JVector.Transform(data.AccumulatedImpulse * data.Jacobian, body2.InverseInertiaWorld);
    }

    /// <summary>
    /// Gets the current twist angle relative to the initial pose.
    /// </summary>
    public JAngle Angle
    {
        get
        {
            ref var data = ref Data;
            JQuaternion q1 = data.Body1.Data.Orientation;
            JQuaternion q2 = data.Body2.Data.Orientation;

            JQuaternion quat0 = data.Q0 * q1.Conjugate() * q2;

            if (quat0.W < (Real)0.0)
            {
                quat0 *= -(Real)1.0;
            }

            Real projection = JVector.Dot(data.B, quat0.Vector);
            JVector perpendicular = quat0.Vector - data.B * projection;
            return (JAngle)((Real)2.0 * StableMath.Atan2(projection,
                MathR.Sqrt(quat0.W * quat0.W + perpendicular.LengthSquared())));
        }
    }

    /// <summary>
    /// Gets or sets the softness (compliance) of the constraint.
    /// </summary>
    /// <value>
    /// Default is <see cref="Constraint.DefaultAngularSoftness"/>. Higher values allow more angular error but improve stability.
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
    /// Gets the accumulated solver impulse from the last solved substep.
    /// </summary>
    public Real Impulse => Data.AccumulatedImpulse;

    public override void DebugDraw(IDebugDrawer drawer)
    {
        ref TwistLimitData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.B, body2.Orientation, out JVector axis);

        const Real axisLength = (Real)0.5;
        drawer.DrawSegment(body1.Position, body1.Position + axis * axisLength);
        drawer.DrawSegment(body2.Position, body2.Position + axis * axisLength);
    }

    public static void IterateTwistAngle(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, TwistLimitData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        if (data.Clamp == 0) return;

        Real jv = (body1.AngularVelocity - body2.AngularVelocity) * data.Jacobian;

        Real softnessScalar = data.AccumulatedImpulse * (data.Softness * timeStep.InverseSubstepDt);

        Real lambda = -data.EffectiveMass * (jv + data.Bias + softnessScalar);

        Real origAcc = data.AccumulatedImpulse;
        data.AccumulatedImpulse += lambda;

        if (data.Clamp == 1)
        {
            data.AccumulatedImpulse = MathR.Min(data.AccumulatedImpulse, (Real)0.0);
        }
        else if (data.Clamp == 2)
        {
            data.AccumulatedImpulse = MathR.Max(data.AccumulatedImpulse, (Real)0.0);
        }

        lambda = data.AccumulatedImpulse - origAcc;

        body1.AngularVelocity += JVector.Transform(lambda * data.Jacobian, body1.InverseInertiaWorld);
        body2.AngularVelocity -= JVector.Transform(lambda * data.Jacobian, body2.InverseInertiaWorld);
    }
}
