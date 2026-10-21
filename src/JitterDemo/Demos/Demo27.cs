using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;

namespace JitterDemo;

public class Demo27 : IDemo
{
    public string Name => "CCD: Iterative discovery";
    public string Description => "Speculative contact discovery with fast-moving objects and thin geometry.";

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        var paddle = world.CreateRigidBody();
        paddle.AddShape(new BoxShape(5, 1, 0.01f));
        paddle.Position = (0, 3, -20);
        paddle.AffectedByGravity = false;
        paddle.EnableCcd = true;

        var hinge = new HingeJoint(world, world.NullBody, paddle, paddle.Position,
            JVector.UnitY, AngularLimit.Full);

        var ball = world.CreateRigidBody();
        ball.AddShape(new SphereShape(0.2f));
        ball.Position = (+2.2f, 3, 400);
        ball.Velocity = (0, 0, -400);
        ball.Damping = (0, 0);
        ball.AffectedByGravity = false;
        ball.EnableCcd = true;

        Common.BuildRagdoll((-2.2f, 3.5f, -19.7f), body =>
        {
            body.AffectedByGravity = false;
            body.EnableCcd = true;
        });

        world.CcdRelaxationFactor = 0.5f;
    }
}
