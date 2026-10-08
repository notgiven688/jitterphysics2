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
/// <remarks>
/// Fixed tilts must lie strictly between zero and pi. Use <see cref="HingeAngle"/> with
/// <see cref="AngularLimit.Full"/> to keep axes aligned while allowing rotation about them.
/// </remarks>
public unsafe class ConeLimit : Constraint<ConeLimit.ConeLimitData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ConeLimitData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAxis1, LocalAxis2;

        public Real BiasFactor;
        public Real Softness;

        public Real EffectiveMass;
        public Real AccumulatedImpulse;
        public Real Bias;

        public Real MinAngle;
        public Real MaxAngle;

        public short Clamp;

        public MemoryHelper.MemBlock6Real J0;
    }

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationConeLimit, &IterateConeLimit);


    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = 0;

    /// <summary>
    /// Initializes the cone limit using two world-space axes and an angular range.
    /// </summary>
    /// <param name="axisBody1">The reference axis for body 1 in world space.</param>
    /// <param name="axisBody2">The reference axis for body 2 in world space.</param>
    /// <param name="limit">The minimum and maximum allowed tilt angles.</param>
    /// <remarks>
    /// Each axis is stored as a local axis on the corresponding body. The constraint measures
    /// the angle between these axes and restricts it to the given range.
    /// Default values: <see cref="Softness"/> = <see cref="Constraint.DefaultAngularSoftness"/>, <see cref="Bias"/> = <see cref="Constraint.DefaultAngularBias"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="axisBody1"/> or <paramref name="axisBody2"/> is zero or contains a non-finite value.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="limit"/> is outside the range [0, pi], or when its upper value is smaller
    /// than its lower value, or when the limit fixes the tilt at zero or pi.
    /// </exception>
    public void Initialize(JVector axisBody1, JVector axisBody2, AngularLimit limit)
    {
        VerifyNotZero();
        ArgumentCheck.NonZero(axisBody1, nameof(axisBody1));
        ArgumentCheck.NonZero(axisBody2, nameof(axisBody2));
        ValidateLimit(limit, nameof(limit));

        ref ConeLimitData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.NormalizeInPlace(ref axisBody1);
        JVector.NormalizeInPlace(ref axisBody2);

        JVector.ConjugatedTransform(axisBody1, body1.Orientation, out data.LocalAxis1);
        JVector.ConjugatedTransform(axisBody2, body2.Orientation, out data.LocalAxis2);

        data.Softness = Constraint.DefaultAngularSoftness;
        data.BiasFactor = Constraint.DefaultAngularBias;

        Real lower = (Real)limit.From;
        Real upper = (Real)limit.To;

        data.MinAngle = lower;
        data.MaxAngle = upper;
    }

    /// <summary>
    /// Initializes the cone limit using a world-space axis and an angular range.
    /// </summary>
    /// <param name="axis">The reference axis in world space for the initial pose.</param>
    /// <param name="limit">The minimum and maximum allowed tilt angles.</param>
    /// <remarks>
    /// Stores the axis as a local axis on each body. The constraint measures the angle between
    /// these axes and restricts it to the given range.
    /// Default values: <see cref="Softness"/> = <see cref="Constraint.DefaultAngularSoftness"/>, <see cref="Bias"/> = <see cref="Constraint.DefaultAngularBias"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="axis"/> is zero or contains a non-finite value.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="limit"/> is outside the range [0, pi], or when its upper value is smaller
    /// than its lower value, or when the limit fixes the tilt at zero or pi.
    /// </exception>
    public void Initialize(JVector axis, AngularLimit limit)
    {
        if (limit.From > (JAngle)0.0)
        {
            Logger.Warning(
                "{0}.{1}(): The lower limit is greater than 0, but this overload initializes both body axes " +
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

            return (JAngle)StableMath.Atan2(JVector.Cross(a1, a2).Length(), JVector.Dot(a1, a2));
        }
    }

    /// <summary>
    /// Gets or sets the reference axis of body 1 in world space.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the assigned value is zero or contains a non-finite value.
    /// </exception>
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
            ArgumentCheck.NonZero(value, nameof(value));

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
    /// <exception cref="ArgumentException">
    /// Thrown when the assigned value is zero or contains a non-finite value.
    /// </exception>
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
            ArgumentCheck.NonZero(value, nameof(value));

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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned limit is outside the range [0, pi], or when its upper value is smaller
    /// than its lower value, or when the limit fixes the tilt at zero or pi.
    /// </exception>
    public AngularLimit Limit
    {
        get
        {
            ref ConeLimitData data = ref Data;
            return new AngularLimit((JAngle)data.MinAngle, (JAngle)data.MaxAngle);
        }
        set
        {
            ValidateLimit(value, nameof(value));

            ref ConeLimitData data = ref Data;
            data.MinAngle = (Real)value.From;
            data.MaxAngle = (Real)value.To;
        }
    }

    private static void ValidateLimit(in AngularLimit limit, string parameterName)
    {
        ArgumentCheck.InRange((Real)limit.From, (Real)0.0, MathR.PI, nameof(limit.From));
        ArgumentCheck.InRange((Real)limit.To, (Real)limit.From, MathR.PI, nameof(limit.To));
        if (limit.From == limit.To && (limit.From == (JAngle)0 || limit.From == (JAngle)MathR.PI))
        {
            throw new ArgumentOutOfRangeException(parameterName,
                "A fixed tilt of zero or pi is not supported. Use HingeAngle with AngularLimit.Full instead.");
        }
    }

    public static void PrepareForIterationConeLimit(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, ConeLimitData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector a1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector a2);

        var jacobian = new Span<JVector>(Unsafe.AsPointer(ref data.J0), 2);

        JVector previousJacobian = jacobian[0];

        jacobian[0] = JVector.Cross(a2, a1);
        jacobian[1] = JVector.Cross(a1, a2);

        data.Clamp = 0;

        Real error = JVector.Dot(a1, a2);
        Real lower = -data.MinAngle, upper = -data.MaxAngle;

        // Use an angle coordinate throughout, so softness and cached impulses keep
        // the same units. The residual uses its negative to retain the limit order;
        // the Jacobian and the negated bias below use the positive angle.
        Real sineSquared = jacobian[0].LengthSquared();
        Real sine = MathR.Sqrt(sineSquared);
        if (sine > 0)
        {
            jacobian[0] *= (Real)1.0 / sine;
        }
        else
        {
            // At an endpoint the angle has a cusp. Choose a one-sided derivative
            // about a responsive perpendicular axis.
            JVector axis = MathHelper.CreateOrthonormal(a1);
            JVector other = a1 % axis;
            JSymmetricMatrix inverseInertia = body1.InverseInertiaWorld + body2.InverseInertiaWorld;
            jacobian[0] = JVector.Transform(axis, inverseInertia) * axis >=
                          JVector.Transform(other, inverseInertia) * other ? axis : other;
        }

        jacobian[1] = -jacobian[0];
        if (previousJacobian * jacobian[0] < 0) data.AccumulatedImpulse = 0;
        error = -StableMath.Atan2(sine, error);

        if (lower == upper)
        {
            data.Clamp = 3;
            error -= upper;
        }
        else if (error <= upper && data.MaxAngle < MathR.PI)
        {
            data.Clamp = 1;
            error -= upper;
        }
        else if (error >= lower && data.MinAngle > 0)
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

        data.EffectiveMass = JVector.Transform(jacobian[0], body1.InverseInertiaWorld) * jacobian[0] +
                             JVector.Transform(jacobian[1], body2.InverseInertiaWorld) * jacobian[1];

        data.EffectiveMass += data.Softness * timeStep.InverseSubstepDt;

        data.EffectiveMass = data.EffectiveMass > 0 ? (Real)1.0 / data.EffectiveMass : 0;
        if (data.EffectiveMass == 0) data.AccumulatedImpulse = 0;

        data.Bias = -error * data.BiasFactor * timeStep.InverseStepDt;

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

    public static void IterateConeLimit(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, ConeLimitData>(ref constraint);
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        if (data.Clamp == 0) return;

        var jacobian = new Span<JVector>(Unsafe.AsPointer(ref data.J0), 2);

        Real jv =
            body1.AngularVelocity * jacobian[0] +
            body2.AngularVelocity * jacobian[1];

        Real softnessScalar = data.AccumulatedImpulse * data.Softness * timeStep.InverseSubstepDt;

        Real lambda = -data.EffectiveMass * (jv + data.Bias + softnessScalar);

        Real oldAccumulated = data.AccumulatedImpulse;

        data.AccumulatedImpulse += lambda;

        if (data.Clamp == 1)
        {
            data.AccumulatedImpulse = MathR.Min(data.AccumulatedImpulse, (Real)0.0);
        }
        else if (data.Clamp == 2)
        {
            data.AccumulatedImpulse = MathR.Max(data.AccumulatedImpulse, (Real)0.0);
        }

        lambda = data.AccumulatedImpulse - oldAccumulated;

        body1.AngularVelocity += JVector.Transform(lambda * jacobian[0], body1.InverseInertiaWorld);
        body2.AngularVelocity += JVector.Transform(lambda * jacobian[1], body2.InverseInertiaWorld);
    }

    public override void DebugDraw(IDebugDrawer drawer)
    {
        ref ConeLimitData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector axis1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector axis2);

        const Real axisLength = (Real)0.5;
        drawer.DrawSegment(body1.Position, body1.Position + axis1 * axisLength);
        drawer.DrawSegment(body2.Position, body2.Position + axis2 * axisLength);
    }
}
