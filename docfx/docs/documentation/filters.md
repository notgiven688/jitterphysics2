# Collision Filters

Collision filters let you skip selected collisions or adjust contact information. They run at three stages:

| Filter | When it runs | Use it to |
| --- | --- | --- |
| `world.DynamicTree.Filter` | When the tree considers a pair of objects | Exclude pairs from collision detection. |
| `world.BroadPhaseFilter` | Before testing the shapes for contact | Apply collision groups or handle custom objects. |
| `world.NarrowPhaseFilter` | After contact points are found | Reject a collision or adjust its contact information. |

With multithreaded stepping, filter calls may run concurrently. Protect shared mutable data.

## Dynamic tree filter

`world.DynamicTree.Filter` has this type:

```cs
public Func<IDynamicTreeProxy, IDynamicTreeProxy, bool> Filter { get; set; }
```

Its default is `World.DefaultDynamicTreeFilter`, which excludes shapes attached to the same body:

```cs
public static bool DefaultDynamicTreeFilter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
{
    if (proxyA is RigidBodyShape rbsA && proxyB is RigidBodyShape rbsB)
    {
        return rbsA.RigidBody != rbsB.RigidBody;
    }

    return true;
}
```

Pairs rejected here are not passed to either of the later filters.

For soft bodies, another collision filter is typically used (defined in `SoftBodies.DynamicTreeCollisionFilter.Filter`), which also filters out collisions between shapes belonging to the same soft body.

## Broad phase filter

`world.BroadPhaseFilter` is optional:

```cs
public IBroadPhaseFilter? BroadPhaseFilter { get; set; }
```

The default is `null`. When assigned, it receives pairs whose bounding boxes overlap. Return `false` to skip the shape collision test.

If you add custom objects to `world.DynamicTree`, handle their collisions in this filter and return `false` for those pairs. The world's built-in collision detection only handles `RigidBodyShape` pairs; passing other types to it throws `InvalidCollisionTypeException`. Soft bodies use this approach through `SoftBodies.BroadPhaseCollisionFilter`.

### Example: Collision groups

This filter lets members of the same team collide, while ignoring collisions between red and blue. Bodies without a team collide normally:

```cs
public class TeamFilter : IBroadPhaseFilter
{
    public class TeamMember { }
    
    public static TeamMember TeamRed = new();
    public static TeamMember TeamBlue = new();
    
    public bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        if (proxyA is not RigidBodyShape shapeA || proxyB is not RigidBodyShape shapeB)
        {
            return true;
        }

        if (shapeA.RigidBody.Tag is not TeamMember || shapeB.RigidBody.Tag is not TeamMember)
        {
            // Handle collision normally if at least one body is not a member of any team
            return true;
        }

        // There is no collision between team red and team blue.
        return shapeA.RigidBody.Tag == shapeB.RigidBody.Tag;
    }
}
```

The `TeamFilter` class can then be instantiated and assigned to `world.BroadPhaseFilter`:

```cs
world.BroadPhaseFilter = new TeamFilter();
...
bodyA.Tag = TeamFilter.TeamBlue;
bodyB.Tag = TeamFilter.TeamRed;
bodyC.Tag = TeamFilter.TeamRed;
```

## Narrow phase filter

`world.NarrowPhaseFilter` receives the detected contact:

```cs
public INarrowPhaseFilter? NarrowPhaseFilter { get; set; }
```

The callback receives contact points, a normal, and penetration depth. Return `false` to reject the collision, or modify the points and normal before returning `true` to keep it. Changing penetration depth alone does not change the contact response.

The default, `TriangleEdgeCollisionFilter`, reduces snagging at edges shared by neighboring `TriangleShape`s. Without it, an object sliding across a flat mesh can catch on an edge between two triangles.

Static and kinematic triangle bodies receive edge filtering. For dynamic triangle bodies,
edge filtering is disabled by default to avoid destabilizing moving meshes. Enable it explicitly
with `FilterDynamicBodies`:

```cs
world.NarrowPhaseFilter = new TriangleEdgeCollisionFilter { FilterDynamicBodies = true };
```
