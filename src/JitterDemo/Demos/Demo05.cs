using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Dust : TriangleMeshDrawable
{
    public Dust() : base("level.obj", 0.8f)
    {
        Material = new Material
        {
            Tint = Vector3.Zero,
            Specular = new Vector3(0.1f, 0.1f, 0.1f),
            Shininess = 128f,
            Alpha = 1f,
            VertexColorWeight = 1.2f,
            TextureWeight = 0.5f
        };
    }
}

public class Demo05 : IDemo, IDrawUpdate
{
    public string Name => "Level Geometry";
    public string Description => "Triangle-mesh level loaded from an OBJ file with a player character.";
    public string Controls => "Arrow Keys - Move player\nLeft Ctrl - Jump";

    private Dust tm = null!;

    private Player player = null!;

    private RigidBody level = null!;

    public IEnumerable<RigidBodyShape> CreateShapes()
    {
        var indices = MemoryMarshal.Cast<TriangleVertexIndex, int>(tm.Mesh.Indices);
        var vertices = tm.Mesh.Vertices.Select(vertex => Conversion.ToJitterVector(vertex.Position)).ToArray();

        var jtm = new Jitter2.Collision.Shapes.TriangleMesh(vertices, indices);
        return TriangleShape.CreateAllShapes(jtm);
    }

    public void Build(Playground pg, World world)
    {
        tm = pg.GetDrawable<Dust>();

        pg.AddFloor();

        level = world.CreateRigidBody();
        level.AddShapes(CreateShapes(), MassInertiaUpdateMode.Preserve);
        level.Tag = new RigidBodyTag(doNotDraw:true);
        level.MotionType = MotionType.Static;

        Common.BuildJenga(new JVector(-2, 6, 24), 20, rigidBody => rigidBody.Friction = 0.3f);

        player = new Player(world, new JVector(-6, 7, 32));
    }

    public void DrawUpdate()
    {
        tm.Push(Conversion.FromJitter(level), new Vector3(0.35f, 0.35f, 0.35f));

        Keyboard kb = Keyboard.Instance;

        if (kb.IsKeyDown(Keyboard.Key.Left)) player.SetAngularInput(-1.0f);
        else if (kb.IsKeyDown(Keyboard.Key.Right)) player.SetAngularInput(1.0f);
        else player.SetAngularInput(0.0f);

        if (kb.IsKeyDown(Keyboard.Key.Up)) player.SetLinearInput(-JVector.UnitZ);
        else if (kb.IsKeyDown(Keyboard.Key.Down)) player.SetLinearInput(JVector.UnitZ);
        else player.SetLinearInput(JVector.Zero);

        if (kb.IsKeyDown(Keyboard.Key.LeftControl)) player.Jump();
    }
}
