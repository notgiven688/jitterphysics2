using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

public static class Common
{
    public class IgnoreCollisionBetweenFilter : IBroadPhaseFilter
    {
        private readonly struct Pair : IEquatable<Pair>
        {
            private readonly RigidBodyShape shapeA, shapeB;

            public Pair(RigidBodyShape shapeA, RigidBodyShape shapeB)
            {
                this.shapeA = shapeA;
                this.shapeB = shapeB;
            }

            public bool Equals(Pair other)
            {
                return shapeA.Equals(other.shapeA) && shapeB.Equals(other.shapeB);
            }

            public override bool Equals(object? obj)
            {
                return obj is Pair other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(shapeA, shapeB);
            }
        }

        private readonly HashSet<Pair> ignore = new();

        public bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
        {
            if (proxyA is not RigidBodyShape shapeA || proxyB is not RigidBodyShape shapeB) return false;

            if (shapeB.ShapeId < shapeA.ShapeId) (shapeA, shapeB) = (shapeB, shapeA);
            return !ignore.Contains(new Pair(shapeA, shapeB));
        }

        public void IgnoreCollisionBetween(RigidBodyShape shapeA, RigidBodyShape shapeB)
        {
            if (shapeB.ShapeId < shapeA.ShapeId) (shapeA, shapeB) = (shapeB, shapeA);
            ignore.Add(new Pair(shapeA, shapeB));
        }
    }

    public enum RagdollParts
    {
        Head,
        UpperLegLeft,
        UpperLegRight,
        LowerLegLeft,
        LowerLegRight,
        UpperArmLeft,
        UpperArmRight,
        LowerArmLeft,
        LowerArmRight,
        Torso
    }


