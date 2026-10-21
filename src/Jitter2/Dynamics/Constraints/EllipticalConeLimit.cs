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
/// Restricts relative swing to the region between two elliptical cones, leaving twist free.
/// </summary>
/// <remarks>
/// The cone is attached to body 1 and limits the tilt of an axis attached to body 2.
/// The two angular ranges set the inner and outer tilt limits in perpendicular directions.
/// Matching ranges produce a circular cone; matching inner and outer limits keep the axis
/// on the cone surface. Translation remains free.
/// </remarks>
public unsafe class EllipticalConeLimit : Constraint<EllipticalConeLimit.EllipticalConeLimitData>
{
    [StructLayout(LayoutKind.Sequential)]
    public struct EllipticalConeLimitData
    {
        internal int _internal;
        internal uint DispatchId;
        internal ulong ConstraintId;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAxis1, LocalAxis2, LocalReference1;

        public Real MinAngle1;
        public Real MaxAngle1;
        public Real MinAngle2;
        public Real MaxAngle2;

        public Real Softness;
        public Real BiasFactor;

        public Real EffectiveMass;
        public Real AccumulatedImpulse;
        public Real Bias;

        public JVector Jacobian;

        public short Clamp;
    }

    private static readonly uint RegisteredDispatchId =
        RegisterFullConstraint(&PrepareForIterationEllipticalConeLimit, &IterateEllipticalConeLimit);

    protected override void Create()
    {
        DispatchId = RegisteredDispatchId;
        base.Create();
    }

    /// <inheritdoc />
    public override void ResetWarmStart() => Data.AccumulatedImpulse = 0;

    /// <summary>
    /// Initializes both body axes from the same world-space direction.
    /// </summary>
    /// <remarks>
    /// The reference direction sets one direction of the ellipse on body 1; it is projected
    /// perpendicular to the cone axis. The other direction is perpendicular to both.
    /// A positive inner boundary means the initially aligned pose lies outside the allowed region.
    /// See the two-axis overload to initialize a pose with nonzero swing.
    /// </remarks>
    public void Initialize(JVector axis, JVector referenceDirection,
        AngularLimit limitAlongReference, AngularLimit limitAlongPerpendicular)
    {
        Initialize(axis, axis, referenceDirection, limitAlongReference, limitAlongPerpendicular);
    }

    /// <summary>
    /// Initializes the constraint using world-space axes and a world-space reference direction on body 1.
    /// </summary>
    /// <param name="axisBody1">The cone's central axis, attached to body 1.</param>
    /// <param name="axisBody2">The axis attached to body 2 whose swing is constrained.</param>
    /// <param name="referenceDirectionBody1">The first ellipse direction, attached to body 1.</param>
    /// <param name="limitAlongReference">Inner and outer angular semiaxes in the reference direction.</param>
    /// <param name="limitAlongPerpendicular">Inner and outer angular semiaxes in the perpendicular direction.</param>
    /// <exception cref="ArgumentException">A direction is invalid or the reference is parallel to the cone axis.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The limits do not satisfy <see cref="SetLimits"/>.</exception>
    public void Initialize(JVector axisBody1, JVector axisBody2, JVector referenceDirectionBody1,
        AngularLimit limitAlongReference, AngularLimit limitAlongPerpendicular)
    {
        VerifyNotZero();
        ArgumentCheck.NonZero(axisBody1, nameof(axisBody1));
        ArgumentCheck.NonZero(axisBody2, nameof(axisBody2));
        ArgumentCheck.NonZero(referenceDirectionBody1, nameof(referenceDirectionBody1));
        ValidateLimits(limitAlongReference, limitAlongPerpendicular);

        JVector.NormalizeInPlace(ref axisBody1);
        JVector.NormalizeInPlace(ref axisBody2);
        JVector.NormalizeInPlace(ref referenceDirectionBody1);

        referenceDirectionBody1 -= axisBody1 * JVector.Dot(axisBody1, referenceDirectionBody1);

        if (referenceDirectionBody1.LengthSquared() < (Real)1e-12)
        {
            throw new ArgumentException("The reference direction must not be parallel to the cone axis.",
                nameof(referenceDirectionBody1));
        }

        JVector.NormalizeInPlace(ref referenceDirectionBody1);

        ref var data = ref Data;

        JVector.ConjugatedTransform(axisBody1, data.Body1.Data.Orientation, out data.LocalAxis1);
        JVector.ConjugatedTransform(axisBody2, data.Body2.Data.Orientation, out data.LocalAxis2);
        JVector.ConjugatedTransform(referenceDirectionBody1, data.Body1.Data.Orientation, out data.LocalReference1);

        data.Softness = DefaultAngularSoftness;
        data.BiasFactor = DefaultAngularBias;
        data.Jacobian = JVector.Zero;

        SetLimits(limitAlongReference, limitAlongPerpendicular);
    }

