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
    /// Implementations that only provide the original overload use their default visualization.
    /// </summary>
    public void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings) => DebugDraw(drawer);
}

/// <summary>
/// Receives colored line segments for debug visualization, in world space.
/// </summary>
/// <remarks>Use <see cref="DebugDrawGeometry"/> to draw markers and other wire geometry.</remarks>
public interface IDebugDrawer
{
    /// <summary>
    /// Draws a line segment between two points with a color packed as <c>0xRRGGBBAA</c>.
    /// </summary>
    /// <param name="pA">The start point of the segment in world space.</param>
    /// <param name="pB">The end point of the segment in world space.</param>
    /// <param name="color">The color packed as <c>0xRRGGBBAA</c>.</param>
    public void DrawSegment(in JVector pA, in JVector pB, uint color);
}
