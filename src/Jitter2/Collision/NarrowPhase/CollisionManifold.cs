/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Represents a contact manifold between two convex shapes.
/// </summary>
/// <remarks>
/// The manifold is constructed from support points sampled around the collision normal,
/// then reduced to a small contact set suitable for the solver.
/// </remarks>
public unsafe struct CollisionManifold
{
    // We sample each shape with six perturbed support directions, but the sampled polygons can
    // intersect in up to twelve vertices before the solver-facing reduction step.
    private const int MaxSupportPoints = 6;
    private const int MaxManifoldPoints = 12;
    private const int SolverContactLimit = 4;
    private const Real DuplicatePointDistanceSq = (Real)0.001;

    // Layout: MaxManifoldPoints on shape A followed by the matching points on shape B.
    private fixed Real manifoldData[MaxManifoldPoints * 2 * 3];

    // Number of sampled support points collected for each shape.
    private int leftCount;
    private int rightCount;
    // Number of actual contact pairs written into manifoldData.
    private int manifoldCount;

    private const Real Sqrt3Over2 = (Real)0.86602540378;
    // Small perturbation used to sample support points around the contact normal.
    private const Real Perturbation = (Real)0.01;

    // Unit hexagon around the normal. These offsets generate the six support directions.
    private static readonly Real[] hexagonVertices = [(Real)1.0, (Real)0.0, (Real)0.5, Sqrt3Over2, -(Real)0.5, Sqrt3Over2,
        -(Real)1.0, (Real)0.0, -(Real)0.5, -Sqrt3Over2, (Real)0.5, -Sqrt3Over2];

