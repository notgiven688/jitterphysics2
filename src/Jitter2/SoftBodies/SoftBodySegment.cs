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
public class SoftBodySegment : SoftBodyShape
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
    /// <param name="v1">The first vertex.</param>
    /// <param name="v2">The second vertex.</param>
    public SoftBodySegment(SoftBody body, RigidBody v1, RigidBody v2)
    {
        this.v1 = v1;
        this.v2 = v2;

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

    /// <inheritdoc/>
    public override void UpdateWorldBoundingBox(Real dt = (Real)0.0)
    {
        Real extraMargin = MathR.Max(radius, (Real)0.01);

        JBoundingBox box = JBoundingBox.SmallBox;

        JBoundingBox.AddPointInPlace(ref box, v1.Position);
        JBoundingBox.AddPointInPlace(ref box, v2.Position);

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
