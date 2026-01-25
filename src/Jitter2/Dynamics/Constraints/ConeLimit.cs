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
/// Limits the relative tilt between two bodies, removing one angular degree of freedom when active.
/// </summary>
public unsafe class ConeLimit : Constraint<ConeLimit.ConeLimitData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ConeLimitData
    {
        internal int _internal;
        public delegate*<ref ConstraintData, void> Iterate;
        public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAxis1, LocalAxis2;

        public Real BiasFactor;
        public Real Softness;

        public Real EffectiveMass;
        public Real AccumulatedImpulse;
        public Real Bias;

        public Real LimitLow;
        public Real LimitHigh;

        public short Clamp;

        public MemoryHelper.MemBlock6Real J0;
    }


    protected override void Create()
    {
        Iterate = &IterateConeLimit;
        PrepareForIteration = &PrepareForIterationConeLimit;
        base.Create();
    }

    /// <summary>
    /// Initializes the cone limit using two world-space axes and an angular range.
    /// </summary>
    /// <param name="axisBody1">The reference axis for body 1 in world space.</param>
    /// <param name="axisBody2">The reference axis for body 2 in world space.</param>
    /// <param name="limit">The minimum and maximum allowed tilt angles.</param>
    /// <remarks>
    /// Each axis is stored as a local axis on the corresponding body. The constraint measures
    /// the angle between these axes and restricts it to the given range.
    /// Default values: <see cref="Softness"/> = 0.001, <see cref="Bias"/> = 0.2.
    /// </remarks>
    public void Initialize(JVector axisBody1, JVector axisBody2, AngularLimit limit)
    {
        VerifyNotZero();
        ArgumentOutOfRangeException.ThrowIfNegative((Real)limit.From);
        ArgumentOutOfRangeException.ThrowIfLessThan((Real)limit.To, (Real)limit.From);

        ref ConeLimitData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.NormalizeInPlace(ref axisBody1);
        JVector.NormalizeInPlace(ref axisBody2);

        JVector.ConjugatedTransform(axisBody1, body1.Orientation, out data.LocalAxis1);
        JVector.ConjugatedTransform(axisBody2, body2.Orientation, out data.LocalAxis2);

        data.Softness = (Real)0.001;
        data.BiasFactor = (Real)0.2;

        Real lower = (Real)limit.From;
        Real upper = (Real)limit.To;

        data.LimitLow = MathR.Cos(lower);
        data.LimitHigh = MathR.Cos(upper);
    }

    /// <summary>
    /// Initializes the cone limit using a world-space axis and an angular range.
    /// </summary>
    /// <param name="axis">The reference axis in world space for the initial pose.</param>
    /// <param name="limit">The minimum and maximum allowed tilt angles.</param>
    /// <remarks>
    /// Stores the axis as a local axis on each body. The constraint measures the angle between
    /// these axes and restricts it to the given range.
    /// Default values: <see cref="Softness"/> = 0.001, <see cref="Bias"/> = 0.2.
    /// </remarks>
    public void Initialize(JVector axis, AngularLimit limit)
    {
        if (limit.From > (JAngle)0.0)
        {
            Logger.Warning(
                "{0}.{1}(): The lower limit is larger 0 but this overload initializes both body axes " +
                "from the same world-space axis (rest angle = 0). Use the two-axis overload " +
                "if you need a non-zero minimum angle.",
                nameof(ConeLimit),
                nameof(Initialize));
        }

        // Same axis for both bodies → rest angle is zero.
        Initialize(axis, axis, limit);
    }

    /// <summary>
    /// Gets the current angle between the two body axes.
    /// </summary>
    public JAngle Angle
    {
        get
        {
            ref ConeLimitData data = ref Data;

            ref RigidBodyData body1 = ref data.Body1.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;

            JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector a1);
            JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector a2);

            return (JAngle)MathR.Acos(JVector.Dot(a1, a2));
        }
    }

    /// <summary>
    /// Gets or sets the reference axis of body 1 in world space.
    /// </summary>
    public JVector AxisBody1
    {
        get
        {
            ref ConeLimitData data = ref Data;
            ref RigidBodyData body1 = ref data.Body1.Data;

            JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector axis);
            return axis;
        }
        set
        {
            ref ConeLimitData data = ref Data;
            ref RigidBodyData body1 = ref data.Body1.Data;

            JVector normalized = value;
            JVector.NormalizeInPlace(ref normalized);

            JVector.ConjugatedTransform(normalized, body1.Orientation, out data.LocalAxis1);
        }
    }

    /// <summary>
    /// Gets or sets the reference axis of body 2 in world space.
    /// </summary>
    public JVector AxisBody2
    {
        get
        {
            ref ConeLimitData data = ref Data;
            ref RigidBodyData body2 = ref data.Body2.Data;

            JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector axis);
            return axis;
        }
        set
        {
            ref ConeLimitData data = ref Data;
            ref RigidBodyData body2 = ref data.Body2.Data;

            JVector normalized = value;
            JVector.NormalizeInPlace(ref normalized);

            JVector.ConjugatedTransform(normalized, body2.Orientation, out data.LocalAxis2);
        }
    }

    /// <summary>
    /// Gets or sets the angular limit of the cone.
    /// </summary>
    public AngularLimit Limit
    {
        get
        {
            ref ConeLimitData data = ref Data;
            return new AngularLimit(JAngle.FromRadian(data.LimitLow), JAngle.FromRadian(data.LimitHigh));
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative((Real)value.From);
            ArgumentOutOfRangeException.ThrowIfLessThan((Real)value.To, (Real)value.From);

            ref ConeLimitData data = ref Data;
            data.LimitLow = (Real)value.From;
            data.LimitHigh = (Real)value.To;
        }
    }

    public static void PrepareForIterationConeLimit(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, ConeLimitData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector a1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector a2);

        var jacobian = new Span<JVector>(Unsafe.AsPointer(ref data.J0), 2);

        jacobian[0] = JVector.Cross(a2, a1);
        jacobian[1] = JVector.Cross(a1, a2);

        data.Clamp = 0;

        Real error = JVector.Dot(a1, a2);

        if (error < data.LimitHigh)
        {
            data.Clamp = 1;
            error -= data.LimitHigh;
        }
        else if (error > data.LimitLow)
        {
            data.Clamp = 2;
            error -= data.LimitLow;
        }
        else
        {
            data.AccumulatedImpulse = (Real)0.0;
            return;
        }

        data.EffectiveMass = JVector.Transform(jacobian[0], body1.InverseInertiaWorld) * jacobian[0] +
                             JVector.Transform(jacobian[1], body2.InverseInertiaWorld) * jacobian[1];

        data.EffectiveMass += data.Softness * idt;

        data.EffectiveMass = (Real)1.0 / data.EffectiveMass;

        data.Bias = -error * data.BiasFactor * idt;

        body1.AngularVelocity +=
            JVector.Transform(data.AccumulatedImpulse * jacobian[0], body1.InverseInertiaWorld);

        body2.AngularVelocity +=
            JVector.Transform(data.AccumulatedImpulse * jacobian[1], body2.InverseInertiaWorld);
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
    public Real Impulse => Data.AccumulatedImpulse;

    public static void IterateConeLimit(ref ConstraintData constraint, Real idt)
    {
        ref var data = ref Unsafe.As<ConstraintData, ConeLimitData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        if (data.Clamp == 0) return;

        var jacobian = new Span<JVector>(Unsafe.AsPointer(ref data.J0), 2);

        Real jv =
            body1.AngularVelocity * jacobian[0] +
            body2.AngularVelocity * jacobian[1];

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

        body1.AngularVelocity += JVector.Transform(lambda * jacobian[0], body1.InverseInertiaWorld);
        body2.AngularVelocity += JVector.Transform(lambda * jacobian[1], body2.InverseInertiaWorld);
    }
}