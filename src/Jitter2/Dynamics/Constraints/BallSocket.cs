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
/// Implements a ball-and-socket joint that anchors a point on each body together,
/// removing three translational degrees of freedom.
/// </summary>
public unsafe class BallSocket : Constraint
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BallSocketData
    {
        internal int _internal;

        public delegate*<ref ConstraintData, void> Iterate;
        public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAnchor1;
        public JVector LocalAnchor2;

        public JVector U;
        public JVector R1;
        public JVector R2;

        public Real BiasFactor;
        public Real Softness;

        public JMatrix EffectiveMass;
        public JVector AccumulatedImpulse;
        public JVector Bias;
    }

    private JHandle<BallSocketData> handle;

    protected override void Create()
    {
        CheckDataSize<BallSocketData>();

        Iterate = &IterateBallSocket;
        PrepareForIteration = &PrepareForIterationBallSocket;
        handle = JHandle<ConstraintData>.AsHandle<BallSocketData>(Handle);
    }

    /// <summary>
    /// Initializes the constraint from a world-space anchor point.
    /// </summary>
    /// <param name="anchor">The anchor point in world space, shared by both bodies.</param>
    /// <remarks>
    /// Computes local anchor points for each body from their current poses.
    /// Default values: <see cref="Bias"/> = 0.2, <see cref="Softness"/> = 0.
    /// </remarks>
    public void Initialize(JVector anchor)
    {
        ref BallSocketData data = ref handle.Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Subtract(anchor, body1.Position, out data.LocalAnchor1);
        JVector.Subtract(anchor, body2.Position, out data.LocalAnchor2);

        JVector.ConjugatedTransform(data.LocalAnchor1, body1.Orientation, out data.LocalAnchor1);
        JVector.ConjugatedTransform(data.LocalAnchor2, body2.Orientation, out data.LocalAnchor2);

        data.BiasFactor = (Real)0.2;
        data.Softness = (Real)0.0;
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
            ref BallSocketData data = ref handle.Data;
            ref RigidBodyData body1 = ref data.Body1.Data;
            JVector.Subtract(value, body1.Position, out data.LocalAnchor1);
            JVector.ConjugatedTransform(data.LocalAnchor1, body1.Orientation, out data.LocalAnchor1);
        }
        get
        {
            ref BallSocketData data = ref handle.Data;
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
            ref BallSocketData data = ref handle.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            JVector.Subtract(value, body2.Position, out data.LocalAnchor2);
            JVector.ConjugatedTransform(data.LocalAnchor2, body2.Orientation, out data.LocalAnchor2);
        }
        get
        {
            ref BallSocketData data = ref handle.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;
            JVector.Transform(data.LocalAnchor2, body2.Orientation, out JVector result);
            JVector.Add(result, body2.Position, out result);
            return result;
        }
    }

    public static void PrepareForIterationBallSocket(ref ConstraintData constraint, Real idt)
    {
        ref BallSocketData data = ref Unsafe.AsRef<BallSocketData>(Unsafe.AsPointer(ref constraint));
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAnchor1, body1.Orientation, out data.R1);
        JVector.Transform(data.LocalAnchor2, body2.Orientation, out data.R2);

        JVector.Add(body1.Position, data.R1, out JVector p1);
        JVector.Add(body2.Position, data.R2, out JVector p2);

        JMatrix cr1 = JMatrix.CreateCrossProduct(data.R1);
        JMatrix cr2 = JMatrix.CreateCrossProduct(data.R2);

        data.EffectiveMass = body1.InverseMass * JMatrix.Identity +
                             JMatrix.Multiply(cr1, JMatrix.MultiplyTransposed(body1.InverseInertiaWorld, cr1)) +
                             body2.InverseMass * JMatrix.Identity +
                             JMatrix.Multiply(cr2, JMatrix.MultiplyTransposed(body2.InverseInertiaWorld, cr2));

        Real softness = data.Softness * idt;

        data.EffectiveMass.M11 += softness;
        data.EffectiveMass.M22 += softness;
        data.EffectiveMass.M33 += softness;

        JMatrix.Inverse(data.EffectiveMass, out data.EffectiveMass);

        data.Bias = (p2 - p1) * data.BiasFactor * idt;

        JVector acc = data.AccumulatedImpulse;

        body1.Velocity -= body1.InverseMass * acc;
        body1.AngularVelocity -= JVector.Transform(JVector.Transform(acc, cr1), body1.InverseInertiaWorld);

        body2.Velocity += body2.InverseMass * acc;
        body2.AngularVelocity += JVector.Transform(JVector.Transform(acc, cr2), body2.InverseInertiaWorld);
    }

    /// <summary>
    /// Gets or sets the softness (compliance) of the constraint.
    /// </summary>
    /// <value>
    /// Default is 0. Higher values allow more positional error but improve stability.
    /// Scaled by inverse timestep during solving.
    /// </value>
    public Real Softness
    {
        get => handle.Data.Softness;
        set => handle.Data.Softness = value;
    }

    /// <summary>
    /// Gets or sets the bias factor controlling how aggressively positional error is corrected.
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
    public JVector Impulse => handle.Data.AccumulatedImpulse;

    public static void IterateBallSocket(ref ConstraintData constraint, Real idt)
    {
        ref BallSocketData data = ref Unsafe.AsRef<BallSocketData>(Unsafe.AsPointer(ref constraint));
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        JMatrix cr1 = JMatrix.CreateCrossProduct(data.R1);
        JMatrix cr2 = JMatrix.CreateCrossProduct(data.R2);

        JVector softnessVector = data.AccumulatedImpulse * data.Softness * idt;

        JVector jv = -body1.Velocity + JVector.Transform(body1.AngularVelocity, cr1) + body2.Velocity -
                     JVector.Transform(body2.AngularVelocity, cr2);

        JVector lambda = -(Real)1.0 * JVector.Transform(jv + data.Bias + softnessVector, data.EffectiveMass);

        data.AccumulatedImpulse += lambda;

        body1.Velocity -= body1.InverseMass * lambda;
        body1.AngularVelocity -= JVector.Transform(JVector.Transform(lambda, cr1), body1.InverseInertiaWorld);

        body2.Velocity += body2.InverseMass * lambda;
        body2.AngularVelocity += JVector.Transform(JVector.Transform(lambda, cr2), body2.InverseInertiaWorld);
    }
}