/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using Jitter2.LinearMath;
using Vertex = Jitter2.Collision.MinkowskiDifference.Vertex;

namespace Jitter2.Collision;

public static partial class NarrowPhase
{
    /// <summary>The outcome of a sweep over a specified time interval.</summary>
    public enum SweepStatus
    {
        /// <summary>The shapes remain separated throughout the interval.</summary>
        Miss,
        /// <summary>The shapes touch or overlap within the collision tolerance.</summary>
        Hit,
        /// <summary>The search exhausted its iteration budget or numerical precision.</summary>
        Failed
    }

    /// <summary>
    /// Calculates the time of impact for two translating and rotating convex shapes.
    /// </summary>
    /// <param name="supportA">Shape A.</param>
    /// <param name="supportB">Shape B.</param>
    /// <param name="orientationA">Initial orientation of shape A in world space.</param>
    /// <param name="orientationB">Initial orientation of shape B in world space.</param>
    /// <param name="positionA">Initial position of shape A in world space.</param>
    /// <param name="positionB">Initial position of shape B in world space.</param>
    /// <param name="sweepA">Linear velocity of shape A.</param>
    /// <param name="sweepB">Linear velocity of shape B.</param>
    /// <param name="sweepAngularA">World-space angular velocity of shape A.</param>
    /// <param name="sweepAngularB">World-space angular velocity of shape B.</param>
    /// <param name="extentA">Radius enclosing shape A about its rotation origin, including any local offset.</param>
    /// <param name="extentB">Radius enclosing shape B about its rotation origin, including any local offset.</param>
    /// <param name="pointA">Collision point on shape A in world space at the sweep origin.</param>
    /// <param name="pointB">Collision point on shape B in world space at the sweep origin.</param>
    /// <param name="normal">Unit normal from A to B at impact. Zero for initial overlap or an unsuccessful search.</param>
    /// <param name="lambda">Time of impact in seconds; zero for initial overlap, infinity for an unsuccessful search.</param>
    /// <returns>True if an impact is found. False if the shapes miss or the search fails.</returns>
    /// <remarks>
    /// Prefer the overload with a maximum time and a <see cref="SweepStatus"/> result when sweeping a simulation step.
    /// An unbounded search may fail to finish, especially when both shapes rotate without relative translation.
    /// </remarks>
    public static bool Sweep<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        in JVector sweepA, in JVector sweepB,
        in JVector sweepAngularA, in JVector sweepAngularB,
        in Real extentA, in Real extentB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where Ta : ISupportMappable where Tb : ISupportMappable =>
        Sweep(supportA, supportB, orientationA, orientationB, positionA, positionB,
            sweepA, sweepB, sweepAngularA, sweepAngularB, extentA, extentB, Real.PositiveInfinity,
            out pointA, out pointB, out normal, out lambda) == SweepStatus.Hit;

    /// <summary>
    /// Searches for the first impact of two translating and rotating convex shapes between zero and
    /// <paramref name="maximumTime"/> seconds, including both endpoints.
    /// </summary>
    /// <param name="supportA">Shape A.</param>
    /// <param name="supportB">Shape B.</param>
    /// <param name="orientationA">Initial orientation of shape A in world space.</param>
    /// <param name="orientationB">Initial orientation of shape B in world space.</param>
    /// <param name="positionA">Initial position of shape A in world space.</param>
    /// <param name="positionB">Initial position of shape B in world space.</param>
    /// <param name="sweepA">Linear velocity of shape A.</param>
    /// <param name="sweepB">Linear velocity of shape B.</param>
    /// <param name="sweepAngularA">World-space angular velocity of shape A.</param>
    /// <param name="sweepAngularB">World-space angular velocity of shape B.</param>
    /// <param name="extentA">Radius enclosing shape A about its rotation origin, including any local offset.</param>
    /// <param name="extentB">Radius enclosing shape B about its rotation origin, including any local offset.</param>
    /// <param name="maximumTime">Nonnegative last time to search. Positive infinity requests an unbounded search.</param>
    /// <param name="pointA">Collision point on shape A in world space at the sweep origin. Zero unless a hit is found.</param>
    /// <param name="pointB">Collision point on shape B in world space at the sweep origin. Zero unless a hit is found.</param>
    /// <param name="normal">Unit normal from A to B at impact. Zero for initial overlap or an unsuccessful search.</param>
    /// <param name="lambda">Time of impact in seconds; zero for initial overlap, infinity unless a hit is found.</param>
    /// <returns>Whether an impact was found, separation was proven, or the search could not finish.</returns>
    /// <remarks>
    /// Includes impacts occurring between separated endpoint poses.
    /// A hit is reported within a distance tolerance of 0.0001. Iteration exhaustion is reported as
    /// <see cref="SweepStatus.Failed"/> rather than a collision or a proven miss.
    /// </remarks>
    public static SweepStatus Sweep<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        in JVector sweepA, in JVector sweepB,
        in JVector sweepAngularA, in JVector sweepAngularB,
        in Real extentA, in Real extentB, Real maximumTime,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        ArgumentCheck.NonNegative(extentA, nameof(extentA));
        ArgumentCheck.NonNegative(extentB, nameof(extentB));
        ArgumentCheck.NotNaN(maximumTime, nameof(maximumTime));
        ArgumentOutOfRangeException.ThrowIfNegative(maximumTime);

