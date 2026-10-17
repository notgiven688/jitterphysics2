/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2;

/// <summary>Renderer-independent wire geometry emitted through <see cref="IDebugDrawer"/>.</summary>
/// <remarks>Directions must be finite and nonzero. Reference directions are projected onto the drawing plane.</remarks>
public static class DebugDrawGeometry
{
    /// <summary>Draws a white line segment between two points in world space.</summary>
    public static void DrawSegment(this IDebugDrawer drawer, in JVector from, in JVector to) =>
        drawer.DrawSegment(from, to, DebugDrawColors.White);

    /// <summary>Draws a point marker as three crossing segments along the world X, Y, and Z axes.</summary>
    /// <param name="drawer">The debug drawer receiving the segments.</param>
    /// <param name="point">The center of the marker in world space.</param>
    /// <param name="radius">Half the length of each segment in world units. Must be finite and nonnegative.</param>
    /// <param name="color">The color packed as <c>0xRRGGBBAA</c>.</param>
    public static void DrawPoint(this IDebugDrawer drawer, JVector point, Real radius, uint color)
    {
        ArgumentCheck.NonNegative(radius, nameof(radius));
        JVector x = JVector.UnitX * radius, y = JVector.UnitY * radius, z = JVector.UnitZ * radius;
        drawer.DrawSegment(point - x, point + x, color);
        drawer.DrawSegment(point - y, point + y, color);
        drawer.DrawSegment(point - z, point + z, color);
    }

    private static void Basis(JVector axis, JVector reference, out JVector first, out JVector second)
    {
        ArgumentCheck.NonZero(axis, nameof(axis));
        ArgumentCheck.Finite(reference, nameof(reference));
        axis = JVector.Normalize(axis);
        first = reference - axis * JVector.Dot(axis, reference);
        first = first.LengthSquared() > (Real)1e-12 ? JVector.Normalize(first) : MathHelper.CreateOrthonormal(axis);
        second = axis % first;
    }

    private static void ValidateCurve(Real radius, int segments)
    {
        ArgumentCheck.NonNegative(radius, nameof(radius));
        if (segments < 3 || segments > 4096) throw new ArgumentOutOfRangeException(nameof(segments));
    }

    /// <summary>Draws a circular arc. Angles are measured from the reference direction around the axis.</summary>
    public static void DrawArc(this IDebugDrawer drawer, JVector center, JVector axis, JVector reference,
        JAngle from, JAngle to, Real radius, uint color, int segments = 48)
    {
        ValidateCurve(radius, segments);
        ArgumentCheck.Finite(from, nameof(from));
        ArgumentCheck.Finite(to, nameof(to));
        Basis(axis, reference, out JVector first, out JVector second);
        Real start = from.Radian, end = to.Radian;
        JVector previous = center + radius * (MathR.Cos(start) * first + MathR.Sin(start) * second);
        for (int i = 1; i <= segments; i++)
        {
            Real angle = start * (1 - (Real)i / segments) + end * ((Real)i / segments);
            JVector point = center + radius * (MathR.Cos(angle) * first + MathR.Sin(angle) * second);
            drawer.DrawSegment(previous, point, color);
            previous = point;
        }
    }

    /// <summary>Draws a circle in the plane perpendicular to the axis.</summary>
    public static void DrawCircle(this IDebugDrawer drawer, JVector center, JVector axis, JVector reference,
        Real radius, uint color, int segments = 48) =>
        drawer.DrawArc(center, axis, reference, (JAngle)0, (JAngle)(2 * MathR.PI), radius, color, segments);

    /// <summary>Draws three perpendicular great circles.</summary>
    public static void DrawSphere(this IDebugDrawer drawer, JVector center, Real radius, uint color, int segments = 48)
    {
        drawer.DrawCircle(center, JVector.UnitX, JVector.UnitY, radius, color, segments);
        drawer.DrawCircle(center, JVector.UnitY, JVector.UnitZ, radius, color, segments);
        drawer.DrawCircle(center, JVector.UnitZ, JVector.UnitX, radius, color, segments);
    }

