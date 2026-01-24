/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

/// <summary>
/// Represents a contact manifold between two convex shapes, storing up to six contact points.
/// </summary>
/// <remarks>
/// The manifold is constructed by projecting support points along perturbed normals
/// in a hexagonal pattern around the collision normal, then clipping to find the contact region.
/// </remarks>
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

    /// <summary>
    /// Gets a span of contact points on shape A. Valid indices are <c>[0, Count)</c>.
    /// </summary>
    public Span<JVector> ManifoldA => MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[0]), 6);

    /// <summary>
    /// Gets a span of contact points on shape B. Valid indices are <c>[0, Count)</c>.
    /// </summary>
    public Span<JVector> ManifoldB => MemoryMarshal.CreateSpan(ref Unsafe.As<Real, JVector>(ref manifoldData[18]), 6);

    /// <summary>
    /// Gets the number of contact points in the manifold.
    /// </summary>
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

    /// <summary>
    /// Builds the contact manifold between two shapes given their transforms and initial contact.
    /// </summary>
    /// <typeparam name="TA">The type of support shape A.</typeparam>
    /// <typeparam name="TB">The type of support shape B.</typeparam>
    /// <param name="shapeA">The first shape.</param>
    /// <param name="shapeB">The second shape.</param>
    /// <param name="quaternionA">Orientation of shape A.</param>
    /// <param name="quaternionB">Orientation of shape B.</param>
    /// <param name="positionA">Position of shape A.</param>
    /// <param name="positionB">Position of shape B.</param>
    /// <param name="pA">Initial contact point on shape A.</param>
    /// <param name="pB">Initial contact point on shape B.</param>
    /// <param name="normal">The collision normal (from B to A).</param>
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

    /// <summary>
    /// Builds the contact manifold between two rigid body shapes using their current transforms.
    /// </summary>
    /// <typeparam name="TA">The type of shape A.</typeparam>
    /// <typeparam name="TB">The type of shape B.</typeparam>
    /// <param name="shapeA">The first rigid body shape.</param>
    /// <param name="shapeB">The second rigid body shape.</param>
    /// <param name="pA">Initial contact point on shape A.</param>
    /// <param name="pB">Initial contact point on shape B.</param>
    /// <param name="normal">The collision normal (from B to A).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public void BuildManifold<TA,TB>(TA shapeA, TB shapeB,
        in JVector pA, in JVector pB, in JVector normal) where TA : RigidBodyShape where TB : RigidBodyShape
    {
        BuildManifold(shapeA, shapeB, shapeA.RigidBody.Orientation, shapeB.RigidBody.Orientation,
            shapeA.RigidBody.Position, shapeB.RigidBody.Position, pA, pB, normal);
    }
}