        const Real tolerance = (Real)1e-4;
        const int maximumIterations = 128;
        pointA = pointB = normal = JVector.Zero;
        lambda = Real.PositiveInfinity;

        JVector relativePosition = positionB - positionA;
        JVector relativeVelocity = sweepB - sweepA;
        bool initiallySeparated = Distance(supportA, supportB, orientationA, orientationB,
            JVector.Zero, relativePosition, out JVector initialA, out JVector initialB,
            out JVector initialNormal, out Real initialDistance);
        if (!initiallySeparated || initialDistance <= tolerance)
        {
            pointA = positionA + initialA;
            pointB = positionA + initialB;
            lambda = 0;
            return SweepStatus.Hit;
        }

        Real speedA = sweepAngularA.Length(), speedB = sweepAngularB.Length();
        Real start = 0, end = maximumTime;
        bool completeInterval = true;

        // A shape's bounding sphere is centered on its rotation origin, not on its support center.
        // Rotation cannot take it outside this sphere. Relative translation bounds the time interval
        // in which the two spheres can meet, even when either shape is offset from its body origin.
        Real radius = extentA + extentB + tolerance;
        Real linearSpeed = relativeVelocity.Length();
        if (linearSpeed > 0)
        {
            JVector direction = relativeVelocity * ((Real)1.0 / linearSpeed);
            Real projection = JVector.Dot(relativePosition, direction);
            JVector perpendicular = relativePosition - projection * direction;
            Real discriminant = radius * radius - perpendicular.LengthSquared();
            if (discriminant < 0) return SweepStatus.Miss;
            Real halfSpan = MathR.Sqrt(discriminant);
            start = MathR.Max(0, (-projection - halfSpan) / linearSpeed);
            end = MathR.Min(end, (-projection + halfSpan) / linearSpeed);
            if (start > end) return SweepStatus.Miss;
        }
        else
        {
            if (relativePosition.LengthSquared() > radius * radius) return SweepStatus.Miss;
            if (Real.IsPositiveInfinity(end))
            {
                Real speed = MathR.Max(speedA, speedB);
                // Equal angular speeds (or a single rotating body) repeat after one revolution.
                // Otherwise this finite window can find a hit, but cannot prove an unbounded miss.
                completeInterval = speedA == 0 || speedB == 0 || speedA == speedB;
                end = speed > 0 ? StableMath.TwoPi / speed : 0;
            }
        }

