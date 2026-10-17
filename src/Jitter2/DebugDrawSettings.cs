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
}

/// <summary>
/// Controls the size, detail, and colors of debug visualizations.
/// A default-initialized value uses the same settings as <see cref="Default"/>.
/// </summary>
public readonly struct DebugDrawSettings
{
    private readonly Real scale;
    private readonly int segments, arrowHeadSegments, arrowHeadSpokes, shapeSubdivisions;
    private readonly JVector? origin;
    private readonly uint? referenceColor, lowerLimitColor, upperLimitColor, currentColor;

    /// <summary>
    /// Default settings: scale 1, 48 curve segments, 12 arrowhead segments with 8 spokes,
    /// shape subdivision depth 3, green guides, red lower limits, white current pose.
    /// </summary>
    public static DebugDrawSettings Default => default;

    /// <summary>
    /// Radius of angular guides and half extent of plane patches, in world units.
    /// Also scales arrows and markers. Physical distances and linear limits are never scaled.
    /// Must be finite and greater than zero. Default is 1.
    /// </summary>
    public Real Scale
    {
        get => scale == 0 ? 1 : scale;
        init
        {
            if (!Real.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(Scale));
            scale = value;
        }
    }

    /// <summary>Number of segments per circle or cone boundary, from 8 to 256. Default is 48. Arrowheads use <see cref="ArrowHeadSegments"/>.</summary>
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
    /// Number of segments around each arrowhead base, from 3 to 256. Default is 12.
    /// A value of 4 draws a square base; higher values approximate a circle.
    /// </summary>
    public int ArrowHeadSegments
    {
        get => arrowHeadSegments == 0 ? 12 : arrowHeadSegments;
        init
        {
            if (value < 3 || value > 256) throw new ArgumentOutOfRangeException(nameof(ArrowHeadSegments));
            arrowHeadSegments = value;
        }
    }

    /// <summary>
    /// Number of segments joining each arrowhead base to its tip, from 1 to 256. Default is 8.
    /// Clamped to <see cref="ArrowHeadSegments"/> so every spoke meets a distinct base vertex.
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
    /// Tessellation recursion depth for rigid-body shapes, including the final emission level.
    /// Must be at least 1. Default is 3. Each additional level splits every triangle into four.
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

    /// <summary>Color of reference frames and unconstrained guides. Default is green.</summary>
    public uint ReferenceColor { get => referenceColor ?? DebugDrawColors.Green; init => referenceColor = value; }

    /// <summary>Color of lower limits. Default is red.</summary>
    public uint LowerLimitColor { get => lowerLimitColor ?? DebugDrawColors.Red; init => lowerLimitColor = value; }

    /// <summary>Color of upper and fixed limits. Default is green.</summary>
    public uint UpperLimitColor { get => upperLimitColor ?? DebugDrawColors.Green; init => upperLimitColor = value; }

    /// <summary>Color of current anchors, axes, poses, and rigid-body shape wireframes. Default is white.</summary>
    public uint CurrentColor { get => currentColor ?? DebugDrawColors.White; init => currentColor = value; }
}
