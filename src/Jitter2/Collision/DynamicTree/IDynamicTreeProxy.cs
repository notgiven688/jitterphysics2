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
    /// <param name="normal">
    /// The surface normal at the intersection point, or <see cref="JVector.Zero"/> if the ray origin
    /// is inside the object. Use the return value, not this parameter, to test whether a hit occurred.
    /// </param>
    /// <param name="lambda">
    /// The distance along the ray to the intersection: <c>hitPoint = origin + lambda * direction</c>.
    /// Zero when the ray origin is inside the object.
    /// </param>
    /// <returns>
    /// <c>true</c> if the ray intersects with the object; otherwise, <c>false</c>.
    /// Also returns <c>true</c> when the ray origin is inside the object, with <paramref name="lambda"/>
    /// set to zero and <paramref name="normal"/> set to <see cref="JVector.Zero"/>.
    /// </returns>
    public bool RayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda);
}

/// <summary>
/// Represents an entity that supports closest-point distance queries against a support-mapped query shape.
/// </summary>
public interface IDistanceTestable
{
    /// <summary>
    /// Finds the closest points between this object and the query shape.
    /// </summary>
    /// <typeparam name="T">The query support-map type.</typeparam>
    /// <param name="support">The query shape.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="pointA">
    /// Closest point on the query shape in world space. Not well-defined when the shapes overlap.
    /// </param>
    /// <param name="pointB">
    /// Closest point on this object in world space. Not well-defined when the shapes overlap.
    /// </param>
    /// <param name="normal">
    /// Unit direction from the query shape toward this object, or <see cref="JVector.Zero"/> when
    /// the shapes overlap. Do not use this to test whether a result was found.
    /// </param>
    /// <param name="distance">The separation distance between the shapes. Zero when overlapping.</param>
    /// <returns><c>true</c> if the shapes are separated; <c>false</c> if they overlap.</returns>
    public bool Distance<T>(in T support, in JQuaternion orientation, in JVector position,
        out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
        where T : ISupportMappable;
}

/// <summary>
/// Represents an entity that can be sweep-tested against a moving support-mapped query shape.
/// </summary>
public interface ISweepTestable
{
    /// <summary>
    /// Performs a sweep test against this object.
    /// </summary>
    /// <typeparam name="T">The query support-map type.</typeparam>
    /// <param name="support">The moving query shape in local space.</param>
    /// <param name="orientation">The query shape orientation in world space.</param>
    /// <param name="position">The query shape position in world space.</param>
    /// <param name="sweep">The query shape translation in world space.</param>
    /// <param name="pointA">Collision point on the query shape in world space at t = 0. Not well-defined when the shapes already overlap.</param>
    /// <param name="pointB">Collision point on this object in world space at t = 0. Not well-defined when the shapes already overlap.</param>
    /// <param name="normal">
    /// Collision normal in world space, or <see cref="JVector.Zero"/> if the shapes already overlap.
    /// Use the return value to determine whether a hit occurred; do not rely on this being non-zero.
    /// </param>
    /// <param name="lambda">The time of impact expressed in units of <paramref name="sweep"/>. Zero if the shapes already overlap.</param>
    /// <returns><c>true</c> if the query shape hits or already overlaps this object; otherwise, <c>false</c>.</returns>
    public bool Sweep<T>(in T support, in JQuaternion orientation, in JVector position, in JVector sweep,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where T : ISupportMappable;
}
