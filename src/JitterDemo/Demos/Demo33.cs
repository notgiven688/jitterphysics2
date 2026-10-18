using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;

namespace JitterDemo;

/*
 * Constraints Showcase
 * Code generated and optimized with OpenAI Codex
 */

public class Demo33 : IDemo, IDrawUpdate, ICleanDemo
{
    public string Name => "Constraints Showcase";
    public string Description => "Examples of the built-in constraints and joints. " +
                                 "Green guides show motion and upper limits, red shows lower limits and stops, " +
                                 "and white shows current anchors and directions. " +
                                 "Orientation frames use red X, green Y, and blue Z.";
    public string Controls => "WASD and right mouse move the camera. Move closer to see labels and descriptions; " +
                              "drag bodies to explore the allowed motion.";

    private sealed record Exhibit(JVector Center, string Title, string Description, Action Draw);

    private readonly List<Exhibit> exhibits = [];
    private readonly List<(LinearMotor Motor, PointOnLine Guide)> linearDrives = [];
    private Playground playground = null!;
    private World world = null!;
    private Common.IgnoreCollisionBetweenFilter collisionFilter = null!;
    private const float Length = 2.7f;
    private const float DrawRadius = 3.2f;
    private const float LabelDistance = 20;
    private const int GridSize = 4;
    private const float GridSpacing = 16;
    private const float BaseHalfExtent = 3.75f;
    // Alphabetical constraint exhibits followed by joints, with the final three cells empty.
    private static readonly (int Row, int Column)[] ExhibitCells =
    [
        (1, 0), (0, 2),                 // Elliptical and circular swing limits.
        (0, 1), (0, 3), (2, 1),         // Ball socket, distance limit, spring.
        (1, 3), (2, 0), (1, 1),         // Line, plane, fixed angle.
        (0, 0), (1, 2),                 // Angular and linear motors.
        (2, 2), (2, 3), (3, 0)          // Hinge, universal, weld.
    ];

    private DebugRenderer Draw => playground.DebugRenderer;

    private static JVector Center(int section, int item)
    {
        int index = section switch
        {
            0 => 0,
            1 => 1,
            2 => 2 + item,
            3 => 5 + item,
            4 => 8 + item,
            _ => 10 + item
        };
        var (row, column) = ExhibitCells[index];
        float half = (GridSize - 1) * 0.5f;
        // Increasing rows move toward the initial camera, reading from top to bottom.
        return new JVector((column - half) * GridSpacing, 6,
            (row - half) * GridSpacing);
    }

    public void Build(Playground pg, World world)
    {
        CleanUp();
        playground = pg;
        this.world = world;
        pg.AddFloor();
        world.SubstepCount = 4;
        world.SolverIterations = (12, 4);
        collisionFilter = new Common.IgnoreCollisionBetweenFilter();
        world.BroadPhaseFilter = collisionFilter;

        BuildEllipticalCone();
        BuildAngularConstraints();
        BuildAnchorsAndSpring();
        BuildLinearAndOrientationConstraints();
        BuildMotors();
        BuildCompositeJoints();
        world.PreSubStep += UpdateLinearDrives;
    }

    public void DrawUpdate()
    {
        JVector cameraPosition = Conversion.ToJitterVector(playground.Camera.Position);
        foreach (Exhibit exhibit in exhibits)
        {
            if (!playground.IsConstraintDebugDrawEnabled)
                exhibit.Draw();
            if ((exhibit.Center - cameraPosition).LengthSquared() <= LabelDistance * LabelDistance)
                playground.DrawWorldLabel(exhibit.Center + JVector.UnitY * 0.7f, exhibit.Title, exhibit.Description);
            DrawBase(exhibit.Center);
        }
    }

    private void UpdateLinearDrives(TimeStep timeStep)
    {
        foreach (var (motor, guide) in linearDrives)
        {
            if (guide.Distance > 1.65f && motor.TargetVelocity > 0) motor.TargetVelocity = -0.9f;
            else if (guide.Distance < -1.65f && motor.TargetVelocity < 0) motor.TargetVelocity = 0.9f;
        }
    }

    public void CleanUp()
    {
        if (world != null) world.PreSubStep -= UpdateLinearDrives;
        exhibits.Clear();
        linearDrives.Clear();
    }

    private void AddExhibit(int section, int item, string title, string description, Action draw) =>
        exhibits.Add(new Exhibit(Center(section, item), title, description, draw));