    /// <summary>
    /// Gets the contact points on shape A.
    /// </summary>
    public Span<JVector> ManifoldA => MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[0]), manifoldCount);

    /// <summary>
    /// Gets the contact points on shape B.
    /// </summary>
    public Span<JVector> ManifoldB => MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[MaxManifoldPoints * 3]), manifoldCount);

    /// <summary>
    /// Gets the number of contact points in the manifold.
    /// </summary>
    public readonly int Count => manifoldCount;

    // Add a support sample for shape A unless it is effectively identical to one we already kept.
    private void PushLeft(Span<JVector> left, in JVector v)
    {
        if (leftCount > 0)
        {
            if ((left[0] - v).LengthSquared() < DuplicatePointDistanceSq) return;
        }

        if (leftCount > 1)
        {
            if ((left[leftCount - 1] - v).LengthSquared() < DuplicatePointDistanceSq) return;
        }

        left[leftCount++] = v;
    }

    // Add a support sample for shape B unless it is effectively identical to one we already kept.
    private void PushRight(Span<JVector> right, in JVector v)
    {
        if (rightCount > 0)
        {
            if ((right[0] - v).LengthSquared() < DuplicatePointDistanceSq) return;
        }

        if (rightCount > 1)
        {
            if ((right[rightCount - 1] - v).LengthSquared() < DuplicatePointDistanceSq) return;
        }

        right[rightCount++] = v;
    }

    // Determine the winding sign of a sampled polygon relative to the contact normal.
    // If the area is near zero, the sample degenerated to a segment or point.
    private static bool TryGetPolygonSign(ReadOnlySpan<JVector> polygon, int count, in JVector normal, out Real sign)
    {
        const Real epsilon = (Real)1e-6;

        Real area = (Real)0.0;
        JVector previous = polygon[count - 1];

        for (int i = 0; i < count; i++)
        {
            JVector current = polygon[i];
            area += JVector.Dot(previous % current, normal);
            previous = current;
        }

        if (MathR.Abs(area) <= epsilon)
        {
            sign = (Real)0.0;
            return false;
        }

        sign = area < (Real)0.0 ? -(Real)1.0 : (Real)1.0;
        return true;
    }

    // Standard convex point-in-polygon test, evaluated directly in 3D via the contact normal.
    private static bool ContainsPoint(ReadOnlySpan<JVector> polygon, int count, in JVector point,
        in JVector normal, Real sign)
    {
        const Real epsilon = (Real)1e-3;

        JVector previous = polygon[count - 1];

        for (int i = 0; i < count; i++)
        {
            JVector current = polygon[i];
            Real side = sign * JVector.Dot((current - previous) % (point - previous), normal);

            if (side < -epsilon) return false;

            previous = current;
        }

        return true;
    }

    // Clip a segment against the half-spaces of a convex polygon.
    // This is the polygon/segment fallback when one sampled feature collapses to a line.
    private static int ClipSegmentAgainstPolygon(in JVector start, in JVector end,
        ReadOnlySpan<JVector> polygon, int polygonCount, in JVector normal, Real sign, Span<JVector> clipped)
    {
        const Real epsilon = (Real)1e-3;

        Real enter = (Real)0.0;
        Real exit = (Real)1.0;
        JVector delta = end - start;

        for (int i = 0; i < polygonCount; i++)
        {
            JVector edgeStart = polygon[i];
            JVector edgeEnd = polygon[(i + 1) % polygonCount];

            Real startSide = sign * JVector.Dot((edgeEnd - edgeStart) % (start - edgeStart), normal);
            Real endSide = sign * JVector.Dot((edgeEnd - edgeStart) % (end - edgeStart), normal);

            bool startInside = startSide >= -epsilon;
            bool endInside = endSide >= -epsilon;

            if (!startInside && !endInside) return 0;
            if (startInside && endInside) continue;

            Real denominator = startSide - endSide;
            if (MathR.Abs(denominator) <= epsilon) return 0;

            Real t = Math.Clamp(startSide / denominator, (Real)0.0, (Real)1.0);

            if (!startInside)
            {
                enter = MathR.Max(enter, t);
            }
            else
            {
                exit = MathR.Min(exit, t);
            }

            if (exit < enter) return 0;
        }

        clipped[0] = start + enter * delta;

        if ((start + exit * delta - clipped[0]).LengthSquared() < epsilon)
        {
            return 1;
        }

        clipped[1] = start + exit * delta;
        return 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // Signed scalar 2D cross product after projecting along the contact normal.
    private static Real CrossAlongNormal(in JVector left, in JVector right, in JVector normal)
    {
        return JVector.Dot(left % right, normal);
    }

    // Intersect two segments in the contact plane.
    // Handles both the normal crossing case and parallel collinear overlap.
    private static int IntersectSegments(in JVector startA, in JVector endA,
        in JVector startB, in JVector endB, in JVector normal,
        Span<JVector> clippedA, Span<JVector> clippedB)
    {
        const Real epsilon = (Real)1e-3;

        JVector deltaA = endA - startA;
        JVector deltaB = endB - startB;
        JVector offset = startB - startA;

        Real cross = CrossAlongNormal(deltaA, deltaB, normal);

        if (MathR.Abs(cross) <= epsilon)
        {
            if (MathR.Abs(CrossAlongNormal(offset, deltaA, normal)) > epsilon) return 0;

            Real lengthASquared = deltaA.LengthSquared();
            Real lengthBSquared = deltaB.LengthSquared();

            if (lengthASquared <= epsilon || lengthBSquared <= epsilon) return 0;

            Real parameterB0 = JVector.Dot(startB - startA, deltaA) / lengthASquared;
            Real parameterB1 = JVector.Dot(endB - startA, deltaA) / lengthASquared;

            Real enter = MathR.Max((Real)0.0, MathR.Min(parameterB0, parameterB1));
            Real exit = MathR.Min((Real)1.0, MathR.Max(parameterB0, parameterB1));

            if (exit < enter - epsilon) return 0;

            clippedA[0] = startA + enter * deltaA;

            Real parameterA0 = Math.Clamp(JVector.Dot(clippedA[0] - startB, deltaB) / lengthBSquared, (Real)0.0, (Real)1.0);
            clippedB[0] = startB + parameterA0 * deltaB;

            clippedA[1] = startA + exit * deltaA;

            if ((clippedA[1] - clippedA[0]).LengthSquared() <= epsilon) return 1;

            Real parameterA1 = Math.Clamp(JVector.Dot(clippedA[1] - startB, deltaB) / lengthBSquared, (Real)0.0, (Real)1.0);
            clippedB[1] = startB + parameterA1 * deltaB;

            return 2;
        }

        Real t = CrossAlongNormal(offset, deltaB, normal) / cross;
        Real u = CrossAlongNormal(offset, deltaA, normal) / cross;

        if (t < -epsilon || t > (Real)1.0 + epsilon ||
            u < -epsilon || u > (Real)1.0 + epsilon)
        {
            return 0;
        }

        t = Math.Clamp(t, (Real)0.0, (Real)1.0);
        u = Math.Clamp(u, (Real)0.0, (Real)1.0);

        clippedA[0] = startA + t * deltaA;
        clippedB[0] = startB + u * deltaB;

        return 1;
    }

    // Avoid inserting the same contact multiple times from overlapping collection paths.
    private static bool IsDuplicatePoint(ReadOnlySpan<JVector> manifold, int count, in JVector point)
    {
        for (int i = 0; i < count; i++)
        {
            if ((manifold[i] - point).LengthSquared() < DuplicatePointDistanceSq) return true;
        }

        return false;
    }

    // Register a contact whose geometric anchor came from shape A.
    // The matching point on B is reconstructed by sliding along the collision normal.
    private static void AddPointOnA(Span<JVector> manifoldA, Span<JVector> manifoldB, ref int manifoldCount,
        in JVector point, in JVector pB, in JVector normal)
    {
        if (manifoldCount == MaxManifoldPoints) return;

        Real diff = JVector.Dot(point - pB, normal);
        if (diff < (Real)0.0) return;
        if (IsDuplicatePoint(manifoldA, manifoldCount, point)) return;

        manifoldA[manifoldCount] = point;
        manifoldB[manifoldCount++] = point - diff * normal;
    }

    // Register a contact whose geometric anchor came from shape B.
    private static void AddPointOnB(Span<JVector> manifoldA, Span<JVector> manifoldB, ref int manifoldCount,
        in JVector point, in JVector pA, in JVector normal)
    {
        if (manifoldCount == MaxManifoldPoints) return;

        Real diff = JVector.Dot(point - pA, normal);
        if (diff > (Real)0.0) return;
        if (IsDuplicatePoint(manifoldB, manifoldCount, point)) return;

        manifoldB[manifoldCount] = point;
        manifoldA[manifoldCount++] = point - diff * normal;
    }

    // Register a contact where both sides came from an explicit intersection computation.
    private static void AddPointPair(Span<JVector> manifoldA, Span<JVector> manifoldB, ref int manifoldCount,
        in JVector pointA, in JVector pointB, in JVector normal)
    {
        if (manifoldCount == MaxManifoldPoints) return;
        if (JVector.Dot(pointA - pointB, normal) < (Real)0.0) return;
        if (IsDuplicatePoint(manifoldA, manifoldCount, pointA)) return;
        if (IsDuplicatePoint(manifoldB, manifoldCount, pointB)) return;

        manifoldA[manifoldCount] = pointA;
        manifoldB[manifoldCount++] = pointB;
    }

    // The solver only wants four contacts. We sort the points around the centroid and
    // keep four evenly distributed samples so the retained set covers the contact patch.
    private static void ReduceManifold(Span<JVector> manifoldA, Span<JVector> manifoldB, ref int manifoldCount, in JVector normal)
    {
        if (manifoldCount <= SolverContactLimit) return;

        JVector centroid = JVector.Zero;

        for (int i = 0; i < manifoldCount; i++)
        {
            centroid += manifoldA[i];
        }

        centroid *= (Real)(1.0 / manifoldCount);

        JVector tangent1 = MathHelper.CreateOrthonormal(normal);
        JVector tangent2 = normal % tangent1;

        Span<double> angles = stackalloc double[MaxManifoldPoints];
        Span<int> order = stackalloc int[MaxManifoldPoints];

        for (int i = 0; i < manifoldCount; i++)
        {
            JVector delta = manifoldA[i] - centroid;
            angles[i] = StableMath.Atan2(JVector.Dot(delta, tangent2), JVector.Dot(delta, tangent1));
            order[i] = i;
        }

        for (int i = 1; i < manifoldCount; i++)
        {
            int current = order[i];
            double currentAngle = angles[current];
            int j = i - 1;

            while (j >= 0 && angles[order[j]] > currentAngle)
            {
                order[j + 1] = order[j];
                j--;
            }

            order[j + 1] = current;
        }

        Span<JVector> reducedA = stackalloc JVector[SolverContactLimit];
        Span<JVector> reducedB = stackalloc JVector[SolverContactLimit];

        for (int i = 0; i < SolverContactLimit; i++)
        {
            int selected = order[((2 * i + 1) * manifoldCount) / (2 * SolverContactLimit)];
            reducedA[i] = manifoldA[selected];
            reducedB[i] = manifoldB[selected];
        }

        reducedA.CopyTo(manifoldA);
        reducedB.CopyTo(manifoldB);
        manifoldCount = SolverContactLimit;
    }

    /// <summary>
    /// Builds the contact manifold between two shapes given their transforms and initial contact.
    /// </summary>
    /// <typeparam name="Ta">The type of support shape A.</typeparam>
    /// <typeparam name="Tb">The type of support shape B.</typeparam>
    /// <param name="shapeA">The first shape.</param>
    /// <param name="shapeB">The second shape.</param>
    /// <param name="quaternionA">Orientation of shape A.</param>
    /// <param name="quaternionB">Orientation of shape B.</param>
    /// <param name="positionA">Position of shape A.</param>
    /// <param name="positionB">Position of shape B.</param>
    /// <param name="pA">Initial contact point on shape A.</param>
    /// <param name="pB">Initial contact point on shape B.</param>
    /// <param name="normal">The collision normal (from B to A).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void BuildManifold<Ta, Tb>(Ta shapeA, Tb shapeB, in JQuaternion quaternionA, in JQuaternion quaternionB,
        in JVector positionA, in JVector positionB, in JVector pA, in JVector pB, in JVector normal)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // Reset
        leftCount = 0;
        rightCount = 0;
        manifoldCount = 0;

        // Build a local tangent frame around the collision normal.
        JVector crossVector1 = MathHelper.CreateOrthonormal(normal);
        JVector crossVector2 = normal % crossVector1;

        // Operate in a frame translated by -positionA so intermediates stay near the origin.
        // Absolute epsilons in the clipping helpers are scaled to shape size, not world offset.
        JVector relPosB = positionB - positionA;
        JVector pAloc = pA - positionA;
        JVector pBloc = pB - positionA;

        Span<JVector> left = stackalloc JVector[MaxSupportPoints];
        Span<JVector> right = stackalloc JVector[MaxSupportPoints];

        // Sample support points for both shapes around the normal in a hexagonal pattern.
        // This approximates the active contact feature without a full clipping pipeline.
        for (int e = 0; e < MaxSupportPoints; e++)
        {
            JVector ptNormal = normal + hexagonVertices[2 * e + 0] * Perturbation * crossVector1 +
                               hexagonVertices[2 * e + 1] * Perturbation * crossVector2;

            JVector.ConjugatedTransform(ptNormal, quaternionA, out JVector tmp);
            shapeA.SupportMap(tmp, out JVector np1);
            JVector.Transform(np1, quaternionA, out np1);
            PushLeft(left, np1);

            JVector.NegateInPlace(ref ptNormal);

            JVector.ConjugatedTransform(ptNormal, quaternionB, out tmp);
            shapeB.SupportMap(tmp, out JVector np2);
            JVector.Transform(np2, quaternionB, out np2);
            JVector.Add(np2, relPosB, out np2);
            PushRight(right, np2);
        }

        Span<JVector> mA = MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[0]), MaxManifoldPoints);
        Span<JVector> mB = MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[MaxManifoldPoints * 3]), MaxManifoldPoints);

        // If a sampled loop has non-zero signed area, treat it as a polygon; otherwise it is
        // effectively a line or point and handled by the dedicated fallback cases below.
        Real leftSign = (Real)0.0;
        Real rightSign = (Real)0.0;
        bool leftPolygon = leftCount > 2 && TryGetPolygonSign(left, leftCount, normal, out leftSign);
        bool rightPolygon = rightCount > 2 && TryGetPolygonSign(right, rightCount, normal, out rightSign);

        // Polygon/point and polygon/segment cases with shape A contributing the polygon.
        if (leftPolygon)
        {
            for (int e = 0; e < rightCount; e++)
            {
                JVector p = right[e];
                if (!ContainsPoint(left, leftCount, p, normal, leftSign)) continue;

                AddPointOnB(mA, mB, ref manifoldCount, p, pAloc, normal);
                if (manifoldCount == MaxManifoldPoints) goto Finalize;
            }

            if (rightCount == 2)
            {
                Span<JVector> clipped = stackalloc JVector[2];
                int clippedCount = ClipSegmentAgainstPolygon(right[0], right[1], left, leftCount, normal, leftSign, clipped);

                for (int i = 0; i < clippedCount; i++)
                {
                    AddPointOnB(mA, mB, ref manifoldCount, clipped[i], pAloc, normal);
                    if (manifoldCount == MaxManifoldPoints) goto Finalize;
                }
            }
        }

        // Same logic with the roles reversed so we also pick anchors contributed by shape A.
        if (rightPolygon)
        {
            for (int e = 0; e < leftCount; e++)
            {
                JVector p = left[e];
                if (!ContainsPoint(right, rightCount, p, normal, rightSign)) continue;

                AddPointOnA(mA, mB, ref manifoldCount, p, pBloc, normal);
                if (manifoldCount == MaxManifoldPoints) goto Finalize;
            }

            if (leftCount == 2)
            {
                Span<JVector> clipped = stackalloc JVector[2];
                int clippedCount = ClipSegmentAgainstPolygon(left[0], left[1], right, rightCount, normal, rightSign, clipped);

                for (int i = 0; i < clippedCount; i++)
                {
                    AddPointOnA(mA, mB, ref manifoldCount, clipped[i], pBloc, normal);
                    if (manifoldCount == MaxManifoldPoints) goto Finalize;
                }
            }
        }

        // Pure segment/segment manifold. This is needed because neither side produced a polygon.
        if (leftCount == 2 && rightCount == 2)
        {
            Span<JVector> clippedA = stackalloc JVector[2];
            Span<JVector> clippedB = stackalloc JVector[2];
            int clippedCount = IntersectSegments(left[0], left[1], right[0], right[1], normal, clippedA, clippedB);

            for (int i = 0; i < clippedCount; i++)
            {
                AddPointPair(mA, mB, ref manifoldCount, clippedA[i], clippedB[i], normal);
                if (manifoldCount == MaxManifoldPoints) goto Finalize;
            }
        }

        // Polygon/polygon case: in addition to inside vertices, also collect explicit edge intersections.
        if (leftPolygon && rightPolygon)
        {
            Span<JVector> clippedA = stackalloc JVector[2];
            Span<JVector> clippedB = stackalloc JVector[2];

            for (int i = 0; i < leftCount; i++)
            {
                JVector startA = left[i];
                JVector endA = left[(i + 1) % leftCount];

                for (int j = 0; j < rightCount; j++)
                {
                    JVector startB = right[j];
                    JVector endB = right[(j + 1) % rightCount];

                    int clippedCount = IntersectSegments(startA, endA, startB, endB, normal, clippedA, clippedB);

                    for (int k = 0; k < clippedCount; k++)
                    {
                        AddPointPair(mA, mB, ref manifoldCount, clippedA[k], clippedB[k], normal);
                        if (manifoldCount == MaxManifoldPoints) goto Finalize;
                    }
                }
            }
        }

        // If none of the feature logic produced anything, fall back to the original deepest pair.
        if (manifoldCount == 0)
        {
            mA[manifoldCount] = pAloc;
            mB[manifoldCount++] = pBloc;
        }

    Finalize:
        // Final reduction from raw manifold candidates to the solver-facing contact set.
        ReduceManifold(mA, mB, ref manifoldCount, normal);

        // Lift the surviving contacts back into world space.
        for (int i = 0; i < manifoldCount; i++)
        {
            mA[i] += positionA;
            mB[i] += positionA;
        }
    } // BuildManifold

    /// <summary>
    /// Builds the contact manifold between two rigid body shapes using their current transforms.
    /// </summary>
    /// <typeparam name="Ta">The type of shape A.</typeparam>
    /// <typeparam name="Tb">The type of shape B.</typeparam>
    /// <param name="shapeA">The first rigid body shape.</param>
    /// <param name="shapeB">The second rigid body shape.</param>
    /// <param name="pA">Initial contact point on shape A.</param>
    /// <param name="pB">Initial contact point on shape B.</param>
    /// <param name="normal">The collision normal (from B to A).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void BuildManifold<Ta, Tb>(Ta shapeA, Tb shapeB,
        in JVector pA, in JVector pB, in JVector normal) where Ta : RigidBodyShape where Tb : RigidBodyShape
    {
        BuildManifold(shapeA, shapeB, shapeA.RigidBody.Orientation, shapeB.RigidBody.Orientation,
            shapeA.RigidBody.Position, shapeB.RigidBody.Position, pA, pB, normal);
    }
}