    /// <summary>Draws an elliptical cone on a sphere, using angular radii along two perpendicular directions.</summary>
    public static void DrawCone(this IDebugDrawer drawer, JVector origin, JVector axis, JVector reference,
        JAngle alongReference, JAngle alongPerpendicular, Real radius, uint color, int segments = 48, int spokeOffset = 0)
    {
        ValidateCurve(radius, segments);
        ArgumentCheck.NonNegative(alongReference.Radian, nameof(alongReference));
        ArgumentCheck.NonNegative(alongPerpendicular.Radian, nameof(alongPerpendicular));
        Basis(axis, reference, out JVector first, out JVector second);
        axis = JVector.Normalize(axis);
        JVector previous = default;
        int spokeStep = Math.Max(1, segments / 8);
        for (int i = 0; i <= segments; i++)
        {
            Real phase = 2 * MathR.PI * i / segments;
            Real x = alongReference.Radian * MathR.Cos(phase), y = alongPerpendicular.Radian * MathR.Sin(phase);
            Real angle = MathR.Sqrt(x * x + y * y);
            Real sinc = angle == 0 ? 1 : MathR.Sin(angle) / angle;
            JVector point = origin + radius * (MathR.Cos(angle) * axis + sinc * (x * first + y * second));
            if (i > 0) drawer.DrawSegment(previous, point, color);
            if (i < segments && i % spokeStep == spokeOffset % spokeStep) drawer.DrawSegment(origin, point, color);
            previous = point;
        }
    }

    /// <summary>Draws a finite grid representing an infinite plane.</summary>
    public static void DrawPlane(this IDebugDrawer drawer, JVector center, JVector normal, JVector reference,
        Real halfExtent, uint color)
    {
        ArgumentCheck.NonNegative(halfExtent, nameof(halfExtent));
        Basis(normal, reference, out JVector first, out JVector second);
        for (int i = -3; i <= 3; i++)
        {
            Real offset = halfExtent * i / 3;
            drawer.DrawSegment(center + first * offset - second * halfExtent,
                center + first * offset + second * halfExtent, color);
            drawer.DrawSegment(center + second * offset - first * halfExtent,
                center + second * offset + first * halfExtent, color);
        }
    }

    /// <summary>Draws an arrow with a wire cone head proportional to its length.</summary>
    /// <param name="segments">Number of segments around the head's base, from 3 to 4096. Default is 12.</param>
    /// <param name="spokes">Number of segments joining the base to the tip, from 1 to 4096, clamped to the base vertex count. Default is 4.</param>
    public static void DrawArrow(this IDebugDrawer drawer, JVector origin, JVector vector, uint color,
        int segments = 12, int spokes = 4)
    {
        Real length = vector.Length();
        ValidateCurve(length, segments);
        if (spokes < 1 || spokes > 4096) throw new ArgumentOutOfRangeException(nameof(spokes));
        JVector tip = origin + vector;
        drawer.DrawSegment(origin, tip, color);
        if (length < (Real)1e-12) return;
        JVector direction = vector * (1 / length);
        JVector sideways = MathHelper.CreateOrthonormal(direction) * (length * (Real)0.08);
        JVector up = direction % sideways;
        JVector root = origin + vector * (Real)0.8;
        JVector first = root + sideways, previous = first;
        int spokeCount = Math.Min(spokes, segments), nextSpoke = 0;
        for (int i = 0; i < segments; i++)
        {
            Real angle = 2 * MathR.PI * i / segments;
            JVector point = i == 0 ? first : root + MathR.Cos(angle) * sideways + MathR.Sin(angle) * up;
            if (i > 0) drawer.DrawSegment(previous, point, color);
            if (i == nextSpoke * segments / spokeCount)
            {
                drawer.DrawSegment(tip, point, color);
                nextSpoke++;
            }
            previous = point;
        }
        drawer.DrawSegment(previous, first, color);
    }

