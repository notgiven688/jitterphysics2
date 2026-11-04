using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace JitterDemo;

/// <summary>
/// A spherical region that reports the rigid bodies inside it without creating contacts.
/// It registers itself in the world's dynamic tree as a custom proxy, so the broad phase pairs it
/// with every shape whose bounding box overlaps its own; <see cref="TriggerFilter"/> claims those
/// pairs. After each step it raises <see cref="Entered"/> and <see cref="Left"/>.
/// </summary>
public class TriggerVolume : IDynamicTreeProxy
{
    public int SetIndex { get; set; } = -1;
    public int NodePtr { get; set; }

    public JVector Velocity => JVector.Zero;
    public JBoundingBox WorldBoundingBox { get; }

    public JVector Center { get; }
    public float Radius { get; }

    /// <summary>Raised after a step for every body that was not inside during the previous step.</summary>
    public event Action<RigidBody>? Entered;

    /// <summary>
    /// Raised after a step for every body that was inside during the previous step but is no longer.
    /// Not raised for a body removed from the world.
    /// </summary>
    public event Action<RigidBody>? Left;

    private readonly World world;
    private readonly SupportPrimitives.Sphere sphere;
    private HashSet<RigidBody> inside = [];
    private HashSet<RigidBody> seen = [];

    public TriggerVolume(World world, JVector center, float radius)
    {
        this.world = world;
        Center = center;
        Radius = radius;
        WorldBoundingBox = new JBoundingBox(center - new JVector(radius), center + new JVector(radius));
        sphere = SupportPrimitives.CreateSphere(radius);

        // The volume never moves, so it is an inactive proxy and costs nothing per step. The tree then
        // drops its pairs with sleeping bodies; OnPostStep keeps those bodies inside until they wake.
        world.DynamicTree.AddProxy(this, active: false);
        world.PostStep += OnPostStep;
    }

    /// <summary>Removes the volume from the world. Safe to call after the world has been cleared.</summary>
    public void Remove()
    {
        world.PostStep -= OnPostStep;
        if (world.DynamicTree.Proxies.Contains(this)) world.DynamicTree.RemoveProxy(this);
        inside.Clear();
        seen.Clear();
    }

    /// <summary>The number of bodies inside the volume as of the last step.</summary>
    public int Count => inside.Count;

    /// <summary>Exact overlap test with a shape whose bounding box already overlaps the volume.</summary>
    public bool Overlaps(RigidBodyShape shape)
    {
        RigidBody body = shape.RigidBody;
        return NarrowPhase.Overlap(sphere, shape, JQuaternion.Identity, body.Orientation, Center, body.Position);
    }

    /// <summary>Records a body for this step. Called from the collision detection threads.</summary>
    public void Collect(RigidBody body)
    {
        lock (seen) seen.Add(body);
    }

    // Collision detection has run by now: compare this step's bodies with the previous step's.
    private void OnPostStep(TimeStep timeStep)
    {
        foreach (RigidBody body in seen)
        {
            if (!inside.Contains(body)) Entered?.Invoke(body);
        }

        foreach (RigidBody body in inside)
        {
            if (seen.Contains(body)) continue;

            // A body removed from the world leaves without an event; its data is gone.
            if (!world.RigidBodies.Contains(body)) continue;

            // The tree drops pairs of two inactive proxies, so a sleeping body is not reported. A body
            // that woke during this step is not reported either: the tree adds its pair back next step.
            // Neither has moved, so test it here.
            if (StillOverlaps(body)) seen.Add(body);
            else Left?.Invoke(body);
        }

        (inside, seen) = (seen, inside);
        seen.Clear();
    }

    private bool StillOverlaps(RigidBody body)
    {
        foreach (RigidBodyShape shape in body.Shapes)
        {
            // A sleeping body was inside when it fell asleep and has not moved since.
            if (!world.DynamicTree.IsActive(shape)) return true;
            if (Overlaps(shape)) return true;
        }

        return false;
    }
}

/// <summary>
/// Claims every broad phase pair that involves a <see cref="TriggerVolume"/>: the volume records the
/// body if the shapes really overlap, and the pair never reaches the narrow phase. Ordinary pairs of
/// rigid body shapes pass through.
/// </summary>
public class TriggerFilter : IBroadPhaseFilter
{
    public bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        TriggerVolume? trigger = proxyA as TriggerVolume;
        IDynamicTreeProxy other = proxyB;

        if (trigger == null)
        {
            trigger = proxyB as TriggerVolume;
            other = proxyA;
        }

        if (trigger == null) return true;

        // Static bodies never enter a volume; the floor touches every volume standing on it.
        if (other is RigidBodyShape { RigidBody.MotionType: not MotionType.Static } shape && trigger.Overlaps(shape))
        {
            trigger.Collect(shape.RigidBody);
        }

        return false;
    }
}
