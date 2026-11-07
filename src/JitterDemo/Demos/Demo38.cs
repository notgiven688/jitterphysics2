using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer.OpenGL;
using Vellum;

namespace JitterDemo;

public class Demo38 : IDemo
{
    public string Name => "Soft Body Rope Twister";

    public string Description => "Four soft-body ropes with self-collision hang between two cubes. " +
                                 "Motors spin the cubes in opposite directions and twist the ropes into a braid.";

    private const float Radius = 0.05f;

    // Number of segments per rope diameter. Higher values give smoother ropes and contacts
    // that land closer to where they happen, at the cost of more bodies and constraints.
    // Long chains also get stretchier in the iterative solver, so thin ropes use fewer.
    private const int SegmentsPerDiameter = 2;
    private const float SegmentLength = 2.0f * Radius / SegmentsPerDiameter;

    // Mass per metre of rope; kept constant when radius or density change.
    private const float LinearDensity = 0.25f;
    private const float InverseMass = 1.0f / (LinearDensity * SegmentLength);

    private const float CubeSize = 2.0f;
    private const float HalfSpan = 6.0f;
    private const float Height = 5.0f;
    private const int Strands = 4;
    private const float StrandCircle = 0.6f;
    private const float Sag = 1.5f;

    private static readonly Vector3[] StrandColors =
    [
        new(0.85f, 0.25f, 0.25f),
        new(0.25f, 0.75f, 0.35f),
        new(0.95f, 0.95f, 0.95f),
        new(0.25f, 0.45f, 0.9f)
    ];

    private readonly List<(SoftBodyRope Rope, Vector3 Color)> ropes = new();
    private Playground pg = null!;
    private RopeCollisionFilter? ropeFilter;

    private readonly WindowState controlsWindow = new();
    private HingeJoint leftHinge = null!;
    private HingeJoint rightHinge = null!;
    private float twistSpeed = 4.0f;
    private float twistTorque = 40.0f;

    public void Build(Playground pg)
    {
        this.pg = pg;
        ropes.Clear();

        World world = pg.World;
        pg.AddFloor();

        world.DynamicTree.Filter = SoftBodyRope.SelfCollisionFilter;
        ropeFilter = new RopeCollisionFilter(world);
        world.BroadPhaseFilter = ropeFilter;

        world.SubstepCount = 24;
        world.SolverIterations = (1, 1);

        BuildTwister(world);

        controlsWindow.Position = new System.Numerics.Vector2(Math.Max(16, pg.Width - 316), 16);
    }

    /// <summary>
    /// Four ropes hang between two cubes, each driven by a hinge motor about the horizontal axis.
    /// The cubes spin in opposite directions. The motors' torque limit lets them stall once the
    /// strands are wound tight, instead of stretching the ropes until they tear through each other.
    /// </summary>
    private void BuildTwister(World world)
    {
        JVector leftCenter = new(-HalfSpan, Height, 0);
        JVector rightCenter = new(HalfSpan, Height, 0);

        RigidBody leftCube = CreateDrivenCube(world, leftCenter, out leftHinge);
        RigidBody rightCube = CreateDrivenCube(world, rightCenter, out rightHinge);
        ApplyTwisterMotor();

        // Attach just off the inner faces, so the first segment does not start inside the cubes.
        float attachOffset = 0.5f * CubeSize + 1.5f * Radius;

        for (int s = 0; s < Strands; s++)
        {
            float angle = (s + 0.5f) * 2.0f * MathF.PI / Strands;
            JVector radial = new(0, MathF.Cos(angle), MathF.Sin(angle));

            JVector from = leftCenter + StrandCircle * radial + JVector.UnitX * attachOffset;
            JVector to = rightCenter + StrandCircle * radial - JVector.UnitX * attachOffset;

            // Let the strands sag, which gives them some slack to wind up.
            var points = ResamplePath(t => from + t * (to - from) - JVector.UnitY * (Sag * MathF.Sin(MathF.PI * t)),
                SegmentLength);

            var rope = new SoftBodyRope(world, points, Radius, InverseMass);

            // Always create contacts within a small margin, so strands that move together and then
            // stop abruptly (a rope snapping taut) still have contacts in place.
            foreach (var shape in rope.Shapes) ((SoftBodySegment)shape).ContactMargin = 0.25f * Radius;

            AttachEnd(world, rope, rope.Vertices[0], leftCube);
            AttachEnd(world, rope, rope.Vertices[^1], rightCube);

            ropes.Add((rope, StrandColors[s % StrandColors.Length]));
        }
    }