    private RigidBody CreateBody(JVector position, RigidBodyShape shape, bool gravity = false)
    {
        RigidBody body = world.CreateRigidBody();
        body.Position = position;
        body.AddShape(shape);
        body.SetMassInertia(1);
        body.AffectedByGravity = gravity;
        body.Damping = (0.004f, 0.004f);
        return body;
    }

    private RigidBody CreateSupport(JVector position)
    {
        RigidBody support = CreateBody(position, new SphereShape(0.12f));
        support.MotionType = MotionType.Static;
        return support;
    }

    private void IgnoreCollision(RigidBody first, RigidBody second)
    {
        foreach (var a in first.Shapes)
            foreach (var b in second.Shapes)
                collisionFilter.IgnoreCollisionBetween(a, b);
    }

    private BallSocket AttachPivot(RigidBody support, RigidBody body, JVector anchor)
    {
        var socket = world.CreateConstraint<BallSocket>(support, body);
        socket.Initialize(anchor);
        socket.Softness = 0;
        IgnoreCollision(support, body);
        return socket;
    }

    private RigidBody CreatePendulum(JVector anchor, JVector direction)
    {
        var rod = new BoxShape(0.18f, Length, 0.18f);
        var weight = new SphereShape(0.35f);
        JVector weightOffset = new(0, -Length / 2, 0);
        rod.CalculateMassInertia(out _, out JVector rodCenter, out float rodMass);
        weight.CalculateMassInertia(out _, out JVector weightCenter, out float weightMass);
        JVector centerOfMass = (rodCenter * rodMass + (weightCenter + weightOffset) * weightMass) *
                              (1 / (rodMass + weightMass));
        RigidBody body = world.CreateRigidBody();
        // Every compound body is centered on its mass-weighted center of mass.
        body.AddShapes([
            new TransformedShape(rod, -centerOfMass),
            new TransformedShape(weight, weightOffset - centerOfMass)
        ]);
        body.Orientation = JQuaternion.CreateFromToRotation(-JVector.UnitY, direction);
        body.Position = anchor + direction * (Length / 2) + JVector.Transform(centerOfMass, body.Orientation);
        body.SetMassInertia(2);
        body.Damping = (0.002f, 0.002f);
        return body;
    }

    private void BuildEllipticalCone()
    {
        JVector anchor = Center(0, 0);
        JVector axis = -JVector.UnitY;
        JVector reference = JVector.UnitX;
        JVector perpendicular = axis % reference;
        JVector tilt = JVector.Normalize(reference + perpendicular * 0.6f);
        const float angle = 0.5f;
        JVector direction = MathF.Cos(angle) * axis + MathF.Sin(angle) * tilt;
        RigidBody support = CreateSupport(anchor);
        RigidBody pendulum = CreatePendulum(anchor, direction);
        pendulum.AngularVelocity = reference * 0.8f + perpendicular * 0.6f;
        AttachPivot(support, pendulum, anchor);

        var cone = world.CreateConstraint<EllipticalConeLimit>(support, pendulum);
        cone.Initialize(axis, direction, reference, AngularLimit.FromDegree(25, 65), AngularLimit.FromDegree(12, 35));
        cone.Softness = 0;
        AddExhibit(0, 0, "EllipticalConeLimit",
            "Inner: 25 / 12 degrees.\nOuter: 65 / 35 degrees.\nSwing between the two surfaces; twist is free.",
            () => cone.DebugDraw(Draw, new DebugDrawSettings { GuideSize = DrawRadius }));
    }

    private static void RotateAround(RigidBody body, JVector pivot, JQuaternion rotation)
    {
        body.Position = pivot + JVector.Transform(body.Position - pivot, rotation);
        body.Orientation = rotation * body.Orientation;
    }

    private void BuildAngularConstraints()
    {
        JVector anchor = Center(1, 0);
        RigidBody support = CreateSupport(anchor);
        JVector direction = JVector.Normalize(new JVector(0.55f, -1, 0.3f));
        RigidBody pendulum = CreatePendulum(anchor, direction);
        AttachPivot(support, pendulum, anchor);
        var cone = world.CreateConstraint<ConeLimit>(support, pendulum);
        cone.Initialize(-JVector.UnitY, direction, AngularLimit.FromDegree(20, 60));
        cone.Softness = 0;
        pendulum.AngularVelocity = new JVector(0.6f, 0.1f, 0.7f);
        AddExhibit(1, 0, "ConeLimit", "Circular swing band: 20 to 60 degrees.\nTwist remains free; a ball socket holds the pivot.", () =>
        {
            cone.DebugDraw(Draw, new DebugDrawSettings { GuideSize = DrawRadius });
        });
    }

