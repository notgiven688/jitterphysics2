/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Jitter2.DataStructures;

namespace Jitter2.Dynamics.Constraints;

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
            if (DrawingDispatch.Get(constraint).UseSingleArgumentOverload) constraint.DebugDraw(drawer);
            else constraint.DebugDraw(drawer, jointSettings);
        }
    }

    private sealed class DrawingDispatch
    {
        private static readonly ConditionalWeakTable<Type, DrawingDispatch> cache = new();
        private delegate void ConfiguredDraw(IDebugDrawer drawer, in DebugDrawSettings settings);
        public readonly bool UseSingleArgumentOverload;

        private DrawingDispatch(Constraint constraint)
        {
            Action<IDebugDrawer> singleArgument = constraint.DebugDraw;
            ConfiguredDraw configured = constraint.DebugDraw;
            // The delegates resolve the virtual overrides, including those inherited by subclasses.
            // Prefer a more-derived single-argument override unless a settings override supersedes it.
            UseSingleArgumentOverload = singleArgument.Method.DeclaringType!.IsSubclassOf(configured.Method.DeclaringType!);
        }

        public static DrawingDispatch Get(Constraint constraint)
        {
            Type type = constraint.GetType();
            if (cache.TryGetValue(type, out DrawingDispatch? dispatch)) return dispatch;
            lock (cache)
            {
                if (!cache.TryGetValue(type, out dispatch))
                {
                    dispatch = new DrawingDispatch(constraint);
                    cache.Add(type, dispatch);
                }
            }
            return dispatch;
        }
    }
}
