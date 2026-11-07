/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.SoftBodies;

/// <summary>
/// A capsule-shaped collision segment spanning two soft-body vertices. Chains of segments can be
/// used to build ropes and cables.
/// </summary>
/// <remarks>
/// Contacts are applied to the vertex closest to the contact point (see <see cref="GetClosest"/>),
/// so segments should be kept short, about twice the <see cref="Radius"/>.
/// </remarks>
public sealed class SoftBodySegment : SoftBodyShape
{
    private readonly RigidBody v1;
    private readonly RigidBody v2;

    private Real radius = (Real)0.05;

    /// <summary>
    /// Gets the first vertex of the segment.
    /// </summary>
    public RigidBody Vertex1 => v1;

    /// <summary>
    /// Gets the second vertex of the segment.
    /// </summary>
    public RigidBody Vertex2 => v2;

    /// <summary>
    /// Gets the position of this segment within its chain. Used by collision filters to
    /// skip neighbouring segments of the same soft body.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets or sets the radius of the capsule.
    /// </summary>
    public Real Radius
    {
        get => radius;
        set
        {
            radius = ArgumentCheck.NonNegative(value, nameof(Radius));
            UpdateWorldBoundingBox();
        }
    }

    /// <summary>
    /// Initializes a new segment between two vertices.
    /// </summary>
    /// <param name="body">The soft body owning this segment.</param>
    /// <param name="index">The position of the segment within its chain.</param>
    /// <param name="v1">The first vertex.</param>
    /// <param name="v2">The second vertex.</param>
    public SoftBodySegment(SoftBody body, int index, RigidBody v1, RigidBody v2)
    {
        this.v1 = v1;
        this.v2 = v2;

        Index = index;
        SoftBody = body;
        UpdateWorldBoundingBox();
    }

    /// <inheritdoc/>
    public override JVector Velocity => (Real)0.5 * (v1.Data.Velocity + v2.Data.Velocity);

    /// <inheritdoc/>
    public override RigidBody GetClosest(in JVector pos)
    {
        Real distance1Squared = (pos - v1.Position).LengthSquared();
        Real distance2Squared = (pos - v2.Position).LengthSquared();

        return distance1Squared < distance2Squared ? v1 : v2;
    }

    /// <summary>
    /// Gets or sets how many steps of motion the bounding box covers ahead of the segment, based on
    /// the vertex velocities. Broad-phase filters that create contacts before shapes touch
    /// (speculative contacts) can only find pairs within this reach. Default is 1.5.
    /// </summary>
    public Real SweepFactor { get; set; } = (Real)1.5;

    /// <summary>
    /// Gets or sets an extra distance by which the bounding box is enlarged, so that broad-phase
    /// filters also see pairs that are this close but not yet touching. Default is zero.
    /// </summary>
    public Real ContactMargin
    {
        get => contactMargin;
        set => contactMargin = ArgumentCheck.NonNegative(value, nameof(ContactMargin));
    }

    private Real contactMargin;

    /// <inheritdoc/>
    public override void UpdateWorldBoundingBox(Real dt = (Real)0.0)
    {
        Real extraMargin = MathR.Max(radius, (Real)0.01) + contactMargin;

        JBoundingBox box = JBoundingBox.SmallBox;

        JBoundingBox.AddPointInPlace(ref box, v1.Position);
        JBoundingBox.AddPointInPlace(ref box, v2.Position);

        // Cover the motion of the next step(s), so that approaching pairs can be found before they touch.
        if (dt > (Real)0.0)
        {
            Real sweep = dt * SweepFactor;
            JBoundingBox.AddPointInPlace(ref box, v1.Position + v1.Data.Velocity * sweep);
            JBoundingBox.AddPointInPlace(ref box, v2.Position + v2.Data.Velocity * sweep);
        }

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

        result = JVector.Dot(a, direction) >= JVector.Dot(b, direction) ? a : b;
        result += JVector.Normalize(direction) * radius;
    }

    /// <inheritdoc/>
    public override void GetCenter(out JVector point)
    {
        point = (Real)0.5 * (v1.Position + v2.Position);
    }
}
