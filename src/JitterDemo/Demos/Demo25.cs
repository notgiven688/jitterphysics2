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

// In this example, we create a heightmap and use it for collision detection.
// Note that only the rendering code requires memory, the heightmap itself may be
// generated procedurally and be of infinite size.
// To save resources when performing raycasts, we use a ray with finite size (segment).

public static class Heightmap
{
    public const int Width = 100;
    public const int Height = 100;
    public const float Amplitude = 2f;

    public static float GetHeight(int x, int y)
    {
        return MathF.Sin(x * 0.1f) * MathF.Cos(y * 0.1f) * Amplitude;
    }

    // Only for rendering...
    public static JVector GetNormal(int x, int z)
    {
        float dx = (GetHeight(x + 1, z) - GetHeight(x - 1, z)) * 0.5f;
        float dz = (GetHeight(x, z + 1) - GetHeight(x, z - 1)) * 0.5f;

        var normal = new JVector(-dx, 1f, -dz);
        normal.Normalize();

        return normal;
    }

    public static JBBox GetBoundingBox()
    {
        JVector min = new JVector(0, -Amplitude, 0);
        JVector max = new JVector(Width, Amplitude, Height);
        return new JBBox(min, max);
    }
}

public class HeightmapTester(JBBox box) : IDynamicTreeProxy, IRayCastable
{
    public int SetIndex { get; set; } = -1;
    public int NodePtr { get; set; }

    public JVector Velocity => JVector.Zero;
    public JBBox WorldBoundingBox { get; } = box;

    private void RayCastTriangle(in JVector origin, in JVector direction,
        in JVector a, in JVector b, in JVector c, out JVector normal, out float lambda)
    {
        JVector u = b - a;
        JVector v = c - a;

        normal = v % u;
        float it = 1.0f / normal.LengthSquared();
        float denominator = JVector.Dot(direction, normal);

        if (Math.Abs(denominator) < 1e-06f)
        {
            // triangle and ray are parallel
            lambda = float.MaxValue;
            normal = JVector.Zero;
            return;
        }

        lambda = JVector.Dot(a - origin, normal);
        if (lambda > 0.0f)
        {
            // ray is pointing away from the triangle
            lambda = float.MaxValue;
            normal = JVector.Zero;
            return;
        }

        lambda /= denominator;

        // point where the ray intersects the plane of the triangle.
        JVector hitPoint = origin + lambda * direction;
        JVector at = a - hitPoint;

        JVector.Cross(u, at, out JVector tmp);
        float gamma = JVector.Dot(tmp, normal) * it;
        JVector.Cross(at, v, out tmp);
        float beta = JVector.Dot(tmp, normal) * it;
        float alpha = 1.0f - gamma - beta;

        if (!(alpha > 0 && beta > 0 && gamma > 0))
        {
            // point is outside the triangle
            normal = JVector.Zero;
            lambda = float.MaxValue;
            return;
        }

        normal *= MathF.Sqrt(it);
    }

    public bool RayCast(in JVector origin, in JVector direction, out JVector normal, out float lambda)
    {
        const float maxDistance = 100.0f;

        float dirX = direction.X;
        float dirZ = direction.Z;

        float len2 = dirX * dirX + dirZ * dirZ;
        float ilen = 1.0f / MathF.Sqrt(len2);

        dirX *= ilen;
        dirZ *= ilen;

        int x = (int)Math.Floor(origin.X);
        int z = (int)Math.Floor(origin.Z);

        int stepX = dirX > 0 ? 1 : -1;
        int stepZ = dirZ > 0 ? 1 : -1;

        float nextX = dirX > 0 ? (x + 1) - origin.X : origin.X - x;
        float nextZ = dirZ > 0 ? (z + 1) - origin.Z : origin.Z - z;

        float tMaxX = dirX != 0 ? nextX / Math.Abs(dirX) : float.PositiveInfinity;
        float tMaxZ = dirZ != 0 ? nextZ / Math.Abs(dirZ) : float.PositiveInfinity;

        float tDeltaX = direction.X != 0 ? 1f / Math.Abs(dirX) : float.PositiveInfinity;
        float tDeltaZ = direction.Z != 0 ? 1f / Math.Abs(dirZ) : float.PositiveInfinity;

        float t = 0f;

        while (t <= maxDistance)
        {
            // check this quad!

            var a = new JVector(x + 0, Heightmap.GetHeight(x + 0, z + 0), z + 0);
            var b = new JVector(x + 1, Heightmap.GetHeight(x + 1, z + 0), z + 0);
            var c = new JVector(x + 1, Heightmap.GetHeight(x + 1, z + 1), z + 1);
            var d = new JVector(x + 0, Heightmap.GetHeight(x + 0, z + 1), z + 1);

            RayCastTriangle(origin, direction, a, b, c, out JVector normal0, out float lambda0);
            RayCastTriangle(origin, direction, a, c, d, out JVector normal1, out float lambda1);

            if (lambda0 < float.MaxValue || lambda1 < float.MaxValue)
            {
                if (lambda0 <= lambda1)
                {
                    normal = normal0;
                    lambda = lambda0;
                }
                else
                {
                    normal = normal1;
                    lambda = lambda1;
                }

                return true;
            }

            if (tMaxX < tMaxZ)
            {
                x += stepX;
                t = tMaxX;
                tMaxX += tDeltaX;
            }
            else
            {
                z += stepZ;
                t = tMaxZ;
                tMaxZ += tDeltaZ;
            }
        }

        normal = JVector.Zero; lambda = 0.0f;
        return false;
    }
}

