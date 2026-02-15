# Arbiters

Arbiters manage contact information between two rigid bodies.
An arbiter is created when two shapes begin overlapping and is removed when they separate or when one of the involved bodies is removed from the world.
Each arbiter can hold up to four cached contact points (see `ContactData`).

## Arbiters and shapes

Arbiters are created between *shapes*, not bodies.
Since a rigid body can have multiple shapes attached to it, a single pair of bodies may produce multiple arbiters—one for each pair of overlapping shapes.

For example, consider two bodies where body A has two box shapes and body B has one sphere shape.
If both boxes overlap with the sphere, two separate arbiters are created: one for each box–sphere pair.

> [!NOTE]
> **One arbiter per shape pair**
> Even if the contact manifold changes over time, the arbiter remains the same as long as the two shapes stay in proximity.
> Contact points within the arbiter are cached and updated across frames.

## ArbiterKey

Each arbiter is identified by an `ArbiterKey`, which is an ordered pair of `ulong` identifiers.
For arbiters created by the engine, these identifiers are the `ShapeId` values of the two shapes involved.
The ordering is canonical: the shape with the smaller ID is always first.

```cs
public readonly struct ArbiterKey(ulong key1, ulong key2) : IEquatable<ArbiterKey>
{
    public readonly ulong Key1 = key1;
    public readonly ulong Key2 = key2;
}
```

The order of `Key1` and `Key2` matters for equality comparison.

## Accessing contact data

The `Arbiter` class exposes a `Handle` property of type `JHandle<ContactData>`.
Through `arbiter.Handle.Data`, the underlying contact information can be read and modified:

```cs
ref ContactData data = ref arbiter.Handle.Data;
```

The `ContactData` structure contains up to four contact slots.
The `UsageMask` bitfield indicates which slots are active.
It also stores the combined `Friction` and `Restitution` coefficients for the contact pair (derived from the maximum of the two bodies' values).

> [!CAUTION]
> **Lifetime**
> The contact data is only valid while the arbiter is registered with the world.
> Do not cache references to `ContactData`.
> Accessing it after the arbiter has been removed results in undefined behavior.

## Collision events

The `RigidBody` class provides two events that supply the associated arbiter:

```cs
body.BeginCollide += (Arbiter arb) =>
{
    // Called when a new arbiter is created involving this body.
};

body.EndCollide += (Arbiter arb) =>
{
    // Called when an arbiter involving this body is removed.
};
```

These events can be used to inspect or modify contact properties.
For example, overriding friction for a specific contact:

```cs
body.BeginCollide += (Arbiter arb) =>
{
    arb.Handle.Data.Friction = 0.0f;
};
```

Since arbiters are per shape pair, a body with multiple shapes may trigger `BeginCollide` multiple times for the same opposing body—once for each shape pair.

## Custom arbiters

Arbiters are not limited to the engine's built-in collision detection.
Custom arbiters can be created and managed using `world.GetOrCreateArbiter` and `world.RegisterContact`.

### Creating an arbiter

`GetOrCreateArbiter` takes two `ulong` identifiers and two rigid bodies.
If an arbiter for the given ID pair already exists, it is returned; otherwise, a new one is created:

```cs
world.GetOrCreateArbiter(id0, id1, body1, body2, out Arbiter arbiter);
```

The identifiers do not have to be shape IDs—they can be any `ulong` values that uniquely identify the contact pair.
This allows custom collision systems to create arbiters for application-specific purposes.

> [!WARNING]
> **ID ordering**
> The order of the two identifiers matters.
> `ArbiterKey(1, 2)` and `ArbiterKey(2, 1)` are considered different keys.
> When creating custom arbiters, be consistent with the ordering.

### Registering contacts

Once an arbiter exists, contacts can be registered into it:

```cs
world.RegisterContact(arbiter, point1, point2, normal);
```

All vectors must be in world space, and the normal must be a unit vector.
The `removeFlags` parameter can be used to exclude certain velocity components from the solver (e.g., `ContactData.SolveMode.Angular` for angular-only or linear-only responses).

Alternatively, contacts can be registered using IDs directly, which also creates the arbiter if needed:

```cs
world.RegisterContact(id0, id1, body1, body2, point1, point2, normal);
```

### Contact caching

An arbiter holds at most four contact points.
When a new contact is registered, the arbiter decides how to incorporate it:

1. **Slot available**: If fewer than four contacts are active, the new point is compared to existing contacts. If it is close enough to an existing contact (within a break threshold), it replaces that contact and inherits its accumulated impulse for warm-starting. Otherwise it is inserted into an empty slot as a new contact.
2. **All slots full**: When all four slots are occupied, the algorithm evaluates which existing contact to replace. For each of the four candidates, it computes the area of the quadrilateral formed by the new point and the three remaining contacts. The candidate whose removal maximizes this area is replaced. This heuristic keeps contacts well-distributed across the contact patch, which produces the most stable contact manifold.

Contacts that drift apart beyond a distance threshold during position integration are automatically removed from the manifold.

## Arbiter lifecycle

1. **Creation**: When two shapes begin overlapping, the engine calls `GetOrCreateArbiter` using the shape IDs as keys. The `BeginCollide` event is raised on both bodies.
2. **Update**: Each `world.Step`, contact positions are updated. Contacts that have separated beyond a threshold are removed from the arbiter. New contacts from ongoing collisions are added.
3. **Removal**: When all contacts in an arbiter are broken and the shapes are no longer overlapping, the arbiter is removed. The `EndCollide` event is raised on both bodies. The arbiter is returned to an internal pool for reuse.
