using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace JitterDemo;

public class VoxelEdgeCollisionFilter : INarrowPhaseFilter
{
    /// <summary>
    /// If the collision normal has a "forbidden" component greater
    /// than this value, it is filtered out.
    /// </summary>
    public float NormalThreshold { get; set; } = 0.1f;

    /// <summary>
    /// Collision points closer than this value to a voxel edge
    /// are subject to filtering.
    /// </summary>
    public float EdgeThreshold { get; set; } = 0.01f;

    public bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB, ref JVector pAA, ref JVector pBB, ref JVector normal,
        ref double penetration)
    {
        VoxelShape? vs1 = shapeA as VoxelShape;
        VoxelShape? vs2 = shapeB as VoxelShape;

        bool c1 = vs1 != null;
        bool c2 = vs2 != null;

        // both shapes are voxels or both of them are not -> return
        if (c1 == c2) return true;

        VoxelShape vshape = c1 ? vs1! : vs2!;

        float closeToEdge = 0.5f - EdgeThreshold;

        float trsh = NormalThreshold;
        uint nb = vshape.Neighbours;

        JVector cnormal = normal;
        JVector relPos = pAA;

        if (c2)
        {
            relPos = pBB;
            cnormal.Negate();
        }

        relPos -= vshape.Position;

        // Check if collision normal points into a specific direction.
        // If yes, check if there is a neighbouring voxel. If yes,
        // discard the collision.

        if (relPos.X > closeToEdge && cnormal.X > trsh)
        {
            if ((nb & 1) != 0) return false;
        }

        if (relPos.X < -closeToEdge&& cnormal.X < -trsh)
        {
            if ((nb & 2) != 0) return false;
        }

        if (relPos.Y > closeToEdge && cnormal.Y > trsh)
        {
            if ((nb & 4) != 0) return false;
        }

        if (relPos.Y < -closeToEdge && cnormal.Y < -trsh)
        {
            if ((nb & 8) != 0) return false;
        }

        if (relPos.Z > closeToEdge && cnormal.Z > trsh)
        {
            if ((nb & 16) != 0) return false;
        }

        if (relPos.Z < -closeToEdge && cnormal.Z < -trsh)
        {
            if ((nb & 32) != 0) return false;
        }

        return true;
    }
}