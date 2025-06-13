/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

// #define DEBUG_EDGEFILTER

using System.Runtime.CompilerServices;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Level geometry is often represented by multiple instances of <see cref="Collision.Shapes.TriangleShape"/>
/// added to a <see cref="Dynamics.RigidBody"/>. Other rigid bodies sliding over these triangles
/// might encounter "internal edges", resulting in jitter. The <see cref="TriangleEdgeCollisionFilter"/>
/// implements the <see cref="INarrowPhaseFilter"/> to help filter out these internal edges.
/// </summary>
public class TriangleEdgeCollisionFilter : INarrowPhaseFilter
{
    /// <summary>
    /// A tweakable parameter. Collision points that are closer than this value to a triangle edge
    /// are considered as edge collisions and might be modified or discarded entirely.
    /// </summary>
    public Real EdgeThreshold { get; set; } = (Real)0.01;

    // approx 2.5°
    private Real cosAngle = (Real)0.999;

    /// <summary>
    /// A tweakable parameter.
    /// </summary>
    public Real ProjectionThreshold { get; set; } = (Real)0.5;

    /// <summary>
    /// A tweakable parameter that defines the threshold to determine when two normals
    /// are considered identical.
    /// </summary>
    public JAngle AngleThreshold
    {
        get => JAngle.FromRadian(MathR.Acos(cosAngle));
        set => cosAngle = MathR.Cos(value.Radian);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProjectPointOnPlane(ref JVector point, in JVector a, in JVector b, in JVector c)
    {
        var ab = b - a;
        var ac = c - a;
        var normal = JVector.Cross(ab, ac);
        JVector.NormalizeInPlace(ref normal);

        var ap = point - a;
        var distance = JVector.Dot(ap, normal);
        point -= distance * normal;
    }

    /// <inheritdoc />
    public bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB,
        ref JVector pointA, ref JVector pointB, ref JVector normal, ref Real penetration)
    {
        TriangleShape? ts1 = shapeA as TriangleShape;
        TriangleShape? ts2 = shapeB as TriangleShape;

        bool c1 = ts1 != null;
        bool c2 = ts2 != null;

        // both shapes are triangles or both of them are not -> return
        if (c1 == c2) return true;

        TriangleShape triangleShape;

        JVector collP;

        if (c1)
        {
            triangleShape = ts1!;
            collP = pointA;
        }
        else
        {
            triangleShape = ts2!;
            collP = pointB;
        }

        ref var triangle = ref triangleShape.Mesh.Indices[triangleShape.Index];

        JVector tnormal = triangle.Normal;
        tnormal = JVector.Transform(tnormal, triangleShape.RigidBody.Data.Orientation);

        if (c2) JVector.NegateInPlace(ref tnormal);

        // Make triangles penetrable from one side
        if (JVector.Dot(normal, tnormal) < -cosAngle) return false;

        triangleShape.GetWorldVertices(out JVector a, out JVector b, out JVector c);

        // project collP onto triangle plane
        ProjectPointOnPlane(ref collP, a, b, c);

        JVector n, pma;

        n = b - a;
        pma = collP - a;
        var d0 = (pma - JVector.Dot(pma, n) * n * ((Real)1.0 / n.LengthSquared())).LengthSquared();

        n = c - a;
        pma = collP - a;
        var d1 = (pma - JVector.Dot(pma, n) * n * ((Real)1.0 / n.LengthSquared())).LengthSquared();

        n = c - b;
        pma = collP - b;
        var d2 = (pma - JVector.Dot(pma, n) * n * ((Real)1.0 / n.LengthSquared())).LengthSquared();

        if (MathR.Min(MathR.Min(d0, d1), d2) > EdgeThreshold * EdgeThreshold) return true;

        JVector nnormal;

        if (d0 < d1 && d0 < d2)
        {
            if (triangle.NeighborC == -1) return true;
            nnormal = triangleShape.Mesh.Indices[triangle.NeighborC].Normal;
        }
        else if (d1 <= d0 && d1 < d2)
        {
            if (triangle.NeighborB == -1) return true;
            nnormal = triangleShape.Mesh.Indices[triangle.NeighborB].Normal;
        }
        else
        {
            if (triangle.NeighborA == -1) return true;
            nnormal = triangleShape.Mesh.Indices[triangle.NeighborA].Normal;
        }

        nnormal = JVector.Transform(nnormal, triangleShape.RigidBody.Data.Orientation);

        ref var b1Data = ref shapeA.RigidBody.Data;
        ref var b2Data = ref shapeB.RigidBody.Data;

        bool isSpeculative = penetration < (Real)0.0;

        if (!isSpeculative)
        {
            // Check collision again (for non-speculative contacts),
            // but with zero epa threshold parameter. This is necessary since
            // MPR is not exact for flat shapes, like triangles.

            bool result = NarrowPhase.MprEpa(shapeA, shapeB,
                b1Data.Orientation, b2Data.Orientation,
                b1Data.Position, b2Data.Position,
                out pointA, out pointB, out normal, out penetration,
                epaThreshold: (Real)0.0);

            if (!result)
            {
                // this should not happen
                return false;
            }
        }

        if (c2) JVector.NegateInPlace(ref nnormal);

        JVector midPoint = (Real)0.5 * (pointA + pointB);

        // now the fun part
        //
        // we have a collision close to an edge, with
        //
        // tnormal -> the triangle normal where collision occurred
        // nnormal -> the normal of neighboring triangle
        // normal  -> the collision normal
        if (JVector.Dot(tnormal, nnormal) > cosAngle)
        {
            // tnormal and nnormal are the same
            // --------------------------------
            Real f5 = JVector.Dot(normal, nnormal);
            Real f6 = JVector.Dot(normal, tnormal);

            if (f5 > f6)
            {
#if DEBUG_EDGEFILTER
                Console.WriteLine($"case #1: adjusting; normal {normal} -> {nnormal}");
#endif

                if (!isSpeculative)
                {
                    penetration = 0;
                    pointA = pointB = midPoint;
                }

                normal = nnormal;
            }
            else
            {
#if DEBUG_EDGEFILTER
                Console.WriteLine($"case #1: adjusting; normal {normal} -> {tnormal}");
#endif

                if (!isSpeculative)
                {
                    penetration = 0;
                    pointA = pointB = midPoint;
                }

                normal = tnormal;
            }

            return true;
        }
        // nnormal and tnormal are different
        // ----------------------------------

        // 1st step, project the normal onto the plane given by tnormal and nnormal
        JVector cross = nnormal % tnormal;
        JVector proj = normal - cross * normal * cross;

        if (proj.LengthSquared() < ProjectionThreshold * ProjectionThreshold)
        {
#if DEBUG_EDGEFILTER
            Console.WriteLine($"case #3: discarding");

#endif
            // can not project onto the plane, discard
            return false;
        }

        // 2nd step, determine if "proj" is between nnormal and tnormal
        //
        //    /    nnormal
        //   /
        //  /
        //  -----  proj
        // \
        //  \
        //   \     tnormal
        Real f1 = proj % nnormal * cross;
        Real f2 = proj % tnormal * cross;

        bool between = f1 <= 0.0f && f2 >= 0.0f;

        if (!between)
        {
            // not in-between, snap normal
            Real f3 = JVector.Dot(normal, nnormal);
            Real f4 = JVector.Dot(normal, tnormal);

            if (f3 > f4)
            {
#if DEBUG_EDGEFILTER
                Console.WriteLine($"case #2: adjusting; normal {normal} -> {nnormal}");

#endif
                if (!isSpeculative)
                {
                    penetration = 0;
                    pointA = pointB = midPoint;
                }

                normal = nnormal;
            }
            else
            {
#if DEBUG_EDGEFILTER
                Console.WriteLine($"case #2: adjusting; normal {normal} -> {tnormal}");
#endif
                if (!isSpeculative)
                {
                    penetration = 0;
                    pointA = pointB = midPoint;
                }

                normal = tnormal;
            }
        }

        return true;
    }
}