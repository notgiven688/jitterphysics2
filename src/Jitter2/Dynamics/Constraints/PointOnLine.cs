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
/// Constrains a fixed point in the reference frame of one body to a line that is fixed in
/// the reference frame of another body. This constraint removes two degrees of translational
/// freedom; three if the limit is enforced.
/// </summary>
public unsafe class PointOnLine : Constraint<PointOnLine.PointOnLineData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PointOnLineData
    {
        internal int _internal;

        public delegate*<ref ConstraintData, void> Iterate;
        public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAxis;

        public JVector LocalAnchor1;
        public JVector LocalAnchor2;

        public Real BiasFactor;
        public Real LimitBias;
        public Real Softness;
        public Real LimitSoftness;

        public JMatrix EffectiveMass;
        public JVector AccumulatedImpulse;
        public JVector Bias;

        public Real Min;
        public Real Max;

        public ushort Clamp;

        // public MemBlock96 J0;
    }

    protected override void Create()
    {
        Iterate = &IteratePointOnLine;
        PrepareForIteration = &PrepareForIterationPointOnLine;
        base.Create();
    }

    /// <inheritdoc cref="Initialize(JVector, JVector, JVector, LinearLimit)"/>
    public void Initialize(JVector axis, JVector anchor1, JVector anchor2)
    {
        Initialize(axis, anchor1, anchor2, LinearLimit.Fixed);
    }

    /// <summary>
    /// Initializes the constraint from world-space parameters.
    /// </summary>
    /// <param name="axis">The line axis in world space, fixed in the reference frame of body 1.</param>
    /// <param name="anchor1">Anchor point on body 1 defining the line origin in world space.</param>
    /// <param name="anchor2">Anchor point on body 2 constrained to the line in world space.</param>
    /// <param name="limit">Distance limit along the axis.</param>
    /// <remarks>
    /// Computes local anchor points and axis from the current body poses.
    /// Default values: <see cref="Bias"/> = 0.01, <see cref="Softness"/> = 0.00001,
    /// <see cref="LimitSoftness"/> = 0.0001, <see cref="LimitBias"/> = 0.2.
    /// </remarks>
    public void Initialize(JVector axis, JVector anchor1, JVector anchor2, LinearLimit limit)
    {
        VerifyNotZero();
        ref PointOnLineData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.NormalizeInPlace(ref axis);

        JVector.Subtract(anchor1, body1.Position, out data.LocalAnchor1);
        JVector.Subtract(anchor2, body2.Position, out data.LocalAnchor2);

        JVector.ConjugatedTransform(data.LocalAnchor1, body1.Orientation, out data.LocalAnchor1);
        JVector.ConjugatedTransform(data.LocalAnchor2, body2.Orientation, out data.LocalAnchor2);

        JVector.ConjugatedTransform(axis, body1.Orientation, out data.LocalAxis);

        data.BiasFactor = (Real)0.01;
        data.Softness = (Real)0.00001;
        data.LimitSoftness = (Real)0.0001;
        data.LimitBias = (Real)0.2;

        (data.Min, data.Max) = limit;
    }

    /// <summary>
    /// Gets the current distance of the anchor point from the line origin along the axis.
    /// </summary>
    public Real Distance
    {
        get
        {
            ref PointOnLineData data = ref Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;

            JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector r1);
            JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector r2);

            JVector.Add(body1.Position, r1, out JVector p1);
            JVector.Add(body2.Position, r2, out JVector p2);

            JVector u = p2 - p1;

            JVector.Transform(data.LocalAxis, body1.Orientation, out JVector aw);

            return JVector.Dot(u, aw);
        }
    }

    [SkipLocalsInit]
    public static void PrepareForIterationPointOnLine(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, PointOnLineData>(ref constraint);
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis, body1.Orientation, out JVector aw);

        JVector n1 = MathHelper.CreateOrthonormal(data.LocalAxis);
        JVector.Transform(n1, body1.Orientation, out n1);

        JVector n2 = aw % n1;

        JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector r1);
        JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector r2);

        JVector.Add(body1.Position, r1, out JVector p1);
        JVector.Add(body2.Position, r2, out JVector p2);

        data.Clamp = 0;

        JVector u = p2 - p1;

        Span<JVector> jacobian = stackalloc JVector[12];

        jacobian[0] = -n1;
        jacobian[1] = -((r1 + u) % n1);
        jacobian[2] = n1;
        jacobian[3] = r2 % n1;

        jacobian[4] = -n2;
        jacobian[5] = -((r1 + u) % n2);
        jacobian[6] = n2;
        jacobian[7] = r2 % n2;

        jacobian[8] = -aw;
        jacobian[9] = -((r1 + u) % aw);
        jacobian[10] = aw;
        jacobian[11] = r2 % aw;

        var error = new JVector(JVector.Dot(u, n1), JVector.Dot(u, n2), JVector.Dot(u, aw));

        data.EffectiveMass = JMatrix.Identity;

        data.EffectiveMass.M11 = body1.InverseMass + body2.InverseMass +
                                 JVector.Transform(jacobian[1], body1.InverseInertiaWorld) * jacobian[1] +
                                 JVector.Transform(jacobian[3], body2.InverseInertiaWorld) * jacobian[3];

        data.EffectiveMass.M22 = body1.InverseMass + body2.InverseMass +
                                 JVector.Transform(jacobian[5], body1.InverseInertiaWorld) * jacobian[5] +
                                 JVector.Transform(jacobian[7], body2.InverseInertiaWorld) * jacobian[7];

        data.EffectiveMass.M12 = JVector.Transform(jacobian[1], body1.InverseInertiaWorld) * jacobian[5] +
                                 JVector.Transform(jacobian[3], body2.InverseInertiaWorld) * jacobian[7];

        data.EffectiveMass.M21 = JVector.Transform(jacobian[5], body1.InverseInertiaWorld) * jacobian[1] +
                                 JVector.Transform(jacobian[7], body2.InverseInertiaWorld) * jacobian[3];

        if (error.Z > data.Max)
        {
            error.Z -= data.Max;
            data.Clamp = 1;
        }
        else if (error.Z < data.Min)
        {
            error.Z -= data.Min;
            data.Clamp = 2;
        }
        else
        {
            data.AccumulatedImpulse.Z = 0;
        }

        if (data.Clamp != 0)
        {
            data.EffectiveMass.M33 = body1.InverseMass + body2.InverseMass +
                                     JVector.Transform(jacobian[9], body1.InverseInertiaWorld) * jacobian[9] +
                                     JVector.Transform(jacobian[11], body2.InverseInertiaWorld) * jacobian[11];

            data.EffectiveMass.M13 = JVector.Transform(jacobian[1], body1.InverseInertiaWorld) * jacobian[9] +
                                     JVector.Transform(jacobian[3], body2.InverseInertiaWorld) * jacobian[11];

            data.EffectiveMass.M31 = JVector.Transform(jacobian[9], body1.InverseInertiaWorld) * jacobian[1] +
                                     JVector.Transform(jacobian[11], body2.InverseInertiaWorld) * jacobian[3];

            data.EffectiveMass.M23 = JVector.Transform(jacobian[5], body1.InverseInertiaWorld) * jacobian[9] +
                                     JVector.Transform(jacobian[7], body2.InverseInertiaWorld) * jacobian[11];

            data.EffectiveMass.M32 = JVector.Transform(jacobian[9], body1.InverseInertiaWorld) * jacobian[5] +
                                     JVector.Transform(jacobian[11], body2.InverseInertiaWorld) * jacobian[7];
        }

        data.EffectiveMass.M11 += data.Softness * idt;
        data.EffectiveMass.M22 += data.Softness * idt;
        data.EffectiveMass.M33 += data.LimitSoftness * idt;

        JMatrix.Inverse(data.EffectiveMass, out data.EffectiveMass);

        data.Bias = error * idt;
        data.Bias.X *= data.BiasFactor;
        data.Bias.Y *= data.BiasFactor;
        data.Bias.Z *= data.LimitBias;

        JVector acc = data.AccumulatedImpulse;

        body1.Velocity += body1.InverseMass * (jacobian[0] * acc.X + jacobian[4] * acc.Y + jacobian[8] * acc.Z);
        body1.AngularVelocity += JVector.Transform(jacobian[1] * acc.X + jacobian[5] * acc.Y + jacobian[9] * acc.Z, body1.InverseInertiaWorld);

        body2.Velocity += body2.InverseMass * (jacobian[2] * acc.X + jacobian[6] * acc.Y + jacobian[10] * acc.Z);
        body2.AngularVelocity += JVector.Transform(jacobian[3] * acc.X + jacobian[7] * acc.Y + jacobian[11] * acc.Z, body2.InverseInertiaWorld);
    }

    /// <summary>
    /// Gets or sets the softness (compliance) of the constraint.
    /// </summary>
    /// <value>
    /// Default is 0.00001. Higher values allow more positional error but improve stability.
    /// </value>
    public Real Softness
    {
        get => Data.Softness;
        set => Data.Softness = value;
    }

    /// <summary>
    /// Gets or sets the bias factor controlling how aggressively positional error is corrected.
    /// </summary>
    /// <value>
    /// Default is 0.01. Range [0, 1]. Higher values correct errors faster but may cause instability.
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

    /// <summary>
    /// Gets or sets the softness (compliance) applied when distance limits are active.
    /// </summary>
    /// <value>
    /// Default is 0.0001. Higher values allow more limit violation but improve stability.
    /// </value>
    public Real LimitSoftness
    {
        get => Data.LimitSoftness;
        set => Data.LimitSoftness = value;
    }

    /// <summary>
    /// Gets or sets the bias factor for distance limit correction.
    /// </summary>
    /// <value>
    /// Default is 0.2. Range [0, 1]. Higher values correct limit violations faster.
    /// </value>
    public Real LimitBias
    {
        get => Data.LimitBias;
        set => Data.LimitBias = value;
    }

    [SkipLocalsInit]
    public static void IteratePointOnLine(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, PointOnLineData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        JVector.Transform(data.LocalAxis, body1.Orientation, out JVector aw);

        JVector n1 = MathHelper.CreateOrthonormal(data.LocalAxis);
        JVector.Transform(n1, body1.Orientation, out n1);

        JVector n2 = aw % n1;

        JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector r1);
        JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector r2);

        JVector.Add(body1.Position, r1, out JVector p1);
        JVector.Add(body2.Position, r2, out JVector p2);

        JVector u = p2 - p1;

        Span<JVector> jacobian = stackalloc JVector[12];

        jacobian[0] = -n1;
        jacobian[1] = -((r1 + u) % n1);
        jacobian[2] = n1;
        jacobian[3] = r2 % n1;

        jacobian[4] = -n2;
        jacobian[5] = -((r1 + u) % n2);
        jacobian[6] = n2;
        jacobian[7] = r2 % n2;

        jacobian[8] = -aw;
        jacobian[9] = -((r1 + u) % aw);
        jacobian[10] = aw;
        jacobian[11] = r2 % aw;

        JVector jv;
        jv.X = jacobian[0] * body1.Velocity + jacobian[1] * body1.AngularVelocity + jacobian[2] * body2.Velocity +
               jacobian[3] * body2.AngularVelocity;

        jv.Y = jacobian[4] * body1.Velocity + jacobian[5] * body1.AngularVelocity + jacobian[6] * body2.Velocity +
               jacobian[7] * body2.AngularVelocity;

        jv.Z = jacobian[8] * body1.Velocity + jacobian[9] * body1.AngularVelocity + jacobian[10] * body2.Velocity +
               jacobian[11] * body2.AngularVelocity;

        JVector softnessVector = data.AccumulatedImpulse * idt;
        softnessVector.X *= data.Softness;
        softnessVector.Y *= data.Softness;
        softnessVector.Z *= data.LimitSoftness;

        JVector lambda = -(Real)1.0 * JVector.Transform(jv + data.Bias + softnessVector, data.EffectiveMass);

        JVector origAcc = data.AccumulatedImpulse;

        data.AccumulatedImpulse += lambda;

        if ((data.Clamp & 1) != 0)
            data.AccumulatedImpulse.Z = MathR.Min(data.AccumulatedImpulse.Z, (Real)0.0);
        else if ((data.Clamp & 2) != 0)
            data.AccumulatedImpulse.Z = MathR.Max(data.AccumulatedImpulse.Z, (Real)0.0);
        else
        {
            data.AccumulatedImpulse.Z = (Real)0.0;
            origAcc.Z = (Real)0.0;
        }

        lambda = data.AccumulatedImpulse - origAcc;

        body1.Velocity += body1.InverseMass * (jacobian[0] * lambda.X + jacobian[4] * lambda.Y + jacobian[8] * lambda.Z);
        body1.AngularVelocity += JVector.Transform(jacobian[1] * lambda.X + jacobian[5] * lambda.Y + jacobian[9] * lambda.Z, body1.InverseInertiaWorld);

        body2.Velocity += body2.InverseMass * (jacobian[2] * lambda.X + jacobian[6] * lambda.Y + jacobian[10] * lambda.Z);
        body2.AngularVelocity += JVector.Transform(jacobian[3] * lambda.X + jacobian[7] * lambda.Y + jacobian[11] * lambda.Z, body2.InverseInertiaWorld);
    }

    public override void DebugDraw(IDebugDrawer drawer)
    {
        ref PointOnLineData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector r1);
        JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector r2);
        JVector.Transform(data.LocalAxis, body1.Orientation, out JVector axis);

        JVector p1 = body1.Position + r1;
        JVector p2 = body2.Position + r2;

        Real lineLength = (Real)1.0;
        drawer.DrawSegment(p1 - axis * lineLength, p1 + axis * lineLength);
        drawer.DrawSegment(body2.Position, p2);
        drawer.DrawPoint(p1);
        drawer.DrawPoint(p2);
    }
}