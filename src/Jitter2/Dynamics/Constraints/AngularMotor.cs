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
/// Represents a motor that drives relative angular velocity between two axes fixed
/// in the reference frames of their respective bodies.
/// </summary>
public unsafe class AngularMotor : Constraint<AngularMotor.AngularMotorData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct AngularMotorData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAxis1;
        public JVector LocalAxis2;

        public Real Velocity;
        public Real MaxForce;
        public Real MaxLambda;

        public Real EffectiveMass;

        public Real AccumulatedImpulse;
    }

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationAngularMotor, &IterateAngularMotor);

    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = (Real)0.0;

    /// <summary>
    /// Initializes the motor with separate axes for each body.
    /// </summary>
    /// <param name="axis1">The motor axis on the first body in world space.</param>
    /// <param name="axis2">The motor axis on the second body in world space.</param>
    /// <remarks>
    /// Stores the axes in local frames. Both axes are normalized internally.
    /// Default values: <see cref="TargetVelocity"/> = 0, <see cref="MaximumTorque"/> = 0.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="axis1"/> or <paramref name="axis2"/> is zero or contains a non-finite value.
    /// </exception>
    public void Initialize(JVector axis1, JVector axis2)
    {
        VerifyNotZero();
        ArgumentCheck.NonZero(axis1, nameof(axis1));
        ArgumentCheck.NonZero(axis2, nameof(axis2));

        ref AngularMotorData data = ref Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.NormalizeInPlace(ref axis1);
        JVector.NormalizeInPlace(ref axis2);

        JVector.ConjugatedTransform(axis1, body1.Orientation, out data.LocalAxis1);
        JVector.ConjugatedTransform(axis2, body2.Orientation, out data.LocalAxis2);

        data.MaxForce = 0;
        data.Velocity = 0;
    }

    /// <summary>
    /// Initializes the motor with the same axis for both bodies.
    /// </summary>
    /// <param name="axis">The motor axis in world space, used for both bodies.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="axis"/> is zero or contains a non-finite value.
    /// </exception>
    public void Initialize(JVector axis)
    {
        Initialize(axis, axis);
    }

    /// <summary>
    /// Gets or sets the target angular velocity in radians per second.
    /// </summary>
    /// <value>Default is 0.</value>
    public Real TargetVelocity
    {
        get => Data.Velocity;
        set
        {
            DebugCheck.IsFinite(value, nameof(value));
            Data.Velocity = value;
        }
    }

    /// <summary>
    /// Gets the motor axis on the first body in local space.
    /// </summary>
    public JVector LocalAxis1 => Data.LocalAxis1;

    /// <summary>
    /// Gets the motor axis on the second body in local space.
    /// </summary>
    public JVector LocalAxis2 => Data.LocalAxis2;

    /// <summary>
    /// Gets or sets the maximum torque the motor can apply.
    /// </summary>
    /// <value>Default is 0. Must be non-negative.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is negative.
    /// </exception>
    public Real MaximumTorque
    {
        get => Data.MaxForce;
        set
        {
            Data.MaxForce = ArgumentCheck.NonNegative(value, nameof(value));
        }
    }

    /// <summary>
    /// Gets or sets the maximum torque the motor can apply.
    /// </summary>
    /// <remarks>Use <see cref="MaximumTorque"/> instead.</remarks>
    [Obsolete("Use MaximumTorque instead.")]
    public Real MaximumForce
    {
        get => MaximumTorque;
        set => MaximumTorque = value;
    }

    public static void PrepareForIterationAngularMotor(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, AngularMotorData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector j1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector j2);

        data.EffectiveMass = JVector.Transform(j1, body1.InverseInertiaWorld) * j1 +
                             JVector.Transform(j2, body2.InverseInertiaWorld) * j2;
        data.EffectiveMass = data.EffectiveMass > 0 ? (Real)1.0 / data.EffectiveMass : 0;
        if (data.EffectiveMass == 0) data.AccumulatedImpulse = 0;

        data.MaxLambda = timeStep.SubstepDt * data.MaxForce;

        body1.AngularVelocity -= JVector.Transform(j1 * data.AccumulatedImpulse, body1.InverseInertiaWorld);
        body2.AngularVelocity += JVector.Transform(j2 * data.AccumulatedImpulse, body2.InverseInertiaWorld);
    }

    public static void IterateAngularMotor(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, AngularMotorData>(ref constraint);

        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector j1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector j2);

        Real jv = -j1 * body1.AngularVelocity + j2 * body2.AngularVelocity;

        Real lambda = -(jv - data.Velocity) * data.EffectiveMass;

        Real oldAccumulated = data.AccumulatedImpulse;

        data.AccumulatedImpulse += lambda;

        data.AccumulatedImpulse = Math.Clamp(data.AccumulatedImpulse, -data.MaxLambda, data.MaxLambda);

        lambda = data.AccumulatedImpulse - oldAccumulated;

        body1.AngularVelocity -= JVector.Transform(j1 * lambda, body1.InverseInertiaWorld);
        body2.AngularVelocity += JVector.Transform(j2 * lambda, body2.InverseInertiaWorld);
    }

    public override void DebugDraw(IDebugDrawer drawer) => DebugDraw(drawer, DebugDrawSettings.Default);

    /// <inheritdoc />
    public override void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        VerifyNotZero();
        ref AngularMotorData data = ref Data;
        JVector first = JVector.Transform(data.LocalAxis1, Body1.Orientation);
        JVector second = JVector.Transform(data.LocalAxis2, Body2.Orientation);
        JVector origin = GetDebugDrawOrigin(settings);
        JVector secondOrigin = settings.Origin ?? (Body2 == Body2.World.NullBody ? Body1.Position : Body2.Position);
        JVector reference = JVector.Transform(MathHelper.CreateOrthonormal(data.LocalAxis1), Body1.Orientation);
        JVector secondReference = JVector.Transform(MathHelper.CreateOrthonormal(data.LocalAxis2), Body2.Orientation);
        bool firstResponds = JVector.Dot(first, JVector.Transform(first, Body1.Data.InverseInertiaWorld)) > 0;
        bool secondResponds = JVector.Dot(second, JVector.Transform(second, Body2.Data.InverseInertiaWorld)) > 0;
        if (firstResponds != secondResponds)
        {
            JVector axis = firstResponds ? first : second;
            JVector center = settings.Origin ?? (firstResponds ? Body1.Position : Body2.Position);
            JVector fixedAxis = firstResponds ? second : first;
            JVector fixedReference = firstResponds ? secondReference : reference;
            // Anchor the indicator to the support so it does not orbit with the rotor.
            JVector radial = JVector.Transform(fixedReference, JQuaternion.CreateFromToRotation(fixedAxis, axis));
            drawer.DrawCircle(center, axis, radial, settings.GuideSize, settings.ReferenceColor, settings.Segments);
            drawer.DrawArrow(center, axis * (settings.GuideSize * (Real)0.5), settings.CurrentColor, settings);
            if (data.Velocity != 0)
            {
                Real sign = (data.Velocity > 0 ? 1 : -1) * (firstResponds ? -1 : 1);
                JVector tangent = (axis % radial) * sign;
                JVector tip = center + radial * settings.GuideSize;
                JVector vector = tangent * (settings.GuideSize * (Real)0.25);
                drawer.DrawArrow(tip - vector, vector, settings.ReferenceColor, settings);
            }
            return;
        }
        // Different radii keep both guides readable when their origins and axes coincide.
        Real firstRadius = settings.GuideSize * (Real)0.8;
        drawer.DrawCircle(origin, first, reference, firstRadius, settings.ReferenceColor, settings.Segments);
        drawer.DrawCircle(secondOrigin, second, secondReference, settings.GuideSize, settings.CurrentColor, settings.Segments);
        drawer.DrawArrow(origin, first * (settings.GuideSize * (Real)0.5), settings.ReferenceColor, settings);
        drawer.DrawArrow(secondOrigin, second * (settings.GuideSize * (Real)0.5),
            settings.CurrentColor, settings);
        if (data.Velocity != 0 && firstResponds && secondResponds)
        {
            Real sign = data.Velocity > 0 ? 1 : -1;
            // Like LinearMotor, show the signed contribution for each body's own axis.
            drawer.DrawArrow(origin + reference * firstRadius,
                (first % reference) * (-sign * settings.GuideSize * (Real)0.5), settings.ReferenceColor, settings);
            drawer.DrawArrow(secondOrigin + secondReference * settings.GuideSize,
                (second % secondReference) * (sign * settings.GuideSize * (Real)0.5), settings.CurrentColor, settings);
        }
    }
}
