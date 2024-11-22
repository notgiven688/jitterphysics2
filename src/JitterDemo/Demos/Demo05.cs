using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;
using TriangleMesh = JitterDemo.Renderer.TriangleMesh;

namespace JitterDemo;

public class Dust : TriangleMesh
{
    public Dust() : base("level.obj", 0.8f)
    {
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();
        shader.MaterialProperties.ColorMixing.Set(1.2f, 0, 0.5f);
        base.LightPass(shader);
    }
}

public class Demo05 : IDemo
{
    public string Name => "Level Geometry";

    private TriangleMesh tm = null!;

    private Player player = null!;

    private RigidBody level = null!;

    private bool debugDraw = false;

    public List<RigidBodyShape> CreateShapes()
    {
        var indices = tm.mesh.Indices;
        var vertices = tm.mesh.Vertices;

        List<RigidBodyShape> shapesToAdd = new();
        List<JTriangle> triangles = new();

        foreach (var tvi in indices)
        {
            JVector v1 = Conversion.ToJitterVector(vertices[tvi.T1].Position);
            JVector v2 = Conversion.ToJitterVector(vertices[tvi.T2].Position);
            JVector v3 = Conversion.ToJitterVector(vertices[tvi.T3].Position);

            triangles.Add(new JTriangle(v1, v2, v3));
        }

        var jtm = new Jitter2.Collision.Shapes.TriangleMesh(triangles);

        for (int i = 0; i < jtm.Indices.Length; i++)
        {
            FatTriangleShape ts = new FatTriangleShape(jtm, i);
            shapesToAdd.Add(ts);
        }

        return shapesToAdd;
    }

    public void Build()
    {
        tm = RenderWindow.Instance.CSMRenderer.GetInstance<Dust>();

        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        level = world.CreateRigidBody();
        level.AddShape(CreateShapes(), false);
        level.Position = new JVector(0, 0, 0);
        level.IsStatic = true;

        Common.BuildJenga(new JVector(-2, 6, 24), 20, rigidBody => rigidBody.Friction = 0.3f);

        player = new Player(world, new JVector(-6, 7, 32));
    }

    public void Draw()
    {
        tm.PushMatrix(Conversion.FromJitter(level), new Vector3(0.35f, 0.35f, 0.35f));

        if (debugDraw)
        {
            Playground pg = (Playground)RenderWindow.Instance;

            foreach (var triangle in tm.mesh.Indices)
            {
                var a = tm.mesh.Vertices[triangle.T1].Position;
                var b = tm.mesh.Vertices[triangle.T2].Position;
                var c = tm.mesh.Vertices[triangle.T3].Position;

                pg.DebugRenderer.PushLine(DebugRenderer.Color.Green, a, b);
                pg.DebugRenderer.PushLine(DebugRenderer.Color.Green, b, c);
                pg.DebugRenderer.PushLine(DebugRenderer.Color.Green, c, a);
            }
        }

        Keyboard kb = Keyboard.Instance;

        if (kb.KeyPressBegin(Keyboard.Key.O)) debugDraw = !debugDraw;

        if (kb.IsKeyDown(Keyboard.Key.Left)) player.SetAngularInput(-1.0f);
        else if (kb.IsKeyDown(Keyboard.Key.Right)) player.SetAngularInput(1.0f);
        else player.SetAngularInput(0.0f);

        if (kb.IsKeyDown(Keyboard.Key.Up)) player.SetLinearInput(-JVector.UnitZ);
        else if (kb.IsKeyDown(Keyboard.Key.Down)) player.SetLinearInput(JVector.UnitZ);
        else player.SetLinearInput(JVector.Zero);

        if (kb.IsKeyDown(Keyboard.Key.LeftControl)) player.Jump();
    }
}