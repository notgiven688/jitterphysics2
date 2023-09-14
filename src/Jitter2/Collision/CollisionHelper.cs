/*
 * Copyright (c) 2009-2023 Thorben Linneweber and others
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
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Collision helper functions.
/// </summary>
public static class CollisionHelper
{
    /// <summary>
    /// Checks if a point projected onto the plane of a triangle is within
    /// the triangle.
    /// </summary>
    public static bool ProjectedPointOnTriangle(in JVector a, in JVector b, in JVector c,
        in JVector point)
    {
        JVector u = a - b;
        JVector v = a - c;

        JVector normal = u % v;
        float t = normal.LengthSquared();

        JVector at = a - point;

        JVector.Cross(u, at, out JVector tmp);
        float gamma = JVector.Dot(tmp, normal) / t;
        JVector.Cross(at, v, out tmp);
        float beta = JVector.Dot(tmp, normal) / t;
        float alpha = 1.0f - gamma - beta;

        return alpha > 0.0f && beta > 0.0f && gamma > 0.0f;
    }

    /// <summary>
    /// Ray triangle intersection.
    /// </summary>
    public static bool RayTriangle(in JVector a, in JVector b, in JVector c,
        in JVector rayStart, in JVector rayDir,
        out float lambda, out JVector normal)
    {
        JVector u = b - a;
        JVector v = c - a;

        normal = v % u;
        float t = normal.LengthSquared();

        // triangle is expected to span an area
        Debug.Assert(t > 1e-06f);

        float denom = JVector.Dot(rayDir, normal);

        if (Math.Abs(denom) < 1e-06f)
        {
            // triangle and ray are parallel
            lambda = 0;
            normal = JVector.Zero;
            return false;
        }

        lambda = JVector.Dot(a - rayStart, normal);
        if (lambda > 0.0f) return false;
        lambda /= denom;

        // point where the ray intersects the plane of the triangle.
        JVector hitPoint = rayStart + lambda * rayDir;
        JVector at = a - hitPoint;

        JVector.Cross(u, at, out JVector tmp);
        float gamma = JVector.Dot(tmp, normal) / t;
        JVector.Cross(at, v, out tmp);
        float beta = JVector.Dot(tmp, normal) / t;
        float alpha = 1.0f - gamma - beta;

        return alpha > 0 && beta > 0 && gamma > 0;
    }
}