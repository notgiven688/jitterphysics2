/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// Creates a universal joint utilizing a <see cref="TwistAngle"/>, <see cref="BallSocket"/>, and an optional <see cref="AngularMotor"/>
/// constraint.
/// </summary>
public class UniversalJoint : Joint
{
    public RigidBody Body1 { get; private set; }
    public RigidBody Body2 { get; private set; }

    public TwistAngle TwistAngle { get; }
    public BallSocket BallSocket { get; }
    public AngularMotor? Motor { get; }

    public UniversalJoint(World world, RigidBody body1, RigidBody body2, JVector center, JVector rotateAxis1, JVector rotateAxis2, bool hasMotor = false)
    {
        Body1 = body1;
        Body2 = body2;

        JVector.NormalizeInPlace(ref rotateAxis1);
        JVector.NormalizeInPlace(ref rotateAxis2);

        TwistAngle = world.CreateConstraint<TwistAngle>(body1, body2);
        TwistAngle.Initialize(rotateAxis1, rotateAxis2);
        Register(TwistAngle);

        BallSocket = world.CreateConstraint<BallSocket>(body1, body2);
        BallSocket.Initialize(center);
        Register(TwistAngle);

        if (hasMotor)
        {
            Motor = world.CreateConstraint<AngularMotor>(body1, body2);
            Motor.Initialize(rotateAxis1, rotateAxis2);
            Register(Motor);
        }
    }
}