using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo27 : IDemo, ICleanDemo
{
    public string Name => "CCD: Proof of concept";
    public string Description => "Continuous collision detection with fast-moving objects and thin geometry.";

    private CcdSolver ccdSolver = null!;

    public void Build(Playground pg, World world)
    {
        ccdSolver = new CcdSolver(world);

        pg.AddFloor();

        var paddle = world.CreateRigidBody();
        paddle.AddShape(new BoxShape(5, 1, 0.01f));
        paddle.Position = (0, 3, -20);
        paddle.AffectedByGravity = false;

        var hinge = new HingeJoint(world, world.NullBody, paddle, paddle.Position,
            JVector.UnitY, AngularLimit.Full, false);

        var ball = world.CreateRigidBody();
        ball.AddShape(new SphereShape(0.2f));
        ball.Position = (+2.2f, 3, 400);
        ball.Velocity = (0, 0, -400);
        ball.Damping = (0, 0);
        ball.AffectedByGravity = false;

        Common.BuildRagdoll((-2.2f, 3.5f, -19.7f), body =>
        {
            body.AffectedByGravity = false;
            ccdSolver.Add(body);
        });

        ccdSolver.Add(paddle);
        ccdSolver.Add(ball);

        world.SpeculativeRelaxationFactor = 0.5f;
    }

    public void CleanUp()
    {
        ccdSolver.Destroy();
    }
}