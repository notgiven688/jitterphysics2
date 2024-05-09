using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;

namespace JitterDemo;

public class VoxelEdgeCollisionFilter : INarrowPhaseFilter
{
    public float Threshold { get; set; } = 0.1f;

    public bool Filter(Shape shapeA, Shape shapeB, ref JVector pAA, ref JVector pBB, ref JVector normal,
        ref float penetration)
    {
        VoxelShape? vs1 = shapeA as VoxelShape;
        VoxelShape? vs2 = shapeB as VoxelShape;

        bool c1 = vs1 != null;
        bool c2 = vs2 != null;

        // both shapes are voxels or both of them are not -> return
        if (c1 == c2) return true;

        VoxelShape vshape = c1 ? vs1! : vs2!;

        if (shapeA.RigidBody == null || shapeB.RigidBody == null)
        {
            return true;
        }

        float trsh = Threshold;

        JVector cnormal = normal;
        if (c2) cnormal.Negate();

        // Check if collision normal points into a specific direction.
        // If yes, check if there is a neighbouring voxel. If yes,
        // discard the collision.

        if(cnormal.X > trsh)
        {
            if((vshape.neighbours & 1) != 0) return false;
        }
        else if(cnormal.X < -trsh)
        {
            if((vshape.neighbours & 2) != 0) return false;
        }

        if(cnormal.Y > trsh)
        {
            if((vshape.neighbours & 4) != 0) return false;
        }
        else if(cnormal.Y < -trsh)
        {
            if((vshape.neighbours & 8) != 0) return false;
        }

        if(cnormal.Z > trsh)
        {
            if((vshape.neighbours & 16) != 0) return false;
        }
        else if(cnormal.Z < -trsh)
        {
            if((vshape.neighbours & 32) != 0) return false;
        }

        return true;
    }
}