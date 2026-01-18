/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Wraps any shape and allows to orientate and translate it.
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
    public TransformedShape(RigidBodyShape shape, in JVector translation, in JMatrix transform)
    {
        OriginalShape = shape;
        this.translation = translation;
        this.transformation = transform;

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

    public RigidBodyShape OriginalShape { get; }

    public JVector Translation
    {
        get => translation;
        set
        {
            translation = value;
            UpdateWorldBoundingBox();
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

    public JMatrix Transformation
    {
        get => transformation;
        set
        {
            transformation = value;
            AnalyzeTransformation();
            UpdateWorldBoundingBox();
        }
    }

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

    public override void GetCenter(out JVector point)
    {
        OriginalShape.GetCenter(out point);
        point = JVector.Transform(point, transformation) + translation;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        OriginalShape.CalculateMassInertia(out JMatrix originalInertia, out JVector originalCom, out mass);

        com = JVector.Transform(originalCom, transformation) + translation;

        Real det = MathR.Abs(transformation.Determinant());
        mass *= det;

        // The inertia tensor I is related to the second moment matrix C by: I = trace(C)·E - C
        // Under transformation T, the second moment transforms as: C' = |det(T)| · T · C · Tᵀ
        // We recover C from I: C = (trace(I)/2)·E - I
        Real halfTrace = originalInertia.Trace() * (Real)0.5;
        JMatrix secondMoment = halfTrace * JMatrix.Identity - originalInertia;

        // Transform second moment matrix
        JMatrix transformedSecondMoment = det * transformation * secondMoment * JMatrix.Transpose(transformation);

        // Convert back to inertia tensor
        inertia = transformedSecondMoment.Trace() * JMatrix.Identity - transformedSecondMoment;

        // Apply parallel axis theorem for translation
        JMatrix pat = mass * (JMatrix.Identity * com.LengthSquared() - JVector.Outer(com, com));
        inertia += pat;
    }
}