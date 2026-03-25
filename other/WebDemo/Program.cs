using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using static Raylib_cs.Raylib;
using static Raylib_cs.Rlgl;
using System.Runtime.InteropServices.JavaScript;

namespace WebDemo;

// ─── Render metadata stored in body.Tag ──────────────────────────────────────

public enum BodyShape { Box, Sphere }

public record struct RenderTag(BodyShape Shape, float Sx, float Sy, float Sz);

// ─── Scene interface ──────────────────────────────────────────────────────────

public interface IScene
{
    string Name { get; }
    string Hint { get; }
    void Build(Playground pg);
}

// ─── Pressurized soft-body sphere ────────────────────────────────────────────

sealed class PressurizedSphere : SoftBody
{
    const float Pressure = 280f;

    private sealed class UnitSphere : ISupportMappable
    {
        public void SupportMap(in JVector direction, out JVector result)
            => result = JVector.Normalize(direction);
        public void GetCenter(out JVector point)
            => throw new NotImplementedException();
    }

    public PressurizedSphere(World world, JVector center, float radius = 1f) : base(world)
    {
        var tris = ShapeHelper.Tessellate(new UnitSphere(), 3);

        var vertMap = new Dictionary<JVector, int>();
        var triIndices = new List<(int, int, int)>();
        var edgeSet = new HashSet<(int, int)>();

        foreach (var tri in tris)
        {
            JVector[] pts = [tri.V0, tri.V1, tri.V2];
            int[] idx = new int[3];
            for (int k = 0; k < 3; k++)
            {
                if (!vertMap.TryGetValue(pts[k], out idx[k]))
                {
                    idx[k] = vertMap.Count;
                    vertMap[pts[k]] = idx[k];
                    var body = world.CreateRigidBody();
                    body.Position = center + pts[k] * radius;
                    body.SetMassInertia(JMatrix.Zero, 15f, true);
                    body.Damping = (0.0004f, 0f);
                    Vertices.Add(body);
                }
            }
            triIndices.Add((idx[0], idx[1], idx[2]));
            for (int k = 0; k < 3; k++)
            {
                int a = idx[k], b = idx[(k + 1) % 3];
                edgeSet.Add(a < b ? (a, b) : (b, a));
            }
        }

        foreach (var (a, b) in edgeSet)
        {
            var spring = world.CreateConstraint<SpringConstraint>(Vertices[a], Vertices[b]);
            spring.Initialize(Vertices[a].Position, Vertices[b].Position);
            spring.Softness = 0.5f;
            Springs.Add(spring);
        }

        foreach (var (i0, i1, i2) in triIndices)
        {
            var shape = new SoftBodyTriangle(this, Vertices[i0], Vertices[i1], Vertices[i2]);
            World.DynamicTree.AddProxy(shape);
            Shapes.Add(shape);
        }
    }

    protected override void WorldOnPostStep(float dt)
    {
        base.WorldOnPostStep(dt);
        if (!IsActive) return;

        float volume = 0f;
        foreach (SoftBodyTriangle tri in Shapes)
        {
            JVector v1 = tri.Vertex1.Position, v2 = tri.Vertex2.Position, v3 = tri.Vertex3.Position;
            volume += ((v2.Y - v1.Y) * (v3.Z - v1.Z) - (v2.Z - v1.Z) * (v3.Y - v1.Y)) * (v1.X + v2.X + v3.X);
        }

        float invVol = 1f / MathF.Max(0.1f, volume);
        foreach (SoftBodyTriangle tri in Shapes)
        {
            JVector p0 = tri.Vertex1.Position, p1 = tri.Vertex2.Position, p2 = tri.Vertex3.Position;
            JVector normal = (p1 - p0) % (p2 - p0);
            JVector force = normal * Pressure * invVol;
            const float maxForce = 2f;
            float fl2 = force.LengthSquared();
            if (fl2 > maxForce * maxForce) force *= maxForce / MathF.Sqrt(fl2);
            tri.Vertex1.AddForce(force, false);
            tri.Vertex2.AddForce(force, false);
            tri.Vertex3.AddForce(force, false);
        }
    }
}

