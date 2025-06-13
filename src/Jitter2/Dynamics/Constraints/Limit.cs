/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2.Dynamics.Constraints;

public struct AngularLimit
{
    public JAngle From { get; set; }
    public JAngle To { get; set; }

    public static readonly AngularLimit Full =
        new(JAngle.FromRadian(-MathR.PI), JAngle.FromRadian(MathR.PI));

    public static readonly AngularLimit Fixed =
        new(JAngle.FromRadian(+(Real)1e-6), JAngle.FromRadian(-(Real)1e-6));

    public static AngularLimit FromDegree(Real min, Real max)
    {
        return new AngularLimit(JAngle.FromDegree(min), JAngle.FromDegree(max));
    }

    public AngularLimit(JAngle from, JAngle to)
    {
        From = from;
        To = to;
    }

    public readonly void Deconstruct(out JAngle limitMin, out JAngle limitMax)
    {
        limitMin = From;
        limitMax = To;
    }
}

public struct LinearLimit
{
    public Real From { get; set; }
    public Real To { get; set; }

    public static readonly LinearLimit Full =
        new(Real.NegativeInfinity, Real.PositiveInfinity);

    public static readonly LinearLimit Fixed =
        new((Real)1e-6, -(Real)1e-6);

    public LinearLimit(Real from, Real to)
    {
        From = from;
        To = to;
    }

    public static LinearLimit FromMinMax(Real min, Real max)
    {
        return new LinearLimit(min, max);
    }

    public readonly void Deconstruct(out Real limitMin, out Real limitMax)
    {
        limitMin = From;
        limitMax = To;
    }
}