using System.IO;
using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class DecomposedTeapot : MultiMesh
{
    public DecomposedTeapot() : base(Path.Combine("assets", "teapot_hull.obj"), 0.03f)
    {
    }
}

public class Demo00 : IDemo, IDrawUpdate
{
    private ConvexDecomposition<DecomposedTeapot> teapotDecomp = null!;

    public string Name => "Convex Decomposition";
    public string Description => "Convex-decomposed teapot models with compound convex hull collision.";

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        teapotDecomp = new ConvexDecomposition<DecomposedTeapot>(world);
        teapotDecomp.Load();

        for (int i = 0; i < 6; i++)
        {
            teapotDecomp.Spawn(new JVector(0, 10 + i * 3, -14));
            teapotDecomp.Spawn(new JVector(0, 10 + i * 3, -6));
            teapotDecomp.Spawn(new JVector(5, 10 + i * 3, -14));
            teapotDecomp.Spawn(new JVector(5, 10 + i * 3, -6));
        }

        world.SolverIterations = (8, 4);
    }

    public void DrawUpdate()
    {
        teapotDecomp.PushMatrices();
    }
}