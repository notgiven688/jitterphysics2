using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class Demo22 : IDemo, ICleanDemo
{
    public string Name => "Conveyor Belt";

    private Playground pg = null!;
    private World world = null!;

    // We need to track time manually for the physics steps
    // to ensure the belt moves perfectly in sync with the solver.
    private double physicsTime = 0.0;

    private struct BeltPlank
    {
        public RigidBody Body;
        public float DistanceOffset;
    }

    private List<BeltPlank> planks = new();

    private static class Curve
    {
        public const float Speed = 2f;
        public const float StraightLength = 12.0f;
        public const float Radius = 6.0f;

        public static float TotalLength => (StraightLength * 2.0f) + (MathF.PI * Radius * 2.0f);

        public static void GetState(float distance, out JVector pos, out JVector vel, out float angVelY)
        {
            distance = distance % TotalLength;
            if (distance < 0) distance += TotalLength;

            float curveLen = MathF.PI * Radius;

            // 1. Bottom Straight
            if (distance < StraightLength)
            {
                float t = distance;
                pos = new JVector(-StraightLength * 0.5f + t, 4.0f, -Radius);
                vel = new JVector(Speed, 0, 0);
                angVelY = 0;
            }
            // 2. Right Turn
            else if (distance < StraightLength + curveLen)
            {
                float t = distance - StraightLength;
                float angle = -MathF.PI * 0.5f + (t / Radius);
                pos = new JVector(StraightLength * 0.5f + MathF.Cos(angle) * Radius, 4.0f, MathF.Sin(angle) * Radius);
                vel = new JVector(-MathF.Sin(angle), 0, MathF.Cos(angle)) * Speed;
                angVelY = Speed / Radius;
            }
            // 3. Top Straight
            else if (distance < (StraightLength * 2.0f) + curveLen)
            {
                float t = distance - (StraightLength + curveLen);
                pos = new JVector(StraightLength * 0.5f - t, 4.0f, Radius);
                vel = new JVector(-Speed, 0, 0);
                angVelY = 0;
            }
            // 4. Left Turn
            else
            {
                float t = distance - ((StraightLength * 2.0f) + curveLen);
                float angle = MathF.PI * 0.5f + (t / Radius);
                pos = new JVector(-StraightLength * 0.5f + MathF.Cos(angle) * Radius, 4.0f, MathF.Sin(angle) * Radius);
                vel = new JVector(-MathF.Sin(angle), 0, MathF.Cos(angle)) * Speed;
                angVelY = Speed / Radius;
            }
        }
    }

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;
        pg.ResetScene(true);
        planks.Clear();
        physicsTime = 0;

        // Subscribe to the physics step event
        world.PreSubStep += OnPreStep;

        float plankWidth = 0.6f;
        int plankCount = (int)(Curve.TotalLength / plankWidth);
        float distStep = Curve.TotalLength / plankCount;

        for (int i = 0; i < plankCount; i++)
        {
            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape(1.8f, 0.1f, 0.55f));
            body.MotionType = MotionType.Kinematic;
            body.Friction = 1.0f;

            float dist = i * distStep;
            Curve.GetState(dist, out JVector pos, out JVector vel, out float w);

            body.Position = pos;

            // Align initial orientation
            JVector fwd = JVector.Normalize(vel);
            JVector up = JVector.UnitY;
            JVector right = JVector.Cross(fwd, up);
            JMatrix ori = new JMatrix(right.X, up.X, fwd.X, right.Y, up.Y, fwd.Y, right.Z, up.Z, fwd.Z);
            body.Orientation = JQuaternion.CreateFromMatrix(ori);

            planks.Add(new BeltPlank { Body = body, DistanceOffset = dist });
        }

        // Debris
        for (int i = 0; i < 8; i++)
        {
            var box = world.CreateRigidBody();
            box.AddShape(new BoxShape(1));
            box.Position = new JVector(-5 + i * 1.5f, 6, -Curve.Radius);
        }

        var floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(100, 1, 100));
        floor.Position = new JVector(0, -5, 0);
        floor.MotionType = MotionType.Static;
    }

    // Called automatically by Jitter before every physics sub-step
    private void OnPreStep(float dt)
    {
        physicsTime += dt;
        float globalDist = (float)physicsTime * Curve.Speed;

        foreach (var plank in planks)
        {
            float d = globalDist + plank.DistanceOffset;

            Curve.GetState(d, out JVector targetPos, out JVector targetVel, out float targetAngVelY);

            // Motion Control Logic
            //
            // We want the platform to follow a path p(t) and orientation. Setting the position/orientation
            // directly results in teleportation, which prevents proper physics simulation.
            //
            // Instead, we use a control scheme that combines Feed-Forward (target velocity)
            // with a Proportional Controller (position error correction) to drive the body velocity v(t)
            // towards the target:
            //
            // v(t) = alpha * (k(t) - x(t))
            //
            // where x(t) is the current position and alpha is a gain constant.
            // By choosing k(t) = p(t) + 1/alpha * p'(t), the differential equation solves to:
            //
            // x(t) = C * exp(-alpha * t) + p(t)
            //
            // This means the body's position x(t) exponentially converges to the target path p(t).
            //
            // We apply this same logic to both Linear Velocity (below) and Angular Velocity.
            plank.Body.Velocity = targetVel + (targetPos - plank.Body.Position) * 10.0f;

            JVector currentForward = plank.Body.Orientation.GetBasisZ();
            JVector targetForward = JVector.Normalize(targetVel);

            // Calculate angle sine error (Cross Product Y-component)
            float angleError = (currentForward.Z * targetForward.X - currentForward.X * targetForward.Z);

            float correction = angleError * 20.0f;
            plank.Body.AngularVelocity = new JVector(0, targetAngVelY + correction, 0);
        }
    }

    public void Draw()
    {
        // Only drawing logic remains here
        const int stepMax = 200;
        float totalLen = Curve.TotalLength;

        for (int step = 0; step < stepMax; step++)
        {
            float d1 = totalLen / stepMax * step;
            float d2 = totalLen / stepMax * (step + 1);
            Curve.GetState(d1, out JVector p1, out _, out _);
            Curve.GetState(d2, out JVector p2, out _, out _);
            pg.DebugRenderer.PushLine(DebugRenderer.Color.Green, Conversion.FromJitter(p1), Conversion.FromJitter(p2));
        }
    }

    // Important: Unsubscribe when the demo is switched to avoid memory leaks
    // or ghost logic running in the next demo.
    public void CleanUp()
    {
        if (world != null)
        {
            world.PreSubStep -= OnPreStep;
        }
    }
}