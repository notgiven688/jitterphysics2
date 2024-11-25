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
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace JitterDemo;

/// <summary>
/// Creates the Jitter default car with 4 wheels. To create a custom car
/// use the Wheel class and add it to a body.
/// </summary>
public class RayCastCar
{
    // the default car has 4 wheels
    private readonly World world;

    public RigidBody Body { get; }

    private double destSteering;
    private double destAccelerate;
    private double steering;
    private double accelerate;

    /// <summary>
    /// The maximum steering angle in degrees
    /// for both front wheels
    /// </summary>
    public double SteerAngle { get; set; }

    /// <summary>
    /// The maximum torque which is applied to the
    /// car when accelerating.
    /// </summary>
    public double DriveTorque { get; set; }

    /// <summary>
    /// Lower/Higher the acceleration of the car.
    /// </summary>
    public double AccelerationRate { get; set; }

    /// <summary>
    /// Lower/Higher the steering rate of the car.
    /// </summary>
    public double SteerRate { get; set; }

    // don't damp perfect, allow some bounciness.
    private const double dampingFrac = 0.8d;
    private const double springFrac = 0.45d;

    /// <summary>
    /// Initializes a new instance of the DefaultCar class.
    /// </summary>
    /// <param name="world">The world the car should be in.</param>
    /// <param name="shape">The shape of the car. Recommend is a box shape.</param>
    public RayCastCar(World world)
    {
        this.world = world;

        // set some default values
        AccelerationRate = 10f;
        SteerAngle = (double)JAngle.FromDegree(40.0d);
        DriveTorque = 340.0d;
        SteerRate = 5.0d;

        Body = world.CreateRigidBody();

        TransformedShape tfs1 = new(new BoxShape(3.1d, 1.4d, 8f), new JVector(0, 0.6d, 0));
        TransformedShape tfs2 = new(new BoxShape(2.4d, 0.8d, 5f), new JVector(0.0d, 1.7d, 1.1d));

        Body.AddShape(tfs1);
        Body.AddShape(tfs2);

        double mass = 100.0d;
        JVector sides = new JVector(3.1d, 1.0d, 8.0d);

        double Ixx = (1.0d / 12.0d) * mass * (sides.Y * sides.Y + sides.Z * sides.Z);
        double Iyy = (1.0d / 12.0d) * mass * (sides.X * sides.X + sides.Z * sides.Z);
        double Izz = (1.0d / 12.0d) * mass * (sides.X * sides.X + sides.Y * sides.Y);

        JMatrix inertia = new JMatrix(Ixx, 0, 0, 0, Iyy, 0, 0, 0, Izz);
        JVector r = new JVector(0, 0, 0);
        inertia += mass * r.LengthSquared() * JMatrix.Identity - mass * JVector.Outer(r, r);

        Body.Position = new JVector(0, 0.5d, -4);
        Body.SetMassInertia(inertia, mass);

        Body.Damping = (0.0001d, 0.0001d);

        // create default wheels
        Wheels[0] = new Wheel(world, Body, new JVector(-1.3d, 0.1d, -2.5d), 0.60d);
        Wheels[1] = new Wheel(world, Body, new JVector(+1.3d, 0.1d, -2.5d), 0.60d);
        Wheels[2] = new Wheel(world, Body, new JVector(-1.3d, 0.1d, +2.4d), 0.60d);
        Wheels[3] = new Wheel(world, Body, new JVector(+1.3d, 0.1d, +2.4d), 0.60d);

        AdjustWheelValues();
    }

    /// <summary>
    /// This recalculates the inertia, damping and spring of all wheels based
    /// on the car mass, the wheel radius and the gravity. Should be called
    /// after manipulating wheel data.
    /// </summary>
    public void AdjustWheelValues()
    {
        double mass = Body.Mass / 4.0d;
        double wheelMass = Body.Mass * 0.03d;

        foreach (Wheel w in Wheels)
        {
            w.Inertia = 0.5d * (w.Radius * w.Radius) * wheelMass;
            w.Spring = mass * world.Gravity.Length() / (w.WheelTravel * springFrac);
            w.Damping = 2.0d * (double)Math.Sqrt(w.Spring * Body.Mass) * 0.25d * dampingFrac;
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
    public void SetInput(double accelerate, double steer)
    {
        destAccelerate = accelerate;
        destSteering = steer;
    }

    public void Step(double timestep)
    {
        foreach (Wheel w in Wheels) w.PreStep(timestep);

        double deltaAccelerate = timestep * AccelerationRate;

        double deltaSteering = timestep * SteerRate;

        double dAccelerate = destAccelerate - accelerate;
        dAccelerate = Math.Clamp(dAccelerate, -deltaAccelerate, deltaAccelerate);

        double dSteering = destSteering - steering;
        dSteering = Math.Clamp(dSteering, -deltaSteering, deltaSteering);

        accelerate += dAccelerate;
        steering += dSteering;

        double maxTorque = DriveTorque * 0.5d;

        foreach (Wheel w in Wheels)
        {
            w.AddTorque(maxTorque * accelerate);

            if (destAccelerate == 0.0d && w.AngularVelocity < 0.8d)
            {
                // if the car is slow enough and destAccelerate is zero
                // apply torque in the opposite direction of the angular velocity
                // to make the car come to a complete halt.
                w.AddTorque(-w.AngularVelocity);
            }
        }

        double alpha = SteerAngle * steering;

        Wheels[0].SteerAngle = alpha;
        Wheels[1].SteerAngle = alpha;

        foreach (Wheel w in Wheels)
        {
            w.PostStep(timestep);
        }
    }
}