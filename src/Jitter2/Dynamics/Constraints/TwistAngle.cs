/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

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
        public delegate*<ref ConstraintData, void> Iterate;
        public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector B;

        public JQuaternion Q0;

        public Real Angle1, Angle2;
        public ushort Clamp;

        public Real BiasFactor;
        public Real Softness;

        public Real EffectiveMass;
        public Real AccumulatedImpulse;
        public Real Bias;

        public JVector Jacobian;
    }

    protected override void Create()
    {
        Iterate = &IterateTwistAngle;
        PrepareForIteration = &PrepareForIterationTwistAngle;
        base.Create();
    }

    /// <summary>
    /// Initializes the constraint from world-space axes and angular limits.
    /// </summary>
    /// <param name="axis1">The twist axis for body 1 in world space.</param>
    /// <param name="axis2">The twist axis for body 2 in world space.</param>
    /// <param name="limit">The allowed relative twist angle range.</param>
    /// <remarks>
    /// Stores each axis in the local frame of its body and records the initial relative orientation.
    /// Default values: <see cref="Softness"/> = 0.0001, <see cref="Bias"/> = 0.2.
    /// </remarks>
    public void Initialize(JVector axis1, JVector axis2, AngularLimit limit)
    {
        VerifyNotZero();
        ref TwistLimitData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        data.Softness = (Real)0.0001;
        data.BiasFactor = (Real)0.2;

        JVector.NormalizeInPlace(ref axis1);
        JVector.NormalizeInPlace(ref axis2);

        data.Angle1 = MathR.Sin((Real)limit.From / (Real)2.0);
        data.Angle2 = MathR.Sin((Real)limit.To / (Real)2.0);

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
    public AngularLimit Limit
    {
        set
        {
            ref TwistLimitData data = ref Data;
            data.Angle1 = MathR.Sin((Real)value.From / (Real)2.0);
            data.Angle2 = MathR.Sin((Real)value.To / (Real)2.0);
        }
    }

    /// <summary>
    /// Initializes the constraint with a fixed twist angle (no rotation allowed).
    /// </summary>
    /// <param name="axis1">The twist axis for body 1 in world space.</param>
    /// <param name="axis2">The twist axis for body 2 in world space.</param>
    public void Initialize(JVector axis1, JVector axis2)
    {
        Initialize(axis1, axis2, AngularLimit.Fixed);
    }

    public static void PrepareForIterationTwistAngle(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, TwistLimitData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JQuaternion q1 = body1.Orientation;
        JQuaternion q2 = body2.Orientation;

        JMatrix m = (-(Real)(1.0 / 2.0)) * QMatrix.ProjectMultiplyLeftRight(data.Q0 * q1.Conjugate(), q2);

        JQuaternion q = data.Q0 * q1.Conjugate() * q2;

        data.Jacobian = JVector.TransposedTransform(data.B, m);

        data.EffectiveMass = JVector.Transform(data.Jacobian, body1.InverseInertiaWorld + body2.InverseInertiaWorld) * data.Jacobian;

        data.EffectiveMass += (data.Softness * idt);

        data.EffectiveMass = (Real)1.0 / data.EffectiveMass;

        Real error = JVector.Dot(data.B, new JVector(q.X, q.Y, q.Z));

        if (q.W < (Real)0.0)
        {
            error *= -(Real)1.0;
            data.Jacobian *= -1;
        }

        data.Clamp = 0;

        if (error >= data.Angle2)
        {
            data.Clamp = 1;
            error -= data.Angle2;
        }
        else if (error < data.Angle1)
        {
            data.Clamp = 2;
            error -= data.Angle1;
        }
        else
        {
            data.AccumulatedImpulse = (Real)0.0;
            return;
        }

        data.Bias = error * data.BiasFactor * idt;

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

            Real error = JVector.Dot(data.B, new JVector(quat0.X, quat0.Y, quat0.Z));
            return (JAngle)((Real)2.0 * MathR.Asin(error));
        }
    }

    /// <summary>
    /// Gets or sets the softness (compliance) of the constraint.
    /// </summary>
    /// <value>
    /// Default is 0.0001. Higher values allow more angular error but improve stability.
    /// </value>
    public Real Softness
    {
        get => Data.Softness;
        set => Data.Softness = value;
    }

    /// <summary>
    /// Gets or sets the bias factor controlling how aggressively angular error is corrected.
    /// </summary>
    /// <value>
    /// Default is 0.2. Range [0, 1]. Higher values correct errors faster but may cause instability.
    /// </value>
    public Real Bias
    {
        get => Data.BiasFactor;
        set => Data.BiasFactor = value;
    }

    /// <summary>
    /// Gets the accumulated impulse applied by this constraint during the last step.
    /// </summary>
    public Real Impulse => Data.AccumulatedImpulse;

    public override void DebugDraw(IDebugDrawer drawer)
    {
    }

    public static void IterateTwistAngle(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, TwistLimitData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        if (data.Clamp == 0) return;

        Real jv = (body1.AngularVelocity - body2.AngularVelocity) * data.Jacobian;

        Real softnessScalar = data.AccumulatedImpulse * (data.Softness * idt);

        Real lambda = -data.EffectiveMass * (jv + data.Bias + softnessScalar);

        Real origAcc = data.AccumulatedImpulse;
        data.AccumulatedImpulse += lambda;

        if (data.Clamp == 1)
        {
            data.AccumulatedImpulse = MathR.Min(data.AccumulatedImpulse, (Real)0.0);
        }
        else
        {
            data.AccumulatedImpulse = MathR.Max(data.AccumulatedImpulse, (Real)0.0);
        }

        lambda = data.AccumulatedImpulse - origAcc;

        body1.AngularVelocity += JVector.Transform(lambda * data.Jacobian, body1.InverseInertiaWorld);
        body2.AngularVelocity -= JVector.Transform(lambda * data.Jacobian, body2.InverseInertiaWorld);
    }
}