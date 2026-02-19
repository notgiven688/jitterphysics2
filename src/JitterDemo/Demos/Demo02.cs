using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo02 : IDemo
{
    public string Name => "Tower of Jitter";
    public string Description => "A single tower of stacked bodies to test solver stability.";

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        Common.BuildTower(JVector.Zero);

        world.SolverIterations = (12, 4);
    }
}