/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
    JBBox WorldBoundingBox { get; }
}

/// <summary>
/// Represents an object for which the bounding box can be updated.
/// </summary>
public interface IUpdatableBoundingBox
{
    /// <summary>
    /// Updates the bounding box.
    /// </summary>
    public void UpdateWorldBoundingBox(Real dt);
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