using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

#pragma warning disable CS8602

public class ConstraintCar
{
    private RigidBody car = null!;
    private readonly RigidBody[] damper = new RigidBody[4];
    private readonly RigidBody[] wheels = new RigidBody[4];
    private readonly HingeJoint[] sockets = new HingeJoint[4];
    private readonly PrismaticJoint[] damperJoints = new PrismaticJoint[4];
    private readonly AngularMotor[] steerMotor = new AngularMotor[2];

    private const int FrontLeft = 0;
    private const int FrontRight = 1;
    private const int BackLeft = 2;
    private const int BackRight = 3;

    private const float MaxAngle = 40;
    private float steer;

    public void BuildCar(World world, JVector position, Action<RigidBody>? action = null)
    {
        List<RigidBody> bodies = new List<RigidBody>(9);

        car = world.CreateRigidBody();
        bodies.Add(car);

        TransformedShape tfs1 = new TransformedShape(new BoxShape(1.5f, 0.60f, 3), new JVector(0, -0.3f, 0.0f));
        TransformedShape tfs2 = new TransformedShape(new BoxShape(1.0f, 0.45f, 1.5f), new JVector(0, 0.20f, 0.3f));

        car.AddShape(tfs1);
        car.AddShape(tfs2);
        car.Position = new JVector(0, 2, 0);
        car.SetMassInertia(new JMatrix(0.4f, 0, 0, 0, 0.4f, 0, 0, 0, 1.0f), 1.0f);

        for (int i = 0; i < 4; i++)
        {
            damper[i] = world.CreateRigidBody();
            damper[i].AddShape(new BoxShape(0.2f));
            damper[i].SetMassInertia(0.1f);

            wheels[i] = world.CreateRigidBody();

            CylinderShape shape = new CylinderShape(0.1f, 0.3f);
            TransformedShape tf = new TransformedShape(shape, JVector.Zero, JMatrix.CreateRotationZ(MathF.PI / 2.0f));

            wheels[i].AddShape(tf);

            bodies.Add(wheels[i]);
            bodies.Add(damper[i]);
            //wheels[i].IsStatic = true;
        }


        //car.IsStatic = true;
        car.DeactivationTime = TimeSpan.MaxValue;

        damper[FrontLeft].Position = new JVector(-0.75f, 1.4f, -1.1f);
        damper[FrontRight].Position = new JVector(+0.75f, 1.4f, -1.1f);

        damper[BackLeft].Position = new JVector(-0.75f, 1.4f, 1.1f);
        damper[BackRight].Position = new JVector(+0.75f, 1.4f, 1.1f);

        for (int i = 0; i < 4; i++)
        {
            wheels[i].Position = damper[i].Position;
        }

        for (int i = 0; i < 4; i++)
        {
            damperJoints[i] = new PrismaticJoint(world, car, damper[i], damper[i].Position, JVector.UnitY, LinearLimit.Fixed, false);

            damperJoints[i].Slider.LimitBias = 2;
            damperJoints[i].Slider.LimitSoftness = 0.6f;
            damperJoints[i].Slider.Bias = 0.2f;

            damperJoints[i].HingeAngle.LimitBias = 0.6f;
            damperJoints[i].HingeAngle.LimitSoftness = 0.01f;
        }

        damperJoints[FrontLeft].HingeAngle.Limit = AngularLimit.FromDegree(-MaxAngle, MaxAngle);
        damperJoints[FrontRight].HingeAngle.Limit = AngularLimit.FromDegree(-MaxAngle, MaxAngle);
        damperJoints[BackLeft].HingeAngle.Limit = AngularLimit.Fixed;
        damperJoints[BackRight].HingeAngle.Limit = AngularLimit.Fixed;

        for (int i = 0; i < 4; i++)
        {
            sockets[i] = new HingeJoint(world, damper[i], wheels[i], wheels[i].Position, JVector.UnitX, true);
        }

        if (world.BroadPhaseFilter is not Common.IgnoreCollisionBetweenFilter filter)
        {
            filter = new Common.IgnoreCollisionBetweenFilter();
            world.BroadPhaseFilter = filter;
        }

        for (int i = 0; i < 4; i++)
        {
            filter.IgnoreCollisionBetween(car.Shapes[0], damper[i].Shapes[0]);
            filter.IgnoreCollisionBetween(wheels[i].Shapes[0], damper[i].Shapes[0]);
            filter.IgnoreCollisionBetween(car.Shapes[0], wheels[i].Shapes[0]);
        }

        steerMotor[FrontLeft] = world.CreateConstraint<AngularMotor>(car, damper[FrontLeft]);
        steerMotor[FrontLeft].Initialize(JVector.UnitY);
        steerMotor[FrontRight] = world.CreateConstraint<AngularMotor>(car, damper[FrontRight]);
        steerMotor[FrontRight].Initialize(JVector.UnitY);

        if (action != null) bodies.ForEach(action);
    }

    public void UpdateControls()
    {
        float accelerate;
        var kb = RenderWindow.Instance.Keyboard;

        if (kb.IsKeyDown(Keyboard.Key.Up)) accelerate = 1.0f;
        else if (kb.IsKeyDown(Keyboard.Key.Down)) accelerate = -1.0f;
        else accelerate = 0.0f;

        if (kb.IsKeyDown(Keyboard.Key.Left)) steer += 0.1f;
        else if (kb.IsKeyDown(Keyboard.Key.Right)) steer -= 0.1f;
        else steer *= 0.9f;

        steer = Math.Clamp(steer, -1.0f, 1.0f);

        float targetAngle = steer * MaxAngle / 180.0f * MathF.PI;
        float currentAngleL = (float)damperJoints[FrontLeft].HingeAngle.Angle;
        float currentAngleR = (float)damperJoints[FrontRight].HingeAngle.Angle;

        steerMotor[FrontLeft].MaximumForce = 10.0f * Math.Abs(targetAngle - currentAngleL);
        steerMotor[FrontLeft].TargetVelocity = 10.0f * (targetAngle - currentAngleL);

        steerMotor[FrontRight].MaximumForce = 10.0f * Math.Abs(targetAngle - currentAngleR);
        steerMotor[FrontRight].TargetVelocity = 10.0f * (targetAngle - currentAngleR);

        for (int i = 0; i < 4; i++)
        {
            wheels[i].Friction = 0.8f;
            sockets[i].Motor.MaximumForce = 1.0f * MathF.Abs(accelerate);
            sockets[i].Motor.TargetVelocity = -80.0f * accelerate;
        }
    }
}