    /// <summary>
    /// Gets body 1's central cone axis in world space.
    /// </summary>
    public JVector AxisBody1 => JVector.Transform(Data.LocalAxis1, Data.Body1.Data.Orientation);

    /// <summary>
    /// Gets body 2's constrained axis in world space.
    /// </summary>
    public JVector AxisBody2 => JVector.Transform(Data.LocalAxis2, Data.Body2.Data.Orientation);

    /// <summary>
    /// Gets the first ellipse direction in world space, perpendicular to <see cref="AxisBody1"/>.
    /// </summary>
    public JVector ReferenceDirectionBody1 => JVector.Transform(Data.LocalReference1, Data.Body1.Data.Orientation);

    /// <summary>
    /// Gets the inner and outer angular semiaxes in the reference direction.
    /// </summary>
    public AngularLimit LimitAlongReference => new((JAngle)Data.MinAngle1, (JAngle)Data.MaxAngle1);

    /// <summary>
    /// Gets the inner and outer angular semiaxes in the perpendicular direction.
    /// </summary>
    public AngularLimit LimitAlongPerpendicular => new((JAngle)Data.MinAngle2, (JAngle)Data.MaxAngle2);

    /// <summary>
    /// Sets the inner and outer tilt limits in both perpendicular directions.
    /// </summary>
    /// <remarks>
    /// Each range must satisfy 0 &lt;= From &lt;= To &lt; pi, with To strictly positive.
    /// The lower bounds must either both be zero (no inner boundary) or both be positive.
    /// Equal bounds in both ranges define a fixed elliptical cone surface.
    /// Fixed zero or pi tilt is unsupported; use HingeAngle with AngularLimit.Full instead.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The ranges violate the requirements above.</exception>
    public void SetLimits(AngularLimit limitAlongReference, AngularLimit limitAlongPerpendicular)
    {
        ValidateLimits(limitAlongReference, limitAlongPerpendicular);

        ref var data = ref Data;

        data.MinAngle1 = (Real)limitAlongReference.From;
        data.MaxAngle1 = (Real)limitAlongReference.To;
        data.MinAngle2 = (Real)limitAlongPerpendicular.From;
        data.MaxAngle2 = (Real)limitAlongPerpendicular.To;

        data.AccumulatedImpulse = 0;
        data.Clamp = 0;
    }

    private static void ValidateLimits(AngularLimit first, AngularLimit second)
    {
        ValidateRange(first, nameof(first));
        ValidateRange(second, nameof(second));

        if ((first.From == (JAngle)0) != (second.From == (JAngle)0))
        {
            throw new ArgumentOutOfRangeException(nameof(second),
                "The inner semiaxes must either both be zero or both be positive.");
        }
    }

    private static void ValidateRange(AngularLimit limit, string parameterName)
    {
        Real min = (Real)limit.From;
        Real max = (Real)limit.To;

        if (!Real.IsFinite(min) || !Real.IsFinite(max) || min < 0 || max <= 0 || max >= MathR.PI || min > max)
        {
            throw new ArgumentOutOfRangeException(parameterName,
                "Expected 0 <= From <= To < pi and To > 0. Use HingeAngle with AngularLimit.Full for fixed zero or pi tilt.");
        }
    }

