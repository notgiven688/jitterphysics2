using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo26 : IDemo
{
    public string Name => "Angular Sweep";

    private BoxShape staticBar = null!;
    private BoxShape dynamicBox = null!;

    private JVector position = new JVector(0, 0, 10);
    private JVector velocity = new JVector(0, 0, -10);
    private JVector angularVelocity = new JVector(1, 2,2);

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene(false);

        staticBar = new BoxShape(10,10,0.1d);
        dynamicBox = new BoxShape(5,1,1);
    }

    private Matrix4 CreateMatrix(JVector pos, JVector vel, JVector angVel, float dt)
    {
        JQuaternion quat = MathHelper.RotationQuaternion(angularVelocity, dt);
        Matrix4 orientation = Conversion.FromJitter(JMatrix.CreateFromQuaternion(quat));
        Matrix4 translation = MatrixHelper.CreateTranslation(Conversion.FromJitter(pos + vel * dt));
        Matrix4 scale = MatrixHelper.CreateScale(5, 1, 1);

        return translation * orientation * scale;
    }

    public void Draw()
    {
        Playground pg = (Playground)RenderWindow.Instance;

        var kb = Keyboard.Instance;
        if(kb.IsKeyDown(Keyboard.Key.O)) position += new JVector(0,0,0.01d);
        if(kb.IsKeyDown(Keyboard.Key.P)) position -= new JVector(0,0,0.01d);

        var cr = pg.CSMRenderer.GetInstance<Cube>();

        cr.PushMatrix(MatrixHelper.CreateScale(10, 10, 0.1f), new Vector3(0.2f, 0.2f, 0.2f));

        bool res = NarrowPhase.Sweep(staticBar, dynamicBox, JQuaternion.Identity, JQuaternion.Identity,
            JVector.Zero, position,
            JVector.Zero, velocity, JVector.Zero, angularVelocity, 10, 10,
            out JVector posA, out JVector posB, out JVector normal, out double lambda);

        if (!res) return;

        for (int i = 0; i <= 10; i++)
        {
            cr.PushMatrix(CreateMatrix(position, velocity, angularVelocity, (float)(i * 0.1d * lambda)),
                ColorGenerator.GetColor(i*4));
        }

        pg.DebugRenderer.PushPoint(DebugRenderer.Color.White, Conversion.FromJitter(posA), 2);
        pg.DebugRenderer.PushPoint(DebugRenderer.Color.White, Conversion.FromJitter(posB), 2);
    }
}