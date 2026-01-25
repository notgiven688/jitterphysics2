using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo16 : IDemo, ICleanDemo
{
    public string Name => "Soft Body Cubes";

    private Playground pg = null!;
    private readonly List<SoftBodyCube> cubes = new();
    private World world = null!;

    public void Build()
    {
        cubes.Clear();

        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene();

        world.DynamicTree.Filter = DynamicTreeCollisionFilter.Filter;
        world.BroadPhaseFilter = new BroadPhaseCollisionFilter(world);

        for (int i = 0; i < 3; i++)
        {
            JVector pos = new JVector(0, 5 + i * 3, 0);

            var cube = new SoftBodyCube(world, pos);
            cubes.Add(cube);

            JVector[] offset =
            {
                new(-0.5d, -1.5d, -0.5d),
                new(-0.5d, -1.5d, +0.5d),
                new(+0.5d, -1.5d, -0.5d),
                new(+0.5d, -1.5d, +0.5d)
            };

            for (int e = 0; e < 4; e++)
            {
                var c0 = world.CreateRigidBody();
                c0.AddShape(new BoxShape(1));
                c0.Position = pos + offset[e];
            }
        }

        JVector position = new JVector(10, 1, 0);

        for (int i = 0; i < 3; i++)
        {
            for (int e = i; e < 3; e++)
            {
                JVector cpos = position + new JVector((e - i * 0.5d) * 1.01d, 0.5d + i * 1.0d, 0) * 2;
                var cube = new SoftBodyCube(world, cpos);
                cubes.Add(cube);
            }
        }

        world.SolverIterations = (4, 2);
        world.SubstepCount = 4;
    }

    public void Draw()
    {
        var dr = RenderWindow.Instance.DebugRenderer;
        foreach (var cube in cubes)
        {
            foreach (var spring in SoftBodyCube.Edges)
            {
                dr.PushLine(DebugRenderer.Color.Green, Conversion.FromJitter(cube.Vertices[spring.Item1].Position),
                    Conversion.FromJitter(cube.Vertices[spring.Item2].Position));

                dr.PushPoint(DebugRenderer.Color.White, Conversion.FromJitter(cube.Center.Position), 0.2f);
            }
        }
    }

    public void CleanUp()
    {
        foreach (var cube in cubes) cube.Destroy();
    }
}