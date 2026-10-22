# Constraints

Constraints limit how two bodies can move relative to each other. A joint combines constraints into a familiar connection, such as a hinge or a weld.

For a complete example with two boxes, a hinge, and a debug drawer, follow the [Hinge tutorial](../tutorials/hinge/project-setup.md).

## Degrees of Freedom

A body can move along three axes and rotate around three axes. These are its six **degrees of freedom** (DOF). A constraint restricts one or more of them.

| Constraint | Removed DOF | Description |
|------------|-------------|-------------|
| `BallSocket` | 3 translational | Anchors a point on each body together |
| `FixedAngle` | 3 rotational | Locks relative orientation between bodies |
| `HingeAngle` | 2 rotational (3 with limit) | Allows rotation around a single axis only, optionally enforces angular limits |
| `PointOnLine` | 2 translational (3 with limit) | Constrains a point to a line, optionally limits distance along the axis |
| `PointOnPlane` | 1 translational (when limit active) | Keeps an anchor on a plane or within a range of distances from it |
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
| `HingeJoint` | `BallSocket` + `HingeAngle` (+ optional `AngularMotor` or `TwistServo`) | 1 rotational |
| `PrismaticJoint` | `PointOnLine` + `FixedAngle` or `HingeAngle` (+ optional `LinearMotor` or `LinearAxisServo`) | 1 translational; also 1 rotational when `pinned` is false |
| `UniversalJoint` | `BallSocket` + `TwistAngle` (+ optional `AngularMotor` or `AngularServo`) | 2 rotational |
| `WeldJoint` | `BallSocket` + `FixedAngle` | 0 (fully locked) |

### Joint drives

`HingeJoint`, `PrismaticJoint`, and `UniversalJoint` can create an optional motor or servo.
Use `JointDrive.Motor` to control speed or `JointDrive.Servo` to reach a target position or angle. Omit the drive, or use `JointDrive.None`, for an undriven joint.

```cs
var elbow = new HingeJoint(
    world, upperArm, lowerArm, center, axis,
    angle: AngularLimit.FromDegree(0, 135),
    drive: JointDrive.Servo);

elbow.Servo!.TargetAngle = JAngle.FromDegree(90);
elbow.Servo.MaximumTorque = 20;
```

A hinge's `Servo` is a `TwistServo`. Its target uses the same zero angle and positive rotation direction as the hinge's `HingeAngle.Angle`.

A prismatic joint's `Servo` is a `LinearAxisServo`. Its target position uses the same zero and direction as the joint's `Slider.Distance`:

```cs
var slider = new PrismaticJoint(
    world, body1, body2, center, axis,
    limit: new LinearLimit(0, 2),
    drive: JointDrive.Servo);

slider.Servo!.TargetPosition = 1.5f;
slider.Servo.MaximumForce = 100;
```

A universal joint's `Servo` is an `AngularServo`. Its target is the orientation of body 2 relative to body 1. Initially, it holds their current relative orientation:

```cs
var universal = new UniversalJoint(
    world, body1, body2, center, axis1, axis2,
    drive: JointDrive.Servo);

universal.Servo!.TargetRelativeOrientation = targetRelativeOrientation;
universal.Servo.MaximumTorque = 20;
```

The twist constraint remains active, so only orientations allowed by the joint can be reached.

Drives start with a maximum force or torque of zero. Set this limit to make the drive act. The drive does not replace the joint's mechanical limits. Enabling, disabling, removing, or drawing the joint also applies to its drive.

The `Motor` property is non-null only with `JointDrive.Motor`; `Servo` is non-null only with `JointDrive.Servo`.

The older `hasMotor` constructor overloads are obsolete and remain supported. Replace
`hasMotor: true` with `drive: JointDrive.Motor`, or omit the drive when no motor is needed.

## Creating constraints

Create constraints through the world:

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

