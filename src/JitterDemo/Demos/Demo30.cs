using System;
using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo30 : IDemo
{
    public string Name => "Sweep Casts";
    public string Description => "Camera-driven sweep casts through a simple physics scene. " +
                                 "Switch between sphere, box, capsule and cylinder queries " +
                                 "to compare the impact pose for the same view direction.";
    public string Controls => "O/P - Previous/next cast type";

    private enum CastKind
    {
        Sphere,
        Box,
        Capsule,
        Cylinder
    }

    private Playground pg = null!;

    private SphereDrawable sphereDrawer = null!;
    private CubeDrawable boxDrawer = null!;
    private CylinderDrawable cylinderDrawer = null!;
    private HalfSphereDrawable halfSphereDrawer = null!;

    private CastKind castKind = CastKind.Sphere;

    private const float Radius = 0.45f;
    private static readonly JVector boxHalfExtents = new(0.75f, 0.5f, 1.1f);
    private const float HalfLength = 0.45f;
    private const float HalfHeight = 0.45f;
    private static readonly Vector3 castColor = new(0.35f, 0.35f, 0.35f);

    public void Build(Playground pg)
    {
        World world = pg.World;

        this.pg = pg;

        sphereDrawer = pg.Spheres;
        boxDrawer = pg.Cubes;
        cylinderDrawer = pg.Cylinders;
        halfSphereDrawer = pg.HalfSpheres;

        pg.AddFloor();

        Common.BuildJenga(world, new JVector(-8, 0, -10), 12);
        Common.BuildPyramid(world, new JVector(6, 0, -12), 10);
        Common.BuildWall(world, new JVector(-4, 0, -22), 8, 8);
    }

    private void HandleInput()
    {
        Keyboard kb = Keyboard.Instance;

        if (kb.KeyPressBegin(Keyboard.Key.O))
        {
            castKind = (CastKind)(((int)castKind + 3) % 4);
        }

        if (kb.KeyPressBegin(Keyboard.Key.P))
        {
            castKind = (CastKind)(((int)castKind + 1) % 4);
        }

    }

    private void DrawCastShape(CastKind kind, in JVector position, in JQuaternion orientation, in Vector3 color)
    {
        Matrix4 mat = MatrixHelper.CreateTranslation(Conversion.FromJitter(position)) *
                      Conversion.FromJitter(JMatrix.CreateFromQuaternion(orientation));

        switch (kind)
        {
            case CastKind.Sphere:
                sphereDrawer.Push(mat * MatrixHelper.CreateScale(Radius * 2.0f), color);
                break;

            case CastKind.Box:
                boxDrawer.Push(mat * MatrixHelper.CreateScale(
                    boxHalfExtents.X * 2.0f,
                    boxHalfExtents.Y * 2.0f,
                    boxHalfExtents.Z * 2.0f), color);
                break;

            case CastKind.Capsule:
                cylinderDrawer.Push(mat * MatrixHelper.CreateScale(
                    Radius, HalfLength * 2.0f, Radius), color);

                Matrix4 cap = MatrixHelper.CreateTranslation(0, HalfLength, 0) *
                              MatrixHelper.CreateScale(Radius * 2.0f);
                halfSphereDrawer.Push(mat * cap, color);
                halfSphereDrawer.Push(mat * MatrixHelper.CreateRotationX(MathF.PI) * cap, color);
                break;

            case CastKind.Cylinder:
                cylinderDrawer.Push(mat * MatrixHelper.CreateScale(
                    Radius, HalfHeight * 2.0f, Radius), color);
                break;
        }
    }

    public void DrawUpdate()
    {
        HandleInput();

        Camera camera = RenderWindow.Instance.Camera;
        JVector origin = Conversion.ToJitterVector(camera.Position);
        JVector direction = JVector.NormalizeSafe(Conversion.ToJitterVector(camera.Direction));

        JQuaternion shapeOrientation = JQuaternion.Identity;

        bool hit;
        float lambda;

        switch (castKind)
        {
            case CastKind.Sphere:
                hit = pg.World.DynamicTree.SweepCastSphere(Radius, origin, direction, null, null,
                    out _, out _, out _, out _, out lambda);
                break;

            case CastKind.Box:
                hit = pg.World.DynamicTree.SweepCastBox(boxHalfExtents, shapeOrientation, origin, direction, null, null,
                    out _, out _, out _, out _, out lambda);
                break;

            case CastKind.Capsule:
                hit = pg.World.DynamicTree.SweepCastCapsule(Radius, HalfLength, shapeOrientation, origin, direction, null, null,
                    out _, out _, out _, out _, out lambda);
                break;

            default:
                hit = pg.World.DynamicTree.SweepCastCylinder(Radius, HalfHeight, shapeOrientation, origin, direction, null, null,
                    out _, out _, out _, out _, out lambda);
                break;
        }

        if (!hit)
        {
            return;
        }

        JVector hitPosition = origin + direction * lambda;
        DrawCastShape(castKind, hitPosition, castKind == CastKind.Sphere ? JQuaternion.Identity : shapeOrientation, castColor);
    }
}
