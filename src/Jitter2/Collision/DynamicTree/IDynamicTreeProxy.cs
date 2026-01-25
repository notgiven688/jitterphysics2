/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.DataStructures;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Represents an entity that can be tracked by the <see cref="DynamicTree"/> for broadphase collision detection.
/// </summary>
public interface IDynamicTreeProxy : IPartitionedSetIndex
{
    /// <summary>
    /// Gets or sets the internal node pointer used by the tree.
    /// </summary>
    /// <remarks>
    /// This value is managed internally by <see cref="DynamicTree"/>. Do not modify directly.
    /// </remarks>
    int NodePtr { get; set; }

    /// <summary>
    /// Gets the velocity of the entity, used for bounding box expansion.
    /// </summary>
    JVector Velocity { get; }

    /// <summary>
    /// Gets the axis-aligned bounding box of the entity in world space.
    /// </summary>
    JBoundingBox WorldBoundingBox { get; }
}

/// <summary>
/// Represents an entity whose bounding box can be recomputed.
/// </summary>
public interface IUpdatableBoundingBox
{
    /// <summary>
    /// Recomputes the world-space bounding box.
    /// </summary>
    /// <param name="dt">The timestep for velocity-based expansion. Default is zero.</param>
    public void UpdateWorldBoundingBox(Real dt = (Real)0.0);
}

/// <summary>
/// Represents an entity that can be intersected by a ray.
/// </summary>
public interface IRayCastable
{
    /// <summary>
    /// Performs a ray cast against this object.
    /// </summary>
    /// <param name="origin">The starting point of the ray.</param>
    /// <param name="direction">The direction of the ray. Does not need to be normalized.</param>
    /// <param name="normal">The surface normal at the intersection point.</param>
    /// <param name="lambda">
    /// The distance along the ray to the intersection: <c>hitPoint = origin + lambda * direction</c>.
    /// </param>
    /// <returns><c>true</c> if the ray intersects with the object; otherwise, <c>false</c>.</returns>
    public bool RayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda);
}