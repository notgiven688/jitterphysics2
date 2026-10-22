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
                Image.LoadImage(filename).FixedData((img, ptr) => carTexture.LoadImage(ptr, img.Width, img.Height));
                carTexture.SetWrap(TextureWrap.Repeat);
                carTexture.SetAnisotropy(Anisotropy.X8);
            }

            return carTexture;
        }
    }
}

public class WheelMesh : TriangleMeshDrawable
{
    public WheelMesh() : base("wheel.obj")
    {
        Material = new Material
        {
            Tint = Vector3.Zero,
            Specular = new Vector3(1, 1, 1),
            Shininess = 1000f,
            Alpha = 1f,
            VertexColorWeight = 0.05f,
            TextureWeight = 1f,
            Texture = CarTextureCache.CarTexture
        };
    }
}

public class CarMesh : TriangleMeshDrawable
{
    public CarMesh() : base("car.obj")
    {
        // Default covers the chassis group.
        Material = new Material
        {
            Tint = Vector3.Zero,
            Specular = new Vector3(1, 1, 1),
            Shininess = 1000f,
            Alpha = 1f,
            VertexColorWeight = 0.1f,
            TextureWeight = 1.2f,
            Texture = CarTextureCache.CarTexture
        };

        // Group 0 is the glass canopy — translucent, no texture.
        Groups = new[]
        {
            new MaterialSlot(0, new Material
            {
                Tint = new Vector3(0.6f, 0.6f, 0.6f),
                Specular = new Vector3(1, 1, 1),
                Shininess = 1000f,
                Alpha = 0.6f,
                VertexColorWeight = 0f,
                TextureWeight = 0f
            })
        };
    }
}

public class Demo06 : IDemo
{
    public string Name => "Ray-cast Car";
    public string Description => "Drivable car using raycasts for wheel-ground contact.";
    public string Controls => "Arrow Keys - Steer and accelerate";

    private RayCastCar defaultCar = null!;

    public void Build(Playground pg)
    {
        World world = pg.World;

        pg.AddFloor();

        defaultCar = new RayCastCar(world);
        defaultCar.Body.Position = new JVector(0, 2, 0);
        defaultCar.Body.DeactivationTime = TimeSpan.MaxValue;
        defaultCar.Body.Tag = new RigidBodyTag();

        Common.BuildPyramid(world, -JVector.UnitZ * 20, 10);
        Common.BuildJenga(world, new JVector(-20, 0, -10), 10);
        Common.BuildWall(world, new JVector(30, 0, -20), 4);

        world.SolverIterations = (4, 4);
        world.SubstepCount = 2;
    }

    public void DrawUpdate()
    {
        var cm = RenderWindow.Instance.GetDrawable<CarMesh>();
        cm.Push(Conversion.FromJitter(defaultCar.Body) *
                MatrixHelper.CreateTranslation(0, -0.3f, 0.8f));

        var whr = RenderWindow.Instance.GetDrawable<WheelMesh>();

        for (int i = 0; i < 4; i++)
        {
            Wheel wh = defaultCar.Wheels[i];

            Matrix4 rotate = Matrix4.Identity;
            if (i == 1 || i == 3) rotate = MatrixHelper.CreateRotationY(MathF.PI);

            Matrix4 whm = Conversion.FromJitter(defaultCar.Body) *
                          MatrixHelper.CreateTranslation(Conversion.FromJitter(wh.GetWheelCenter())) *
                          MatrixHelper.CreateRotationY(wh.SteerAngle) *
                          MatrixHelper.CreateRotationX(-wh.WheelRotation) *
                          rotate;

            whr.Push(whm);
        }

        float steer, accelerate;
        var kb = Keyboard.Instance;

        if (kb.IsKeyDown(Keyboard.Key.Up)) accelerate = 1f;
        else if (kb.IsKeyDown(Keyboard.Key.Down)) accelerate = -1f;
        else accelerate = 0f;

        if (kb.IsKeyDown(Keyboard.Key.Left)) steer = 1;
        else if (kb.IsKeyDown(Keyboard.Key.Right)) steer = -1;
        else steer = 0f;

        defaultCar.SetInput(accelerate, steer);
        defaultCar.Step(1f / 100f);
    }
}
