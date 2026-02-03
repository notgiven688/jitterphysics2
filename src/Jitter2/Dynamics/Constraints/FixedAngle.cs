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
/// Constrains the relative orientation between two bodies, eliminating three degrees of rotational freedom.
/// </summary>
public unsafe class FixedAngle : Constraint<FixedAngle.FixedAngleData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FixedAngleData
    {
        internal int _internal;
        public delegate*<ref ConstraintData, void> Iterate;
        public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public Real MinAngle;
        public Real MaxAngle;

        public Real BiasFactor;
        public Real Softness;

        public JQuaternion Q0;

        public JVector AccumulatedImpulse;
        public JVector Bias;

        public JMatrix EffectiveMass;
        public JMatrix Jacobian;

        public ushort Clamp;
    }

    protected override void Create()
    {
        Iterate = &IterateFixedAngle;
        PrepareForIteration = &PrepareForIterationFixedAngle;
        base.Create();
    }

    /// <summary>
    /// Initializes the constraint using the current relative orientation of the bodies.
    /// </summary>
    /// <remarks>
    /// Records the current relative orientation as the target.
    /// Default values: <see cref="Softness"/> = 0.001, <see cref="Bias"/> = 0.2.
    /// </remarks>
    public void Initialize()
    {
        VerifyNotZero();
        ref FixedAngleData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        data.Softness = (Real)0.001;
        data.BiasFactor = (Real)0.2;

        JQuaternion q1 = body1.Orientation;
        JQuaternion q2 = body2.Orientation;

        data.Q0 = q2.Conjugate() * q1;
    }

    public static void PrepareForIterationFixedAngle(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, FixedAngleData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JQuaternion q1 = body1.Orientation;
        JQuaternion q2 = body2.Orientation;

        JQuaternion quat0 = data.Q0 * q1.Conjugate() * q2;

        JVector error = new(quat0.X, quat0.Y, quat0.Z);

        data.Clamp = 1024;

        data.Jacobian = QMatrix.ProjectMultiplyLeftRight(data.Q0 * q1.Conjugate(), q2);

        if (quat0.W < (Real)0.0)
        {
            error *= -(Real)1.0;
            data.Jacobian *= -(Real)1.0;
        }

        data.EffectiveMass = JMatrix.Multiply(data.Jacobian, JMatrix.MultiplyTransposed(body1.InverseInertiaWorld + body2.InverseInertiaWorld, data.Jacobian));

        data.EffectiveMass.M11 += data.Softness * idt;
        data.EffectiveMass.M22 += data.Softness * idt;
        data.EffectiveMass.M33 += data.Softness * idt;

        JMatrix.Inverse(data.EffectiveMass, out data.EffectiveMass);

        data.Bias = -error * data.BiasFactor * idt;

        body1.AngularVelocity += JVector.Transform(JVector.TransposedTransform(data.AccumulatedImpulse, data.Jacobian), body1.InverseInertiaWorld);
        body2.AngularVelocity -= JVector.Transform(JVector.TransposedTransform(data.AccumulatedImpulse, data.Jacobian), body2.InverseInertiaWorld);
    }

    /// <summary>
    /// Gets or sets the softness (compliance) of the constraint.
    /// </summary>
    /// <value>
    /// Default is 0.001. Higher values allow more angular error but improve stability.
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
    public JVector Impulse => Data.AccumulatedImpulse;

    public static void IterateFixedAngle(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, FixedAngleData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        JVector jv = JVector.Transform(body1.AngularVelocity - body2.AngularVelocity, data.Jacobian);
        JVector softness = data.AccumulatedImpulse * (data.Softness * idt);
        JVector lambda = -(Real)1.0 * JVector.Transform(jv + data.Bias + softness, data.EffectiveMass);

        data.AccumulatedImpulse += lambda;

        body1.AngularVelocity += JVector.Transform(JVector.TransposedTransform(lambda, data.Jacobian), body1.InverseInertiaWorld);
        body2.AngularVelocity -= JVector.Transform(JVector.TransposedTransform(lambda, data.Jacobian), body2.InverseInertiaWorld);
    }

    public override void DebugDraw(IDebugDrawer drawer)
    {
        ref FixedAngleData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        const Real axisLength = (Real)0.5;

        JVector x1 = body1.Orientation.GetBasisX() * axisLength;
        JVector y1 = body1.Orientation.GetBasisY() * axisLength;
        JVector z1 = body1.Orientation.GetBasisZ() * axisLength;

        JVector x2 = body2.Orientation.GetBasisX() * axisLength;
        JVector y2 = body2.Orientation.GetBasisY() * axisLength;
        JVector z2 = body2.Orientation.GetBasisZ() * axisLength;

        drawer.DrawSegment(body1.Position, body1.Position + x1);
        drawer.DrawSegment(body1.Position, body1.Position + y1);
        drawer.DrawSegment(body1.Position, body1.Position + z1);

        drawer.DrawSegment(body2.Position, body2.Position + x2);
        drawer.DrawSegment(body2.Position, body2.Position + y2);
        drawer.DrawSegment(body2.Position, body2.Position + z2);
    }
}