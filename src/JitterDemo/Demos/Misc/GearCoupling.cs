using System;
using Jitter2;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;

namespace JitterDemo;

/*
 --- 1. The Basic Idea ---
 We simulate the gear connection using a standard DistanceLimit constraint.
 This constraint acts like a "Virtual Tooth" that pins the two bodies together
 at the contact point.

 Crucially, we hook into the physics loop (OnPreStep) to update this constraint
 EVERY FRAME. Since the gears are rotating, the specific points on the surface
 that are touching change constantly. By resetting the DistanceLimit anchors to the
 current world contact point every frame, we allow the gears to rotate freely
 while maintaining the velocity relationship at the contact.

 --- 2. The Refinement: Error Correction ---
 If we strictly placed the anchors at the current contact point, the gears would
 eventually drift out of sync ("slip"). This happens because the physics solver
 has limited iterations and cannot solve the velocity constraints perfectly
 every frame, leading to accumulated position error.

 To fix this, this demo implements an active Error Correction system:
 - We track the EXACT total rotation of both gears (counting full 360 loops).
 - We calculate the "Angular Error" (how far they have drifted from the ideal ratio).
 - We shift the BallSocket anchors slightly along the tangent based on this error.

 This forces the physics solver to push the gears back into perfect phase,
 correcting any drift.
 */

public class GearCoupling
{
    // DistanceLimit seems to lead to more stable systems.
    // BallSocketConstraint over-constraints the system (Hinge, Hinge, BallSocket).
    public DistanceLimit DistanceLimit { get; private set; }

    // Using simple HingeJoints to pin the gears to the world (NullBody)
    public HingeJoint HingeJoint1 { get; private set; }
    public HingeJoint HingeJoint2 { get; private set; }

    public RigidBody Body1 { get; }
    public RigidBody Body2 { get; }

    private readonly JQuaternion initialOrientation1;
    private readonly JQuaternion initialOrientation2;

    public JVector ContactPoint { get; }

    private readonly JVector localAxis1;
    private readonly JVector localAxis2;

    private readonly World world;

    private float prevAngle1, prevAngle2;

    public float GearRatio { get; private set; }

    // Accumulators for full rotations
    public int Rotations1 { get; private set; }
    public int Rotations2 { get; private set; }

    public GearCoupling(World world, RigidBody body1, RigidBody body2,
        JVector rotationAxis1, JVector rotationAxis2, JVector contactPoint)
    {
        JVector.NormalizeInPlace(ref rotationAxis1);
        JVector.NormalizeInPlace(ref rotationAxis2);

        this.Body1 = body1;
        this.Body2 = body2;
        this.world = world;
        this.ContactPoint = contactPoint;

        // 1. Store Local Axes
        // We must store the axis relative to EACH body's initial orientation
        localAxis1 = JVector.ConjugatedTransform(rotationAxis1, body1.Orientation);
        localAxis2 = JVector.ConjugatedTransform(rotationAxis2, body2.Orientation);

        // 2. Setup World Hinges (Pins the gears in place)
        HingeJoint1 = new HingeJoint(world, world.NullBody, body1, body1.Position, rotationAxis1, AngularLimit.Full);
        HingeJoint2 = new HingeJoint(world, world.NullBody, body2, body2.Position, rotationAxis2, AngularLimit.Full);

        // Make hinges stiff so gears don't wobble
        HingeJoint1.HingeAngle.Bias = 0.3f;
        HingeJoint2.HingeAngle.Bias = 0.3f;
        HingeJoint1.HingeAngle.Softness = 0.0f;
        HingeJoint2.HingeAngle.Softness = 0.0f;

        // 3. Setup the Virtual Tooth
        DistanceLimit = world.CreateConstraint<DistanceLimit>(body1, body2);
        DistanceLimit.Initialize(contactPoint, contactPoint, LinearLimit.Fixed);

        DistanceLimit.Bias = 0.1f;
        DistanceLimit.Softness = 0.0000f;

        initialOrientation1 = body1.Orientation;
        initialOrientation2 = body2.Orientation;

        // Calculate Ratio based on radius to contact point
        // Ratio = r1 / r2
        float r1 = (body1.Position - contactPoint).Length();
        float r2 = (body2.Position - contactPoint).Length();
        this.GearRatio = r1 / r2;

        // 4. Initialize Angles immediately to prevent "spin on spawn"
        // (Pass the correct local axis for each!)
        this.prevAngle1 = GetTwistAngle(body1, initialOrientation1, localAxis1);
        this.prevAngle2 = GetTwistAngle(body2, initialOrientation2, localAxis2);

        world.PreSubStep += OnPreStep;
    }

