/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// Allows two bodies to slide along an axis, with optional distance limits and a motor or servo.
/// Relative rotation can be locked or allowed around the sliding axis.
/// </summary>
public class PrismaticJoint : Joint
{
    public RigidBody Body1 { get; private set; }
    public RigidBody Body2 { get; private set; }

    public PointOnLine Slider { get; }

    public FixedAngle? FixedAngle { get; }
    public HingeAngle? HingeAngle { get; }
    public LinearMotor? Motor { get; }
    public LinearAxisServo? Servo { get; }

    /// <inheritdoc cref="PrismaticJoint(World, RigidBody, RigidBody, JVector, JVector, LinearLimit, JointDrive, bool)"/>
    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis) :
        this(world, body1, body2, center, axis, LinearLimit.Full, JointDrive.None)
    {
    }

    /// <inheritdoc cref="PrismaticJoint(World, RigidBody, RigidBody, JVector, JVector, LinearLimit, JointDrive, bool)"/>
    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis,
        bool pinned) : this(world, body1, body2, center, axis, LinearLimit.Full, JointDrive.None, pinned)
    {
    }

    /// <inheritdoc cref="PrismaticJoint(World, RigidBody, RigidBody, JVector, JVector, LinearLimit, JointDrive, bool)"/>
    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis,
        LinearLimit limit) : this(world, body1, body2, center, axis, limit, JointDrive.None)
    {
    }

    /// <inheritdoc cref="PrismaticJoint(World, RigidBody, RigidBody, JVector, JVector, LinearLimit, JointDrive, bool)"/>
    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis,
        LinearLimit limit, bool pinned) : this(world, body1, body2, center, axis, limit, JointDrive.None, pinned)
    {
    }

    /// <inheritdoc cref="PrismaticJoint(World, RigidBody, RigidBody, JVector, JVector, LinearLimit, JointDrive, bool)"/>
    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis,
        JointDrive drive, bool pinned = true) : this(world, body1, body2, center, axis, LinearLimit.Full, drive, pinned)
    {
    }

    /// <inheritdoc cref="PrismaticJoint(World, RigidBody, RigidBody, JVector, JVector, LinearLimit, JointDrive, bool)"/>
    [Obsolete("Use the overload accepting JointDrive instead of hasMotor.")]
    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis,
        bool pinned = true, bool hasMotor = false) :
        this(world, body1, body2, center, axis, LinearLimit.Full,
            hasMotor ? JointDrive.Motor : JointDrive.None, pinned)
    {
    }

    /// <inheritdoc cref="PrismaticJoint(World, RigidBody, RigidBody, JVector, JVector, LinearLimit, JointDrive, bool)"/>
    [Obsolete("Use the overload accepting JointDrive instead of hasMotor.")]
    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis, LinearLimit limit,
        bool pinned = true, bool hasMotor = false) :
        this(world, body1, body2, center, axis, limit,
            hasMotor ? JointDrive.Motor : JointDrive.None, pinned)
    {
    }

    /// <summary>
    /// Initializes a new prismatic joint.
    /// </summary>
    /// <param name="world">The world containing both bodies.</param>
    /// <param name="body1">The first body.</param>
    /// <param name="body2">The second body.</param>
    /// <param name="center">The joint center in world space.</param>
    /// <param name="axis">The sliding axis in world space.</param>
    /// <param name="limit">Allowed displacement along the sliding axis.</param>
    /// <param name="pinned">If true, locks relative rotation; otherwise allows rotation around the sliding axis.</param>
    /// <param name="drive">The optional drive. Motor and servo force default to zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">The drive selection is invalid.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="center"/> contains a non-finite value, when <paramref name="axis"/> is zero
    /// or contains a non-finite value, when either limit value is NaN, when either body does not belong to
    /// <paramref name="world"/>, or when both body references are the same.
    /// </exception>
    public PrismaticJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector axis, LinearLimit limit,
        JointDrive drive, bool pinned = true)
    {
        Body1 = body1;
        Body2 = body2;

        ArgumentCheck.Finite(center, nameof(center));
        ArgumentCheck.NonZero(axis, nameof(axis));
        ArgumentCheck.NotNaN(limit.From, nameof(limit.From));
        ArgumentCheck.NotNaN(limit.To, nameof(limit.To));
        if (drive is not (JointDrive.None or JointDrive.Motor or JointDrive.Servo))
            throw new ArgumentOutOfRangeException(nameof(drive));

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

        if (drive == JointDrive.Motor)
        {
            Motor = world.CreateConstraint<LinearMotor>(body1, body2);
            Motor.Initialize(axis, axis);
            Register(Motor);
        }
        else if (drive == JointDrive.Servo)
        {
            Servo = world.CreateConstraint<LinearAxisServo>(body1, body2);
            Servo.Initialize(axis, center, center);
            Register(Servo);
        }
    }
}
