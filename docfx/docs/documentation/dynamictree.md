# Dynamic Tree

The dynamic tree holds instances which implement the `IDynamicTreeProxy` interface.
Its main task is to efficiently determine if a proxy's axis-aligned bounding box overlaps with the axis-aligned bounding box of any other proxy in the world.
A naive implementation requires $\mathcal{O}\left(n\right)$ operations (checking for an overlap with every of the $n-1$ entities).
The tree structure accelerates this to $\mathcal{O}\left(\mathrm{log}(n)\right)$.
Since proxies are dynamic and can move, the tree must be continuously updated.
To trigger updates less frequently, entities are enclosed within slightly larger bounding boxes than their actual size.
This bounding box extension is defined by the `Velocity` property of the `IDynamicTreeProxy` interface.

![img alt](images/dynamictree.png)

## Adding proxies

All shapes added to a rigid body (`body.AddShape`) are automatically registered with `world.DynamicTree`.
Custom implementations of `IDynamicTreeProxy` can be added to the tree using `tree.AddProxy`.
In this case, a `BroadPhaseFilter` must be implemented and registered (using `world.BroadPhaseFilter`) to handle collisions with the custom proxy, otherwise an `InvalidCollisionTypeException` is thrown.

## Enumerate Overlaps

The tree implementation needs to be updated using `tree.Update`.
This is done automatically for the dynamic tree owned by the world class (`world.DynamicTree`).
Internally, `UpdateWorldBoundingBox` is called for active proxies implementing the `IUpdatableBoundingBox` interface, and the internal book-keeping of overlapping pairs is updated.
Overlaps can be queried using `tree.EnumerateOverlaps`.

## Querying the tree

All tree proxies that overlap a given axis-aligned box can be queried:

```cs
public void Query<T>(T hits, in JBBox box) where T : ICollection<IDynamicTreeProxy>
```

As well as all proxies which overlap with a ray:

```cs
public void Query<T>(T hits, JVector rayOrigin, JVector rayDirection) where T : ICollection<IDynamicTreeProxy>
```

Custom queries can easily be implemented.
An implementation which queries all proxies overlapping with a single point:

```cs
var stack = new Stack<int>();
stack.Push(tree.Root);

while (stack.TryPop(out int id))
{
    ref DynamicTree.Node node = ref tree.Nodes[id];
    
    if (node.ExpandedBox.Contains(point))
    {
        if (node.IsLeaf)
        {
            Console.WriteLine($'{node.Proxy} contains {point}.');
        }
        else
        {
            stack.Push(node.Left);
            stack.Push(node.Right);
        }
    }
}
```

## Ray casting

All proxies in the tree which implement the `IRayCastable` interface can be raycasted, including all shapes:

```cs
public bool RayCast(JVector origin, JVector direction, RayCastFilterPre? pre, RayCastFilterPost? post,
    out IDynamicTreeProxy? proxy, out JVector normal, out float lambda)
```

The pre- and post-filters can be used to discard hits during the ray cast.
A ray is shot from the origin into the specified direction.
The function returns `true` if a hit was found.
The point of collision is given by `hit = origin + lambda * direction`, where `lambda` is in the range `[0, ∞)`.

The returned `normal` is the normalized surface normal at the hit point.
