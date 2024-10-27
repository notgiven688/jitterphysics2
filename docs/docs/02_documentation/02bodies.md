# Rigid Bodies

Rigid bodies represent the main entity of the dynamics system of the engine.

## Creating a body

Rigid bodies are associated to an instance of the `World` class. They can be created like this:

```cs
var world = new World();
var body = world.CreateRigidBody();
```

## Adding shapes

Multiple shapes can be added to a rigid body, for example:

```cs
body.AddShape([new SphereShape(radius: 2), new BoxShape(size: 1)]);
```

Shapes determine how bodies collide with each other.

:::warning Creating many bodies at once
When calling body.AddShape(shape), the shape is registered in the collision system of the engine and immediately added to the spatial tree structure (`DynamicTree`) for efficient broad-phase collision detection.
Registering many objects at $(0, 0, 0)$ must be prevented by specifying the rigid body position, before adding shapes.
:::

:::warning Passing the same instance to multiple bodies
Passing the same instance of a shape to multiple bodies is not allowed in Jitter and will throw an exception.
:::

In Jitter, the sphere shape is defined so that its geometric center aligns with the (local) coordinate system's center at $(0, 0, 0)$.
The same holds for all basic primitives (sphere, box, capsule, cone, cylinder).

After adding a shape to Jitter the mass properties (mass and inertia) of the associated rigid body are calculated accordingly.
Jitter assumes unit density for the calculations.

Adding just a sphere

```cs
body.AddShape(new SphereShape(radius: 1));
```

will result in a body with the textbook inertia and mass of a unit-density sphere of radius one.

Of course, the mass properties of the body can be set directly, using `body.SetMassInertia`.
Setting `setMassProperties: false` in `body.AddShape(...)` prevents Jitter from using the shapes' mass properties.

***In Jitter the position of the rigid body has to align with the center of mass.**
So in the local reference frame of the body, the center of mass is $(0, 0, 0)$. Shapes or combinations of shapes must be translated accordingly.*

### Debugging shapes

The `RigidBody` class offers the `body.DebugDraw(IDebugDrawer drawer)` method which creates a triangle hull for each shape added to the body and calls the `drawer.DrawTriangle` method in the provided `IDebugDrawer` implementation.
The coordinates of the triangles are in world space and can be drawn to debug the collision shape of the rigid body.

:::warning body.DebugDraw performance
Every call to `body.DebugDraw` the triangle hulls are generated on the fly.
Since this is a slow operation the method should only be called for debugging purposes.
:::

## Gravity

The gravity for the world can be set using `world.Gravity`.
The property `body.AffectedByGravity` can be used to disable gravity for individual bodies.

## Damping

Jitter uses a very simple damping system to slow rigid bodies down.
This improves simulation stability and also resembles mechanical systems 'losing' energy in the real world.
In Jitter there is a linear and an angular damping factor for each body which can be set using `body.Damping`.
With each `world.Step`, Jitter multiplies the angular and linear velocity of each rigid body by $$1-\gamma$$, where $$\gamma$$ is the damping factor.
For performance reasons there is no time dependency for the damping system.
As a result, bodies in a simulation with smaller timesteps experience greater damping.

## Speculative contacts

Speculative contacts can be utilized to prevent fast and small objects from tunneling through thin objects.
An object moving quickly enough might 'miss' a collision since the distance traveled between two frames exceeds the thickness of another object.
Speculative contacts can be enabled on a per-body basis using `body.EnableSpeculativeContacts`.
The `world.SpeculativeRelaxationFactor` and `world.SpeculativeVelocityThreshold` can be adjusted to fine-tune speculative contacts for specific use cases.
However, it should be noted that an accurate simulation of fast-moving objects is only possible using smaller time steps.
Speculative contacts may involve a trade-off of less accurate collision detection and response.

## Friction and Restitution

Friction and restitution coefficients may be set through `body.Friction` and `body.Restitution`.
For a collision of two bodies with different coefficients the maximum value of each body is taken.

## Collide events

An instance of `RigidBody` provides two events: `BeginCollide` and `EndCollide`.
These events are triggered whenever an arbiter (Contact) is created or removed which involves the rigid body.
By default, arbiters are created between colliding shapes.
For example, the `BeginCollide` event can be used to modify the coefficient of friction of a contact:

```cs
body.BeginCollide += BodyOnBeginCollide;

private void BodyOnBeginCollide(Arbiter arb)
{
    arb.Handle.Data.Friction = 0.0f;
}
```

## Activation/Deactivation

A rigid body is always assigned to an island.
Islands are formed by bodies which are pairwise interacting with each other trough contacts or constraints.
Different islands are not interacting with each other in any way.

Active rigid bodies may be marked for deactivation by the world once their angular and linear velocity remain below the thresholds defined in `body.DeactivationThreshold` for a period defined by `body.DeactivationTime`.
If all bodies within an island are marked for deactivation the whole island gets deactivated.
The simulation cost for inactive bodies is effectively zero.
Islands (and their associated bodies) might get waken up, as soon as a collision with an active body is registered.

Using `body.SetActivationState`, the user can reset the internal deactivation time clock for the rigid body.
It will not immediately change the activation state of the body (`body.IsActive`).
Jitter will then in the next `world.Step` consider this body and it's connected island for activation or deactivation.
Calling e.g. `body.SetActivationState(false)` on a falling body with a velocity greater than `body.DeactivationThreshold` will have no effect.

## Static bodies

Static bodies have infinite mass and therefore are not affected by collisions or constraints.
They also do not join islands.
Static bodies do not generate collisions with other static or inactive bodies.
Because of this, the position of static bodies should not be altered while in contact with other bodies.

## Kinematic bodies

There is no explicit concept of kinematic bodies in Jitter.
In game physics kinematic bodies are bodies which can have a velocity and therefore change their position.
They act similiar to static bodies during collisions - their velocity is not changed when colliding with a regular body.
Their velocity if often set directly using `body.Velocity`.
In Jitter kinematic bodies are just regular rigid bodies with infinite (or very high) mass.
This can be achieved using `body.SetMassInertia` like in this example:

```cs
body.SetMassInertia(JMatrix.Zero, 1e-3f, setAsInverse: true);
```

Additionaly, damping can be disabled:

```cs
body.Damping = (linear: 0.0f, angular: 0.0f);
```

:::danger Kinematic bodies
Setting the mass of a rigid body to infinity makes a body unstoppable.
Having an unstoppable object colliding with a static (immovable object) or another unstoppable object might crash the solver.
It must be ensured that no contact points are generated in such cases.
:::
