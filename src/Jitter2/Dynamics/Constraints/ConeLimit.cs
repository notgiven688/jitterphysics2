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
/// A constraint that limits the relative tilt between two bodies.
/// The allowed motion forms a cone defined by a minimum and maximum angle
/// around an initial reference axis.
/// </summary>
public unsafe class ConeLimit : Constraint
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

    private JHandle<ConeLimitData> handle;

    protected override void Create()
    {
        CheckDataSize<ConeLimitData>();

        Iterate = &IterateConeLimit;
        PrepareForIteration = &PrepareForIterationConeLimit;
        handle = JHandle<ConstraintData>.AsHandle<ConeLimitData>(Handle);
    }

    /// <summary>
    /// Initializes the cone limit using two world-space axes and an angular range.
    /// </summary>
    /// <param name="axisBody1">The reference axis for body 1 in world space.</param>
    /// <param name="axisBody2">The reference axis for body 2 in world space.</param>
    /// <param name="limit">The minimum and maximum allowed tilt angles.</param>
    /// <remarks>
    /// This overload allows specifying an initial angular offset between the bodies.
    /// Each axis is stored as a local axis on the corresponding body. The constraint
    /// then measures the angle between these axes (transformed back into world space)
    /// and restricts it to the range given by the angular limit.
    /// </remarks>
    public void Initialize(JVector axisBody1, JVector axisBody2, AngularLimit limit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative((Real)limit.From);
        ArgumentOutOfRangeException.ThrowIfLessThan((Real)limit.To, (Real)limit.From);

        ref ConeLimitData data = ref handle.Data;
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
    /// When initialized, the given world-space axis is stored as a local axis
    /// on each body. The constraint then measures the angle between these two
    /// axes (transformed back into world space) and restricts it to the range
    /// specified by the angular limit.
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

    public JAngle Angle
    {
        get
        {
            ref ConeLimitData data = ref handle.Data;

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
            ref ConeLimitData data = ref handle.Data;
            ref RigidBodyData body1 = ref data.Body1.Data;

            JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector axis);
            return axis;
        }
        set
        {
            ref ConeLimitData data = ref handle.Data;
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
            ref ConeLimitData data = ref handle.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;

            JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector axis);
            return axis;
        }
        set
        {
            ref ConeLimitData data = ref handle.Data;
            ref RigidBodyData body2 = ref data.Body2.Data;

            JVector normalized = value;
            JVector.NormalizeInPlace(ref normalized);

            JVector.ConjugatedTransform(normalized, body2.Orientation, out data.LocalAxis2);
        }
    }

    public static void PrepareForIterationConeLimit(ref ConstraintData constraint, Real idt)
    {
        ref ConeLimitData data = ref Unsafe.AsRef<ConeLimitData>(Unsafe.AsPointer(ref constraint));

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

    public Real Softness
    {
        get => handle.Data.Softness;
        set => handle.Data.Softness = value;
    }

    public Real Bias
    {
        get => handle.Data.BiasFactor;
        set => handle.Data.BiasFactor = value;
    }

    public Real Impulse => handle.Data.AccumulatedImpulse;

    public static void IterateConeLimit(ref ConstraintData constraint, Real idt)
    {
        ref ConeLimitData data = ref Unsafe.AsRef<ConeLimitData>(Unsafe.AsPointer(ref constraint));
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