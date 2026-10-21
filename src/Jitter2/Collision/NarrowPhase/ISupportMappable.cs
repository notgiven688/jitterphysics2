/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Describes a convex shape by returning its furthest point in any given direction.
/// </summary>
/// <remarks>All points and directions use the shape's local coordinate system.</remarks>
public interface ISupportMappable
{
    /// <summary>
    /// Computes the point on the shape that is furthest in the specified direction.
    /// </summary>
    /// <param name="direction">The search direction in local space. Does not need to be normalized.</param>
    /// <param name="result">The point on the shape's surface furthest along <paramref name="direction"/>.</param>
    void SupportMap(in JVector direction, out JVector result);

    /// <summary>
    /// Returns a point inside the shape. It does not need to be the center of mass.
    /// </summary>
    /// <param name="point">An interior point in local space.</param>
    void GetCenter(out JVector point);
}
