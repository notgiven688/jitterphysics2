/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// Represents an angular limit defined by a minimum and maximum angle.
/// Used by constraints to restrict rotational motion within a specified range.
/// </summary>
/// <param name="from">The minimum angle of the limit.</param>
/// <param name="to">The maximum angle of the limit.</param>
public struct AngularLimit(JAngle from, JAngle to)
{
    /// <summary>
    /// Gets or sets the minimum angle of the limit.
    /// </summary>
    public JAngle From { get; set; } = from;

    /// <summary>
    /// Gets or sets the maximum angle of the limit.
    /// </summary>
    public JAngle To { get; set; } = to;

    /// <summary>
    /// A limit that allows full rotation (-π to +π radians).
    /// </summary>
    public static readonly AngularLimit Full =
        new(JAngle.FromRadian(-MathR.PI), JAngle.FromRadian(MathR.PI));

    /// <summary>
    /// A limit that locks the angle in place (no rotation allowed).
    /// </summary>
    public static readonly AngularLimit Fixed =
        new(JAngle.FromRadian(+(Real)1e-6), JAngle.FromRadian(-(Real)1e-6));

    /// <summary>
    /// Creates an angular limit from degree values.
    /// </summary>
    /// <param name="min">The minimum angle in degrees.</param>
    /// <param name="max">The maximum angle in degrees.</param>
    /// <returns>A new <see cref="AngularLimit"/> instance.</returns>
    public static AngularLimit FromDegree(Real min, Real max)
    {
        return new AngularLimit(JAngle.FromDegree(min), JAngle.FromDegree(max));
    }

    /// <summary>
    /// Deconstructs the limit into its minimum and maximum angles.
    /// </summary>
    /// <param name="limitMin">The minimum angle.</param>
    /// <param name="limitMax">The maximum angle.</param>
    public readonly void Deconstruct(out JAngle limitMin, out JAngle limitMax)
    {
        limitMin = From;
        limitMax = To;
    }
}

/// <summary>
/// Represents a linear limit defined by a minimum and maximum distance.
/// Used by constraints to restrict translational motion within a specified range.
/// </summary>
/// <param name="from">The minimum distance of the limit.</param>
/// <param name="to">The maximum distance of the limit.</param>
public struct LinearLimit(Real from, Real to)
{
    /// <summary>
    /// Gets or sets the minimum distance of the limit.
    /// </summary>
    public Real From { get; set; } = from;

    /// <summary>
    /// Gets or sets the maximum distance of the limit.
    /// </summary>
    public Real To { get; set; } = to;

    /// <summary>
    /// A limit that allows unrestricted movement (negative to positive infinity).
    /// </summary>
    public static readonly LinearLimit Full =
        new(Real.NegativeInfinity, Real.PositiveInfinity);

    /// <summary>
    /// A limit that locks the position in place (no translation allowed).
    /// </summary>
    public static readonly LinearLimit Fixed =
        new((Real)1e-6, -(Real)1e-6);

    /// <summary>
    /// Creates a linear limit from minimum and maximum values.
    /// </summary>
    /// <param name="min">The minimum distance.</param>
    /// <param name="max">The maximum distance.</param>
    /// <returns>A new <see cref="LinearLimit"/> instance.</returns>
    public static LinearLimit FromMinMax(Real min, Real max)
    {
        return new LinearLimit(min, max);
    }

    /// <summary>
    /// Deconstructs the limit into its minimum and maximum distances.
    /// </summary>
    /// <param name="limitMin">The minimum distance.</param>
    /// <param name="limitMax">The maximum distance.</param>
    public readonly void Deconstruct(out Real limitMin, out Real limitMax)
    {
        limitMin = From;
        limitMax = To;
    }
}
