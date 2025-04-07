using System;
using Jitter2;
using Jitter2.Collision;
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

    private readonly double capsuleHalfHeight;
    private readonly World world;

    public Player(World world, JVector position)
    {
        Body = world.CreateRigidBody();
        var cs = new CapsuleShape();
        Body.AddShape(cs);
        Body.Position = position;

        // disable velocity damping
        Body.Damping = (0, 0);

        capsuleHalfHeight = cs.Radius + cs.Length * 0.5d;

        this.world = world;

        // Disable deactivation
        Body.DeactivationTime = TimeSpan.MaxValue;

        // Add two arms - to be able to visually tell how the player is orientated
        var arm1 = new TransformedShape(new BoxShape(0.2d, 0.8d, 0.2d), new JVector(+0.5d, 0.3d, 0));
        var arm2 = new TransformedShape(new BoxShape(0.2d, 0.8d, 0.2d), new JVector(-0.5d, 0.3d, 0));

        // Add the shapes without recalculating mass and inertia, we take mass and inertia from the capsule
        // shape we added before.
        Body.AddShape(arm1, false);
        Body.AddShape(arm2, false);

        // Make the capsule stand upright, but able to rotate 360 degrees.
        var ur = world.CreateConstraint<HingeAngle>(Body, world.NullBody);
        ur.Initialize(JVector.UnitY, AngularLimit.Full);

        // Add some friction
        Body.Friction = 0.8d;

        // An angular motor for turning.
        AngularMovement = world.CreateConstraint<AngularMotor>(Body, world.NullBody);
        AngularMovement.Initialize(JVector.UnitY, JVector.UnitY);
        AngularMovement.MaximumForce = 1000;
    }

    public void SetAngularInput(double rotate)
    {
        AngularMovement.TargetVelocity = rotate;
    }

    private DynamicTree.RayCastFilterPre? preFilter;

    public bool FilterShape(IDynamicTreeProxy shape)
    {
        if (shape is RigidBodyShape rbs)
        {
            if (rbs.RigidBody == Body) return false;
        }

        return true;
    }

    private bool CanJump(out RigidBody? floor, out JVector hitPoint)
    {
        preFilter ??= FilterShape;

        foreach (var contact in Body.Contacts)
        {
            // go through all contacts of the capsule (player)

            var cd = contact.Handle.Data;
            int numContacts = 0;
            hitPoint = JVector.Zero;

            // a contact may contain up to four contact points,
            // see which ones were used during the last step.
            uint mask = cd.UsageMask >> 4;

            if (contact.Body1 == Body)
            {
                if ((mask & ContactData.MaskContact0) != 0)
                {
                    hitPoint += cd.Contact0.RelativePosition1;
                    numContacts++;
                }

                if ((mask & ContactData.MaskContact1) != 0)
                {
                    hitPoint += cd.Contact1.RelativePosition1;
                    numContacts++;
                }

                if ((mask & ContactData.MaskContact2) != 0)
                {
                    hitPoint += cd.Contact2.RelativePosition1;
                    numContacts++;
                }

                if ((mask & ContactData.MaskContact3) != 0)
                {
                    hitPoint += cd.Contact3.RelativePosition1;
                    numContacts++;
                }

                floor = contact.Body2;
            }
            else
            {
                if ((mask & ContactData.MaskContact0) != 0)
                {
                    hitPoint += cd.Contact0.RelativePosition2;
                    numContacts++;
                }

                if ((mask & ContactData.MaskContact1) != 0)
                {
                    hitPoint += cd.Contact1.RelativePosition2;
                    numContacts++;
                }

                if ((mask & ContactData.MaskContact2) != 0)
                {
                    hitPoint += cd.Contact2.RelativePosition2;
                    numContacts++;
                }

                if ((mask & ContactData.MaskContact3) != 0)
                {
                    hitPoint += cd.Contact3.RelativePosition2;
                    numContacts++;
                }

                floor = contact.Body1;
            }

            if (numContacts == 0) continue;

            // divide the result by the number of contact points to get the "center" of the contact
            hitPoint *= (1.0d / numContacts);

            // check if the hitpoint is on the players base
            if (hitPoint.Y <= -0.8d) return true;
        }

        hitPoint = JVector.Zero;
        floor = null;

        return false;

        /*
        // ...or the more traditional way of using a raycast

        bool hit = world.RayCast(Body.Position, -JVector.UnitY, preFilter, null,
            out floor, out JVector normal, out float lambda);

        float delta = lambda - capsuleHalfHeight;

        hitPoint = Body.Position - JVector.UnitY * lambda;
        return (hit && delta < 0.04d && floor != null);
        */
    }

    public void Jump()
    {
        if (CanJump(out RigidBody? floorBody, out JVector hitPoint))
        {
            double newYVel = 5.0d;

            if (floorBody != null && floorBody != null)
            {
                newYVel += floorBody.Velocity.Y;
            }

            double deltaVel = Body.Velocity.Y - newYVel;

            Body.Velocity = new JVector(Body.Velocity.X, newYVel, Body.Velocity.Z);

            if (floorBody != null && floorBody.IsStatic)
            {
                double force = Body.Mass * deltaVel * 100.0d;
                floorBody.SetActivationState(true);
                floorBody.AddForce(JVector.UnitY * force, hitPoint);
            }
        }
    }

    public void SetLinearInput(JVector deltaMove)
    {
        if (!CanJump(out var floor, out JVector hitpoint))
        {
            return;
        }

        deltaMove *= 3.0d;

        double deltaMoveLen = deltaMove.Length();

        JVector bodyVel = Body.Velocity;
        bodyVel.Y = 0;

        double bodyVelLen = bodyVel.Length();

        if (deltaMoveLen > 0.01d)
        {
            if (bodyVelLen < 5f)
            {
                var force = JVector.Transform(deltaMove, Body.Orientation) * 10.0d;

                Body.AddForce(force);

                // follow Newton's law (for once) and add a force
                // with equal magnitude in the opposite direction.
                floor!.AddForce(-force, Body.Position + hitpoint);
            }
        }

    }
}