using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;
using TriangleMesh = JitterDemo.Renderer.TriangleMesh;

namespace JitterDemo;

public class Dragon : TriangleMesh
{
    public Dragon() : base("dragon.obj.zip", 5)
    {
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();
        shader.MaterialProperties.ColorMixing.Set(1.2f, 0, 0.5f);
        base.LightPass(shader);
    }
}

public struct CollisionTriangle : ISupportMappable
{
    public JVector A, B, C;

    public void SupportMap(in JVector direction, out JVector result)
    {
        double min = JVector.Dot(A, direction);
        double dot = JVector.Dot(B, direction);

        result = A;
        if (dot > min)
        {
            min = dot;
            result = B;
        }

        dot = JVector.Dot(C, direction);
        if (dot > min)
        {
            result = C;
        }
    }

    public void GetCenter(out JVector point)
    {
        point = (1.0f / 3.0f) * (A + B + C);
    }
}

public class CustomCollisionDetection : IBroadPhaseFilter
{
    private readonly World world;
    private readonly Tester shape;
    private readonly Octree octree;
    private readonly ulong minIndex;

    public CustomCollisionDetection(World world, Tester shape, Octree octree)
    {
        this.shape = shape;
        this.octree = octree;
        this.world = world;

        (minIndex, _) = World.RequestId(octree.Indices.Length);
    }

    [ThreadStatic] private static Stack<uint>? candidates;

    public bool Filter(IDynamicTreeProxy shapeA, IDynamicTreeProxy shapeB)
    {
        if (shapeA != shape && shapeB != shape) return true;

        var collider = shapeA == shape ? shapeB : shapeA;

        if (collider is not RigidBodyShape rbs || rbs.RigidBody.MotionType != MotionType.Dynamic
                                               || !rbs.RigidBody.IsActive) return false;

        candidates ??= new Stack<uint>();
        CollisionTriangle ts = new();

        octree.Query(candidates, collider.WorldBoundingBox);

        while (candidates.Count > 0)
        {
            uint index = candidates.Pop();
            ts.A = octree.Vertices[octree.Indices[index].IndexA];
            ts.B = octree.Vertices[octree.Indices[index].IndexB];
            ts.C = octree.Vertices[octree.Indices[index].IndexC];

            bool hit = NarrowPhase.MprEpa(ts, rbs, rbs.RigidBody!.Orientation, rbs.RigidBody!.Position,
                out JVector pointA, out JVector pointB, out _, out double penetration);

            if (hit)
            {
                JVector normal = JVector.Normalize(JVector.Cross(ts.B - ts.A, ts.C - ts.A));

                if (world.EnableAuxiliaryContactPoints)
                {
                    Unsafe.SkipInit(out CollisionManifold manifold);

                    // Build the whole contact manifold if enabled.
                    manifold.BuildManifold(ts, rbs, JQuaternion.Identity, rbs.RigidBody!.Orientation,
                        JVector.Zero, rbs.RigidBody!.Position, pointA, pointB, normal);

                    world.RegisterContact(rbs.ShapeId, minIndex + index, world.NullBody, rbs.RigidBody, normal, ref manifold);
                }
                else
                {
                    world.RegisterContact(rbs.ShapeId, minIndex + index, world.NullBody, rbs.RigidBody, pointB, pointB, normal);
                }
            }
        }

        return false;
    }
}

public class Tester(Octree tree) : IDynamicTreeProxy, IRayCastable
{
    public int SetIndex { get; set; } = -1;
    public int NodePtr { get; set; }

    public JVector Velocity => JVector.Zero;
    public JBoundingBox WorldBoundingBox { get; } = tree.Dimensions;

    public bool RayCast(in JVector origin, in JVector direction, out JVector normal, out double lambda)
    {
        return tree.Raycast(origin,direction, out normal, out lambda);
    }
}

public class Demo20 : IDemo, ICleanDemo
{
    public string Name => "Custom Collision (Octree)";

    private Playground pg = null!;
    private World world = null!;

    private Tester testShape = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene();

        var tm = RenderWindow.Instance.CSMRenderer.GetInstance<Dragon>();

        var indices = tm.Mesh.Indices.Select(i
            => new Octree.TriangleIndices(i.T1, i.T2, i.T3)).ToArray();

        var vertices = tm.Mesh.Vertices.Select(v
            => Conversion.ToJitterVector(v.Position)).ToArray();

        // Build the octree
        var octree = new Octree(indices, vertices);

        // Add a "test" shape to the jitter world. We will filter out broad phase collision
        // events generated by Jitter and add our own collision handling. Make the test shape
        // the dimensions of the octree.
        testShape = new Tester(octree);
        world.DynamicTree.AddProxy(testShape, false);

        world.BroadPhaseFilter = new CustomCollisionDetection(world, testShape, octree);
    }

    public void Draw()
    {
        var tm = RenderWindow.Instance.CSMRenderer.GetInstance<Dragon>();
        tm.PushMatrix(Matrix4.Identity, new Vector3(0.35f, 0.35f, 0.35f));
    }

    public void CleanUp()
    {
        world.DynamicTree.RemoveProxy(testShape);
    }
}