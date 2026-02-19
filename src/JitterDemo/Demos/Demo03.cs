using Jitter2;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo03 : IDemo
{
    public string Name => "Ancient Pyramids";
    public string Description => "Large pyramids of boxes and cylinders to stress-test stacking.";

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        Common.BuildPyramid(JVector.Zero, 40);
        Common.BuildPyramidCylinder(new JVector(10, 0, 10));

        world.SolverIterations = (4, 4);
    }
}