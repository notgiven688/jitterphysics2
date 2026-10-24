using System;
using System.Numerics;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Raylib_cs;
using static Raylib_cs.Raylib;

using World world = new();
world.Gravity = new JVector(0, -9.81f, 0);
world.SubstepCount = 4;

RigidBody left = world.CreateRigidBody();
left.AddShape(new BoxShape(2));
left.Position = new JVector(-1, 1, 0);
left.MotionType = MotionType.Static;

RigidBody right = world.CreateRigidBody();
right.AddShape(new BoxShape(2));
right.Position = new JVector(1, 1, 0);

JVector hingeCenter = new(0, 0, 0);
JVector hingeAxis = JVector.UnitZ;
var hinge = new HingeJoint(world, left, right, hingeCenter, hingeAxis);

var leftStyle = new DebugDrawSettings { CurrentColor = 0xFFD166FF };
var rightStyle = new DebugDrawSettings { CurrentColor = 0xFF997FFF };
var hingeStyle = new DebugDrawSettings
{
    GuideSize = 0.65f,
    PointRadius = 0.04f,
    ArrowHeadSize = 0.1f,
    SolidArrows = false,
    ReferenceColor = 0x8CDBAAFF,
    CurrentColor = 0xFFD166FF
};

SetConfigFlags(ConfigFlags.Msaa4xHint);
InitWindow(1000, 750, "Jitter2 - Two boxes and a hinge");
SetTargetFPS(100);

Camera3D camera = new()
{
    Position = new Vector3(5, 3, 9),
    Target = new Vector3(0, -0.2f, 0),
    Up = Vector3.UnitY,
    FovY = 45,
    Projection = CameraProjection.Perspective
};

var bodyDrawer = new RaylibDebugDrawer { ShadeTriangles = true };
var jointDrawer = new RaylibDebugDrawer();
bool paused = true;

while (!WindowShouldClose())
{
    if (IsKeyPressed(KeyboardKey.Space)) paused = !paused;

    if (!paused)
    {
        world.Step(1.0f / 100.0f, true);
    }

    BeginDrawing();
    ClearBackground(Color.Blue);

    BeginMode3D(camera);
    left.DebugDraw(bodyDrawer, leftStyle);
    right.DebugDraw(bodyDrawer, rightStyle);

    // Finish the solid geometry before changing the depth-test state.
    Rlgl.DrawRenderBatchActive();
    Rlgl.DisableDepthTest();
    hinge.DebugDraw(jointDrawer, hingeStyle);
    Rlgl.DrawRenderBatchActive();
    Rlgl.EnableDepthTest();
    EndMode3D();

    DrawText(paused ? "Paused - Space to release" : "Running - Space to pause",
        20, 20, 22, Color.White);
    DrawText("Yellow: fixed box | Coral: dynamic box | Green / gold: hinge guides",
        20, 52, 18, Color.White);
    EndDrawing();
}

CloseWindow();

sealed class RaylibDebugDrawer : IDebugDrawer
{
    public bool ShadeTriangles { get; init; }
    private static readonly Vector3 LightDirection = Vector3.Normalize(new(-1, 2, 3));

    private static Vector3 ToVector(in JVector p) => new(p.X, p.Y, p.Z);

    private static Color ToColor(uint rgba) => new(
        (byte)((rgba >> 24) & 0xFF),
        (byte)((rgba >> 16) & 0xFF),
        (byte)((rgba >> 8) & 0xFF),
        (byte)(rgba & 0xFF));

    public void DrawSegment(in JVector pA, in JVector pB, uint color)
    {
        DrawLine3D(ToVector(pA), ToVector(pB), ToColor(color));
    }

    public void DrawTriangle(in JVector pA, in JVector pB, in JVector pC, uint color)
    {
        Vector3 a = ToVector(pA), b = ToVector(pB), c = ToVector(pC);
        Color tint = ToColor(color);

        // Simple face shading makes the solid boxes easier to read.
        Vector3 normal = Vector3.Cross(b - a, c - a);
        if (ShadeTriangles && normal.LengthSquared() > 0)
        {
            float light = 0.55f + 0.45f * Math.Max(0,
                Vector3.Dot(Vector3.Normalize(normal), LightDirection));
            tint = new Color((byte)(tint.R * light), (byte)(tint.G * light),
                (byte)(tint.B * light), tint.A);
        }

        DrawTriangle3D(a, b, c, tint);
    }
}
