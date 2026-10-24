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

public class CustomSupportMapInstance<T> : InstancedDrawable where T : Shape, new()
{
    public CustomSupportMapInstance() : base(Tessellate())
    {
        Material = new Material
        {
            Tint = Vector3.Zero,
            Specular = new Vector3(0.1f, 0.1f, 0.1f),
            Shininess = 128f,
            Alpha = 1f,
            VertexColorWeight = 0.7f,
            TextureWeight = 0f
        };
    }

    private static Mesh Tessellate()
    {
        T shape = new();
        List<Vertex> verts = new();
        List<TriangleVertexIndex> inds = new();

        int idx = 0;
        foreach (var triangle in ShapeHelper.Tessellate(shape))
        {
            JVector normal = (triangle.V1 - triangle.V0) % (triangle.V2 - triangle.V0);
            JVector.NormalizeInPlace(ref normal);

            verts.Add(new Vertex(Conversion.FromJitter(triangle.V0), Conversion.FromJitter(normal)));
            verts.Add(new Vertex(Conversion.FromJitter(triangle.V1), Conversion.FromJitter(normal)));
            verts.Add(new Vertex(Conversion.FromJitter(triangle.V2), Conversion.FromJitter(normal)));

            inds.Add(new TriangleVertexIndex(idx + 0, idx + 1, idx + 2));
            idx += 3;
        }

        return new Mesh(verts.ToArray(), inds.ToArray());
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
        dir.X *= 0.8f;
        dir.Y *= 1.2f;
        dir.Z *= 0.4f;
        result = dir;
        JVector.NormalizeInPlace(ref result);
        result.X *= 0.8f;
        result.Y *= 1.2f;
        result.Z *= 0.4f;
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

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }
}

public class Icosahedron : RigidBodyShape
{
    private static readonly float GR = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;

    private static readonly JVector[] Vertices =
    [
        new(0, +1, +GR), new(0, -1, +GR), new(0, +1, -GR), new(0, -1, -GR),
        new(+1, +GR, 0), new(+1, -GR, 0), new(-1, +GR, 0), new(-1, -GR, 0),
        new(+GR, 0, +1), new(+GR, 0, -1), new(-GR, 0, +1), new(-GR, 0, -1)
    ];

    public Icosahedron()
    {
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        int largestIndex = 0;
        float maxDot = JVector.Dot(Vertices[0], direction);

        for (int i = 1; i < Vertices.Length; i++)
        {
            float dot = JVector.Dot(Vertices[i], direction);
            if (dot > maxDot)
            {
                maxDot = dot;
                largestIndex = i;
            }
        }

        result = Vertices[largestIndex] * 0.5f;
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }
}

public class Demo14 : IDemo
{
    public string Name => "Custom Shapes";
    public string Description => "Custom support-mapped shapes: ellipsoid, double-sphere, and icosahedron.";

    private Playground pg = null!;

    private List<RigidBody> demoBodies = null!;

    public void Build(Playground pg)
    {
        World world = pg.World;

        this.pg = pg;

        pg.AddFloor();

        demoBodies = new List<RigidBody>();

        for (int i = 0; i < 100; i++)
        {
            var ellipsoid = new EllipsoidShape();
            var doublesSphere = new DoubleSphereShape();
            var icosahedron = new Icosahedron();

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

    public void DrawUpdate()
    {
        var cesd = pg.GetDrawable<CustomSupportMapInstance<EllipsoidShape>>();
        var rbsd = pg.GetDrawable<CustomSupportMapInstance<DoubleSphereShape>>();
        var icsd = pg.GetDrawable<CustomSupportMapInstance<Icosahedron>>();

        foreach (var body in demoBodies)
        {
            var color = ColorGenerator.GetColor(body.GetHashCode());
            if (!body.IsActive) color += new Vector3(0.2f, 0.2f, 0.2f);

            switch (body.Shapes[0])
            {
                case EllipsoidShape:
                    cesd.Push(Conversion.FromJitter(body), color);
                    break;
                case DoubleSphereShape:
                    rbsd.Push(Conversion.FromJitter(body), color);
                    break;
                case Icosahedron:
                    icsd.Push(Conversion.FromJitter(body), color);
                    break;
            }
        }
    }
}
