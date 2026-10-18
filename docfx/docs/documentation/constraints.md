# Constraints

Constraints restrict degrees of freedom of rigid bodies.
In Jitter2, constraints always act between two bodies.
A joint is a collection of multiple constraints, so it may act on multiple bodies.

## Degrees of Freedom

A rigid body has 6 degrees of freedom (DOF): 3 translational and 3 rotational.
Constraints remove one or more of these degrees of freedom.

| Constraint | Removed DOF | Description |
|------------|-------------|-------------|
| `BallSocket` | 3 translational | Anchors a point on each body together |
| `FixedAngle` | 3 rotational | Locks relative orientation between bodies |
| `HingeAngle` | 2 rotational (3 with limit) | Allows rotation around a single axis only, optionally enforces angular limits |
| `PointOnLine` | 2 translational (3 with limit) | Constrains a point to a line, optionally limits distance along the axis |
| `PointOnPlane` | 1 translational (when limit active) | Constrains a point to a plane, only enforced when the limit is violated |
| `DistanceLimit` | 1 translational (when limit active) | Constrains the distance between anchor points, only enforced when outside the limit range |
| `TwistAngle` | 1 rotational (when limit active) | Limits relative twist around an axis, only enforced when outside the limit range |
| `ConeLimit` | 1 rotational (when limit active) | Limits angular tilt between two axes |
| `EllipticalConeLimit` | 1 rotational (when limit active) | Limits swing between inner and outer elliptical cones; leaves twist free |
| `AngularMotor` | — | Drives angular velocity (does not remove DOF) |
| `LinearMotor` | — | Drives linear velocity (does not remove DOF) |
| `SpringConstraint` | 1 translational | Spring-like force along the anchor connection |

## Joints

Joints are combinations of constraints for common use cases:

| Joint | Constraints used | Remaining DOF |
|-------|------------------|---------------|
| `HingeJoint` | `BallSocket` + `HingeAngle` (+ optional `AngularMotor`) | 1 rotational |
| `PrismaticJoint` | `PointOnLine` + `FixedAngle` or `HingeAngle` (+ optional `LinearMotor`) | 1 translational |
| `UniversalJoint` | `BallSocket` + `TwistAngle` (+ optional `AngularMotor`) | 2 rotational |
| `WeldJoint` | `BallSocket` + `FixedAngle` | 0 (fully locked) |

## Creating constraints

Constraints are instantiated through a create method in the world class:

```cs
var constraint = world.CreateConstraint<ConstraintType>(body1, body2);
```

Jitter2 allocates and assigns unmanaged memory to constraints internally, allowing them to be used like regular classes.
The solver interacts with unmanaged memory structures of type `ConstraintData` (256 bytes) or `SmallConstraintData` (128 bytes), depending on whether `constraint.IsSmallConstraint` is set.
Small constraints are designed for simpler constraints with a smaller memory footprint, such as spring constraints for soft bodies.

> [!CAUTION]
> **Constraint Initialization**
> For all default constraints, `constraint.Initialize` must be called once after `world.CreateConstraint`.

### World space initialization

Constraints are initialized using world space coordinates (anchor points, axes).
The bodies should be positioned in a valid configuration before calling `Initialize`.
The constraint then computes and stores local reference frames for each body based on their current poses.
This means the constraint "remembers" the initial setup and will try to maintain it during simulation.

### Example: Hinge joint

A swinging door can be implemented by combining two constraints: A `HingeAngle` constraint which removes two degrees of angular freedom (the bodies can only rotate around a single axis), and a `BallSocket` constraint removing all three translational degrees of freedom:

```cs
var hingeAngle = world.CreateConstraint<HingeAngle>(body1, body2);
hingeAngle.Initialize(hingeAxis, AngularLimit.Full);

var ballSocket = world.CreateConstraint<BallSocket>(body1, body2);
ballSocket.Initialize(hingeCenter);
```

Alternatively, the `HingeJoint` can be used for convenience:

