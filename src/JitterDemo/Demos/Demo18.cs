using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo18 : IDemo
{
    public void Draw()
    {
        float time = (float)pg.Time;

        JMatrix ori = JMatrix.CreateRotationX(1.1d * time) *
                      JMatrix.CreateRotationY(2.1d * time) *
                      JMatrix.CreateRotationZ(1.7d * time);

        for (int i = -15; i < 16; i++)
        {
            for (int e = -15; e < 16; e++)
            {
                for (int k = -15; k < 16; k++)
                {
                    JVector point = new JVector(i, e, k) * 0.1d;

                    bool result = NarrowPhase.PointTest(testShape, ori, JVector.Zero, point);

                    if (result)
                    {
                        pg.DebugRenderer.PushPoint(DebugRenderer.Color.White,
                            Conversion.FromJitter(point), 0.01f);
                    }
                }
            }
        }
    }

    public string Name => "PointTest";

    private Playground pg = null!;
    private World world = null!;

    private Shape testShape = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        testShape = new ConeShape(1d);
        pg.ResetScene(false);
    }
}