    private void BuildAnchorsAndSpring()
    {
        JVector anchor = Center(2, 0);
        RigidBody support = CreateSupport(anchor);
        RigidBody body = CreatePendulum(anchor, JVector.Normalize(new JVector(0.55f, -1, 0.3f)));
        BallSocket socket = AttachPivot(support, body, anchor);
        body.AngularVelocity = new JVector(0.7f, 0.4f, 0.6f);
        RigidBody socketBody = body;
        AddExhibit(2, 0, "BallSocket", "Keeps two anchor points together.\nAll three rotations remain free.", () =>
        {
            socket.DebugDraw(Draw, new DebugDrawSettings { GuideSize = 2 });
            Draw.DrawFrame(socketBody.Position, socketBody.Orientation, DebugDrawSettings.Default);
        });

        anchor = Center(2, 1) - JVector.UnitY * 2.5f;
        support = CreateSupport(anchor);
        body = CreateBody(anchor + JVector.Normalize(new JVector(1, -1.6f, 0.5f)) * 2.4f, new SphereShape(0.32f), true);
        var distance = world.CreateConstraint<DistanceLimit>(support, body);
        distance.Initialize(anchor, body.Position, new LinearLimit(-0.6f, 0.6f));
        distance.Softness = 0;
        body.Velocity = new JVector(0, 0, 1);
        IgnoreCollision(support, body);
        AddExhibit(2, 1, "DistanceLimit", "Anchor separation: 1.8 to 3.0 units.\nThe body moves freely between the red and green shells.", () =>
        {
            distance.DebugDraw(Draw);
        });

        anchor = Center(2, 2);
        support = CreateSupport(anchor);
        RigidBody mass = CreateBody(anchor - JVector.UnitY * 3.2f, new SphereShape(0.38f), true);
        mass.Damping = (0.0005f, 0.0005f);
        var spring = world.CreateConstraint<SpringConstraint>(support, mass);
        spring.Initialize(anchor, mass.Position);
        spring.TargetDistance = 1.8f;
        spring.SetSpringParameters(1, 0.02f, 0.01f);
        mass.Velocity = JVector.UnitX * 0.4f;
        IgnoreCollision(support, mass);
        JVector springReference = JVector.UnitX;
        AddExhibit(2, 2, "SpringConstraint", "A damped distance spring for mass points.\nStretches and compresses around its 1.8-unit rest length.", () =>
        {
            spring.DebugDraw(Draw, ref springReference, DebugDrawSettings.Default);
        });
    }

