using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;

namespace JitterDemo;

// Shows one way to implement a character controller.
public class Player
{
    public RigidBody Body { get; }
    public LinearMotor FrictionMotor { get; }
    public AngularMotor AngularMovement { get; }

    private readonly float capsuleHalfHeight;
    private readonly World world;

    public Player(World world, JVector position)
    {
        Body = world.CreateRigidBody();
        var cs = new CapsuleShape(0.5f, 1.0f);
        Body.AddShape(cs);
        Body.Position = position;
        
        // disable velocity damping
        Body.Damping = (0, 0);

        this.capsuleHalfHeight = cs.Radius + cs.Length * 0.5f;

        this.world = world;

        // Disable deactivation
        Body.DeactivationTime = TimeSpan.MaxValue;

        // Add two arms - to be able to visually tell how the player is orientated
        var arm1 = new TransformedShape(new BoxShape(0.2f, 0.8f, 0.2f), new JVector(+0.5f, 0.3f, 0));
        var arm2 = new TransformedShape(new BoxShape(0.2f, 0.8f, 0.2f), new JVector(-0.5f, 0.3f, 0));

        // Add the shapes without recalculating mass and inertia, we take mass and inertia from the capsule
        // shape we added before.
        Body.AddShape(arm1, false);
        Body.AddShape(arm2, false);

        // Make the capsule stand upright, but able to rotate 360 degrees.
        var ur = world.CreateConstraint<HingeAngle>(Body, world.NullBody);
        ur.Initialize(JVector.UnitY, AngularLimit.Full);

        // Add a "motor" to the body. The motor target velocity is zero.
        // This acts like friction and stops the player.
        FrictionMotor = world.CreateConstraint<LinearMotor>(Body, world.NullBody);
        FrictionMotor.Initialize(JVector.UnitZ, JVector.UnitX);
        FrictionMotor.MaximumForce = 10;

        // An angular motor for turning.
        AngularMovement = world.CreateConstraint<AngularMotor>(Body, world.NullBody);
        AngularMovement.Initialize(JVector.UnitY, JVector.UnitY);
        AngularMovement.MaximumForce = 1000;
    }

    public void SetAngularInput(float rotate)
    {
        AngularMovement.TargetVelocity = rotate;
    }

    private World.RaycastFilterPre? preFilter = null;
    public bool FilterShape(Shape shape)
    {
        if (shape.RigidBody != null)
        {
            if (shape.RigidBody == Body) return false;
        }
        return true;
    }
    
    private bool IsOnFloor(out Shape? floor, out JVector hitPoint)
    {
        preFilter ??= FilterShape;

        bool hit = world.Raycast(Body.Position, -JVector.UnitY, preFilter, null,
            out floor, out JVector normal, out float fraction);

        float delta = fraction - capsuleHalfHeight;

        hitPoint = Body.Position - JVector.UnitY * fraction;
        return (hit && delta < 0.04f && floor != null);
    }

    public void Jump()
    {
        if (IsOnFloor(out Shape? result, out JVector hitPoint))
        {
            float newYVel = 5.0f;

            if (result != null && result.RigidBody != null)
            {
                newYVel += result.Velocity.Y;
            }

            float deltaVel = Body.Velocity.Y - newYVel;

            Body.Velocity = new JVector(Body.Velocity.X, newYVel, Body.Velocity.Z);

            if (result!.RigidBody != null && !result.RigidBody.IsStatic)
            {
                float force = Body.Mass * deltaVel * 100.0f;
                result.RigidBody.SetActivationState(true);
                result.RigidBody.AddForce(JVector.UnitY * force, hitPoint);
            }
        }
    }

    public void SetLinearInput(JVector deltaMove)
    {
        if (!IsOnFloor(out _, out _))
        {
            FrictionMotor.IsEnabled = false;
            return;
        }
        
        deltaMove *= 3.0f;

        float deltaMoveLen = deltaMove.Length();

        JVector bodyVel = Body.Velocity;
        bodyVel.Y = 0;

        float bodyVelLen = bodyVel.Length();

        if (deltaMoveLen > 0.01f)
        {
            if (bodyVelLen < 5f)
            {
                Body.AddForce(JVector.Transform(deltaMove, Body.Orientation) * 10);
            }
        }

        if (bodyVelLen > 0.01f)
        {
            FrictionMotor.LocalAxis1 = JVector.TransposedTransform(bodyVel * (1.0f / bodyVelLen), Body.Orientation);
            FrictionMotor.TargetVelocity = 0;
            FrictionMotor.IsEnabled = true;
        }
        else
        {
            FrictionMotor.IsEnabled = false;
        }
    }
}