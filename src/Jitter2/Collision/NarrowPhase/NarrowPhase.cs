/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jitter2.LinearMath;
using Vertex = Jitter2.Collision.MinkowskiDifference.Vertex;

namespace Jitter2.Collision;

/// <summary>
/// Provides collision detection algorithms for convex shapes defined by support functions.
/// </summary>
/// <remarks>
/// <para>
/// Implements GJK (Gilbert-Johnson-Keerthi) for distance queries, MPR (Minkowski Portal Refinement)
/// for fast overlap detection, and EPA (Expanding Polytope Algorithm) for penetration depth.
/// </para>
/// <para>
/// All methods are thread-safe when used with thread-local solver state.
/// </para>
/// </remarks>
public static partial class NarrowPhase
{
    private const Real NumericEpsilon = (Real)1e-16;

    // Contract: GJK- and MPR-based queries report geometric yes/no results; they do not expose
    // an algorithm-failed state. EPA is the only step that may fail to converge. MPR refinement falls back
    // to the MPR result if EPA fails, while 'NarrowPhase.Collision' documents false as EPA/convergence failure.
    private struct MprEpaSolver
    {
        private ConvexPolytope convexPolytope;

        private static bool IsOnSupportPlane(in JVector point, in JVector support, in JVector normal)
        {
#if USE_DOUBLE_PRECISION
            const Real roundoff = 8 * 2.2204460492503131e-16;
#else
            const Real roundoff = 8 * 1.1920928955078125e-7f;
#endif
            JVector delta = support - point;
            Real x = delta.X * normal.X, y = delta.Y * normal.Y, z = delta.Z * normal.Z;
            return MathR.Abs(x + y + z) <= roundoff * (MathR.Abs(x) + MathR.Abs(y) + MathR.Abs(z));
        }

