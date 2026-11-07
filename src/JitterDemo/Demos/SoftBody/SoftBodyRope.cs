using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

namespace JitterDemo;

/// <summary>
/// A rope made of point-mass vertices connected by springs, with capsule segments for collision.
/// </summary>
public class SoftBodyRope : SoftBody
{
    /// <summary>
    /// Segments of the same rope closer than this (by index) never collide. Nearby segments
    /// overlap at bends, so they would push the rope apart. Scales with the segment density:
    /// segments only collide if they are at least two diameters apart along the rope.
    /// </summary>
    public int SelfCollisionIndexGap { get; }

    /// <summary>
    /// Default bias factor of the links between neighbouring vertices. Much higher than the
    /// constraint default (<see cref="Jitter2.Dynamics.Constraints.Constraint.DefaultLinearBias"/>, 0.2):
    /// a stretched link is pulled back to its rest length within a few steps, so the rope stays
    /// close to its length under load and does not thin out where other ropes press into it.
    /// </summary>
    public const float DefaultStretchBias = 0.8f;

    public float Radius { get; }

    /// <summary>
    /// Creates a rope through the given points, one vertex per point.
    /// </summary>
    /// <param name="world">The world to create the rope in.</param>
    /// <param name="points">The vertex positions, evenly spaced along the rope.</param>
    /// <param name="radius">The radius of the collision capsules.</param>
    /// <param name="inverseMass">The inverse mass of each vertex.</param>
    /// <param name="stretchSoftness">Softness of the links between neighbouring vertices.</param>
    /// <param name="stretchBias">Bias factor of those links; see <see cref="DefaultStretchBias"/>.</param>
    /// <param name="bendSoftness">Softness of springs across every second vertex, which resist bending
    /// (cable). Negative leaves them out (rope). They use the constraint default bias.</param>
    public SoftBodyRope(World world, IReadOnlyList<JVector> points, float radius, float inverseMass = 10,
        float stretchSoftness = 0.001f, float stretchBias = DefaultStretchBias, float bendSoftness = -1) : base(world)
    {
        Radius = radius;

        float segmentLength = (points[1] - points[0]).Length();
        SelfCollisionIndexGap = Math.Max(1, (int)MathF.Ceiling(4.0f * radius / segmentLength - 1e-3f));

        foreach (var point in points)
        {
            RigidBody body = World.CreateRigidBody();
            body.SetMassInertia(JSymmetricMatrix.Zero, inverseMass, true);
            body.Position = point;
            body.Damping = (0.002f, 0);
            Vertices.Add(body);
        }

        for (int i = 0; i < Vertices.Count - 1; i++)
        {
            AddLink(i, i + 1, stretchSoftness, stretchBias);
        }

        if (bendSoftness >= 0)
        {
            for (int i = 0; i < Vertices.Count - 2; i++)
            {
                AddLink(i, i + 2, bendSoftness, Jitter2.Dynamics.Constraints.Constraint.DefaultLinearBias);
            }
        }

        for (int i = 0; i < Vertices.Count - 1; i++)
        {
            AddShape(new SoftBodySegment(this, i, Vertices[i], Vertices[i + 1]) { Radius = radius });
        }
    }

    private void AddLink(int a, int b, float softness, float bias)
    {
        var spring = World.CreateConstraint<SpringConstraint>(Vertices[a], Vertices[b]);
        spring.Initialize(Vertices[a].Position, Vertices[b].Position);
        spring.Softness = softness;
        spring.Bias = bias;
        Springs.Add(spring);
    }

    /// <summary>
    /// Like <see cref="DynamicTreeCollisionFilter.Filter"/>, but lets segments of the same rope
    /// collide with each other unless they are within <see cref="SelfCollisionIndexGap"/>.
    /// </summary>
    public static bool SelfCollisionFilter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        if (proxyA is SoftBodySegment sa && proxyB is SoftBodySegment sb && sa.SoftBody == sb.SoftBody &&
            sa.SoftBody is SoftBodyRope)
        {
            return Math.Abs(sa.Index - sb.Index) > ((SoftBodyRope)sa.SoftBody).SelfCollisionIndexGap;
        }

        return DynamicTreeCollisionFilter.Filter(proxyA, proxyB);
    }
}
