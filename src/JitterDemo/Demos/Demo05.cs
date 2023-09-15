using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

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

    public List<Shape> CreateShapes()
    {
        var indices = tm.mesh.Indices;
        var vertices = tm.mesh.Vertices;

        List<Shape> shapesToAdd = new();
        List<JTriangle> triangles = new();

        foreach (var tvi in indices)
        {
            JVector v1 = Conversion.ToJitterVector(vertices[tvi.T1].Position);
            JVector v2 = Conversion.ToJitterVector(vertices[tvi.T2].Position);
            JVector v3 = Conversion.ToJitterVector(vertices[tvi.T3].Position);

            triangles.Add(new JTriangle(v1, v2, v3));
        }

        var jtm = new Jitter2.Collision.TriangleMesh(triangles);

        for (int i = 0; i < jtm.Indices.Length; i++)
        {
            TriangleShape ts = new TriangleShape(jtm, i);
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

        RigidBody body = world.CreateRigidBody();
        body.AddShape(CreateShapes());
        body.Position = new JVector(0, 0, 0);
        body.IsStatic = true;

        Common.BuildJenga(new JVector(-2, 6, 24), 20, rigidBody => rigidBody.Friction = 0.3f);
    }

    public void Draw()
    {
        tm.PushMatrix(Matrix4.Identity, new Vector3(0.35f, 0.35f, 0.35f));
    }
}