public class HeightmapDetection : IBroadPhaseFilter
{
    private readonly World world;
    private readonly HeightmapTester shape;
    private readonly ulong minIndex;

    public HeightmapDetection(World world, HeightmapTester shape)
    {
        this.shape = shape;
        this.world = world;

        (minIndex, _) = World.RequestId(Heightmap.Width * Heightmap.Height * 2);
    }

    public bool Filter(IDynamicTreeProxy shapeA, IDynamicTreeProxy shapeB)
    {
        if (shapeA != shape && shapeB != shape) return true;

        var collider = shapeA == shape ? shapeB : shapeA;

        if (collider is not RigidBodyShape rbs || rbs.RigidBody.Data.IsStaticOrInactive) return false;

        ref RigidBodyData body = ref rbs.RigidBody!.Data;

        var min = collider.WorldBoundingBox.Min;
        var max = collider.WorldBoundingBox.Max;

        int minX = Math.Max(0, (int)min.X);
        int minZ = Math.Max(0, (int)min.Z);
        int maxX = Math.Min(Heightmap.Width, (int)max.X + 1);
        int maxZ = Math.Min(Heightmap.Height, (int)max.Z + 1);

        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                // First triangle of the quad

                ulong index = 2 * (ulong)(x * Heightmap.Width + z);

                CollisionTriangle triangle;

                triangle.A = new JVector(x + 0, Heightmap.GetHeight(x + 0, z + 0), z + 0);
                triangle.B = new JVector(x + 1, Heightmap.GetHeight(x + 1, z + 0), z + 0);
                triangle.C = new JVector(x + 1, Heightmap.GetHeight(x + 1, z + 1), z + 1);

                JVector normal = JVector.Normalize((triangle.C - triangle.A) % (triangle.B - triangle.A));

                bool hit = NarrowPhase.MPREPA(triangle, rbs, body.Orientation, body.Position,
                    out JVector pointA, out JVector pointB, out _, out float penetration);

                if (hit)
                {
                    world.RegisterContact(rbs.ShapeId, minIndex + index, world.NullBody, rbs.RigidBody,
                        pointA, pointB, normal, penetration);
                }

                // Second triangle of the quad

                index += 1;
                triangle.A = new JVector(x + 0, Heightmap.GetHeight(x + 0, z + 0), z + 0);
                triangle.B = new JVector(x + 1, Heightmap.GetHeight(x + 1, z + 1), z + 1);
                triangle.C = new JVector(x + 0, Heightmap.GetHeight(x + 0, z + 1), z + 1);

                normal = JVector.Normalize((triangle.C - triangle.A) % (triangle.B - triangle.A));

                hit = NarrowPhase.MPREPA(triangle, rbs, body.Orientation, body.Position,
                    out pointA, out pointB, out _, out penetration);

                if (hit)
                {
                    world.RegisterContact(rbs.ShapeId, minIndex + index, world.NullBody, rbs.RigidBody,
                        pointA, pointB, normal, penetration);
                }
            }
        }

        return false;
    }
}

public class Demo25 : IDemo
{
    public string Name => "Custom Collision (Heightmap)";

    private Cloth terrainRenderer = null!;

    // Only for rendering...
    TriangleVertexIndex[] GetIndices(int width, int height)
    {
        var indices = new List<TriangleVertexIndex>();

        for (int j = 0; j < height - 1; j++)
        {
            for (int i = 0; i < width - 1; i++)
            {
                int a = j * width + i;
                int b = j * width + (i + 1);
                int c = (j + 1) * width + i;
                int d = (j + 1) * width + (i + 1);

                indices.Add(new TriangleVertexIndex(a, b, c)); // Triangle 1
                indices.Add(new TriangleVertexIndex(b, d, c)); // Triangle 2
            }
        }
        return indices.ToArray();
    }

    // Only for rendering...
    void FillVertices(Vertex[] vertices, int width, int height)
    {
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = j * width + i;

                vertices[index].Position = new Vector3(i, Heightmap.GetHeight(i, j), j);
                vertices[index].Texture = new Vector2(i * 0.5f, j * 0.5f);
                vertices[index].Normal = Conversion.FromJitter(Heightmap.GetNormal(i, j));
            }
        }
    }

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene(false);

        var tester = new HeightmapTester(Heightmap.GetBoundingBox());

        world.BroadPhaseFilter = new HeightmapDetection(world, tester);
        world.DynamicTree.AddProxy(tester, false);

        // Only for rendering...
        terrainRenderer = RenderWindow.Instance.CSMRenderer.GetInstance<Cloth>();
        terrainRenderer.SetIndices(GetIndices(Heightmap.Width, Heightmap.Height));
        FillVertices(terrainRenderer.Vertices, Heightmap.Width, Heightmap.Height);
        terrainRenderer.VerticesChanged();
    }

    public void Draw()
    {
        terrainRenderer.PushMatrix(Matrix4.Identity);
    }
}