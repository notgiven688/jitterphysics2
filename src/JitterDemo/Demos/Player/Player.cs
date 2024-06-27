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

    private World.RayCastFilterPre? preFilter = null;
    public bool FilterShape(Shape shape)
    {
        if (shape.RigidBody != null)
        {
            if (shape.RigidBody == Body) return false;
        }
        return true;
    }
    
    private bool CanJump(out RigidBody? floor, out JVector hitPoint)
    {
        preFilter ??= FilterShape;

        foreach(var contact in Body.Contacts)
        {
            // go through all contacts of the capsule (player)

            var cd = contact.Handle.Data;
            int numContacts = 0;
            hitPoint = JVector.Zero;

            // a contact may contain up to four contact points,
            // see which ones were used during the last step.
            uint mask = cd.UsageMask >> 4;

            if  (contact.Body1 == Body)
            {
                if ((mask & ContactData.MaskContact0) != 0) { hitPoint += cd.Contact0.RelativePos1; numContacts++; }
                if ((mask & ContactData.MaskContact1) != 0) { hitPoint += cd.Contact1.RelativePos1; numContacts++; }
                if ((mask & ContactData.MaskContact2) != 0) { hitPoint += cd.Contact2.RelativePos1; numContacts++; }
                if ((mask & ContactData.MaskContact3) != 0) { hitPoint += cd.Contact3.RelativePos1; numContacts++; }
                floor = contact.Body2;
            }
            else
            {
                if ((mask & ContactData.MaskContact0) != 0) { hitPoint += cd.Contact0.RelativePos2; numContacts++; }
                if ((mask & ContactData.MaskContact1) != 0) { hitPoint += cd.Contact1.RelativePos2; numContacts++; }
                if ((mask & ContactData.MaskContact2) != 0) { hitPoint += cd.Contact2.RelativePos2; numContacts++; }
                if ((mask & ContactData.MaskContact3) != 0) { hitPoint += cd.Contact3.RelativePos2; numContacts++; }
                floor = contact.Body1;
            }

            if(numContacts == 0) continue;

            // divide the result by the number of contact points to get the "center" of the contact
            hitPoint *= (1.0f / (float)numContacts);

            // check if the hitpoint is on the players base
            if(hitPoint.Y <= -0.8f) return true;
        }

        hitPoint = JVector.Zero;
        floor = null;

        return false;

        /*
        // ...or the more traditional way of using a raycast

        bool hit = world.RayCast(Body.Position, -JVector.UnitY, preFilter, null,
            out floor, out JVector normal, out float fraction);

        float delta = fraction - capsuleHalfHeight;

        hitPoint = Body.Position - JVector.UnitY * fraction;
        return (hit && delta < 0.04f && floor != null);
        */
    }

    public void Jump()
    {
        if (CanJump(out RigidBody? floorBody, out JVector hitPoint))
        {
            float newYVel = 5.0f;

            if (floorBody != null && floorBody != null)
            {
                newYVel += floorBody.Velocity.Y;
            }

            float deltaVel = Body.Velocity.Y - newYVel;

            Body.Velocity = new JVector(Body.Velocity.X, newYVel, Body.Velocity.Z);

            if (floorBody != null && floorBody.IsStatic)
            {
                float force = Body.Mass * deltaVel * 100.0f;
                floorBody.SetActivationState(true);
                floorBody.AddForce(JVector.UnitY * force, hitPoint);
            }
        }
    }

    public void SetLinearInput(JVector deltaMove)
    {
        if (!CanJump(out _, out _))
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