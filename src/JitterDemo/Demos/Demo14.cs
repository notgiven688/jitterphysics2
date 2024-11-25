using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
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

public class EllipsoidShape : RigidBodyShape
{
    public EllipsoidShape()
    {
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        JVector dir = direction;
        dir.X *= 0.8d;
        dir.Y *= 1.2d;
        dir.Z *= 0.4d;
        result = dir;
        result.Normalize();
        result.X *= 0.8d;
        result.Y *= 1.2d;
        result.Z *= 0.4d;
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }
}

public class DoubleSphereShape : RigidBodyShape
{
    public DoubleSphereShape()
    {
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        JVector.Normalize(direction, out JVector ndir);

        JVector sphere1 = ndir * 1.0d + JVector.UnitY * 1.1d;
        JVector sphere2 = ndir * 1.5d - JVector.UnitY * 0.5d;

        if (JVector.Dot(sphere1, ndir) > JVector.Dot(sphere2, ndir))
        {
            result = sphere1 * 0.5d;
        }
        else
        {
            result = sphere2 * 0.5d;
        }
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }
}

public class Icosahedron : RigidBodyShape
{
    public Icosahedron()
    {
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        double gr = (1.0d + Math.Sqrt(5.0d)) / 2.0d;

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

        result = vertices[largestIndex] * 0.5d;
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }
}

public class Demo14 : IDemo
{
    public string Name => "Custom Shapes";

    private Playground pg = null!;

    private EllipsoidShape ellipsoid = null!;
    private DoubleSphereShape doublesSphere = null!;
    private Icosahedron icosahedron = null!;

    private List<RigidBody> demoBodies = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        demoBodies = new List<RigidBody>();

        for (int i = 0; i < 100; i++)
        {
            ellipsoid = new EllipsoidShape();
            doublesSphere = new DoubleSphereShape();
            icosahedron = new Icosahedron();

            var body1 = world.CreateRigidBody();
            body1.AddShape(ellipsoid);
            body1.Position = new JVector(-3, 3 + i * 5, 0);
            demoBodies.Add(body1);

            var body2 = world.CreateRigidBody();
            body2.AddShape(doublesSphere);
            body2.Position = new JVector(0, 3 + i * 5, 0);
            demoBodies.Add(body2);

            var body3 = world.CreateRigidBody();
            body3.AddShape(icosahedron);
            body3.Position = new JVector(3, 3 + i * 5, 0);
            demoBodies.Add(body3);
        }
    }

    public void Draw()
    {
        var cesd = pg.CSMRenderer.GetInstance<CustomSupportMapInstance<EllipsoidShape>>();
        var rbsd = pg.CSMRenderer.GetInstance<CustomSupportMapInstance<DoubleSphereShape>>();
        var icsd = pg.CSMRenderer.GetInstance<CustomSupportMapInstance<Icosahedron>>();

        foreach (var body in demoBodies)
        {
            var color = ColorGenerator.GetColor(body.GetHashCode());
            if (!body.IsActive) color += new Vector3(0.2f, 0.2f, 0.2f);

            switch (body.Shapes[0])
            {
                case EllipsoidShape:
                    cesd.PushMatrix(Conversion.FromJitter(body), color);
                    break;
                case DoubleSphereShape:
                    rbsd.PushMatrix(Conversion.FromJitter(body), color);
                    break;
                case Icosahedron:
                    icsd.PushMatrix(Conversion.FromJitter(body), color);
                    break;
            }
        }
    }
}