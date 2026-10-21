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
    private readonly int segments, arrowSegments, shapeSubdivisions;
    private readonly JVector? origin;
    private readonly uint? referenceColor, lowerLimitColor, upperLimitColor, currentColor;
    private readonly uint? axisXColor, axisYColor, axisZColor;
    private readonly bool? solidArrows;
    private readonly Real? arrowShaftRadius, pointRadius;

    /// <summary>
    /// The default drawing settings. Individual properties describe their default values.
    /// </summary>
    public static DebugDrawSettings Default => default;

    /// <summary>
    /// Size of constraint guides in world units. Must be finite and positive. Default is 1.
    /// </summary>
    /// <remarks>
    /// Sets angular guide radii, plane half extents, constraint arrow lengths, and stop marker sizes.
    /// Physical distances and linear limits keep their actual sizes. Coordinate frames, arrowheads,
    /// shafts, and anchor markers have separate size properties.
    /// </remarks>
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
    /// Length of each coordinate-frame axis in world units. Must be finite and positive. Default is 1.
    /// Changing this does not resize arrowheads or shafts.
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
    /// Radius of spherical anchor markers in world units.
    /// Default is 0.06. Must be finite and nonnegative. Zero hides the markers.
    /// </summary>
    public Real PointRadius
    {
        get => pointRadius ?? (Real)0.06;
        init
        {
            if (!Real.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(PointRadius));
            pointRadius = value;
        }
    }

    /// <summary>
    /// Length of solid and wire arrowheads in world units. Must be finite and positive. Default is 0.2.
    /// Clamped to half the arrow length for short arrows.
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
    /// Number of sides around arrowheads and solid shafts, from 3 to 256. Default is 12.
    /// Higher values make arrows look rounder.
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
    /// Draws arrows and orientation frames as solid shafts with cone heads. Default is true.
    /// Set to false for wire arrows.
    /// </summary>
    public bool SolidArrows { get => solidArrows ?? true; init => solidArrows = value; }

    /// <summary>
    /// Radius of solid arrow shafts in world units. Must be finite and positive. Default is 0.025.
    /// Reduced for short arrows or small arrowheads. Does not affect wire arrows.
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
    /// Optional position for angular guides, in world space. Default is null.
    /// </summary>
    /// <remarks>
    /// When null, angular constraints use body 1's position, or body 2's if body 1 is
    /// <see cref="World.NullBody"/>. Joints with a ball socket use its anchor.
    /// This does not move positional anchors or the frames drawn by <c>FixedAngle</c>.
    /// </remarks>
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
