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
/// Constrains a fixed point in the reference frame of one body to a plane that is fixed in
/// the reference frame of another body. This constraint removes one degree of translational
/// freedom if the limit is enforced.
/// </summary>
public unsafe class PointOnPlane : Constraint<PointOnPlane.SliderData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SliderData
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
        public Real Softness;

        public Real EffectiveMass;
        public Real AccumulatedImpulse;
        public Real Bias;

        public Real Min;
        public Real Max;

        public ushort Clamp;

        public MemoryHelper.MemBlock12Real J0;
    }

    protected override void Create()
    {
        Iterate = &IteratePointOnPlane;
        PrepareForIteration = &PrepareForIterationPointOnPlane;
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
    /// <param name="axis">The plane normal in world space, fixed in the reference frame of body 1.</param>
    /// <param name="anchor1">Anchor point on body 1 defining the plane origin in world space.</param>
    /// <param name="anchor2">Anchor point on body 2 constrained to the plane in world space.</param>
    /// <param name="limit">Distance limit from the plane.</param>
    /// <remarks>
    /// Computes local anchor points and axis from the current body poses.
    /// Default values: <see cref="Bias"/> = 0.01, <see cref="Softness"/> = 0.00001.
    /// </remarks>
    public void Initialize(JVector axis, JVector anchor1, JVector anchor2, LinearLimit limit)
    {
        VerifyNotZero();
        ref SliderData data = ref Data;
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

        (data.Min, data.Max) = limit;
    }

    public static void PrepareForIterationPointOnPlane(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, SliderData>(ref constraint);
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis, body1.Orientation, out JVector axis);

        JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector r1);
        JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector r2);

        JVector.Add(body1.Position, r1, out JVector p1);
        JVector.Add(body2.Position, r2, out JVector p2);

        data.Clamp = 0;

        JVector u = p2 - p1;

        var jacobian = new Span<JVector>(Unsafe.AsPointer(ref data.J0), 4);

        jacobian[0] = -axis;
        jacobian[1] = -((r1 + u) % axis);
        jacobian[2] = axis;
        jacobian[3] = r2 % axis;

        Real error = JVector.Dot(u, axis);

        data.EffectiveMass = (Real)1.0;

        if (error > data.Max)
        {
            error -= data.Max;
            data.Clamp = 1;
        }
        else if (error < data.Min)
        {
            error -= data.Min;
            data.Clamp = 2;
        }
        else
        {
            data.AccumulatedImpulse = 0;
            return;
        }

        data.EffectiveMass = body1.InverseMass + body2.InverseMass +
                             JVector.Transform(jacobian[1], body1.InverseInertiaWorld) * jacobian[1] +
                             JVector.Transform(jacobian[3], body2.InverseInertiaWorld) * jacobian[3];

        data.EffectiveMass += (data.Softness * idt);
        data.EffectiveMass = (Real)1.0 / data.EffectiveMass;

        data.Bias = error * data.BiasFactor * idt;

        Real acc = data.AccumulatedImpulse;

        body1.Velocity += body1.InverseMass * (jacobian[0] * acc);
        body1.AngularVelocity += JVector.Transform(jacobian[1] * acc, body1.InverseInertiaWorld);

        body2.Velocity += body2.InverseMass * (jacobian[2] * acc);
        body2.AngularVelocity += JVector.Transform(jacobian[3] * acc, body2.InverseInertiaWorld);
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
    public Real Impulse => Data.AccumulatedImpulse;

    public static void IteratePointOnPlane(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, SliderData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        if (data.Clamp == 0) return;

        var jacobian = new Span<JVector>(Unsafe.AsPointer(ref data.J0), 4);

        Real jv = jacobian[0] * body1.Velocity + jacobian[1] * body1.AngularVelocity + jacobian[2] * body2.Velocity +
                   jacobian[3] * body2.AngularVelocity;

        Real softness = data.AccumulatedImpulse * data.Softness * idt;

        Real lambda = -(Real)1.0 * (jv + data.Bias + softness) * data.EffectiveMass;

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

        body1.Velocity += body1.InverseMass * (jacobian[0] * lambda);
        body1.AngularVelocity += JVector.Transform(jacobian[1] * lambda, body1.InverseInertiaWorld);

        body2.Velocity += body2.InverseMass * (jacobian[2] * lambda);
        body2.AngularVelocity += JVector.Transform(jacobian[3] * lambda, body2.InverseInertiaWorld);
    }

    public override void DebugDraw(IDebugDrawer drawer)
    {
        ref SliderData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector r1);
        JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector r2);
        JVector.Transform(data.LocalAxis, body1.Orientation, out JVector axis);

        JVector p1 = body1.Position + r1;
        JVector p2 = body2.Position + r2;

        Real normalLength = (Real)0.5;
        drawer.DrawSegment(p1, p1 + axis * normalLength);
        drawer.DrawSegment(body2.Position, p2);
        drawer.DrawPoint(p1);
        drawer.DrawPoint(p2);
    }
}