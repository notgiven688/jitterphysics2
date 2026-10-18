using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo01 : IDemo, IDrawUpdate
{
    public string Name => "Constraint car";
    public string Description => "Constraint-based car with a breakable hinge-joint suspension bridge.";
    public string Controls => "Arrow Keys - Steer and accelerate";

    private readonly ConstraintCar car = new();
    private readonly List<HingeJoint> hinges = new();
    private World world = null!;

    public void Build(Playground pg, World world)
    {
        this.world = world;

        hinges.Clear();

        pg.AddFloor();

        {
            // -------  bridge
            RigidBody body = null!;

            JVector startPos = new JVector(-10, 8, -20);
            const int numElements = 30;
            const float panelLength = 0.7f;
            const float panelSpacing = 0.8f;

            for (int i = 0; i < numElements; i++)
            {
                var nbody = world.CreateRigidBody();
                nbody.AddShape(new BoxShape(panelLength, 0.1f, 4f));
                nbody.Position = startPos + new JVector(i * panelSpacing, 0, 0);

                if (i == 0)
                {
                    var hinge = new HingeJoint(world, world.NullBody, nbody,
                        nbody.Position - JVector.UnitX * (panelLength * 0.5f), JVector.UnitZ);
                }
                else
                {
                    var hinge = new HingeJoint(world, body, nbody,
                        (body.Position + nbody.Position) * 0.5f, JVector.UnitZ);

                    hinge.BallSocket.Softness = 0.02f;
                    hinges.Add(hinge);
                }

                if (i == numElements - 1)
                {
                    var hinge = new HingeJoint(world, nbody, world.NullBody,
                        nbody.Position + JVector.UnitX * (panelLength * 0.5f), JVector.UnitZ);
                }

                body = nbody;
            }
        }

        {
            // Add a car made out of constraints
            JVector carPos = new JVector(10, 9, -20);
            JQuaternion rot = JQuaternion.CreateRotationY(MathF.PI / 2.0f);

            car.BuildCar(world, carPos, body =>
                {
                    body.Position = JVector.Transform(body.Position, rot) + carPos;
                    body.Orientation = rot;
                }
            );
        }

        world.SubstepCount = 4;
        world.SolverIterations = (2, 2);
    }

    public void DrawUpdate()
    {
        for (int i = hinges.Count; i-- > 0;)
        {
            var h = hinges[i];
            if (h.BallSocket.Impulse.Length() > 1f)
            {
                h.Remove();
                hinges.RemoveAt(i);
            }
        }

        car.UpdateControls();
    }
}
