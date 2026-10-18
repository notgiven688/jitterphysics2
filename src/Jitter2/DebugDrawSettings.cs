/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2;

/// <summary>Common colors packed as <c>0xRRGGBBAA</c>.</summary>
public static class DebugDrawColors
{
    public const uint White = 0xFFFFFFFF;
    public const uint Green = 0x00FF00FF;
    public const uint Red = 0xFF0000FF;

    // Lighter axis colors stay readable against dark backgrounds.
    public const uint AxisX = 0xFF6666FF;
    public const uint AxisY = 0x66EE88FF;
    public const uint AxisZ = 0x77AAFFFF;
}

/// <summary>
/// Controls the size, detail, and colors of debug visualizations.
/// A default-initialized value uses the same settings as <see cref="Default"/>.
/// </summary>
public readonly struct DebugDrawSettings
{
    private readonly Real guideSize, frameSize, arrowHeadSize;
    private readonly int segments, arrowSegments, arrowHeadSpokes, shapeSubdivisions;
    private readonly JVector? origin;
    private readonly uint? referenceColor, lowerLimitColor, upperLimitColor, currentColor;
    private readonly uint? axisXColor, axisYColor, axisZColor;
    private readonly bool? solidArrows;
    private readonly Real? arrowShaftRadius;

    /// <summary>
    /// Default settings: guide size 1, frame size 1, 48 curve segments, 12 arrow segments with 8 wire spokes,
    /// shape detail 3, solid arrows, green guides, red lower limits, white current pose, and RGB orientation frames.
    /// </summary>
    public static DebugDrawSettings Default => default;

    /// <summary>
    /// Radius of angular guides and half extent of plane patches, in world units.
    /// Also sets constraint arrow lengths and marker sizes. Coordinate frames use <see cref="FrameSize"/> instead.
    /// Arrowheads and shafts use <see cref="ArrowHeadSize"/> and <see cref="ArrowShaftRadius"/> independently.
    /// Physical distances and linear limits are never scaled.
    /// Must be finite and greater than zero. Default is 1.
    /// </summary>
    public Real GuideSize
    {
        get => guideSize == 0 ? 1 : guideSize;
        init
        {
            if (!Real.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(GuideSize));
            guideSize = value;
        }
    }

    /// <summary>
    /// Length of each coordinate-frame axis, in world units, independently of <see cref="GuideSize"/>.
    /// Also controls the automatic spacing of FixedAngle's frames. Arrowhead size and shaft radius are independent.
    /// Must be finite and greater than zero. Default is 1.
    /// </summary>
    public Real FrameSize
    {
        get => frameSize == 0 ? 1 : frameSize;
        init
        {
            if (!Real.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(FrameSize));
            frameSize = value;
        }
    }

    /// <summary>
    /// Length of arrowheads in world units, independently of arrow length, <see cref="GuideSize"/>,
    /// <see cref="FrameSize"/>, and shaft thickness. Default is 0.2. Used by both solid and wire arrows.
    /// Heads are clamped to half the arrow length; their base radius is 40% of their length.
    /// Must be finite and greater than zero.
    /// </summary>
    public Real ArrowHeadSize
    {
        get => arrowHeadSize == 0 ? (Real)0.2 : arrowHeadSize;
        init
        {
            if (!Real.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(ArrowHeadSize));
            arrowHeadSize = value;
        }
    }

    /// <summary>Number of segments per circle, cone boundary, or built-in round shape, from 8 to 256. Default is 48. Arrowheads use <see cref="ArrowSegments"/>.</summary>
    public int Segments
    {
        get => segments == 0 ? 48 : segments;
        init
        {
            if (value < 8 || value > 256) throw new ArgumentOutOfRangeException(nameof(Segments));
            segments = value;
        }
    }

    /// <summary>
    /// Number of segments around each arrowhead base and solid shaft, from 3 to 256. Default is 12.
    /// A value of 4 draws a square base; higher values approximate a circle.
    /// </summary>
    public int ArrowSegments
    {
        get => arrowSegments == 0 ? 12 : arrowSegments;
        init
        {
            if (value < 3 || value > 256) throw new ArgumentOutOfRangeException(nameof(ArrowSegments));
            arrowSegments = value;
        }
    }

    /// <summary>
    /// Number of segments joining each arrowhead base to its tip, from 1 to 256. Default is 8.
    /// Used for wire arrows only. Clamped to <see cref="ArrowSegments"/> so every spoke meets a distinct base vertex.
    /// </summary>
    public int ArrowHeadSpokes
    {
        get => arrowHeadSpokes == 0 ? 8 : arrowHeadSpokes;
        init
        {
            if (value < 1 || value > 256) throw new ArgumentOutOfRangeException(nameof(ArrowHeadSpokes));
            arrowHeadSpokes = value;
        }
    }

    /// <summary>
    /// Draws arrows and orientation frames as solid shafts with cone heads. Default is true.
    /// Set to false for wire arrows.
    /// </summary>
    public bool SolidArrows { get => solidArrows ?? true; init => solidArrows = value; }

    /// <summary>
    /// Radius of solid arrow shafts in world units, independently of arrow length,
    /// <see cref="GuideSize"/>, and <see cref="FrameSize"/>. Default is 0.025.
    /// Must be finite and greater than zero. The radius is clamped to 5% of the arrow length and
    /// half the arrowhead's base radius. Wire arrows are unaffected.
    /// </summary>
    public Real ArrowShaftRadius
    {
        get => arrowShaftRadius ?? (Real)0.025;
        init
        {
            if (!Real.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(ArrowShaftRadius));
            arrowShaftRadius = value;
        }
    }

    /// <summary>
    /// Drawing detail for point clouds and custom shapes that use an approximate surface.
    /// Must be at least 1. Default is 3. Each additional level increases the drawing cost roughly fourfold.
    /// Built-in round shapes use <see cref="Segments"/> instead.
    /// </summary>
    public int ShapeSubdivisions
    {
        get => shapeSubdivisions == 0 ? 3 : shapeSubdivisions;
        init
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(ShapeSubdivisions));
            shapeSubdivisions = value;
        }
    }

    /// <summary>
    /// Optional world-space origin for angular guides. By default they use body 1's position,
    /// or body 2's position when body 1 is World.NullBody. Joints with a ball socket use its anchor.
    /// FixedAngle places its reference near body 2 by default.
    /// Positional constraints always use their real anchors.
    /// </summary>
    public JVector? Origin
    {
        get => origin;
        init
        {
            if (value.HasValue) ArgumentCheck.Finite(value.Value, nameof(Origin));
            origin = value;
        }
    }

    /// <summary>Color of reference guides. Default is green.</summary>
    public uint ReferenceColor { get => referenceColor ?? DebugDrawColors.Green; init => referenceColor = value; }

    /// <summary>Color of lower limits. Default is red.</summary>
    public uint LowerLimitColor { get => lowerLimitColor ?? DebugDrawColors.Red; init => lowerLimitColor = value; }

    /// <summary>Color of upper and fixed limits. Default is green.</summary>
    public uint UpperLimitColor { get => upperLimitColor ?? DebugDrawColors.Green; init => upperLimitColor = value; }

    /// <summary>Color of current anchors, directions, and shape geometry. Default is white.</summary>
    public uint CurrentColor { get => currentColor ?? DebugDrawColors.White; init => currentColor = value; }

    /// <summary>Color of a frame's local X axis. Default is light red.</summary>
    public uint AxisXColor { get => axisXColor ?? DebugDrawColors.AxisX; init => axisXColor = value; }

    /// <summary>Color of a frame's local Y axis. Default is light green.</summary>
    public uint AxisYColor { get => axisYColor ?? DebugDrawColors.AxisY; init => axisYColor = value; }

    /// <summary>Color of a frame's local Z axis. Default is light blue.</summary>
    public uint AxisZColor { get => axisZColor ?? DebugDrawColors.AxisZ; init => axisZColor = value; }
}
