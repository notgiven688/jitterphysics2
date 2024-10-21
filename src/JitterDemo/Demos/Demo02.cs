using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo02 : IDemo
{
    public string Name => "Tower of Jitter";

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        Common.BuildTower(JVector.Zero);

        world.SolverIterations = (12, 4);
    }

    public void Draw()
    {
    }
}