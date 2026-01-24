/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

// #define DEBUG_EDGEFILTER

using System.Runtime.CompilerServices;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Filters internal edge collisions for triangle mesh geometry.
/// </summary>
/// <remarks>
/// <para>
/// When rigid bodies slide over triangle meshes, they may collide with internal edges where
/// triangles meet, causing jitter. This filter adjusts collision normals at edges to match
/// neighboring triangles or discards the collision entirely.
/// </para>
/// <para>
/// Works best with manifold meshes that have adjacency information. Boundary edges
/// (no neighbor) are processed normally. Back-face collisions are discarded.
/// </para>
/// </remarks>
public class TriangleEdgeCollisionFilter : INarrowPhaseFilter
{
    /// <summary>
    /// Gets or sets the distance threshold for edge collision detection, in world units.
    /// </summary>
    /// <remarks>
    /// Collision points closer than this distance to a triangle edge are considered edge collisions
    /// and may have their normals adjusted or be discarded. Larger values are more aggressive at
    /// filtering edges but may incorrectly affect legitimate collisions near triangle boundaries.
    /// </remarks>
    /// <value>The default value is 0.01 world units.</value>
    public Real EdgeThreshold { get; set; } = (Real)0.01;

    private Real cosAngle = (Real)0.999;

    /// <summary>
    /// Gets or sets the minimum length of the projected collision normal required to keep the contact.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the collision normal is projected onto the plane formed by the triangle normal and its
    /// neighbor's normal, a very short projection indicates the collision is occurring along the
    /// edge crease itself. Such collisions are discarded as they typically represent internal edge artifacts.
    /// </para>
    /// <para>
    /// Lower values allow more edge collisions through; higher values are more aggressive at filtering.
    /// </para>
    /// </remarks>
    /// <value>The default value is 0.5.</value>
    public Real ProjectionThreshold { get; set; } = (Real)0.5;

    /// <summary>
    /// Gets or sets the angle threshold for determining when two triangle normals are considered identical.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a collision occurs near an edge, this threshold determines whether the current triangle
    /// and its neighbor are treated as coplanar (same normal) or as forming a crease. Coplanar
    /// neighbors use simpler normal snapping logic.
    /// </para>
    /// <para>
    /// This threshold is also used to detect and discard back-face collisions: if the collision
    /// normal points opposite to the triangle normal (beyond this angle from perpendicular),
    /// the collision is filtered out.
    /// </para>
    /// </remarks>
    /// <value>The default value is approximately 2.5 degrees.</value>
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

        ref readonly var triangle = ref triangleShape.Mesh.Indices[triangleShape.Index];

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
        else if (d1 < d2)
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
        // by removing the component along the cross product axis
        JVector cross = nnormal % tnormal;
        Real crossLenSq = cross.LengthSquared();
        JVector proj = normal - (cross * normal / crossLenSq) * cross;

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