        private static bool TryFaceContact<Ta, Tb>(in Ta supportA, in Tb supportB,
            in JQuaternion orientationB, in JVector positionB,
            in Vertex v1, in Vertex v2, in Vertex v3, bool faceOnA, Real tolerance,
            ref JVector pointA, ref JVector pointB, ref JVector normal, ref Real penetration)
            where Ta : ISupportMappable where Tb : ISupportMappable
        {
            JVector a = faceOnA ? v1.A : v1.B;
            JVector b = faceOnA ? v2.A : v2.B;
            JVector c = faceOnA ? v3.A : v3.B;
            JVector ab = b - a, ac = c - a;
            JVector rawNormal = ab % ac;
            Real normalSq = rawNormal.LengthSquared();
            if (!(normalSq > NumericEpsilon) || !Real.IsFinite(normalSq)) return false;

            JVector faceNormal = rawNormal * ((Real)1.0 / MathR.Sqrt(normalSq));
            if (JVector.Dot(faceNormal, normal) < (Real)0.0) faceNormal = -faceNormal;

            // Three support samples need not belong to one face. Check that their plane supports
            // the shape, then query the opposite shape in that face's normal direction.
            MinkowskiDifference.Support(supportA, supportB, orientationB, positionB, faceNormal, out Vertex support);
            JVector faceSupport = faceOnA ? support.A : support.B;
            // Allow only arithmetic roundoff here. The collision tolerance can also accept a
            // triangle just inside a curved surface, which is not a supporting face.
            if (!IsOnSupportPlane(a, faceSupport, faceNormal)) return false;
            Real depth = JVector.Dot(support.V, faceNormal);
            if (depth < (Real)0.0 || depth > penetration + tolerance) return false;

            JVector candidateA = faceOnA ? support.B + depth * faceNormal : support.A;
            JVector candidateB = faceOnA ? support.B : support.A - depth * faceNormal;
            JVector point = faceOnA ? candidateA : candidateB;

            // The projected point must lie on the sampled face, not just its infinite plane.
            if (JVector.Dot(ab % (point - a), rawNormal) < (Real)0.0 ||
                JVector.Dot((c - b) % (point - b), rawNormal) < (Real)0.0 ||
                JVector.Dot((a - c) % (point - c), rawNormal) < (Real)0.0) return false;

            pointA = candidateA;
            pointB = candidateB;
            normal = faceNormal;
            penetration = depth;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SolveMprEpa<Ta, Tb>(in Ta supportA, in Tb supportB, in JQuaternion orientationB, in JVector positionB,
            ref JVector point1, ref JVector point2, ref JVector normal, ref Real penetration)
            where Ta : ISupportMappable where Tb : ISupportMappable
        {
            const Real collideEpsilon = (Real)1e-5;
            const int maxIter = 85;

            if (!convexPolytope.InitTetrahedron()) return false;

            int iter = 0;

            Unsafe.SkipInit(out ConvexPolytope.Triangle ctri);

            while (++iter < maxIter)
            {
                ctri = convexPolytope.GetClosestTriangle();

                JVector searchDir = ctri.ClosestToOrigin;
                Real searchDirSq = ctri.ClosestToOriginSq;

                if (!convexPolytope.OriginEnclosed) JVector.NegateInPlace(ref searchDir);
                if (ctri.ClosestToOriginSq < NumericEpsilon)
                {
                    searchDir = ctri.Normal;
                    searchDirSq = ctri.NormalSq;
                }

                MinkowskiDifference.Support(supportA, supportB, orientationB, positionB,
                    searchDir, out Vertex vertex);

                // compare with the corresponding code in Collision.
                Real deltaDist = JVector.Dot(ctri.ClosestToOrigin - vertex.V, searchDir);

                if (deltaDist * deltaDist <= collideEpsilon * collideEpsilon * searchDirSq)
                {
                    goto converged;
                }

                var expansion = convexPolytope.AddVertexDetailed(vertex);
                if (expansion == ConvexPolytope.AddVertexResult.NoHorizon) goto converged;
                if (expansion != ConvexPolytope.AddVertexResult.Added) return false;
            }

            Logger.Warning("{0}: EPA, Could not converge within {1} iterations.", nameof(NarrowPhase), maxIter);

            return false;

        converged:

            if (!convexPolytope.OriginEnclosed) return false;
            convexPolytope.CalculatePoints(ctri, out point1, out point2);

            normal = ctri.Normal * ((Real)1.0 / MathR.Sqrt(ctri.NormalSq));
            penetration = MathR.Sqrt(ctri.ClosestToOriginSq);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public bool SolveMpr<Ta, Tb>(in Ta supportA, in Tb supportB, in JQuaternion orientationB,
            in JVector positionB, Real epaThreshold,
            out JVector pointA, out JVector pointB, out JVector normal, out Real penetration)
            where Ta : ISupportMappable where Tb : ISupportMappable
        {
            /*
            This method is adapted from XenoCollide.
            Modified from the original version.

            XenoCollide is available under the zlib license:

            XenoCollide Collision Detection and Physics Library
            Copyright (c) 2007-2014 Gary Snethen http://xenocollide.com

            This software is provided 'as-is', without any express or implied warranty.
            In no event will the authors be held liable for any damages arising
            from the use of this software.
            Permission is granted to anyone to use this software for any purpose,
            including commercial applications, and to alter it and redistribute it freely,
            subject to the following restrictions:

            1. The origin of this software must not be misrepresented; you must
            not claim that you wrote the original software. If you use this
            software in a product, an acknowledgment in the product documentation
            would be appreciated but is not required.
            2. Altered source versions must be plainly marked as such, and must
            not be misrepresented as being the original software.
            3. This notice may not be removed or altered from any source distribution.
            */
            const Real collideEpsilon = (Real)1e-5;
            const int maxIter = 34;

            Unsafe.SkipInit(out Vertex v0);
            Unsafe.SkipInit(out Vertex v1);
            Unsafe.SkipInit(out Vertex v2);
            Unsafe.SkipInit(out Vertex v3);
            Unsafe.SkipInit(out Vertex v4);

            Unsafe.SkipInit(out JVector temp1);
            Unsafe.SkipInit(out JVector temp2);
            Unsafe.SkipInit(out JVector temp3);

            penetration = (Real)0.0;

            MinkowskiDifference.GetCenter(supportA, supportB, orientationB, positionB, out v0);

            if (Math.Abs(v0.V.X) < NumericEpsilon &&
                Math.Abs(v0.V.Y) < NumericEpsilon &&
                Math.Abs(v0.V.Z) < NumericEpsilon)
            {
                // any direction is fine
                v0.V.X = (Real)1e-05;
            }

            JVector.Negate(v0.V, out normal);

            MinkowskiDifference.Support(supportA, supportB, orientationB, positionB, normal, out v1);

            pointA = v1.A;
            pointB = v1.B;

            if (JVector.Dot(v1.V, normal) <= (Real)0.0) return false;
            JVector.Cross(v1.V, v0.V, out normal);

            const Real sphericalEpsilon = (Real)1e-12;
            if (normal.LengthSquared() < sphericalEpsilon)
            {
                // The origin, v0 and v1 form a line. Most probably
                // two spheres colliding.
                JVector.Subtract(v1.V, v0.V, out normal);
                JVector.NormalizeInPlace(ref normal);

                JVector.Subtract(v1.A, v1.B, out temp1);
                penetration = JVector.Dot(temp1, normal);

                return true;
            }

            MinkowskiDifference.Support(supportA, supportB, orientationB, positionB, normal, out v2);

            if (JVector.Dot(v2.V, normal) <= (Real)0.0) return false;

            // Determine whether the origin is on + or - side of plane (v1.V, v0.V, v2.V)
            JVector.Subtract(v1.V, v0.V, out temp1);
            JVector.Subtract(v2.V, v0.V, out temp2);
            JVector.Cross(temp1, temp2, out normal);

            Real dist = JVector.Dot(normal, v0.V);

            // If the origin is on the - side of the plane, reverse the direction of the plane
            if (dist > (Real)0.0)
            {
                JVector.Swap(ref v1.V, ref v2.V);
                JVector.Swap(ref v1.A, ref v2.A);
                JVector.Swap(ref v1.B, ref v2.B);
                JVector.Negate(normal, out normal);
            }

            int phase2 = 0;
            int phase1 = 0;
            bool hit = false;

            // Phase One: Identify a portal
            while (true)
            {
                if (phase1 > maxIter) return false;

                phase1++;

                MinkowskiDifference.Support(supportA, supportB, orientationB, positionB, normal, out v3);

                if (JVector.Dot(v3.V, normal) <= (Real)0.0)
                {
                    return false;
                }

                // If origin is outside (v1.V,v0.V,v3.V), then eliminate v2.V and loop
                JVector.Cross(v1.V, v3.V, out temp1);
                if (JVector.Dot(temp1, v0.V) < (Real)0.0)
                {
                    v2 = v3;
                    JVector.Subtract(v1.V, v0.V, out temp1);
                    JVector.Subtract(v3.V, v0.V, out temp2);
                    JVector.Cross(temp1, temp2, out normal);
                    continue;
                }

                // If origin is outside (v3.V,v0.V,v2.V), then eliminate v1.V and loop
                JVector.Cross(v3.V, v2.V, out temp1);
                if (JVector.Dot(temp1, v0.V) < (Real)0.0)
                {
                    v1 = v3;
                    JVector.Subtract(v3.V, v0.V, out temp1);
                    JVector.Subtract(v2.V, v0.V, out temp2);
                    JVector.Cross(temp1, temp2, out normal);
                    continue;
                }

                break;
            }

            // Phase Two: Refine the portal
            // We are now inside a wedge...
            while (true)
            {
                phase2++;

                // Compute normal of the wedge face
                JVector.Subtract(v2.V, v1.V, out temp1);
                JVector.Subtract(v3.V, v1.V, out temp2);
                JVector.Cross(temp1, temp2, out normal);

                // normal.Normalize();
                Real normalSq = normal.LengthSquared();

                // Can this happen???  Can it be handled more cleanly?
                if (normalSq < NumericEpsilon)
                {
                    // was: return true;
                    // better not return a collision
                    Debug.Assert(false, "MPR: This should not happen.");
                    return false;
                }

                if (!hit)
                {
                    // Compute distance from origin to wedge face
                    Real d = JVector.Dot(normal, v1.V);
                    // If the origin is inside the wedge, we have a hit
                    hit = d >= 0;
                }

                MinkowskiDifference.Support(supportA, supportB, orientationB, positionB, normal, out v4);

                JVector.Subtract(v4.V, v3.V, out temp3);
                Real delta = JVector.Dot(temp3, normal);
                penetration = JVector.Dot(v4.V, normal);

                // If the boundary is thin enough, the origin is outside the support plane for the newly discovered
                // vertex, or the maximum number of iterations has been reached, then we can terminate
                if (delta * delta <= collideEpsilon * collideEpsilon * normalSq || penetration <= (Real)0.0 ||
                    phase2 > maxIter)
                {
                    if (hit)
                    {
                        Real invnormal = (Real)1.0 / MathR.Sqrt(normalSq);

                        penetration *= invnormal;

                        if (penetration > epaThreshold)
                        {
                            convexPolytope.InitHeap();
                            convexPolytope.GetVertex(0) = v0;
                            convexPolytope.GetVertex(1) = v1;
                            convexPolytope.GetVertex(2) = v2;
                            convexPolytope.GetVertex(3) = v3;

                            // If epa fails it does not set any result data. We continue with the mpr data.
                            if (SolveMprEpa(supportA, supportB, orientationB, positionB,
                                    ref pointA, ref pointB, ref normal, ref penetration)) return true;
                        }

                        normal *= invnormal;

                        // A portal can have an accurate normal while its interpolated shape points remain
                        // offset along a face. Prefer a fresh support point projected onto a verified face;
                        // this also avoids averaging curved-surface samples into a point inside the shape.
                        if (TryFaceContact(supportA, supportB, orientationB, positionB, v1, v2, v3, true, collideEpsilon,
                                ref pointA, ref pointB, ref normal, ref penetration) ||
                            TryFaceContact(supportA, supportB, orientationB, positionB, v1, v2, v3, false, collideEpsilon,
                                ref pointA, ref pointB, ref normal, ref penetration)) return true;

                        // Compute the barycentric coordinates of the origin
                        JVector.Cross(v1.V, temp1, out temp3);
                        Real gamma = JVector.Dot(temp3, normal) * invnormal;
                        JVector.Cross(temp2, v1.V, out temp3);
                        Real beta = JVector.Dot(temp3, normal) * invnormal;
                        Real alpha = (Real)1.0 - gamma - beta;

                        pointA = alpha * v1.A + beta * v2.A + gamma * v3.A;
                        pointB = alpha * v1.B + beta * v2.B + gamma * v3.B;
                    }

                    return hit;
                }

                // Compute the tetrahedron dividing face (v4.V, v0.V, v3.V)
                JVector.Cross(v4.V, v0.V, out temp1);
                Real dot = JVector.Dot(temp1, v1.V);

                if (dot >= (Real)0.0)
                {
                    dot = JVector.Dot(temp1, v2.V);

                    if (dot >= (Real)0.0)
                    {
                        v1 = v4; // Inside d1 & inside d2 -> eliminate v1.V
                    }
                    else
                    {
                        v3 = v4; // Inside d1 & outside d2 -> eliminate v3.V
                    }
                }
                else
                {
                    dot = JVector.Dot(temp1, v3.V);

                    if (dot >= (Real)0.0)
                    {
                        v2 = v4; // Outside d1 & inside d3 -> eliminate v2.V
                    }
                    else
                    {
                        v1 = v4; // Outside d1 & outside d3 -> eliminate v1.V
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Collision<Ta, Tb>(in Ta supportA, in Tb supportB, in JQuaternion orientationB, in JVector positionB,
            out JVector point1, out JVector point2, out JVector normal, out Real penetration)
            where Ta : ISupportMappable where Tb : ISupportMappable
        {
            const Real collideEpsilon = (Real)1e-4;
            const int maxIter = 85;

            MinkowskiDifference.GetCenter(supportA, supportB, orientationB, positionB, out Vertex centerVertex);
            JVector center = centerVertex.V;

            convexPolytope.InitHeap();
            if (!convexPolytope.InitTetrahedron(center))
            {
                point1 = point2 = normal = JVector.Zero;
                penetration = 0;
                return false;
            }

            int iter = 0;

            Unsafe.SkipInit(out ConvexPolytope.Triangle ctri);

            while (++iter < maxIter)
            {
                ctri = convexPolytope.GetClosestTriangle();

                JVector searchDir = ctri.ClosestToOrigin;
                Real searchDirSq = ctri.ClosestToOriginSq;

                if (!convexPolytope.OriginEnclosed) JVector.NegateInPlace(ref searchDir);

                if (ctri.ClosestToOriginSq < NumericEpsilon)
                {
                    searchDir = ctri.Normal;
                    searchDirSq = ctri.NormalSq;
                }

                MinkowskiDifference.Support(supportA, supportB, orientationB, positionB, searchDir, out Vertex vertex);

                // Can we further "extend" the convex hull by adding the new vertex?
                //
                // v = Vertices[vPointer] (support point)
                // c = Triangles[Head].ClosestToOrigin
                // s = searchDir
                //
                // abs(dot(c - v, s)) / len(s) < e <=> [dot(c - v, s)]^2 = e*e*s^2
                Real deltaDist = JVector.Dot(ctri.ClosestToOrigin - vertex.V, searchDir);

                if (deltaDist * deltaDist <= collideEpsilon * collideEpsilon * searchDirSq)
                {
                    goto converged;
                }

                var expansion = convexPolytope.AddVertexDetailed(vertex);
                if (expansion == ConvexPolytope.AddVertexResult.NoHorizon) goto converged;
                if (expansion != ConvexPolytope.AddVertexResult.Added)
                {
                    point1 = point2 = normal = JVector.Zero;
                    penetration = 0;
                    return false;
                }
            }

            point1 = point2 = normal = JVector.Zero;
            penetration = (Real)0.0;

            Logger.Warning("{0}: EPA, Could not converge within {1} iterations.", nameof(NarrowPhase), maxIter);

            return false;

        converged:

            convexPolytope.CalculatePoints(ctri, out point1, out point2);

            penetration = MathR.Sqrt(ctri.ClosestToOriginSq);
            if (!convexPolytope.OriginEnclosed) penetration *= -(Real)1.0;

            if (MathR.Abs(penetration) > NumericEpsilon) normal = ctri.ClosestToOrigin * ((Real)1.0 / penetration);
            else normal = ctri.Normal * ((Real)1.0 / MathR.Sqrt(ctri.NormalSq));

            return true;
        }
    }

    // ------------------------------------------------------------------------------------------------------------
    [ThreadStatic] private static MprEpaSolver _solver;

    /// <summary>
    /// Check if a point is inside a shape.
    /// </summary>
    /// <param name="support">Support map representing the shape.</param>
    /// <param name="point">Point to check.</param>
    /// <returns>Returns true if the point is contained within the shape, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PointTest<Ta>(in Ta support, in JVector point) where Ta : ISupportMappable
    {
        const Real collideEpsilon = (Real)1e-4;
        const int maxIter = 34;

        JVector x = point;

        support.GetCenter(out var center);
        JVector v = x - center;

        Unsafe.SkipInit(out SimplexSolver simplexSolver);
        simplexSolver.Reset();

        int iter = maxIter;

        Real distSq = v.LengthSquared();

        while (distSq > collideEpsilon * collideEpsilon && iter-- != 0)
        {
            support.SupportMap(v, out JVector p);
            JVector.Subtract(x, p, out JVector w);

            Real vw = JVector.Dot(v, w);

            if (vw >= (Real)0.0)
            {
                return false;
            }

            if (!simplexSolver.AddVertex(w, out v))
            {
                goto converged;
            }

            distSq = v.LengthSquared();
        }

    converged:

        return true;
    }

    /// <summary>
    /// Check if a point is inside a shape.
    /// </summary>
    /// <param name="support">Support map representing the shape.</param>
    /// <param name="orientation">Orientation of the shape.</param>
    /// <param name="position">Position of the shape.</param>
    /// <param name="point">Point to check.</param>
    /// <returns>Returns true if the point is contained within the shape, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PointTest<Ta>(in Ta support, in JMatrix orientation,
        in JVector position, in JVector point) where Ta : ISupportMappable
    {
        JVector transformedOrigin = JVector.TransposedTransform(point - position, orientation);
        return PointTest(support, transformedOrigin);
    }

    /// <summary>
    /// Performs a ray cast against a shape.
    /// </summary>
    /// <param name="support">The support function of the shape.</param>
    /// <param name="orientation">The orientation of the shape in world space.</param>
    /// <param name="position">The position of the shape in world space.</param>
    /// <param name="origin">The origin of the ray.</param>
    /// <param name="direction">The direction of the ray; normalization is not necessary.</param>
    /// <param name="lambda">Specifies the hit point of the ray, calculated as 'origin + lambda * direction'.
    /// Zero if the origin is inside the shape, <see cref="Real.PositiveInfinity"/> if the ray does not hit.</param>
    /// <param name="normal">
    /// The normalized normal vector perpendicular to the surface, pointing outwards. Zero if the ray does not hit or
    /// the ray origin is inside the shape.
    /// </param>
    /// <returns>Returns true if the ray intersects with the shape; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RayCast<Ta>(in Ta support, in JQuaternion orientation,
        in JVector position, in JVector origin, in JVector direction, out Real lambda, out JVector normal)
        where Ta : ISupportMappable
    {
        // rotate the ray into the reference frame of bodyA...
        JVector transformedDir = JVector.ConjugatedTransform(direction, orientation);
        JVector transformedOrigin = JVector.ConjugatedTransform(origin - position, orientation);

        bool result = RayCast(support, transformedOrigin, transformedDir, out lambda, out normal);

        // ...rotate back.
        JVector.Transform(normal, orientation, out normal);

        return result;
    }

    /// <summary>
    /// Performs a ray cast against a shape.
    /// </summary>
    /// <param name="support">The support function of the shape.</param>
    /// <param name="origin">The origin of the ray.</param>
    /// <param name="direction">The direction of the ray; normalization is not necessary.</param>
    /// <param name="lambda">Specifies the hit point of the ray, calculated as 'origin + lambda * direction'.
    /// Zero if the origin is inside the shape, <see cref="Real.PositiveInfinity"/> if the ray does not hit.</param>
    /// <param name="normal">
    /// The normalized normal vector perpendicular to the surface, pointing outwards. Zero if the ray does not hit or
    /// the ray origin is inside the shape.
    /// </param>
    /// <returns>Returns true if the ray intersects with the shape; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RayCast<Ta>(in Ta support, in JVector origin, in JVector direction,
        out Real lambda, out JVector normal) where Ta : ISupportMappable
    {
        const Real collideEpsilon = (Real)1e-4;
        const int maxIter = 34;

        normal = JVector.Zero;
        lambda = (Real)0.0;

        JVector r = direction;
        JVector x = origin;

        support.GetCenter(out var center);
        JVector v = x - center;

        Unsafe.SkipInit(out SimplexSolver simplexSolver);
        simplexSolver.Reset();

        int iter = maxIter;

        Real distSq = v.LengthSquared();

        while (distSq > collideEpsilon * collideEpsilon && iter-- != 0)
        {
            support.SupportMap(v, out JVector p);

            JVector.Subtract(x, p, out JVector w);

            Real vdotW = JVector.Dot(v, w);

            if (vdotW > (Real)0.0)
            {
                Real vdotR = JVector.Dot(v, r);

                if (vdotR >= -NumericEpsilon)
                {
                    lambda = Real.PositiveInfinity;
                    return false;
                }

                lambda -= vdotW / vdotR;

                JVector.Multiply(r, lambda, out x);
                JVector.Add(origin, x, out x);
                JVector.Subtract(x, p, out w);
                normal = v;
            }

            if (!simplexSolver.AddVertex(w, out v))
            {
                goto converged;
            }

            distSq = v.LengthSquared();
        }

    converged:

        normal = JVector.NormalizeSafe(in normal, NumericEpsilon);
        return true;
    }

    /// <summary>
    /// Determines whether two convex shapes overlap, providing detailed information for both overlapping and separated
    /// cases. It assumes that support shape A is at position zero and not rotated.
    /// Internally, the method employs the Expanding Polytope Algorithm (EPA) to gather collision information.
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationB">The orientation of shape B.</param>
    /// <param name="positionB">The position of shape B.</param>
    /// <param name="pointA">
    /// For the overlapping case: the deepest point on shape A inside shape B; for the separated case: the
    /// closest point on shape A to shape B.
    /// </param>
    /// <param name="pointB">
    /// For the overlapping case: the deepest point on shape B inside shape A; for the separated case: the
    /// closest point on shape B to shape A.
    /// </param>
    /// <param name="normal">
    /// The normalized collision normal. Its dot product with pointA - pointB gives the signed penetration:
    /// positive for overlap and negative for separation. It remains defined when the points coincide.
    /// </param>
    /// <param name="penetration">Signed penetration depth: positive for overlap, negative for separation.</param>
    /// <returns>
    /// Returns true if the algorithm completes successfully, false otherwise. In case of algorithm convergence
    /// failure, collision information reverts to the type's default values.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Collision<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationB, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real penetration)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // ..perform collision detection..
        bool success = _solver.Collision(supportA, supportB, orientationB, positionB,
            out pointA, out pointB, out normal, out penetration);

        return success;
    }

    /// <summary>
    /// Determines whether two convex shapes overlap, providing detailed information for both overlapping and separated
    /// cases. Internally, the method employs the Expanding Polytope Algorithm (EPA) to gather collision information.
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationA">The orientation of shape A in world space.</param>
    /// <param name="orientationB">The orientation of shape B in world space.</param>
    /// <param name="positionA">The position of shape A in world space.</param>
    /// <param name="positionB">The position of shape B in world space.</param>
    /// <param name="pointA">
    /// For the overlapping case: the deepest point on shape A inside shape B; for the separated case: the
    /// closest point on shape A to shape B.
    /// </param>
    /// <param name="pointB">
    /// For the overlapping case: the deepest point on shape B inside shape A; for the separated case: the
    /// closest point on shape B to shape A.
    /// </param>
    /// <param name="normal">
    /// The normalized collision normal. Its dot product with pointA - pointB gives the signed penetration:
    /// positive for overlap and negative for separation. It remains defined when the points coincide.
    /// </param>
    /// <param name="penetration">Signed penetration depth: positive for overlap, negative for separation.</param>
    /// <returns>
    /// Returns true if the algorithm completes successfully, false otherwise. In case of algorithm convergence
    /// failure, collision information reverts to the type's default values.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Collision<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real penetration)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // rotate into the reference frame of bodyA...
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // ...perform collision detection...
        bool success = _solver.Collision(supportA, supportB, orientation, position,
            out pointA, out pointB, out normal, out penetration);

        // ...rotate back. this hopefully saves some matrix vector multiplication
        // when calling the support function multiple times.
        JVector.Transform(pointA, orientationA, out pointA);
        JVector.Add(pointA, positionA, out pointA);
        JVector.Transform(pointB, orientationA, out pointB);
        JVector.Add(pointB, positionA, out pointB);
        JVector.Transform(normal, orientationA, out normal);

        return success;
    }

