/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using Jitter2.LinearMath;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// Holds two bodies together at a shared anchor and preserves their relative orientation.
/// </summary>
public class WeldJoint : Joint
{
    public RigidBody Body1 { get; private set; }
    public RigidBody Body2 { get; private set; }

    public FixedAngle FixedAngle { get; }
    public BallSocket BallSocket { get; }

    /// <summary>
    /// Initializes a new weld joint.
    /// </summary>
    /// <param name="center">The shared anchor point in world space.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="center"/> contains a non-finite value, when either body does not belong
    /// to <paramref name="world"/>, or when both body references are the same.
    /// </exception>
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
