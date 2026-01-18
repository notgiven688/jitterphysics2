/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2;

/// <summary>
/// Defines an interface for objects that can be debug-drawn.
/// </summary>
public interface IDebugDrawable
{
    /// <summary>
    /// Passes an <see cref="IDebugDrawer"/> to draw basic debug information for the object.
    /// </summary>
    /// <param name="drawer">The debug drawer used for rendering debug information.</param>
    public void DebugDraw(IDebugDrawer drawer);
}

/// <summary>
/// Defines an interface for drawing debug visualization elements.
/// </summary>
public interface IDebugDrawer
{
    /// <summary>
    /// Draws a line segment between two points.
    /// </summary>
    /// <param name="pA">The start point of the segment.</param>
    /// <param name="pB">The end point of the segment.</param>
    public void DrawSegment(in JVector pA, in JVector pB);

    /// <summary>
    /// Draws a triangle defined by three vertices.
    /// </summary>
    /// <param name="pA">The first vertex of the triangle.</param>
    /// <param name="pB">The second vertex of the triangle.</param>
    /// <param name="pC">The third vertex of the triangle.</param>
    public void DrawTriangle(in JVector pA, in JVector pB, in JVector pC);

    /// <summary>
    /// Draws a point at the specified position.
    /// </summary>
    /// <param name="p">The position of the point.</param>
    public void DrawPoint(in JVector p);
}