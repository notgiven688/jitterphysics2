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
/// Constrains the distance between a fixed point in the reference frame of one body and a fixed
/// point in the reference frame of another body. This constraint removes one translational degree
/// of freedom. For a distance of zero, use the <see cref="BallSocket"/> constraint.
/// </summary>
public unsafe class DistanceLimit : Constraint
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DistanceLimitData
    {
        internal int _internal;
        public delegate*<ref ConstraintData, void> Iterate;
        public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAnchor1;
        public JVector LocalAnchor2;

        public Real BiasFactor;
        public Real Softness;
        public Real Distance;

        public Real LimitMin;
        public Real LimitMax;

        public Real EffectiveMass;
        public Real AccumulatedImpulse;
        public Real Bias;

        public MemoryHelper.MemBlock12Real J0;

        public short Clamp;
    }

    private JHandle<DistanceLimitData> handle;

    protected override void Create()
    {
        CheckDataSize<DistanceLimitData>();

        Iterate = &IterateFixedAngle;
        PrepareForIteration = &PrepareForIterationFixedAngle;
        handle = JHandle<ConstraintData>.AsHandle<DistanceLimitData>(Handle);
    }

    /// <summary>
    /// Initializes the constraint with a fixed distance between anchor points.
    /// </summary>
    /// <param name="anchor1">Anchor point on the first body in world space.</param>
    /// <param name="anchor2">Anchor point on the second body in world space.</param>
    public void Initialize(JVector anchor1, JVector anchor2)
    {
        Initialize(anchor1, anchor2, LinearLimit.Fixed);
    }

    /// <summary>
    /// Initializes the constraint with distance limits between anchor points.
    /// </summary>
    /// <param name="anchor1">Anchor point on the first body in world space.</param>
    /// <param name="anchor2">Anchor point on the second body in world space.</param>
    /// <param name="limit">The allowed distance range between anchor points.</param>
    /// <remarks>
    /// Computes local anchor points and the initial distance from current poses.
    /// Default values: <see cref="Softness"/> = 0.001, <see cref="Bias"/> = 0.2.
    /// </remarks>
    public void Initialize(JVector anchor1, JVector anchor2, LinearLimit limit)
    {
        ref DistanceLimitData data = ref handle.Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Subtract(anchor1, body1.Position, out data.LocalAnchor1);
        JVector.Subtract(anchor2, body2.Position, out data.LocalAnchor2);

        JVector.ConjugatedTransform(data.LocalAnchor1, body1.Orientation, out data.LocalAnchor1);
        JVector.ConjugatedTransform(data.LocalAnchor2, body2.Orientation, out data.LocalAnchor2);

        data.Softness = (Real)0.001;
        data.BiasFactor = (Real)0.2;
        data.Distance = (anchor2 - anchor1).Length();

        (data.LimitMin, data.LimitMax) = limit;
    }

    /// <summary>
    /// Gets or sets the anchor point on the first rigid body in world space. The anchor point is
    /// fixed in the local reference frame of the first body.
    /// </summary>
    [ReferenceFrame(ReferenceFrame.World)]
    public JVector Anchor1
    {
        set
        {
            ref DistanceLimitData data = ref handle.Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            JVector.Subtract(value, body1.Position, out data.LocalAnchor1);
            JVector.ConjugatedTransform(data.LocalAnchor1, body1.Orientation, out data.LocalAnchor1);
        }
        get
        {
            ref DistanceLimitData data = ref handle.Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector result);
            JVector.Add(result, body1.Position, out result);
            return result;
        }
    }

    /// <summary>
    /// Gets or sets the anchor point on the second rigid body in world space. The anchor point is
    /// fixed in the local reference frame of the second body.
    /// </summary>
    [ReferenceFrame(ReferenceFrame.World)]
    public JVector Anchor2
    {
        set
        {
            ref DistanceLimitData data = ref handle.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            JVector.Subtract(value, body2.Position, out data.LocalAnchor2);
            JVector.ConjugatedTransform(data.LocalAnchor2, body2.Orientation, out data.LocalAnchor2);
        }
        get
        {
            ref DistanceLimitData data = ref handle.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector result);
            JVector.Add(result, body2.Position, out result);
            return result;
        }
    }

    /// <summary>
    /// Gets or sets the target distance between anchor points.
    /// </summary>
    /// <value>Set during initialization from the initial anchor separation.</value>
    public Real TargetDistance
    {
        set
        {
            ref DistanceLimitData data = ref handle.Data;
            data.Distance = value;
        }
        get => handle.Data.Distance;
    }

    /// <summary>
    /// Gets the current distance between anchor points in world space.
    /// </summary>
    public Real Distance
    {
        get
        {
            ref DistanceLimitData data = ref handle.Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;

            JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector r1);
            JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector r2);

            JVector.Add(body1.Position, r1, out JVector p1);
            JVector.Add(body2.Position, r2, out JVector p2);

            JVector.Subtract(p2, p1, out JVector dp);

            return dp.Length();
        }
    }

    public static void PrepareForIterationFixedAngle(ref ConstraintData constraint, Real idt)
    {
        ref DistanceLimitData data = ref Unsafe.AsRef<DistanceLimitData>(Unsafe.AsPointer(ref constraint));
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAnchor1, body1.Orientation, out JVector r1);
        JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector r2);

        JVector.Add(body1.Position, r1, out JVector p1);
        JVector.Add(body2.Position, r2, out JVector p2);

        JVector.Subtract(p2, p1, out JVector dp);

        Real error = dp.Length() - data.Distance;

        data.Clamp = 0;

        if (error >= data.LimitMax)
        {
            data.Clamp = 1;
            error -= data.LimitMax;
        }
        else if (error < data.LimitMin)
        {
            data.Clamp = 2;
            error -= data.LimitMin;
        }
        else
        {
            data.AccumulatedImpulse = (Real)0.0;
            return;
        }

        JVector n = p2 - p1;
        if (n.LengthSquared() > (Real)1e-12) JVector.NormalizeInPlace(ref n);

        var jacobian = new Span<JVector>(Unsafe.AsPointer(ref data.J0), 4);

        jacobian[0] = -(Real)1.0 * n;
        jacobian[1] = -(Real)1.0 * (r1 % n);
        jacobian[2] = (Real)1.0 * n;
        jacobian[3] = r2 % n;

        data.EffectiveMass = body1.InverseMass +
                             body2.InverseMass +
                             JVector.Transform(jacobian[1], body1.InverseInertiaWorld) * jacobian[1] +
                             JVector.Transform(jacobian[3], body2.InverseInertiaWorld) * jacobian[3];

        data.EffectiveMass += data.Softness * idt;

        data.EffectiveMass = (Real)1.0 / data.EffectiveMass;

        data.Bias = error * data.BiasFactor * idt;

        body1.Velocity += body1.InverseMass * data.AccumulatedImpulse * jacobian[0];
        body1.AngularVelocity += JVector.Transform(data.AccumulatedImpulse * jacobian[1], body1.InverseInertiaWorld);

        body2.Velocity += body2.InverseMass * data.AccumulatedImpulse * jacobian[2];
        body2.AngularVelocity += JVector.Transform(data.AccumulatedImpulse * jacobian[3], body2.InverseInertiaWorld);
    }

    /// <summary>
    /// Gets or sets the softness (compliance) of the constraint.
    /// </summary>
    /// <value>
    /// Default is 0.001. Higher values allow more distance error but improve stability.
    /// </value>
    public Real Softness
    {
        get => handle.Data.Softness;
        set => handle.Data.Softness = value;
    }

    /// <summary>
    /// Gets or sets the bias factor controlling how aggressively distance error is corrected.
    /// </summary>
    /// <value>
    /// Default is 0.2. Range [0, 1]. Higher values correct errors faster but may cause instability.
    /// </value>
    public Real Bias
    {
        get => handle.Data.BiasFactor;
        set => handle.Data.BiasFactor = value;
    }

    /// <summary>
    /// Gets the accumulated impulse applied by this constraint during the last step.
    /// </summary>
    public Real Impulse => handle.Data.AccumulatedImpulse;

    public static void IterateFixedAngle(ref ConstraintData constraint, Real idt)
    {
        ref DistanceLimitData data = ref Unsafe.AsRef<DistanceLimitData>(Unsafe.AsPointer(ref constraint));
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        if (data.Clamp == 0) return;

        var jacobian = new Span<JVector>(Unsafe.AsPointer(ref data.J0), 4);

        Real jv =
            body1.Velocity * jacobian[0] +
            body1.AngularVelocity * jacobian[1] +
            body2.Velocity * jacobian[2] +
            body2.AngularVelocity * jacobian[3];

        Real softnessScalar = data.AccumulatedImpulse * data.Softness * idt;

        Real lambda = -data.EffectiveMass * (jv + data.Bias + softnessScalar);

        Real oldAccumulated = data.AccumulatedImpulse;

        data.AccumulatedImpulse += lambda;

        if (data.Clamp == 1)
        {
            data.AccumulatedImpulse = MathR.Min(data.AccumulatedImpulse, (Real)0.0);
        }
        else
        {
            data.AccumulatedImpulse = MathR.Max(data.AccumulatedImpulse, (Real)0.0);
        }

        lambda = data.AccumulatedImpulse - oldAccumulated;

        body1.Velocity += body1.InverseMass * lambda * jacobian[0];
        body1.AngularVelocity += JVector.Transform(lambda * jacobian[1], body1.InverseInertiaWorld);

        body2.Velocity += body2.InverseMass * lambda * jacobian[2];
        body2.AngularVelocity += JVector.Transform(lambda * jacobian[3], body2.InverseInertiaWorld);
    }
}