Supply anchor points and axes in world coordinates. Most constraints use the bodies' current positions and orientations as their reference, so arrange the bodies as intended before calling `Initialize`. The anchors and axes then move with their respective bodies. Servos can instead be initialized with a target they have not yet reached.

### Example: Hinge joint

For a swinging door, `BallSocket` keeps the hinge anchors together and `HingeAngle` allows rotation around the hinge axis:

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

`EllipticalConeLimit` allows different tilt limits in two perpendicular directions. The cone is attached to body 1 and limits an axis attached to body 2. Supply the directions in world space; they will rotate with their respective bodies.

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

The limits form an ellipse, rather than two independent angle limits. Tilting partway toward one limit leaves less room in the perpendicular direction. `GetSwing` returns these two tilt components, measured along the reference direction and `axis cross referenceDirection`. Each boundary satisfies `(swing1 / angle1)^2 + (swing2 / angle2)^2 = 1`.

The reference direction is projected onto the plane perpendicular to the cone axis. A direction parallel to the axis is rejected.

Each range must satisfy `0 <= From <= To < 180°`, with a positive upper bound. The lower bounds
must either both be zero or both be positive. Fixed zero and 180° tilt are unsupported. Change
the ranges together using `SetLimits`; inspect the current coordinates using `GetSwing`.
The two-axis `Initialize` overload supports an initially tilted body 2. The single-axis overload
starts at zero tilt, which violates a positive inner boundary and will be corrected by the solver.

Translation and twist of body 2 about its constrained axis remain free. Combine this constraint
with `BallSocket` for a joint, and optionally `TwistAngle` to limit twist.

### Servos

Servos drive bodies toward a target configuration. They can start outside that configuration,
and their targets can change between simulation steps. Changing a target wakes sleeping
connected dynamic bodies on the next step. Motors control velocity; servos control
position or orientation.

| Servo | Controls | Target |
|-------|----------|--------|
| `AngularServo` | 3 angular directions | `TargetRelativeOrientation`: body 2's orientation in body 1's frame |
| `TwistServo` | 1 angular direction | `TargetAngle`: twist relative to the initial zero-twist pose |
| `LinearServo` | 3 linear directions | `TargetPosition`: offset from anchor 1 to anchor 2, in body 1's frame |
| `LinearAxisServo` | 1 linear direction | `TargetPosition`: signed anchor separation along the axis attached to body 1 |

Maximum force or torque defaults to **zero**, as with motors. Set `MaximumForce` or
`MaximumTorque` to enable the drive. Positive infinity allows unlimited force or torque.
`Bias` controls how quickly the servo corrects its error. Higher `Softness` lets it yield more under load.

For example, drive two robot arm segments toward a relative orientation:

```cs
var servo = world.CreateConstraint<AngularServo>(upperArm, lowerArm);
servo.Initialize(JQuaternion.CreateRotationX(0.5f));
servo.MaximumTorque = 20;

// Choose a different configuration later without moving the bodies directly.
servo.TargetRelativeOrientation = JQuaternion.CreateRotationX(1.0f);
```

The parameterless `AngularServo.Initialize()` captures the current relative orientation instead.
`LinearServo.Initialize(anchor1, anchor2)` accepts separate world-space attachment points;
its default target of zero pulls them together. `LinearAxisServo.Initialize` also
accepts the desired signed separation. `TwistServo.Initialize` accepts a target angle relative
to its initial reference, so a nonzero target begins with an angular error.

`TwistServo` uses signed joint angles from -180° to +180°. Moving from -170° to +170°
travels 340° through zero. Targets outside this interval are rejected; revolutions are not
tracked. Use mechanical limits to keep the joint's motion inside the angular interval.
`AngularServo` targets an orientation and uses the shortest rotation to reach it, so
equivalent orientations represent the same target.

Servos act alongside mechanical constraints. For an elbow, use a hinge to keep the arm segments
connected and enforce the joint's angle limits, then add a `TwistServo` around the same axis.
Its torque limit controls the actuator's strength while the hinge retains its mechanical limits.

