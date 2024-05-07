using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class CustomSupportMapInstance<T> : CSMInstance where T : Shape, new()
{
    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        T es = new();

        List<Vertex> verts = new();
        List<TriangleVertexIndex> inds = new();

        int idx = 0;

        foreach (var triangle in ShapeHelper.MakeHull(es))
        {
            JVector normal = -(triangle.V2 - triangle.V0) % (triangle.V1 - triangle.V0);
            normal.Normalize();

            verts.Add(new Vertex(Conversion.FromJitter(triangle.V1), Conversion.FromJitter(normal)));
            verts.Add(new Vertex(Conversion.FromJitter(triangle.V0), Conversion.FromJitter(normal)));
            verts.Add(new Vertex(Conversion.FromJitter(triangle.V2), Conversion.FromJitter(normal)));

            inds.Add(new TriangleVertexIndex(idx + 0, idx + 1, idx + 2));
            idx += 3;
        }

        return (verts.ToArray(), inds.ToArray());
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.ColorMixing.Set(0.7f, 0, 0);
        base.LightPass(shader);
    }
}

public class EllipsoidShape : Shape
{
    public EllipsoidShape()
    {
        UpdateShape();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        JVector dir = direction;
        dir.X *= 0.8f;
        dir.Y *= 1.2f;
        dir.Z *= 0.4f;
        result = dir;
        result.Normalize();
        result.X *= 0.8f;
        result.Y *= 1.2f;
        result.Z *= 0.4f;
    }
}

public class DoubleSphereShape : Shape
{
    public DoubleSphereShape()
    {
        UpdateShape();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        JVector.Normalize(direction, out JVector ndir);

        JVector sphere1 = ndir * 1.0f + JVector.UnitY * 1.1f;
        JVector sphere2 = ndir * 1.5f - JVector.UnitY * 0.5f;

        if (JVector.Dot(sphere1, ndir) > JVector.Dot(sphere2, ndir))
        {
            result = sphere1 * 0.5f;
        }
        else
        {
            result = sphere2 * 0.5f;
        }
    }
}

public class Icosahedron : Shape
{
    public Icosahedron()
    {
        UpdateShape();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        float gr = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;

        Span<JVector> vertices = stackalloc JVector[12];

        vertices[0] = new JVector(0, +1, +gr);
        vertices[1] = new JVector(0, -1, +gr);
        vertices[2] = new JVector(0, +1, -gr);
        vertices[3] = new JVector(0, -1, -gr);
        vertices[4] = new JVector(+1, +gr, 0);
        vertices[5] = new JVector(+1, -gr, 0);
        vertices[6] = new JVector(-1, +gr, 0);
        vertices[7] = new JVector(-1, -gr, 0);
        vertices[8] = new JVector(+gr, 0, +1);
        vertices[9] = new JVector(+gr, 0, -1);
        vertices[10] = new JVector(-gr, 0, +1);
        vertices[11] = new JVector(-gr, 0, -1);

        int largestIndex = 0;

        for (int i = 1; i < 12; i++)
        {
            if (JVector.Dot(vertices[i], direction) > JVector.Dot(vertices[largestIndex], direction))
            {
                largestIndex = i;
            }
        }

        result = vertices[largestIndex] * 0.5f;
    }
}

public class Demo14 : IDemo
{
    public string Name => "Custom Shapes";

    private Playground pg = null!;

    private EllipsoidShape ellipsoid = null!;
    private DoubleSphereShape doublesSphere = null!;
    private Icosahedron icosahedron = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        ellipsoid = new EllipsoidShape();
        doublesSphere = new DoubleSphereShape();
        icosahedron = new Icosahedron();

        var body1 = world.CreateRigidBody();
        body1.AddShape(ellipsoid);
        body1.Position = new JVector(-3, 3, 0);

        var body2 = world.CreateRigidBody();
        body2.AddShape(doublesSphere);
        body2.Position = new JVector(0, 3, 0);

        var body3 = world.CreateRigidBody();
        body3.AddShape(icosahedron);
        body3.Position = new JVector(3, 3, 0);
    }

    public void Draw()
    {
        var cesd = pg.CSMRenderer.GetInstance<CustomSupportMapInstance<EllipsoidShape>>();
        cesd.PushMatrix(Conversion.FromJitter(ellipsoid.RigidBody!), Vector3.UnitX);

        var rbsd = pg.CSMRenderer.GetInstance<CustomSupportMapInstance<DoubleSphereShape>>();
        rbsd.PushMatrix(Conversion.FromJitter(doublesSphere.RigidBody!), Vector3.UnitY);

        var icsd = pg.CSMRenderer.GetInstance<CustomSupportMapInstance<Icosahedron>>();
        icsd.PushMatrix(Conversion.FromJitter(icosahedron.RigidBody!), Vector3.UnitZ);
    }
}