    public static void BuildRagdoll(JVector position, Action<RigidBody>? action = null)
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        RigidBody[] parts = new RigidBody[Enum.GetNames<RagdollParts>().Length];

        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = world.CreateRigidBody();
        }

        parts[(int)RagdollParts.Head].AddShape(new SphereShape(0.15d));
        parts[(int)RagdollParts.UpperLegLeft].AddShape(new CapsuleShape(0.08d, 0.3d));
        parts[(int)RagdollParts.UpperLegRight].AddShape(new CapsuleShape(0.08d, 0.3d));
        parts[(int)RagdollParts.LowerLegLeft].AddShape(new CapsuleShape(0.08d, 0.3d));
        parts[(int)RagdollParts.LowerLegRight].AddShape(new CapsuleShape(0.08d, 0.3d));
        parts[(int)RagdollParts.UpperArmLeft].AddShape(new CapsuleShape(0.07d, 0.2d));
        parts[(int)RagdollParts.UpperArmRight].AddShape(new CapsuleShape(0.07d, 0.2d));
        parts[(int)RagdollParts.LowerArmLeft].AddShape(new CapsuleShape(0.06d, 0.2d));
        parts[(int)RagdollParts.LowerArmRight].AddShape(new CapsuleShape(0.06d, 0.2d));
        parts[(int)RagdollParts.Torso].AddShape(new BoxShape(0.35d, 0.6d, 0.2d));

        parts[(int)RagdollParts.Head].Position = new JVector(0, 0, 0);
        parts[(int)RagdollParts.Torso].Position = new JVector(0, -0.46d, 0);
        parts[(int)RagdollParts.UpperLegLeft].Position = new JVector(0.11d, -0.85d, 0);
        parts[(int)RagdollParts.UpperLegRight].Position = new JVector(-0.11d, -0.85d, 0);
        parts[(int)RagdollParts.LowerLegLeft].Position = new JVector(0.11d, -1.2d, 0);
        parts[(int)RagdollParts.LowerLegRight].Position = new JVector(-0.11d, -1.2d, 0);

        parts[(int)RagdollParts.UpperArmLeft].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0d);
        parts[(int)RagdollParts.UpperArmRight].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0d);
        parts[(int)RagdollParts.LowerArmLeft].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0d);
        parts[(int)RagdollParts.LowerArmRight].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0d);

        parts[(int)RagdollParts.UpperArmLeft].Position = new JVector(0.30d, -0.2d, 0);
        parts[(int)RagdollParts.UpperArmRight].Position = new JVector(-0.30d, -0.2d, 0);

        parts[(int)RagdollParts.LowerArmLeft].Position = new JVector(0.55d, -0.2d, 0);
        parts[(int)RagdollParts.LowerArmRight].Position = new JVector(-0.55d, -0.2d, 0);

        var spine0 = world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.Head], parts[(int)RagdollParts.Torso]);
        spine0.Initialize(new JVector(0, -0.15d, 0));

        var spine1 = world.CreateConstraint<ConeLimit>(parts[(int)RagdollParts.Head], parts[(int)RagdollParts.Torso]);
        spine1.Initialize(-JVector.UnitZ, AngularLimit.FromDegree(0, 45));

        var hipLeft0 =
            world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegLeft]);
        hipLeft0.Initialize(new JVector(0.11d, -0.7d, 0));

        var hipLeft1 =
            world.CreateConstraint<TwistAngle>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegLeft]);
        hipLeft1.Initialize(JVector.UnitY, JVector.UnitY, AngularLimit.FromDegree(-80, +80));

        var hipLeft2 =
            world.CreateConstraint<ConeLimit>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegLeft]);
        hipLeft2.Initialize(-JVector.UnitY, AngularLimit.FromDegree(0, 60));

        var hipRight0 =
            world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegRight]);
        hipRight0.Initialize(new JVector(-0.11d, -0.7d, 0));

        var hipRight1 =
            world.CreateConstraint<TwistAngle>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegRight]);
        hipRight1.Initialize(JVector.UnitY, JVector.UnitY, AngularLimit.FromDegree(-80, +80));

        var hipRight2 =
            world.CreateConstraint<ConeLimit>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegRight]);
        hipRight2.Initialize(-JVector.UnitY, AngularLimit.FromDegree(0, 60));

        var kneeLeft = new HingeJoint(world, parts[(int)RagdollParts.UpperLegLeft],
            parts[(int)RagdollParts.LowerLegLeft],
            new JVector(0.11d, -1.05d, 0), JVector.UnitX, AngularLimit.FromDegree(-120, 0));

        var kneeRight = new HingeJoint(world, parts[(int)RagdollParts.UpperLegRight],
            parts[(int)RagdollParts.LowerLegRight],
            new JVector(-0.11d, -1.05d, 0), JVector.UnitX, AngularLimit.FromDegree(-120, 0));

        var armLeft = new HingeJoint(world, parts[(int)RagdollParts.LowerArmLeft],
            parts[(int)RagdollParts.UpperArmLeft],
            new JVector(0.42d, -0.2d, 0), JVector.UnitY, AngularLimit.FromDegree(-160, 0));

        var armRight = new HingeJoint(world, parts[(int)RagdollParts.LowerArmRight],
            parts[(int)RagdollParts.UpperArmRight],
            new JVector(-0.42d, -0.2d, 0), JVector.UnitY, AngularLimit.FromDegree(0, 160));

        // Soften the limits for the hinge joints
        kneeLeft.HingeAngle.LimitSoftness = kneeLeft.HingeAngle.Softness = 1;
        kneeRight.HingeAngle.LimitSoftness = kneeRight.HingeAngle.Softness = 1;
        armLeft.HingeAngle.LimitSoftness = armLeft.HingeAngle.Softness = 1;
        armRight.HingeAngle.LimitSoftness = armRight.HingeAngle.Softness = 1;

        var shoulderLeft0 =
            world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.UpperArmLeft], parts[(int)RagdollParts.Torso]);
        shoulderLeft0.Initialize(new JVector(0.20d, -0.2d, 0));

        var shoulderLeft1 =
            world.CreateConstraint<TwistAngle>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperArmLeft]);
        shoulderLeft1.Initialize(JVector.UnitX, JVector.UnitX, AngularLimit.FromDegree(-20, 60));

        var shoulderRight0 =
            world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.UpperArmRight], parts[(int)RagdollParts.Torso]);
        shoulderRight0.Initialize(new JVector(-0.20d, -0.2d, 0));

        var shoulderRight1 =
            world.CreateConstraint<TwistAngle>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperArmRight]);
        shoulderRight1.Initialize(JVector.UnitX, JVector.UnitX, AngularLimit.FromDegree(-20, 60));

        shoulderLeft1.Bias = 0.01d;
        shoulderRight1.Bias = 0.01d;
        hipLeft1.Bias = 0.01d;
        hipRight1.Bias = 0.01d;

        if (world.BroadPhaseFilter is not IgnoreCollisionBetweenFilter filter)
        {
            filter = new IgnoreCollisionBetweenFilter();
            world.BroadPhaseFilter = filter;
        }

        filter.IgnoreCollisionBetween(parts[(int)RagdollParts.UpperLegLeft].Shapes[0],
            parts[(int)RagdollParts.Torso].Shapes[0]);
        filter.IgnoreCollisionBetween(parts[(int)RagdollParts.UpperLegRight].Shapes[0],
            parts[(int)RagdollParts.Torso].Shapes[0]);
        filter.IgnoreCollisionBetween(parts[(int)RagdollParts.UpperLegLeft].Shapes[0],
            parts[(int)RagdollParts.LowerLegLeft].Shapes[0]);
        filter.IgnoreCollisionBetween(parts[(int)RagdollParts.UpperLegRight].Shapes[0],
            parts[(int)RagdollParts.LowerLegRight].Shapes[0]);
        filter.IgnoreCollisionBetween(parts[(int)RagdollParts.UpperArmLeft].Shapes[0],
            parts[(int)RagdollParts.Torso].Shapes[0]);
        filter.IgnoreCollisionBetween(parts[(int)RagdollParts.UpperArmRight].Shapes[0],
            parts[(int)RagdollParts.Torso].Shapes[0]);
        filter.IgnoreCollisionBetween(parts[(int)RagdollParts.UpperArmLeft].Shapes[0],
            parts[(int)RagdollParts.LowerArmLeft].Shapes[0]);
        filter.IgnoreCollisionBetween(parts[(int)RagdollParts.UpperArmRight].Shapes[0],
            parts[(int)RagdollParts.LowerArmRight].Shapes[0]);

        for (int i = 0; i < parts.Length; i++)
        {
            parts[i].Position += position;
            action?.Invoke(parts[i]);
        }
    }

    public static void BuildPyramid(JVector position, int size = 20, Action<RigidBody>? action = null)
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        for (int i = 0; i < size; i++)
        {
            for (int e = i; e < size; e++)
            {
                RigidBody body = world.CreateRigidBody();
                body.Position = position + new JVector((e - i * 0.5d) * 1.01d, 0.5d + i * 1.0d, 0);
                var shape = new BoxShape(1);
                body.AddShape(shape);
                action?.Invoke(body);
            }
        }
    }

    public static void BuildTower(JVector pos, int height = 40, Action<RigidBody>? action = null)
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        JQuaternion halfRotationStep = JQuaternion.CreateRotationY(MathF.PI * 2.0d / 64.0d);
        JQuaternion fullRotationStep = halfRotationStep * halfRotationStep;
        JQuaternion orientation = JQuaternion.Identity;

        for (int e = 0; e < height; e++)
        {
            orientation *= halfRotationStep;

            for (int i = 0; i < 32; i++)
            {
                JVector position = pos + JVector.Transform(
                    new JVector(0, 0.5d + e, 19.5d), orientation);

                var shape = new BoxShape(3, 1, 0.2d);
                var body = world.CreateRigidBody();

                body.Orientation = orientation;
                body.Position = new JVector(position.X, position.Y, position.Z);

                body.AddShape(shape);

                orientation *= fullRotationStep;

                action?.Invoke(body);
            }
        }
    }

    public static void BuildJenga(JVector position, int size = 20, Action<RigidBody>? action = null)
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        position += new JVector(0, 0.5d, 0);

        for (int i = 0; i < size; i++)
        {
            for (int e = 0; e < 3; e++)
            {
                var body = world.CreateRigidBody();

                if (i % 2 == 0)
                {
                    body.AddShape(new BoxShape(3, 1, 1));
                    body.Position = position + new JVector(0, i, -1 + e);
                }
                else
                {
                    body.AddShape(new BoxShape(1, 1, 3));
                    body.Position = position + new JVector(-1 + e, i, 0);
                }

                action?.Invoke(body);
            }
        }
    }

    public static void BuildWall(JVector position, int sizex = 20, int sizey = 14, Action<RigidBody>? action = null)
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        for (int i = 0; i < sizex; i++)
        {
            for (int e = 0; e < sizey; e++)
            {
                RigidBody body = world.CreateRigidBody();
                body.Position = position + new JVector((i % 2 == 0 ? 0.5d : 0.0d) + e * 2.01d, 0.5d + i * 1.0d, 0.0d);
                var shape = new BoxShape(2, 1, 1);
                body.AddShape(shape);
                action?.Invoke(body);
            }
        }
    }

    public static void BuildPyramidCylinder(JVector position, int size = 20, Action<RigidBody>? action = null)
    {
        Playground pg = (Playground)RenderWindow.Instance;
        World world = pg.World;

        for (int i = 0; i < size; i++)
        {
            for (int e = i; e < size; e++)
            {
                RigidBody body = world.CreateRigidBody();
                body.Position = position + new JVector((e - i * 0.5d) * 1.01d, 0.5d + i * 1, 0.0d);
                var shape = new CylinderShape(1, 0.5d);
                body.AddShape(shape);
                action?.Invoke(body);
            }
        }
    }
}