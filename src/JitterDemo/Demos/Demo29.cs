using System;
using System.Collections.Generic;
using JitterDemo.Renderer.OpenGL;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public class IgnoreGearCollisionFilter : IBroadPhaseFilter
{
    public class GearMarker
    {
    }

    public static GearMarker Marker { get; } = new();

    public bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        if (proxyA is not RigidBodyShape shapeA || proxyB is not RigidBodyShape shapeB) return false;
        return !(shapeA.RigidBody.Tag is GearMarker && shapeB.RigidBody.Tag is GearMarker);
    }
}

public class Demo29 : IDemo, ICleanDemo, IDrawUpdate
{
    public string Name => "Gears";
    public string Description => "Interlocking gear bodies coupled via constraints.";

    private readonly List<GearCoupling> couplings = [];

    private RigidBody CreateGear(World world, JVector position)
    {
        var gear = world.CreateRigidBody();
        gear.AddShape(new TransformedShape(new CylinderShape(0.2f, 2.0f), (+0.0f, -0.1f, +0.0f)));
        gear.AddShape(new TransformedShape(new CylinderShape(0.2f, 1.0f), (+0.0f, +0.1f, +0.0f)));
        gear.AddShape(new TransformedShape(new CylinderShape(0.8f, 0.1f), (-0.8f, +0.4f, +0.0f)));

        gear.Orientation = JQuaternion.CreateRotationX((float)JAngle.FromDegree(90));
        gear.Position = position;
        gear.AffectedByGravity = false;
        gear.Tag = IgnoreGearCollisionFilter.Marker;

        return gear;
    }

    private void CreateGears(World world)
    {
        var g0 = CreateGear(world, (-6, 3, 0.0f));
        var g1 = CreateGear(world, (-3, 3, 0.2f));
        var g2 = CreateGear(world, (+0, 3, 0.4f));
        var g3 = CreateGear(world, (+3, 3, 0.2f));
        var g4 = CreateGear(world, (+6, 3, 0.0f));

        var gc0 = new GearCoupling(world, g0, g1, JVector.UnitZ, JVector.UnitZ, g0.Position + (1,0,0));
        var gc1 = new GearCoupling(world, g1, g2, JVector.UnitZ, JVector.UnitZ, g1.Position + (1,0,0));
        var gc2 = new GearCoupling(world, g2, g3, JVector.UnitZ, JVector.UnitZ, g3.Position - (1,0,0));
        var gc3 = new GearCoupling(world, g3, g4, JVector.UnitZ, JVector.UnitZ, g4.Position - (1,0,0));

        couplings.AddRange([gc0, gc1, gc2, gc3]);
    }

    public void Build(Playground pg, World world)
    {
        pg.AddFloor();

        world.BroadPhaseFilter = new IgnoreGearCollisionFilter();
        CreateGears(world);

        world.SolverIterations = (6, 2);
        world.SubstepCount = 3;
    }

    public void DrawUpdate()
    {
        var pg = (Playground)RenderWindow.Instance;

        foreach (var coupling in couplings)
        {
            pg.DebugRenderer.PushPoint(DebugRenderer.Color.Green, Conversion.FromJitter(coupling.ContactPoint));
        }
    }

    public void CleanUp()
    {
        foreach (var coupling in couplings)
        {
            coupling.Remove();
        }

        couplings.Clear();
    }
}