using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo18 : IDemo
{
    public void Draw()
    {
        
        JMatrix ori = JMatrix.CreateRotationX((float)pg.Time) * JMatrix.CreateRotationY(2.1f *(float)pg.Time)
                                                              * JMatrix.CreateRotationZ(3.1f *(float)pg.Time);

        
        for (int i = -15; i < 16; i++)
        {
            for (int e = -15; e < 16; e++)
            {
                for (int k = -15; k < 16; k++)
                {
                    JVector point = new JVector(i, e, k) * 0.1f;

                    bool result = NarrowPhase.PointTest(testShape, ori, JVector.Zero, point);

                    if (result)
                    {
                        pg.DebugRenderer.PushPoint(DebugRenderer.Color.White,
                            Conversion.FromJitter(point), 0.01f);
                    }
                    else
                    {

                    }

                }
            }
        }

    }

    public string Name => "PointTest";

    private Playground pg = null!;
    private World world = null!;
    

    private Shape testShape;


    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        testShape = new ConeShape(1f, 1);
        

        pg.ResetScene(false);

    }
    


}