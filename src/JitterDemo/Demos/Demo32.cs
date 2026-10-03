using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo32 : IDemo
{
    public string Name => "2D Hanging Chain";
    public string Description => "A hanging chain swings in the XY plane. Drag a link to set it in motion.";

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        world.SubstepCount = 2;
        world.SolverIterations = (8, 4);

        var filter = new Common.IgnoreCollisionBetweenFilter();
        world.BroadPhaseFilter = filter;
        
        const int linkCount = 12;
        const float linkSpacing = 0.9f;
        JVector anchor = new(0, 14, -8);
        JQuaternion orientation = JQuaternion.CreateRotationZ(0.55f);
        JVector direction = JVector.Transform(-JVector.UnitY, orientation);

        RigidBody support = world.CreateRigidBody();
        support.AddShape(new BoxShape(10, 0.4f, 0.6f));
        support.Position = anchor + new JVector(0, 0.25f, 0);
        support.MotionType = MotionType.Static;

        RigidBody previous = support;
        for (int i = 0; i < linkCount; i++)
        {
            RigidBody link = world.CreateRigidBody();
            link.AddShape(new BoxShape(0.28f, 0.85f, 0.28f));
            link.SetMassInertia(1);
            link.Position = anchor + direction * (0.5f * linkSpacing);
            link.Orientation = orientation;
            link.AllowedMotion = MotionAxes.PlaneXY;
            link.Damping = (0.001f, 0.001f);

            // The allowed axes provide planar rotation; each connection only needs a ball socket.
            BallSocket socket = world.CreateConstraint<BallSocket>(previous, link);
            socket.Initialize(anchor);
            socket.Softness = 0;
            filter.IgnoreCollisionBetween(previous.Shapes[0], link.Shapes[0]);

            previous = link;
            anchor += direction * linkSpacing;
        }

        RigidBody weight = world.CreateRigidBody();
        weight.AddShape(new SphereShape(0.65f));
        weight.SetMassInertia(3);
        weight.Position = anchor + direction * 0.8f;
        weight.AllowedMotion = MotionAxes.PlaneXY;
        weight.Damping = (0.001f, 0.001f);

        BallSocket lastSocket = world.CreateConstraint<BallSocket>(previous, weight);
        lastSocket.Initialize(anchor);
        lastSocket.Softness = 0;
        filter.IgnoreCollisionBetween(previous.Shapes[0], weight.Shapes[0]);
    }
}
