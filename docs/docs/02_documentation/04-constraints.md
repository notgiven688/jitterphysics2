# Constraints

Constraints constraint various degrees of freedom of rigid bodies.
In Jitter, constraints always act between two bodies.
A joint is a collection of multiple constraints, so it may act on multiple bodies.

## Default constraints

Default constraints available are:

```text
AngularMotor, BallSocket, ConeLimit, DistanceLimit, FixedAngle, HingeAngle, LinearMotor, PointOnLine, PointOnPlane, TwistAngle, SpringConstraint
```

Available joints:

```text
HingeJoint, PrismaticJoint, UniversalJoint
```

## Creating constraints

Similar to rigid bodies but unlike shapes, constraints are instantiated through a create method in the world class:

```cs
var constraint = world.CreateConstraint<ConstraintType>(body1, body2);
```

Jitter allocates and assigns unmanaged memory to constraints internally, allowing them to be used like regular classes.
The solver interacts with unmanaged memory structures of type `ConstraintData` (256 bytes) or `SmallConstraintData` (128 bytes), depending on whether `constraint.IsSmallConstraint` is set.
Small constraints are designed for simpler constraints with a smaller memory footprint, such as spring constraints for soft bodies.

:::danger Constraint Initialization
For all default constraints in Jitter, `constraint.Initialize` must be called once after `world.CreateConstraint`.
:::

### Example: Hinge joint

A swinging door may be implemented by a combination of two constraints: A `HingeAngle` constraint which removes two degrees of angular freedom (i.e. the bodies can only rotate around a single axis), and a `BallSocket` constraint removing all three degrees of freedom (there is no translation possible):

```cs
var hingeAngle = world.CreateConstraint<HingeAngle>(body1, body2);
hingeAngle.Initialize(hingeAxis, AngularLimit.Full);

var ballSocket = world.CreateConstraint<BallSocket>(body1, body2);
ballSocket.Initialize(hingeCenter);
```

Alternatively the `HingeJoint` might be used for convenience:

```cs
var hinge = new HingeJoint(world, body1, body2, hingeCenter, hingeAxis);
```

### Fixed constraints

To constrain a rigid body relative to world space, use `world.NullBody` as one of the bodies.
For example, to keep a capsule upright, a constraint can be set up as follows:

```cs
var upright = world.CreateConstraint<HingeAngle>(capsule, world.NullBody);
upright.Initialize(JVector.UnitY, AngularLimit.Full);
```

### Softness and Bias

For most constraints a softness and bias value can be set.
These values define how strict the constraint limits are enforced.
Softer constraints might improve simulation stability but do not fully enforce the constraint limits.
The softness and bias parameters can be tweaked for optimal results.
Better constraint behaviour can also be archived by sub-stepping, see [world.Step](/docs/documentation/world).

### Enable/Disable constraints

Constraints can be temporarily enabled or disables using `constraint.IsEnabled`.
