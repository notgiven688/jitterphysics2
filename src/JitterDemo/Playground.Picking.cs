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
    public Vector3 Unproject(Vector3 source, in Matrix4 projection, in Matrix4 view)
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

        return farPoint - nearPoint;
    }

    private RigidBody? grepBody;
    private bool grepping;

    private DistanceLimit? grepConstraint;
    private double hitDistance;
    private double hitWheelPosition;

    private void Pick()
    {
        Vector3 dir = Vector3.Normalize(RayTo((int)Mouse.Position.X, (int)Mouse.Position.Y));
        Vector3 pos = Camera.Position;
        JVector origin = Conversion.ToJitterVector(pos);
        JVector jdir = Conversion.ToJitterVector(dir);

        if (grepping)
        {
            if (grepBody == null) return;
            if (grepConstraint == null) return;

            hitDistance += Mouse.ScrollWheel.Y - hitWheelPosition;

            grepConstraint.Anchor2 = origin + hitDistance * jdir;
            grepBody.SetActivationState(true);

            grepBody.Data.Velocity *= 0.98d;
            grepBody.Data.AngularVelocity *= 0.98d;
        }
        else
        {
            grepBody = null;

            bool result = World.DynamicTree.RayCast(origin, jdir, null, null,
                out IDynamicTreeProxy? grepShape, out JVector _, out hitDistance);

            if (grepShape != null)
            {
                if (grepShape is SoftBodyShape gs)
                {
                    grepBody = gs.GetClosest(origin + jdir * hitDistance);
                }
                else if (grepShape is RigidBodyShape rbs)
                {
                    grepBody = rbs.RigidBody;
                }
            }

            if (!result || grepBody == null || grepBody.IsStatic) return;
            grepping = true;

            hitWheelPosition = Mouse.ScrollWheel.Y;

            if (grepConstraint != null) World.Remove(grepConstraint);

            JVector anchor = origin + hitDistance * jdir;

            grepConstraint = World.CreateConstraint<DistanceLimit>(grepBody, World.NullBody);
            grepConstraint.Initialize(anchor, anchor);
            grepConstraint.Softness = 0.01d;
            grepConstraint.Bias = 0.1d;
        }
    }
}