        // Keep only the right endpoints of pending intervals. The current interval is always
        // the earliest one that has not been ruled out. Subdivision never steps over a possible hit.
        Span<Real> pendingEnds = stackalloc Real[64];
        int pendingCount = 0;
        JVector previousNormal = initialNormal;
        int iterations = 0;
        while (iterations++ < maximumIterations)
        {
            JQuaternion deltaA = MathHelper.RotationQuaternion(sweepAngularA, start);
            JQuaternion deltaB = MathHelper.RotationQuaternion(sweepAngularB, start);
            JQuaternion oriA = deltaA * orientationA, oriB = deltaB * orientationB;
            JVector offset = relativePosition + start * relativeVelocity;
            JVector pA = initialA, pB = initialB, axis = initialNormal;
            Real distance = initialDistance;
            bool separated = start == 0 || Distance(supportA, supportB, oriA, oriB, JVector.Zero, offset,
                out pA, out pB, out axis, out distance);

            if (!separated || distance <= tolerance)
            {
                // Distance works relative to A's translating origin. Undo only the rotations
                // to return anchors at the initial poses; the normal remains at the impact pose.
                pointA = positionA + JVector.ConjugatedTransform(pA, deltaA);
                pointB = positionB + JVector.ConjugatedTransform(pB - offset, deltaB);
                normal = start == 0 ? JVector.Zero : separated ? axis : previousNormal;
                lambda = start;
                return SweepStatus.Hit;
            }

            previousNormal = axis;
            while (true)
            {
                Real projectionStart = JVector.Dot(offset, axis);
                Real projectionEnd = JVector.Dot(relativePosition + end * relativeVelocity, axis);
                Real boundA = SweepSupportBound(supportA, orientationA, sweepAngularA, speedA,
                    extentA, axis, start, end, out Real firstA, out Real lastA);
                Real boundB = SweepSupportBound(supportB, orientationB, sweepAngularB, speedB,
                    extentB, -axis, start, end, out Real firstB, out Real lastB);
                Real separation = MathR.Min(projectionStart, projectionEnd) - boundA - boundB;

                // An independent bound preserves cancellation between translation and the two
                // rotations. Each rotating point deviates from its endpoint chord by at most
                // radius * omega^2 * duration^2 / 8. Linear translation has no chord error.
                Real duration = end - start;
                Real chordError = (extentA * speedA * speedA + extentB * speedB * speedB) *
                                  duration * duration * (Real)0.125;
                Real chordSeparation = MathR.Min(projectionStart - firstA - firstB,
                    projectionEnd - lastA - lastB) - chordError;
                separation = MathR.Max(separation, chordSeparation);

                if (separation > 0 || start == end)
                {
                    if (pendingCount == 0) return completeInterval ? SweepStatus.Miss : SweepStatus.Failed;
                    start = end;
                    end = pendingEnds[--pendingCount];
                    break;
                }

                Real middle = start + (end - start) * (Real)0.5;
                if (middle == start || middle == end || pendingCount == pendingEnds.Length)
                    return SweepStatus.Failed;
                pendingEnds[pendingCount++] = end;
                end = middle;
            }
        }