    /// <summary>Draws the three axes of an orientation.</summary>
    /// <param name="segments">Number of segments around each arrowhead base, from 3 to 4096. Default is 12.</param>
    /// <param name="spokes">Number of segments joining each base to its tip, from 1 to 4096, clamped to the base vertex count. Default is 4.</param>
    public static void DrawFrame(this IDebugDrawer drawer, JVector center, JQuaternion orientation, Real size, uint color,
        int segments = 12, int spokes = 4)
    {
        drawer.DrawArrow(center, orientation.GetBasisX() * size, color, segments, spokes);
        drawer.DrawArrow(center, orientation.GetBasisY() * size, color, segments, spokes);
        drawer.DrawArrow(center, orientation.GetBasisZ() * size, color, segments, spokes);
    }

    /// <summary>Draws a cross perpendicular to an axis, for marking a linear stop.</summary>
    public static void DrawStop(this IDebugDrawer drawer, JVector center, JVector axis, Real size, uint color)
    {
        Basis(axis, JVector.Zero, out JVector first, out JVector second);
        drawer.DrawSegment(center - first * size, center + first * size, color);
        drawer.DrawSegment(center - second * size, center + second * size, color);
    }

    /// <summary>
    /// Draws a spring, transporting the supplied reference direction to keep the coil continuous between frames.
    /// Keep the reference in the caller's visualization state, independently for each spring.
    /// </summary>
    public static void DrawSpring(this IDebugDrawer drawer, JVector from, JVector to, ref JVector reference,
        Real radius, uint color, int segments = 96)
    {
        ValidateCurve(radius, segments);
        JVector displacement = to - from;
        if (displacement.LengthSquared() < (Real)1e-20) return;
        Basis(JVector.Normalize(displacement), reference, out JVector first, out JVector second);
        reference = first;
        JVector previous = from;
        for (int i = 1; i <= segments; i++)
        {
            Real t = (Real)i / segments;
            Real phase = t * 20 * MathR.PI;
            Real taper = radius * MathR.Sin(t * MathR.PI);
            JVector point = i == segments ? to : from + displacement * t +
                taper * (MathR.Cos(phase) * first + MathR.Sin(phase) * second);
            drawer.DrawSegment(previous, point, color);
            previous = point;
        }
    }

    internal static void AngularRange(IDebugDrawer drawer, JVector origin, JVector axis, JVector reference,
        Real minimum, Real maximum, Real current, in DebugDrawSettings settings)
    {
        Real radius = settings.Scale;
        JVector other = axis % reference;
        bool fixedAngle = minimum >= maximum;
        bool fullRange = minimum <= -MathR.PI && maximum >= MathR.PI;
        if (fixedAngle)
        {
            Real angle = minimum * (Real)0.5 + maximum * (Real)0.5;
            drawer.DrawArrow(origin, radius * (MathR.Cos(angle) * reference + MathR.Sin(angle) * other),
                settings.UpperLimitColor, settings.ArrowHeadSegments, settings.ArrowHeadSpokes);
        }
        else
        {
            Real from = Real.Clamp(minimum, -MathR.PI, MathR.PI), to = Real.Clamp(maximum, -MathR.PI, MathR.PI);
            drawer.DrawArc(origin, axis, reference, (JAngle)from, (JAngle)to, radius, settings.UpperLimitColor, settings.Segments);
            if (!fullRange)
            {
                drawer.DrawSegment(origin, origin + radius * (MathR.Cos(from) * reference + MathR.Sin(from) * other), settings.LowerLimitColor);
                drawer.DrawSegment(origin, origin + radius * (MathR.Cos(to) * reference + MathR.Sin(to) * other), settings.UpperLimitColor);
            }
        }
        drawer.DrawArrow(origin, axis * (radius * (Real)0.4), settings.ReferenceColor,
            settings.ArrowHeadSegments, settings.ArrowHeadSpokes);
        drawer.DrawArrow(origin, radius * (MathR.Cos(current) * reference + MathR.Sin(current) * other),
            settings.CurrentColor, settings.ArrowHeadSegments, settings.ArrowHeadSpokes);
    }
}
