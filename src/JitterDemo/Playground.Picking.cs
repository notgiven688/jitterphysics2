using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public partial class Playground : RenderWindow
{
    private Vector3 Unproject(Vector3 source, in Matrix4 projection, in Matrix4 view)
    {
        source.X = source.X / Width * 2.0f - 1.0f;
        source.Y = -source.Y / Height * 2.0f + 1.0f;
        Matrix4 matrix = Matrix4.Multiply(projection, view);
        Matrix4.Invert(matrix, out matrix);
        Vector4 vec = new(source, 1.0f);
        Vector4 up = matrix * vec;
        return new Vector3(up.X / up.W, up.Y / up.W, up.Z / up.W);
    }

    private Vector3 RayTo(int x, int y)
    {
        Vector3 nearSource = new(x, y, 0.1f);
        Vector3 farSource = new(x, y, 0.5f);

        Vector3 nearPoint = Unproject(nearSource, Camera.ProjectionMatrix, Camera.ViewMatrix);
        Vector3 farPoint = Unproject(farSource, Camera.ProjectionMatrix, Camera.ViewMatrix);

        return Vector3.Normalize(farPoint - nearPoint);
    }

    private RigidBody? grabBody;
    private bool grabbing;

    private DistanceLimit? grabConstraint;
    private float hitDistance;
    private void Pick()
    {
        JVector origin = Conversion.ToJitterVector(Camera.Position);
        JVector dir = Conversion.ToJitterVector(RayTo((int)Mouse.Position.X, (int)Mouse.Position.Y));

        if (grabbing)
        {
            if (grabBody == null) return;
            if (grabConstraint == null) return;

            hitDistance += (float)Mouse.ScrollWheel.Y;

            grabConstraint.Anchor2 = origin + hitDistance * dir;
            grabBody.SetActivationState(true);

            grabBody.Data.Velocity *= 0.98f;
            grabBody.Data.AngularVelocity *= 0.98f;
        }
        else
        {
            grabBody = null;

            bool result = World.DynamicTree.RayCast(origin, dir, null, null,
                out IDynamicTreeProxy? grabShape, out JVector hitNormal, out hitDistance);

            if (!result) return;

            JVector hitPoint = origin + hitDistance * dir;

            if (grabShape != null)
            {
                if (grabShape is SoftBodyShape gs)
                {
                    grabBody = gs.GetClosest(hitPoint);
                }
                else if (grabShape is RigidBodyShape rbs)
                {
                    grabBody = rbs.RigidBody;
                }
            }

            if (grabBody == null || grabBody.MotionType != MotionType.Dynamic) return;
            grabbing = true;

            if (grabConstraint != null) World.Remove(grabConstraint);

            grabConstraint = World.CreateConstraint<DistanceLimit>(grabBody, World.NullBody);
            grabConstraint.Initialize(hitPoint, hitPoint);
            grabConstraint.Softness = 0.01f;
            grabConstraint.Bias = 0.1f;
        }
    }
}