using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer.OpenGL;
using Vellum;

namespace JitterDemo;

/*
 * Robot Arm
 * Code generated and optimized with OpenAI Codex
 */

public class Demo34 : IDemo, IDemoGui
{
    public string Name => "Robot Arm";
    public string Description => "A robot arm driven by hinge, universal, and prismatic servos, with adjustable joint targets.";
    public string Controls => "Use the sliders to pose the arm and open or close the gripper.";

    private const float Scale = 2f;
    // With fixed masses, inertia and drive torque scale with length squared.
    private const float TorqueScale = Scale * Scale;
    private const float ShoulderHeight = 1f * Scale;
    private const float UpperLength = 2.1f * Scale;
    private const float LowerLength = 1.9f * Scale;
    private const float WristLength = 0.3f * Scale;
    private const float PalmLength = 0.25f * Scale;
    private const float FingerLength = 0.65f * Scale;
    private const float FingerWidth = 0.12f * Scale;
    private const float MaximumOpening = 0.9f * Scale;

    private static readonly Vector3 Orange = new(0.95f, 0.48f, 0.12f);
    private static readonly Vector3 Dark = new(0.23f, 0.27f, 0.32f);
    private static readonly Vector3 Metal = new(0.65f, 0.71f, 0.76f);

    private RigidBody turntable = null!, upper = null!, lower = null!, wrist = null!, palm = null!;
    private readonly RigidBody[] fingers = new RigidBody[2];
    private HingeJoint baseJoint = null!, shoulderJoint = null!, elbowJoint = null!, rollJoint = null!;
    private UniversalJoint wristJoint = null!;
    private readonly PrismaticJoint[] fingerJoints = new PrismaticJoint[2];

    private float azimuth, shoulder, elbow, wristPitch, wristTilt, roll, opening;
    private readonly WindowState controlsWindow = new();

