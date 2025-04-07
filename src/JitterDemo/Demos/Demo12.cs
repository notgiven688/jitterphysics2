using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo12 : IDemo
{
    public string Name => "Speculative Contacts";

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        world.BroadPhaseFilter = new Common.IgnoreCollisionBetweenFilter();

        var wallBody = world.CreateRigidBody();
        wallBody.AddShape(new BoxShape(10, 10f, 0.02d));
        wallBody.Position = new JVector(0, 6, -10);
        wallBody.IsStatic = true;

        var sphereBody = world.CreateRigidBody();
        sphereBody.AddShape(new SphereShape(0.3d));
        sphereBody.Position = new JVector(-3, 8, -1);
        sphereBody.Velocity = new JVector(0, 0, -107);
        sphereBody.EnableSpeculativeContacts = true;

        var boxBody = world.CreateRigidBody();
        boxBody.AddShape(new BoxShape(0.3d));
        boxBody.Position = new JVector(+3, 8, -1);
        boxBody.Velocity = new JVector(0, 0, -107);
        boxBody.EnableSpeculativeContacts = true;


        Common.BuildRagdoll(new JVector(0, 8, -1),
            body =>
            {
                body.Velocity = new JVector(0, 0, -107);
                body.EnableSpeculativeContacts = true;
                body.SetActivationState(false);
            }
        );
    }

    public void Draw()
    {
        //Console.WriteLine(sphereBody.Velocity.Y);
    }
}