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
/// Represents a contact manifold between two convex shapes, storing up to six contact points.
/// </summary>
/// <remarks>
/// The manifold is constructed by projecting support points along perturbed normals
/// in a hexagonal pattern around the collision normal, then clipping to find the contact region.
/// </remarks>
public unsafe struct CollisionManifold
{
    private readonly struct ClipPoint
    {
        public readonly Real X;
        public readonly Real Y;

        public ClipPoint(Real x, Real y)
        {
            X = x;
            Y = y;
        }

        public static ClipPoint operator +(ClipPoint left, ClipPoint right)
        {
            return new ClipPoint(left.X + right.X, left.Y + right.Y);
        }

        public static ClipPoint operator -(ClipPoint left, ClipPoint right)
        {
            return new ClipPoint(left.X - right.X, left.Y - right.Y);
        }

        public static ClipPoint operator *(Real scale, ClipPoint point)
        {
            return new ClipPoint(scale * point.X, scale * point.Y);
        }

        public readonly Real LengthSquared()
        {
            return X * X + Y * Y;
        }
    }

    private fixed Real manifoldData[12*3];

    private int leftCount;
    private int rightCount;
    private int manifoldCount;

    private const int MaxManifoldPoints = 6;
    private const int MaxClipPoints = 12;
    private const int SolverContactLimit = 4;
    private const Real Sqrt3Over2 = (Real)0.86602540378;
    private const Real Perturbation = (Real)0.01;

    private static readonly Real[] hexagonVertices = [(Real)1.0, (Real)0.0, (Real)0.5, Sqrt3Over2, -(Real)0.5, Sqrt3Over2,
        -(Real)1.0, (Real)0.0, -(Real)0.5, -Sqrt3Over2, (Real)0.5, -Sqrt3Over2];

    // The solver always keeps 4 contacts. For 5- and 6-point manifolds we can enumerate all
    // candidate quadrilaterals up front instead of generating combinations on the fly.
    private static readonly byte[] quadrilateralCombinations5 =
    [
        0, 1, 2, 3,
        0, 1, 2, 4,
        0, 1, 3, 4,
        0, 2, 3, 4,
        1, 2, 3, 4
    ];

    private static readonly byte[] quadrilateralCombinations6 =
    [
        0, 1, 2, 3,
        0, 1, 2, 4,
        0, 1, 2, 5,
        0, 1, 3, 4,
        0, 1, 3, 5,
        0, 1, 4, 5,
        0, 2, 3, 4,
        0, 2, 3, 5,
        0, 2, 4, 5,
        0, 3, 4, 5,
        1, 2, 3, 4,
        1, 2, 3, 5,
        1, 2, 4, 5,
        1, 3, 4, 5,
        2, 3, 4, 5
    ];

