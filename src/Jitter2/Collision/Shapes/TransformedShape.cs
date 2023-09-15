/*
 * Copyright (c) 2009-2023 Thorben Linneweber and others
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

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Wraps any shape and allows to orientate and translate it.
/// </summary>
public class TransformedShape : Shape
{
    private JVector translation;
    private JMatrix orientation;

    private readonly bool hasOrientation;

    /// <summary>
    /// Constructs a transformed shape with specified orientation and translation applied to the original shape.
    /// </summary>
    /// <param name="shape">The original shape to be transformed.</param>
    /// <param name="orientation">The orientation matrix to be applied to the shape.</param>
    /// <param name="translation">The translation vector to be applied to the shape.</param>
    public TransformedShape(Shape shape, JMatrix orientation, JVector translation)
    {
        OriginalShape = shape;
        this.translation = translation;
        this.orientation = orientation;
        hasOrientation = !orientation.Equals(JMatrix.Identity);
        UpdateShape();
    }

    public TransformedShape(Shape shape, JVector translation)
    {
        OriginalShape = shape;
        this.translation = translation;
        orientation = JMatrix.Identity;
        hasOrientation = false;
        UpdateShape();
    }

    public Shape OriginalShape { get; }

    public JVector Translation
    {
        get => translation;
        set
        {
            translation = value;
            UpdateShape();
        }
    }

    public JMatrix Orientation
    {
        get => orientation;
        set
        {
            orientation = value;
            UpdateShape();
        }
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        if (hasOrientation)
        {
            JVector.TransposedTransform(direction, orientation, out JVector dir);
            OriginalShape.SupportMap(dir, out JVector sm);
            JVector.Transform(sm, orientation, out result);
            result += translation;
        }
        else
        {
            OriginalShape.SupportMap(direction, out result);
            result += translation;
        }
    }

    public override void CalculateBoundingBox(in JMatrix orientation, in JVector position, out JBBox box)
    {
        OriginalShape.CalculateBoundingBox(orientation * this.orientation,
            JVector.Transform(translation, orientation) + position, out box);
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out float mass)
    {
        OriginalShape.AttachRigidBody(RigidBody);
        OriginalShape.CalculateMassInertia(out inertia, out com, out mass);

        com = JVector.Transform(com, orientation) + translation;

        inertia = orientation * JMatrix.Multiply(inertia, JMatrix.Transpose(orientation));

        JMatrix pat = Mass * (JMatrix.Identity * translation.LengthSquared() - JVector.Outer(translation, translation));

        inertia += pat;
    }
}