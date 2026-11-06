/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Finds the outermost feature of a shape in a given direction by sampling its support mapping.
/// </summary>
/// <remarks>
/// <para>
/// This is the default implementation of <see cref="ISupportFeature.SupportFeature"/>. It probes the
/// support mapping in six directions tilted around the given one in a hexagonal pattern. A flat
/// feature returns its vertices for these probes. A curved surface would return points beside the
/// actual contact, by the radius times the tilt; those probes are recognized by how they move when
/// the tilt is doubled and are extrapolated to zero tilt, which puts them on the resting point or
/// line of the curved side.
/// </para>
/// <para>
/// Shapes that know their features analytically should override the query instead: the sampling
/// costs twelve to thirty support calls.
/// </para>
/// </remarks>
public static class SupportFeatureSampler
{
    private const int SampleCount = 6;

    // Sampled points closer than this (squared) count as the same point.
    internal const Real DuplicatePointDistanceSq = (Real)0.001;

    private const Real Sqrt3Over2 = (Real)0.86602540378;

    // Support points of a flat feature do not move when the tilt is doubled, except for roundoff,
    // which grows with the distance of the point from the shape's origin.
    private const Real ProbeNoise = (Real)1e-6;
    // On a curved surface doubling the tilt again moves the support point twice as far.
    // Relative deviation from that accepted before the samples are treated as a vertex jump.
    private const Real CurvedTolerance = (Real)0.5;
    // Components of a probe direction below this are treated as exactly zero. Single-precision
    // roundoff level: snapping a real component moves the probe of a sphere along its surface by
    // R times this.
    private const Real DirectionSnap = (Real)1e-6;
    // Sampled points within this fraction of their extent of a line are a segment, not a polygon.
    private const Real ThinRatio = (Real)1e-4;

    // Unit hexagon around the direction. These offsets generate the six probe directions.
    private static readonly Real[] hexagonVertices = [(Real)1.0, (Real)0.0, (Real)0.5, Sqrt3Over2, -(Real)0.5, Sqrt3Over2,
        -(Real)1.0, (Real)0.0, -(Real)0.5, -Sqrt3Over2, (Real)0.5, -Sqrt3Over2];

    /// <summary>
    /// Samples the feature of a shape that supports it in the given direction.
    /// </summary>
    /// <typeparam name="T">The type of the shape.</typeparam>
    /// <param name="shape">The shape to sample.</param>
    /// <param name="direction">The direction in the shape's local space. Does not need to be normalized.</param>
    /// <param name="tolerance">The sine of the tilt angle of the probes.</param>
    /// <param name="vertices">
    /// Receives the vertices in local space: one for a point, two for a segment, more for a polygon in
    /// winding order. At least <see cref="ISupportFeature.MaxVertices"/> entries long.
    /// </param>
    /// <returns>The number of vertices written, at least one.</returns>
    public static int Sample<T>(T shape, in JVector direction, Real tolerance, Span<JVector> vertices)
        where T : ISupportMappable
    {
        // The support mapping selects features by the signs of the direction's components (a lying
        // capsule picks its end by the sign of the axial component, which is at roundoff level when
        // it lies flat). Snapping them keeps all probes of one tilt on one feature instead of letting
        // roundoff send them to either end; the tilt's components then decide the sign the same way
        // for every multiple of the tilt.
        JVector normal = Snap(JVector.Normalize(direction));
        JVector tangent1 = MathHelper.CreateOrthonormal(normal);
        JVector tangent2 = normal % tangent1;

        int count = 0;
        bool curved = false;

        for (int e = 0; e < SampleCount; e++)
        {
            JVector tilt = hexagonVertices[2 * e + 0] * tolerance * tangent1 +
                           hexagonVertices[2 * e + 1] * tolerance * tangent2;

            Push(vertices, ref count, SampleFeature(shape, normal, tilt, ref curved));
        }

        // All probes collapsing onto one point means the shape touches at a single point: its
        // support point along the direction, which the support mapping gives exactly. For a curved
        // side that avoids the extrapolation's residual (about R * tilt^4 / 6); for a vertex it is
        // the vertex itself, also on shapes smaller than the duplicate distance.
        if (count == 1)
        {
            vertices[0] = Probe(shape, normal);
            return 1;
        }

        // Collapsed probes of a curved side can be three or more points on one line, off it only by
        // the extrapolation's residual. They are a segment, not the area of a sliver polygon.
        if (curved && count > 2) ReduceToThinSegment(vertices, ref count);

        return count;
    }

