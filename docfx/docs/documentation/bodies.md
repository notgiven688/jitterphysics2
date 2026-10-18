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
Inertia tensors use `JSymmetricMatrix`, which stores the six independent components of a symmetric matrix.
Both `body.InverseInertia` and shape mass-property calculations use this type.

```cs
body.SetMassInertia(JSymmetricMatrix.CreateScale(2, 3, 4), mass: 5);
```

The tensor is expressed in body space, about the body's center of mass.

Passing `MassInertiaUpdateMode.Preserve` to `body.AddShape(...)` or `body.AddShapes(...)` prevents the automatic recalculation of mass properties when shapes are added.

> [!IMPORTANT]
> The position of a rigid body must align with its center of mass.
> In the body's local reference frame, the center of mass is $(0, 0, 0)$.
> Shapes or combinations of shapes must be translated accordingly.

### Debugging shapes

Debug drawing shows the collision shapes attached to a body at their current positions and rotations.
To display them, connect Jitter to your renderer by implementing `IDebugDrawer`:

```cs
using Jitter2;
using Jitter2.LinearMath;

public sealed class PhysicsDebugDrawer : IDebugDrawer
{
    public void DrawSegment(in JVector pA, in JVector pB, uint color)
    {
        // Draw a line from pA to pB using your renderer.
        // The positions are in world space; color is packed as 0xRRGGBBAA.
    }

    public void DrawTriangle(in JVector pA, in JVector pB, in JVector pC, uint color)
    {
        DrawSegment(pA, pB, color);
        DrawSegment(pB, pC, color);
        DrawSegment(pC, pA, color);
    }
}
```

Replace the comment in `DrawSegment` with a call to your renderer's line-drawing function.
Create one drawer and reuse it:

```cs
IDebugDrawer drawer = new PhysicsDebugDrawer();
```

Then call this in your application's drawing loop, after the physics step has completed:

```cs
body.DebugDraw(drawer);
```

> [!WARNING]
> **Debug drawing can be slow**
> Drawing many bodies or using high detail settings can significantly reduce the frame rate.
> Point clouds and custom shapes can be especially expensive to draw.
> Limit debug drawing to the objects you need to inspect.

This draws all shapes attached to `body`, including their offsets and rotations. The drawer above
draws each triangle as three edges, producing a wireframe. For filled surfaces, call your renderer's
triangle-drawing function in `DrawTriangle`; see [shape drawing](shapes.md#debug-drawing).

The default drawing is white. To draw green shapes and make curved surfaces smoother:

```cs
body.DebugDraw(drawer, new DebugDrawSettings
{
    CurrentColor = DebugDrawColors.Green,
    Segments = 64
});
```

Spheres, capsules, cylinders, and cones are drawn using triangles. `Segments` sets the number of
pieces around each circular cross-section; its default is 48. For example, `Segments = 8` gives a
cylinder an octagonal outline, while `64` makes it look much rounder. Higher values require more
triangles. Boxes, triangle shapes, and convex hulls already have flat faces, so `Segments` has no
effect on them. These settings affect only the visualization.

`PointCloudShape` and custom shapes may use an approximate surface for debug drawing. See
[shape drawing](shapes.md#debug-drawing) for its limitations.

## Forces and impulses

Forces and torques can be applied using `body.AddForce`. They accumulate until the next `world.Step` and are cleared at the end of that call. The resulting velocity change is applied during the **following** step. For a body initially at rest, with no gravity or other influences:

```cs
body.AddForce(force);
world.Step(dt); // Stores the velocity change; velocity and position are unchanged.
world.Step(dt); // Applies the change to velocity and moves the body.
```

This one-step delay also applies to gravity. Keep the timestep and substep count fixed: the velocity change is calculated using one step's substep duration and applied once per substep in the following step.

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
Gravity follows the [force integration timing](#forces-and-impulses) described above.

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
Gyroscopic integration is skipped while any linear or angular axis is restricted.
Do not change the property concurrently with `World.Step`.

`MotionAxes.None` prohibits simulated movement while preserving the body's `MotionType` and
collision/island participation. Use `MotionType.Static` for ordinary static geometry.

Planar motion uses the existing 3D collision shapes and contact generation. Bodies retain their
initial coordinate along the prohibited translation axis; set that coordinate consistently when
building a planar scene. This does not add separate 2D shape types or collision detection.

### Custom constraints

The low-level `RigidBodyData.InverseMassVector` is a `JVector`. Use
`JVector.Multiply(data.InverseMassVector, impulse)` for the componentwise velocity response and
`JVector.Multiply(direction, data.InverseMassVector) * direction` for a solver row's translational inverse effective mass.
Multiplying two `JVector` values with `*` computes a dot product, so it cannot replace
componentwise multiplication.

`RigidBodyData.InverseInertiaWorld` is a `JSymmetricMatrix` stored in six scalars. It supports
`JVector.Transform` and can be expanded to `JMatrix` using `ToMatrix()`. Custom solvers must handle zero
response in prohibited directions, including singular effective-mass matrices. The body data
occupies 128 bytes in single precision and 256 bytes in double precision. Access its fields
by name rather than relying on field offsets.

For symmetric, positive-semidefinite effective-mass matrices, use
`MathHelper.PseudoInverseSymmetric` to invert the responsive subspace while retaining zero
response in blocked directions:

```cs
JMatrix effectiveMass = MathHelper.PseudoInverseSymmetric(inverseEffectiveMass);
```

Compute this during constraint preparation and reuse the result in solver iterations.
The method assumes finite, symmetric, positive-semidefinite input. It uses a numerical
cutoff when diagonalizing singular or nearly singular matrices; see its API documentation
for the precision-dependent tolerance.

## Damping

Jitter2 uses a simple damping system to slow rigid bodies down.
This improves simulation stability and also resembles mechanical systems losing energy in the real world.
There is a linear and an angular damping factor for each body which can be set using `body.Damping`.
With each `world.Step`, the angular and linear velocity of each dynamic rigid body is multiplied by $1-\gamma$, where $\gamma$ is the damping factor.
For performance reasons there is no time dependency for the damping system.
As a result, bodies in a simulation with smaller time steps experience greater damping.

## Speculative contacts

Jitter's built-in speculative contacts are **linear-only**. They help prevent fast, small objects
from tunneling through thin obstacles when their translation between frames would skip a collision.
Enable them on a per-body basis using `body.EnableSpeculativeContacts`.

Speculative bounding box expansion uses only linear velocity. Sweeps predict translation while
holding the shapes at their current orientations. The speculative constraints also ignore angular
surface velocity (`ω × r`) and apply impulses only to linear velocity. They do not prevent
tunneling caused by rotation, including rotation introduced by other contacts or constraints during
the timestep. Bodies still rotate normally, and ordinary overlap contacts retain their angular response.

`world.SpeculativeVelocityThreshold` sets the minimum relative linear speed for speculative
contact generation. `world.SpeculativeRelaxationFactor` controls how much the bodies may close
the predicted gap. This approximation trades collision-response accuracy for reduced tunneling;
smaller timesteps may still be needed for fast translation or rotation.

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
