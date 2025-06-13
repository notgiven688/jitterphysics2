/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Generic;
using Jitter2.DataStructures;

namespace Jitter2.Dynamics.Constraints;

public class Joint : IDebugDrawable
{
    private readonly List<Constraint> constraints = new(4);
    public ReadOnlyList<Constraint> Constraints => new (constraints);

    /// <summary>
    /// Add a constraint to the internal bookkeeping
    /// </summary>
    protected void Register(Constraint constraint) => constraints.Add(constraint);

    /// <summary>
    /// Remove a constraint from the internal bookkeeping
    /// </summary>
    protected void Deregister(Constraint constraint) => constraints.Remove(constraint);

    /// <summary>
    /// Enables all constraints that this joint is composed of.
    /// </summary>
    public void Enable()
    {
        foreach (var constraint in constraints)
        {
            if (constraint.Handle.IsZero) continue;
            constraint.IsEnabled = true;
        }
    }

    /// <summary>
    /// Disables all constraints that this joint is composed of temporarily.
    /// For a complete removal use <see cref="Joint.Remove()"/>.
    /// </summary>
    public void Disable()
    {
        foreach (var constraint in constraints)
        {
            if (constraint.Handle.IsZero) continue;
            constraint.IsEnabled = false;
        }
    }

    /// <summary>
    /// Removes all constraints that this joint is composed of from the physics world.
    /// </summary>
    public void Remove()
    {
        foreach (var constraint in constraints)
        {
            if (constraint.Handle.IsZero) continue;
            constraint.Body1.World.Remove(constraint);
        }

        constraints.Clear();
    }

    public virtual void DebugDraw(IDebugDrawer drawer)
    {
        foreach (var constraint in constraints)
        {
            constraint.DebugDraw(drawer);
        }
    }
}