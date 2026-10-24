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
/// Defines collision geometry in local coordinates.
/// </summary>
/// <remarks>
/// A shape has no position or orientation of its own. Shapes attached to a <see cref="RigidBody"/>
/// use that body's transform.
/// </remarks>
public abstract class Shape : IDynamicTreeProxy, IUpdatableBoundingBox, ISupportMappable, IRayCastable, IAngularSweepTestable, IDistanceTestable, IDebugDrawable
{
    int IPartitionedSetIndex.SetIndex { get; set; } = -1;

    /// <summary>
    /// A unique identifier for this shape.
    /// </summary>
    public readonly ulong ShapeId = World.RequestId();

    /// <summary>
    /// The bounding box of the shape in world space. It is automatically updated when the position or
    /// orientation of the corresponding instance of <see cref="RigidBody"/> changes.
    /// It may include the body's predicted translation and rotation over the next step.
    /// </summary>
    public JBoundingBox WorldBoundingBox { get; protected set; }

    int IDynamicTreeProxy.NodePtr { get; set; }

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

    /// <inheritdoc />
    [ReferenceFrame(ReferenceFrame.World)]
    public virtual bool Sweep<T>(in T support, in JQuaternion orientation, in JVector position,
        in JVector velocity, in JVector angularVelocity, Real extent, Real maximumTime,
        out JVector pointA, out JVector pointB, out JVector normal, out Real lambda)
        where T : ISupportMappable
    {
        ShapeHelper.CalculateBoundingBox(this, JQuaternion.Identity, JVector.Zero, out JBoundingBox box);
        Real targetExtent = JVector.Max(JVector.Abs(box.Min), JVector.Abs(box.Max)).Length();
        return NarrowPhase.Sweep(support, this, orientation, JQuaternion.Identity, position, JVector.Zero,
            velocity, JVector.Zero, angularVelocity, JVector.Zero, extent, targetExtent, maximumTime,
            out pointA, out pointB, out normal, out lambda) == NarrowPhase.SweepStatus.Hit;
    }

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
    /// Point clouds and custom shapes may use an approximate surface with gaps or overlapping faces.
    /// Higher detail settings increase the drawing cost and do not guarantee a more accurate surface.
    /// </remarks>
    public virtual void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
    {
        ArgumentNullException.ThrowIfNull(drawer);
        var sink = new ShapeDebugDraw.DrawerSink(drawer, JQuaternion.Identity, JVector.Zero, settings.CurrentColor);
        DebugDrawLocal(ref sink, settings);
    }

    /// <summary>Draws the shape's surface as triangles in local coordinates.</summary>
    /// <remarks>
    /// Override to provide custom debug geometry; otherwise, the surface is approximated.
    /// Use outward-facing triangles for closed surfaces. Body and shape transforms are applied automatically.
    /// </remarks>
    /// <param name="triangleSink">The destination for generated triangles.</param>
    /// <param name="settings">The visualization settings.</param>
    protected internal virtual void DebugDrawLocal<TSink>(ref TSink triangleSink, in DebugDrawSettings settings)
        where TSink : ISink<JTriangle>
    {
        ShapeHelper.Tessellate(this, ref triangleSink, settings.ShapeSubdivisions);
    }
}
