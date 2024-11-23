// NOTE: The ray cast car demo is a copied and slightly modified version
//       of the vehicle example from the great JigLib. License follows.

/*
Copyright (c) 2007 Danny Chapman
http://www.rowlhouse.co.uk

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
claim that you wrote the original software. If you use this software
in a product, an acknowledgment in the product documentation would be
appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be
misrepresented as being the original software.

3. This notice may not be removed or altered from any source
distribution.
*/

using System;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

#if USE_DOUBLE_PRECISION
using Real = System.Double;
using MathR = System.Math;
#else
using Real = System.Single;
using MathR = System.MathF;
#endif

namespace JitterDemo;

/// <summary>
/// A wheel which adds drive forces to a body.
/// Can be used to create a vehicle.
/// </summary>
public class Wheel
{
    private readonly World world;

    private readonly RigidBody car;

    private Real displacement, upSpeed, lastDisplacement;
    private bool onFloor;
    private Real driveTorque;

    private Real angVel;

    /// used to estimate the friction
    private Real angVelForGrip;

    private Real torque;

    private readonly DynamicTree.RayCastFilterPre rayCast;

    /// <summary>
    /// Sets or gets the current steering angle of
    /// the wheel in degrees.
    /// </summary>
    public Real SteerAngle { get; set; }

    /// <summary>
    /// Gets the current rotation of the wheel in degrees.
    /// </summary>
    public Real WheelRotation { get; private set; }

    /// <summary>
    /// The damping factor of the supension spring.
    /// </summary>
    public Real Damping { get; set; }

    /// <summary>
    /// The supension spring.
    /// </summary>
    public Real Spring { get; set; }

    /// <summary>
    /// Inertia of the wheel.
    /// </summary>
    public Real Inertia { get; set; }

    /// <summary>
    /// The wheel radius.
    /// </summary>
    public Real Radius { get; set; }

    /// <summary>
    /// The friction of the car in the side direction.
    /// </summary>
    public Real SideFriction { get; set; }

    /// <summary>
    /// Friction of the car in forward direction.
    /// </summary>
    public Real ForwardFriction { get; set; }

    /// <summary>
    /// The length of the suspension spring.
    /// </summary>
    public Real WheelTravel { get; set; }

    /// <summary>
    /// If set to true the wheel blocks.
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// The highest possible velocity of the wheel.
    /// </summary>
    public Real MaximumAngularVelocity { get; set; }

    /// <summary>
    /// The number of rays used for this wheel.
    /// </summary>
    public int NumberOfRays { get; set; }

    /// <summary>
    /// The position of the wheel in body space.
    /// </summary>
    public JVector Position { get; set; }

    public Real AngularVelocity => angVel;

    public readonly JVector Up = JVector.UnitY;

    /// <summary>
    /// Creates a new instance of the Wheel class.
    /// </summary>
    /// <param name="world">The world.</param>
    /// <param name="car">The RigidBody on which to apply the wheel forces.</param>
    /// <param name="position">The position of the wheel on the body (in body space).</param>
    /// <param name="radius">The wheel radius.</param>
    public Wheel(World world, RigidBody car, JVector position, Real radius)
    {
        this.world = world;
        this.car = car;
        Position = position;

        rayCast = RayCastCallback;

        // set some default values.
        SideFriction = 3.2f;
        ForwardFriction = 5.0f;
        Radius = radius;
        Inertia = 1.0f;
        WheelTravel = 0.2f;
        MaximumAngularVelocity = 200;
        NumberOfRays = 1;
    }

    /// <summary>
    /// Gets the position of the wheel in world space.
    /// </summary>
    /// <returns>The position of the wheel in world space.</returns>
    public JVector GetWheelCenter()
    {
        return Position + JVector.Transform(Up, car.Orientation) * displacement;
    }

    /// <summary>
    /// Adds drivetorque.
    /// </summary>
    /// <param name="torque">The amount of torque applied to this wheel.</param>
    public void AddTorque(Real torque)
    {
        driveTorque += torque;
    }

