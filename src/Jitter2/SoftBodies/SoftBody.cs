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

    /// <summary>
    /// Gets the world in which the soft body exists.
    /// </summary>
    public World World { get; }

    /// <summary>
    /// Gets a value indicating whether the soft body is active. A soft body is considered active
    /// if its first vertex is still valid and active.
    /// </summary>
    public bool IsActive => Vertices.Count > 0 && Vertices[0].IsValid && Vertices[0].IsActive;

    /// <summary>
    /// Initializes a new instance of the <see cref="SoftBody"/> class.
    /// </summary>
    /// <param name="world">The world in which the soft body will be created.</param>
    public SoftBody(World world)
    {
        World = world;
        world.PostStep += WorldOnPostStep;
    }

    /// <summary>
    /// Adds a shape to the soft body and registers it with the world's dynamic tree.
    /// </summary>
    /// <param name="shape">The shape to add.</param>
    public void AddShape(SoftBodyShape shape)
    {
        World.DynamicTree.AddProxy(shape);
        Shapes.Add(shape);
    }

    /// <summary>
    /// Adds a spring (constraint) to the soft body.
    /// </summary>
    /// <param name="constraint">The constraint to add.</param>
    public void AddSpring(Constraint constraint)
    {
        Springs.Add(constraint);
    }

    /// <summary>
    /// Destroys the soft body, removing all its components from the simulation world.
    /// Safe to call more than once, including after the world has been cleared or disposed.
    /// </summary>
    public virtual void Destroy()
    {
        World.PostStep -= WorldOnPostStep;

        if (!World.IsDisposed)
        {
            foreach (var shape in Shapes)
            {
                if (World.DynamicTree.Proxies.Contains(shape)) World.DynamicTree.RemoveProxy(shape);
            }

            foreach (var spring in Springs)
            {
                if (spring.IsValid) World.Remove(spring);
            }

            foreach (var point in Vertices)
            {
                if (point.IsValid) World.Remove(point);
            }
        }

        Shapes.Clear();
        Springs.Clear();
        Vertices.Clear();
        active = false;
    }

    private bool active = true;

    /// <summary>
    /// Called after each world step to update the activation state of the soft body's shapes.
    /// </summary>
    /// <param name="timeStep">The full step and substep timing information.</param>
    protected virtual void WorldOnPostStep(TimeStep timeStep)
    {
        // World.Clear removes the vertices without going through SoftBody.Destroy.
        if (Vertices.Count > 0 && !Vertices[0].IsValid)
        {
            Destroy();
            return;
        }

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
