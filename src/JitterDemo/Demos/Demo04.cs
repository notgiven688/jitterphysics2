using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using static JitterDemo.Common;

namespace JitterDemo;

public class Demo04 : IDemo
{
    public string Name => "Many Ragdolls";
    public string Description => "100 ragdolls dropping from increasing heights with collision filtering between limbs.";

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        for (int i = 0; i < 100; i++)
        {
            BuildRagdoll(new JVector(0, 3 + 2 * i, 0));
        }

        world.SolverIterations = (8, 4);
    }
}