```cs
var hinge = new HingeJoint(world, body1, body2, hingeCenter, hingeAxis);
```

### Cone limits

`ConeLimit` restricts the angle between two axes to a range from 0° to 180°.
Ranges such as `[0°, 45°]`, `[135°, 180°]`, and `[0°, 180°]` are valid.
Equal limits fix the angle when it lies strictly between 0° and 180°, for example `[30°, 30°]`.

The fixed ranges `[0°, 0°]` and `[180°, 180°]` are rejected by `Initialize` and the `Limit` property.
To keep axes parallel or antiparallel while allowing free twist, use `HingeAngle` with `AngularLimit.Full`:

```cs
var hingeAngle = world.CreateConstraint<HingeAngle>(body1, body2);
hingeAngle.Initialize(hingeAxis, AngularLimit.Full);
```

Position the bodies with the intended axes aligned or antiparallel before initialization.
`HingeAngle` stores their current relative orientation as its reference; it does not pull two
arbitrarily misaligned axes into alignment during initialization.

### Elliptical cone limits

`EllipticalConeLimit` allows different swing limits in two perpendicular directions. Body 1 owns
the cone frame; body 2 owns the axis being constrained. All initialization directions are supplied
in world space and stored relative to their respective bodies.

```cs
var cone = world.CreateConstraint<EllipticalConeLimit>(body1, body2);
cone.Initialize(
    axis: JVector.UnitY,
    referenceDirection: JVector.UnitX,
    limitAlongReference: AngularLimit.FromDegree(15, 60),
    limitAlongPerpendicular: AngularLimit.FromDegree(5, 25));
```

The lower bounds define the inner ellipse (15° and 5°); the upper bounds define the outer ellipse
(60° and 25°). Swing is allowed between these boundaries. Setting both lower bounds to zero
removes the inner boundary. Setting each lower bound equal to its upper bound constrains swing
to one elliptical cone surface. Identical ranges in both directions give a circular cone.

The ellipses are defined in **swing-angle coordinates**: the shortest tilt angle resolved along
the reference direction and `axis cross referenceDirection`. Each boundary satisfies
`(swing1 / angle1)^2 + (swing2 / angle2)^2 = 1`. These are coupled boundaries, not independent
clamps on two angles. The reference direction is projected perpendicular to the cone axis;
parallel directions are rejected. Rotation of body 1 rotates the entire ellipse frame.

Each range must satisfy `0 <= From <= To < 180°`, with a positive upper bound. The lower bounds
must either both be zero or both be positive. Fixed zero and 180° tilt are unsupported. Change
the ranges together using `SetLimits`; inspect the current coordinates using `GetSwing`.
The two-axis `Initialize` overload supports an initially tilted body 2. The single-axis overload
starts at zero tilt, which violates a positive inner boundary and will be corrected by the solver.

Translation and twist of body 2 about its constrained axis remain free. Combine this constraint
with `BallSocket` for a joint, and optionally `TwistAngle` to limit twist.

### Fixed constraints

To constrain a rigid body relative to world space, use `world.NullBody` as one of the bodies.
For example, to keep a capsule upright:

```cs
var upright = world.CreateConstraint<HingeAngle>(capsule, world.NullBody);
upright.Initialize(JVector.UnitY, AngularLimit.Full);
```

### Softness and Bias

For most constraints, softness and bias values can be set.
These values define how strictly the constraint limits are enforced.
Softer constraints may improve simulation stability but do not fully enforce the constraint limits.
The softness and bias parameters can be tweaked for optimal results.
Better constraint behavior can also be achieved by sub-stepping, see [World](world.md).

### Enable/Disable constraints

Constraints can be temporarily enabled or disabled using `constraint.IsEnabled`.

## Debug drawing

Constraints and joints can draw their anchors, axes, and limits through an `IDebugDrawer`:

```cs
constraint.DebugDraw(drawer);
joint.DebugDraw(drawer);
```