    private static RigidBody CreateDrivenCube(World world, JVector center, out HingeJoint hinge)
    {
        RigidBody cube = world.CreateRigidBody();
        cube.AddShape(new BoxShape(CubeSize));
        cube.Position = center;
        cube.Damping = (0, 0);

        hinge = new HingeJoint(world, cube, world.NullBody, center, JVector.UnitX, JointDrive.Motor);
        return cube;
    }

    private static void AttachEnd(World world, SoftBodyRope rope, RigidBody vertex, RigidBody body)
    {
        BallSocket socket = world.CreateConstraint<BallSocket>(vertex, body);
        socket.Initialize(vertex.Position);
        rope.Springs.Add(socket);
    }

    /// <summary>
    /// Samples a curve over t in [0, 1] at (nearly) equal arc-length spacing.
    /// </summary>
    private static List<JVector> ResamplePath(Func<float, JVector> curve, float spacing)
    {
        const int fineSteps = 4000;
        var fine = new JVector[fineSteps + 1];
        var arc = new float[fineSteps + 1];

        for (int i = 0; i <= fineSteps; i++)
        {
            fine[i] = curve(i / (float)fineSteps);
            if (i > 0) arc[i] = arc[i - 1] + (fine[i] - fine[i - 1]).Length();
        }

        int segments = Math.Max(1, (int)MathF.Round(arc[fineSteps] / spacing));
        float step = arc[fineSteps] / segments;

        var result = new List<JVector> { fine[0] };
        int k = 1;

        for (int i = 1; i < segments; i++)
        {
            float target = i * step;
            while (arc[k] < target) k++;
            float f = (target - arc[k - 1]) / (arc[k] - arc[k - 1]);
            result.Add(fine[k - 1] + f * (fine[k] - fine[k - 1]));
        }

        result.Add(fine[fineSteps]);
        return result;
    }

    private void ApplyTwisterMotor()
    {
        // Opposite directions: the ropes are twisted at twice the speed of each cube.
        leftHinge.Motor!.TargetVelocity = -twistSpeed;
        rightHinge.Motor!.TargetVelocity = twistSpeed;
        leftHinge.Motor.MaximumTorque = twistTorque;
        rightHinge.Motor.MaximumTorque = twistTorque;
    }

    public void DrawGui(Ui root)
    {
        root.Window(MaterialSymbols.Sync + " Twister", controlsWindow, 300f, controls =>
        {
            controls.ItemSpacing(8f);
            controls.Spacing(4f);
            bool changed = controls.Slider("Speed per cube (rad/s)", ref twistSpeed, -12, 12, controls.AvailableWidth).Changed;
            changed |= controls.Slider("Max torque", ref twistTorque, 0, 150, controls.AvailableWidth).Changed;
            if (changed) ApplyTwisterMotor();
            controls.Spacing(4f);
        }, resizable: false, closable: false, id: "rope-twister-controls");
    }

    public void DrawUpdate()
    {
        foreach (var (rope, color) in ropes)
        {
            var vertices = rope.Vertices;

            for (int i = 0; i < vertices.Count; i++)
            {
                JVector p = vertices[i].Position;
                pg.Spheres.Push(MatrixHelper.CreateTranslation(Conversion.FromJitter(p)) *
                                MatrixHelper.CreateScale(2.0f * rope.Radius), color);

                if (i == 0) continue;

                JVector a = vertices[i - 1].Position;
                JVector delta = p - a;
                float length = delta.Length();
                if (length < 1e-6f) continue;

                JQuaternion q = JQuaternion.CreateFromToRotation(JVector.UnitY, delta * (1.0f / length));
                Matrix4 rotation = Conversion.FromJitter(JMatrix.CreateFromQuaternion(q));

                pg.Cylinders.Push(MatrixHelper.CreateTranslation(Conversion.FromJitter(0.5f * (a + p))) * rotation *
                                  MatrixHelper.CreateScale(rope.Radius, length, rope.Radius), color);
            }
        }
    }

    public void CleanUp()
    {
        ropeFilter?.Detach();
        ropeFilter = null;
        foreach (var (rope, _) in ropes) rope.Destroy();
        ropes.Clear();
    }
}
