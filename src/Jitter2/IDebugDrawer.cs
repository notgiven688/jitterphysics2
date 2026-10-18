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

    /// <summary>
    /// Draws the object using the supplied visualization settings.
    /// Implementations that only provide <see cref="DebugDraw(IDebugDrawer)"/> use their default visualization.
    /// </summary>
    public void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings) => DebugDraw(drawer);
}

/// <summary>
/// Receives colored line segments and triangles for debug visualization, in world space.
/// </summary>
/// <remarks>
/// Both drawing methods must be implemented. A wireframe drawer can draw triangles as three line segments.
/// Use <see cref="DebugDrawGeometry"/> to generate point markers and other drawing geometry.
/// </remarks>
public interface IDebugDrawer
{
    /// <summary>
    /// Draws a line segment between two points with a color packed as <c>0xRRGGBBAA</c>.
    /// </summary>
    /// <param name="pA">The start point of the segment in world space.</param>
    /// <param name="pB">The end point of the segment in world space.</param>
    /// <param name="color">The color packed as <c>0xRRGGBBAA</c>.</param>
    public void DrawSegment(in JVector pA, in JVector pB, uint color);

    /// <summary>
    /// Draws a triangle with a color packed as <c>0xRRGGBBAA</c>.
    /// </summary>
    /// <remarks>
    /// Vertices are counterclockwise when viewed from the front. The front normal points along
    /// <c>(pB - pA) x (pC - pA)</c>. Implementations may use back-face culling.
    /// </remarks>
    /// <param name="pA">The first vertex in world space.</param>
    /// <param name="pB">The second vertex in world space.</param>
    /// <param name="pC">The third vertex in world space.</param>
    /// <param name="color">The color packed as <c>0xRRGGBBAA</c>.</param>
    public void DrawTriangle(in JVector pA, in JVector pB, in JVector pC, uint color);
}
