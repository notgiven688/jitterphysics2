using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo13 : IDemo
{
    public string Name => "Motor and Limit";
    public string Description => "Hinge joints with angular motors, angular limits, and coupled rotating wheels.";

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        {
            // Motor
            var b0 = world.CreateRigidBody();
            b0.AddShape(new BoxShape(2, 0.1f, 0.1f));
            b0.Position = new JVector(-1.1f, 4, 0);

            var b1 = world.CreateRigidBody();
            b1.AddShape(new BoxShape(2, 0.1f, 0.1f));
            b1.Position = new JVector(1.1f, 4, 0);

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
            b0.AddShape(new BoxShape(2, 0.1f, 3));
            b0.AddShape(new BoxShape(0.1f, 2, 2.9f));
            b0.Position = new JVector(-5, 3, 0);

            HingeJoint hj = new HingeJoint(world, world.NullBody, b0, b0.Position, JVector.UnitZ, AngularLimit.FromDegree(-120, 120));
            hj.HingeAngle.Bias = 0.3f;
            hj.HingeAngle.Softness = 0.0f;
            hj.HingeAngle.LimitBias = 0.3f;
            hj.HingeAngle.LimitSoftness = 0.0f;

            for (int i = 0; i < 4; i++)
                Common.BuildRagdoll(new JVector(-4, 5 + i * 3, 0));
        }

        {
            float angle = (float)JAngle.FromDegree(90);
            JVector rot1Axis = JVector.Transform(JVector.UnitZ, JQuaternion.CreateRotationY(angle));

            // two free rotating wheels
            var b0 = world.CreateRigidBody();
            b0.Position = new JVector(5, 4, 0);
            b0.Orientation = JQuaternion.CreateRotationX(MathF.PI / 2);
            b0.AddShape(new CylinderShape(0.4f, 2.0f));

            var b1 = world.CreateRigidBody();
            b1.AddShape(new CylinderShape(0.4f, 2.0f));
            b1.Position = new JVector(9.2f, 4, 0);
            b1.Orientation = JQuaternion.CreateRotationY(angle) * JQuaternion.CreateRotationX(MathF.PI / 2);

            HingeJoint hj1 = new HingeJoint(world, world.NullBody, b0, b0.Position, JVector.UnitZ, AngularLimit.Full);
            HingeJoint hj2 = new HingeJoint(world, world.NullBody, b1, b1.Position, rot1Axis, AngularLimit.Full);
            hj1.HingeAngle.Softness = 0;
            hj2.HingeAngle.Softness = 0;

            // constraint them to have the same rotation
            var relative = world.CreateConstraint<TwistAngle>(b0, b1);
            relative.Initialize(JVector.UnitZ, rot1Axis);
        }

        world.SolverIterations = (4, 2);
        world.SubstepCount = 3;
    }
}