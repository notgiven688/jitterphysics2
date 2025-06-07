using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo03 : IDemo
{
    public string Name => "Ancient Pyramids";

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        Common.BuildPyramid(JVector.Zero, 40);
        Common.BuildPyramidCylinder(new JVector(10, 0, 10));

        world.SolverIterations = (4, 4);
    }

    public void Draw()
    {
    }
}