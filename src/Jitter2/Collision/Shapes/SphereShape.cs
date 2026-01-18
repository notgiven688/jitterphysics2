/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a sphere.
/// </summary>
public class SphereShape : RigidBodyShape
{
    private Real radius;

    /// <summary>
    /// Gets or sets the radius of the sphere.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value"/> is less than or equal to zero.
    /// </exception>
    public Real Radius
    {
        get => radius;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(Radius));
            radius = value;
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SphereShape"/> class with an optional radius parameter.
    /// The default radius is 1.0 units.
    /// </summary>
    /// <param name="radius">The radius of the sphere. Defaults to (Real)1.0.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius"/> is less than or equal to zero.
    /// </exception>
    public SphereShape(Real radius = (Real)1.0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius, nameof(radius));

        this.radius = radius;
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        result = JVector.Normalize(direction);
        JVector.Multiply(result, radius, out result);
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        box.Min = new JVector(-radius);
        box.Max = new JVector(+radius);

        JVector.Add(box.Min, position, out box.Min);
        JVector.Add(box.Max, position, out box.Max);
    }

    public override bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda)
    {
        normal = JVector.Zero;
        lambda = (Real)0.0;

        Real disq = (Real)1.0 / direction.LengthSquared();
        Real p = JVector.Dot(direction, origin) * disq;
        Real d = p * p - (origin.LengthSquared() - radius * radius) * disq;

        if (d < (Real)0.0) return false;

        Real sqrtd = MathR.Sqrt(d);

        Real t0 = -p - sqrtd;
        Real t1 = -p + sqrtd;

        if (t0 >= (Real)0.0)
        {
            lambda = t0;
            JVector.Normalize(origin + t0 * direction, out normal);
            return true;
        }

        return t1 > (Real)0.0;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        mass = (Real)(4.0 / 3.0) * MathR.PI * radius * radius * radius;

        // (0,0,0) is the center of mass
        inertia = JMatrix.Identity;
        inertia.M11 = (Real)(2.0 / 5.0) * mass * radius * radius;
        inertia.M22 = (Real)(2.0 / 5.0) * mass * radius * radius;
        inertia.M33 = (Real)(2.0 / 5.0) * mass * radius * radius;

        com = JVector.Zero;
    }
}