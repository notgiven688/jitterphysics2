using System;
using System.IO;
using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public static class CarTextureCache
{
    private static Texture2D? carTexture;

    public static Texture2D CarTexture
    {
        get
        {
            if (carTexture == null)
            {
                carTexture = new Texture2D();
                string filename = Path.Combine("assets", "car.tga");

                Image.LoadImage(filename).FixedData((img, ptr) => { carTexture.LoadImage(ptr, img.Width, img.Height); });

                carTexture.SetWrap(Texture.Wrap.Repeat);
                carTexture.SetAnisotropicFiltering(Texture.Anisotropy.Filter_8x);
            }

            return carTexture;
        }
    }
}

public class WheelMesh : TriangleMesh
{
    public WheelMesh() : base("wheel.obj")
    {
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();
        shader.MaterialProperties.ColorMixing.Set(0.05f, 0, 1);

        shader.MaterialProperties.Specular.Set(1, 1, 1);
        shader.MaterialProperties.Shininess.Set(1000);

        base.LightPass(shader);
    }

    public override void Load()
    {
        Texture = CarTextureCache.CarTexture;
        base.Load();
    }
}

public class CarMesh : MultiMesh
{
    public CarMesh() : base("./assets/car.obj")
    {
    }

    public override void LightPass(PhongShader shader)
    {
        if (mesh.Groups.Length == 0) return;

        Texture?.Bind(3);

        shader.MaterialProperties.SetDefaultMaterial();

        Vao.Bind();

        int sof = sizeof(float);
        Mesh.Group mg;

        // chassis
        mg = mesh.Groups[1];
        shader.MaterialProperties.ColorMixing.Set(0.1f, 0, 1.2f);
        shader.MaterialProperties.Shininess.Set(1000);
        shader.MaterialProperties.Specular.Set(1, 1, 1);
        GLDevice.DrawElementsInstanced(DrawMode.Triangles, 3 * (mg.ToExclusive - mg.FromInclusive),
            IndexType.UnsignedInt, mg.FromInclusive * sof * 3, Count);

        // glass
        mg = mesh.Groups[0];
        shader.MaterialProperties.ColorMixing.Set(0, 1, 0);
        shader.MaterialProperties.Color.Set(0.6f, 0.6f, 0.6f);
        shader.MaterialProperties.Alpha.Set(0.6f);
        shader.MaterialProperties.Shininess.Set(1000.0f);
        GLDevice.DrawElementsInstanced(DrawMode.Triangles, 3 * (mg.ToExclusive - mg.FromInclusive),
            IndexType.UnsignedInt, mg.FromInclusive * sof * 3, Count);
        shader.MaterialProperties.Alpha.Set(1.0f);
    }

    public override void Load()
    {
        Texture = CarTextureCache.CarTexture;
        base.Load();
    }
}

public class Demo06 : IDemo
{
    public string Name => "Ray-cast Car";

    private RayCastCar defaultCar = null!;

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        defaultCar = new RayCastCar(world);
        defaultCar.Body.Position = new JVector(0, 2, 0);
        defaultCar.Body.DeactivationTime = TimeSpan.MaxValue;
        defaultCar.Body.Tag = new RigidBodyTag();

        Common.BuildPyramid(-JVector.UnitZ * 20, 10);
        Common.BuildJenga(new JVector(-20, 0, -10), 10);
        Common.BuildWall(new JVector(30, 0, -20), 4);

        world.SolverIterations = (4, 4);
        world.SubstepCount = 2;
    }

    public void Draw()
    {
        var cm = RenderWindow.Instance.CSMRenderer.GetInstance<CarMesh>();
        cm.PushMatrix(Conversion.FromJitter(defaultCar.Body) *
                      MatrixHelper.CreateTranslation(0, -0.2f, 0.8f));

        var whr = RenderWindow.Instance.CSMRenderer.GetInstance<WheelMesh>();

        for (int i = 0; i < 4; i++)
        {
            Wheel wh = defaultCar.Wheels[i];

            Matrix4 rotate = Matrix4.Identity;

            if (i == 1 || i == 3) rotate = MatrixHelper.CreateRotationY(MathF.PI);

            Matrix4 whm = Conversion.FromJitter(defaultCar.Body) *
                          MatrixHelper.CreateTranslation(Conversion.FromJitter(wh.GetWheelCenter())) *
                          MatrixHelper.CreateRotationY((float)wh.SteerAngle) *
                          MatrixHelper.CreateRotationX(-(float)wh.WheelRotation) *
                          rotate;

            whr.PushMatrix(whm);
        }

        double steer, accelerate;
        var kb = Keyboard.Instance;

        if (kb.IsKeyDown(Keyboard.Key.Up)) accelerate = 1.0d;
        else if (kb.IsKeyDown(Keyboard.Key.Down)) accelerate = -1.0d;
        else accelerate = 0.0d;

        if (kb.IsKeyDown(Keyboard.Key.Left)) steer = 1;
        else if (kb.IsKeyDown(Keyboard.Key.Right)) steer = -1;
        else steer = 0.0d;

        defaultCar.SetInput(accelerate, steer);

        defaultCar.Step(1.0d / 100.0d);
    }
}