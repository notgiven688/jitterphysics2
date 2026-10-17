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
JitterDemo's **Degrees of Freedom** gallery includes an elliptical swing example with both
inner and outer limits: green outer boundaries, red inner boundaries, and white current axes.
Its 13 exhibits demonstrate every built-in constraint and three composite joints in a centered
4×4 grid filled left to right, top to bottom in the initial view, with the final three cells at
the lower right empty. Individual constraint exhibits are ordered alphabetically, followed by
hinge, universal, and weld. Fixed columns and equal square floor outlines keep the layout regular,
with room for each exhibit's motion.
The initial camera shows the full layout from above, with motion guides and world-anchored
labels drawn in the Vellum overlay.
`HingeJoint` demonstrates `HingeAngle`, and `UniversalJoint` demonstrates the fixed-twist case
of `TwistAngle`. Each joint's label names its component constraints. The labels keep a readable
screen size, share the main panel's background and border colors, and appear within 20 world
units of an exhibit. Use **WASD** and the right mouse
button to move around the gallery; drag the bodies to explore their allowed motion.
The `PointOnPlane` exhibit also uses a `DistanceLimit` to keep its anchor inside the red circle.
The `FixedAngle` exhibit similarly bounds translation to the red sphere with a `DistanceLimit`.
The `AngularMotor` exhibit uses `BallSocket` and an unrestricted `HingeAngle` to keep the rotor
in the displayed plane while allowing continuous rotation about its axis.
The joints use the same simple bodies and debug guides as the individual constraints: a bounded
angular arc for the hinge, crossed swing guides for the universal joint, and matching frames for
the weld. The sliding motion of a prismatic joint is already covered by `PointOnLine` and the
guided `LinearMotor` example, so it has no separate exhibit.

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

Every built-in constraint and joint implements `IDebugDrawable`. Supply an `IDebugDrawer`
to render anchors, reference frames, current directions, and limits:

```cs
constraint.DebugDraw(drawer);

var settings = new DebugDrawSettings
{
    Scale = 2,
    Segments = 64,
    ArrowHeadSegments = 12,
    ArrowHeadSpokes = 8,
    Origin = hingeCenter
};
hinge.DebugDraw(drawer, settings);
```

`Scale` sets the radius of angular guides, the half extent of plane patches, and the size of
markers. Linear stops and distance shells always use their physical distances. Infinite lines
and planes are represented by finite guides; unbounded line ends have arrows instead of stops.
`Origin` optionally places angular guides at a world-space point. By default, angular constraints
use body 1's position (body 2's position when body 1 is `World.NullBody`), and joints with a ball
socket use its anchor. `FixedAngle` places its
reference frame near body 2 by default. Positional constraints always draw at their actual anchors.
`Segments` controls guide curve detail from 8 to 256; its default is 48.
`ArrowHeadSegments` independently controls arrowhead bases from 3 to 256, with a default of 12.
Use 4 for square bases or higher values for rounder bases. `ArrowHeadSpokes` sets the number of
segments joining the base to the tip, from 1 to 256, with a default of 8. The spoke count is
clamped to the number of base vertices. A default-initialized settings value has the same behavior
as `DebugDrawSettings.Default`.
`ShapeSubdivisions` controls rigid-body shape tessellation when drawing bodies with these settings;
see [Debugging shapes](bodies.md#debugging-shapes).

Green identifies reference geometry and upper or fixed limits, red identifies lower limits,
and white identifies the current pose. Override `ReferenceColor`, `LowerLimitColor`,
`UpperLimitColor`, and `CurrentColor` in the settings to change these colors. Colors are unsigned
32-bit values packed as **`0xRRGGBBAA`**; `DebugDrawColors` provides the default constants.

An `IDebugDrawer` implementation supplies one method, receiving world-space endpoints and a color:

```cs
void DrawSegment(in JVector pA, in JVector pB, uint color);
```

This replaces the previous interface containing uncolored segments, triangles, and points.
Existing drawers must implement the colored segment method. The uncolored `DrawSegment` extension
supplies white. `DrawPoint(point, radius, color)` is a geometry helper that draws three crossing segments
along the world X, Y, and Z axes. Each segment extends by `radius` on either side of the point.
Constraint anchor markers use `0.06 * Scale`, independently of `Segments`.

The settings overload on `IDebugDrawable` falls back to the original drawing method for existing
implementations. `Constraint` provides the same fallback for custom constraints. Default interface
methods are called through the interface when the concrete class does not declare the corresponding overload.

`DebugDrawGeometry` provides reusable point markers, wire circles, arcs, spheres, cones, planes, arrows,
frames, stops, and springs. These helpers only emit colored segments and have no rendering dependency.
Arrows have a shaft and a wire cone head with a polygonal base and spokes joining the base to the tip.
The `DrawArrow` and `DrawFrame` helpers also accept optional `segments` and `spokes` arguments for arrowhead detail.
They use the library's selected precision. Drawing does not modify bodies or solver data and
must be called while the world is not being stepped or modified by another thread.

The spring's default drawing uses a reference frame derived from body 1. For a coil that stays
continuous through arbitrary motion, retain a separate reference direction in visualization
state and use the spring-specific overload, as the gallery does:

```cs
// Retain this variable between frames, independently for each spring.
JVector coilReference = JVector.UnitX;

// Each drawing frame:
spring.DebugDraw(drawer, ref coilReference, DebugDrawSettings.Default);
```

JitterDemo's gallery uses these built-in drawings; its extra guides illustrate supporting
constraints such as the distance limit around the plane example.

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
