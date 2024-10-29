# Collision Filters

There are three types of collision filters in Jitter: `world.DynamicTree.Filter`, `world.BroadPhaseFilter` and `world.NarrowPhaseFilter`.

## Dynamic tree filter

The `world.DynamicTree.Filter`

```cs
public Func<IDynamicTreeProxy, IDynamicTreeProxy, bool> Filter { get; set; }
```

is the earliest filter applied during a `world.Step` and set by default to `World.DefaultDynamicTreeFilter`:

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

This filters out collisions between shapes that belong to the same body.
The dynamic tree will ignore these collisions, and no potential pairs will be created.

For soft bodies, another collision filter is typically used (defined in `SoftBodies.DynamicTreeCollisionFilter.Filter`), which also filters out collisions between shapes belonging to the same soft body.

## Broad phase filter

By default `world.BroadPhaseFilter`

```cs
public IBroadPhaseFilter? BroadPhaseFilter { get; set; }
```

is `null`. It is used to filter out collisions that passed broad phase collision detection - that is, after the `DynamicTree` has added the collision to the `PotentialPair` hash set.

This can be useful if custom collision proxies got added to `world.DynamicTree`.
Since the Jitter `world` only knows how to handle collisions between `RigidBodyShape`s, a filter must handle the detected collision (i.e. implement custom collision response code and filter out the collision) such that no `InvalidCollisionTypeException` is thrown.
Jitter’s soft body implementation is based on this kind of filter (see `SoftBodies.BroadPhaseCollisionFilter`).

### Example: Collision groups

Collision groups might be easily implemented using a broad phase filter.
In this example, there are two 'teams', team blue and team red.
A filter that disregards all collisions between team members (rigid bodies) of different colors is implemented:

```cs
public class TeamFilter : IBroadPhaseFilter
{
    public class TeamMember { }
    
    public static TeamMember TeamRed = new();
    public static TeamMember TeamBlue = new();
    
    public bool Filter(Shape shapeA, Shape shapeB)
    {
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

The `TeamFilter` class can then be instantiated and assigned to `world.BroadPhaseFilter`, ensuring that rigid bodies of different colors will not interact:

```cs
world.BroadPhaseFilter = new TeamFilter();
...
bodyA.Tag = TeamFilter.TeamBlue;
bodyB.Tag = TeamFilter.TeamRed;
bodyC.Tag = TeamFilter.TeamRed;
```

## Narrow phase filter

The `world.NarrowPhaseFilter`

```cs
public INarrowPhaseFilter? NarrowPhaseFilter { get; set; }
```

operates similarly.
However, this callback is called after narrow phase collision detection, meaning detailed collision information (such as normal, penetration depth, and collision points) is available at this stage.
The filter can not only exclude collisions but also modify collision information.

The default narrow phase collision filter in Jitter is assigned to an instance of `TriangleEdgeCollisionFilter`, which filters out so-called 'internal edges' for `TriangleShape`s.
These internal edges typically cause collision artifacts when rigid bodies slide over the edges of connected triangles forming static geometry.
In the literature, this problem is also known as 'ghost collisions'.
