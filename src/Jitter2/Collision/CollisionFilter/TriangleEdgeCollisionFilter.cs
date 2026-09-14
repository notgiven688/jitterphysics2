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
/// Filters internal edge collisions for <see cref="TriangleShape"/> geometry. Adjusts collision
/// normals at shared edges to match neighboring triangles, or discards the collision if the normal
/// cannot be resolved. Requires triangle adjacency information; boundary edges (no neighbor)
/// are left unmodified. Back-face collisions are discarded.
/// </summary>
public class TriangleEdgeCollisionFilter : INarrowPhaseFilter
{
    private enum EdgeContactEvaluation
    {
        Keep,
        Discard,
        Edge
    }

    /// <summary>
    /// Gets or sets the distance threshold for edge collision detection, in world units.
    /// </summary>
    /// <remarks>
    /// Larger values are more aggressive at filtering edges but may incorrectly affect
    /// legitimate collisions near triangle boundaries.
    /// </remarks>
    /// <value>The default value is 0.01 world units.</value>
    public Real EdgeThreshold { get; set; } = (Real)0.01;

    private Real cosAngle = (Real)0.999;

    /// <summary>
    /// Gets or sets the minimum length of the projected collision normal required to keep the contact.
    /// </summary>
    /// <remarks>
    /// When the collision normal is projected onto the plane formed by the triangle normal and its
    /// neighbor's normal, a very short projection indicates the collision is occurring along the
    /// edge crease itself and is discarded. Lower values allow more edge collisions through;
    /// higher values are more aggressive at filtering.
    /// </remarks>
    /// <value>The default value is 0.5.</value>
    public Real ProjectionThreshold { get; set; } = (Real)0.5;

    /// <summary>
    /// Gets or sets the angle threshold used for two purposes: determining when two triangle normals
    /// are considered coplanar (using simpler snapping logic), and discarding back-face collisions
    /// whose normal is anti-parallel to the triangle normal within this angle.
    /// </summary>
    /// <value>The default value is approximately 2.5 degrees.</value>
    public JAngle AngleThreshold
    {
        get => JAngle.FromRadian(StableMath.Acos(cosAngle));
        set => cosAngle = StableMath.Cos(value.Radian);
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

    private EdgeContactEvaluation EvaluateTriangleContact(TriangleShape triangleShape, bool triangleIsShapeB,
        in JVector contactPoint, in JVector normal, out JVector triangleNormal, out JVector neighborNormal)
    {
        ref readonly var triangle = ref triangleShape.Mesh.Indices[triangleShape.Index];

        triangleNormal = triangle.Normal;
        triangleNormal = JVector.Transform(triangleNormal, triangleShape.RigidBody.Data.Orientation);
        neighborNormal = default;

        if (triangleIsShapeB) JVector.NegateInPlace(ref triangleNormal);

        // Make triangles penetrable from one side.
        if (JVector.Dot(normal, triangleNormal) < -cosAngle) return EdgeContactEvaluation.Discard;

        triangleShape.GetWorldVertices(out JVector a, out JVector b, out JVector c);

        JVector projectedContactPoint = contactPoint;
        ProjectPointOnPlane(ref projectedContactPoint, a, b, c);

        var n = b - a;
        var pma = projectedContactPoint - a;
        var d0 = (pma - JVector.Dot(pma, n) * n * ((Real)1.0 / n.LengthSquared())).LengthSquared();

        n = c - a;
        pma = projectedContactPoint - a;
        var d1 = (pma - JVector.Dot(pma, n) * n * ((Real)1.0 / n.LengthSquared())).LengthSquared();

        n = c - b;
        pma = projectedContactPoint - b;
        var d2 = (pma - JVector.Dot(pma, n) * n * ((Real)1.0 / n.LengthSquared())).LengthSquared();

        if (MathR.Min(MathR.Min(d0, d1), d2) > EdgeThreshold * EdgeThreshold)
        {
            return EdgeContactEvaluation.Keep;
        }

        if (d0 < d1 && d0 < d2)
        {
            if (triangle.NeighborC == -1) return EdgeContactEvaluation.Keep;
            neighborNormal = triangleShape.Mesh.Indices[triangle.NeighborC].Normal;
        }
        else if (d1 < d2)
        {
            if (triangle.NeighborB == -1) return EdgeContactEvaluation.Keep;
            neighborNormal = triangleShape.Mesh.Indices[triangle.NeighborB].Normal;
        }
        else
        {
            if (triangle.NeighborA == -1) return EdgeContactEvaluation.Keep;
            neighborNormal = triangleShape.Mesh.Indices[triangle.NeighborA].Normal;
        }

        neighborNormal = JVector.Transform(neighborNormal, triangleShape.RigidBody.Data.Orientation);

        if (triangleIsShapeB) JVector.NegateInPlace(ref neighborNormal);

        return EdgeContactEvaluation.Edge;
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

        if (c1)
        {
            triangleShape = ts1!;
        }
        else
        {
            triangleShape = ts2!;
        }

        EdgeContactEvaluation evaluation = EvaluateTriangleContact(triangleShape, c2,
            c1 ? pointA : pointB, normal, out JVector triangleNormal, out JVector neighborNormal);

        if (evaluation == EdgeContactEvaluation.Discard) return false;
        if (evaluation == EdgeContactEvaluation.Keep) return true;

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
                // MPR refinement failed; reject the contact.
                return false;
            }

            evaluation = EvaluateTriangleContact(triangleShape, c2,
                c1 ? pointA : pointB, normal, out triangleNormal, out neighborNormal);

            if (evaluation == EdgeContactEvaluation.Discard) return false;
            if (evaluation == EdgeContactEvaluation.Keep) return true;
        }