### Fixed constraints

To constrain a rigid body relative to world space, use `world.NullBody` as one of the bodies.
For example, to keep a capsule upright:

```cs
var upright = world.CreateConstraint<HingeAngle>(capsule, world.NullBody);
upright.Initialize(JVector.UnitY, AngularLimit.Full);
```

### Softness and Bias

`Bias` controls how quickly a constraint corrects position or angle errors. Higher `Softness` lets the constraint yield more under load, which can improve stability at the cost of accuracy.

[Substeps](world.md#substep-count) can also improve joint stability.

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
| `HingeAngle` | An arc shows the allowed angles, or a green ray marks a fixed angle. The white ray shows the current angle. Short axis arrows reveal hinge misalignment. |
| `TwistAngle` | The dial shows the current twist and its limits. The white axis shows the current body direction; swing remains free. |
| `FixedAngle` | Each body has a coordinate frame adjusted for the initial relative orientation. Corresponding axes stay parallel when the constraint is satisfied. No frame is drawn for `World.NullBody`. |
| `LinearMotor` | Green arrows show body 1's contribution and white arrows show body 2's contribution to the requested relative motion. These show target direction, not measured velocity; a static body stays still. |
| `AngularMotor` | A green circle and white arrow show the target rotation of body 2, or body 1 when body 2 is static or kinematic. Short lines show both motor axes. The arrow shows target direction, not measured speed; a zero target has no arrow. |
| `AngularServo` | A green target frame and an RGB current frame share an origin. Corresponding axes align when the target is reached. |
| `TwistServo` | The dial marks the target twist and the current twist angle. |
| `LinearServo` | Green and white markers show the target and current anchor positions; the connecting line shows the position error. |
| `LinearAxisServo` | Markers show target and current position projected onto the reference axis. Perpendicular motion is uncontrolled. |
| `SpringConstraint` | The coil joins the anchors; the green cross marks the rest length along the spring direction. |

Pass `DebugDrawSettings` to change the appearance:

```cs
constraint.DebugDraw(drawer, new DebugDrawSettings
{
    GuideSize = 2,
    FrameSize = 1.5f
});
```

`GuideSize` changes the size of guides. `FrameSize` sets the length of coordinate-frame
axes in world units, and `PointRadius` sets the radius of anchor spheres. Arrowhead size and
shaft thickness are adjusted separately using `ArrowHeadSize` and `ArrowShaftRadius`.
Physical distances and angular limits keep their actual values.
See <xref:Jitter2.DebugDrawSettings> for color, arrow, and detail settings.

Arrows are solid by default. Set `SolidArrows = false` for wire arrows. `ArrowSegments` controls
how round arrowheads and shafts look.
Your drawer's `DrawTriangle` implementation controls whether solid arrows and anchor markers
appear filled or as triangle edges. When drawing your own guides, `DrawArrow` and `DrawFrame`
use the same defaults; `DrawWireArrow` and `DrawWireFrame` explicitly draw wire geometry.

## Constraint lifetime

Removing a constraint makes its simulation data unavailable. This also happens automatically when:

- either connected body is removed;
- `world.Clear()` removes the connected bodies; or
- the world is disposed.

Changing a body's motion type does not remove the constraint. It is suspended while neither connected body is dynamic.

Check `constraint.IsValid` before using a reference that may have been removed:

```cs
world.Remove(body1); // also removes constraints attached to body1

if (constraint.IsValid)
{
    constraint.IsEnabled = false;
}
```

If `IsValid` is `false`, do not use properties or methods that access the constraint's simulation data. Rigid bodies have the same `body.IsValid` check after removal or world disposal.

Use `World.ConstraintRemoved` to clean up application data when a constraint is removed:

```cs
world.ConstraintRemoved += removedConstraint =>
{
    // Drop any application-side state associated with removedConstraint.
};
```

The constraint is already invalid when the callback runs. Disabling a constraint and disposing the
world do not raise this event.
