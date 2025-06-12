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

using Jitter2.DataStructures;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

/// <summary>
/// The main entity of the collision system. Implements <see cref="ISupportMappable"/> for
/// narrow-phase and <see cref="IDynamicTreeProxy"/> for broad-phase collision detection.
/// The shape itself does not have a position or orientation. Shapes can be associated with
/// instances of <see cref="RigidBody"/>.
/// </summary>
public abstract class Shape : IDynamicTreeProxy, IUpdatableBoundingBox, ISupportMappable, IRayCastable
{
    int IPartitionedSetIndex.SetIndex { get; set; } = -1;

    /// <summary>
    /// A 64-bit integer representing the shape ID. This is used by algorithms that require
    /// arranging shapes in a well-defined order.
    /// </summary>
    public readonly ulong ShapeId = World.RequestId();

    /// <summary>
    /// The bounding box of the shape in world space. It is automatically updated when the position or
    /// orientation of the corresponding instance of <see cref="RigidBody"/> changes.
    /// </summary>
    public JBoundingBox WorldBoundingBox { get; protected set; }

    int IDynamicTreeProxy.NodePtr { get; set; }

    protected void SweptExpandBoundingBox(Real dt)
    {
        Real swept = dt * Velocity.Length();
        JBoundingBox box = WorldBoundingBox;
        box.Min -= new JVector(swept);
        box.Max += new JVector(swept);
        WorldBoundingBox = box;
    }

    internal bool IsRegistered => (this as IPartitionedSetIndex).SetIndex != -1;

    [ReferenceFrame(ReferenceFrame.World)]
    public abstract JVector Velocity { get; }

    [ReferenceFrame(ReferenceFrame.World)]
    public abstract void UpdateWorldBoundingBox(Real dt = (Real)0.0);

    [ReferenceFrame(ReferenceFrame.World)]
    public abstract bool RayCast(in JVector origin, in JVector direction, out JVector normal, out Real lambda);

    /// <inheritdoc/>
    [ReferenceFrame(ReferenceFrame.Local)]
    public abstract void SupportMap(in JVector direction, out JVector result);

    /// <inheritdoc/>
    [ReferenceFrame(ReferenceFrame.Local)]
    public abstract void GetCenter(out JVector point);
}