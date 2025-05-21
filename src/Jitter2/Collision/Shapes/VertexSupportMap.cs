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

using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Implements a SIMD accelerated support map for a set of vertices.
/// </summary>
public struct VertexSupportMap : ISupportMappable, IEquatable<VertexSupportMap>
{
    private Real[] xvalues, yvalues, zvalues;
    private JVector center;

    public VertexSupportMap(IReadOnlyList<JVector> vertices)
    {
        int length = vertices.Count;

        xvalues = new Real[length];
        yvalues = new Real[length];
        zvalues = new Real[length];

        for (int i = 0; i < length; i++)
        {
            xvalues[i] = vertices[i].X;
            yvalues[i] = vertices[i].Y;
            zvalues[i] = vertices[i].Z;

            center.X += vertices[i].X;
            center.Y += vertices[i].Y;
            center.Z += vertices[i].Z;
        }

        center *= ((Real)1.0 / (Real)length);
    }

    public void SupportMap(in JVector direction, out JVector result)
    {
        if (Vector.IsHardwareAccelerated) SupportMapAccelerated(direction, out result);
        else SupportMapScalar(direction, out result);
    }

    private void SupportMapAccelerated(in JVector direction, out JVector result)
    {
        Real maxDotProduct = Real.MinValue;
        int length = xvalues.Length;
        int index = 0;

        var dirX = Vector.Create(direction.X);
        var dirY = Vector.Create(direction.Y);
        var dirZ = Vector.Create(direction.Z);

        int i = 0;
        for (; i <= length - 4; i += 4)
        {
            var vx = Vector.LoadUnsafe(ref xvalues[i]);
            var vy = Vector.LoadUnsafe(ref yvalues[i]);
            var vz = Vector.LoadUnsafe(ref zvalues[i]);

            var dx = Vector.Multiply(vx, dirX);
            var dy = Vector.Multiply(vy, dirY);
            var dz = Vector.Multiply(vz, dirZ);

            var sum = Vector.Add(dx, Vector.Add(dy, dz));

            Real d0 = sum.GetElement(0);
            Real d1 = sum.GetElement(1);
            Real d2 = sum.GetElement(2);
            Real d3 = sum.GetElement(3);

            if (d0 > maxDotProduct) { maxDotProduct = d0; index = i + 0; }
            if (d1 > maxDotProduct) { maxDotProduct = d1; index = i + 1; }
            if (d2 > maxDotProduct) { maxDotProduct = d2; index = i + 2; }
            if (d3 > maxDotProduct) { maxDotProduct = d3; index = i + 3; }
        }

        for (; i < length; i++)
        {
            Real dotProduct = xvalues[i] * direction.X +
                              yvalues[i] * direction.Y +
                              zvalues[i] * direction.Z;

            if (dotProduct < maxDotProduct) continue;
            maxDotProduct = dotProduct;
            index = i;
        }

        result = new JVector(xvalues[index], yvalues[index], zvalues[index]);
    }

    private void SupportMapScalar(in JVector direction, out JVector result)
    {
        Real maxDotProduct = Real.MinValue;
        int length = xvalues.Length;
        int index = 0;

        for (int i = 0; i < length; i++)
        {
            Real dotProduct = xvalues[i] * direction.X +
                              yvalues[i] * direction.Y +
                              zvalues[i] * direction.Z;

            if (dotProduct < maxDotProduct) continue;
            maxDotProduct = dotProduct;
            index = i;
        }

        result = new JVector(xvalues[index], yvalues[index], zvalues[index]);
    }

    public void GetCenter(out JVector point) => point = center;

    public bool Equals(VertexSupportMap other) => xvalues.Equals(other.xvalues) &&
                                                  yvalues.Equals(other.yvalues) &&
                                                  zvalues.Equals(other.zvalues);

    public override bool Equals(object? obj) => obj is VertexSupportMap other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(xvalues, yvalues, zvalues);
}