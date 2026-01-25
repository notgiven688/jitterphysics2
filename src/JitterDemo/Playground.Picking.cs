using System;
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

        return Vector3.Normalize(farPoint - nearPoint);
    }

    private RigidBody? grepBody;
    private bool grepping;

    private DistanceLimit? grepConstraint;
    private double hitDistance;
    private double hitWheelPosition;

    private void Pick()
    {
        JVector origin = Conversion.ToJitterVector(Camera.Position);
        JVector dir = Conversion.ToJitterVector(RayTo((int)Mouse.Position.X, (int)Mouse.Position.Y));

        if (grepping)
        {
            if (grepBody == null) return;
            if (grepConstraint == null) return;

            hitDistance += ((float)Mouse.ScrollWheel.Y - hitWheelPosition);

            grepConstraint.Anchor2 = origin + hitDistance * dir;
            grepBody.SetActivationState(true);

            grepBody.Data.Velocity *= 0.98f;
            grepBody.Data.AngularVelocity *= 0.98f;
        }
        else
        {
            grepBody = null;

            bool result = World.DynamicTree.RayCast(origin, dir, null, null,
                out IDynamicTreeProxy? grepShape, out JVector hitNormal, out hitDistance);

            if (!result) return;

            JVector hitPoint = origin + hitDistance * dir;

            Console.WriteLine($"Ray cast, hit point: {hitPoint}; hit normal: {hitNormal}; distance: {hitDistance}");

            if (grepShape != null)
            {
                if (grepShape is SoftBodyShape gs)
                {
                    grepBody = gs.GetClosest(hitPoint);
                }
                else if (grepShape is RigidBodyShape rbs)
                {
                    grepBody = rbs.RigidBody;
                }
            }

            if (grepBody == null || grepBody.MotionType != MotionType.Dynamic) return;
            grepping = true;

            hitWheelPosition = (float)Mouse.ScrollWheel.Y;

            if (grepConstraint != null) World.Remove(grepConstraint);

            grepConstraint = World.CreateConstraint<DistanceLimit>(grepBody, World.NullBody);
            grepConstraint.Initialize(hitPoint, hitPoint);
            grepConstraint.Softness = 0.01f;
            grepConstraint.Bias = 0.1f;
        }
    }
}