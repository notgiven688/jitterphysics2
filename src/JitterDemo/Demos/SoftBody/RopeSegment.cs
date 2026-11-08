using System;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

namespace JitterDemo;

/// <summary>
/// A <see cref="SoftBodySegment"/> whose bounding box reaches ahead of its motion, so that
/// <see cref="RopeCollisionFilter"/> can find pairs before they touch.
/// </summary>
public class RopeSegment : SoftBodySegment
{
    /// <summary>
    /// Gets or sets how many steps of motion the bounding box covers ahead of the segment, based on
    /// the vertex velocities. Default is 1.5, matching <see cref="RopeCollisionFilter.SpeculativeFactor"/>.
    /// </summary>
    public float SweepFactor { get; set; } = 1.5f;

    /// <summary>
    /// Gets or sets the distance within which <see cref="RopeCollisionFilter"/> always creates a contact
    /// for this segment. The bounding box is enlarged by the same amount. Default is zero.
    /// </summary>
    public float ContactMargin { get; set; }

    public RopeSegment(SoftBody body, int index, RigidBody v1, RigidBody v2) : base(body, index, v1, v2)
    {
    }

    /// <inheritdoc/>
    public override void UpdateWorldBoundingBox(float dt = 0.0f)
    {
        JBoundingBox box = JBoundingBox.SmallBox;

        JBoundingBox.AddPointInPlace(ref box, Vertex1.Position);
        JBoundingBox.AddPointInPlace(ref box, Vertex2.Position);

        if (dt > 0.0f)
        {
            float sweep = dt * SweepFactor;
            JBoundingBox.AddPointInPlace(ref box, Vertex1.Position + Vertex1.Velocity * sweep);
            JBoundingBox.AddPointInPlace(ref box, Vertex2.Position + Vertex2.Velocity * sweep);
        }

        JVector extra = new(MathF.Max(Radius, 0.01f) + ContactMargin);
        box.Min -= extra;
        box.Max += extra;

        WorldBoundingBox = box;
    }
}
