using System.Collections.Generic;
using System.Linq;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;
using TriangleMesh = JitterDemo.Renderer.TriangleMesh;

namespace JitterDemo;

public class Teapot : TriangleMesh
{
    public Teapot() : base("teapot.obj.zip", 1.0f) { }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();
        base.LightPass(shader);
    }
}

public class Demo24 : IDemo
{
    public string Name => "Convex PointCloudShape";

    private TriangleMesh teapot = null!;
    private List<RigidBody> teapotBodies = null!;
    private Matrix4 shift;

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        teapotBodies = new List<RigidBody>();

        teapot = RenderWindow.Instance.CSMRenderer.GetInstance<Teapot>();

        var vertices = teapot.Mesh.Vertices.Select(v
            => new JVector(v.Position.X, v.Position.Y, v.Position.Z)).Distinct().ToList();

        // Find a few points on the convex hull of the teapot.
        var reducedVertices = ShapeHelper.SampleHull(vertices, subdivisions: 3);

        // Use these points to create a PointCloudShape. One could also use all vertices
        // of the teapot, but this would be slower since it also includes vertices that are
        // inside the convex hull of the teapot.
        PointCloudShape pcs = new PointCloudShape(reducedVertices);

        // Jitter requires the center of mass to be at the origin!

        // get the center of mass and shift the convex hull, such that the new
        // center of mass is at the origin.
        pcs.GetCenter(out JVector ctr);
        pcs.Shift = -ctr;
        // pcs.GetCenter(out JVector ctr); <- this would now return (0,0,0)

        // also shift the visual representation of the teapot
        shift = MatrixHelper.CreateTranslation(-(float)ctr.X, -(float)ctr.Y, -(float)ctr.Z);

        for (int i = 0; i < 16; i++)
        {
            RigidBody body = world.CreateRigidBody();
            body.Position = new JVector(0, 10 + i * 3, 0);
            body.AddShape(pcs.Clone());
            teapotBodies.Add(body);
        }
    }

    public void Draw()
    {
        foreach (var body in teapotBodies)
        {
            var color = ColorGenerator.GetColor(body.GetHashCode());
            if (!body.IsActive) color += new Vector3(0.2f, 0.2f, 0.2f);
            teapot.PushMatrix(Conversion.FromJitter(body) * shift, color);
        }
    }
}