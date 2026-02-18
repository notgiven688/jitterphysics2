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
    public AngularMotor AngularMovement { get; }

    private readonly World world;

    public Player(World world, JVector position)
    {
        Body = world.CreateRigidBody();
        Body.AddShape(new CapsuleShape());
        Body.Position = position;

        // disable velocity damping
        Body.Damping = (0, 0);

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

        // Add some friction
        Body.Friction = 0.8f;

        // An angular motor for turning.
        AngularMovement = world.CreateConstraint<AngularMotor>(Body, world.NullBody);
        AngularMovement.Initialize(JVector.UnitY, JVector.UnitY);
        AngularMovement.MaximumForce = 1000;
    }

    public void SetAngularInput(float rotate)
    {
        AngularMovement.TargetVelocity = rotate;
    }

    private bool CanJump(out RigidBody? floor, out JVector hitPoint)
    {
        foreach (var contact in Body.Contacts)
        {
            // go through all contacts of the capsule (player)

            var cd = contact.Handle.Data;
            int numContacts = 0;
            hitPoint = JVector.Zero;

            // a contact may contain up to four contact points,
            // see which ones were used during the last step.
            uint mask = cd.UsageMask >> 4;

            bool isBody1 = contact.Body1 == Body;
            floor = isBody1 ? contact.Body2 : contact.Body1;

            if ((mask & ContactData.MaskContact0) != 0) { hitPoint += isBody1 ? cd.Contact0.RelativePosition1 : cd.Contact0.RelativePosition2; numContacts++; }
            if ((mask & ContactData.MaskContact1) != 0) { hitPoint += isBody1 ? cd.Contact1.RelativePosition1 : cd.Contact1.RelativePosition2; numContacts++; }
            if ((mask & ContactData.MaskContact2) != 0) { hitPoint += isBody1 ? cd.Contact2.RelativePosition1 : cd.Contact2.RelativePosition2; numContacts++; }
            if ((mask & ContactData.MaskContact3) != 0) { hitPoint += isBody1 ? cd.Contact3.RelativePosition1 : cd.Contact3.RelativePosition2; numContacts++; }

            if (numContacts == 0) continue;

            // divide the result by the number of contact points to get the "center" of the contact
            hitPoint *= (1.0f / numContacts);

            // check if the hitpoint is on the players base
            if (hitPoint.Y <= -0.8f) return true;
        }

        hitPoint = JVector.Zero;
        floor = null;

        return false;
    }

    public void Jump()
    {
        if (CanJump(out RigidBody? floorBody, out JVector hitPoint))
        {
            float newYVel = 5.0f;

            if (floorBody != null)
            {
                newYVel += floorBody.Velocity.Y;
            }

            float deltaVel = Body.Velocity.Y - newYVel;

            Body.Velocity = new JVector(Body.Velocity.X, newYVel, Body.Velocity.Z);

            if (floorBody != null && floorBody.MotionType == MotionType.Dynamic)
            {
                float force = Body.Mass * deltaVel * 100.0f;
                floorBody.SetActivationState(true);
                floorBody.AddForce(JVector.UnitY * force, floorBody.Position + hitPoint);
            }
        }
    }

    public void SetLinearInput(JVector deltaMove)
    {
        if (!CanJump(out var floor, out JVector hitpoint))
        {
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
                var force = JVector.Transform(deltaMove, Body.Orientation) * 10.0f;

                Body.AddForce(force);

                // follow Newton's law (for once) and add a force
                // with equal magnitude in the opposite direction.
                floor!.AddForce(-force, Body.Position + hitpoint);
            }
        }

    }
}