See [setting up a debug drawer](bodies.md#debugging-shapes) to connect this to your renderer.
Call these methods after the physics step, while the world is not being modified by another thread.

Green shows reference guides and upper or fixed limits, red shows lower limits, and white shows
current anchors and directions. Coordinate frames use red for X, green for Y, and blue for Z.

| Constraint | Reading the drawing |
|------------|---------------------|
| `BallSocket` | The two anchor markers should coincide. The line between them shows separation. |
| `DistanceLimit` | Spheres around the first anchor show the allowed distances to the second anchor. |
| `PointOnLine` | Crosses mark stops along the line; arrows mark unbounded ends. A white line shows displacement away from the line. |
| `PointOnPlane` | Grids show the limiting planes. A white projection line appears when the anchor is outside the allowed region. The planes extend beyond the grids. |
| `ConeLimit`, `EllipticalConeLimit` | The white axis must stay within the outer cone and outside any red inner cone. Equal limits allow motion along one cone surface. |
| `HingeAngle` | The arc shows the allowed angles and the white ray shows the current angle. The short axis arrows reveal hinge misalignment. |
| `TwistAngle` | The dial shows the current twist and its limits. When twist is fixed, crossed circles illustrate the two remaining swing directions. |
| `FixedAngle` | The frame near body 2 shows its target orientation; the frame at body 2 shows its current orientation. Corresponding axes should be parallel. In a joint, the target frame is placed at the anchor. |
| `LinearMotor` | Green arrows show body 1's contribution and white arrows show body 2's contribution to the requested relative motion. These show target direction, not measured velocity; a static body stays still. |
| `AngularMotor` | With a fixed support, one green circle and arrow show the rotor's target rotation. When both bodies can respond, separate green and white guides show their respective target directions. |
| `SpringConstraint` | The coil joins the anchors; the green cross marks the rest length along the spring direction. |

Pass `DebugDrawSettings` to change the appearance:

```cs
constraint.DebugDraw(drawer, new DebugDrawSettings
{
    GuideSize = 2,
    FrameSize = 1.5f
});
```

`GuideSize` changes the size of guides and markers. `FrameSize` sets the length of coordinate-frame
axes in world units. Arrowhead size and shaft thickness are adjusted separately using
`ArrowHeadSize` and `ArrowShaftRadius`. Physical distances and angular limits keep their actual values.
See <xref:Jitter2.DebugDrawSettings> for color, arrow, and detail settings.

Arrows are solid by default. Set `SolidArrows = false` for wire arrows. `ArrowSegments` controls
arrow smoothness, while `ArrowHeadSpokes` controls the number of spokes in wire arrowheads.
Your drawer's `DrawTriangle` implementation controls whether solid arrows and anchor markers
appear filled or as triangle edges. When drawing your own guides, `DrawArrow` and `DrawFrame`
use the same defaults; `DrawWireArrow` and `DrawWireFrame` explicitly draw wire geometry.

## Constraint lifetime

Constraints are attached to their two rigid bodies. They can become invalid without an explicit
call to `world.Remove(constraint)` when:

- either connected body is removed;
- changing a body's motion type leaves both connected bodies non-dynamic;
- `world.Clear()` removes the connected bodies; or
- the world is disposed.

Use `constraint.IsValid` before accessing a retained constraint reference when another operation
may have removed it:

```cs
world.Remove(body1); // also removes constraints attached to body1

if (constraint.IsValid)
{
    constraint.IsEnabled = false;
}
```

Once `IsValid` is `false`, the constraint's unmanaged data is no longer available and its
data-backed properties and methods must not be accessed. Rigid bodies provide the equivalent
`body.IsValid` check after body removal or world disposal.

For reactive cleanup, `World.ConstraintRemoved` is raised when a constraint is removed explicitly
or automatically:

```cs
world.ConstraintRemoved += removedConstraint =>
{
    // Drop any application-side state associated with removedConstraint.
};
```

The constraint is already invalid when the callback runs. Disabling a constraint and disposing the
world do not raise this event.