    /// <summary>
    /// Provides the distance and closest points for non-overlapping shapes. It
    /// assumes that support shape A is located at position zero and not rotated.
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationB">The orientation of shape B in world space.</param>
    /// <param name="positionB">The position of shape B in world space.</param>
    /// <param name="pointA">Closest point on shape A. Undefined for the overlapping case.</param>
    /// <param name="pointB">Closest point on shape B. Undefined for the overlapping case.</param>
    /// <param name="normal">Unit direction from shape A toward shape B. Undefined for the overlapping case.</param>
    /// <param name="distance">The distance between the separating shapes. Zero if shapes overlap.</param>
    /// <returns>Returns true if the shapes do not overlap and distance information
    /// can be provided.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Distance<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationB, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // ..perform overlap test..
        const Real collideEpsilon = (Real)1e-4;
        const int maxIter = 34;

        Unsafe.SkipInit(out SimplexSolverAB simplexSolver);
        simplexSolver.Reset();

        int iter = maxIter;

        MinkowskiDifference.GetCenter(supportA, supportB, orientationB, positionB, out Vertex center);

        JVector v = center.V;
        Real distSq = v.LengthSquared();

        while (iter-- != 0)
        {
            if (distSq < collideEpsilon * collideEpsilon) goto ret_false;

            MinkowskiDifference.Support(supportA, supportB, orientationB, positionB, -v, out var w);

            Real deltaDist = JVector.Dot(v - w.V, v);
            if (deltaDist * deltaDist < collideEpsilon * collideEpsilon * distSq)
            {
                break;
            }

            if (!simplexSolver.AddVertex(w, out v)) goto ret_false;

            distSq = v.LengthSquared();
        }

