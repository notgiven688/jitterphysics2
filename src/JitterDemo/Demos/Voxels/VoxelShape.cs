using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;

namespace JitterDemo;

public class VoxelShape : Shape
{
    public JVector Position { private set; get; }
    public int VoxelIndex { private set; get; }

    public VoxelGrid VoxelGrid { private set; get; }

    public uint neighbours = 0;

    public VoxelShape(VoxelGrid grid, int index)
    {
        this.Position = grid.PositionFromIndex(index);
        this.neighbours = grid.GetNeighbours(index);
        this.VoxelIndex = index;
        this.VoxelGrid = grid;
        UpdateShape();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        result.X = Math.Sign(direction.X) * 0.5f;
        result.Y = Math.Sign(direction.Y) * 0.5f;
        result.Z = Math.Sign(direction.Z) * 0.5f;

        result += Position;
    }

    public override void CalculateBoundingBox(in JMatrix orientation, in JVector position, out JBBox box)
    {
        // TODO: respect the body's transformation
        box.Min = Position - JVector.One * 0.5f;
        box.Max = Position + JVector.One * 0.5f;
    }

    public override void CalculateMassInertia(out JMatrix inertia, out JVector com, out float mass)
    {
        // TODO: One could calculate mass properties here
        mass = 1;
        inertia = JMatrix.Identity;
        com = Position;
    }
}