    public void Build(Playground pg, World world)
    {
        controlsWindow.Position = new System.Numerics.Vector2(Math.Max(16, pg.Width - 316), 16);
        pg.AddFloor();
        world.SubstepCount = 4;
        // The stiff arm needs enough solver iterations for its joints and drives to settle together.
        world.SolverIterations = (24, 4);
        var filter = new Common.IgnoreCollisionBetweenFilter();
        world.BroadPhaseFilter = filter;

        void IgnoreCollision(RigidBody first, RigidBody second)
        {
            foreach (var a in first.Shapes)
            foreach (var b in second.Shapes)
                filter.IgnoreCollisionBetween(a, b);
        }

        RigidBody Part(RigidBodyShape shape, JVector position, float mass, Vector3 color)
        {
            var body = world.CreateRigidBody();
            body.AddShape(shape);
            body.Position = position;
            body.SetMassInertia(mass);
            body.Friction = 0.9f;
            body.Tag = new RigidBodyTag(false) { Color = color };
            return body;
        }

        HingeJoint Hinge(RigidBody first, RigidBody second, JVector center, JVector axis,
            float minAngle, float maxAngle, float torque)
        {
            var joint = new HingeJoint(world, first, second, center, axis,
                AngularLimit.FromDegree(minAngle, maxAngle), JointDrive.Servo);
            // Keep the joint anchors and locked axes rigid.
            joint.BallSocket.Softness = 0;
            joint.HingeAngle.Softness = 0;
            joint.HingeAngle.LimitSoftness = 0;
            joint.Servo!.MaximumTorque = torque * TorqueScale;
            joint.Servo.Bias = 0.08f;
            joint.Servo.Softness = 0.0001f / TorqueScale;
            IgnoreCollision(first, second);
            return joint;
        }

        var pedestal = Part(new BoxShape(1.2f * Scale, 0.5f * Scale, 1.2f * Scale), new JVector(0, 0.25f * Scale, 0), 1, Dark);
        pedestal.MotionType = MotionType.Static;
        turntable = Part(new CylinderShape(0.3f * Scale, 0.48f * Scale), new JVector(0, 0.7f * Scale, 0), 4, Dark);
        upper = Part(new BoxShape(0.4f * Scale, UpperLength, 0.4f * Scale), new JVector(0, ShoulderHeight + UpperLength / 2, 0), 3, Orange);
        lower = Part(new BoxShape(0.32f * Scale, LowerLength, 0.32f * Scale), new JVector(0, ShoulderHeight + UpperLength + LowerLength / 2, 0), 2, Orange);
        var pinRotation = JMatrix.CreateRotationX(MathF.PI / 2);
        upper.AddShape(new TransformedShape(new CylinderShape(0.6f * Scale, 0.28f * Scale),
            new JVector(0, -UpperLength / 2, 0), pinRotation));
        upper.SetMassInertia(3);
        lower.AddShape(new TransformedShape(new CylinderShape(0.5f * Scale, 0.24f * Scale),
            new JVector(0, -LowerLength / 2, 0), pinRotation));
        lower.SetMassInertia(2);
        float wristHeight = ShoulderHeight + UpperLength + LowerLength;
        wrist = Part(new BoxShape(0.42f * Scale, WristLength, 0.42f * Scale), new JVector(0, wristHeight + WristLength / 2, 0), 0.6f, Dark);
        palm = Part(new BoxShape(0.38f * Scale, PalmLength, 1.1f * Scale), new JVector(0, wristHeight + WristLength + PalmLength / 2, 0), 0.6f, Orange);

        baseJoint = Hinge(pedestal, turntable, turntable.Position, JVector.UnitY, -170, 170, 150);
        shoulderJoint = Hinge(turntable, upper, new JVector(0, ShoulderHeight, 0), JVector.UnitZ, -75, 75, 250);
        elbowJoint = Hinge(upper, lower, new JVector(0, ShoulderHeight + UpperLength, 0), JVector.UnitZ, -145, 0, 100);
        foreach (var servo in new[] { baseJoint.Servo!, shoulderJoint.Servo!, elbowJoint.Servo! })
        {
            servo.Bias = 0.04f;
            servo.Softness = 0.00001f / TorqueScale;
        }
        wristJoint = new UniversalJoint(world, lower, wrist, new JVector(0, wristHeight, 0),
            JVector.UnitY, JVector.UnitY, JointDrive.Servo);
        wristJoint.BallSocket.Softness = 0;
        wristJoint.TwistAngle.Softness = 0;
        wristJoint.Servo!.MaximumTorque = 40 * TorqueScale;
        wristJoint.Servo.Bias = 0.08f;
        wristJoint.Servo.Softness = 0.0001f / TorqueScale;
        IgnoreCollision(lower, wrist);
        rollJoint = Hinge(wrist, palm, new JVector(0, wristHeight + WristLength, 0), JVector.UnitY, -170, 170, 15);

        for (int i = 0; i < fingers.Length; i++)
        {
            float side = i == 0 ? -1 : 1;
            var position = new JVector(0, wristHeight + WristLength + PalmLength + FingerLength / 2,
                side * (MaximumOpening + FingerWidth) / 2);
            fingers[i] = Part(new BoxShape(0.28f * Scale, FingerLength, FingerWidth), position, 0.3f, Metal);
            fingerJoints[i] = new PrismaticJoint(world, palm, fingers[i], position, JVector.UnitZ,
                new LinearLimit(i == 0 ? 0 : -MaximumOpening / 2, i == 0 ? MaximumOpening / 2 : 0), JointDrive.Servo);
            fingerJoints[i].Slider.Softness = 0;
            fingerJoints[i].Slider.LimitSoftness = 0;
            fingerJoints[i].FixedAngle!.Softness = 0;
            fingerJoints[i].Servo!.MaximumForce = 30 * Scale;
            IgnoreCollision(palm, fingers[i]);
        }

        var table = world.CreateRigidBody();
        table.AddShape(new BoxShape(2.2f * Scale, 0.2f * Scale, 2.4f * Scale));
        table.Position = new JVector(3.1f, 1, 0) * Scale;
        table.MotionType = MotionType.Static;
        table.Tag = new RigidBodyTag(false) { Color = Dark };
        // Legs are part of the same static body.
        foreach (float x in new[] { -0.9f, 0.9f })
        foreach (float z in new[] { -1f, 1f })
            table.AddShape(new TransformedShape(new BoxShape(0.12f * Scale, 0.9f * Scale, 0.12f * Scale),
                new JVector(x, -0.55f, z) * Scale));

        foreach (float z in new[] { 0f, -0.8f, 0.8f })
        {
            var block = world.CreateRigidBody();
            block.AddShape(new BoxShape(0.4f * Scale, 0.5f * Scale, 0.4f * Scale));
            block.Position = new JVector(3.08f, 1.35f, z) * Scale;
            block.SetMassInertia(0.3f);
            block.Friction = 0.9f;
            block.Tag = new RigidBodyTag(false) { Color = new Vector3(0.2f, 0.65f, 0.85f) };
        }

        azimuth = wristTilt = roll = 0;
        shoulder = -10;
        elbow = -75;
        wristPitch = -95;
        opening = MaximumOpening;
        ApplyTargets();
        PlaceInitialPose();
    }

