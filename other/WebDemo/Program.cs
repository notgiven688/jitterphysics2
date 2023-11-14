using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

static Texture2D GenCheckedTexture(int size, int checks, Color colorA, Color colorB)
{
    Image imageMag = GenImageChecked(size, size, checks, checks, colorA, colorB);
    Texture2D textureMag = LoadTextureFromImage(imageMag);
    UnloadImage(imageMag);
    return textureMag;
}

// Set a hint for anti-aliasing
SetConfigFlags(ConfigFlags.FLAG_MSAA_4X_HINT);

// Initialize a 1200x800 px window with a title
InitWindow(1200, 800, "BoxDrop Example");

// Dynamically create a plane model
Texture2D texture = GenCheckedTexture(10, 1,  Color.LIGHTGRAY, Color.GRAY);
Model planeModel = LoadModelFromMesh(GenMeshPlane(10, 10, 10, 10));
SetMaterialTexture(ref planeModel, 0, MaterialMapIndex.MATERIAL_MAP_DIFFUSE, ref texture);

// Create a camera
Camera3D camera = new ()
{
    Position = new Vector3(-20.0f, 8.0f, 10.0f),
    Target = new Vector3(0.0f, 4.0f, 0.0f),
    Up = new Vector3(0.0f, 1.0f, 0.0f),
    FovY = 45.0f,
    Projection = CameraProjection.CAMERA_PERSPECTIVE
};

// Set a target of 100 fps
SetTargetFPS(100);

// Simple render loop
while (!WindowShouldClose())
{
    BeginDrawing();
    ClearBackground(Color.BLUE);

    BeginMode3D(camera);

    DrawModel(planeModel, Vector3.Zero, 1.0f, Color.WHITE);

    EndMode3D();
    DrawText($"{GetFPS()} fps", 10, 10, 20, Color.WHITE); 

    EndDrawing();
}

CloseWindow();