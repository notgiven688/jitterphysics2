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

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Jitter2.LinearMath;
using Vertex = Jitter2.Collision.MinkowskiDifference.Vertex;

namespace Jitter2.Collision;

/// <summary>
/// Provides efficient and accurate collision detection algorithms for general convex objects
/// implicitly defined by a support function, see <see cref="ISupportMappable"/>.
/// </summary>
public static class NarrowPhase
{
    private const Real NumericEpsilon = (Real)1e-16;

    private struct MPREPASolver
    {
        private ConvexPolytope convexPolytope;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SolveMPREPA<TA,TB>(in TA supportA, in TB supportB, in JQuaternion orientationB, in JVector positionB,
            ref JVector point1, ref JVector point2, ref JVector normal, ref Real penetration)
            where TA : ISupportMappable where TB : ISupportMappable
        {
            const Real CollideEpsilon = (Real)1e-5;
            const int MaxIter = 85;

            convexPolytope.InitTetrahedron();

            int iter = 0;

            Unsafe.SkipInit(out ConvexPolytope.Triangle ctri);

            while (++iter < MaxIter)
            {
                ctri = convexPolytope.GetClosestTriangle();

                JVector searchDir = ctri.ClosestToOrigin;
                Real searchDirSq = ctri.ClosestToOriginSq;

                if (ctri.ClosestToOriginSq < NumericEpsilon)
                {
                    searchDir = ctri.Normal;
                    searchDirSq = ctri.NormalSq;
                }

                MinkowskiDifference.Support(supportA, supportB, orientationB, positionB,
                    searchDir, out Vertex vertex);

                // compare with the corresponding code in SolveGJKEPA.
                Real deltaDist = JVector.Dot(ctri.ClosestToOrigin - vertex.V, searchDir);

                if (deltaDist * deltaDist <= CollideEpsilon * CollideEpsilon * searchDirSq)
                {
                    goto converged;
                }

                if (!convexPolytope.AddVertex(vertex))
                {
                    goto converged;
                }
            }

            Logger.Warning("{0}: EPA, Could not converge within {1} iterations.", nameof(NarrowPhase), MaxIter);

            return false;

            converged:

            convexPolytope.CalculatePoints(ctri, out point1, out point2);

            normal = ctri.Normal * ((Real)1.0 / MathR.Sqrt(ctri.NormalSq));
            penetration = MathR.Sqrt(ctri.ClosestToOriginSq);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SolveMPR<TA,TB>(in TA supportA, in TB supportB, in JQuaternion orientationB,
            in JVector positionB, Real epaThreshold,
            out JVector pointA, out JVector pointB, out JVector normal, out Real penetration)
            where TA : ISupportMappable where TB : ISupportMappable
        {
            /*
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
            const Real CollideEpsilon = (Real)1e-5;
            const int MaxIter = 34;

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

            // Determine whether origin is on + or - side of plane (v1.V,v0.V,v2.V)
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
                if (phase1 > MaxIter) return false;

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
            // We are now inside of a wedge...
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

                // If the boundary is thin enough or the origin is outside the support plane for the newly discovered
                // vertex, then we can terminate
                if (delta * delta <= CollideEpsilon * CollideEpsilon * normalSq || penetration <= (Real)0.0 ||
                    phase2 > MaxIter)
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
                            if (SolveMPREPA(supportA, supportB, orientationB, positionB,
                                    ref pointA, ref pointB, ref normal, ref penetration)) return true;
                        }

                        normal *= invnormal;

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

                // Compute the tetrahedron dividing face (v4.V,v0.V,v3.V)
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
        public bool Collision<TA,TB>(in TA supportA, in TB supportB, in JQuaternion orientationB, in JVector positionB,
            out JVector point1, out JVector point2, out JVector normal, out Real penetration)
            where TA : ISupportMappable where TB : ISupportMappable
        {
            const Real CollideEpsilon = (Real)1e-4;
            const int MaxIter = 85;

            MinkowskiDifference.GetCenter(supportA, supportB,orientationB, positionB, out Vertex centerVertex);
            JVector center = centerVertex.V;

            convexPolytope.InitHeap();
            convexPolytope.InitTetrahedron(center);

            int iter = 0;

            Unsafe.SkipInit(out ConvexPolytope.Triangle ctri);

            while (++iter < MaxIter)
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

                if (deltaDist * deltaDist <= CollideEpsilon * CollideEpsilon * searchDirSq)
                {
                    goto converged;
                }

                if (!convexPolytope.AddVertex(vertex))
                {
                    goto converged;
                }
            }

            point1 = point2 = normal = JVector.Zero;
            penetration = (Real)0.0;

            Logger.Warning("{0}: EPA, Could not converge within {1} iterations.\"", nameof(NarrowPhase), MaxIter);

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
    [ThreadStatic] private static MPREPASolver solver;

    /// <summary>
    /// Check if a point is inside a shape.
    /// </summary>
    /// <param name="support">Support map representing the shape.</param>
    /// <param name="point">Point to check.</param>
    /// <returns>Returns true if the point is contained within the shape, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool PointTest<TA>(in TA support, in JVector point) where TA : ISupportMappable
    {
        const Real CollideEpsilon = (Real)1e-4;
        const int MaxIter = 34;

        JVector x = point;

        support.GetCenter(out var center);
        JVector v = x - center;

        Unsafe.SkipInit(out SimplexSolver simplexSolver);
        simplexSolver.Reset();

        int maxIter = MaxIter;

        Real distSq = v.LengthSquared();

        while (distSq > CollideEpsilon * CollideEpsilon && maxIter-- != 0)
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
    public static bool PointTest<TA>(in TA support, in JMatrix orientation,
        in JVector position, in JVector point) where TA : ISupportMappable
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
    /// <param name="lambda">Specifies the hit point of the ray, calculated as 'origin + lambda * direction'.</param>
    /// <param name="normal">
    /// The normalized normal vector perpendicular to the surface, pointing outwards. If the ray does not
    /// hit, this parameter will be zero.
    /// </param>
    /// <returns>Returns true if the ray intersects with the shape; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RayCast<TA>(in TA support, in JQuaternion orientation,
        in JVector position, in JVector origin, in JVector direction, out Real lambda, out JVector normal)
        where TA : ISupportMappable
    {
        // rotate the ray into the reference frame of bodyA..
        JVector tdirection = JVector.ConjugatedTransform(direction, orientation);
        JVector torigin = JVector.ConjugatedTransform(origin - position, orientation);

        bool result = RayCast(support, torigin, tdirection, out lambda, out normal);

        // ..rotate back.
        JVector.Transform(normal, orientation, out normal);

        return result;
    }

    /// <summary>
    /// Performs a ray cast against a shape.
    /// </summary>
    /// <param name="support">The support function of the shape.</param>
    /// <param name="origin">The origin of the ray.</param>
    /// <param name="direction">The direction of the ray; normalization is not necessary.</param>
    /// <param name="lambda">Specifies the hit point of the ray, calculated as 'origin + lambda * direction'.</param>
    /// <param name="normal">
    /// The normalized normal vector perpendicular to the surface, pointing outwards. If the ray does not
    /// hit, this parameter will be zero.
    /// </param>
    /// <returns>Returns true if the ray intersects with the shape; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RayCast<TA>(in TA support, in JVector origin, in JVector direction,
        out Real lambda, out JVector normal) where TA : ISupportMappable
    {
        const Real CollideEpsilon = (Real)1e-4;
        const int MaxIter = 34;

        normal = JVector.Zero;
        lambda = (Real)0.0;

        JVector r = direction;
        JVector x = origin;

        support.GetCenter(out var center);
        JVector v = x - center;

        Unsafe.SkipInit(out SimplexSolver simplexSolver);
        simplexSolver.Reset();

        int maxIter = MaxIter;

        Real distSq = v.LengthSquared();

        while (distSq > CollideEpsilon * CollideEpsilon && maxIter-- != 0)
        {
            support.SupportMap(v, out JVector p);

            JVector.Subtract(x, p, out JVector w);

            Real VdotW = JVector.Dot(v, w);

            if (VdotW > (Real)0.0)
            {
                Real VdotR = JVector.Dot(v, r);

                if (VdotR >= -NumericEpsilon)
                {
                    lambda = Real.PositiveInfinity;
                    return false;
                }

                lambda -= VdotW / VdotR;

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

        Real nlen2 = normal.LengthSquared();

        if (nlen2 > NumericEpsilon)
        {
            normal *= (Real)1.0 / MathR.Sqrt(nlen2);
        }

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
    /// The normalized collision normal pointing from pointB to pointA. This normal remains defined even
    /// if pointA and pointB coincide. It denotes the direction in which the shapes should be moved by the minimum distance
    /// (defined by the penetration depth) to either separate them in the overlapping case or bring them into contact in
    /// the separated case.
    /// </param>
    /// <param name="penetration">The penetration depth.</param>
    /// <returns>
    /// Returns true if the algorithm completes successfully, false otherwise. In case of algorithm convergence
    /// failure, collision information reverts to the type's default values.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Collision<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationB, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real penetration)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // ..perform collision detection..
        bool success = solver.Collision(supportA, supportB, orientationB, positionB,
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
    /// The normalized collision normal pointing from pointB to pointA. This normal remains defined even
    /// if pointA and pointB coincide. It denotes the direction in which the shapes should be moved by the minimum distance
    /// (defined by the penetration depth) to either separate them in the overlapping case or bring them into contact in
    /// the separated case.
    /// </param>
    /// <param name="penetration">The penetration depth.</param>
    /// <returns>
    /// Returns true if the algorithm completes successfully, false otherwise. In case of algorithm convergence
    /// failure, collision information reverts to the type's default values.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Collision<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real penetration)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // rotate into the reference frame of bodyA..
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // ..perform collision detection..
        bool success = solver.Collision(supportA, supportB, orientation, position,
            out pointA, out pointB, out normal, out penetration);

        // ..rotate back. this hopefully saves some matrix vector multiplication
        // when calling the support function multiple times.
        JVector.Transform(pointA, orientationA, out pointA);
        JVector.Add(pointA, positionA, out pointA);
        JVector.Transform(pointB, orientationA, out pointB);
        JVector.Add(pointB, positionA, out pointB);
        JVector.Transform(normal, orientationA, out normal);

        return success;
    }

    /// <summary>
    /// Provides the distance and closest points for non overlapping shapes. It
    /// assumes that support shape A is located at position zero and not rotated.
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationB">The orientation of shape B in world space.</param>
    /// <param name="positionB">The position of shape B in world space.</param>
    /// <param name="pointA">Closest point on shape A. Not well-defined for the overlapping case.</param>
    /// <param name="pointB">Closest point on shape B. Not well-defined for the overlapping case.</param>
    /// <param name="distance">The distance between the separating shapes. Zero if shapes overlap.</param>
    /// <returns>Returns true if the shapes do not overlap and distance information
    /// can be provided.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Distance<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationB, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // ..perform overlap test..
        const Real CollideEpsilon = (Real)1e-4;
        const int MaxIter = 34;

        Unsafe.SkipInit(out SimplexSolverAB simplexSolver);
        simplexSolver.Reset();

        int maxIter = MaxIter;

        MinkowskiDifference.GetCenter(supportA, supportB, orientationB, positionB, out Vertex center);

        JVector v = center.V;
        Real distSq = v.LengthSquared();

        while (maxIter-- != 0)
        {
            if (distSq < CollideEpsilon * CollideEpsilon) goto ret_false;

            MinkowskiDifference.Support(supportA, supportB, orientationB, positionB, -v, out var w);

            Real deltaDist = JVector.Dot(v - w.V, v);
            if (deltaDist * deltaDist < CollideEpsilon * CollideEpsilon * distSq)
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
    /// Provides the distance and closest points for non overlapping shapes.
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationA">The orientation of shape A in world space.</param>
    /// <param name="orientationB">The orientation of shape B in world space.</param>
    /// <param name="positionA">The position of shape A in world space.</param>
    /// <param name="positionB">The position of shape B in world space.</param>
    /// <param name="pointA">Closest point on shape A. Not well-defined for the overlapping case.</param>
    /// <param name="pointB">Closest point on shape B. Not well-defined for the overlapping case.</param>
    /// <param name="distance">The distance between the separating shapes. Zero if shapes overlap.</param>
    /// <returns>Returns true if the shapes do not overlap and distance information
    /// can be provided.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Distance<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // rotate into the reference frame of bodyA..
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // ..perform distance test..
        bool result = Distance(supportA, supportB, orientation, position, out pointA, out pointB, out normal, out distance);

        // ..rotate back. This approach potentially saves some matrix-vector multiplication when
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
    public static bool Overlap<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationB, in JVector positionB) where TA : ISupportMappable where TB : ISupportMappable
    {
        // ..perform overlap test..
        const Real CollideEpsilon = (Real)1e-4;
        const int MaxIter = 34;

        Unsafe.SkipInit(out SimplexSolverAB simplexSolver);
        simplexSolver.Reset();

        int maxIter = MaxIter;

        MinkowskiDifference.GetCenter(supportA, supportB, orientationB, positionB, out Vertex center);

        JVector v = center.V;
        Real distSq = v.LengthSquared();

        while (distSq > CollideEpsilon * CollideEpsilon && maxIter-- != 0)
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
    public static bool Overlap<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // rotate into the reference frame of bodyA..
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // ..perform overlap test..
        return Overlap(supportA, supportB, orientation, position);
    }

    /// <summary>
    /// If MPR reports a penetration deeper than this value we do not trust MPR to have found the global minimum
    /// and perform an EPA run.
    /// </summary>
    private const Real EPAPenetrationThreshold = (Real)0.02;

    /// <summary>
    /// Detects whether two convex shapes overlap and provides detailed collision information for overlapping shapes.
    /// Internally, this method utilizes the Minkowski Portal Refinement (MPR) to obtain the collision information.
    /// Although MPR is not exact, it delivers a strict upper bound for the penetration depth. If the upper bound surpasses
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
    /// <param name="epaThreshold">The threshold parameter.</param>
    /// <returns>Returns true if the shapes overlap (collide), and false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MPREPA<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real penetration,
        Real epaThreshold = EPAPenetrationThreshold)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // rotate into the reference frame of bodyA..
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // ..perform collision detection..
        bool res = solver.SolveMPR(supportA, supportB, orientation, position,
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
    /// surpasses a predefined threshold, the results are further refined using the Expanding Polytope Algorithm (EPA).
    /// </summary>
    /// <param name="supportA">The support function of shape A.</param>
    /// <param name="supportB">The support function of shape B.</param>
    /// <param name="orientationB">The orientation of shape B.</param>
    /// <param name="positionB">The position of shape B.</param>
    /// <param name="pointA">The deepest point on shape A that is inside shape B.</param>
    /// <param name="pointB">The deepest point on shape B that is inside shape A.</param>
    /// <param name="normal">
    /// The normalized collision normal pointing from pointB to pointA. This normal remains d
    /// if pointA and pointB coincide, representing the direction in which the shapes must be
    /// distance (determined by the penetration depth) to avoid overlap.
    /// </param>
    /// <param name="penetration">The penetration depth.</param>
    /// <param name="epaThreshold">The threshold parameter.</param>
    /// <returns>Returns true if the shapes overlap (collide), and false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MPREPA<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationB, in JVector positionB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real penetration,
        Real epaThreshold = EPAPenetrationThreshold)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // ..perform collision detection..
        return solver.SolveMPR(supportA, supportB, orientationB, positionB , epaThreshold, out pointA, out pointB, out normal, out penetration);
    }

    /// <summary>
    /// Calculates the time of impact (TOI) and the collision points in world space for two shapes with linear
    /// velocities <paramref name="sweepA"/> and <paramref name="sweepB"/> and angular velocities
    /// <paramref name="sweepAngularA"/> and <paramref name="sweepAngularB"/>.
    /// </summary>
    /// <param name="pointA">Collision point on shape A in world space at t = 0, where collision will occur.</param>
    /// <param name="pointB">Collision point on shape B in world space at t = 0, where collision will occur.</param>
    /// <param name="normal">Collision normal in world space at time of impact (points from A to B).</param>
    /// <param name="lambda">Time of impact. <c>Infinity</c> if no hit is detected. Zero if shapes overlap.</param>
    /// <returns>True if the shapes will hit or already overlap, false otherwise.</returns>
    /// <remarks>
    /// Uses conservative advancement for continuous collision detection. May fail to converge to the correct TOI
    /// and collision points in certain edge cases due to limitations in linear motion approximation and
    /// distance gradient estimation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Sweep<TA, TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        in JVector sweepA, in JVector sweepB,
        in JVector sweepAngularA, in JVector sweepAngularB,
        in Real extentA, in Real extentB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        const Real CollideEpsilon = (Real)1e-4;
        const int MaxIter = 64;

        Real maxAngularSpeed = extentA * sweepAngularA.Length() + extentB * sweepAngularB.Length();
        Real combinedRadius = extentA + extentB;

        JVector posA = positionA;
        JVector posB = positionB;

        JQuaternion oriA = orientationA;
        JQuaternion oriB = orientationB;

        lambda = 0;

        int iter = 0;

        JQuaternion sweepAngularDeltaA;
        JQuaternion sweepAngularDeltaB;

        Distance(supportA, supportB, oriA, oriB, posA, posB, out pointA, out pointB, out normal, out var distance);

        if (distance < CollideEpsilon)
        {
            // We already overlap (or nearly overlap) at time 0.
            // In this case the Sweep function should return true. normal and lambda are set to zero.
            return true;
        }

        while (true)
        {
            Real sweepLinearProj = JVector.Dot(normal, sweepA - sweepB);
            Real sweepLen = sweepLinearProj + maxAngularSpeed;

            if(sweepLen < NumericEpsilon || (sweepLinearProj < 0 && distance > combinedRadius))
            {
                normal = JVector.Zero;
                lambda = Real.PositiveInfinity;
                return false;
            }

            Real tmpLambda = distance / sweepLen;

            lambda += tmpLambda;

            Debug.Assert(!Real.IsNaN(lambda));

            sweepAngularDeltaA = MathHelper.RotationQuaternion(sweepAngularA, lambda);
            sweepAngularDeltaB = MathHelper.RotationQuaternion(sweepAngularB, lambda);

            oriA = sweepAngularDeltaA * orientationA;
            oriB = sweepAngularDeltaB * orientationB;

            posA = positionA + sweepA * lambda;
            posB = positionB + sweepB * lambda;

            if (iter++ > MaxIter) break;

            bool res = Distance(supportA, supportB, oriA, oriB, posA, posB, out pointA, out pointB, out JVector nn, out distance);

            // We are a bit in a pickle here.
            // If the advanced shapes are slightly overlapping (Distance returns false; this can either happen if the
            // simplex solver encompasses the origin or the closest point on the simplex is close enough to the origin),
            // we have valid posA and posB information, but the normal is not well-defined. So we keep the old normal.
            if(res) normal = nn;

            if (distance < CollideEpsilon)
                break;
        }

        // Hit point found at in world space at time lambda. Transform back to time 0.
        var linearTransformationA = sweepA * lambda;
        var linearTransformationB = sweepB * lambda;

        var deltaA = pointA - posA;
        var deltaB = pointB - posB;

        pointA -= linearTransformationA + (deltaA - JVector.ConjugatedTransform(deltaA, sweepAngularDeltaA));
        pointB -= linearTransformationB + (deltaB - JVector.ConjugatedTransform(deltaB, sweepAngularDeltaB));

        return true;
    }

    /// <summary>
    /// Calculates the time of impact and the collision points in world space for two shapes with velocities
    /// sweepA and sweepB.
    /// </summary>
    /// <param name="pointA">Collision point on shapeA in world space at t = 0, where collision will occur.</param>
    /// <param name="pointB">Collision point on shapeB in world space at t = 0, where collision will occur.</param>
    /// <param name="lambda">Time of impact. Infinity if no hit is detected. Zero if shapes overlap.</param>
    /// <returns>True if the shapes will hit or already overlap, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Sweep<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationA, in JQuaternion orientationB,
        in JVector positionA, in JVector positionB,
        in JVector sweepA, in JVector sweepB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // rotate into the reference frame of bodyA..
        JQuaternion.ConjugateMultiply(orientationA, orientationB, out JQuaternion orientation);
        JVector.Subtract(positionB, positionA, out JVector position);
        JVector.ConjugatedTransform(position, orientationA, out position);

        // we also transform the relative velocities
        JVector sweep = sweepB - sweepA;
        JVector.ConjugatedTransform(sweep, orientationA, out sweep);

        // ..perform toi calculation
        bool res = Sweep(supportA, supportB, orientation, position, sweep,
            out pointA, out pointB, out normal, out lambda);

        if (!res) return false;

        // ..rotate back. This approach potentially saves some matrix-vector multiplication when the support function is
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
    /// Perform a sweep test where support shape A is at position zero, not rotated and has no sweep
    /// direction.
    /// </summary>
    /// <param name="pointA">Collision point on shapeA in world space at t = 0, where collision will occur.</param>
    /// <param name="pointB">Collision point on shapeB in world space at t = 0, where collision will occur.</param>
    /// <param name="lambda">Time of impact. Infinity if no hit is detected. Zero if shapes overlap.</param>
    /// <returns>True if the shapes will hit or already overlap, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Sweep<TA,TB>(in TA supportA, in TB supportB,
        in JQuaternion orientationB, in JVector positionB, in JVector sweepB,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // ..perform toi calculation
        const Real CollideEpsilon = (Real)1e-4;
        const int MaxIter = 34;

        Unsafe.SkipInit(out SimplexSolverAB simplexSolver);
        simplexSolver.Reset();

        MinkowskiDifference.GetCenter(supportA, supportB, orientationB, positionB, out var center);;

        JVector posB = positionB;

        lambda = (Real)0.0;

        pointA = pointB = JVector.Zero;

        JVector r = sweepB;
        JVector v = -center.V;

        normal = JVector.Zero;

        int iter = MaxIter;

        Real distSq = Real.MaxValue;

        while ((distSq > CollideEpsilon * CollideEpsilon) && (iter-- != 0))
        {
            MinkowskiDifference.Support(supportA, supportB, orientationB, posB, v, out Vertex vertex);
            var w = vertex.V;

            Real VdotW = -JVector.Dot(v, w);

            if (VdotW > (Real)0.0)
            {
                Real VdotR = JVector.Dot(v, r);

                if (VdotR >= -(Real)1e-12)
                {
                    lambda = Real.PositiveInfinity;
                    return false;
                }

                lambda -= VdotW / VdotR;

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

        Real nlen2 = normal.LengthSquared();

        if (nlen2 > NumericEpsilon)
        {
            normal *= (Real)1.0 / MathR.Sqrt(nlen2);
        }

        return true;
    }
}