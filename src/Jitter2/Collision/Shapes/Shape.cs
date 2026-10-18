/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
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
public abstract class Shape : IDynamicTreeProxy, IUpdatableBoundingBox, ISupportMappable, IRayCastable, ISweepTestable, IDistanceTestable, IDebugDrawable
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

    [ReferenceFrame(ReferenceFrame.World)]
    public abstract bool Sweep<T>(in T support, in JQuaternion orientation, in JVector position, in JVector sweep,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where T : ISupportMappable;

    [ReferenceFrame(ReferenceFrame.World)]
    public abstract bool Distance<T>(in T support, in JQuaternion orientation, in JVector position,
        out JVector pointA, out JVector pointB, out JVector normal, out Real distance)
        where T : ISupportMappable;

    /// <inheritdoc/>
    [ReferenceFrame(ReferenceFrame.Local)]
    public abstract void SupportMap(in JVector direction, out JVector result);

    /// <inheritdoc/>
    [ReferenceFrame(ReferenceFrame.Local)]
    public abstract void GetCenter(out JVector point);

    /// <summary>Draws the shape using the default visualization settings.</summary>
    public void DebugDraw(IDebugDrawer drawer) => DebugDraw(drawer, DebugDrawSettings.Default);

    /// <summary>Draws the shape as world-space triangles using the supplied settings.</summary>
    /// <remarks>
    /// Shapes with known surface geometry draw it directly. The default implementation approximates
    /// the support map on every call; its triangles may overlap or fold and have inconsistent winding.
    /// Drawers that only implement segments receive triangle edges through the default triangle method.
    /// </remarks>
    public virtual void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        var sink = new ShapeDebugDraw.DrawerSink(drawer, JQuaternion.Identity, JVector.Zero, settings.CurrentColor);
        DebugDrawLocal(ref sink, settings);
    }

    /// <summary>Appends debug triangles in the coordinate system used by <see cref="SupportMap"/>.</summary>
    /// <remarks>
    /// Override this method to supply custom surface geometry. Rigid-body and affine transforms are
    /// applied by the caller. Built-in closed surfaces use outward-facing winding; the support-map
    /// fallback preserves the triangles produced by <see cref="ShapeHelper.Tessellate{TSupport, TSink}(in TSupport, ref TSink, int)"/>.
    /// </remarks>
    /// <param name="triangleSink">The destination for generated triangles.</param>
    /// <param name="settings">The visualization settings.</param>
    protected internal virtual void DebugDrawLocal<TSink>(ref TSink triangleSink, in DebugDrawSettings settings)
        where TSink : ISink<JTriangle>
    {
        ShapeHelper.Tessellate(this, ref triangleSink, settings.ShapeSubdivisions);
    }
}
