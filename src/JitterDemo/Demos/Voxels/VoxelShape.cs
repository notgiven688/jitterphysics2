using System;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace JitterDemo;

public class VoxelShape : Shape
{
    public JVector Position { private set; get; }
    public int VoxelIndex { private set; get; }
    public VoxelGrid VoxelGrid { private set; get; }

    public uint Neighbours { private set; get; } = 0;

    public VoxelShape(VoxelGrid grid, int index)
    {
        this.Position = grid.PositionFromIndex(index);
        this.Neighbours = grid.GetNeighbours(index);
        this.VoxelIndex = index;
        this.VoxelGrid = grid;
        UpdateShape();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        // this is the support function of a box with size 1.
        result.X = Math.Sign(direction.X) * 0.5f;
        result.Y = Math.Sign(direction.Y) * 0.5f;
        result.Z = Math.Sign(direction.Z) * 0.5f;

        result += Position;
    }

    public override void PointWithin(out JVector point)
    {
        point = this.Position;
    }

    public override void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        // NOTE: We do not support any transformation of the body here.
        System.Diagnostics.Debug.Assert(orientation.W > 0.999f, "Voxel shape can not be attached to a transformed body.");
        System.Diagnostics.Debug.Assert(MathHelper.CloseToZero(position), "Voxel shape can not be attached to a transformed body.");

        box.Min = Position - JVector.One * 0.5f;
        box.Max = Position + JVector.One * 0.5f;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out float mass)
    {
        // Do not try to calculate mass properties here.
        mass = 1;
        inertia = JMatrix.Identity;
        com = Position;
    }
}