    // Add a point unless it is effectively identical to the first or the last one kept.
    internal static void Push(Span<JVector> points, ref int count, in JVector v)
    {
        if (count > 0)
        {
            if ((points[0] - v).LengthSquared() < DuplicatePointDistanceSq) return;
        }

        if (count > 1)
        {
            if ((points[count - 1] - v).LengthSquared() < DuplicatePointDistanceSq) return;
        }

        points[count++] = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JVector Probe<T>(T shape, in JVector direction) where T : ISupportMappable
    {
        shape.SupportMap(direction, out JVector point);
        return point;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JVector Snap(in JVector v)
    {
        return new JVector(MathR.Abs(v.X) < DirectionSnap ? (Real)0.0 : v.X,
            MathR.Abs(v.Y) < DirectionSnap ? (Real)0.0 : v.Y,
            MathR.Abs(v.Z) < DirectionSnap ? (Real)0.0 : v.Z);
    }

    // Probe the feature of a shape at one tilt of the direction. A flat feature returns the same
    // vertex for the tilt and for twice the tilt. A curved surface moves by R * tilt and then by
    // 2 * R * tilt again, so a single probe would report a point R * tilt beside the actual contact;
    // a lying capsule or cylinder then gets contacts beside its resting line and does not roll
    // steadily. When the probes respond like a curved surface, they are extrapolated to zero tilt,
    // which places the sample on the resting point, line or patch of the curved side. Any other
    // response, such as a jump to the next vertex of a fine mesh, keeps the first probe.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JVector SampleFeature<T>(T shape, in JVector normal, in JVector tilt, ref bool curved)
        where T : ISupportMappable
    {
        JVector p1 = Probe(shape, normal + tilt);
        JVector p2 = Probe(shape, normal + (Real)2.0 * tilt);

        JVector move = p2 - p1;
        Real noiseSq = ProbeNoise * ProbeNoise * ((Real)1.0 + p1.LengthSquared());
        if (move.LengthSquared() <= noiseSq) return p1;

        JVector p3 = Probe(shape, normal + (Real)4.0 * tilt);
        if (IsCurvedMove(move, p3 - p2))
        {
            // A quadratic through the three probes would still be off along the surface by about
            // 1.3 * R * tilt^3 (0.3 µm for R = 0.25), the same way every step; on a rolling body that
            // is a lever arm for the normal impulse. A fourth probe makes the fit cubic.
            curved = true;
            JVector pHalf = Probe(shape, normal + (Real)0.5 * tilt);

            // The half-tilt probe can still sit across a feature boundary that the other three are
            // beyond, such as the other end of a lying capsule whose normal is slightly off the tie.
            // The cubic would then extrapolate metres past the shape; the quadratic through the three
            // consistent probes stays on their feature, which touches as well.
            if (!IsCurvedMove(p1 - pHalf, move)) return ExtrapolateToNormal(p1, p2, p3);

            return ExtrapolateToNormal(pHalf, p1, p2, p3);
        }

        // The probes may still straddle a feature boundary when the normal is slightly off the tie.
        // Probing closer to the normal stays on one side of it; a vertex does not move there either.
        JVector pq = Probe(shape, normal + (Real)0.25 * tilt);
        JVector ph = Probe(shape, normal + (Real)0.5 * tilt);

        move = ph - pq;
        if (move.LengthSquared() <= noiseSq) return p1;
        if (!IsCurvedMove(move, p1 - ph)) return p1;

        curved = true;
        return ExtrapolateToNormal(pq, ph, p1);
    }

    // Cubic extrapolation to zero tilt through probes at one half, one, two and four times a tilt.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JVector ExtrapolateToNormal(in JVector ph, in JVector p1, in JVector p2, in JVector p4)
    {
        return ((Real)64.0 * ph - (Real)56.0 * p1 + (Real)14.0 * p2 - p4) * (Real)(1.0 / 21.0);
    }

    // Quadratic extrapolation to zero tilt through probes at one, two and four times a tilt.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JVector ExtrapolateToNormal(in JVector p1, in JVector p2, in JVector p4)
    {
        return (Real)(8.0 / 3.0) * p1 - (Real)2.0 * p2 + (Real)(1.0 / 3.0) * p4;
    }

    // On a curved surface doubling the tilt again doubles the move of the support point.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsCurvedMove(in JVector move, in JVector nextMove)
    {
        JVector mismatch = nextMove - (Real)2.0 * move;
        return mismatch.LengthSquared() <= CurvedTolerance * CurvedTolerance * move.LengthSquared();
    }

    // Keep the two points farthest apart if every point lies within ThinRatio times their distance
    // of the line through them.
    private static void ReduceToThinSegment(Span<JVector> points, ref int count)
    {
        int first = 0;
        int second = 1;
        Real longest = (points[1] - points[0]).LengthSquared();

        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                Real lengthSq = (points[j] - points[i]).LengthSquared();
                if (lengthSq <= longest) continue;

                longest = lengthSq;
                first = i;
                second = j;
            }
        }

        JVector start = points[first];
        JVector end = points[second];
        JVector axis = end - start;
        Real widthSq = ThinRatio * ThinRatio * longest * longest;

        for (int i = 0; i < count; i++)
        {
            // Squared distance from the segment's line, scaled by the squared length.
            if (((points[i] - start) % axis).LengthSquared() > widthSq) return;
        }

        points[0] = start;
        points[1] = end;
        count = 2;
    }
}
