# Arbiters

An **arbiter** stores contact information for a pair of shapes. It holds up to four contact points and reuses them between simulation steps. CCD can create an arbiter before the shapes touch.

## Arbiters and shapes

A body with several shapes can have several arbiters with the same opposing body, one for each shape pair in contact.

For example, consider two bodies where body A has two box shapes and body B has one sphere shape.
If both boxes overlap with the sphere, two separate arbiters are created: one for each box–sphere pair.

> [!NOTE]
> **One arbiter per shape pair**
> The contact points can change while the arbiter remains the same. It lasts until contact ends or one of the shapes or bodies is removed.

## ArbiterKey

Each arbiter is identified by an `ArbiterKey`, which is an ordered pair of `ulong` identifiers.
For arbiters created by the engine, these identifiers are the `ShapeId` values of the two shapes involved.
The shape with the smaller ID comes first.

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

`ContactData` has four contact slots. `UsageMask` identifies the valid slots. The structure also stores the pair's friction and restitution; each is the maximum of the two bodies' values.

> [!CAUTION]
> **Lifetime**
> Obtain a fresh data reference each time you need one; do not retain it across simulation steps or world changes.
> Do not use an arbiter after it is removed or after its `EndCollide` callback, because it may be reused for another collision.

## Collision events

The `RigidBody` class provides two events that supply the associated arbiter:

```cs
body.BeginCollide += (Arbiter arb) =>
{
    // Called when a new arbiter is created involving this body.
};

body.EndCollide += (Arbiter arb) =>
{
    // Called when this contact pair expires during a simulation step.
};
```

`BeginCollide` can also occur for a predicted CCD contact before the shapes touch. `EndCollide` is raised when contact expires during a simulation step. Explicitly removing a body, shape, or arbiter does not raise `EndCollide`.

Use these events to inspect contacts. For example, set friction when a contact pair begins:

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

Supply both contact points in world space. The normal must be a unit vector pointing in the direction that pushes body 2 away from body 1.

The optional `removeFlags` parameter disables parts of the response. For example, `ContactData.SolveMode.Angular` removes the angular response, leaving only linear motion.

Alternatively, contacts can be registered using IDs directly, which also creates the arbiter if needed:

```cs
world.RegisterContact(id0, id1, body1, body2, point1, point2, normal);
```

### Contact caching

An arbiter keeps at most four contact points. A new point close to an existing one refreshes that contact and reuses its saved impulse. Otherwise it fills a free slot. When all slots are full, the engine chooses points that cover the touching region well.

Contacts are removed when the shapes separate too far or the points slide too far apart. Reusing nearby contacts gives the solver a useful starting point on the next step.

## Arbiter lifecycle

1. **Creation:** the engine creates an arbiter for a new shape pair, including predicted CCD contacts, and raises `BeginCollide` on both bodies.
2. **Update:** each step adds or refreshes contact points and removes points that have separated too far.
3. **Removal:** once contact ends, the engine raises `EndCollide` on both bodies and removes the arbiter. The arbiter object may then be reused for another pair.
