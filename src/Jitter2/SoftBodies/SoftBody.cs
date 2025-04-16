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
    public List<RigidBody> Vertices { get; } = new();

    /// <summary>
    /// Gets the list of springs (constraints) that connect the vertices of the soft body.
    /// </summary>
    public List<Constraint> Springs { get; } = new();

    /// <summary>
    /// Gets the list of shapes that define the geometry of the soft body.
    /// </summary>
    public List<SoftBodyShape> Shapes { get; } = new();

    protected World World;

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
                World.DynamicTree.Activate(shape);
            }
            else
            {
                World.DynamicTree.Deactivate(shape);
            }
        }
    }
}