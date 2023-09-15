// NOTE: The raycast car demo is a copied and slightly modified version
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
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace JitterDemo;

/// <summary>
/// Creates the Jitter default car with 4 wheels. To create a custom car
/// use the Wheel class and add it to a body.
/// </summary>
public class RaycastCar
{
    // the default car has 4 wheels
    private readonly World world;

    public RigidBody Body { get; }

    private float destSteering;
    private float destAccelerate;
    private float steering;
    private float accelerate;

    /// <summary>
    /// The maximum steering angle in degrees
    /// for both front wheels
    /// </summary>
    public float SteerAngle { get; set; }

    /// <summary>
    /// The maximum torque which is applied to the
    /// car when accelerating.
    /// </summary>
    public float DriveTorque { get; set; }

    /// <summary>
    /// Lower/Higher the acceleration of the car.
    /// </summary>
    public float AccelerationRate { get; set; }

    /// <summary>
    /// Lower/Higher the steering rate of the car.
    /// </summary>
    public float SteerRate { get; set; }

    // don't damp perfect, allow some bounciness.
    private const float dampingFrac = 0.8f;
    private const float springFrac = 0.45f;

    /// <summary>
    /// Initializes a new instance of the DefaultCar class.
    /// </summary>
    /// <param name="world">The world the car should be in.</param>
    /// <param name="shape">The shape of the car. Recommend is a box shape.</param>
    public RaycastCar(World world)
    {
        this.world = world;

        // set some default values
        AccelerationRate = 10f;
        SteerAngle = (float)JAngle.FromDegree(40.0f);
        DriveTorque = 340.0f;
        SteerRate = 5.0f;

        Body = world.CreateRigidBody();

        TransformedShape tfs1 = new(new BoxShape(3.1f, 1.4f, 8f), new JVector(0, 0.6f, 0));
        TransformedShape tfs2 = new(new BoxShape(2.4f, 0.8f, 5f), new JVector(0.0f, 1.7f, 1.1f));

        Body.AddShape(tfs1);
        Body.AddShape(tfs2);

        float mass = 100.0f;
        JVector sides = new JVector(3.1f, 1.0f, 8.0f);

        float Ixx = (1.0f / 12.0f) * mass * (sides.Y * sides.Y + sides.Z * sides.Z);
        float Iyy = (1.0f / 12.0f) * mass * (sides.X * sides.X + sides.Z * sides.Z);
        float Izz = (1.0f / 12.0f) * mass * (sides.X * sides.X + sides.Y * sides.Y);

        JMatrix inertia = new JMatrix(Ixx, 0, 0, 0, Iyy, 0, 0, 0, Izz);
        JVector r = new JVector(0, 0, 0);
        inertia += mass * r.LengthSquared() * JMatrix.Identity - mass * JVector.Outer(r, r);

        Body.Position = new JVector(0, 0.5f, -4);
        Body.SetMassInertia(inertia, mass);

        Body.Damping = (0.9999f, 0.9999f);

        // create default wheels
        Wheels[0] = new Wheel(world, Body, new JVector(-1.3f, 0.1f, -2.5f), 0.60f);
        Wheels[1] = new Wheel(world, Body, new JVector(+1.3f, 0.1f, -2.5f), 0.60f);
        Wheels[2] = new Wheel(world, Body, new JVector(-1.3f, 0.1f, +2.4f), 0.60f);
        Wheels[3] = new Wheel(world, Body, new JVector(+1.3f, 0.1f, +2.4f), 0.60f);

        AdjustWheelValues();
    }

    /// <summary>
    /// This recalculates the inertia, damping and spring of all wheels based
    /// on the car mass, the wheel radius and the gravity. Should be called
    /// after manipulating wheel data.
    /// </summary>
    public void AdjustWheelValues()
    {
        float mass = Body.Mass / 4.0f;
        float wheelMass = Body.Mass * 0.03f;

        foreach (Wheel w in Wheels)
        {
            w.Inertia = 0.5f * (w.Radius * w.Radius) * wheelMass;
            w.Spring = mass * world.Gravity.Length() / (w.WheelTravel * springFrac);
            w.Damping = 2.0f * (float)Math.Sqrt(w.Spring * Body.Mass) * 0.25f * dampingFrac;
        }
    }

    /// <summary>
    /// Access the wheels.
    /// </summary>
    public Wheel[] Wheels { get; } = new Wheel[4];

    /// <summary>
    /// Set input values for the car.
    /// </summary>
    /// <param name="accelerate">
    /// A value between -1 and 1 (other values get clamped). Adjust
    /// the maximum speed of the car by setting <see cref="DriveTorque" />. The maximum acceleration is adjusted
    /// by setting <see cref="AccelerationRate" />.
    /// </param>
    /// <param name="steer">
    /// A value between -1 and 1 (other values get clamped). Adjust
    /// the maximum steer angle by setting <see cref="SteerAngle" />. The speed of steering
    /// change is adjusted by <see cref="SteerRate" />.
    /// </param>
    public void SetInput(float accelerate, float steer)
    {
        destAccelerate = accelerate;
        destSteering = steer;
    }

    public void Step(float timestep)
    {
        foreach (Wheel w in Wheels) w.PreStep(timestep);

        float deltaAccelerate = timestep * AccelerationRate;

        float deltaSteering = timestep * SteerRate;

        float dAccelerate = destAccelerate - accelerate;
        dAccelerate = Math.Clamp(dAccelerate, -deltaAccelerate, deltaAccelerate);

        float dSteering = destSteering - steering;
        dSteering = Math.Clamp(dSteering, -deltaSteering, deltaSteering);

        accelerate += dAccelerate;
        steering += dSteering;

        float maxTorque = DriveTorque * 0.5f;

        foreach (Wheel w in Wheels)
        {
            w.AddTorque(maxTorque * accelerate);
            
            if (destAccelerate == 0.0f && w.AngularVelocity < 0.8f)
            {
                // if the car is slow enough and destAccelerate is zero
                // apply torque in the opposite direction of the angular velocity
                // to make the car come to a complete halt.
                w.AddTorque(-w.AngularVelocity);
            }
        }

        float alpha = SteerAngle * steering;

        Wheels[0].SteerAngle = alpha;
        Wheels[1].SteerAngle = alpha;

        foreach (Wheel w in Wheels)
        {
            w.PostStep(timestep);
        }
    }
}