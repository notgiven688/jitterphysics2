/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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