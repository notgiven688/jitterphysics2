/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Defines an interface for a generic convex shape characterized by its support function.
/// </summary>
/// <remarks>
/// The support function is the fundamental operation for GJK-based collision detection algorithms.
/// Any convex shape can be represented implicitly through its support mapping without requiring
/// explicit vertex or face data.
/// </remarks>
public interface ISupportMappable
{
    /// <summary>
    /// Computes the point on the shape that is furthest in the specified direction.
    /// </summary>
    /// <param name="direction">The search direction in local space. Does not need to be normalized.</param>
    /// <param name="result">The point on the shape's surface furthest along <paramref name="direction"/>.</param>
    void SupportMap(in JVector direction, out JVector result);

    /// <summary>
    /// Computes a point deep within the shape, used as an initial search point in GJK-based algorithms.
    /// </summary>
    /// <param name="point">A point guaranteed to be inside the convex hull, typically the center of mass.</param>
    void GetCenter(out JVector point);
}