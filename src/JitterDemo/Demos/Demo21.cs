using System;
using Jitter2;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public class Demo21 : IDemo
{
    public string Name => "Voxel Demo";
    VoxelGrid voxelGrid = null!;
    Playground pg = null!;
    World world = null!;

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

        voxelGrid.BuildJitterBoxes();
        world.NarrowPhaseFilter = new VoxelEdgeCollisionFilter();

        Console.WriteLine("Optimizing tree..");

        for (int i = 0; i < 3; i++)
        {
            Console.WriteLine($"({i+1}/3) Current cost: {(long)world.DynamicTree.CalculateCost()}");
            world.DynamicTree.Optimize(40);
        }

        Console.WriteLine("Done.");
    }

    public void Draw()
    {
        var cd = RenderWindow.Instance.CSMRenderer.GetInstance<Cube>();

        foreach(var voxel in voxelGrid.Voxels)
        {
            var pos = voxelGrid.PositionFromIndex(voxel);
            cd.PushMatrix(MatrixHelper.CreateTranslation(pos.X, pos.Y, pos.Z), 
                          ColorGenerator.GetColor(Math.Abs(voxel * voxel + voxel)));
        }
    }
}