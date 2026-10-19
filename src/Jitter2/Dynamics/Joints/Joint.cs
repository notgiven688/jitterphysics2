/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System.Collections.Generic;
using Jitter2.DataStructures;

namespace Jitter2.Dynamics.Constraints;

/// <summary>Selects the optional drive created with a joint.</summary>
public enum JointDrive
{
    /// <summary>Creates the mechanical joint without a drive.</summary>
    None,

    /// <summary>Adds a motor that drives relative velocity.</summary>
    Motor,

    /// <summary>Adds a servo that drives toward a target position or angle.</summary>
    Servo
}

/// <summary>
/// Base class for joints, which are composite constraints built from multiple <see cref="Constraint"/> instances.
/// </summary>
public class Joint : IDebugDrawable
{
    private readonly List<Constraint> constraints = new(2);
    public ReadOnlyList<Constraint> Constraints => new(constraints);

    /// <summary>
    /// Adds a constraint to the internal bookkeeping.
    /// </summary>
    protected void Register(Constraint constraint) => constraints.Add(constraint);

    /// <summary>
    /// Removes a constraint from the internal bookkeeping.
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
        => DebugDraw(drawer, DebugDrawSettings.Default);

    /// <summary>Draws the constituent constraints, placing angular guides at the joint anchor when available.</summary>
    public virtual void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
    {
        foreach (var constraint in constraints)
        {
            if (!constraint.IsValid) continue;
            DebugDrawSettings jointSettings = settings;
            if (!settings.Origin.HasValue)
            {
                foreach (var candidate in constraints)
                {
                    if (candidate is not BallSocket socket || !socket.IsValid) continue;
                    if (socket.Body1 == constraint.Body1 && socket.Body2 == constraint.Body2)
                    {
                        jointSettings = settings with { Origin = socket.Anchor1 };
                        break;
                    }
                    if (socket.Body2 == constraint.Body1 && socket.Body1 == constraint.Body2)
                    {
                        jointSettings = settings with { Origin = socket.Anchor2 };
                        break;
                    }
                }
            }
            constraint.DebugDraw(drawer, jointSettings);
        }
    }
}