        JVector midPoint = (Real)0.5 * (pointA + pointB);

        // Now the fun part.
        //
        // we have a collision close to an edge, with
        //
        // triangleNormal -> the triangle normal where collision occurred
        // neighborNormal -> the normal of the neighboring triangle
        // normal         -> the collision normal
        if (JVector.Dot(triangleNormal, neighborNormal) > cosAngle)
        {
            // triangleNormal and neighborNormal are the same
            // ----------------------------------------------
            Real f5 = JVector.Dot(normal, neighborNormal);
            Real f6 = JVector.Dot(normal, triangleNormal);

            if (f5 > f6)
            {
#if DEBUG_EDGEFILTER
                Console.WriteLine($"case #1: adjusting; normal {normal} -> {neighborNormal}");
#endif

                if (!isSpeculative)
                {
                    penetration = 0;
                    pointA = pointB = midPoint;
                }

                normal = neighborNormal;
            }
            else
            {
#if DEBUG_EDGEFILTER
                Console.WriteLine($"case #1: adjusting; normal {normal} -> {triangleNormal}");
#endif

                if (!isSpeculative)
                {
                    penetration = 0;
                    pointA = pointB = midPoint;
                }

                normal = triangleNormal;
            }

            return true;
        }
        // neighborNormal and triangleNormal are different
        // -----------------------------------------------

        // 1st step, project the normal onto the plane given by triangleNormal and neighborNormal
        // by removing the component along the cross product axis
        JVector cross = neighborNormal % triangleNormal;
        Real crossLenSq = cross.LengthSquared();
        JVector proj = normal - (cross * normal / crossLenSq) * cross;

        if (proj.LengthSquared() < ProjectionThreshold * ProjectionThreshold)
        {
#if DEBUG_EDGEFILTER
            Console.WriteLine($"case #3: discarding");

#endif
            // Cannot project onto the plane, discard.
            return false;
        }

        // 2nd step, determine if "proj" is between neighborNormal and triangleNormal
        //
        //    /    neighborNormal
        //   /
        //  /
        //  -----  proj
        // \
        //  \
        //   \     triangleNormal
        Real f1 = proj % neighborNormal * cross;
        Real f2 = proj % triangleNormal * cross;

        bool between = f1 <= (Real)0.0 && f2 >= (Real)0.0;

        if (!between)
        {
            // not in-between, snap normal
            Real f3 = JVector.Dot(normal, neighborNormal);
            Real f4 = JVector.Dot(normal, triangleNormal);

            if (f3 > f4)
            {
#if DEBUG_EDGEFILTER
                Console.WriteLine($"case #2: adjusting; normal {normal} -> {neighborNormal}");

#endif
                if (!isSpeculative)
                {
                    penetration = 0;
                    pointA = pointB = midPoint;
                }

                normal = neighborNormal;
            }
            else
            {
#if DEBUG_EDGEFILTER
                Console.WriteLine($"case #2: adjusting; normal {normal} -> {triangleNormal}");
#endif
                if (!isSpeculative)
                {
                    penetration = 0;
                    pointA = pointB = midPoint;
                }

                normal = triangleNormal;
            }
        }

        return true;
    }
}
