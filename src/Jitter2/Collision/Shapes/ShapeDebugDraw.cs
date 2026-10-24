/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.DataStructures;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

internal static class ShapeDebugDraw
{
    internal readonly struct DrawerSink(IDebugDrawer drawer, JQuaternion orientation, JVector position, uint color)
        : ISink<JTriangle>
    {
        public void Add(in JTriangle triangle)
        {
            drawer.DrawTriangle(
                JVector.Transform(triangle.V0, orientation) + position,
                JVector.Transform(triangle.V1, orientation) + position,
                JVector.Transform(triangle.V2, orientation) + position, color);
        }
    }

    private static void Quad<TSink>(ref TSink sink, JVector a, JVector b, JVector c, JVector d)
        where TSink : ISink<JTriangle>
    {
        sink.Add(new JTriangle(a, b, c));
        sink.Add(new JTriangle(a, c, d));
    }

    internal static void Box<TSink>(ref TSink sink, JVector halfSize) where TSink : ISink<JTriangle>
    {
        Real x = halfSize.X, y = halfSize.Y, z = halfSize.Z;
        JVector a = new(-x, -y, -z), b = new(x, -y, -z), c = new(x, y, -z), d = new(-x, y, -z);
        JVector e = new(-x, -y, z), f = new(x, -y, z), g = new(x, y, z), h = new(-x, y, z);
        Quad(ref sink, a, d, c, b);
        Quad(ref sink, e, f, g, h);
        Quad(ref sink, a, e, h, d);
        Quad(ref sink, b, c, g, f);
        Quad(ref sink, a, b, f, e);
        Quad(ref sink, d, h, g, c);
    }

    private static void Circle(Span<JVector> points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            Real angle = 2 * MathR.PI * i / points.Length;
            points[i] = new JVector(MathR.Cos(angle), 0, MathR.Sin(angle));
        }
    }

    // Join two horizontal rings, using a single fan triangle at either pole.
    private static void Band<TSink>(ref TSink sink, ReadOnlySpan<JVector> circle,
        Real lowerRadius, Real lowerY, Real upperRadius, Real upperY) where TSink : ISink<JTriangle>
    {
        for (int i = 0; i < circle.Length; i++)
        {
            JVector current = circle[i], next = circle[(i + 1) % circle.Length];
            JVector a = lowerRadius * current + new JVector(0, lowerY, 0);
            JVector b = upperRadius * current + new JVector(0, upperY, 0);
            JVector c = upperRadius * next + new JVector(0, upperY, 0);
            JVector d = lowerRadius * next + new JVector(0, lowerY, 0);
            if (upperRadius > 0) sink.Add(new JTriangle(a, b, c));
            if (lowerRadius > 0) sink.Add(new JTriangle(a, c, d));
        }
    }

    internal static void Capsule<TSink>(ref TSink sink, Real radius, Real halfLength, int segments)
        where TSink : ISink<JTriangle>
    {
        Span<JVector> circle = stackalloc JVector[segments];
        Circle(circle);
        int rings = Math.Max(2, segments / 4);
        Real previousRadius = 0, previousY = -halfLength - radius;
        for (int i = 1; i <= rings; i++)
        {
            Real angle = MathR.PI * (Real)0.5 * i / rings;
            Real r = i == rings ? radius : radius * MathR.Sin(angle);
            Real y = i == rings ? -halfLength : -halfLength - radius * MathR.Cos(angle);
            Band(ref sink, circle, previousRadius, previousY, r, y);
            previousRadius = r;
            previousY = y;
        }

        if (halfLength > 0) Band(ref sink, circle, radius, -halfLength, radius, halfLength);
        previousY = halfLength;
        for (int i = 1; i <= rings; i++)
        {
            Real angle = MathR.PI * (Real)0.5 * i / rings;
            Real r = i == rings ? 0 : radius * MathR.Cos(angle);
            Real y = i == rings ? halfLength + radius : halfLength + radius * MathR.Sin(angle);
            Band(ref sink, circle, previousRadius, previousY, r, y);
            previousRadius = r;
            previousY = y;
        }
    }

    internal static void Cylinder<TSink>(ref TSink sink, Real radius, Real height, int segments)
        where TSink : ISink<JTriangle>
    {
        Span<JVector> circle = stackalloc JVector[segments];
        Circle(circle);
        Real halfHeight = height * (Real)0.5;
        Band(ref sink, circle, 0, -halfHeight, radius, -halfHeight);
        Band(ref sink, circle, radius, -halfHeight, radius, halfHeight);
        Band(ref sink, circle, radius, halfHeight, 0, halfHeight);
    }

    internal static void Cone<TSink>(ref TSink sink, Real radius, Real height, int segments)
        where TSink : ISink<JTriangle>
    {
        Span<JVector> circle = stackalloc JVector[segments];
        Circle(circle);
        Real bottom = -height * (Real)0.25;
        Band(ref sink, circle, 0, bottom, radius, bottom);
        Band(ref sink, circle, radius, bottom, 0, height * (Real)0.75);
    }
}
