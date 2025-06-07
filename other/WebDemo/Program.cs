using System.Numerics;
using Raylib_cs;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using static Raylib_cs.Raylib;
using System.Runtime.InteropServices.JavaScript;

namespace WebDemo;

public class Playground
{
    private Camera3D camera;
    private Shader shader;
    private Texture2D texture;

    private Model modelPlane;
    private Model modelCube;
    private Model modelSphere;

    private World world;
    private RigidBody planeBody;

    public Playground()
    {
        const int screenWidth = 800;
        const int screenHeight = 600;

        // Enable Multi Sampling Anti Aliasing 4x (if available)
        SetConfigFlags(ConfigFlags.Msaa4xHint);
        InitWindow(screenWidth, screenHeight, "Jitter2 Demo");

        // Define the camera to look into our 3d world
        camera= new()
        {
            Position = new Vector3(2.0f, 8.0f, -16.0f),
            Target = new Vector3(0.0f, 0.5f, 0.0f),
            Up = new Vector3(0.0f, 1.0f, 0.0f),
            FovY = 45.0f,
            Projection = CameraProjection.Perspective
        };

        // Load models and texture
        modelPlane = LoadModelFromMesh(GenMeshPlane(24, 24, 1, 1));
        modelCube = LoadModelFromMesh(GenMeshCube(1.0f, 1.0f, 1.0f));
        modelSphere = LoadModelFromMesh(GenMeshSphere(0.5f, 32, 32));

        var img = LoadImage("assets/texel_checker.png");

        texture = LoadTextureFromImage(img);
        Texture2D textureQ = LoadTextureFromImage(ImageFromImage(img, new Rectangle(0, 0, 512, 512)));

        // Assign texture to default model material
        Raylib.SetMaterialTexture(ref modelCube, 0, MaterialMapIndex.Albedo, ref textureQ);
        Raylib.SetMaterialTexture(ref modelSphere, 0, MaterialMapIndex.Albedo, ref texture);
        Raylib.SetMaterialTexture(ref modelPlane, 0, MaterialMapIndex.Albedo, ref texture);

        // Load shader
        shader = LoadShader("assets/lighting.vs", "assets/lighting.fs");

        unsafe
        {
            shader.Locs[(int)ShaderLocationIndex.MatrixModel] = GetShaderLocation(shader, "matModel");
            shader.Locs[(int)ShaderLocationIndex.VectorView] = GetShaderLocation(shader, "viewPos");
        }

        // Ambient light level
        int ambientLoc = GetShaderLocation(shader, "ambient");
        Raylib.SetShaderValue(
            shader,
            ambientLoc,
            new float[]
            {
                0.2f, 0.2f, 0.2f, 1.0f
            },
            ShaderUniformDataType.Vec4
        );

        // NOTE: All models share the same shader
        Raylib.SetMaterialShader(ref modelPlane, 0, ref shader);
        Raylib.SetMaterialShader(ref modelCube, 0, ref shader);
        Raylib.SetMaterialShader(ref modelSphere, 0, ref shader);

        // Using just 1 point lights
        Rlights.CreateLight(0, LightType.Point, new Vector3(0, 8, 6), Vector3.Zero, Color.White, shader);

        SetTargetFPS(60);

        world = new World();
        InitWorld();
    }

    public void Close()
    {
        UnloadModel(modelCube);
        UnloadModel(modelSphere);

        UnloadTexture(texture);
        UnloadShader(shader);

        CloseWindow();
    }

    void InitWorld()
    {
        world.Clear();

        // add a body representing the plane
        planeBody = world.CreateRigidBody();
        planeBody.AddShape(new BoxShape(24));
        planeBody.Position = new JVector(0, -12, 0);
        planeBody.IsStatic = true;

        for (int i = 0; i < 120; i++)
        {
            SpawnBox(new Vector3(0, 3f * i + 0.5f, 0));
            SpawnSphere(new Vector3(0, 3f * i + 1.5f, 0));
        }
    }

    static JVector ToJitter(in Vector3 v) => new JVector(v.X, v.Y, v.Z);

    static Matrix4x4 GetRayLibTransformMatrix(RigidBody body)
    {
        JMatrix ori = JMatrix.CreateFromQuaternion(body.Orientation);
        JVector pos = body.Position;

        return new Matrix4x4(ori.M11, ori.M12, ori.M13, pos.X,
            ori.M21, ori.M22, ori.M23, pos.Y,
            ori.M31, ori.M32, ori.M33, pos.Z,
            0, 0, 0, 1.0f);
    }

    RigidBody SpawnBox(Vector3 position)
    {
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new BoxShape(1));
        body.Position = ToJitter(position);
        body.Friction = 0.35f;
        return body;
    }

    RigidBody SpawnSphere(Vector3 position)
    {
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new SphereShape(0.5f));
        body.Position = ToJitter(position);
        body.Friction = 0.35f;
        return body;
    }

    public void UpdateFrame()
    {
        UpdateCamera(ref camera, CameraMode.Orbital);

        if (IsKeyPressed(KeyboardKey.R) || IsMouseButtonDown(MouseButton.Left))
        {
            InitWorld();
        }

        unsafe
        {
            // Update the light shader with the camera view position
            Raylib.SetShaderValue(
                shader,
                shader.Locs[(int)ShaderLocationIndex.VectorView],
                camera.Position,
                ShaderUniformDataType.Vec3
            );
        }

        BeginDrawing();
        ClearBackground(Color.Gray);

        BeginMode3D(camera);

        // Draw the spheres and cubes
        foreach (var body in world.RigidBodies)
        {
            if (body == planeBody) continue; // do not draw this
            if (body == world.NullBody) continue;

            if (body.Shapes[0] is BoxShape)
            {
                modelCube.Transform = GetRayLibTransformMatrix(body);
                DrawModel(modelCube, Vector3.Zero, 1.0f, Color.White);
            }
            else if (body.Shapes[0] is SphereShape)
            {
                modelSphere.Transform = GetRayLibTransformMatrix(body);
                DrawModel(modelSphere, Vector3.Zero, 1.0f, Color.White);
            }
        }

        DrawModel(modelPlane, Vector3.Zero, 1.0f, Color.White);

        EndMode3D();

        DrawText($"{GetFPS()} fps\nPress R (or touch) to reset", 10, 10, 20, Color.RayWhite);

        EndDrawing();

        world.Step(1.0f / 100.0f, true);
    }
}

public partial class Application
{
    private static Playground _playground;

    [JSExport]
    public static void UpdateFrame()
    {
        _playground?.UpdateFrame();
    }

    public static void Main()
    {
        _playground = new Playground();
    }
}