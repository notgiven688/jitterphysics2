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
using Jitter2.LinearMath;

#if USE_DOUBLE_PRECISION
using Real = System.Double;
using MathR = System.Math;
#else
using Real = System.Single;
using MathR = System.MathF;
#endif

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a shape in the form of a capsule.
/// </summary>
public class CapsuleShape : RigidBodyShape
{
    private Real radius;
    private Real halfLength;

    /// <summary>
    /// Gets or sets the radius of the capsule.
    /// </summary>
    public Real Radius
    {
        get => radius;
        set
        {
            radius = value;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Gets or sets the length of the cylindrical part of the capsule, excluding the half-spheres on both ends.
    /// </summary>
    public Real Length
    {
        get => 2.0f * halfLength;
        set
        {
            halfLength = value / 2.0f;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Initializes a new instance of the CapsuleShape class with the specified radius and length. The symmetry axis of the capsule is aligned along the Y-axis.
    /// </summary>
    /// <param name="radius">The radius of the capsule.</param>
    /// <param name="length">The length of the cylindrical part of the capsule, excluding the half-spheres at both ends.</param>
    public CapsuleShape(Real radius = 0.5f, Real length = 1.0f)
    {
        this.radius = radius;
        halfLength = 0.5f * length;
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        // capsule = segment + sphere

        // sphere
        JVector.Normalize(direction, out JVector ndir);
        result = ndir * radius;

        // two endpoint of the segment are
        // p_1 = (0, +length/2, 0)
        // p_2 = (0, -length/2, 0)

        // we have to calculate the dot-product with the direction
        // vector to decide whether p_1 or p_2 is the correct support point
        result.Y += MathR.Sign(direction.Y) * halfLength;
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        JVector delta = halfLength * orientation.GetBasisY();

        box.Max.X = +radius + MathR.Abs(delta.X);
        box.Max.Y = +radius + MathR.Abs(delta.Y);
        box.Max.Z = +radius + MathR.Abs(delta.Z);

        box.Min = -box.Max;

        box.Min += position;
        box.Max += position;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        Real length = 2.0f * halfLength;

        Real massSphere = 4.0f / 3.0f * MathR.PI * radius * radius * radius;
        Real massCylinder = MathR.PI * radius * radius * length;

        inertia = JMatrix.Identity;

        inertia.M11 = massCylinder * (1.0f / 12.0f * length * length + 1.0f / 4.0f * radius * radius) + massSphere *
            (2.0f / 5.0f * radius * radius + 1.0f / 4.0f * length * length + 3.0f / 8.0f * length * radius);
        inertia.M22 = 1.0f / 2.0f * massCylinder * radius * radius + 2.0f / 5.0f * massSphere * radius * radius;
        inertia.M33 = massCylinder * (1.0f / 12.0f * length * length + 1.0f / 4.0f * radius * radius) + massSphere *
            (2.0f / 5.0f * radius * radius + 1.0f / 4.0f * length * length + 3.0f / 8.0f * length * radius);

        mass = massCylinder + massSphere;
        com = JVector.Zero;
    }
}