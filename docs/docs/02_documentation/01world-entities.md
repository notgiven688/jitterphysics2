# World Entities

For a more detailed description of the entities of a world, namely Islands, Shapes, RigidBodies, Constraints, and Arbiters, refer to the corresponding documentation pages.

### Creating RigidBodies and Constraints

Use `world.CreateRigidBody()` to create a new instance of the RigidBody class. A typical code snippet for adding a unit cube to Jitter could look like this:

```cs
RigidBody body = world.CreateRigidBody();
body.Position = new JVector(0, 5, 0);
var shape = new BoxShape(1);
body.AddShape(shape);
```

It is not valid to add the same instance of a shape to multiple bodies.

:::info Order of Initialization
When calling `body.AddShape(shape)`, the shape is registered in the collision system of the engine and added to the spatial tree structure (AABB-tree) for efficient broad-phase collision detection. Make sure to not accidentally register many objects at (0, 0, 0). This may happen by repeatedly calling `body.AddShape`, before specifying the positions of the associated rigid bodies.
:::

Constraints are another type of entities that can be explicitly added to the world using `world.CreateConstraint<T>(body1, body2)`, where T is a constraint. In the following example, two constraints (a HingeAngle and a BallSocket constraint) are added:

```cs
hingeAngle = world.CreateConstraint<HingeAngle>(body1, body2);
hingeAngle.Initialize(hingeAxis, angle);

ballSocket = world.CreateConstraint<BallSocket>(body1, body2);
ballSocket.Initialize(hingeCenter);
```

`CreateConstraint` always takes two bodies as parameters; the `Initialize` method depends on the constraint used.

:::caution Constraint Initialization
For all default constraints available in Jitter, `constraint.Initialize` must be called once after `world.CreateConstraint`.
:::

### Accessing Entities

Once created, **RigidBodies** are available as an `ActiveList<RigidBody>` under `world.RigidBodies`. You can loop over all rigid bodies in the world using a regular foreach loop or a simple for loop:

```cs
foreach(var body in world.RigidBodies)
{
    // ...
}

for(int i = 0; i < world.RigidBodies.Count; i++)
{
    var body = world.RigidBodies[i];
    // ...
}
```

Note that the items in `ReadOnlyActiveList<RigidBody>` are not necessarily in a fixed order; Jitter may rearrange items during a call to `world.Step()`. However, active bodies (bodies which have not been inactivated by the engine) are guaranteed to be enumerated first. Using a for-loop, active and inactive bodies can be enumerated separately:

```cs
// enumerate all active bodies
for(int i = 0; i < world.RigidBodies.Active; i++)
{
    var body = world.RigidBodies[i];
    // ...
}

// enumerate all inactive bodies
for(int i = world.RigidBodies.Active; i < world.RigidBodies.Count; i++)
{
    var body = world.RigidBodies[i];
    // ...
}
```

**Constraints** are not directly available from the world class. Users can keep track of them independently or by enumerating the HashSet of constraints owned by the associated bodies, using `body.Constraints`.

**Islands** and **Shapes** are also stored in `ReadOnlyActiveList`s and can be accessed using `world.Islands` and `world.Shapes`.

### Removing Entities

The `world.Remove` function has overloads for removing instances of `RigidBody`, `Arbiter`, and `Constraint`.

Removing a rigid body also removes all constraints and contacts in which the body is involved.

:::caution Accessing Removed Entities
Instances of `RigidBody`, `Arbiter`, and `Constraint` store some of their data in unmanaged memory, which is automatically freed once the entities are removed from the world. Do not use these entities any longer, i.e., do not call functions or use their properties, otherwise, a NullReferenceException may be thrown.
:::
