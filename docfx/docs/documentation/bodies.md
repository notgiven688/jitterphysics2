# Rigid Bodies

A rigid body represents an object's position, rotation, mass, and motion. Attach shapes to define what it collides with.

## Creating a body

Create a body in the world that will simulate it:

```cs
var world = new World();
var body = world.CreateRigidBody();
```

## Adding shapes

A body can have more than one shape:

```cs
body.AddShapes([new SphereShape(radius: 2), new BoxShape(size: 1)]);
```

Shapes determine how bodies collide with each other.

> [!WARNING]
> **Creating many bodies at once**
> Set each body's position before adding shapes. Shapes enter collision detection immediately,
> so adding many at the origin creates unnecessary overlap checks.

> [!WARNING]
> **Passing the same instance to multiple bodies**
> Each shape instance can belong to only one body. Create a new instance for each body, or use `Clone()` on shape types that support it.

Built-in primitives use their center of mass as the local origin, $(0, 0, 0)$.

Adding a shape recalculates the body's mass and inertia from all its shapes, assuming a density of 1.

Adding just a sphere

```cs
body.AddShape(new SphereShape(radius: 1));
```

gives the body the mass and inertia of a sphere with radius 1 and density 1.

The mass properties of the body can also be set directly using `body.SetMassInertia`.
Inertia tensors use `JSymmetricMatrix`, which stores the six independent components of a symmetric matrix.
Both `body.InverseInertia` and shape mass-property calculations use this type.

```cs
body.SetMassInertia(JSymmetricMatrix.CreateScale(2, 3, 4), mass: 5);
```

The tensor is expressed in body space, about the body's center of mass.

Passing `MassInertiaUpdateMode.Preserve` to `body.AddShape(...)` or `body.AddShapes(...)` prevents the automatic recalculation of mass properties when shapes are added.

> [!IMPORTANT]
> The body's position represents its center of mass. Arrange its shapes so their combined center of mass is at the local origin, $(0, 0, 0)$.
> Use shape offsets when needed; see the [teapot example](../tutorials/teapots/hello-world.md).

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

`body.AllowedMotion` selects the directions in which a body can move and the axes around which it can rotate. The default, `MotionAxes.All`, allows all motion. Setting the property replaces the previous selection.

All selected axes are in **world space**.

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

Forces, impulses, contacts, and joints cannot move a body in a blocked direction. The restrictions also apply to kinematic velocities, but you can still set `Position` and `Orientation` directly.

Restrictions preserve `body.Mass` and its body-space `InverseInertia`. If rotation around one axis affects rotation around another, locking an axis also changes the response around the remaining axes.

Changing shapes or calling `SetMassInertia` updates mass properties without removing the restrictions. To allow all motion again:

```cs
body.AllowedMotion = MotionAxes.All;
```

Changing `AllowedMotion` stops motion in the blocked directions, clears saved contact and joint impulses, and schedules affected bodies to wake. Allowing motion again does not restore the old velocities. Gyroscopic forces are skipped while any axis is restricted.

Do not change this property while another thread is stepping the world.

`MotionAxes.None` blocks all simulated movement without changing the body's `MotionType`. The body still participates in contacts and connected groups. Use `MotionType.Static` for ordinary static geometry.

Planar scenes still use 3D collision shapes. Bodies keep their initial position along the blocked axis, so place them in the same plane when building the scene.

### Custom constraints

When writing a custom solver, use `RigidBodyData.InverseMassVector` for the inverse mass along each world axis. Calculate a linear velocity change with `JVector.Multiply(data.InverseMassVector, impulse)`. Along a constraint direction, the inverse effective mass is `JVector.Multiply(direction, data.InverseMassVector) * direction`.

`JVector.Multiply` multiplies matching components. The `*` operator between two vectors calculates a dot product, so it cannot replace that operation.

`RigidBodyData.InverseInertiaWorld` is a `JSymmetricMatrix` stored in six scalars. It supports
`JVector.Transform` and can be expanded to `JMatrix` using `ToMatrix()`. Custom solvers must handle zero
response in prohibited directions, including singular effective-mass matrices. The body data
occupies 128 bytes in single precision and 256 bytes in double precision. Access its fields
by name rather than relying on field offsets.

An effective-mass matrix can be singular when motion is blocked. For symmetric, positive-semidefinite matrices, `MathHelper.PseudoInverseSymmetric` calculates an inverse for the allowed motion and leaves blocked directions at zero:

```cs
JMatrix effectiveMass = MathHelper.PseudoInverseSymmetric(inverseEffectiveMass);
```

Compute this during constraint preparation and reuse the result in solver iterations.
The method assumes finite, symmetric, positive-semidefinite input. It uses a numerical
cutoff when diagonalizing singular or nearly singular matrices; see its API documentation
for the precision-dependent tolerance.

## Damping

Damping gradually slows a body's motion. Set the linear and angular factors with `body.Damping`; zero disables damping and one stops that motion.

Each call to `world.Step` multiplies velocity by $1-\gamma$, where $\gamma$ is the corresponding damping factor. Damping is applied per step, so smaller time steps produce more damping over the same elapsed time.

## Continuous collision detection

Continuous collision detection (CCD) helps prevent fast-moving or rotating bodies from passing through obstacles between simulation steps. Enable it with `body.EnableCcd = true`.

When two bodies both move quickly, enable CCD on both, for example two fast projectiles. A moving body without CCD can otherwise be missed.

CCD predicts collisions from both movement and rotation. A collision response can change that prediction, so Jitter checks again using the new velocities. It also checks bodies set in motion by those responses, even if they did not have CCD enabled.

Three world settings control the cost and behavior:

- `CcdIterations`: number of collision discovery passes. More passes can find additional collisions but cost more. Default is 4.
- `CcdVelocityThreshold`: pairs below this estimated relative surface speed skip CCD checks. The estimate includes translation and rotation. Default is `0.1` world units per second.
- `CcdRelaxationFactor`: fraction of the predicted gap that a contact allows to close during one step. Default is `0.9`, leaving a small separation margin.

A fixed number of passes cannot guarantee that every collision is found. Joints are solved afterward and can change motion again. Smaller time steps may still be needed.

## Friction and Restitution

Friction and restitution coefficients may be set through `body.Friction` and `body.Restitution`.
For a collision of two bodies with different coefficients the maximum value of each body is taken.

## Collide events

`RigidBody` provides two events: `BeginCollide` and `EndCollide`.
`BeginCollide` is raised when a contact pair begins, including a predicted CCD contact. `EndCollide` is raised when that pair expires during a simulation step; explicit removal does not raise it.
See [Arbiters](arbiters.md#collision-events) for details and examples.

## Activation/Deactivation

Bodies connected through contacts or constraints form an **island**. These groups go to sleep and wake together.

A body is ready to sleep once its linear and angular speeds stay below `body.DeactivationThreshold` for `body.DeactivationTime` seconds. When all non-static bodies in an island are ready, the island sleeps. Sleeping bodies have very little simulation cost and can wake when they collide with an active body.

`body.SetActivationState(true)` requests a wake-up on the next step. Passing `false` requests sleep, but a body that is still moving too fast, or is connected to an active body, can stay awake. The call does not immediately change `body.IsActive`.

For explicit immediate deactivation, use `world.ForceSleepIsland(island)`.
This deactivates the whole island at once, ignores the normal deactivation thresholds, and clears linear velocity, angular velocity, queued forces, and queued torques for all bodies in the island.

Activation state is most meaningful for dynamic and kinematic bodies:

- Dynamic bodies are actively simulated while awake and skipped while sleeping.
- Kinematic bodies can be part of islands. They are treated as infinite-mass bodies by the solver, but they can wake connected dynamic bodies through contacts or constraints.
- Static bodies remain inactive and do not connect islands. Calling `body.SetActivationState(true)` does not activate the static body itself.

## Static bodies

Static bodies (`body.MotionType == MotionType.Static`) have infinite mass and therefore are not affected by collisions or constraints.
They also do not join islands.
Static bodies do not generate collisions with other static or inactive bodies.
Static bodies are normally inactive even if `body.SetActivationState(true)` is called.

Changing a static body's position or orientation updates its collision bounds and wakes affected non-static bodies on the next step. The static body stays inactive. Use this for occasional edits to level geometry; use a kinematic body for a platform or obstacle that moves continuously.

## Kinematic bodies

Kinematic bodies (`body.MotionType == MotionType.Kinematic`) move according to their assigned linear and angular velocities. Forces, collisions, and constraints do not change those velocities. They can push dynamic bodies and take part in island activation and sleeping.
