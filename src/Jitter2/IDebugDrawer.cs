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
    public void DrawSegment(in JVector pA, in JVector pB);
    public void DrawTriangle(in JVector pA, in JVector pB, in JVector pC);
    public void DrawPoint(in JVector p);
}