    public void PostStep(Real timeStep)
    {
        if (timeStep <= 0.0f) return;

        Real origAngVel = angVel;
        upSpeed = (displacement - lastDisplacement) / timeStep;

        if (Locked)
        {
            angVel = 0;
            torque = 0;
        }
        else
        {
            angVel += torque * timeStep / Inertia;
            torque = 0;

            if (!onFloor) driveTorque *= 0.1f;

            // prevent friction from reversing dir - todo do this better
            // by limiting the torque
            if ((origAngVel > angVelForGrip && angVel < angVelForGrip) ||
                (origAngVel < angVelForGrip && angVel > angVelForGrip))
                angVel = angVelForGrip;

            angVel += driveTorque * timeStep / Inertia;
            driveTorque = 0;

            Real maxAngVel = MaximumAngularVelocity;
            angVel = Math.Clamp(angVel, -maxAngVel, maxAngVel);

            WheelRotation += timeStep * angVel;
        }
    }

    public void PreStep(Real timeStep)
    {
        // var dr = Playground.Instance.DebugRenderer;

        JVector force = JVector.Zero;
        lastDisplacement = displacement;
        displacement = 0.0f;

        Real vel = car.Velocity.Length();

        JVector worldPos = car.Position + JVector.Transform(Position, car.Orientation);
        JVector worldAxis = JVector.Transform(Up, car.Orientation);


        JVector forward = JVector.Transform(-JVector.UnitZ, car.Orientation); //-car.Orientation.GetColumn(2);
        JVector wheelFwd = JVector.Transform(forward, JMatrix.CreateRotationMatrix(worldAxis, SteerAngle));

        JVector wheelLeft = JVector.Cross(worldAxis, wheelFwd);
        wheelLeft.Normalize();

        JVector wheelUp = JVector.Cross(wheelFwd, wheelLeft);

        Real rayLen = 2.0f * Radius + WheelTravel;

        JVector wheelRayEnd = worldPos - Radius * worldAxis;
        JVector wheelRayOrigin = wheelRayEnd + rayLen * worldAxis;
        JVector wheelRayDelta = wheelRayEnd - wheelRayOrigin;

        Real deltaFwd = 2.0f * Radius / (NumberOfRays + 1);
        Real deltaFwdStart = deltaFwd;

        onFloor = false;

        JVector groundNormal = JVector.Zero;
        JVector groundPos = JVector.Zero;
        Real deepestFrac = Real.MaxValue;
        RigidBody worldBody = null!;

        for (int i = 0; i < NumberOfRays; i++)
        {
            Real distFwd = deltaFwdStart + i * deltaFwd - Radius;
            Real zOffset = Radius * (1.0f - (Real)Math.Cos(Math.PI / 2.0f * (distFwd / Radius)));

            JVector newOrigin = wheelRayOrigin + distFwd * wheelFwd + zOffset * wheelUp;

            RigidBody body;

            bool result = world.DynamicTree.RayCast(newOrigin, wheelRayDelta,
                rayCast, null, out IDynamicTreeProxy? shape, out JVector normal, out Real frac);

            // Debug Rendering
            // dr.PushPoint(DebugRenderer.Color.Green, Conversion.FromJitter(newOrigin), 0.2f);
            // dr.PushPoint(DebugRenderer.Color.Red, Conversion.FromJitter(newOrigin + wheelRayDelta), 0.2f);

            JVector minBox = worldPos - new JVector(Radius);
            JVector maxBox = worldPos + new JVector(Radius);

            // dr.PushBox(DebugRenderer.Color.Green, Conversion.FromJitter(minBox), Conversion.FromJitter(maxBox));

            if (result && frac <= 1.0f)
            {
                // shape must be RigidBodyShape since we filter out other ray tests
                body = (shape as RigidBodyShape)!.RigidBody;

                if (frac < deepestFrac)
                {
                    deepestFrac = frac;
                    groundPos = newOrigin + frac * wheelRayDelta;
                    worldBody = body;
                    groundNormal = normal;
                }

                onFloor = true;
            }
        }

        if (!onFloor) return;

        // dr.PushPoint(DebugRenderer.Color.Green, Conversion.FromJitter(groundPos), 0.2f);

        if (groundNormal.LengthSquared() > 0.0f) groundNormal.Normalize();

        // System.Diagnostics.Debug.WriteLine(groundPos.ToString());

        displacement = rayLen * (1.0f - deepestFrac);
        displacement = Math.Clamp(displacement, 0.0f, WheelTravel);

        Real displacementForceMag = displacement * Spring;

        // reduce force when suspension is par to ground
        displacementForceMag *= JVector.Dot(groundNormal, worldAxis);

        // apply damping
        Real dampingForceMag = upSpeed * Damping;

        Real totalForceMag = displacementForceMag + dampingForceMag;

        if (totalForceMag < 0.0f) totalForceMag = 0.0f;

        JVector extraForce = totalForceMag * worldAxis;

        force += extraForce;

        // side-slip friction and drive force. Work out wheel- and floor-relative coordinate frame
        JVector groundUp = groundNormal;
        JVector groundLeft = JVector.Cross(groundNormal, wheelFwd);
        if (groundLeft.LengthSquared() > 0.0f) groundLeft.Normalize();

        JVector groundFwd = JVector.Cross(groundLeft, groundUp);

        JVector wheelPointVel = car.Velocity +
                                JVector.Cross(car.AngularVelocity, JVector.Transform(Position, car.Orientation));

        // rimVel=(wxr)*v
        JVector rimVel = angVel * JVector.Cross(wheelLeft, groundPos - worldPos);
        wheelPointVel += rimVel;

        if (worldBody == null) throw new Exception("car: world body is null.");

        JVector worldVel = worldBody.Velocity +
                           JVector.Cross(worldBody.AngularVelocity, groundPos - worldBody.Position);

        wheelPointVel -= worldVel;

        // sideways forces
        Real noslipVel = 0.2f;
        Real slipVel = 0.4f;
        Real slipFactor = 0.7f;

        Real smallVel = 3.0f;
        Real friction = SideFriction;

        Real sideVel = JVector.Dot(wheelPointVel, groundLeft);

        if (sideVel > slipVel || sideVel < -slipVel)
        {
            friction *= slipFactor;
        }
        else if (sideVel > noslipVel || sideVel < -noslipVel)
        {
            friction *= 1.0f - (1.0f - slipFactor) * (Math.Abs(sideVel) - noslipVel) / (slipVel - noslipVel);
        }

        if (sideVel < 0.0f)
            friction *= -1.0f;

        if (Math.Abs(sideVel) < smallVel)
            friction *= Math.Abs(sideVel) / smallVel;

        Real sideForce = -friction * totalForceMag;

        extraForce = sideForce * groundLeft;
        force += extraForce;

        // fwd/back forces
        friction = ForwardFriction;
        Real fwdVel = JVector.Dot(wheelPointVel, groundFwd);

        if (fwdVel > slipVel || fwdVel < -slipVel)
        {
            friction *= slipFactor;
        }
        else if (fwdVel > noslipVel || fwdVel < -noslipVel)
        {
            friction *= 1.0f - (1.0f - slipFactor) * (Math.Abs(fwdVel) - noslipVel) / (slipVel - noslipVel);
        }

        if (fwdVel < 0.0f)
            friction *= -1.0f;

        if (Math.Abs(fwdVel) < smallVel)
            friction *= Math.Abs(fwdVel) / smallVel;

        Real fwdForce = -friction * totalForceMag;

        extraForce = fwdForce * groundFwd;
        force += extraForce;

        // fwd force also spins the wheel
        JVector wheelCentreVel = car.Velocity +
                                 JVector.Cross(car.AngularVelocity, JVector.Transform(Position, car.Orientation));

        angVelForGrip = JVector.Dot(wheelCentreVel, groundFwd) / Radius;
        torque += -fwdForce * Radius;

        // add force to car
        car.AddForce(force, groundPos);

        RenderWindow.Instance.DebugRenderer.PushPoint(DebugRenderer.Color.White, Conversion.FromJitter(groundPos), 0.2f);

        // add force to the world
        if (!worldBody.IsStatic)
        {
            const Real maxOtherBodyAcc = 500.0f;
            Real maxOtherBodyForce = maxOtherBodyAcc * worldBody.Mass;

            if (force.LengthSquared() > (maxOtherBodyForce * maxOtherBodyForce))
                force *= maxOtherBodyForce / force.Length();

            worldBody.SetActivationState(true);

            worldBody.AddForce(force * -1, groundPos);
        }
    }

    private bool RayCastCallback(IDynamicTreeProxy shape)
    {
        if (shape is not RigidBodyShape rbs) return false;
        return rbs.RigidBody != car;
    }
}