        return SweepStatus.Failed;
    }

    private static Real SweepSupportBound<T>(in T shape, in JQuaternion orientation,
        in JVector angularVelocity, Real angularSpeed, Real extent, in JVector axis, Real start, Real end,
        out Real firstProjection, out Real lastProjection)
        where T : ISupportMappable
    {
        if (angularSpeed == 0)
        {
            JVector localAxis = JVector.ConjugatedTransform(axis, orientation);
            shape.SupportMap(localAxis, out JVector support);
            return firstProjection = lastProjection = JVector.Dot(localAxis, support);
        }

#if USE_DOUBLE_PRECISION
        const Real roundoff = (Real)1e-12;
#else
        const Real roundoff = (Real)2e-6;
#endif
        // Account for rounding in the unit direction, rotations, and support projections.
        Real sphereBound = extent * ((Real)1.0 + roundoff);
        Real angle = angularSpeed * (end - start);
        if (angle >= StableMath.Pi) return firstProjection = lastProjection = sphereBound;

        JVector first = JVector.ConjugatedTransform(axis,
            MathHelper.RotationQuaternion(angularVelocity, start) * orientation);
        JVector last = JVector.ConjugatedTransform(axis,
            MathHelper.RotationQuaternion(angularVelocity, end) * orientation);
        JVector middle = JVector.ConjugatedTransform(axis,
            MathHelper.RotationQuaternion(angularVelocity, start + (end - start) * (Real)0.5) * orientation);
        JVector rotationAxis = JVector.ConjugatedTransform(angularVelocity * ((Real)1.0 / angularSpeed), orientation);
        JVector parallel = JVector.Dot(middle, rotationAxis) * rotationAxis;
        Real cosine = StableMath.Cos(angle * (Real)0.5);
        JVector tip = parallel + (middle - parallel) * ((Real)1.0 / cosine);

        // In the body's local frame the query direction follows a circular arc. For an arc
        // shorter than half a revolution, its endpoints and the intersection of their tangents
        // enclose the entire arc. A support function is convex, so its maximum on this triangle
        // is attained at one of these three directions. This bounds every intermediate pose,
        // including a stationary closest vertex while a different vertex swings toward the plane.
        shape.SupportMap(first, out JVector p0);
        shape.SupportMap(last, out JVector p1);
        shape.SupportMap(tip, out JVector p2);
        Real padding = roundoff * extent / cosine;
        firstProjection = JVector.Dot(first, p0) + padding;
        lastProjection = JVector.Dot(last, p1) + padding;
        Real bound = MathR.Max(firstProjection, MathR.Max(lastProjection, JVector.Dot(tip, p2) + padding));
        return MathR.Min(sphereBound, bound);
    }

    /// <summary>
    /// Calculates the time of impact and the collision points in world space for two shapes with linear velocities
    /// <paramref name="sweepA"/> and <paramref name="sweepB"/>.
    /// </summary>
    /// <param name="supportA">Shape A.</param>
    /// <param name="supportB">Shape B.</param>
    /// <param name="orientationA">Orientation of shape A in world space.</param>
    /// <param name="orientationB">Orientation of shape B in world space.</param>
    /// <param name="positionA">Position of shape A in world space.</param>
    /// <param name="positionB">Position of shape B in world space.</param>
    /// <param name="sweepA">Linear velocity of shape A.</param>
    /// <param name="sweepB">Linear velocity of shape B.</param>
    /// <param name="pointA">Collision point on shape A in world space at the sweep origin.</param>
    /// <param name="pointB">Collision point on shape B in world space at the sweep origin.</param>
    /// <param name="normal">Normalized collision normal in world space (points from A to B). Zero if the shapes already overlap or do not hit.</param>
    /// <param name="lambda">Time of impact. <see cref="Real.PositiveInfinity"/> if no hit is detected, zero if shapes overlap.</param>
    /// <returns>True if the shapes will hit or already overlap, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Sweep<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        in JVector sweepA, in JVector sweepB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // rotate into the reference frame of bodyA...
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // we also transform the relative velocities
        JVector sweep = sweepB - sweepA;
        JVector.ConjugatedTransform(sweep, orientationA, out sweep);

        // ...perform toi calculation
        bool res = Sweep(supportA, supportB, orientation, position, sweep,
            out pointA, out pointB, out normal, out lambda);

        if (!res) return false;

        // ...rotate back. This approach potentially saves some matrix-vector multiplication when the support function is
        // called multiple times.
        JVector.Transform(pointA, orientationA, out pointA);
        JVector.Add(pointA, positionA, out pointA);
        JVector.Transform(pointB, orientationA, out pointB);
        JVector.Add(pointB, positionA, out pointB);
        JVector.Transform(normal, orientationA, out normal);

        // transform back from the relative velocities

        // This is where the collision will occur in world space:
        //      pointA += lambda * sweepA;
        //      pointB += lambda * sweepA; // sweepA is not a typo

        pointB += lambda * (sweepA - sweepB);

        return true;
    }

    /// <summary>
    /// Performs a sweep test where shape A is fixed at the origin with identity orientation and zero velocity.
    /// </summary>
    /// <param name="supportA">Shape A (fixed at the origin, identity orientation).</param>
    /// <param name="supportB">Shape B.</param>
    /// <param name="orientationB">Orientation of shape B in world space.</param>
    /// <param name="positionB">Position of shape B in world space.</param>
    /// <param name="sweepB">Linear velocity of shape B.</param>
    /// <param name="pointA">Collision point on shape A in world space at the sweep origin.</param>
    /// <param name="pointB">Collision point on shape B in world space at the sweep origin.</param>
    /// <param name="normal">Normalized collision normal in world space (points from A to B). Zero if the shapes already overlap or do not hit.</param>
    /// <param name="lambda">Time of impact. <see cref="Real.PositiveInfinity"/> if no hit is detected, zero if shapes overlap.</param>
    /// <returns>True if the shapes hit or already overlap, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Sweep<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationB, in JVector positionB, in JVector sweepB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // ..perform toi calculation
        const Real collideEpsilon = (Real)1e-4;
        const int maxIter = 34;

        Unsafe.SkipInit(out SimplexSolverAB simplexSolver);
        simplexSolver.Reset();

        MinkowskiDifference.GetCenter(supportA, supportB, orientationB, positionB, out var center);

        JVector posB = positionB;

        lambda = (Real)0.0;

        pointA = pointB = JVector.Zero;

        JVector r = sweepB;
        JVector v = -center.V;

        normal = JVector.Zero;

        int iter = maxIter;

        Real distSq = v.LengthSquared();

        while ((distSq > collideEpsilon * collideEpsilon) && (iter-- != 0))
        {
            MinkowskiDifference.Support(supportA, supportB, orientationB, posB, v, out Vertex vertex);
            var w = vertex.V;

            Real vDotW = -JVector.Dot(v, w);

            if (vDotW > (Real)0.0)
            {
                Real vDotR = JVector.Dot(v, r);

                if (vDotR >= -(Real)1e-12)
                {
                    lambda = Real.PositiveInfinity;
                    return false;
                }

                lambda -= vDotW / vDotR;

                posB = positionB + lambda * r;
                normal = v;
            }

            if (!simplexSolver.AddVertex(vertex, out v))
            {
                goto converged;
            }

            JVector.NegateInPlace(ref v);

            distSq = v.LengthSquared();
        }

    converged:

        simplexSolver.GetClosest(out pointA, out pointB);
        normal = JVector.NormalizeSafe(in normal, NumericEpsilon);

        return true;
    }
}
