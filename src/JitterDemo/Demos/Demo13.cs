using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo13 : IDemo
{
    public string Name => "Motor and Limit";

    public void Build()
    {
        var pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        pg.ResetScene();

        {
            // Motor
            var b0 = world.CreateRigidBody();
            b0.AddShape(new BoxShape(2, 0.1d, 0.1d));
            b0.Position = new JVector(-1.1d, 4, 0);

            var b1 = world.CreateRigidBody();
            b1.AddShape(new BoxShape(2, 0.1d, 0.1d));
            b1.Position = new JVector(1.1d, 4, 0);

            HingeJoint hj = new HingeJoint(world, world.NullBody, b0, b0.Position, JVector.UnitX, AngularLimit.Full, true);
            UniversalJoint uj = new UniversalJoint(world, b0, b1, new JVector(0, 4, 0), JVector.UnitX, JVector.UnitX);

            if (hj.Motor != null)
            {
                hj.Motor.TargetVelocity = 4;
                hj.Motor.MaximumForce = 1;
            }

            if (world.BroadPhaseFilter is not Common.IgnoreCollisionBetweenFilter filter)
            {
                filter = new Common.IgnoreCollisionBetweenFilter();
                world.BroadPhaseFilter = filter;
            }

            filter.IgnoreCollisionBetween(b0.Shapes[0], b1.Shapes[0]);
        }

        {
            // Hinge Joint with -120 <-> + 120 Limit
            var b0 = world.CreateRigidBody();
            b0.AddShape(new BoxShape(2, 0.1d, 3));
            b0.AddShape(new BoxShape(0.1d, 2, 2.9d));
            b0.Position = new JVector(-5, 3, 0);

            HingeJoint hj = new HingeJoint(world, world.NullBody, b0, b0.Position, JVector.UnitZ, AngularLimit.FromDegree(-120, 120));
            hj.HingeAngle.Bias = 0.3d;
            hj.HingeAngle.Softness = 0.0d;
            hj.HingeAngle.LimitBias = 0.3d;
            hj.HingeAngle.LimitSoftness = 0.0d;

            for (int i = 0; i < 4; i++)
                Common.BuildRagdoll(new JVector(-4, 5 + i * 3, 0));
        }


        world.SolverIterations = (4, 2);
        world.SubstepCount = 3;
    }

    public void Draw()
    {
    }
}