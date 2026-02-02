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
| `HingeAngle` | 2 rotational | Allows rotation around a single axis only |
| `PointOnLine` | 2 translational | Constrains a point to a line (1 DOF with limit) |
| `PointOnPlane` | 1 translational | Constrains a point to a plane |
| `DistanceLimit` | 1 translational | Maintains distance between anchor points |
| `TwistAngle` | 1 rotational | Limits relative twist around an axis |
| `ConeLimit` | 1 rotational | Limits angular tilt between two axes |
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
