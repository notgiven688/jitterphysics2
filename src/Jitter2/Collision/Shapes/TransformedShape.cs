/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a shape wrapper defined by an original shape and an affine transformation (translation and linear map).
/// </summary>
public class TransformedShape : RigidBodyShape
{
    private enum TransformationType
    {
        Identity,
        Rotation,
        General
    }

    private JVector translation;
    private JMatrix transformation;
    private TransformationType type;

    /// <summary>
    /// Constructs a transformed shape through an affine transformation defined by
    /// a linear map and a translation.
    /// </summary>
    /// <param name="shape">The shape to transform.</param>
    /// <param name="translation">The translation component of the affine transform.</param>
    /// <param name="transform">The linear component of the affine transform.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="shape"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="translation"/> or <paramref name="transform"/> contains a non-finite value.
    /// </exception>
    public TransformedShape(RigidBodyShape shape, in JVector translation, in JMatrix transform)
    {
        ArgumentNullException.ThrowIfNull(shape);

        OriginalShape = shape;
        this.translation = ArgumentCheck.Finite(translation, nameof(translation));
        this.transformation = ArgumentCheck.Finite(transform, nameof(transform));

        AnalyzeTransformation();
        UpdateWorldBoundingBox();
    }

    /// <summary>
    /// Constructs a transformed shape with a translation (offset), assuming identity rotation/scale.
    /// </summary>
    public TransformedShape(RigidBodyShape shape, JVector translation) :
        this(shape, translation, JMatrix.Identity)
    {
    }

    /// <summary>
    /// Constructs a transformed shape with a linear transformation (rotation, scale, or shear),
    /// assuming zero translation.
    /// </summary>
    public TransformedShape(RigidBodyShape shape, JMatrix transform) :
        this(shape, JVector.Zero, transform)
    {
    }

    /// <summary>
    /// Gets the original shape that is being transformed.
    /// </summary>
    public RigidBodyShape OriginalShape { get; }

    /// <summary>
    /// Gets or sets the translation applied to the shape.
    /// </summary>
    public JVector Translation
    {
        get => translation;
        set
        {
            DebugCheck.IsFinite(value, nameof(value));
            translation = value;
            ShapeChanged();
        }
    }

    private void AnalyzeTransformation()
    {
        if (MathHelper.IsRotationMatrix(transformation))
        {
            JMatrix delta = transformation - JMatrix.Identity;
            type = MathHelper.UnsafeIsZero(ref delta) ? TransformationType.Identity : TransformationType.Rotation;
        }
        else
        {
            type = TransformationType.General;
        }
    }

    /// <summary>
    /// Gets or sets the linear transformation (rotation, scale, or shear) applied to the shape.
    /// </summary>
    public JMatrix Transformation
    {
        get => transformation;
        set
        {
            DebugCheck.IsFinite(value, nameof(value));
            transformation = value;
            AnalyzeTransformation();
            ShapeChanged();
        }
    }

    /// <inheritdoc/>
    public override void SupportMap(in JVector direction, out JVector result)
    {
        if (type == TransformationType.Identity)
        {
            OriginalShape.SupportMap(direction, out result);
            result += translation;
        }
        else
        {
            JVector.TransposedTransform(direction, transformation, out JVector dir);
            OriginalShape.SupportMap(dir, out JVector sm);
            JVector.Transform(sm, transformation, out result);
            result += translation;
        }
    }

    /// <inheritdoc/>
    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        if (type == TransformationType.General)
        {
            // just get the bounding box from the support map
            base.CalculateBoundingBox(orientation, position, out box);
        }
        else
        {
            JQuaternion quat = JQuaternion.CreateFromMatrix(transformation);
            OriginalShape.CalculateBoundingBox(orientation * quat,
                JVector.Transform(translation, orientation) + position, out box);
        }
    }

    /// <inheritdoc/>
    public override void GetCenter(out JVector point)
    {
        OriginalShape.GetCenter(out point);
        point = JVector.Transform(point, transformation) + translation;
    }

    /// <inheritdoc/>
    public override void CalculateMassInertia(out JSymmetricMatrix inertia, out JVector com, out Real mass)
    {
        OriginalShape.CalculateMassInertia(out JSymmetricMatrix originalInertia, out JVector originalCom, out mass);

        // The source inertia is about its local origin. Move it to the center of mass
        // before transforming, so an existing offset is not counted a second time.
        JSymmetricMatrix originalParallelAxis = mass *
            (JSymmetricMatrix.Identity * originalCom.LengthSquared() - JSymmetricMatrix.Outer(originalCom));
        JSymmetricMatrix centralInertia = originalInertia - originalParallelAxis;

        com = JVector.Transform(originalCom, transformation) + translation;

        Real det = MathR.Abs(transformation.Determinant());
        mass *= det;

        // The inertia tensor I is related to the second moment matrix C by: I = trace(C)·E - C
        // Under transformation T, the central second moment transforms as: C' = |det(T)| · T · C · Tᵀ
        // We recover C from I: C = (trace(I)/2)·E - I
        Real halfTrace = centralInertia.Trace() * (Real)0.5;
        JSymmetricMatrix secondMoment = halfTrace * JSymmetricMatrix.Identity - centralInertia;

        // Transform second moment matrix
        JSymmetricMatrix transformedSecondMoment = det * JSymmetricMatrix.Transform(secondMoment, transformation);

        // Convert back to inertia tensor
        inertia = transformedSecondMoment.Trace() * JSymmetricMatrix.Identity - transformedSecondMoment;

        // Move the transformed inertia from its center of mass to the local origin.
        JSymmetricMatrix pat = mass * (JSymmetricMatrix.Identity * com.LengthSquared() - JSymmetricMatrix.Outer(com));
        inertia += pat;
    }
}
