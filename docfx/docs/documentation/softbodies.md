# Soft Bodies

A soft body in Jitter2 is built from the rigid-body machinery. Each vertex is a rigid body that acts as a point mass, springs are constraints between those vertices, and the soft body's collision shapes follow the vertices as they move. `SoftBody` groups these parts, keeps their activation state in sync, and removes them together.

## Setting up the world

The world's built-in collision detection handles pairs of `RigidBodyShape` only. Soft bodies need two filters:

```cs
world.DynamicTree.Filter = Jitter2.SoftBodies.DynamicTreeCollisionFilter.Filter;
world.BroadPhaseFilter = new Jitter2.SoftBodies.BroadPhaseCollisionFilter(world);
```

`DynamicTreeCollisionFilter.Filter` rejects pairs of shapes that belong to the same rigid body, like the default filter, and also pairs of shapes that belong to the same soft body, so a soft body does not collide with itself.

`BroadPhaseCollisionFilter` handles every pair that involves a soft-body shape. It runs the narrow phase itself and registers the contact with the vertex closest to the contact point, then returns `false` so the world does not test the pair again. Pairs of rigid-body shapes pass through untouched. If you already use a broad phase filter, call these two from it. See [Collision Filters](filters.md).

## Building a soft body

Create a `SoftBody` for the world, then add vertices, springs and shapes.

**Vertices** are rigid bodies without shapes. Give them a mass and an inverse inertia of zero, so they never rotate:

```cs
var vertex = world.CreateRigidBody();
vertex.Position = position;
vertex.SetMassInertia(JSymmetricMatrix.Zero, 100.0f, setAsInverse: true);
softBody.Vertices.Add(vertex);
```

With `setAsInverse` both values are inverses, so the line above gives the vertex a mass of 0.01.

**Springs** hold the body together. `SpringConstraint` keeps the distance between two vertices and ignores their rotation, which makes it cheaper than the general constraints. Any other constraint works too, for example `BallSocket` constraints from the corners of a cube to a body at its centre.

```cs
var spring = world.CreateConstraint<SpringConstraint>(a, b);
spring.Initialize(a.Position, b.Position);
spring.Softness = 0.2f;
softBody.AddSpring(spring);
```

`Softness` and `Bias` control how the spring responds. `SetSpringParameters(frequency, damping, dt)` computes both from a frequency in Hz and a damping ratio for the given time step; call it again when the masses or the time step change. `TargetDistance` is the rest length, set to the initial distance by `Initialize`.

**Shapes** give the body a surface to collide with. They do not hold it together. A `SoftBodySegment` is a capsule between two vertices, with a `Radius` (default 0.05). A `SoftBodyTriangle` spans three vertices and has a `Thickness` (default 0.1) so that the sheet has volume. A `SoftBodyTetrahedron` spans four vertices and has volume by itself. The shape is added to the soft body, not to a rigid body:

```cs
softBody.AddShape(new SoftBodySegment(softBody, a, b));
softBody.AddShape(new SoftBodyTriangle(softBody, a, b, c));
softBody.AddShape(new SoftBodyTetrahedron(softBody, a, b, c, d));
```

A cloth, for example, is a grid of vertices with springs along the grid lines and across the cells, and two triangles per cell. A rope is a chain of vertices with a spring and a segment between each pair of neighbours. Contacts go to the nearer vertex, so keep segments no longer than about twice their radius. To pin a vertex, make it static:

```cs
corner.MotionType = MotionType.Static;
```

## Collisions

A soft-body shape computes its support points from the current vertex positions, so no transform is needed; the narrow phase sees the shape as it is deformed. Contacts are registered with the vertex closest to the contact point. Against a rigid body, the vertex only takes linear impulses while the rigid body responds fully. Between two soft bodies, both sides are vertices.

Shapes of the same soft body never collide with each other, so a cloth does not stop itself from passing through its own folds. For self-collision, replace the dynamic tree filter with one that lets pairs of the same soft body through when they share no vertex. The broad phase filter then handles them like shapes of two different soft bodies.

For a chain of `SoftBodySegment` shapes that test is not enough. Segments a few places apart share no vertex but still overlap where the rope bends, which pushes the rope apart. Let a pair through only if the segments are far enough apart along the chain, for example two diameters. A segment does not know its position in the chain, so store it yourself, for example in a subclass.

A vertex is light compared to a typical rigid body, so a heavy body on a cloth stretches the springs considerably. Substeps help.

## Activation and removal

`SoftBody.IsActive` follows the first vertex. After each step the soft body activates or deactivates its shapes in the dynamic tree together with its vertices, so a soft body that has come to rest falls asleep like a rigid body. The springs put all vertices into one island.

`softBody.Destroy()` removes the shapes, springs and vertices from the world. It is safe to call more than once. After `world.Clear()` the soft body cleans itself up on the next step.

## Custom behaviour

Derive from `SoftBody` and override `WorldOnPostStep` to apply forces every step. A pressurized body, for example, computes the enclosed volume of its triangles there and pushes each triangle's vertices along its normal with `AddForce`. Call the base method first; it keeps the activation state in sync.

The soft-body shapes can be subclassed as well, for example to override `UpdateWorldBoundingBox` so that a custom broad phase filter can find pairs before they touch. The base constructors call `UpdateWorldBoundingBox`, so an override runs before the constructor body of the subclass. `RopeSegment` and `RopeCollisionFilter` in JitterDemo do this to keep thin, fast ropes from passing through each other.

`SpringConstraint.DebugDraw` draws a spring as a coil between its anchors; see [Debug drawing](constraints.md#debug-drawing).
