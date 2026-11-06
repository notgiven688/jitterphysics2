/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Extension of <see cref="ISupportMappable"/> for shapes that report their whole outermost feature
/// in a given direction: a point, a segment or a convex polygon.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CollisionManifold"/> uses the feature to place contact points. Every
/// <see cref="Shapes.Shape"/> implements it by sampling its support mapping with
/// <see cref="SupportFeatureSampler"/>; shapes with known geometry override it, which is faster
/// and exact. Other types implementing <see cref="ISupportMappable"/> can forward to
/// <see cref="SupportFeatureSampler.Sample{T}"/>.
/// </para>
/// <para>
/// The feature must be the support set of the shape for all directions within
/// <c>tolerance</c> (the sine of an angle) of the given direction: a face whose normal is that close
/// to the direction is reported as the face, an edge as the edge, and a curved side as its resting
/// point or line. Every reported vertex must lie on the shape's surface.
/// </para>
/// </remarks>
public interface ISupportFeature : ISupportMappable
{
    /// <summary>
    /// Writes the outermost feature of the shape in the given direction.
    /// </summary>
    /// <param name="direction">The direction in local space. Does not need to be normalized.</param>
    /// <param name="tolerance">The sine of the angle within which directions share the feature.</param>
    /// <param name="vertices">
    /// Receives the vertices in local space: one for a point, two for a segment, three or more for a
    /// convex polygon in winding order. At least <see cref="MaxVertices"/> entries long.
    /// </param>
    /// <returns>The number of vertices written, at least one.</returns>
    int SupportFeature(in JVector direction, Real tolerance, Span<JVector> vertices);

    /// <summary>
    /// The largest number of vertices a feature may have.
    /// </summary>
    public const int MaxVertices = 8;
}
