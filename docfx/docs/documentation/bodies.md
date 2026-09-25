# Rigid Bodies

Rigid bodies are the main entity of the dynamics system.

## Creating a body

Rigid bodies are associated with an instance of the `World` class:

```cs
var world = new World();
var body = world.CreateRigidBody();
```

## Adding shapes

Multiple shapes can be added to a rigid body, for example:

```cs
body.AddShapes([new SphereShape(radius: 2), new BoxShape(size: 1)]);
```

Shapes determine how bodies collide with each other.

> [!WARNING]
> **Creating many bodies at once**
> When calling `body.AddShape(shape)`, the shape is registered in the collision system of the engine and immediately added to the spatial tree structure (`DynamicTree`) for efficient broad-phase collision detection.
> Avoid registering many objects at $(0, 0, 0)$ by setting the rigid body position before adding shapes.

> [!WARNING]
> **Passing the same instance to multiple bodies**
> Passing the same instance of a shape to multiple bodies is not allowed in Jitter and will throw an exception.

The sphere shape is defined so that its geometric center aligns with the (local) coordinate system's center at $(0, 0, 0)$.
The same holds for all basic primitives (sphere, box, capsule, cone, cylinder).

After adding a shape, the mass properties (mass and inertia) of the associated rigid body are calculated accordingly.
Unit density is assumed for the calculations.

Adding just a sphere

```cs
body.AddShape(new SphereShape(radius: 1));
```

will result in a body with the textbook inertia and mass of a unit-density sphere of radius one.

The mass properties of the body can also be set directly using `body.SetMassInertia`.
Passing `MassInertiaUpdateMode.Preserve` to `body.AddShape(...)` or `body.AddShapes(...)` prevents the automatic recalculation of mass properties when shapes are added.

> [!IMPORTANT]
> The position of a rigid body must align with its center of mass.
> In the body's local reference frame, the center of mass is $(0, 0, 0)$.
> Shapes or combinations of shapes must be translated accordingly.

### Debugging shapes

The `RigidBody` class offers the `body.DebugDraw(IDebugDrawer drawer)` method which creates a triangle hull for each shape added to the body and calls the `drawer.DrawTriangle` method in the provided `IDebugDrawer` implementation.
The coordinates of the triangles are in world space and can be drawn to debug the collision shape of the rigid body.

> [!WARNING]
> **body.DebugDraw performance**
> Every call to `body.DebugDraw` generates the triangle hulls on the fly.
> Since this is a slow operation the method should only be called for debugging purposes.

## Forces and impulses

Forces and torques can be applied using `body.AddForce`. Forces accumulate over the current step and are reset after integration.

For instantaneous velocity changes, use `body.ApplyImpulse`:

```cs
// Linear impulse — changes velocity immediately.
body.ApplyImpulse(new JVector(0, 10, 0));

// Impulse at a world-space position — changes both linear and angular velocity.
body.ApplyImpulse(new JVector(0, 10, 0), hitPosition);
```

Both overloads accept an optional `wakeup` parameter (default `true`).
When set to `false`, the impulse is silently ignored if the body is sleeping.

## Gravity

The gravity for the world can be set using `world.Gravity`.
The property `body.AffectedByGravity` can be used to disable gravity for individual bodies.

## Restricting motion and planar simulations

`body.AllowedMotion` specifies the translations and rotations permitted by the simulation.
The default is `MotionAxes.All`. Each assignment replaces the previous selection.
All axes are in **world space**, including the angular axes.

For motion in the XY plane:

```cs
body.AllowedMotion = MotionAxes.PlaneXY;
// Equivalent to LinearX | LinearY | AngularZ.
```

`PlaneXZ` and `PlaneYZ` provide the corresponding presets for the other coordinate planes.
You can also combine individual axes:

```cs
body.AllowedMotion = MotionAxes.LinearX | MotionAxes.AngularZ;
```

An angular flag names the axis of rotation: `AngularZ` permits rotation around the world Z axis.
`MotionAxes.Linear` permits all translations, and `MotionAxes.Angular` permits all rotations.

Restrictions change the **effective inverse mass and inverse inertia used by the solver**.
Prohibited directions behave as having infinite mass or inertia. `body.Mass` remains a scalar,
and `body.InverseInertia` continues to describe the original inertia in body space.
For coupled inertia tensors, restricting rotation also changes the effective response around
the remaining axes. The engine derives this response from the original tensor.

The original mass properties are preserved. Calls to `SetMassInertia` and shape changes update
those properties while retaining the current restrictions. To restore unrestricted response
from the latest mass properties, assign:

```cs
body.AllowedMotion = MotionAxes.All;
```

