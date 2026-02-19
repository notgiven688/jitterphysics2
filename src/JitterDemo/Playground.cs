using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class RigidBodyTag(bool doNotDraw = true)
{
    public bool DoNotDraw { get; set; } = doNotDraw;
}

public partial class Playground : RenderWindow
{
    private readonly World world;

    private const float PhysicsTimestep = 1.0f / 100.0f;
    private bool multiThread = true;
    private RigidBodyShape? floorShape;

    private CSMInstance sphereDrawer = null!;
    private CSMInstance boxDrawer = null!;
    private CSMInstance coneDrawer = null!;
    private CSMInstance cylinderDrawer = null!;
    private CSMInstance halfSphereDrawer = null!;
    private CSMInstance floorDrawer = null!;

    private readonly List<IDemo> demos = new()
    {
        new Demo00(),
        new Demo01(),
        new Demo02(),
        new Demo03(),
        new Demo04(),
        new Demo05(),
        new Demo06(),
        new Demo07(),
        //new Demo08(),   // contact manifold test
        new Demo09(),
        new Demo10(),
        // new Demo11(),  // double pendulum
        new Demo12(),
        new Demo13(),
        new Demo14(),
        new Demo15(),
        new Demo16(),
        new Demo17(),
        // new Demo18(),  // point test
        // new Demo19(),  // ray cast test
        new Demo20(),
        new Demo21(),
        new Demo22(),
        new Demo23(),
        new Demo24(),
        new Demo25(),
        new Demo26(), // angular sweep
        new Demo27(),
        new Demo28(),
        new Demo29(),
    };

    private IDemo? currentDemo;

    private void SwitchDemo(int index)
    {
        (currentDemo as ICleanDemo)?.CleanUp();
        ResetScene();
        currentDemo = demos[index];
        currentDemo.Build(this, world);
    }

    public Playground()
    {
        world = new World();
        world.NullBody.Tag = new RigidBodyTag();
        drawBox = DrawBox;
    }

    private void ResetScene()
    {
        floorShape = null;
        world.Clear();
        world.DynamicTree.Filter = World.DefaultDynamicTreeFilter;
        world.BroadPhaseFilter = null;
        world.NarrowPhaseFilter = new TriangleEdgeCollisionFilter();
        world.Gravity = new JVector(0, -9.81f, 0);
        world.SubstepCount = 1;
        world.SolverIterations = (8, 4);
    }

    public void AddFloor()
    {
        RigidBody body = World.CreateRigidBody();
        floorShape = new BoxShape(200, 200, 200);
        body.Position = new JVector(0, -100, 0f);
        body.MotionType = MotionType.Static;
        body.AddShape(floorShape);
    }

    public override void Load()
    {
        base.Load();
        ResetScene();
        AddFloor();

        sphereDrawer = CSMRenderer.GetInstance<Sphere>();
        boxDrawer = CSMRenderer.GetInstance<Cube>();
        coneDrawer = CSMRenderer.GetInstance<Cone>();
        cylinderDrawer = CSMRenderer.GetInstance<Cylinder>();
        halfSphereDrawer = CSMRenderer.GetInstance<HalfSphere>();
        floorDrawer = CSMRenderer.GetInstance<JitterFloor>();

        VerticalSync = false;
    }

    public RigidBodyShape? FloorShape => floorShape;

    public World World => world;

    public void ShootPrimitive()
    {
        const float primitiveVelocity = 20.0f;

        var pos = Camera.Position;
        var dir = Camera.Direction;

        var sb = World.CreateRigidBody();
        sb.Position = Conversion.ToJitterVector(pos);
        sb.Velocity = Conversion.ToJitterVector(dir * primitiveVelocity);

        var ss = new BoxShape(1);
        sb.AddShape(ss);
    }

    private void DrawShape(Shape shape, in Matrix4 mat, in Vector3 color)
    {
        Matrix4 ms;

        switch (shape)
        {
            case BoxShape s:
                ms = MatrixHelper.CreateScale(s.Size.X, s.Size.Y, s.Size.Z);
                boxDrawer.PushMatrix(mat * ms, color);
                break;
            case SphereShape s:
                ms = MatrixHelper.CreateScale(s.Radius * 2);
                sphereDrawer.PushMatrix(mat * ms, color);
                break;
            case CylinderShape s:
                ms = MatrixHelper.CreateScale(s.Radius, s.Height, s.Radius);
                cylinderDrawer.PushMatrix(mat * ms, color);
                break;
            case CapsuleShape s:
                ms = MatrixHelper.CreateScale(s.Radius, s.Length, s.Radius);
                cylinderDrawer.PushMatrix(mat * ms, color);
                ms = MatrixHelper.CreateTranslation(0, 0.5f * s.Length, 0) * MatrixHelper.CreateScale(s.Radius * 2);
                halfSphereDrawer.PushMatrix(mat * ms, color);
                halfSphereDrawer.PushMatrix(mat * MatrixHelper.CreateRotationX(MathF.PI) * ms, color);
                break;
            case ConeShape s:
                ms = MatrixHelper.CreateScale(s.Radius * 2, s.Height, s.Radius * 2);
                coneDrawer.PushMatrix(mat * ms, color);
                break;
        }
    }

    public override void Draw()
    {
        world.Step(PhysicsTimestep, multiThread);

        UpdateDisplayText();
        LayoutGui();

        foreach (RigidBody body in world.RigidBodies)
        {
            if (body.Tag is RigidBodyTag { DoNotDraw: true }) continue;

            Matrix4 mat = Conversion.FromJitter(body);

            foreach (var shape in body.Shapes)
            {
                if (shape == floorShape)
                {
                    floorDrawer.PushMatrix(Matrix4.Identity);
                    continue;
                }

                var color = ColorGenerator.GetColor(shape.GetHashCode());
                if (!shape.RigidBody.Data.IsActive) color += new Vector3(0.2f, 0.2f, 0.2f);

                if (shape is TransformedShape ts)
                {
                    Matrix4 tmat = mat * MatrixHelper.CreateTranslation(Conversion.FromJitter(ts.Translation)) *
                                   Conversion.FromJitter(ts.Transformation);
                    DrawShape(ts.OriginalShape, tmat, color);
                }
                else
                {
                    DrawShape(shape, mat, color);
                }
            }
        }

        (currentDemo as IDrawUpdate)?.DrawUpdate();

        DebugDraw();

        if (!GuiRenderer.WantsCaptureMouse && (Mouse.ButtonPressBegin(Mouse.Button.Left) || grabbing))
        {
            Pick();
        }

        if (!Mouse.IsButtonDown(Mouse.Button.Left))
        {
            if (grabConstraint != null) world.Remove(grabConstraint);
            grabBody = null;
            grabConstraint = null;
            grabbing = false;
        }

        if (Keyboard.KeyPressBegin(Keyboard.Key.M))
        {
            multiThread = !multiThread;
        }

        if (!GuiRenderer.WantsCaptureKeyboard && Keyboard.KeyPressBegin(Keyboard.Key.Space))
        {
            ShootPrimitive();
        }

        base.Draw();
    }
}