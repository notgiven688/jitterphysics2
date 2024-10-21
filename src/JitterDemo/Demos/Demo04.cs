using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using static JitterDemo.Common;

namespace JitterDemo;

public class Demo04 : IDemo
{
    public string Name => "Many Ragdolls";


    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        var filter = new IgnoreCollisionBetweenFilter();

        world.BroadPhaseFilter = filter;

        pg.ResetScene();

        for (int i = 0; i < 100; i++)
        {
            BuildRagdoll(new JVector(0, 3 + 2 * i, 0));
        }

        world.SolverIterations = (8, 4);
    }

    public void Draw()
    {
    }
}