    private void BuildLinearAndOrientationConstraints()
    {
        JVector center = Center(3, 0) - JVector.UnitY * 1.5f;
        RigidBody support = CreateSupport(center);
        RigidBody sliderBody = CreateBody(center, new BoxShape(0.7f), true);
        var line = world.CreateConstraint<PointOnLine>(support, sliderBody);
        line.Initialize(JVector.UnitX, center, center, new LinearLimit(-2, 2));
        line.Softness = line.LimitSoftness = 0;
        sliderBody.Velocity = JVector.UnitX;
        sliderBody.AngularVelocity = new JVector(0.3f, 0.6f, 0.4f);
        IgnoreCollision(support, sliderBody);
        AddExhibit(3, 0, "PointOnLine", "An anchor slides along a bounded line.\nThe body can still rotate around all three axes.", () => line.DebugDraw(Draw, new DebugDrawSettings { GuideSize = DrawRadius }));

        center = Center(3, 1) - JVector.UnitY * 1.5f;
        support = CreateSupport(center);
        RigidBody planeBody = CreateBody(center, new BoxShape(0.7f));
        var plane = world.CreateConstraint<PointOnPlane>(support, planeBody);
        plane.Initialize(JVector.UnitZ, center, center);
        plane.Softness = 0;
        // Leave room for the rotating cube within the drawn plane.
        const float planeRadius = 1.4f;
        var planeDistance = world.CreateConstraint<DistanceLimit>(support, planeBody);
        planeDistance.Initialize(center, center, new LinearLimit(0, planeRadius));
        planeDistance.Softness = 0;
        planeBody.Velocity = new JVector(0.35f, 0.2f, 0);
        planeBody.AngularVelocity = new JVector(0.3f, 0.5f, 0.2f);
        IgnoreCollision(support, planeBody);
        JVector planeCenter = center;
        AddExhibit(3, 1, "PointOnPlane", "An anchor stays on the green plane.\nA distance limit keeps it inside the red circle.\nAll rotations remain free.", () =>
        {
            plane.DebugDraw(Draw, new DebugDrawSettings { GuideSize = 2.1f });
            Draw.DrawCircle(planeCenter, JVector.UnitZ, JVector.UnitX, planeRadius, DebugDrawColors.Red);
        });

        center = Center(3, 2) - JVector.UnitY * 1.5f;
        RigidBody fixedBody = CreateBody(center, new BoxShape(1.4f, 0.6f, 1));
        fixedBody.Orientation = JQuaternion.CreateRotationX(0.2f) * JQuaternion.CreateRotationZ(0.35f);
        var fixedAngle = world.CreateConstraint<FixedAngle>(world.NullBody, fixedBody);
        fixedAngle.Initialize();
        fixedAngle.Softness = 0;
        const float fixedRadius = 2;
        var fixedDistance = world.CreateConstraint<DistanceLimit>(world.NullBody, fixedBody);
        fixedDistance.Initialize(center, center, new LinearLimit(0, fixedRadius));
        fixedDistance.Softness = 0;
        fixedBody.Velocity = new JVector(0.2f, 0.1f, 0);
        AddExhibit(3, 2, "FixedAngle", "Locks relative orientation.\nA distance limit keeps the box's center inside the green sphere.\nThe RGB frame follows the box.", () =>
        {
            fixedDistance.DebugDraw(Draw);
            fixedAngle.DebugDraw(Draw);
        });
    }

    private void BuildMotors()
    {
        JVector center = Center(4, 0) - JVector.UnitY * 1.5f;
        RigidBody support = CreateSupport(center);
        RigidBody rotor = CreateBody(center, new BoxShape(2.3f, 0.3f, 0.55f));
        AttachPivot(support, rotor, center);
        var rotationGuide = world.CreateConstraint<HingeAngle>(support, rotor);
        rotationGuide.Initialize(JVector.UnitZ, AngularLimit.Full);
        rotationGuide.Softness = 0;
        var angularMotor = world.CreateConstraint<AngularMotor>(support, rotor);
        angularMotor.Initialize(JVector.UnitZ);
        angularMotor.TargetVelocity = 2;
        angularMotor.MaximumTorque = 4;
        AddExhibit(4, 0, "AngularMotor", "Drives 2 rad/s with a torque limit of 4.\nThe white arrow around the green ring shows the target rotation.\nBallSocket + HingeAngle hold the rotor on its axis.", () =>
        {
            angularMotor.DebugDraw(Draw, new DebugDrawSettings { GuideSize = 1.7f });
        });

        center = Center(4, 1) - JVector.UnitY * 1.5f;
        support = CreateSupport(center);
        RigidBody carriage = CreateBody(center, new BoxShape(1, 0.65f, 0.65f));
        var guide = world.CreateConstraint<PointOnLine>(support, carriage);
        guide.Initialize(JVector.UnitX, center, center, new LinearLimit(-2, 2));
        guide.Softness = guide.LimitSoftness = 0;
        var orientation = world.CreateConstraint<FixedAngle>(support, carriage);
        orientation.Initialize();
        orientation.Softness = 0;
        var linearMotor = world.CreateConstraint<LinearMotor>(support, carriage);
        linearMotor.Initialize(JVector.UnitX, JVector.UnitX);
        linearMotor.TargetVelocity = 0.9f;
        linearMotor.MaximumForce = 6;
        linearDrives.Add((linearMotor, guide));
        IgnoreCollision(support, carriage);
        AddExhibit(4, 1, "LinearMotor", "Drives +/-0.9 units/s with a force limit of 6.\nReverses near each stop; line and orientation guides form the rail.", () =>
        {
            guide.DebugDraw(Draw, new DebugDrawSettings { GuideSize = DrawRadius });
            linearMotor.DebugDraw(Draw, new DebugDrawSettings { GuideSize = 1.4f });
        });
    }