        distance = MathR.Sqrt(distSq);
        normal = v * (-(Real)1.0 / distance);
        simplexSolver.GetClosest(out pointA, out pointB);
        return true;

    ret_false:

        distance = (Real)0.0;
        normal = JVector.Zero;
        simplexSolver.GetClosest(out pointA, out pointB);
        return false;
    }

    /// <summary>
    /// Provides the distance and closest points for non-overlapping shapes.
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationA">The orientation of shape A in world space.</param>
    /// <param name="orientationB">The orientation of shape B in world space.</param>
    /// <param name="positionA">The position of shape A in world space.</param>
    /// <param name="positionB">The position of shape B in world space.</param>
    /// <param name="pointA">Closest point on shape A. Undefined for the overlapping case.</param>
    /// <param name="pointB">Closest point on shape B. Undefined for the overlapping case.</param>
    /// <param name="normal">Unit direction from shape A toward shape B. Undefined for the overlapping case.</param>
    /// <param name="distance">The distance between the separating shapes. Zero if shapes overlap.</param>
    /// <returns>Returns true if the shapes do not overlap and distance information
    /// can be provided.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Distance<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // rotate into the reference frame of bodyA...
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // ...perform distance test...
        bool result = Distance(supportA, supportB, orientation, position, out pointA, out pointB, out normal, out distance);

