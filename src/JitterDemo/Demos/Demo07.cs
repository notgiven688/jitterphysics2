using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo07 : IDemo
{
    public string Name => "Many Pyramids";

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        world.SolverIterations = 8;

        for (int e = 0; e < 2; e++)
        {
            for (int i = 0; i < 30; i++)
            {
                Common.BuildPyramid(new JVector(-20 + 40 * e, 0, -75 + 5 * i), 20,
                    body => body.SetActivationState(false));
            }
        }
    }

    public void Draw()
    {
    }
}