    private void BuildCompositeJoints()
    {
        JVector anchor = Center(5, 0);
        RigidBody support = CreateSupport(anchor);
        RigidBody hingeBody = CreatePendulum(anchor, -JVector.UnitY);
        var hinge = new HingeJoint(world, support, hingeBody, anchor, JVector.UnitZ, AngularLimit.FromDegree(-65, 65));
        hinge.HingeAngle.Softness = hinge.HingeAngle.LimitSoftness = 0;
        hinge.BallSocket.Softness = 0;
        IgnoreCollision(support, hingeBody);
        RotateAround(hingeBody, anchor, JQuaternion.CreateRotationZ(0.65f));
        AddExhibit(5, 0, "HingeJoint",
            "BallSocket + HingeAngle.\nOne rotational DOF, limited to +/-65 degrees.",
            () => hinge.DebugDraw(Draw, new DebugDrawSettings { GuideSize = DrawRadius }));

        anchor = Center(5, 1);
        JVector inputCenter = anchor - JVector.UnitX * 1.1f;
        support = CreateSupport(inputCenter);
        RigidBody inputShaft = CreateBody(inputCenter, new BoxShape(2, 0.16f, 0.5f));
        JQuaternion outputTilt = JQuaternion.CreateRotationZ(-0.45f);
        JVector outputAxis = JVector.Transform(JVector.UnitX, outputTilt);
        RigidBody outputShaft = CreateBody(anchor + outputAxis * 1.1f, new BoxShape(2, 0.16f, 0.5f), true);
        outputShaft.Orientation = outputTilt;
        var drive = new HingeJoint(world, support, inputShaft, inputCenter, JVector.UnitX, AngularLimit.Full, true);
        drive.HingeAngle.Softness = drive.BallSocket.Softness = 0;
        drive.Motor!.TargetVelocity = 2;
        drive.Motor.MaximumTorque = 4;
        var universal = new UniversalJoint(world, inputShaft, outputShaft, anchor, JVector.UnitX, outputAxis);
        universal.TwistAngle.Softness = universal.BallSocket.Softness = 0;
        IgnoreCollision(support, inputShaft);
        IgnoreCollision(support, outputShaft);
        IgnoreCollision(inputShaft, outputShaft);
        AddExhibit(5, 1, "UniversalJoint",
            "A motor turns the input shaft at 2 rad/s.\nThe universal joint couples the output rotation while allowing bending.\nDrag the free shaft to change the angle.",
            () =>
            {
                drive.Motor.DebugDraw(Draw, new DebugDrawSettings { GuideSize = 0.5f });
                universal.DebugDraw(Draw, new DebugDrawSettings { GuideSize = 0.65f });
            });

        anchor = Center(5, 2);
        support = CreateSupport(anchor);
        RigidBody first = CreateBody(anchor + new JVector(-0.55f, -1.4f, 0), new BoxShape(0.75f), true);
        RigidBody second = CreateBody(first.Position + new JVector(1.35f, -0.25f, 0), new BoxShape(0.9f, 0.55f, 0.75f), true);
        BallSocket suspension = AttachPivot(support, first, anchor);
        var weld = new WeldJoint(world, first, second, (first.Position + second.Position) * 0.5f);
        weld.FixedAngle.Softness = weld.BallSocket.Softness = 0;
        IgnoreCollision(first, second);
        IgnoreCollision(support, second);
        first.AngularVelocity = second.AngularVelocity = new JVector(0.3f, 0.2f, 0.4f);
        AddExhibit(5, 2, "WeldJoint",
            "BallSocket + FixedAngle.\nBoth frames keep their relative pose.",
            () =>
            {
                suspension.DebugDraw(Draw, new DebugDrawSettings { GuideSize = 0.55f });
                weld.DebugDraw(Draw, new DebugDrawSettings { GuideSize = 0.55f });
            });
    }

    private void DrawBase(JVector center)
    {
        JVector origin = new(center.X, 0.035f, center.Z);
        JVector a = origin + new JVector(-BaseHalfExtent, 0, -BaseHalfExtent);
        JVector b = origin + new JVector(BaseHalfExtent, 0, -BaseHalfExtent);
        JVector c = origin + new JVector(BaseHalfExtent, 0, BaseHalfExtent);
        JVector d = origin + new JVector(-BaseHalfExtent, 0, BaseHalfExtent);
        Draw.DrawSegment(a, b, DebugDrawColors.White);
        Draw.DrawSegment(b, c, DebugDrawColors.White);
        Draw.DrawSegment(c, d, DebugDrawColors.White);
        Draw.DrawSegment(d, a, DebugDrawColors.White);
    }
}