// ─── Scenes ──────────────────────────────────────────────────────────────────

sealed class SceneSoftBall : IScene
{
    public string Name => "Soft Body";
    public string Hint => "Pressurized sphere rolls down a ramp";

    public PressurizedSphere? Sphere { get; private set; }

    public void Build(Playground pg)
    {
        pg.World.SubstepCount = 2;
        pg.World.DynamicTree.Filter = DynamicTreeCollisionFilter.Filter;
        pg.World.BroadPhaseFilter = new BroadPhaseCollisionFilter(pg.World);
        pg.AddGround();

        // Tilted ramp (~30°)
        var ramp = pg.AddBox(new JVector(0f, 3f, 0f), 9f, 0.4f, 3.5f);
        ramp.MotionType = MotionType.Static;
        ramp.Orientation = JQuaternion.CreateRotationZ(-0.52f);
        ramp.Friction = 0.6f;

        // Small 2D pyramid
        int[] layerN = [3, 2, 1];
        for (int layer = 0; layer < layerN.Length; layer++)
        {
            int n = layerN[layer];
            float zOff = layer * 0.5f;
            for (int iz = 0; iz < n; iz++)
                pg.AddBox(new JVector(6f, 0.5f + layer, -1f + zOff + iz));
        }

        Sphere = new PressurizedSphere(pg.World, new JVector(-3f, 10f, 0f));
    }
}

// ─────────────────────────────────────────────────────────────────────────────

sealed class SceneTower : IScene
{
    public string Name => "Plank Tower";
    public string Hint => "Stacked plank tower stress test";

