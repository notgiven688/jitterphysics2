/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

public abstract class RigidBodyShape : Shape
{
    /// <summary>
    /// The instance of <see cref="RigidBody"/> to which this shape is attached.
    /// </summary>
    public RigidBody RigidBody { get; internal set; } = null!;

    public sealed override JVector Velocity => RigidBody?.Velocity ?? JVector.Zero;

    public sealed override void UpdateWorldBoundingBox(Real dt = (Real)0.0)
    {
        JBoundingBox box;

        if (RigidBody == null)
        {
            CalculateBoundingBox(JQuaternion.Identity, JVector.Zero, out box);
            WorldBoundingBox = box;
            return;
        }

        ref var data = ref RigidBody.Data;
        CalculateBoundingBox(data.Orientation, data.Position, out box);
        WorldBoundingBox = box;
        if (RigidBody.EnableSpeculativeContacts) SweptExpandBoundingBox(dt);
    }

    public virtual void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        ShapeHelper.CalculateBoundingBox(this, orientation, position, out box);
    }

    /// <summary>
    /// Calculates the mass and inertia of the shape. Can be overridden by child classes to improve
    /// performance or accuracy. The default implementation relies on an approximation of the shape
    /// constructed using the support map function.
    /// </summary>
    /// <remarks>
    /// The inertia tensor is computed relative to the coordinate system origin (0,0,0),
    /// not the center of mass.
    /// </remarks>
    [ReferenceFrame(ReferenceFrame.Local)]
    public virtual void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        ShapeHelper.CalculateMassInertia(this, out inertia, out com, out mass);
    }

    /// <summary>
    /// Performs a local ray cast against the shape, checking if a ray originating from a specified point
    /// and traveling in a specified direction intersects with the object. It does not take into account the
    /// transformation of the associated rigid body.
    /// </summary>
    /// <param name="origin">The starting point of the ray.</param>
    /// <param name="direction">
    /// The direction of the ray. This vector does not need to be normalized.
    /// </param>
    /// <param name="normal">
    /// When this method returns, contains the surface normal at the point of intersection, if an intersection occurs.
    /// </param>
    /// <param name="lambda">
    /// When this method returns, contains the scalar value representing the distance along the ray's direction vector
    /// from the <paramref name="origin"/> to the intersection point. The hit point can be calculated as:
    /// <c>origin + lambda * direction</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the ray intersects with the object; otherwise, <c>false</c>.
    /// </returns>
    [ReferenceFrame(ReferenceFrame.Local)]
    public virtual bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda)
    {
        return NarrowPhase.RayCast(this, origin, direction, out lambda, out normal);
    }

    [ReferenceFrame(ReferenceFrame.World)]
    public sealed override bool RayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda)
    {
        if (RigidBody == null)
        {
            return LocalRayCast(origin, direction, out normal, out lambda);
        }

        ref var data = ref RigidBody.Data;

        // rotate the ray into the reference frame of the body...
        JVector transformedDir = JVector.ConjugatedTransform(direction, data.Orientation);
        JVector transformedOrigin = JVector.ConjugatedTransform(origin - data.Position, data.Orientation);

        bool result = LocalRayCast(transformedOrigin, transformedDir, out normal, out lambda);

        // ...rotate back.
        JVector.Transform(normal, data.Orientation, out normal);

        return result;
    }
}