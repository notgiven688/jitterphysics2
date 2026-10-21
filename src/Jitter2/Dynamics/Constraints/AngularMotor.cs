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
/// Drives relative angular velocity about an axis attached to each body.
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
    /// The axes rotate with their respective bodies and are normalized internally.
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

        if (body1.MotionType == MotionType.Dynamic)
        {
            body1.AngularVelocity -= JVector.Transform(j1 * data.AccumulatedImpulse, body1.InverseInertiaWorld);
        }
        if (body2.MotionType == MotionType.Dynamic)
        {
            body2.AngularVelocity += JVector.Transform(j2 * data.AccumulatedImpulse, body2.InverseInertiaWorld);
        }
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

        if (body1.MotionType == MotionType.Dynamic)
        {
            body1.AngularVelocity -= JVector.Transform(j1 * lambda, body1.InverseInertiaWorld);
        }
        if (body2.MotionType == MotionType.Dynamic)
        {
            body2.AngularVelocity += JVector.Transform(j2 * lambda, body2.InverseInertiaWorld);
        }
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
        Real length = settings.GuideSize * (Real)0.4;
        drawer.DrawSegment(origin, origin + first * length, settings.ReferenceColor);
        drawer.DrawSegment(secondOrigin, secondOrigin + second * length, settings.CurrentColor);

        // Show body 2's target rotation, or body 1's opposite rotation when body 2 is fixed.
        bool drawFirst = Body2.MotionType != MotionType.Dynamic;
        JVector axis = drawFirst ? first : second;
        JVector center = drawFirst ? origin : secondOrigin;
        // Keep the arrow attached to the other body's frame, rather than choosing
        // a new perpendicular from the slightly changing world-space motor axis.
        JQuaternion referenceOrientation = drawFirst ? Body2.Orientation : Body1.Orientation;
        JVector referenceAxis = drawFirst ? data.LocalAxis2 : data.LocalAxis1;
        JVector reference = JVector.Transform(MathHelper.CreateOrthonormal(referenceAxis), referenceOrientation);
        JVector radial = JVector.Transform(reference, JQuaternion.CreateFromToRotation(drawFirst ? second : first, axis));
        drawer.DrawCircle(center, axis, radial, settings.GuideSize, settings.ReferenceColor, settings.Segments);
        if (data.Velocity == 0) return;
        Real sign = (data.Velocity > 0 ? 1 : -1) * (drawFirst ? -1 : 1);
        JVector vector = (axis % radial) * (sign * settings.GuideSize * (Real)0.2);
        JVector tip = center + radial * settings.GuideSize;
        drawer.DrawArrow(tip - vector, vector, settings.CurrentColor, settings);
    }
}
