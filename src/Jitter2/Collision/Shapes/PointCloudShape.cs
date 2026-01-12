/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using Jitter2.Internal;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// Represents a generic convex hull, similar to <see cref="ConvexHullShape"/>. The shape is
/// implicitly defined by a point cloud. It is not necessary for the points to lie on the convex hull.
/// For performance optimization, this shape should ideally be used for a small number of points (~300).
/// </summary>
public class PointCloudShape : RigidBodyShape, ICloneableShape<PointCloudShape>
{
    private JBoundingBox cachedBoundingBox;
    private JMatrix cachedInertia;
    private Real cachedMass;
    private JVector cachedCenter;

    private VertexSupportMap supportMap;
    private JVector shifted;

    /// <inheritdoc cref="PointCloudShape(ReadOnlySpan{JVector})"/>
    public PointCloudShape(IEnumerable<JVector> vertices) :
        this(GeometryInput.AsReadOnlySpan(vertices, out _))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PointCloudShape"/> class.
    /// </summary>
    /// <param name="vertices">All vertices that define the convex hull.</param>
    public PointCloudShape(ReadOnlySpan<JVector> vertices)
    {
        supportMap = new VertexSupportMap(vertices);
        UpdateShape();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PointCloudShape"/> class.
    /// </summary>
    public PointCloudShape(VertexSupportMap supportMap)
    {
        this.supportMap = supportMap;
        UpdateShape();
    }

    private PointCloudShape()
    {
    }

    /// <summary>
    /// Creates a copy of this shape. The underlying data structure is shared
    /// among the instances.
    /// </summary>
    public PointCloudShape Clone()
    {
        PointCloudShape result = new()
        {
            supportMap = supportMap,
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

    public void UpdateShape()
    {
        CalculateMassInertia();
        CalcInitBox();
    }

    public void CalculateMassInertia()
    {
        ShapeHelper.CalculateMassInertia(this, out cachedInertia, out cachedCenter, out cachedMass);
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out Real mass)
    {
        inertia = cachedInertia;
        com = cachedCenter;
        mass = cachedMass;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBoundingBox box)
    {
        JVector halfSize = (Real)0.5 * (cachedBoundingBox.Max - cachedBoundingBox.Min);
        JVector center = (Real)0.5 * (cachedBoundingBox.Max + cachedBoundingBox.Min);

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
        supportMap.SupportMap(direction, out result);
        result += shifted;
    }

    public override void GetCenter(out JVector point)
    {
        point = cachedCenter;
    }
}