        // ...rotate back. This approach potentially saves some matrix-vector multiplication when
        // the support function is called multiple times.
        JVector.Transform(pointA, orientationA, out pointA);
        JVector.Add(pointA, positionA, out pointA);
        JVector.Transform(pointB, orientationA, out pointB);
        JVector.Add(pointB, positionA, out pointB);
        JVector.Transform(normal, orientationA, out normal);

        return result;
    }

    /// <summary>
    /// Performs an overlap test. It assumes that support shape A is located
    /// at position zero and not rotated.
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationB">The orientation of shape B in world space.</param>
    /// <param name="positionB">The position of shape B in world space.</param>
    /// <returns>Returns true of the shapes overlap, and false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Overlap<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationB, in JVector positionB) where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // ..perform overlap test..
        const Real collideEpsilon = (Real)1e-4;
        const int maxIter = 34;

        Unsafe.SkipInit(out SimplexSolverAB simplexSolver);
        simplexSolver.Reset();

        int iter = maxIter;

        MinkowskiDifference.GetCenter(supportA, supportB, orientationB, positionB, out Vertex center);

        JVector v = center.V;
        Real distSq = v.LengthSquared();

        while (distSq > collideEpsilon * collideEpsilon && iter-- != 0)
        {
            MinkowskiDifference.Support(supportA, supportB, orientationB, positionB, -v, out var w);
            Real vw = JVector.Dot(v, w.V);
            if (vw >= (Real)0.0)
                return false;
            if (!simplexSolver.AddVertex(w, out v)) return true;
            distSq = v.LengthSquared();
        }

        return true;
    }

    /// <summary>
    /// Performs an overlap test.
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationA">The orientation of shape A in world space.</param>
    /// <param name="orientationB">The orientation of shape B in world space.</param>
    /// <param name="positionA">The position of shape A in world space.</param>
    /// <param name="positionB">The position of shape B in world space.</param>
    /// <returns>Returns true of the shapes overlap, and false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Overlap<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // rotate into the reference frame of bodyA...
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // ...perform overlap test...
        return Overlap(supportA, supportB, orientation, position);
    }

    /// <summary>
    /// If MPR reports a penetration deeper than this value, we do not trust MPR to have found the global minimum
    /// and perform an EPA run.
    /// </summary>
    private const Real EpaPenetrationThreshold = (Real)0.02;

    /// <summary>
    /// Detects whether two convex shapes overlap and provides detailed collision information for overlapping shapes.
    /// Internally, this method utilizes the Minkowski Portal Refinement (MPR) to obtain the collision information.
    /// Although MPR is not exact, it delivers a strict upper bound for the penetration depth. If the upper bound exceeds
    /// a predefined threshold, the results are further refined using the Expanding Polytope Algorithm (EPA).
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationA">The orientation of shape A in world space.</param>
    /// <param name="orientationB">The orientation of shape B in world space.</param>
    /// <param name="positionA">The position of shape A in world space.</param>
    /// <param name="positionB">The position of shape B in world space.</param>
    /// <param name="pointA">The deepest point on shape A that is inside shape B.</param>
    /// <param name="pointB">The deepest point on shape B that is inside shape A.</param>
    /// <param name="normal">
    /// The normalized collision normal pointing from pointB to pointA. This normal remains defined even
    /// if pointA and pointB coincide, representing the direction in which the shapes must be separated by the minimal
    /// distance (determined by the penetration depth) to avoid overlap.
    /// </param>
    /// <param name="penetration">The penetration depth.</param>
    /// <param name="epaThreshold">Penetration depth threshold above which MPR results are refined with EPA.</param>
    /// <returns>Returns true if the shapes overlap (collide), and false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MprEpa<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real penetration,
        Real epaThreshold = EpaPenetrationThreshold)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        // rotate into the reference frame of bodyA..
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // ..perform collision detection..
        bool res = _solver.SolveMpr(supportA, supportB, orientation, position,
            epaThreshold, out pointA, out pointB, out normal, out penetration);

        // ..rotate back. This approach potentially saves some matrix-vector multiplication when the support
        // function is called multiple times.
        JVector.Transform(pointA, orientationA, out pointA);
        JVector.Add(pointA, positionA, out pointA);
        JVector.Transform(pointB, orientationA, out pointB);
        JVector.Add(pointB, positionA, out pointB);
        JVector.Transform(normal, orientationA, out normal);

        return res;
    }

    /// <summary>
    /// Detects whether two convex shapes overlap and provides detailed collision information for overlapping shapes.
    /// It assumes that support shape A is at position zero and not rotated.
    /// Internally, this method utilizes the Minkowski Portal Refinement (MPR) to obtain the collision information.
    /// Although MPR is not exact, it delivers a strict upper bound for the penetration depth. If the upper bound
    /// exceeds a predefined threshold, the results are further refined using the Expanding Polytope Algorithm (EPA).
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationB">The orientation of shape B.</param>
    /// <param name="positionB">The position of shape B.</param>
    /// <param name="pointA">The deepest point on shape A that is inside shape B.</param>
    /// <param name="pointB">The deepest point on shape B that is inside shape A.</param>
    /// <param name="normal">
    /// The normalized collision normal pointing from KpointB to pointA. This normal remains defined even
    /// if pointA and pointB coincide, representing the direction in which the shapes must be separated by the minimal
    /// distance (determined by the penetration depth) to avoid overlap.
    /// </param>
    /// <param name="penetration">The penetration depth.</param>
    /// <param name="epaThreshold">Penetration depth threshold above which MPR results are refined with EPA.</param>
    /// <returns>Returns true if the shapes overlap (collide), and false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MprEpa<Ta, Tb>(in Ta supportA, in Tb supportB,
        in JQuaternion orientationB, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real penetration,
        Real epaThreshold = EpaPenetrationThreshold)
        where Ta : ISupportMappable where Tb : ISupportMappable
    {
        return _solver.SolveMpr(supportA, supportB, orientationB, positionB, epaThreshold, out pointA, out pointB, out normal, out penetration);
    }
}
