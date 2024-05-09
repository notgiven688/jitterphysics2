using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace JitterDemo;

public class VoxelGrid
{
    public const int Size = 100;
    private readonly World world;
    public HashSet<int> Voxels = new HashSet<int>();

    public VoxelGrid(World world)
    {
        this.world = world;
    }

    public uint GetNeighbours(int index)
    {
        uint result = 0;

        if(Voxels.Contains(index + 1)) result |= 1;
        if(Voxels.Contains(index - 1)) result |= 2;
        if(Voxels.Contains(index + Size)) result |= 4;
        if(Voxels.Contains(index - Size)) result |= 8;
        if(Voxels.Contains(index + Size * Size)) result |= 16;
        if(Voxels.Contains(index - Size * Size)) result |= 32; 

        return result;
    }

    public JVector PositionFromIndex(int index)
    {
        if(index < 0 || index >= Size * Size * Size) 
        {
            throw new ArgumentOutOfRangeException();
        }

        int z = index / (Size * Size);
        int y = (index - z * (Size * Size)) / Size;
        int x = index - z * (Size * Size) - y * Size;
        return new JVector(x, y, z);
    }

    public RigidBody? Body { get; private set; }

    public bool AddVoxel(int x, int y, int z)
    {
        if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Size) 
        {
            throw new ArgumentOutOfRangeException();
        }

        return Voxels.Add(x + y * Size + z * Size * Size);
    }
}