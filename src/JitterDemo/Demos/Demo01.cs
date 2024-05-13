using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo01 : IDemo
{
    public string Name => "Constraint car";

    private readonly ConstraintCar car = new();
    private readonly List<HingeJoint> hinges = new();
    private World world = null!;

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        hinges.Clear();

        pg.ResetScene();

        {
            // -------  bridge
            RigidBody body = null!;

            JVector startPos = new JVector(-10, 8, -20);
            const int numElements = 30;

            for (int i = 0; i < numElements; i++)
            {
                var nbody = world.CreateRigidBody();
                nbody.AddShape(new BoxShape(0.7f, 0.1f, 4f));
                nbody.Position = startPos + new JVector(i * 0.8f, 0, 0);

                if (i == 0)
                {
                    var hinge = new HingeJoint(world, world.NullBody, nbody,
                        startPos + new JVector(i * 0.8f - 0.1f, 0, 0), JVector.UnitZ);
                }
                else
                {
                    var hinge = new HingeJoint(world, body, nbody,
                        startPos + new JVector(i * 0.8f - 0.1f, 0, 0), JVector.UnitZ);

                    hinge.BallSocket.Softness = 0.1f;
                    hinges.Add(hinge);
                }

                if (i == numElements - 1)
                {
                    var hinge = new HingeJoint(world, nbody, world.NullBody,
                        startPos + new JVector(i * 0.8f + 0.7f, 0, 0), JVector.UnitZ);
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

        world.NumberSubsteps = 4;
        world.SolverIterations = 4;
    }

    public void Draw()
    {
        for (int i = hinges.Count; i-- > 0;)
        {
            var h = hinges[i];
            if (h.BallSocket.Impulse.Length() > 0.5f)
            {
                h.Remove();
                hinges.RemoveAt(i);
            }
        }

        Playground pg = (Playground)RenderWindow.Instance;
        car.UpdateControls();
    }
}