    public void Build(Playground pg)
    {
        pg.World.SolverIterations = (8, 4);
        pg.World.SubstepCount = 3;
        pg.AddGround();

        static void AddPlank(Playground pg, float x, float y, float z, float sx, float sy, float sz)
        {
            var body = pg.AddBox(new JVector(x, y, z), sx, sy, sz);
            body.Friction = 0.4f;
        }

        static void BuildBlock(Playground pg, Vector3 halfExtents, Vector3 shift, int numx, int numy, int numz)
        {
            Vector3[] dimensions = [halfExtents, new Vector3(halfExtents.Z, halfExtents.Y, halfExtents.X)];
            float blockWidth = 2.0f * halfExtents.Z * numx;
            float blockHeight = 2.0f * halfExtents.Y * numy;
            float spacing = (halfExtents.Z * numx - halfExtents.X) / (numz - 1.0f);
            int nx = numx;
            int nz = numz;

            for (int i = 0; i < numy; i++)
            {
                (nx, nz) = (nz, nx);
                Vector3 dim = dimensions[i % 2];
                float y = dim.Y * i * 2.0f;

                for (int j = 0; j < nx; j++)
                {
                    float x = (i % 2 == 0) ? spacing * j * 2.0f : dim.X * j * 2.0f;

                    for (int k = 0; k < nz; k++)
                    {
                        float z = (i % 2 == 0) ? dim.Z * k * 2.0f : spacing * k * 2.0f;
                        AddPlank(pg,
                            x + dim.X + shift.X,
                            y + dim.Y + shift.Y,
                            z + dim.Z + shift.Z,
                            dim.X * 2.0f,
                            dim.Y * 2.0f,
                            dim.Z * 2.0f);
                    }
                }
            }

            Vector3 topDim = new(halfExtents.Z, halfExtents.X, halfExtents.Y);
            int topNx = (int)(blockWidth / (topDim.X * 2.0f));
            int topNz = (int)(blockWidth / (topDim.Z * 2.0f));

            for (int i = 0; i < topNx; i++)
            for (int j = 0; j < topNz; j++)
                AddPlank(pg,
                    i * topDim.X * 2.0f + topDim.X + shift.X,
                    topDim.Y + shift.Y + blockHeight,
                    j * topDim.Z * 2.0f + topDim.Z + shift.Z,
                    topDim.X * 2.0f,
                    topDim.Y * 2.0f,
                    topDim.Z * 2.0f);
        }

        Vector3 halfExtents = new(0.1f, 0.65f, 2.0f);
        int[] layerCounts = [0, 3, 5, 7, 9, 0];
        float stackBaseHeight = 0.0f;

        for (int i = 3; i >= 1; i--)
        {
            int numx = i;
            int numy = layerCounts[i];
            int numz = numx * 3 + 1;
            float blockWidth = numx * halfExtents.Z * 2.0f;
            BuildBlock(
                pg,
                halfExtents,
                new Vector3(-blockWidth / 2.0f, stackBaseHeight, -blockWidth / 2.0f),
                numx,
                numy,
                numz);
            stackBaseHeight += numy * halfExtents.Y * 2.0f + halfExtents.X * 2.0f;
        }

        var ball = pg.AddSphere(new JVector(0f, 5f, -18f), 1.2f);
        ball.SetMassInertia(12f);
        ball.Restitution = 0.15f;
        ball.Friction = 0.7f;
        ball.Velocity = new JVector(0f, 0f, 38f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

sealed class SceneDominoes : IScene
{
    public string Name => "Dominoes";
    public string Hint => "Toppling domino chain";

    // domino geometry
    const float Sp = 0.72f;   // spacing
    const float DH = 2.0f;    // height
    const float DW = 0.9f;    // width
    const float DT = 0.15f;   // thickness
    const float DY = DH * 0.5f;

    public void Build(Playground pg)
    {
        pg.AddGround();

        void Add(float x, float z, float angle)
        {
            var d = pg.AddBox(new JVector(x, DY, z), DT, DH, DW);
            d.Orientation = JQuaternion.CreateRotationY(angle);
            d.Friction = 0.5f;
        }

        // ── Segment 1: straight along +X ─────────────────────────────
        const int n1 = 9;
        const float s1x = -7f, s1z = -7f;
        for (int i = 0; i < n1; i++)
            Add(s1x + i * Sp, s1z, 0f);

        // ── Arc 1: 90° left turn (+X → +Z), R=3, CCW ─────────────────
        float a1cx = s1x + n1 * Sp, a1cz = s1z + 3f;
        const int na1 = 7;
        for (int i = 1; i <= na1; i++)
        {
            float phi = -MathF.PI / 2f + i * (MathF.PI / 2f) / (na1 + 1);
            Add(a1cx + 3f * MathF.Cos(phi), a1cz + 3f * MathF.Sin(phi), -(phi + MathF.PI / 2f));
        }

        // ── Segment 2: straight along +Z ─────────────────────────────
        const int n2 = 9;
        float s2x = a1cx + 3f, s2z = a1cz;
        for (int i = 0; i < n2; i++)
            Add(s2x, s2z + i * Sp, -MathF.PI / 2f);

        // ── Arc 2: 90° right turn (+Z → +X), R=3, CW ─────────────────
        float a2cx = s2x + 3f, a2cz = s2z + n2 * Sp;
        const int na2 = 7;
        for (int i = 1; i <= na2; i++)
        {
            float phi = MathF.PI - i * (MathF.PI / 2f) / (na2 + 1);
            Add(a2cx + 3f * MathF.Cos(phi), a2cz + 3f * MathF.Sin(phi), MathF.PI / 2f - phi);
        }

        // ── Segment 3: straight along +X ─────────────────────────────
        const int n3 = 9;
        float s3x = a2cx, s3z = a2cz + 3f;
        for (int i = 0; i < n3; i++)
            Add(s3x + i * Sp, s3z, 0f);

        // ── Trigger ball ──────────────────────────────────────────────
        var ball = pg.AddSphere(new JVector(s1x - 3.5f, 0.9f, s1z), 0.9f);
        ball.SetMassInertia(3f);
        ball.Restitution = 0.1f;
        ball.Friction = 0.8f;
        ball.Velocity = new JVector(4f, 0f, 0f);
        ball.AngularVelocity = new JVector(0f, 0f, -4.4f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

sealed class SceneJenga : IScene
{
    public string Name => "Jenga";
    public string Hint => "Projectile tests a classic stacked tower";

    public void Build(Playground pg)
    {
        pg.World.SolverIterations = (8, 4);
        pg.World.SubstepCount = 3;
        pg.AddGround();

        const float bD = 0.8f, bH = 0.8f, bW = 3f * bD; // bW = 3*bD keeps both layer orientations identical
        const int layers = 22;
        for (int layer = 0; layer < layers; layer++)
        {
            float y = bH * 0.5f + layer * bH;
            if (layer % 2 == 0)
                for (int i = -1; i <= 1; i++)
                    pg.AddBox(new JVector(i * bD, y, 0f), bD, bH, bW);
            else
                for (int i = -1; i <= 1; i++)
                    pg.AddBox(new JVector(0f, y, i * bD), bW, bH, bD);
        }

        var projectile = pg.AddSphere(new JVector(-12f, 3.5f, 0f), 0.9f);
        projectile.SetMassInertia(14f);
        projectile.Restitution = 0.15f;
        projectile.Velocity = new JVector(28f, 0f, 0f);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

sealed class SceneWreckingBall : IScene
{
    public const float BallRadius = 1.2f;

    public string Name => "Wrecking Ball";
    public string Hint => "Pendulum demolishes a tower";

    public JVector AnchorPos;
    public RigidBody Ball = null!;

    public void Build(Playground pg)
    {
        pg.AddGround();

        // Box tower on the right
        for (int ix = 0; ix < 3; ix++)
        for (int iz = 0; iz < 3; iz++)
        for (int iy = 0; iy < 8; iy++)
            pg.AddBox(new JVector(5f + ix * 1.0f, 0.5f + iy, iz * 1.0f - 1.0f));

        // Static anchor point above and left
        AnchorPos = new JVector(-1f, 14f, 0f);
        var anchor = pg.World.CreateRigidBody();
        anchor.Position = AnchorPos;
        anchor.MotionType = MotionType.Static;

        // Heavy ball: rope attaches at sphere surface toward anchor
        const float RopeLength = 10f;
        const float PullAngle = MathF.PI * 0.25f; // 45° from vertical
        var ropeDir = new JVector(-MathF.Sin(PullAngle), -MathF.Cos(PullAngle), 0f);
        JVector constraintPt = AnchorPos + ropeDir * RopeLength;           // on ball surface
        var ball = pg.AddSphere(AnchorPos + ropeDir * (RopeLength + BallRadius), BallRadius);
        ball.SetMassInertia(40f);
        ball.Restitution = 0.2f;
        Ball = ball;

        var rope = pg.World.CreateConstraint<DistanceLimit>(anchor, ball);
        rope.Initialize(AnchorPos, constraintPt, LinearLimit.Fixed);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

// ─── Playground ──────────────────────────────────────────────────────────────

public class Playground
{
    private const float FixedTimeStep = 1f / 100f;
    private const int MaxSimulationStepsPerFrame = 4;
    private const float GroundSize = 40f;
    private static readonly (float angular, float linear) DemoDeactivationThreshold = (0.02f, 0.02f);
    private const int UiTitleFontSize = 26;
    private const int UiBodyFontSize = 16;
    private const int UiSmallFontSize = 15;
    private const float UiFontOversample = 2.0f;

    private Camera3D _camera;
    private Shader   _shader;
    private Model    _modelBox, _modelSphere, _modelGround, _modelRope;
    private Texture2D _texFull, _texQuad, _texBlue, _texYellow, _texGray;
    private Font     _fontTitle, _fontBody, _fontSmall;
    private bool     _disposed;
    private float    _accumulatedTime;
    private bool     _hasPendingPointerTap;
    private Vector2  _pendingPointerTap;

    public World World { get; private set; } = null!;

    private readonly List<IScene> _scenes = new()
    {
        new SceneSoftBall(),
        new SceneTower(),
        new SceneDominoes(),
        new SceneJenga(),
        new SceneWreckingBall(),
    };
    private int _sceneIndex;
    private IScene Current => _scenes[_sceneIndex];

    private static readonly Color SceneBgDark = new(28, 30, 36, 255);
    private static readonly Color SceneBgLight = new(238, 243, 248, 255);

    private Color _sceneBackground = SceneBgDark;
    private Color _panelBackground = new(18, 20, 26, 255);
    private Color _tabActive = new(91, 168, 245, 255);
    private Color _tabInactive = new(50, 50, 50, 255);
    private Color _hintColor = new(180, 180, 180, 255);
    private Color _ctrlColor = new(130, 130, 130, 255);

    public Playground()
    {
        SetConfigFlags(ConfigFlags.Msaa4xHint);
        InitWindow(800, 600, "Jitter2 Physics Demo");

        _camera = new Camera3D
        {
            Position   = new Vector3(2f, 12f, -22f),
            Target     = new Vector3(0f, 4f, 0f),
            Up         = new Vector3(0f, 1f, 0f),
            FovY       = 45f,
            Projection = CameraProjection.Perspective
        };

        _modelBox    = LoadModelFromMesh(GenMeshCube(1f, 1f, 1f));
        _modelSphere = LoadModelFromMesh(GenMeshSphere(0.5f, 24, 24));
        _modelGround = LoadModelFromMesh(GenMeshPlane(GroundSize, GroundSize, 1, 1));
        _modelRope   = LoadModelFromMesh(GenMeshCylinder(1f, 1f, 12));

        var img      = LoadImage("assets/texel_checker.png");
        _texFull     = LoadTextureFromImage(img);
        _texQuad     = LoadTextureFromImage(ImageFromImage(img, new Rectangle(0, 0, 512, 512)));
        UnloadImage(img);

        var imgBlue   = GenImageChecked(1024, 1024, 256, 256, new Color(30, 90, 165, 255), new Color(55, 130, 210, 255));
        _texBlue      = LoadTextureFromImage(imgBlue);
        UnloadImage(imgBlue);

        var imgYellow = GenImageChecked(1024, 1024, 128, 128, new Color(180, 140, 20, 255), new Color(235, 195, 50, 255));
        _texYellow    = LoadTextureFromImage(imgYellow);
        UnloadImage(imgYellow);

        var imgGray = GenImageColor(16, 16, new Color(90, 93, 100, 255));
        _texGray    = LoadTextureFromImage(imgGray);
        UnloadImage(imgGray);

        SetMaterialTexture(ref _modelBox,    0, MaterialMapIndex.Albedo, ref _texBlue);
        SetMaterialTexture(ref _modelSphere, 0, MaterialMapIndex.Albedo, ref _texYellow);
        SetMaterialTexture(ref _modelGround, 0, MaterialMapIndex.Albedo, ref _texFull);
        SetMaterialTexture(ref _modelRope,   0, MaterialMapIndex.Albedo, ref _texGray);

        _shader = LoadShader("assets/lighting.vs", "assets/lighting.fs");
        unsafe
        {
            _shader.Locs[(int)ShaderLocationIndex.MatrixModel] = GetShaderLocation(_shader, "matModel");
            _shader.Locs[(int)ShaderLocationIndex.VectorView]  = GetShaderLocation(_shader, "viewPos");
        }
        int ambLoc = GetShaderLocation(_shader, "ambient");
        SetShaderValue(_shader, ambLoc, new float[] { 0.25f, 0.25f, 0.25f, 1f }, ShaderUniformDataType.Vec4);

        SetMaterialShader(ref _modelBox,    0, ref _shader);
        SetMaterialShader(ref _modelSphere, 0, ref _shader);
        SetMaterialShader(ref _modelGround, 0, ref _shader);
        SetMaterialShader(ref _modelRope,   0, ref _shader);

        Rlights.CreateLight(0, LightType.Point, new Vector3(0f, 14f, -8f), Vector3.Zero, Color.White, _shader);

        _fontTitle = LoadFontEx("assets/JetBrainsMono-Regular.ttf", GetAtlasFontSize(UiTitleFontSize), null, 0);
        _fontBody = LoadFontEx("assets/JetBrainsMono-Regular.ttf", GetAtlasFontSize(UiBodyFontSize), null, 0);
        _fontSmall = LoadFontEx("assets/JetBrainsMono-Regular.ttf", GetAtlasFontSize(UiSmallFontSize), null, 0);
        SetTextureFilter(_fontTitle.Texture, TextureFilter.Bilinear);
        SetTextureFilter(_fontBody.Texture, TextureFilter.Bilinear);
        SetTextureFilter(_fontSmall.Texture, TextureFilter.Bilinear);

        SetTargetFPS(60);
        LoadScene(0);
    }

    // ─── Scene management ────────────────────────────────────────────────────

    private void LoadScene(int index)
    {
        _sceneIndex = index;
        World?.Dispose();
        World = new World();
        World.Gravity = new JVector(0f, -9.81f, 0f);
        Current.Build(this);
    }

    // ─── Scene helpers ───────────────────────────────────────────────────────

    public RigidBody AddGround(float y = 0f)
    {
        var body = World.CreateRigidBody();
        body.AddShape(new BoxShape(GroundSize, 2f, GroundSize));
        body.Position = new JVector(0f, y - 1f, 0f);
        body.MotionType = MotionType.Static;
        // no Tag → not drawn by DrawBodies
        return body;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        World?.Dispose();
        UnloadFont(_fontTitle);
        UnloadFont(_fontBody);
        UnloadFont(_fontSmall);
        UnloadModel(_modelBox);
        UnloadModel(_modelSphere);
        UnloadModel(_modelGround);
        UnloadModel(_modelRope);
        UnloadTexture(_texQuad);
        UnloadTexture(_texFull);
        UnloadTexture(_texBlue);
        UnloadTexture(_texYellow);
        UnloadTexture(_texGray);
        UnloadShader(_shader);
    }

    public RigidBody AddBox(JVector pos, float sx = 1f, float sy = 1f, float sz = 1f)
    {
        var body = World.CreateRigidBody();
                body.Position = pos;
        body.AddShape(new BoxShape(sx, sy, sz));
        body.DeactivationThreshold = DemoDeactivationThreshold;
        body.Tag = new RenderTag(BodyShape.Box, sx, sy, sz);
        return body;
    }

    public RigidBody AddSphere(JVector pos, float radius = 0.5f)
    {
        var body = World.CreateRigidBody();
        body.AddShape(new SphereShape(radius));
        body.Position = pos;
        body.DeactivationThreshold = DemoDeactivationThreshold;
        float d = radius * 2f;
        body.Tag = new RenderTag(BodyShape.Sphere, d, d, d);
        return body;
    }

    public void SetTheme(bool lightTheme)
    {
        _sceneBackground = lightTheme ? SceneBgLight : SceneBgDark;
        _panelBackground = lightTheme ? new Color(34, 48, 60, 255) : new Color(18, 20, 26, 255);
        _tabActive = lightTheme ? new Color(91, 168, 245, 255) : new Color(91, 168, 245, 255);
        _tabInactive = lightTheme ? new Color(78, 96, 114, 255) : new Color(50, 50, 50, 255);
        _hintColor = lightTheme ? new Color(214, 224, 235, 255) : new Color(180, 180, 180, 255);
        _ctrlColor = lightTheme ? new Color(176, 193, 209, 255) : new Color(130, 130, 130, 255);
    }

    // ─── Frame ───────────────────────────────────────────────────────────────

    public void UpdateFrame()
    {
        HandleInput();

        Viewport(0, 0, GetRenderWidth(), GetRenderHeight());

        unsafe
        {
            SetShaderValue(_shader,
                _shader.Locs[(int)ShaderLocationIndex.VectorView],
                _camera.Position, ShaderUniformDataType.Vec3);
        }

        UpdateCamera(ref _camera, CameraMode.Orbital);

        BeginDrawing();
        ClearBackground(_sceneBackground);

        BeginMode3D(_camera);
        DrawGround();
        DrawBodies();
        DrawExtras();
        EndMode3D();

        DrawUI();
        EndDrawing();

        _accumulatedTime += GetFrameTime();
        int steps = 0;

        while (_accumulatedTime >= FixedTimeStep && steps < MaxSimulationStepsPerFrame)
        {
            World.Step(FixedTimeStep, false);
            _accumulatedTime -= FixedTimeStep;
            steps++;
        }

        if (steps == MaxSimulationStepsPerFrame && _accumulatedTime > FixedTimeStep)
        {
            _accumulatedTime = FixedTimeStep;
        }
    }

    // ─── Input ───────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        if (IsKeyPressed(KeyboardKey.Right))
            LoadScene((_sceneIndex + 1) % _scenes.Count);
        else if (IsKeyPressed(KeyboardKey.Left))
            LoadScene((_sceneIndex + _scenes.Count - 1) % _scenes.Count);
        else if (IsKeyPressed(KeyboardKey.R))
            LoadScene(_sceneIndex);

        if (_hasPendingPointerTap)
        {
            _hasPendingPointerTap = false;
            HandleTabPress(_pendingPointerTap);
        }
    }

    private void HandleTabPress(Vector2 pointer)
    {
        const int tabH = 40;
        int sw = GetScreenWidth();
        int sh = GetScreenHeight();
        int tabY = sh - tabH;

        if (pointer.Y >= tabY && pointer.Y < sh)
        {
            int tabW = sw / _scenes.Count;
            int index = Math.Clamp((int)(pointer.X / tabW), 0, _scenes.Count - 1);
            if (index != _sceneIndex) LoadScene(index);
        }
    }

    public void QueuePointerTap(float x, float y)
    {
        _pendingPointerTap = new Vector2(x, y);
        _hasPendingPointerTap = true;
    }

    // ─── Drawing ─────────────────────────────────────────────────────────────

    private void DrawGround()
    {
        _modelGround.Transform = Matrix4x4.Identity;
        DrawModel(_modelGround, Vector3.Zero, 1f, new Color(200, 200, 200, 255));
    }

    private void DrawBodies()
    {
        foreach (var body in World.RigidBodies)
        {
            if (body.Tag is not RenderTag tag) continue;
            var m = BuildTransform(body, tag.Sx, tag.Sy, tag.Sz);
            switch (tag.Shape)
            {
                case BodyShape.Box:
                    _modelBox.Transform = m;
                    DrawModel(_modelBox, Vector3.Zero, 1f, Color.White);
                    break;
                case BodyShape.Sphere:
                    _modelSphere.Transform = m;
                    DrawModel(_modelSphere, Vector3.Zero, 1f, Color.White);
                    break;
            }
        }
    }

    private void DrawExtras()
    {
        // Soft Body: draw triangles
        if (Current is SceneSoftBall sb && sb.Sphere is { } sphere)
        {
            var col = new Color(60, 220, 80, 255);
            foreach (SoftBodyTriangle tri in sphere.Shapes)
            {
                Vector3 p1 = new(tri.Vertex1.Position.X, tri.Vertex1.Position.Y, tri.Vertex1.Position.Z);
                Vector3 p2 = new(tri.Vertex2.Position.X, tri.Vertex2.Position.Y, tri.Vertex2.Position.Z);
                Vector3 p3 = new(tri.Vertex3.Position.X, tri.Vertex3.Position.Y, tri.Vertex3.Position.Z);
                DrawLine3D(p1, p2, col);
                DrawLine3D(p2, p3, col);
                DrawLine3D(p3, p1, col);
            }
        }

        // Wrecking Ball: draw rope
        if (Current is SceneWreckingBall wb && wb.Ball is { } ball)
        {
            Vector3 a = new(wb.AnchorPos.X, wb.AnchorPos.Y, wb.AnchorPos.Z);
            Vector3 center = new(ball.Position.X, ball.Position.Y, ball.Position.Z);
            Vector3 toAnchor = Vector3.Normalize(a - center);
            Vector3 b = center + toAnchor * SceneWreckingBall.BallRadius;
            Vector3 delta = b - a;
            float length = delta.Length();

            if (length > 1e-4f)
            {
                Vector3 dir = delta / length;
                Vector3 axis = Vector3.Cross(Vector3.UnitY, dir);
                float dot = Math.Clamp(Vector3.Dot(Vector3.UnitY, dir), -1f, 1f);
                float angle = MathF.Acos(dot) * 180f / MathF.PI;

                if (axis.LengthSquared() < 1e-6f)
                {
                    axis = Vector3.UnitX;
                    if (dot > 0f) angle = 0f;
                }
                else
                {
                    axis = Vector3.Normalize(axis);
                }

                DrawModelEx(
                    _modelRope,
                    a,
                    axis,
                    angle,
                    new Vector3(0.05f, length, 0.05f),
                    Color.Yellow);
            }

            DrawSphere(a, 0.15f, Color.Gray);
        }

    }

    // ─── UI overlay ──────────────────────────────────────────────────────────

    private Font SelectFont(float size)
    {
        if (size >= UiTitleFontSize - 1f) return _fontTitle;
        if (size >= UiBodyFontSize - 1f) return _fontBody;
        return _fontSmall;
    }

    private static int GetAtlasFontSize(int uiSize)
    {
        return (int)MathF.Ceiling(uiSize * UiFontOversample);
    }

    private void DrawTextF(string text, float x, float y, float size, Color color)
    {
        Font font = SelectFont(size);
        float drawSize = MathF.Round(size);
        float spacing = MathF.Round(drawSize / 20f);
        Vector2 position = new(MathF.Round(x), MathF.Round(y));
        DrawTextEx(font, text, position, drawSize, spacing, color);
    }

    private float MeasureTextF(string text, float size)
    {
        Font font = SelectFont(size);
        float drawSize = MathF.Round(size);
        float spacing = MathF.Round(drawSize / 20f);
        return MeasureTextEx(font, text, drawSize, spacing).X;
    }

    private void DrawUI()
    {
        int sw = GetScreenWidth();
        int sh = GetScreenHeight();
        float titleSize = 26f;
        float hintSize = 16f;
        float ctrlSize = 15f;
        float tabLabelSize = 15f;
        int topBarHeight = 54;
        int tabH = 40;

        DrawRectangle(0, 0, sw, topBarHeight, _panelBackground);
        DrawTextF(Current.Name, 12, 7, titleSize, Color.White);
        DrawTextF(Current.Hint, 12, 34, hintSize, _hintColor);
        string ctrl = $"Arrow Keys to Switch Scene   R Reset   {GetFPS()} fps";
        DrawTextF(ctrl, sw - MeasureTextF(ctrl, ctrlSize) - 10, 10, ctrlSize, _ctrlColor);

        DrawRectangle(0, sh - tabH, sw, tabH, _panelBackground);
        int tabW = sw / _scenes.Count;
        for (int i = 0; i < _scenes.Count; i++)
        {
            int tx = i * tabW;
            DrawRectangle(tx + 1, sh - tabH + 2, tabW - 2, tabH - 4,
                i == _sceneIndex ? _tabActive : _tabInactive);
            string label = _scenes[i].Name;
            float lw = MeasureTextF(label, tabLabelSize);
            DrawTextF(label, tx + (tabW - lw) / 2f, sh - tabH + 13, tabLabelSize, Color.White);
        }
    }

    // ─── Math ────────────────────────────────────────────────────────────────

    static Matrix4x4 BuildTransform(RigidBody body, float sx, float sy, float sz)
    {
        JMatrix r = JMatrix.CreateFromQuaternion(body.Orientation);
        JVector p = body.Position;
        return new Matrix4x4(
            r.M11 * sx, r.M12 * sy, r.M13 * sz, p.X,
            r.M21 * sx, r.M22 * sy, r.M23 * sz, p.Y,
            r.M31 * sx, r.M32 * sy, r.M33 * sz, p.Z,
            0f, 0f, 0f, 1f);
    }
}

// ─── Application entry point ──────────────────────────────────────────────────

public partial class Application
{
    private static Playground _playground = null!;

    [JSExport] public static void UpdateFrame() => _playground.UpdateFrame();
    [JSExport] public static void Resize(int w, int h) => SetWindowSize(w, h);
    [JSExport] public static void SetTheme(bool lightTheme) => _playground.SetTheme(lightTheme);
    [JSExport] public static void PointerTap(float x, float y) => _playground.QueuePointerTap(x, y);
    [JSExport] public static void Shutdown() => _playground.Dispose();

    public static void Main() => _playground = new Playground();
}
