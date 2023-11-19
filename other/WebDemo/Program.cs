using System.Numerics;
using Raylib_cs;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using static Raylib_cs.Raylib;

static Matrix4x4 GetRayLibTransformMatrix(RigidBody body)
{
    JMatrix ori = body.Orientation;
    JVector pos = body.Position;

    return new Matrix4x4(ori.M11, ori.M12, ori.M13, pos.X,
                         ori.M21, ori.M22, ori.M23, pos.Y,
                         ori.M31, ori.M32, ori.M33, pos.Z,
                         0, 0, 0, 1.0f);
}

static JVector ToJitter(in Vector3 v) => new JVector(v.X, v.Y, v.Z);

// initialize the Jitter physics world
World world = new();

RigidBody SpawnBox(Vector3 position)
{
    RigidBody body = world.CreateRigidBody();
    body.AddShape(new BoxShape(1));
    body.Position = ToJitter(position);
    return body;
}

RigidBody SpawnSphere(Vector3 position)
{
    RigidBody body = world.CreateRigidBody();
    body.AddShape(new SphereShape(0.5f));
    body.Position = ToJitter(position);
    return body;
}

RigidBody? planeBody = null;

void InitWorld()
{
    world.Clear();

    // add a body representing the plane
    planeBody = world.CreateRigidBody();
    planeBody.AddShape(new BoxShape(12));
    planeBody.Position = new JVector(0, -6, 0);
    planeBody.IsStatic = true;

    for (int i = 0; i < 8; i++)
    {
        SpawnBox(new Vector3(0, 3f * i + 0.5f, 0));
        SpawnSphere(new Vector3(0, 3f * i + 1.5f, 0));
    }
}

// Initialization
//--------------------------------------------------------------------------------------
const int screenWidth = 800;
const int screenHeight = 600;

// Enable Multi Sampling Anti Aliasing 4x (if available)
SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT);
InitWindow(screenWidth, screenHeight, "Jitter2 Demo");

// Define the camera to look into our 3d world
Camera3D camera = new()
{
    Position = new Vector3(2.0f, 8.0f, -16.0f),
    Target = new Vector3(0.0f, 0.5f, 0.0f),
    Up = new Vector3(0.0f, 1.0f, 0.0f),
    FovY = 45.0f,
    Projection = CameraProjection.CAMERA_PERSPECTIVE
    
};

// Load models and texture
Model modelPlane = LoadModelFromMesh(GenMeshPlane(12, 12, 1, 1));
Model modelCube = LoadModelFromMesh(GenMeshCube(1.0f, 1.0f, 1.0f));
Model modelSphere = LoadModelFromMesh(GenMeshSphere(0.5f, 32, 32));

Texture2D texture = LoadTexture("assets/texel_checker.png");
Texture2D textureSmall = LoadTexture("assets/texel_checker_small.png");

// Assign texture to default model material
Raylib.SetMaterialTexture(ref modelCube, 0, MaterialMapIndex.MATERIAL_MAP_ALBEDO, ref textureSmall);
Raylib.SetMaterialTexture(ref modelSphere, 0, MaterialMapIndex.MATERIAL_MAP_ALBEDO, ref texture);
Raylib.SetMaterialTexture(ref modelPlane, 0, MaterialMapIndex.MATERIAL_MAP_ALBEDO, ref texture);

// Load shader
Shader shader = LoadShader("assets/lighting.vs", "assets/lighting.fs");

unsafe
{
    shader.Locs[(int)ShaderLocationIndex.SHADER_LOC_MATRIX_MODEL] = GetShaderLocation(shader, "matModel");
    shader.Locs[(int)ShaderLocationIndex.SHADER_LOC_VECTOR_VIEW] = GetShaderLocation(shader, "viewPos");
}

// Ambient light level
int ambientLoc = GetShaderLocation(shader, "ambient");
Raylib.SetShaderValue(
    shader,
    ambientLoc,
    new float[] { 0.2f, 0.2f, 0.2f, 1.0f },
    ShaderUniformDataType.SHADER_UNIFORM_VEC4
);

// NOTE: All models share the same shader
Raylib.SetMaterialShader(ref modelPlane, 0, ref shader);
Raylib.SetMaterialShader(ref modelCube, 0, ref shader);
Raylib.SetMaterialShader(ref modelSphere, 0, ref shader);

// Using just 1 point lights
Rlights.CreateLight(0, LightType.Point, new Vector3(0, 4, 6), Vector3.Zero, Color.WHITE, shader);

SetTargetFPS(60);

InitWorld();

//SetCamera(camera, CameraMode.CAMERA_ORBITAL);

//--------------------------------------------------------------------------------------

// Main game loop
while (!WindowShouldClose())
{
    // Update
    //----------------------------------------------------------------------------------

    UpdateCamera(ref camera, CameraMode.CAMERA_ORBITAL);

    // if (IsKeyPressed(KeyboardKey.KEY_SPACE))
    // {
    //     var ob = SpawnBox(camera.position);
    //     ob.Velocity = JVector.Normalize(ToJitter(camera.target - camera.position)) * 10.0f;
    // }

    if (IsKeyPressed(KeyboardKey.KEY_R) || IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
    {
        InitWorld();
    }

    unsafe
    {
        // Update the light shader with the camera view position
        Raylib.SetShaderValue(
            shader,
            shader.Locs[(int)ShaderLocationIndex.SHADER_LOC_VECTOR_VIEW],
            camera.Position,
            ShaderUniformDataType.SHADER_UNIFORM_VEC3
        );
    }
    //----------------------------------------------------------------------------------

    // Draw
    //----------------------------------------------------------------------------------
    BeginDrawing();
    ClearBackground(Color.GRAY);

    BeginMode3D(camera);

    // Draw the spheres and cubes
    foreach (var body in world.RigidBodies)
    {
        if (body == planeBody) continue; // do not draw this
        if (body == world.NullBody) continue;

        if (body.Shapes[0] is BoxShape)
        {
            modelCube.Transform = GetRayLibTransformMatrix(body);
            DrawModel(modelCube, Vector3.Zero, 1.0f, Color.WHITE);
        }
        else if (body.Shapes[0] is SphereShape)
        {
            modelSphere.Transform = GetRayLibTransformMatrix(body);
            DrawModel(modelSphere, Vector3.Zero, 1.0f, Color.WHITE);
        }
    }

    DrawModel(modelPlane, Vector3.Zero, 1.0f, Color.WHITE);

    EndMode3D();

    DrawText(
        $"{GetFPS()} fps\nPress R (or touch) to reset",
        10,
        10,
        20,
        Color.RAYWHITE
    );

    EndDrawing();

    world.Step(1.0f / 100.0f, true);
    //----------------------------------------------------------------------------------
}

// De-Initialization
//--------------------------------------------------------------------------------------
UnloadModel(modelCube);
UnloadModel(modelSphere);

UnloadTexture(texture);
UnloadShader(shader);

CloseWindow();
//--------------------------------------------------------------------------------------

return 0;