/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;
using Jitter2.Dynamics;

namespace Jitter2.Dynamics.Constraints;

internal static class ServoMath
{
    internal static Real CheckMaximum(Real value)
    {
        if (Real.IsNaN(value) || value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), value, "Maximum force or torque must be non-negative.");
        return value;
    }

    internal static void WakeBodies(Constraint servo)
    {
        if (servo.Body1.MotionType == MotionType.Dynamic && !servo.Body1.IsActive)
            servo.Body1.SetActivationState(true);
        if (servo.Body2.MotionType == MotionType.Dynamic && !servo.Body2.IsActive)
            servo.Body2.SetActivationState(true);
    }

    internal static Real MaximumImpulse(Real maximum, Real dt) => dt > 0 ? maximum * dt : 0;

    internal static void ClampMagnitude(ref JVector impulse, Real maximumImpulse)
    {
        if (Real.IsPositiveInfinity(maximumImpulse)) return;
        Real lengthSquared = impulse.LengthSquared();
        if (lengthSquared > maximumImpulse * maximumImpulse)
            impulse *= maximumImpulse / MathR.Sqrt(lengthSquared);
    }

    internal static void ClampAngular(ref JVector impulse, in JMatrix jacobian, Real maximumImpulse)
    {
        if (Real.IsPositiveInfinity(maximumImpulse)) return;
        // The solver coordinates are not necessarily unit axes. Limit the angular
        // impulse actually applied to a body, then scale the stored coordinates with it.
        Real lengthSquared = JVector.TransposedTransform(impulse, jacobian).LengthSquared();
        if (lengthSquared > maximumImpulse * maximumImpulse)
            impulse *= maximumImpulse / MathR.Sqrt(lengthSquared);
    }
}
