/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// Connects two bodies at a shared anchor, allowing rotation about two axes while constraining twist.
/// Supports an optional motor or servo.
/// </summary>
public class UniversalJoint : Joint
{
    public RigidBody Body1 { get; private set; }
    public RigidBody Body2 { get; private set; }

    public TwistAngle TwistAngle { get; }
    public BallSocket BallSocket { get; }
    public AngularMotor? Motor { get; }
    public AngularServo? Servo { get; }

    /// <summary>
    /// Initializes a new universal joint.
    /// </summary>
    /// <param name="center">The shared anchor point in world space.</param>
    /// <param name="rotateAxis1">The rotation axis attached to body 1, in world space.</param>
    /// <param name="rotateAxis2">The rotation axis attached to body 2, in world space.</param>
    /// <param name="drive">The optional drive. Motor and servo torque default to zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">The drive selection is invalid.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="center"/> contains a non-finite value, when either rotation axis is zero
    /// or contains a non-finite value, when either body does not belong to <paramref name="world"/>,
    /// or when both body references are the same.
    /// </exception>
    public UniversalJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector rotateAxis1, JVector rotateAxis2,
        JointDrive drive = JointDrive.None)
    {
        Body1 = body1;
        Body2 = body2;

        ArgumentCheck.Finite(center, nameof(center));
        ArgumentCheck.NonZero(rotateAxis1, nameof(rotateAxis1));
        ArgumentCheck.NonZero(rotateAxis2, nameof(rotateAxis2));
        if (drive is not (JointDrive.None or JointDrive.Motor or JointDrive.Servo))
            throw new ArgumentOutOfRangeException(nameof(drive));

        JVector.NormalizeInPlace(ref rotateAxis1);
        JVector.NormalizeInPlace(ref rotateAxis2);

        TwistAngle = world.CreateConstraint<TwistAngle>(body1, body2);
        TwistAngle.Initialize(rotateAxis1, rotateAxis2);
        Register(TwistAngle);

        BallSocket = world.CreateConstraint<BallSocket>(body1, body2);
        BallSocket.Initialize(center);
        Register(BallSocket);

        if (drive == JointDrive.Motor)
        {
            Motor = world.CreateConstraint<AngularMotor>(body1, body2);
            Motor.Initialize(rotateAxis1, rotateAxis2);
            Register(Motor);
        }
        else if (drive == JointDrive.Servo)
        {
            Servo = world.CreateConstraint<AngularServo>(body1, body2);
            Servo.Initialize();
            Register(Servo);
        }
    }

    /// <inheritdoc cref="UniversalJoint(World, RigidBody, RigidBody, JVector, JVector, JVector, JointDrive)"/>
    [Obsolete("Use the overload accepting JointDrive instead of hasMotor.")]
    public UniversalJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector rotateAxis1, JVector rotateAxis2,
        bool hasMotor) :
        this(world, body1, body2, center, rotateAxis1, rotateAxis2, hasMotor ? JointDrive.Motor : JointDrive.None)
    {
    }
}
