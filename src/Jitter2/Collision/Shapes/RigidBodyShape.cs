/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents the abstract base class for shapes that can be attached to a rigid body.
/// </summary>
public abstract class RigidBodyShape : Shape
{
    /// <summary>
    /// The <see cref="RigidBody"/> to which this shape is attached. It is null until the shape is attached
    /// and after it is removed from a body.
    /// </summary>
    public RigidBody RigidBody { get; internal set; } = null!;

    /// <summary>Draws this shape using its attached body's transform, or the identity transform when unattached.</summary>
    /// <inheritdoc cref="Shape.DebugDraw(IDebugDrawer, in DebugDrawSettings)" path="/remarks"/>
    public override void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        JQuaternion orientation = RigidBody?.Orientation ?? JQuaternion.Identity;
        JVector position = RigidBody?.Position ?? JVector.Zero;
        var sink = new ShapeDebugDraw.DrawerSink(drawer, orientation, position, settings.CurrentColor);
        DebugDrawLocal(ref sink, settings);
    }

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

        if (dt > (Real)0.0 && RigidBody.World.NeedsCcdBounds(RigidBody))
        {
            JVector omega = data.AngularVelocity;
            Real angularSpeed = omega.Length();
            if (angularSpeed > (Real)0.0)
            {
                // Enclose the shape about the body's rotation origin, including local shape offsets.
                JVector extent = JVector.Max(JVector.Abs(box.Min - data.Position),
                    JVector.Abs(box.Max - data.Position));
                Real radius = extent.Length();

                // Rotation moves a point by at most radius * |omega| * dt, capped at 2 * radius.
                // Only the angular velocity perpendicular to each world axis can move points along it.
                Real scale = radius * MathR.Min(dt, (Real)2.0 / angularSpeed);
                JVector expansion = scale * new JVector(
                    MathR.Sqrt(omega.Y * omega.Y + omega.Z * omega.Z),
                    MathR.Sqrt(omega.X * omega.X + omega.Z * omega.Z),
                    MathR.Sqrt(omega.X * omega.X + omega.Y * omega.Y));

                // Every intermediate orientation also stays inside the enclosing sphere.
                box.Min = JVector.Max(box.Min - expansion, data.Position - new JVector(radius));
                box.Max = JVector.Min(box.Max + expansion, data.Position + new JVector(radius));
            }

            // Preserve the linear margin after bounding the rotation about the initial body position.
            Real swept = dt * data.Velocity.Length();
            box.Min -= new JVector(swept);
            box.Max += new JVector(swept);
        }

        WorldBoundingBox = box;
    }

    public virtual void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        ShapeHelper.CalculateBoundingBox(this, orientation, position, out box);
    }

    /// <summary>
    /// Calculates the shape's mass, center of mass, and inertia at unit density.
    /// The default implementation approximates the surface; overrides can provide exact mass properties.
    /// </summary>
    /// <remarks>
    /// The inertia tensor is computed relative to the coordinate system origin (0,0,0),
    /// not the center of mass.
    /// </remarks>
    [ReferenceFrame(ReferenceFrame.Local)]
    public virtual void CalculateMassInertia(out JSymmetricMatrix inertia, out JVector com, out Real mass)
    {
        ShapeHelper.CalculateMassInertia(this, out inertia, out com, out mass);
    }

    /// <summary>
    /// Casts a ray against the shape in local coordinates, without applying the body's transform.
    /// </summary>
    /// <param name="origin">The starting point of the ray.</param>
    /// <param name="direction">
    /// The direction of the ray. This vector does not need to be normalized.
    /// </param>
    /// <param name="normal">
    /// The surface normal at the point of intersection, if an intersection occurs.
    /// </param>
    /// <param name="lambda">
    /// The ray parameter from the <paramref name="origin"/> to the intersection point. Because
    /// <paramref name="direction"/> need not be normalized, this is not necessarily a distance. The hit point is:
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

    [ReferenceFrame(ReferenceFrame.World)]
    public sealed override bool Sweep<T>(in T support, in JQuaternion orientation, in JVector position, in JVector sweep,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
    {
        if (RigidBody == null)
        {
            bool hit = NarrowPhase.Sweep(this, support,
                orientation, position, sweep,
                out pointB, out pointA, out normal, out lambda);

            JVector.NegateInPlace(ref normal);
            return hit;
        }

        ref var data = ref RigidBody.Data;

        return NarrowPhase.Sweep(support, this,
            orientation, data.Orientation,
            position, data.Position,
            sweep, JVector.Zero,
            out pointA, out pointB, out normal, out lambda);
    }

    [ReferenceFrame(ReferenceFrame.World)]
    public sealed override bool Distance<T>(in T support, in JQuaternion orientation, in JVector position,
        out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
    {
        if (RigidBody == null)
        {
            return NarrowPhase.Distance(support, this,
                orientation, JQuaternion.Identity,
                position, JVector.Zero,
                out pointA, out pointB, out normal, out distance);
        }

        ref var data = ref RigidBody.Data;

        return NarrowPhase.Distance(support, this,
            orientation, data.Orientation,
            position, data.Position,
            out pointA, out pointB, out normal, out distance);
    }
}
