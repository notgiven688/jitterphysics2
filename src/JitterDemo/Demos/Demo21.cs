using System;
using Jitter2;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

// This demo shows how to use the VoxelGrid class to create a voxel-based world with
// a custom edge collision filter.
//
// **What it does NOT show**: How to stream voxels to the physics engine dynamically.
// This can be achieved by custom collision, see the Heightmap and the Octree demos.

public class Demo21 : IDemo
{
    public string Name => "Voxel Demo";
    private VoxelGrid voxelGrid = null!;
    private Playground pg = null!;
    private World world = null!;
    private Player player = null!;

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene(false);

        voxelGrid = new VoxelGrid(world);

        // create a plane
        for (int i = 0; i < 100; i++)
        {
            for (int e = 0; e < 100; e++)
            {
                voxelGrid.AddVoxel(e, 0, i);
            }
        }

        // create a sphere
        for (int i = 0; i < 40; i++)
        {
            for (int e = 30; e < 70; e++)
            {
                for (int k = 30; k < 70; k++)
                {
                    if ((i - 20) * (i - 20) + (e - 50) * (e - 50) + (k - 50) * (k - 50) > 300) continue;
                    voxelGrid.AddVoxel(e, i, k);
                }
            }
        }

        var body = world.CreateRigidBody();
        body.MotionType = MotionType.Static;

        foreach (var voxel in voxelGrid.Voxels)
        {
            body.AddShape(new VoxelShape(voxelGrid, voxel), false);
        }

        body.SetMassInertia(JMatrix.Identity, 1.0f);
        body.SetActivationState(false);

        body.Tag = new RigidBodyTag(true);

        world.NarrowPhaseFilter = new VoxelEdgeCollisionFilter();

        Console.WriteLine($"Current cost: {(long)world.DynamicTree.CalculateCost()}");
        Console.WriteLine("Optimizing tree..");
        world.DynamicTree.Optimize(1000);
        Console.WriteLine($"Done. Cost: {(long)world.DynamicTree.CalculateCost()}");

        player = new Player(world, new JVector(50, 40, 50));
    }

    public void Draw()
    {
        var cd = RenderWindow.Instance.CSMRenderer.GetInstance<Cube>();

        foreach (var voxel in voxelGrid.Voxels)
        {
            var pos = voxelGrid.PositionFromIndex(voxel);
            cd.PushMatrix(MatrixHelper.CreateTranslation(pos.X, pos.Y, pos.Z),
                ColorGenerator.GetColor(Math.Abs(voxel * voxel + voxel)));
        }

        Keyboard kb = Keyboard.Instance;

        if (kb.IsKeyDown(Keyboard.Key.Left)) player.SetAngularInput(-1.0f);
        else if (kb.IsKeyDown(Keyboard.Key.Right)) player.SetAngularInput(1.0f);
        else player.SetAngularInput(0.0f);

        if (kb.IsKeyDown(Keyboard.Key.Up)) player.SetLinearInput(-JVector.UnitZ);
        else if (kb.IsKeyDown(Keyboard.Key.Down)) player.SetLinearInput(JVector.UnitZ);
        else player.SetLinearInput(JVector.Zero);

        if (kb.IsKeyDown(Keyboard.Key.LeftControl)) player.Jump();
    }
}