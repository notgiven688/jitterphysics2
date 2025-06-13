/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// Constructs a prismatic joint utilizing a <see cref="PointOnLine"/> constraint in conjunction with
/// <see cref="FixedAngle"/>, <see cref="HingeAngle"/>, and <see cref="LinearMotor"/> constraints.
/// </summary>
public class PrismaticJoint : Joint
{
    public RigidBody Body1 { get; private set; }
    public RigidBody Body2 { get; private set; }

    public PointOnLine Slider { get; }

    public FixedAngle? FixedAngle { get; }
    public HingeAngle? HingeAngle { get; }
    public LinearMotor? Motor { get; }

    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis,
        bool pinned = true, bool hasMotor = false) :
        this(world, body1, body2, center, axis, LinearLimit.Full, pinned, hasMotor)
    {
    }

    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis, LinearLimit limit,
        bool pinned = true, bool hasMotor = false)
    {
        Body1 = body1;
        Body2 = body2;

        JVector.NormalizeInPlace(ref axis);

        Slider = world.CreateConstraint<PointOnLine>(body1, body2);
        Slider.Initialize(axis, center, center, limit);
        Register(Slider);

        if (pinned)
        {
            FixedAngle = world.CreateConstraint<FixedAngle>(body1, body2);
            FixedAngle.Initialize();
            Register(FixedAngle);
        }
        else
        {
            HingeAngle = world.CreateConstraint<HingeAngle>(body1, body2);
            HingeAngle.Initialize(axis, AngularLimit.Full);
            Register(HingeAngle);
        }

        if (hasMotor)
        {
            Motor = world.CreateConstraint<LinearMotor>(body1, body2);
            Motor.Initialize(axis, axis);
            Register(Motor);
        }
    }
}