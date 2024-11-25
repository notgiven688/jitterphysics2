using System;
using System.Diagnostics;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace JitterDemo;

public class VoxelShape : RigidBodyShape
{
    public JVector Position { get; }
    public int VoxelIndex { private set; get; }
    public VoxelGrid VoxelGrid { private set; get; }

    public uint Neighbours { private set; get; }

    public VoxelShape(VoxelGrid grid, int index)
    {
        Position = grid.PositionFromIndex(index);
        Neighbours = grid.GetNeighbours(index);
        VoxelIndex = index;
        VoxelGrid = grid;
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        // this is the support function of a box with size 1.
        result.X = Math.Sign(direction.X) * 0.5f;
        result.Y = Math.Sign(direction.Y) * 0.5f;
        result.Z = Math.Sign(direction.Z) * 0.5f;

        result += Position;
    }

    public override void GetCenter(out JVector point)
    {
        point = Position;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        // NOTE: We do not support any transformation of the body here.
        Debug.Assert(orientation.W > 0.999f, "Voxel shape can not be attached to a transformed body.");
        Debug.Assert(MathHelper.CloseToZero(position), "Voxel shape can not be attached to a transformed body.");

        box.Min = Position - JVector.One * 0.5f;
        box.Max = Position + JVector.One * 0.5f;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out double mass)
    {
        // Do not try to calculate mass properties here.
        mass = 1;
        inertia = JMatrix.Identity;
        com = Position;
    }
}