    /// <summary>
    /// Gets signed swing-angle coordinates along the reference and perpendicular directions.
    /// Their Euclidean length is the tilt angle. At exactly opposite axes the swing direction is ambiguous.
    /// </summary>
    public void GetSwing(out JAngle alongReference, out JAngle alongPerpendicular)
    {
        JVector axis = AxisBody1;
        JVector reference = ReferenceDirectionBody1;
        JVector other = axis % reference;
        JVector target = AxisBody2;

        Real x = target * reference;
        Real y = target * other;
        Real sine = MathR.Sqrt(x * x + y * y);
        Real angle = StableMath.Atan2(sine, target * axis);
        Real factor = sine > 0 ? angle / sine : 0;

        alongReference = (JAngle)(sine > 0 ? x * factor : angle);
        alongPerpendicular = (JAngle)(y * factor);
    }

    /// <summary>
    /// Gets or sets the softness (compliance). Default is 0.001.
    /// </summary>
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
    /// Gets or sets the angular error correction factor. Default is 0.2.
    /// </summary>
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

    // Radius and logarithmic azimuth derivative of the ellipse in swing-angle coordinates.
    private static Real EllipseRadius(Real a, Real b, Real cosine, Real sine, out Real derivative)
    {
        Real scale = MathR.Min(a, b);
        Real u = scale / a;
        Real v = scale / b;

        Real squared = cosine * cosine * u * u + sine * sine * v * v;
        derivative = cosine * sine * (v * v - u * u) / squared;

        return scale / MathR.Sqrt(squared);
    }

    public static void PrepareForIterationEllipticalConeLimit(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, EllipticalConeLimitData>(ref constraint);

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector axis = JVector.Transform(data.LocalAxis1, body1.Orientation);
        JVector reference = JVector.Transform(data.LocalReference1, body1.Orientation);
        JVector other = axis % reference;
        JVector target = JVector.Transform(data.LocalAxis2, body2.Orientation);

        Real x = target * reference;
        Real y = target * other;
        Real z = target * axis;
        Real sine = MathR.Sqrt(x * x + y * y);
        Real angle = StableMath.Atan2(sine, z);

        Real cosineAzimuth, sineAzimuth;

        if (sine > 0)
        {
            cosineAzimuth = x / sine;
            sineAzimuth = y / sine;
        }
        else
        {
            // The logarithm has no unique direction at either pole. Pick a principal direction
            // with a responsive angular axis, including when one body's rotation is restricted.
            JSymmetricMatrix inertia = body1.InverseInertiaWorld + body2.InverseInertiaWorld;
            bool useReference = JVector.Transform(other, inertia) * other >= JVector.Transform(reference, inertia) * reference;
            cosineAzimuth = useReference ? 1 : 0;
            sineAzimuth = useReference ? 0 : 1;
        }

        Real outer = EllipseRadius(data.MaxAngle1, data.MaxAngle2, cosineAzimuth, sineAzimuth, out Real derivative);
        bool fixedCone = data.MinAngle1 == data.MaxAngle1 && data.MinAngle2 == data.MaxAngle2;
        Real boundary = outer;
        short clamp = 0;

        if (fixedCone)
        {
            clamp = 3;
        }
        else if (angle >= outer)
        {
            clamp = 1;
        }
        else if (data.MinAngle1 > 0)
        {
            Real inner = EllipseRadius(data.MinAngle1, data.MinAngle2, cosineAzimuth, sineAzimuth, out Real innerDerivative);

            if (angle <= inner)
            {
                clamp = 2;
                boundary = inner;
                derivative = innerDerivative;
            }
        }

        short previousClamp = data.Clamp;
        data.Clamp = clamp;

        if (clamp == 0)
        {
            data.AccumulatedImpulse = 0;
            return;
        }

        JVector tiltDirection = cosineAzimuth * reference + sineAzimuth * other;
        JVector azimuthDirection = axis % tiltDirection;
        JVector angleDirection = z * tiltDirection - sine * axis;

        // Differentiate sqrt((swing1/a)^2 + (swing2/b)^2) - 1, then scale the row
        // to a unit angular Jacobian. This includes the changing azimuth of the ellipse;
        // using only cross(target, axis) would incorrectly make the limit circular.
        Real azimuthFactor = sine > 0 ? angle / sine * derivative : 0;

        // Scale before taking the length: close to the antipode, the logarithm's
        // azimuth derivative can be large enough to overflow a squared vector length.
        Real scale = MathR.Max((Real)1, MathR.Abs(azimuthFactor));
        JVector jacobian = -azimuthDirection * ((Real)1 / scale) + (azimuthFactor / scale) * angleDirection;
        Real inverseLength = (Real)1 / jacobian.Length();

        jacobian *= inverseLength;
        inverseLength /= scale;

        if (previousClamp != clamp || data.Jacobian * jacobian < 0)
        {
            data.AccumulatedImpulse = 0;
        }

        data.Jacobian = jacobian;

        if (clamp == 1)
        {
            data.AccumulatedImpulse = MathR.Min(data.AccumulatedImpulse, 0);
        }
        else if (clamp == 2)
        {
            data.AccumulatedImpulse = MathR.Max(data.AccumulatedImpulse, 0);
        }

        Real mass = JVector.Transform(jacobian, body1.InverseInertiaWorld) * jacobian +
                    JVector.Transform(jacobian, body2.InverseInertiaWorld) * jacobian +
                    data.Softness * timeStep.InverseSubstepDt;
        data.EffectiveMass = mass > 0 ? (Real)1 / mass : 0;

        if (data.EffectiveMass == 0)
        {
            data.AccumulatedImpulse = 0;
        }

        data.Bias = (angle - boundary) * inverseLength * data.BiasFactor * timeStep.InverseStepDt;

        if (body1.MotionType == MotionType.Dynamic)
        {
            body1.AngularVelocity += JVector.Transform(data.AccumulatedImpulse * jacobian, body1.InverseInertiaWorld);
        }
        if (body2.MotionType == MotionType.Dynamic)
        {
            body2.AngularVelocity -= JVector.Transform(data.AccumulatedImpulse * jacobian, body2.InverseInertiaWorld);
        }
    }

