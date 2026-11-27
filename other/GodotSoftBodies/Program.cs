using System;
using System.Collections.Generic;
using Godot;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

public partial class Program : Node3D
{
    private const int LayerCount = 5;
    private const int PieceSpacing = 30;

    private static readonly (Color Color, Vector2I[] Blocks)[] Pieces =
    [
        (new(0, 0.94f, 0.94f), [new(0, 0), new(0, 1), new(0, 2), new(0, 3)]),
        (new(0, 0, 0.94f), [new(0, 0), new(0, 1), new(1, 1), new(2, 1)]),
        (new(0.94f, 0, 0), [new(0, 0), new(1, 0), new(1, 1), new(2, 1)]),
        (new(0.63f, 0, 0.94f), [new(0, 0), new(1, 0), new(2, 0), new(1, 1)]),
        (new(0.94f, 0.94f, 0), [new(0, 0), new(0, 1), new(1, 0), new(1, 1)])
    ];

    private readonly ImmediateMesh mesh = new();
    private readonly List<(CubedSoftBody Body, Material Material)> bodies = [];
    private readonly Material[] materials = new Material[Pieces.Length];
    private World world = null!;

    public override void _Ready()
    {
        world = new World { SubstepCount = 4, SolverIterations = (4, 1) };
        world.DynamicTree.Filter = DynamicTreeCollisionFilter.Filter;
        world.BroadPhaseFilter = new BroadPhaseCollisionFilter(world);

        var baseMaterial = GD.Load<StandardMaterial3D>("res://box.material");
        for (int i = 0; i < Pieces.Length; i++)
        {
            var material = (StandardMaterial3D)baseMaterial.Duplicate();
            material.AlbedoColor = Pieces[i].Color;
            materials[i] = material;
        }

        var reset = new Button { Text = "Reset scene", Position = new Vector2(4, 4) };
        reset.Pressed += ResetScene;
        AddChild(reset);
        AddChild(new MeshInstance3D { Mesh = mesh });
        ResetScene();
    }

    private void ResetScene()
    {
        ClearBodies();
        world.Clear();

        RigidBody floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(40));
        floor.Position = new JVector(0, -20, 0);
        floor.MotionType = MotionType.Static;

        for (int layer = 0; layer < LayerCount; layer++)
        for (int piece = 0; piece < Pieces.Length; piece++)
        {
            int height = (layer * Pieces.Length + piece) * PieceSpacing;
            AddPiece(Pieces[piece].Blocks, height, materials[piece]);
        }
    }

    private void AddPiece(ReadOnlySpan<Vector2I> blocks, int height, Material material)
    {
        Span<Vector3I> cells = stackalloc Vector3I[blocks.Length * 8];
        int next = 0;
        foreach (Vector2I block in blocks)
        {
            for (int y = 1; y >= -1; y -= 2)
            for (int x = 1; x >= -1; x -= 2)
            for (int z = 1; z >= -1; z -= 2)
                cells[next++] = new Vector3I(block.X * 4 + x, height + block.Y * 4 + y, z);
        }

        bodies.Add((new CubedSoftBody(world, cells), material));
    }

    public override void _PhysicsProcess(double delta) => world.Step((float)delta, true);

    public override void _Process(double delta)
    {
        mesh.ClearSurfaces();
        foreach (var (body, material) in bodies)
        {
            mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles, material);
            ReadOnlySpan<CubedSoftBody.SurfaceVertex> surface = body.Surface;
            for (int i = 0; i < surface.Length; i += 3)
            {
                Vector3 a = ToGodot(body.Vertices[surface[i].Index].Position);
                Vector3 b = ToGodot(body.Vertices[surface[i + 1].Index].Position);
                Vector3 c = ToGodot(body.Vertices[surface[i + 2].Index].Position);

                // Godot treats clockwise triangles as front-facing.
                mesh.SurfaceSetNormal((c - a).Cross(b - a).Normalized());
                mesh.SurfaceSetUV(surface[i].Uv);
                mesh.SurfaceAddVertex(a);
                mesh.SurfaceSetUV(surface[i + 1].Uv);
                mesh.SurfaceAddVertex(b);
                mesh.SurfaceSetUV(surface[i + 2].Uv);
                mesh.SurfaceAddVertex(c);
            }
            mesh.SurfaceEnd();
        }
    }

    private void ClearBodies()
    {
        foreach (var (body, _) in bodies) body.Destroy();
        bodies.Clear();
        mesh.ClearSurfaces();
    }

    public override void _ExitTree()
    {
        ClearBodies();
        world.Dispose();
    }

    private static Vector3 ToGodot(in JVector value) => new(value.X, value.Y, value.Z);
}
