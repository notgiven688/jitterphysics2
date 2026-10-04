using System;
using System.Collections.Generic;
using System.Drawing;
using Vellum;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;
using Color = Vellum.Rendering.Color;

namespace JitterDemo;

public class RigidBodyTag(bool doNotDraw = true)
{
    public bool DoNotDraw { get; set; } = doNotDraw;
}

// Built-in primitive drawables. Subclassing is the simplest way to key them in
// the RenderWindow's Type-based registry and to configure per-shape material bits.
public sealed class CubeDrawable() : InstancedDrawable(Mesh.Cube());
public sealed class SphereDrawable() : InstancedDrawable(Mesh.Sphere());
public sealed class ConeDrawable() : InstancedDrawable(Mesh.Cone());
public sealed class CylinderDrawable() : InstancedDrawable(Mesh.Cylinder());
public sealed class HalfSphereDrawable() : InstancedDrawable(Mesh.HalfSphere());

public sealed class FloorDrawable : InstancedDrawable
{
    public FloorDrawable() : base(Mesh.Quad(halfSize: 100, uvScale: 100))
    {
        var texture = new Texture2D();
        Image.LoadImage(System.IO.Path.Combine("assets", "unit.tga"))
             .FixedData((img, ptr) => texture.LoadImage(ptr, img.Width, img.Height));
        texture.SetWrap(TextureWrap.Repeat);
        texture.SetAnisotropy(Anisotropy.X8);

        Material = new Material
        {
            Tint = Vector3.Zero,
            Specular = new Vector3(0.1f, 0.1f, 0.1f),
            Shininess = 10f,
            Alpha = 1f,
            VertexColorWeight = 0f,
            TextureWeight = 1f,
            Texture = texture
        };
    }
}

public partial class Playground : RenderWindow
{
    private readonly World world;

    private const float PhysicsTimestep = 1f / 100f;
    private bool multiThread = true;
    private RigidBodyShape? floorShape;

    private CubeDrawable cubes = null!;
    private SphereDrawable spheres = null!;
    private ConeDrawable cones = null!;
    private CylinderDrawable cylinders = null!;
    private HalfSphereDrawable halfSpheres = null!;
    private FloorDrawable floor = null!;

    public CubeDrawable Cubes => cubes;
    public SphereDrawable Spheres => spheres;
    public ConeDrawable Cones => cones;
    public CylinderDrawable Cylinders => cylinders;
    public HalfSphereDrawable HalfSpheres => halfSpheres;
    public FloorDrawable Floor => floor;

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
        new Demo30(),
        new Demo31(),
        new Demo32(),
    };

    private IDemo? currentDemo;

    private void SwitchDemo(int index)
    {
        (currentDemo as ICleanDemo)?.CleanUp();
        ResetScene();
        selectedDemoIndex = index;
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
        world.SpeculativeRelaxationFactor = 0.9f;
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

        cubes = GetDrawable<CubeDrawable>();
        spheres = GetDrawable<SphereDrawable>();
        cones = GetDrawable<ConeDrawable>();
        cylinders = GetDrawable<CylinderDrawable>();
        halfSpheres = GetDrawable<HalfSphereDrawable>();
        floor = GetDrawable<FloorDrawable>();

        VerticalSync = false;

        ConfigureGuiTheme();
    }

    public RigidBodyShape? FloorShape => floorShape;

    public World World => world;

    public void ShootPrimitive()
    {
        const float primitiveVelocity = 20f;

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
                cubes.Push(mat * ms, color);
                break;
            case SphereShape s:
                ms = MatrixHelper.CreateScale(s.Radius * 2);
                spheres.Push(mat * ms, color);
                break;
            case CylinderShape s:
                ms = MatrixHelper.CreateScale(s.Radius, s.Height, s.Radius);
                cylinders.Push(mat * ms, color);
                break;
            case CapsuleShape s:
                ms = MatrixHelper.CreateScale(s.Radius, s.Length, s.Radius);
                cylinders.Push(mat * ms, color);
                ms = MatrixHelper.CreateTranslation(0, 0.5f * s.Length, 0) * MatrixHelper.CreateScale(s.Radius * 2);
                halfSpheres.Push(mat * ms, color);
                halfSpheres.Push(mat * MatrixHelper.CreateRotationX(MathF.PI) * ms, color);
                break;
            case ConeShape s:
                ms = MatrixHelper.CreateScale(s.Radius * 2, s.Height, s.Radius * 2);
                cones.Push(mat * ms, color);
                break;
        }
    }

    protected override void DrawCustomOverlay(int logicalWidth, int logicalHeight,
        int framebufferWidth, int framebufferHeight)
    {
        if (!overlayFramePrepared)
            PrepareCustomOverlayFrame(logicalWidth, logicalHeight, framebufferWidth, framebufferHeight);

        Gui.EndFrame();
        overlayFramePrepared = false;
        WantsCaptureKeyboard = Gui.WantsCaptureKeyboard;
        WantsCaptureMouse = Gui.WantsCaptureMouse;
    }

    public override void Draw()
    {
        world.Step(PhysicsTimestep, multiThread);

        UpdateDisplayText();
        (int framebufferWidth, int framebufferHeight) = FramebufferSize;
        PrepareCustomOverlayFrame(Width, Math.Max(1, Height), framebufferWidth, framebufferHeight);

        foreach (RigidBody body in world.RigidBodies)
        {
            if (body.Tag is RigidBodyTag { DoNotDraw: true }) continue;

            Matrix4 mat = Conversion.FromJitter(body);

            foreach (var shape in body.Shapes)
            {
                if (shape == floorShape)
                {
                    floor.Push(Matrix4.Identity);
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

        if (!WantsCaptureMouse && (Mouse.ButtonPressBegin(Mouse.Button.Left) || grabbing))
        {
            Pick();
        }

        if (!Mouse.IsButtonDown(Mouse.Button.Left))
        {
            ClearGrab();
        }

        if (Keyboard.KeyPressBegin(Keyboard.Key.M))
        {
            multiThread = !multiThread;
        }

        if (Keyboard.KeyPressBegin(Keyboard.Key.Space))
        {
            ShootPrimitive();
        }

        base.Draw();
    }
}
