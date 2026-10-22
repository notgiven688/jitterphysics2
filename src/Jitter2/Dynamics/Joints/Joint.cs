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
/// Groups constraints into a joint that can be enabled, disabled, removed, or drawn as a whole.
/// </summary>
public class Joint : IDebugDrawable
{
    private readonly List<Constraint> constraints = new(2);
    /// <summary>Gets the constraints that make up this joint.</summary>
    public ReadOnlyList<Constraint> Constraints => new(constraints);

    /// <summary>
    /// Includes a constraint in this joint. The constraint must already belong to a world.
    /// </summary>
    protected void Register(Constraint constraint) => constraints.Add(constraint);

    /// <summary>
    /// Excludes a constraint from this joint without removing it from its world.
    /// </summary>
    protected void Deregister(Constraint constraint) => constraints.Remove(constraint);

    /// <summary>
    /// Enables all constraints in this joint.
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
    /// Temporarily disables all constraints in this joint.
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
    /// Removes all constraints in this joint from their world.
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

    /// <summary>Draws the joint's constraints, using the joint anchor for angular guides when available.</summary>
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