    /// <summary>
    /// Calculates the signed twist angle relative to the initial orientation.
    /// </summary>
    private static float GetTwistAngle(RigidBody body, JQuaternion initialOrientation, JVector localAxis)
    {
        JQuaternion q = JQuaternion.MultiplyConjugate(body.Orientation, initialOrientation);
        JVector ax = JVector.Transform(localAxis, body.Orientation);

        float y = JVector.Dot(q.Vector, ax);
        float x = q.Scalar;

        // Double-cover check
        if (x < 0)
        {
            x = -x;
            y = -y;
        }

        return 2.0f * MathF.Atan2(y, x);
    }

    public float GearAngle1 => GetTwistAngle(Body1, initialOrientation1, localAxis1);
    public float GearAngle2 => GetTwistAngle(Body2, initialOrientation2, localAxis2);

    public float TrackDeltaAngle()
    {
        float angle1 = GearAngle1;
        float angle2 = GearAngle2;

        float deltaAngle1 = angle1 - prevAngle1;
        float deltaAngle2 = angle2 - prevAngle2;

        // Phase Unwrapping
        if (MathF.Abs(deltaAngle1) > MathF.PI)
            Rotations1 -= MathF.Sign(deltaAngle1);

        if (MathF.Abs(deltaAngle2) > MathF.PI)
            Rotations2 -= MathF.Sign(deltaAngle2);

        prevAngle1 = angle1;
        prevAngle2 = angle2;

        float totRot1 = Rotations1 * MathF.PI * 2.0f + angle1;
        float totRot2 = Rotations2 * MathF.PI * 2.0f + angle2;

        // Constraint: Theta2 - Ratio * Theta1 = 0
        // We return the error (delta)
        return totRot2 + GearRatio * totRot1; // Note: Usually '+' if gears rotate opposite directions!
    }

    private void OnPreStep(float dt)
    {
        // 1. Calculate the Error
        float error = TrackDeltaAngle();

        // Optional: Clamp error to prevent explosions if physics glitched hard
        error = Math.Clamp(error, -0.1f, 0.1f);

        // 2. Calculate the Tangent Direction
        // The tangent is the direction the teeth move at the contact point.
        // We approximate this using the Cross Product of the axle and the radius.

        JVector ax1 = JVector.Transform(localAxis1, Body1.Orientation);
        JVector r = ContactPoint - Body1.Position;

        // Tangent direction at the contact point
        JVector tangent = JVector.Normalize(JVector.Cross(ax1, r));

        // 3. Move the Anchor Points ("Virtual Tooth")
        // We shift the anchor points along the tangent to compensate for the angular error.
        // The factor '0.5' splits the correction between both bodies.
        // 'r2' (radius) acts as the lever arm converter (Angle -> Arc Length).
        float radius2 = (Body2.Position - ContactPoint).Length();
        JVector offset = tangent * (error * radius2 * 0.5f);

        DistanceLimit.Anchor1 = ContactPoint + offset;
        DistanceLimit.Anchor2 = ContactPoint - offset;
    }

    public void Remove()
    {
        world.PreSubStep -= OnPreStep;
    }
}