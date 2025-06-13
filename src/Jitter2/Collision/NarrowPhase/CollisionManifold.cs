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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Represents a contact manifold between two convex shapes, storing up to six contact points.
/// The manifold is constructed using the GJK-based support mapping of both shapes projected
/// along perturbed normals distributed in a hexagonal pattern around the collision normal.
/// </summary>
public unsafe struct CollisionManifold
{
    private fixed Real manifoldData[12*3];

    private int leftCount;
    private int rightCount;
    private int manifoldCount;

    private const Real Sqrt3Over2 = (Real)0.86602540378;
    private const Real Perturbation = (Real)0.01;

    private static readonly Real[] hexagonVertices = [(Real)1.0, (Real)0.0, (Real)0.5, Sqrt3Over2, -(Real)0.5, Sqrt3Over2,
        -(Real)1.0, (Real)0.0, -(Real)0.5, -Sqrt3Over2, (Real)0.5, -Sqrt3Over2];

    public Span<JVector> ManifoldA => MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[0]), 6);
    public Span<JVector> ManifoldB => MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[18]), 6);

    public readonly int Count => manifoldCount;

    private void PushLeft(Span<JVector> left, in JVector v)
    {
        const Real epsilon = (Real)0.001;

        if (leftCount > 0)
        {
            if ((left[0] - v).LengthSquared() < epsilon) return;
        }

        if (leftCount > 1)
        {
            if ((left[leftCount - 1] - v).LengthSquared() < epsilon) return;
        }

        left[leftCount++] = v;
    }

    private void PushRight(Span<JVector> right, in JVector v)
    {
        const Real epsilon = (Real)0.001;

        if (rightCount > 0)
        {
            if ((right[0] - v).LengthSquared() < epsilon) return;
        }

        if (rightCount > 1)
        {
            if ((right[rightCount - 1] - v).LengthSquared() < epsilon) return;
        }

        right[rightCount++] = v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void BuildManifold<TA,TB>(TA shapeA, TB shapeB, in JQuaternion quaternionA, in JQuaternion quaternionB,
        in JVector positionA, in JVector positionB, in JVector pA, in JVector pB, in JVector normal)
        where TA : ISupportMappable where TB : ISupportMappable
    {
        // Reset
        leftCount = 0;
        rightCount = 0;
        manifoldCount = 0;

        JVector crossVector1 = MathHelper.CreateOrthonormal(normal);
        JVector crossVector2 = normal % crossVector1;

        Span<JVector> left = stackalloc JVector[6];
        Span<JVector> right = stackalloc JVector[6];

        for (int e = 0; e < 6; e++)
        {
            JVector ptNormal = normal + hexagonVertices[2 * e + 0] * Perturbation * crossVector1 +
                               hexagonVertices[2 * e + 1] * Perturbation * crossVector2;

            JVector.ConjugatedTransform(ptNormal, quaternionA, out JVector tmp);
            shapeA.SupportMap(tmp, out JVector np1);
            JVector.Transform(np1, quaternionA, out np1);
            JVector.Add(np1, positionA, out np1);
            PushLeft(left, np1);

            JVector.NegateInPlace(ref ptNormal);

            JVector.ConjugatedTransform(ptNormal, quaternionB, out tmp);
            shapeB.SupportMap(tmp, out JVector np2);
            JVector.Transform(np2, quaternionB, out np2);
            JVector.Add(np2, positionB, out np2);
            PushRight(right, np2);
        }

        Span<JVector> mA = MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[0]), 6);
        Span<JVector> mB = MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[18]), 6);

        // ---

        if (leftCount > 2)
        {
            for (int e = 0; e < rightCount; e++)
            {
                JVector p = right[e];
                JVector a = left[leftCount - 1];
                JVector b = left[0];

                JVector cr = (b - a) % (p - a);

                bool sameSign = true;

                for (int i = 0; i < leftCount - 1; i++)
                {
                    a = left[i];
                    b = left[i + 1];

                    JVector cr2 = (b - a) % (p - a);

                    sameSign = JVector.Dot(cr, cr2) > (Real)1e-3;
                    if (!sameSign) break;
                }

                if (sameSign)
                {
                    Real diff = JVector.Dot(p - pA, normal);
                    mB[manifoldCount] = p;
                    mA[manifoldCount++] = p - diff * normal;

                    if (manifoldCount == 6) return;
                }
            }
        }

        // ---
        if (rightCount > 2)
        {
            for (int e = 0; e < leftCount; e++)
            {
                JVector p = left[e];
                JVector a = right[rightCount - 1];
                JVector b = right[0];

                JVector cr = (b - a) % (p - a);

                bool sameSign = true;

                for (int i = 0; i < rightCount - 1; i++)
                {
                    a = right[i];
                    b = right[i + 1];

                    JVector cr2 = (b - a) % (p - a);

                    sameSign = JVector.Dot(cr, cr2) > (Real)1e-3;
                    if (!sameSign) break;
                }

                if (sameSign)
                {
                    Real diff = JVector.Dot(p - pB, normal);
                    mA[manifoldCount] = p;
                    mB[manifoldCount++] = p - diff * normal;

                    if (manifoldCount == 6) return;
                }
            }
        }

        mA[manifoldCount] = pA;
        mB[manifoldCount++] = pB;
    } // BuildManifold

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void BuildManifold<TA,TB>(TA shapeA, TB shapeB,
        in JVector pA, in JVector pB, in JVector normal) where TA : RigidBodyShape where TB : RigidBodyShape
    {
        BuildManifold(shapeA, shapeB, shapeA.RigidBody.Orientation, shapeB.RigidBody.Orientation,
            shapeA.RigidBody.Position, shapeB.RigidBody.Position, pA, pB, normal);
    }
}