/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.SoftBodies;

/// <summary>
/// Represents a triangular shape in a soft body simulation.
/// </summary>
public sealed class SoftBodyTriangle : SoftBodyShape
{
    private readonly RigidBody v1;
    private readonly RigidBody v2;
    private readonly RigidBody v3;

    /// <summary>
    /// Gets the first vertex (rigid body) of the triangle.
    /// </summary>
    public RigidBody Vertex1 => v1;

    /// <summary>
    /// Gets the second vertex (rigid body) of the triangle.
    /// </summary>
    public RigidBody Vertex2 => v2;

    /// <summary>
    /// Gets the third vertex (rigid body) of the triangle.
    /// </summary>
    public RigidBody Vertex3 => v3;

    private Real halfThickness = (Real)0.05;

    /// <summary>
    /// Gets or sets the thickness of the triangle.
    /// </summary>
    public Real Thickness
    {
        get => halfThickness * (Real)2.0;
        set => halfThickness = value * (Real)0.5;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftBodyTriangle"/> class.
    /// </summary>
    /// <param name="body">The soft body this shape belongs to.</param>
    /// <param name="v1">The first vertex.</param>
    /// <param name="v2">The second vertex.</param>
    /// <param name="v3">The third vertex.</param>
    public SoftBodyTriangle(SoftBody body, RigidBody v1, RigidBody v2, RigidBody v3)
    {
        this.v1 = v1;
        this.v2 = v2;
        this.v3 = v3;

        SoftBody = body;
        UpdateWorldBoundingBox();
    }

    /// <inheritdoc/>
    public override JVector Velocity => (Real)(1.0 / 3.0) * (v1.Data.Velocity + v2.Data.Velocity + v3.Data.Velocity);

    /// <inheritdoc/>
    public override RigidBody GetClosest(in JVector pos)
    {
        Real len1 = (pos - v1.Position).LengthSquared();
        Real len2 = (pos - v2.Position).LengthSquared();
        Real len3 = (pos - v3.Position).LengthSquared();

        return (len1 < len2 && len1 < len3) ? v1 :
            (len2 < len3) ? v2 : v3;
    }

    /// <inheritdoc/>
    public override void UpdateWorldBoundingBox(Real dt = (Real)0.0)
    {
        Real extraMargin = MathR.Max(halfThickness, (Real)0.01);

        JBoundingBox box = JBoundingBox.SmallBox;

        JBoundingBox.AddPointInPlace(ref box, Vertex1.Position);
        JBoundingBox.AddPointInPlace(ref box, Vertex2.Position);
        JBoundingBox.AddPointInPlace(ref box, Vertex3.Position);

        // prevent a degenerate bounding box
        JVector extra = new(extraMargin);
        box.Min -= extra;
        box.Max += extra;

        WorldBoundingBox = box;
    }

    /// <inheritdoc/>
    public override void SupportMap(in JVector direction, out JVector result)
    {
        JVector a = v1.Position;
        JVector b = v2.Position;
        JVector c = v3.Position;

        Real min = JVector.Dot(a, direction);
        Real dot = JVector.Dot(b, direction);

        result = a;

        if (dot > min)
        {
            min = dot;
            result = b;
        }

        dot = JVector.Dot(c, direction);

        if (dot > min)
        {
            result = c;
        }

        result += JVector.Normalize(direction) * halfThickness;
    }

    /// <inheritdoc/>
    public override void GetCenter(out JVector point)
    {
        point = ((Real)(1.0 / 3.0)) * (Vertex1.Position + Vertex2.Position + Vertex3.Position);
    }
}