using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo36 : IDemo
{
    public string Name => "Trigger Volumes";

    public string Description => "A zero-gravity bubble: a trigger volume that switches off gravity and damps the " +
                                 "motion of every body inside it. Falling bodies that cross the bubble are caught " +
                                 "and float; drag one out with the mouse and it falls again. The volume is a custom " +
                                 "proxy in the dynamic tree, so it creates no contacts.";

    public string Controls => "R - Drop the bodies again";

    private static readonly Vector3 InsideColor = new(0.3f, 0.85f, 0.45f);

    private Playground pg = null!;
    private World world = null!;
    private TriggerVolume bubble = null!;

    private readonly List<RigidBody> bodies = [];
    private readonly Dictionary<RigidBody, (float Linear, float Angular)> savedDamping = [];

    public void Build(Playground pg)
    {
        this.pg = pg;
        world = pg.World;

        pg.AddFloor();

        world.BroadPhaseFilter = new TriggerFilter();

        bubble = new TriggerVolume(world, new JVector(0.0f, 3.5f, 0.0f), 2.0f);
        bubble.Entered += OnEntered;
        bubble.Left += OnLeft;

        for (int i = 0; i < 16; i++)
        {
            RigidBody body = world.CreateRigidBody();
            body.AddShape((i % 4) switch
            {
                0 => new BoxShape(0.5f),
                1 => new SphereShape(0.3f),
                2 => new CapsuleShape(0.2f, 0.5f),
                _ => new CylinderShape(0.4f, 0.3f)
            });
            body.Tag = new RigidBodyTag(false);
            bodies.Add(body);
        }

        Drop();
    }

    private void OnEntered(RigidBody body)
    {
        savedDamping[body] = body.Damping;
        body.AffectedByGravity = false;
        body.Damping = (0.04f, 0.04f);
        // Any body can enter, also one shot in with the playground's tools, which carries no tag.
        if (body.Tag is not RigidBodyTag tag) body.Tag = tag = new RigidBodyTag(false);
        tag.Color = InsideColor;
    }

    private void OnLeft(RigidBody body)
    {
        body.AffectedByGravity = true;
        if (savedDamping.Remove(body, out var damping)) body.Damping = damping;
        if (body.Tag is RigidBodyTag tag) tag.Color = null;
    }

    // A four by four grid above the bubble: the inner bodies fall through it, the outer ones miss.
    private void Drop()
    {
        for (int i = 0; i < bodies.Count; i++)
        {
            RigidBody body = bodies[i];
            body.Position = new JVector((i % 4 - 1.5f) * 1.6f, 9.0f + (i % 3) * 1.5f, (i / 4 - 1.5f) * 1.6f);
            body.Orientation = JQuaternion.CreateRotationX(i * 0.7f) * JQuaternion.CreateRotationZ(i * 0.3f);
            body.Velocity = JVector.Zero;
            body.AngularVelocity = new JVector(0.0f, 1.0f, 0.0f);
            body.SetActivationState(true);
        }
    }

    public void DrawUpdate()
    {
        if (Keyboard.Instance.KeyPressBegin(Keyboard.Key.R)) Drop();

        uint color = bubble.Count > 0 ? DebugDrawColors.Green : DebugDrawColors.White;
        RenderWindow.Instance.DebugRenderer.DrawSphere(bubble.Center, bubble.Radius, color);

        pg.DrawWorldLabel(bubble.Center + JVector.UnitY * (bubble.Radius + 0.4f), "Zero gravity",
            $"{bubble.Count} bodies inside");
    }

    public void CleanUp()
    {
        bubble.Remove();
        bodies.Clear();
        savedDamping.Clear();
    }
}