    /// <summary>
    /// Gets a span of contact points on shape A. Valid indices are <c>[0, Count)</c>.
    /// </summary>
    public Span<JVector> ManifoldA => MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[0]), MaxManifoldPoints);

    /// <summary>
    /// Gets a span of contact points on shape B. Valid indices are <c>[0, Count)</c>.
    /// </summary>
    public Span<JVector> ManifoldB => MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[18]), MaxManifoldPoints);

    /// <summary>
    /// Gets the number of contact points in the manifold.
    /// </summary>
    public readonly int Count => manifoldCount;

    private void PushLeft(Span<JVector> left, in JVector v)
    {
        const Real epsilon = (Real)0.001;

        if (leftCount > 0)
        {
            if ((left[0] - v).LengthSquared() < epsilon) return;
        }

        if (leftCount > 1)
        {
            if ((left[leftCount - 1] - v).LengthSquared() < epsilon) return;
        }

        left[leftCount++] = v;
    }

    private void PushRight(Span<JVector> right, in JVector v)
    {
        const Real epsilon = (Real)0.001;

        if (rightCount > 0)
        {
            if ((right[0] - v).LengthSquared() < epsilon) return;
        }

        if (rightCount > 1)
        {
            if ((right[rightCount - 1] - v).LengthSquared() < epsilon) return;
        }

        right[rightCount++] = v;
    }

    private static Real Cross2D(in ClipPoint left, in ClipPoint right)
    {
        return left.X * right.Y - left.Y * right.X;
    }

    private static void StoreLinearIntersection(in ClipPoint start, in ClipPoint end,
        Real distanceEpsilonSq, Span<ClipPoint> clipped, out int clippedCount)
    {
        clipped[0] = start;
        clippedCount = 1;

        if ((end - start).LengthSquared() <= distanceEpsilonSq) return;

        clipped[1] = end;
        clippedCount = 2;
    }

    private static ClipPoint ProjectToPlane(in JVector point, in JVector origin, in JVector tangent1, in JVector tangent2)
    {
        JVector delta = point - origin;
        return new ClipPoint(JVector.Dot(delta, tangent1), JVector.Dot(delta, tangent2));
    }

    private static JVector LiftFromPlane(in ClipPoint point, in JVector origin, in JVector tangent1, in JVector tangent2)
    {
        return origin + point.X * tangent1 + point.Y * tangent2;
    }

    private static Real SignedArea(ReadOnlySpan<ClipPoint> polygon, int count)
    {
        if (count < 3) return (Real)0.0;

        Real area = (Real)0.0;

        for (int i = 0; i < count; i++)
        {
            ClipPoint current = polygon[i];
            ClipPoint next = polygon[(i + 1) % count];
            area += Cross2D(current, next);
        }

        return area;
    }

    private static void ReversePolygon(Span<ClipPoint> polygon, int count)
    {
        for (int i = 0, j = count - 1; i < j; i++, j--)
        {
            (polygon[i], polygon[j]) = (polygon[j], polygon[i]);
        }
    }

    private static void NormalizeWinding(Span<ClipPoint> polygon, int count)
    {
        if (SignedArea(polygon, count) < (Real)0.0)
        {
            ReversePolygon(polygon, count);
        }
    }

    private static void CalculateClipTolerance(ReadOnlySpan<ClipPoint> left, int leftCount,
        ReadOnlySpan<ClipPoint> right, int rightCount,
        out Real sideEpsilon, out Real distanceEpsilonSq, out Real areaEpsilon)
    {
        Real scale = (Real)1.0;

        for (int i = 0; i < leftCount; i++)
        {
            scale = MathR.Max(scale, MathR.Max(MathR.Abs(left[i].X), MathR.Abs(left[i].Y)));
        }

        for (int i = 0; i < rightCount; i++)
        {
            scale = MathR.Max(scale, MathR.Max(MathR.Abs(right[i].X), MathR.Abs(right[i].Y)));
        }

        Real distanceEpsilon = (Real)1e-5 * scale + (Real)1e-7;
        distanceEpsilonSq = distanceEpsilon * distanceEpsilon;
        areaEpsilon = distanceEpsilon * scale;
        sideEpsilon = areaEpsilon;
    }

    private static void CompactPolygon(Span<ClipPoint> polygon, ref int count,
        Real distanceEpsilonSq, Real areaEpsilon)
    {
        if (count == 0) return;

        int write = 0;

        for (int i = 0; i < count; i++)
        {
            ClipPoint current = polygon[i];

            if (write > 0 && (polygon[write - 1] - current).LengthSquared() <= distanceEpsilonSq)
            {
                continue;
            }

            polygon[write++] = current;
        }

        if (write > 1 && (polygon[0] - polygon[write - 1]).LengthSquared() <= distanceEpsilonSq)
        {
            write -= 1;
        }

        count = write;
        if (count < 3) return;

        bool removed;

        do
        {
            removed = false;

            for (int i = 0; i < count; i++)
            {
                ClipPoint previous = polygon[(i + count - 1) % count];
                ClipPoint current = polygon[i];
                ClipPoint next = polygon[(i + 1) % count];

                ClipPoint edge0 = current - previous;
                ClipPoint edge1 = next - current;

                if (MathR.Abs(Cross2D(edge0, edge1)) > areaEpsilon) continue;

                for (int j = i; j < count - 1; j++)
                {
                    polygon[j] = polygon[j + 1];
                }

                count -= 1;
                removed = true;
                break;
            }
        }
        while (removed && count >= 3);
    }

    private static Real SideOfEdge(in ClipPoint edgeStart, in ClipPoint edgeEnd, in ClipPoint point)
    {
        return Cross2D(edgeEnd - edgeStart, point - edgeStart);
    }

    private static ClipPoint IntersectSegmentsAgainstEdge(in ClipPoint edgeStart, in ClipPoint edgeEnd,
        in ClipPoint start, in ClipPoint end, Real startSide, Real endSide, Real distanceEpsilonSq)
    {
        Real denominator = startSide - endSide;

        if (MathR.Abs(denominator) <= distanceEpsilonSq)
        {
            return MathR.Abs(startSide) <= MathR.Abs(endSide) ? start : end;
        }

        Real t = startSide / denominator;
        t = Math.Clamp(t, (Real)0.0, (Real)1.0);

        return start + t * (end - start);
    }

    private static int ClipConvexPolygon(Span<ClipPoint> subject, int subjectCount,
        ReadOnlySpan<ClipPoint> clip, int clipCount, Span<ClipPoint> buffer,
        Real sideEpsilon, Real distanceEpsilonSq, Real areaEpsilon)
    {
        Span<ClipPoint> input = subject;
        Span<ClipPoint> output = buffer;
        int inputCount = subjectCount;
        bool resultInSubject = true;

        for (int edge = 0; edge < clipCount; edge++)
        {
            if (inputCount == 0) return 0;

            ClipPoint edgeStart = clip[edge];
            ClipPoint edgeEnd = clip[(edge + 1) % clipCount];
            int outputCount = 0;

            ClipPoint start = input[inputCount - 1];
            Real startSide = SideOfEdge(edgeStart, edgeEnd, start);
            bool startInside = startSide >= -sideEpsilon;

            for (int i = 0; i < inputCount; i++)
            {
                ClipPoint end = input[i];
                Real endSide = SideOfEdge(edgeStart, edgeEnd, end);
                bool endInside = endSide >= -sideEpsilon;

                if (startInside != endInside)
                {
                    output[outputCount++] = IntersectSegmentsAgainstEdge(
                        edgeStart, edgeEnd, start, end, startSide, endSide, distanceEpsilonSq);
                }

                if (endInside)
                {
                    output[outputCount++] = end;
                }

                start = end;
                startSide = endSide;
                startInside = endInside;
            }

            CompactPolygon(output, ref outputCount, distanceEpsilonSq, areaEpsilon);

            Span<ClipPoint> temporary = input;
            input = output;
            output = temporary;
            inputCount = outputCount;
            resultInSubject = !resultInSubject;
        }

        if (!resultInSubject)
        {
            input[..inputCount].CopyTo(subject);
        }

        return inputCount;
    }

    // When the projected overlap degenerates from a 2D polygon to a 1D feature, the polygon clipper
    // above returns nothing. These helpers recover the line overlap and emit one or two endpoints.
    private static bool ClipSegmentAgainstPolygon(in ClipPoint segmentStart, in ClipPoint segmentEnd,
        ReadOnlySpan<ClipPoint> polygon, int polygonCount,
        Real sideEpsilon, Real distanceEpsilonSq, Span<ClipPoint> clipped, out int clippedCount)
    {
        Real enter = (Real)0.0;
        Real exit = (Real)1.0;
        ClipPoint delta = segmentEnd - segmentStart;

        for (int edge = 0; edge < polygonCount; edge++)
        {
            ClipPoint edgeStart = polygon[edge];
            ClipPoint edgeEnd = polygon[(edge + 1) % polygonCount];

            Real startSide = SideOfEdge(edgeStart, edgeEnd, segmentStart) + sideEpsilon;
            Real endSide = SideOfEdge(edgeStart, edgeEnd, segmentEnd) + sideEpsilon;

            bool startInside = startSide >= (Real)0.0;
            bool endInside = endSide >= (Real)0.0;

            if (!startInside && !endInside)
            {
                clippedCount = 0;
                return false;
            }

            if (startInside && endInside) continue;

            Real denominator = startSide - endSide;

            if (MathR.Abs(denominator) <= distanceEpsilonSq)
            {
                clippedCount = 0;
                return false;
            }

            Real t = startSide / denominator;
            t = Math.Clamp(t, (Real)0.0, (Real)1.0);

            if (!startInside)
            {
                enter = MathR.Max(enter, t);
            }
            else
            {
                exit = MathR.Min(exit, t);
            }

            if (exit < enter)
            {
                clippedCount = 0;
                return false;
            }
        }

        StoreLinearIntersection(segmentStart + enter * delta, segmentStart + exit * delta,
            distanceEpsilonSq, clipped, out clippedCount);

        return true;
    }

    private static bool IntersectSegments(in ClipPoint leftStart, in ClipPoint leftEnd,
        in ClipPoint rightStart, in ClipPoint rightEnd,
        Real sideEpsilon, Real distanceEpsilonSq, Real areaEpsilon,
        Span<ClipPoint> clipped, out int clippedCount)
    {
        ClipPoint leftDelta = leftEnd - leftStart;
        ClipPoint rightDelta = rightEnd - rightStart;
        ClipPoint offset = rightStart - leftStart;

        Real cross = Cross2D(leftDelta, rightDelta);
        const Real parameterEpsilon = (Real)1e-5;

        if (MathR.Abs(cross) <= areaEpsilon)
        {
            if (MathR.Abs(Cross2D(offset, leftDelta)) > areaEpsilon)
            {
                clippedCount = 0;
                return false;
            }

            bool useLeft = leftDelta.LengthSquared() >= rightDelta.LengthSquared();
            ClipPoint baseStart = useLeft ? leftStart : rightStart;
            ClipPoint baseDelta = useLeft ? leftDelta : rightDelta;

            bool useXAxis = MathR.Abs(baseDelta.X) >= MathR.Abs(baseDelta.Y);
            Real baseOrigin = useXAxis ? baseStart.X : baseStart.Y;
            Real baseExtent = useXAxis ? baseDelta.X : baseDelta.Y;

            if (MathR.Abs(baseExtent) <= sideEpsilon)
            {
                clippedCount = 0;
                return false;
            }

            Real leftMin = MathR.Min(useXAxis ? leftStart.X : leftStart.Y, useXAxis ? leftEnd.X : leftEnd.Y);
            Real leftMax = MathR.Max(useXAxis ? leftStart.X : leftStart.Y, useXAxis ? leftEnd.X : leftEnd.Y);
            Real rightMin = MathR.Min(useXAxis ? rightStart.X : rightStart.Y, useXAxis ? rightEnd.X : rightEnd.Y);
            Real rightMax = MathR.Max(useXAxis ? rightStart.X : rightStart.Y, useXAxis ? rightEnd.X : rightEnd.Y);

            Real overlapMin = MathR.Max(leftMin, rightMin);
            Real overlapMax = MathR.Min(leftMax, rightMax);

            if (overlapMax + sideEpsilon < overlapMin)
            {
                clippedCount = 0;
                return false;
            }

            Real t0 = (overlapMin - baseOrigin) / baseExtent;
            Real t1 = (overlapMax - baseOrigin) / baseExtent;

            StoreLinearIntersection(baseStart + t0 * baseDelta, baseStart + t1 * baseDelta,
                distanceEpsilonSq, clipped, out clippedCount);

            return true;
        }

        Real t = Cross2D(offset, rightDelta) / cross;
        Real u = Cross2D(offset, leftDelta) / cross;

        if (t < -parameterEpsilon || t > (Real)1.0 + parameterEpsilon ||
            u < -parameterEpsilon || u > (Real)1.0 + parameterEpsilon)
        {
            clippedCount = 0;
            return false;
        }

        t = Math.Clamp(t, (Real)0.0, (Real)1.0);
        clipped[0] = leftStart + t * leftDelta;
        clippedCount = 1;

        return true;
    }

    private static bool TryClipLinearIntersection(ReadOnlySpan<ClipPoint> left, int leftCount,
        ReadOnlySpan<ClipPoint> right, int rightCount,
        Real sideEpsilon, Real distanceEpsilonSq, Real areaEpsilon,
        Span<ClipPoint> clipped, out int clippedCount)
    {
        if (leftCount < 2 || rightCount < 2)
        {
            clippedCount = 0;
            return false;
        }

        if (leftCount == 2 && rightCount == 2)
        {
            return IntersectSegments(left[0], left[1], right[0], right[1],
                sideEpsilon, distanceEpsilonSq, areaEpsilon, clipped, out clippedCount);
        }

        if (leftCount == 2)
        {
            return ClipSegmentAgainstPolygon(left[0], left[1], right, rightCount,
                sideEpsilon, distanceEpsilonSq, clipped, out clippedCount);
        }

        if (rightCount == 2)
        {
            return ClipSegmentAgainstPolygon(right[0], right[1], left, leftCount,
                sideEpsilon, distanceEpsilonSq, clipped, out clippedCount);
        }

        clippedCount = 0;
        return false;
    }

    private static Real CalculateQuadrilateralArea(in JVector p0, in JVector p1, in JVector p2, in JVector p3, in JVector normal)
    {
        JVector area = p0 % p1;
        area += p1 % p2;
        area += p2 % p3;
        area += p3 % p0;

        return MathR.Abs(JVector.Dot(area, normal));
    }

    private static void ReducePolygon(Span<ClipPoint> polygon, ref int count)
    {
        if (count <= MaxManifoldPoints) return;

        Span<ClipPoint> reduced = stackalloc ClipPoint[MaxManifoldPoints];

        for (int i = 0; i < MaxManifoldPoints; i++)
        {
            int index = ((2 * i + 1) * count) / (2 * MaxManifoldPoints);
            reduced[i] = polygon[index];
        }

        reduced.CopyTo(polygon);
        count = MaxManifoldPoints;
    }

    private int SelectSolverContacts(in JVector normal, Span<int> selected)
    {
        if (selected.Length < SolverContactLimit)
        {
            throw new ArgumentException($"Selected span must hold at least {SolverContactLimit} indices.", nameof(selected));
        }

        if (manifoldCount <= SolverContactLimit)
        {
            for (int i = 0; i < manifoldCount; i++)
            {
                selected[i] = i;
            }

            return manifoldCount;
        }

        ReadOnlySpan<JVector> manifold = ManifoldA;
        ReadOnlySpan<byte> combinations = manifoldCount switch
        {
            5 => quadrilateralCombinations5,
            6 => quadrilateralCombinations6,
            _ => throw new InvalidOperationException($"Unexpected manifold count {manifoldCount}.")
        };

        Real bestArea = Real.MinValue;
        int best0 = 0, best1 = 1, best2 = 2, best3 = 3;

        for (int i = 0; i < combinations.Length; i += SolverContactLimit)
        {
            int i0 = combinations[i + 0];
            int i1 = combinations[i + 1];
            int i2 = combinations[i + 2];
            int i3 = combinations[i + 3];

            Real area = CalculateQuadrilateralArea(manifold[i0], manifold[i1], manifold[i2], manifold[i3], normal);

            if (area <= bestArea) continue;

            bestArea = area;
            best0 = i0;
            best1 = i1;
            best2 = i2;
            best3 = i3;
        }

        selected[0] = best0;
        selected[1] = best1;
        selected[2] = best2;
        selected[3] = best3;

        return SolverContactLimit;
    }

    // Keep only the 4 contacts that span the largest area in the contact plane.
    internal void ReduceToSolverContacts(in JVector normal)
    {
        if (manifoldCount <= SolverContactLimit) return;

        Span<int> selected = stackalloc int[SolverContactLimit];
        int selectedCount = SelectSolverContacts(normal, selected);

        Span<JVector> manifoldA = ManifoldA;
        Span<JVector> manifoldB = ManifoldB;
        Span<JVector> reducedA = stackalloc JVector[SolverContactLimit];
        Span<JVector> reducedB = stackalloc JVector[SolverContactLimit];

        for (int i = 0; i < selectedCount; i++)
        {
            int index = selected[i];
            reducedA[i] = manifoldA[index];
            reducedB[i] = manifoldB[index];
        }

        reducedA[..selectedCount].CopyTo(manifoldA);
        reducedB[..selectedCount].CopyTo(manifoldB);
        manifoldCount = selectedCount;
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
    [SkipLocalsInit]
    public void BuildManifold<Ta,Tb>(Ta shapeA, Tb shapeB, in JQuaternion quaternionA, in JQuaternion quaternionB,
        in JVector positionA, in JVector positionB, in JVector pA, in JVector pB, in JVector normal)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // Reset
        leftCount = 0;
        rightCount = 0;
        manifoldCount = 0;

        JVector crossVector1 = MathHelper.CreateOrthonormal(normal);
        JVector crossVector2 = normal % crossVector1;

        Span<JVector> left = stackalloc JVector[MaxManifoldPoints];
        Span<JVector> right = stackalloc JVector[MaxManifoldPoints];

        for (int e = 0; e < MaxManifoldPoints; e++)
        {
            JVector ptNormal = normal + hexagonVertices[2 * e + 0] * Perturbation * crossVector1 +
                               hexagonVertices[2 * e + 1] * Perturbation * crossVector2;

            JVector.ConjugatedTransform(ptNormal, quaternionA, out JVector tmp);
            shapeA.SupportMap(tmp, out JVector np1);
            JVector.Transform(np1, quaternionA, out np1);
            JVector.Add(np1, positionA, out np1);
            PushLeft(left, np1);

            JVector.NegateInPlace(ref ptNormal);

            JVector.ConjugatedTransform(ptNormal, quaternionB, out tmp);
            shapeB.SupportMap(tmp, out JVector np2);
            JVector.Transform(np2, quaternionB, out np2);
            JVector.Add(np2, positionB, out np2);
            PushRight(right, np2);
        }

        Span<JVector> mA = MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[0]), MaxManifoldPoints);
        Span<JVector> mB = MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[18]), MaxManifoldPoints);

        if (leftCount > 1 && rightCount > 1)
        {
            Span<ClipPoint> left2 = stackalloc ClipPoint[MaxManifoldPoints];
            Span<ClipPoint> right2 = stackalloc ClipPoint[MaxManifoldPoints];

            for (int i = 0; i < leftCount; i++)
            {
                left2[i] = ProjectToPlane(left[i], pA, crossVector1, crossVector2);
            }

            for (int i = 0; i < rightCount; i++)
            {
                right2[i] = ProjectToPlane(right[i], pA, crossVector1, crossVector2);
            }

            CalculateClipTolerance(left2, leftCount, right2, rightCount,
                out Real sideEpsilon, out Real distanceEpsilonSq, out Real areaEpsilon);

            CompactPolygon(left2, ref leftCount, distanceEpsilonSq, areaEpsilon);
            CompactPolygon(right2, ref rightCount, distanceEpsilonSq, areaEpsilon);

            if (leftCount > 2) NormalizeWinding(left2, leftCount);
            if (rightCount > 2) NormalizeWinding(right2, rightCount);

            Span<ClipPoint> clipped = stackalloc ClipPoint[MaxClipPoints];

            int clippedCount = 0;

            if (leftCount > 2 && rightCount > 2)
            {
                Span<ClipPoint> buffer = stackalloc ClipPoint[MaxClipPoints];

                left2[..leftCount].CopyTo(clipped);

                clippedCount = ClipConvexPolygon(clipped, leftCount, right2[..rightCount], rightCount, buffer,
                    sideEpsilon, distanceEpsilonSq, areaEpsilon);
            }

            if (clippedCount == 0)
            {
                TryClipLinearIntersection(left2[..leftCount], leftCount, right2[..rightCount], rightCount,
                    sideEpsilon, distanceEpsilonSq, areaEpsilon, clipped, out clippedCount);
            }

            CompactPolygon(clipped, ref clippedCount, distanceEpsilonSq, areaEpsilon);
            ReducePolygon(clipped, ref clippedCount);

            Real depth = JVector.Dot(pB - pA, normal);

            for (int i = 0; i < clippedCount; i++)
            {
                JVector pointOnA = LiftFromPlane(clipped[i], pA, crossVector1, crossVector2);
                mA[manifoldCount] = pointOnA;
                mB[manifoldCount++] = pointOnA + depth * normal;

                if (manifoldCount == MaxManifoldPoints) return;
            }
        }

        if (manifoldCount == 0)
        {
            mA[manifoldCount] = pA;
            mB[manifoldCount++] = pB;
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
    public void BuildManifold<Ta,Tb>(Ta shapeA, Tb shapeB,
        in JVector pA, in JVector pB, in JVector normal) where Ta : RigidBodyShape where Tb : RigidBodyShape
    {
        BuildManifold(shapeA, shapeB, shapeA.RigidBody.Orientation, shapeB.RigidBody.Orientation,
            shapeA.RigidBody.Position, shapeB.RigidBody.Position, pA, pB, normal);
    }
}