    public void DrawGui(Ui root)
    {
        root.Window(MaterialSymbols.PrecisionManufacturing + " Robot Arm", controlsWindow, 300f, controls =>
        {
            controls.ItemSpacing(8f);
            controls.Spacing(4f);
            AngleSlider(controls, "Base", ref azimuth, -170, 170, baseJoint.Servo!);
            AngleSlider(controls, "Shoulder", ref shoulder, -75, 75, shoulderJoint.Servo!);
            AngleSlider(controls, "Elbow", ref elbow, -145, 0, elbowJoint.Servo!);

            controls.Spacing(4f);
            bool wristChanged = controls.Slider("Wrist pitch (deg)", ref wristPitch, -110, 110, controls.AvailableWidth).Changed;
            wristChanged |= controls.Slider("Wrist tilt (deg)", ref wristTilt, -45, 45, controls.AvailableWidth).Changed;
            if (wristChanged) wristJoint.Servo!.TargetRelativeOrientation = WristTarget();
            AngleSlider(controls, "Wrist roll", ref roll, -170, 170, rollJoint.Servo!);

            controls.Spacing(4f);
            if (controls.Slider("Gripper opening", ref opening, 0, MaximumOpening, controls.AvailableWidth).Changed)
                ApplyGripperTarget();
            controls.Spacing(4f);
        }, resizable: false, closable: false, id: "robot-arm-controls");
    }

    private static void AngleSlider(Ui ui, string label, ref float value, float min, float max, TwistServo servo)
    {
        if (ui.Slider(label + " (deg)", ref value, min, max, ui.AvailableWidth).Changed)
            servo.TargetAngle = JAngle.FromDegree(value);
    }

    private void ApplyTargets()
    {
        baseJoint.Servo!.TargetAngle = JAngle.FromDegree(azimuth);
        shoulderJoint.Servo!.TargetAngle = JAngle.FromDegree(shoulder);
        elbowJoint.Servo!.TargetAngle = JAngle.FromDegree(elbow);
        wristJoint.Servo!.TargetRelativeOrientation = WristTarget();
        rollJoint.Servo!.TargetAngle = JAngle.FromDegree(roll);
        ApplyGripperTarget();
    }

    private void ApplyGripperTarget()
    {
        fingerJoints[0].Servo!.TargetPosition = (MaximumOpening - opening) / 2;
        fingerJoints[1].Servo!.TargetPosition = (opening - MaximumOpening) / 2;
    }

    private JQuaternion WristTarget()
    {
        // A swing around an axis in the XZ plane leaves the universal joint's Y twist at zero.
        var rotation = new JVector(wristTilt, 0, wristPitch);
        float angle = rotation.Length();
        return angle > 0 ? JQuaternion.CreateFromAxisAngle(rotation * (1 / angle), JAngle.FromDegree(angle).Radian) : JQuaternion.Identity;
    }

    private void PlaceInitialPose()
    {
        // Pose once at construction. During simulation only the servo targets are changed.
        turntable.Orientation = JQuaternion.CreateRotationY(JAngle.FromDegree(azimuth).Radian);
        JVector shoulderPosition = new(0, ShoulderHeight, 0);
        upper.Orientation = turntable.Orientation * JQuaternion.CreateRotationZ(JAngle.FromDegree(shoulder).Radian);
        upper.Position = shoulderPosition + JVector.Transform(JVector.UnitY * (UpperLength / 2), upper.Orientation);
        JVector elbowPosition = shoulderPosition + JVector.Transform(JVector.UnitY * UpperLength, upper.Orientation);
        lower.Orientation = upper.Orientation * JQuaternion.CreateRotationZ(JAngle.FromDegree(elbow).Radian);
        lower.Position = elbowPosition + JVector.Transform(JVector.UnitY * (LowerLength / 2), lower.Orientation);
        JVector wristPosition = elbowPosition + JVector.Transform(JVector.UnitY * LowerLength, lower.Orientation);
        wrist.Orientation = lower.Orientation * WristTarget();
        wrist.Position = wristPosition + JVector.Transform(JVector.UnitY * (WristLength / 2), wrist.Orientation);
        JVector rollPosition = wristPosition + JVector.Transform(JVector.UnitY * WristLength, wrist.Orientation);
        palm.Orientation = wrist.Orientation * JQuaternion.CreateRotationY(JAngle.FromDegree(roll).Radian);
        palm.Position = rollPosition + JVector.Transform(JVector.UnitY * (PalmLength / 2), palm.Orientation);
        for (int i = 0; i < fingers.Length; i++)
        {
            float side = i == 0 ? -1 : 1;
            fingers[i].Orientation = palm.Orientation;
            fingers[i].Position = rollPosition + JVector.Transform(new JVector(0, PalmLength + FingerLength / 2,
                side * (opening + FingerWidth) / 2), palm.Orientation);
        }
    }
}
