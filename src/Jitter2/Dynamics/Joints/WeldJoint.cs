/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using Jitter2.LinearMath;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// Creates a rigid weld joint between two bodies using a <see cref="Constraints.FixedAngle"/> 
/// constraint for orientation locking and a <see cref="BallSocket"/> constraint
/// for positional locking. This effectively removes all relative motion between
/// the connected bodies.
/// </summary>
public class WeldJoint : Joint
{
    public RigidBody Body1 { get; private set; }
    public RigidBody Body2 { get; private set; }

    public FixedAngle FixedAngle { get; }
    public BallSocket BallSocket { get; }

    public WeldJoint(World world, RigidBody body1, RigidBody body2, JVector center)
    {
        Body1 = body1;
        Body2 = body2;
        
        FixedAngle = world.CreateConstraint<FixedAngle>(body1, body2);
        FixedAngle.Initialize();
        Register(FixedAngle);

        BallSocket = world.CreateConstraint<BallSocket>(body1, body2);
        BallSocket.Initialize(center);
        Register(BallSocket);
    }
}