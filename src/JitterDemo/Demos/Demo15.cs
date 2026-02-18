using System.Collections.Generic;
using Jitter2;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo15 : IDemo, ICleanDemo, IDrawUpdate
{
    public string Name => "Pressurized Soft Bodies";
    public string Description => "Soft-body spheres using spring-connected particles and internal pressure.";

    private readonly List<SoftBodySphere> spheres = new();
    private World world = null!;

    public void Build(Playground pg, World world)
    {
        spheres.Clear();

        this.world = world;

        pg.AddFloor();

        // Prepare Jitter to handle soft bodies.
        world.DynamicTree.Filter = DynamicTreeCollisionFilter.Filter;
        world.BroadPhaseFilter = new BroadPhaseCollisionFilter(world);

        for (int i = 0; i < 3; i++)
        {
            var sphere = new SoftBodySphere(world, new JVector(-3 + i * 3, 2, 0));
            sphere.Pressure = 300;
            spheres.Add(sphere);
        }

        world.SolverIterations = (4, 2);
        world.SubstepCount = 3;
    }

    public void DrawUpdate()
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