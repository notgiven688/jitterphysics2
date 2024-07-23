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

using System.Collections.Generic;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a generic convex hull, similar to <see cref="ConvexHullShape"/>. The shape is
/// implicitly defined by a point cloud. It is not necessary for the points to lie on the convex hull.
/// For performance optimization, this shape should ideally be used for a small number of points (maximum
/// of 20-30).
/// </summary>
public class PointCloudShape : Shape
{

    private JBBox cachedBoundingBox;
    private JMatrix cachedInertia;
    private float cachedMass;
    private JVector cachedCenter;

    private List<JVector> vertices;
    private JVector shifted;

    /// <summary>
    /// Initializes a new instance of the <see cref="PointCloudShape"/> class.
    /// </summary>
    /// <param name="vertices">
    /// A list containing all vertices that define the convex hull. The list is referenced and should
    /// not be modified after passing it to the constructor.
    /// </param>
    public PointCloudShape(List<JVector> vertices)
    {
        this.vertices = vertices;
        UpdateShape();
    }

    private PointCloudShape()
    {
        vertices = null!;
    }

    /// <summary>
    /// Creates a copy of this shape. The underlying data structure is shared
    /// among the instances.
    /// </summary>
    public PointCloudShape Clone()
    {
        PointCloudShape result = new()
        {
            vertices = vertices,
            cachedBoundingBox = cachedBoundingBox,
            cachedCenter = cachedCenter,
            cachedInertia = cachedInertia,
            cachedMass = cachedMass,
            shifted = shifted
        };
        return result;
    }

    /// <summary>
    /// Gets or sets the shift value for the shape. This property can be used when constructing a rigid
    /// body that contains one or more shapes.
    /// </summary>
    public JVector Shift
    {
        get => shifted;
        set
        {
            shifted = value;
            UpdateShape();
        }
    }

    public override void UpdateShape()
    {
        CalculateMassInertia();
        CalcInitBox();
        base.UpdateShape();
    }

    public void CalculateMassInertia()
    {
        ShapeHelper.CalculateMassInertia(this, out cachedInertia, out cachedCenter, out cachedMass); 
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out float mass)
    {
        inertia = cachedInertia;
        com = cachedCenter;
        mass = cachedMass;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        JVector halfSize = 0.5f * (cachedBoundingBox.Max - cachedBoundingBox.Min);
        JVector center = 0.5f * (cachedBoundingBox.Max + cachedBoundingBox.Min);

        JMatrix ori = JMatrix.CreateFromQuaternion(orientation);
        JMatrix.Absolute(in ori, out JMatrix abs);
        JVector.Transform(halfSize, abs, out JVector temp);
        JVector.Transform(center, orientation, out JVector temp2);

        box.Max = temp;
        JVector.Negate(temp, out box.Min);

        JVector.Add(box.Min, position + temp2, out box.Min);
        JVector.Add(box.Max, position + temp2, out box.Max);
    }

    private void CalcInitBox()
    {
        JVector vec = JVector.UnitX;
        SupportMap(vec, out JVector res);
        cachedBoundingBox.Max.X = res.X;

        vec = JVector.UnitY;
        SupportMap(vec, out res);
        cachedBoundingBox.Max.Y = res.Y;

        vec = JVector.UnitZ;
        SupportMap(vec, out res);
        cachedBoundingBox.Max.Z = res.Z;

        vec = -JVector.UnitX;
        SupportMap(vec, out res);
        cachedBoundingBox.Min.X = res.X;

        vec = -JVector.UnitY;
        SupportMap(vec, out res);
        cachedBoundingBox.Min.Y = res.Y;

        vec = -JVector.UnitZ;
        SupportMap(vec, out res);
        cachedBoundingBox.Min.Z = res.Z;
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        float maxDotProduct = float.MinValue;
        int maxIndex = 0;
        float dotProduct;

        for (int i = 0; i < vertices.Count; i++)
        {
            dotProduct = JVector.Dot(vertices[i], direction);
            if (dotProduct > maxDotProduct)
            {
                maxDotProduct = dotProduct;
                maxIndex = i;
            }
        }

        result = vertices[maxIndex] + shifted;
    }

    public override void PointWithin(out JVector point)
    {
        point = cachedCenter;
    }

}