    public static void IterateEllipticalConeLimit(ref ConstraintData constraint, in TimeStep timeStep)
    {
        ref var data = ref Unsafe.As<ConstraintData, EllipticalConeLimitData>(ref constraint);

        if (data.Clamp == 0) return;

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        Real jv = (body1.AngularVelocity - body2.AngularVelocity) * data.Jacobian;
        Real lambda = -data.EffectiveMass * (jv + data.Bias +
            data.AccumulatedImpulse * data.Softness * timeStep.InverseSubstepDt);

        Real old = data.AccumulatedImpulse;
        data.AccumulatedImpulse += lambda;

        if (data.Clamp == 1)
        {
            data.AccumulatedImpulse = MathR.Min(data.AccumulatedImpulse, 0);
        }
        else if (data.Clamp == 2)
        {
            data.AccumulatedImpulse = MathR.Max(data.AccumulatedImpulse, 0);
        }

        lambda = data.AccumulatedImpulse - old;

        if (body1.MotionType == MotionType.Dynamic)
        {
            body1.AngularVelocity += JVector.Transform(lambda * data.Jacobian, body1.InverseInertiaWorld);
        }
        if (body2.MotionType == MotionType.Dynamic)
        {
            body2.AngularVelocity -= JVector.Transform(lambda * data.Jacobian, body2.InverseInertiaWorld);
        }
    }

    /// <inheritdoc />
    public override void DebugDraw(IDebugDrawer drawer) => DebugDraw(drawer, DebugDrawSettings.Default);

    /// <inheritdoc />
    public override void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        VerifyNotZero();

        JVector origin = GetDebugDrawOrigin(settings);
        JVector axis = AxisBody1, reference = ReferenceDirectionBody1;

        drawer.DrawArrow(origin, axis * settings.GuideSize, settings.ReferenceColor, settings);
        drawer.DrawArrow(origin, AxisBody2 * settings.GuideSize, settings.CurrentColor, settings);

        drawer.DrawCone(origin, axis, reference, (JAngle)Data.MaxAngle1, (JAngle)Data.MaxAngle2,
            settings.GuideSize, settings.UpperLimitColor, settings.Segments);

        if (Data.MinAngle1 > 0 && (Data.MinAngle1 != Data.MaxAngle1 || Data.MinAngle2 != Data.MaxAngle2))
        {
            drawer.DrawCone(origin, axis, reference, (JAngle)Data.MinAngle1, (JAngle)Data.MinAngle2,
                settings.GuideSize, settings.LowerLimitColor, settings.Segments, settings.Segments / 16);
        }
    }
}