Changing `AllowedMotion` immediately clears prohibited velocity components, invalidates cached
contact and joint impulses, and schedules the affected bodies for activation. Cleared velocities
are not restored when motion is allowed again. Restrictions apply to forces, gravity, impulses,
contacts, joints, and kinematic velocities. Position and orientation can still be assigned directly.
Gyroscopic integration is skipped while any angular axis is restricted.
Do not change the property concurrently with `World.Step`.

`MotionAxes.None` prohibits simulated movement while preserving the body's `MotionType` and
collision/island participation. Use `MotionType.Static` for ordinary static geometry.

Planar motion uses the existing 3D collision shapes and contact generation. Bodies retain their
initial coordinate along the prohibited translation axis; set that coordinate consistently when
building a planar scene. This does not add separate 2D shape types or collision detection.

### Custom constraints

The low-level `RigidBodyData.InverseMass` is a `JVector`. Use
`JVector.Multiply(data.InverseMass, impulse)` for the componentwise velocity response and
`data.GetInverseMass(direction)` for a solver row's translational inverse effective mass.
Multiplying two `JVector` values with `*` computes a dot product, so it cannot replace
componentwise multiplication.

`RigidBodyData.InverseInertiaWorld` is a `JSymmetricMatrix` stored in six scalars. It supports
`JVector.Transform` and converts implicitly to `JMatrix`. Custom solvers must handle zero
response in prohibited directions, including singular effective-mass matrices. The body data
occupies 128 bytes in single precision and 256 bytes in double precision. Access its fields
by name rather than relying on field offsets.

## Damping

Jitter2 uses a simple damping system to slow rigid bodies down.
This improves simulation stability and also resembles mechanical systems losing energy in the real world.
There is a linear and an angular damping factor for each body which can be set using `body.Damping`.
With each `world.Step`, the angular and linear velocity of each dynamic rigid body is multiplied by $1-\gamma$, where $\gamma$ is the damping factor.
For performance reasons there is no time dependency for the damping system.
As a result, bodies in a simulation with smaller time steps experience greater damping.

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

`RigidBody` provides two events: `BeginCollide` and `EndCollide`.
These events are triggered whenever an arbiter is created or removed which involves the rigid body.
See [Arbiters](arbiters.md#collision-events) for details and examples.

## Activation/Deactivation

A rigid body is always assigned to an island.
Islands are formed by bodies which are pairwise interacting with each other through contacts or constraints.
Different islands are not interacting with each other in any way.

Active rigid bodies may be marked for deactivation by the world once their angular and linear velocity remain below the thresholds defined in `body.DeactivationThreshold` for a period defined by `body.DeactivationTime`.
If all non-static bodies within an island are ready to sleep, the whole island gets deactivated.
The simulation cost for inactive bodies is effectively zero.
Islands (and their associated bodies) may get woken up as soon as a collision with an active body is registered.

Using `body.SetActivationState`, the user can schedule a body for activation or deactivation.
It will not immediately change the activation state of the body (`body.IsActive`).
The next `world.Step` will then consider this body and its connected island for activation or deactivation.
Calling e.g. `body.SetActivationState(false)` on a falling body with a velocity greater than `body.DeactivationThreshold` will have no lasting effect, because the body marks its island active again during the update.

For explicit immediate deactivation, use `world.ForceSleepIsland(island)`.
This deactivates the whole island at once, ignores the normal deactivation thresholds, and clears linear velocity, angular velocity, queued forces, and queued torques for all bodies in the island.

Activation state is most meaningful for dynamic and kinematic bodies:

- Dynamic bodies are actively simulated while awake and skipped while sleeping.
- Kinematic bodies can be part of islands. They are treated as infinite-mass bodies by the solver, but they can wake connected dynamic bodies through contacts or constraints.
- Static bodies are normally inactive bookkeeping objects. They do not form regular island connections and `body.SetActivationState(true)` does not make the static body itself active.

## Static bodies

Static bodies (`body.MotionType == MotionType.Static`) have infinite mass and therefore are not affected by collisions or constraints.
They also do not join islands.
Static bodies do not generate collisions with other static or inactive bodies.
Static bodies are normally inactive even if `body.SetActivationState(true)` is called.

Changing the position or orientation of a static body updates its broad-phase proxies and wakes affected non-static bodies on the next step.
The static body itself remains inactive; the bodies that have to react to the changed contact state are activated.
This is useful for occasional edits to static level geometry, but it is a discontinuous transform from the solver's point of view.
For continuously moving platforms or obstacles, prefer kinematic bodies.

## Kinematic bodies

Kinematic bodies (`body.MotionType == MotionType.Kinematic`) can have a velocity and therefore change their position.
They act similar to static bodies during collisions—their velocity is not changed when colliding with a regular body.
They do take part in collision islands.
Because of that, kinematic bodies participate in island activation and deactivation like other non-static bodies.
