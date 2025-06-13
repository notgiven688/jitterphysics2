/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Defines an interface for a generic convex shape, which is characterized by its support function.
/// </summary>
public interface ISupportMappable
{
    /// <summary>
    /// Identifies the point on the shape that is furthest in the specified direction.
    /// </summary>
    /// <param name="direction">The direction in which to search for the furthest point. It does not need to be normalized.</param>
    void SupportMap(in JVector direction, out JVector result);

    /// <summary>
    /// Returns a point deep within the shape. This is used in algorithms which work with the implicit
    /// definition of the support map function. The center of mass is a good choice.
    /// </summary>
    void GetCenter(out JVector point);
}