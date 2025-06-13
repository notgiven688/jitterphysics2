/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Generic;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;

namespace Jitter2.SoftBodies;

/// <summary>
/// Represents a soft body in the physics simulation. A soft body is composed of vertices (rigid bodies),
/// springs (constraints), and shapes.
/// </summary>
public class SoftBody
{
    /// <summary>
    /// Gets the list of vertices (rigid bodies) that make up the soft body.
    /// </summary>
    public List<RigidBody> Vertices { get; } = [];

    /// <summary>
    /// Gets the list of springs (constraints) that connect the vertices of the soft body.
    /// </summary>
    public List<Constraint> Springs { get; } = [];

    /// <summary>
    /// Gets the list of shapes that define the geometry of the soft body.
    /// </summary>
    public List<SoftBodyShape> Shapes { get; } = [];

    public World World { get; }

    public bool IsActive => Vertices.Count > 0 && Vertices[0].IsActive;

    public SoftBody(World world)
    {
        this.World = world;
        world.PostStep += WorldOnPostStep;
    }

    /// <summary>
    /// Destroys the soft body, removing all its components from the simulation world.
    /// </summary>
    public virtual void Destroy()
    {
        World.PostStep -= WorldOnPostStep;

        foreach (var shape in Shapes)
            World.DynamicTree.RemoveProxy(shape);
        Shapes.Clear();

        foreach (var spring in Springs)
            World.Remove(spring);
        Springs.Clear();

        foreach (var point in Vertices)
            World.Remove(point);
        Vertices.Clear();
    }

    private bool active = true;

    protected virtual void WorldOnPostStep(Real dt)
    {
        if (IsActive == active) return;
        active = IsActive;

        foreach (var shape in Shapes)
        {
            if (active)
            {
                World.DynamicTree.ActivateProxy(shape);
            }
            else
            {
                World.DynamicTree.DeactivateProxy(shape);
            }
        }
    }
}