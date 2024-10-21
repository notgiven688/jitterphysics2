using System.IO;
using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Teapot : MultiMesh
{
    public Teapot() : base(Path.Combine("assets", "teapot_hull.obj"), 0.03f)
    {
    }
}

public class Demo00 : IDemo
{
    private ConvexDecomposition<Teapot> teapotDecomp = null!;

    public string Name => "Convex Decomposition";

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        teapotDecomp = new ConvexDecomposition<Teapot>(world);
        teapotDecomp.Load();

        for (int i = 0; i < 6; i++)
            teapotDecomp.Spawn(new JVector(0, 10 + i * 3, -14));

        for (int i = 0; i < 6; i++)
            teapotDecomp.Spawn(new JVector(0, 10 + i * 3, -6));

        for (int i = 0; i < 6; i++)
            teapotDecomp.Spawn(new JVector(5, 10 + i * 3, -14));

        for (int i = 0; i < 6; i++)
            teapotDecomp.Spawn(new JVector(5, 10 + i * 3, -6));

        world.SolverIterations = (8, 4);
    }

    public void Draw()
    {
        teapotDecomp.PushMatrices();
    }
}