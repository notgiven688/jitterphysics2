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
using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.Collision;

public interface IUpdatableBoundingBox
{
    public void UpdateWorldBoundingBox(float dt);
}

public interface IRayCastable
{
    public bool RayCast(in JVector origin, in JVector direction, out JVector normal, out float lambda);
}

/// <summary>
/// The main entity of the collision system. Implements <see cref="ISupportMappable"/> for
/// narrow-phase and <see cref="IDynamicTreeProxy"/> for broad-phase collision detection.
/// The shape itself does not have a position or orientation. Shapes can be associated with
/// instances of <see cref="RigidBody"/>.
/// </summary>
public abstract class Shape : IDynamicTreeProxy, IUpdatableBoundingBox, ISupportMappable, IRayCastable
{
    int IListIndex.ListIndex { get; set; } = -1;

    /// <summary>
    /// A 64-bit integer representing the shape ID. This is used by algorithms that require
    /// arranging shapes in a well-defined order.
    /// </summary>
    public readonly ulong ShapeId = World.RequestId();

    /// <summary>
    /// The bounding box of the shape in world space. It is automatically updated when the position or
    /// orientation of the corresponding instance of <see cref="RigidBody"/> changes.
    /// </summary>
    public JBBox WorldBoundingBox { get; protected set; }

    int IDynamicTreeProxy.NodePtr { get; set; }

    protected void SweptExpandBoundingBox(float dt)
    {
        JVector sweptDirection = dt * Velocity;

        JBBox box = WorldBoundingBox;

        float sxa = MathF.Abs(sweptDirection.X);
        float sya = MathF.Abs(sweptDirection.Y);
        float sza = MathF.Abs(sweptDirection.Z);

        float max = MathF.Max(MathF.Max(sxa, sya), sza);

        if (sweptDirection.X < 0.0f) box.Min.X -= max;
        else box.Max.X += max;

        if (sweptDirection.Y < 0.0f) box.Min.Y -= max;
        else box.Max.Y += max;

        if (sweptDirection.Z < 0.0f) box.Min.Z -= max;
        else box.Max.Z += max;

        WorldBoundingBox = box;
    }

    public bool IsRegistered => (this as IListIndex).ListIndex != -1;

    [ReferenceFrame(ReferenceFrame.World)] public abstract JVector Velocity { get; }

    public abstract void UpdateWorldBoundingBox(float dt = 0.0f);

    public abstract bool RayCast(in JVector origin, in JVector direction, out JVector normal, out float lambda);

    /// <inheritdoc/>
    [ReferenceFrame(ReferenceFrame.Local)]
    public abstract void SupportMap(in JVector direction, out JVector result);

    /// <inheritdoc/>
    [ReferenceFrame(ReferenceFrame.Local)]
    public abstract void PointWithin(out JVector point);
}