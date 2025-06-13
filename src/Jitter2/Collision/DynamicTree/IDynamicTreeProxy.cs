/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.DataStructures;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Interface for entities which should be added to the <see cref="DynamicTree"/>.
/// </summary>
public interface IDynamicTreeProxy : IPartitionedSetIndex
{
    /// <summary>
    /// A pointer value which should only by internally modified by the tree.
    /// </summary>
    int NodePtr { get; set; }

    /// <summary>
    /// The velocity of the entity.
    /// </summary>
    JVector Velocity { get; }

    /// <summary>
    /// The world bounding box of the entity.
    /// </summary>
    JBoundingBox WorldBoundingBox { get; }
}

/// <summary>
/// Represents an object for which the bounding box can be updated.
/// </summary>
public interface IUpdatableBoundingBox
{
    /// <summary>
    /// Updates the bounding box.
    /// </summary>
    public void UpdateWorldBoundingBox(Real dt = (Real)0.0);
}

/// <summary>
/// Represents an object that can be intersected by a ray.
/// </summary>
public interface IRayCastable
{
    /// <summary>
    /// Performs a ray cast against the object, checking if a ray originating from a specified point
    /// and traveling in a specified direction intersects with the object.
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
    public bool RayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda);
}