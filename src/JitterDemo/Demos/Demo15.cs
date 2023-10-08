using System.Collections.Generic;
using Jitter2;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo15 : IDemo, ICleanDemo
{
    public string Name => "Pressurized Soft Bodies";

    private Playground pg = null!;
    private readonly List<SoftBodySphere> spheres = new();
    private World world = null!;

    public void Build()
    {
        spheres.Clear();

        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene();

        // Prepare Jitter to handle soft bodies.
        world.DynamicTree.Filter = DynamicTreeCollisionFilter.Filter;
        world.BroadPhaseFilter = new BroadPhaseCollisionFilter(world);

        for (int i = 0; i < 3; i++)
        {
            var sphere = new SoftBodySphere(world, new JVector(-3 + i * 3, 2, 0));
            sphere.Pressure = 300;
            spheres.Add(sphere);
        }

        world.SolverIterations = 4;
        world.NumberSubsteps = 4;
    }

    public void Draw()
    {
        var dr = RenderWindow.Instance.DebugRenderer;

        foreach (var sphere in spheres)
        {
            foreach (var spring in sphere.Springs)
            {
                dr.PushLine(DebugRenderer.Color.Green, Conversion.FromJitter(spring.Body1.Position),
                    Conversion.FromJitter(spring.Body2.Position));
            }
        }
    }

    public void CleanUp()
    {
        spheres.ForEach(sphere => sphere.Destroy());
    }
}