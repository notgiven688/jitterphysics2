using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public static class Common
{
    public class IgnoreCollisionBetweenFilter : IBroadPhaseFilter
    {
        private readonly struct Pair : IEquatable<Pair>
        {
            private readonly Shape shapeA, shapeB;

            public Pair(Shape shapeA, Shape shapeB)
            {
                this.shapeA = shapeA;
                this.shapeB = shapeB;
            }

            public bool Equals(Pair other) => shapeA.Equals(other.shapeA) && shapeB.Equals(other.shapeB);
            public override bool Equals(object? obj) => obj is Pair other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(shapeA, shapeB);
        }

        private readonly HashSet<Pair> ignore = new();

        public bool Filter(Shape shapeA, Shape shapeB)
        {
            ulong a = shapeA.ShapeId;
            ulong b = shapeB.ShapeId;

            if (b < a) (shapeA, shapeB) = (shapeB, shapeA);
            return !ignore.Contains(new Pair(shapeA, shapeB));
        }

        public void IgnoreCollisionBetween(Shape shapeA, Shape shapeB)
        {
            ulong a = shapeA.ShapeId;
            ulong b = shapeB.ShapeId;

            if (b < a) (shapeA, shapeB) = (shapeB, shapeA);
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

        parts[(int)RagdollParts.Head].AddShape(new SphereShape(0.15f));
        parts[(int)RagdollParts.UpperLegLeft].AddShape(new CapsuleShape(0.08f, 0.3f));
        parts[(int)RagdollParts.UpperLegRight].AddShape(new CapsuleShape(0.08f, 0.3f));
        parts[(int)RagdollParts.LowerLegLeft].AddShape(new CapsuleShape(0.08f, 0.3f));
        parts[(int)RagdollParts.LowerLegRight].AddShape(new CapsuleShape(0.08f, 0.3f));
        parts[(int)RagdollParts.UpperArmLeft].AddShape(new CapsuleShape(0.07f, 0.2f));
        parts[(int)RagdollParts.UpperArmRight].AddShape(new CapsuleShape(0.07f, 0.2f));
        parts[(int)RagdollParts.LowerArmLeft].AddShape(new CapsuleShape(0.06f, 0.2f));
        parts[(int)RagdollParts.LowerArmRight].AddShape(new CapsuleShape(0.06f, 0.2f));
        parts[(int)RagdollParts.Torso].AddShape(new BoxShape(0.35f, 0.6f, 0.2f));

        parts[(int)RagdollParts.Head].Position = new JVector(0, 0, 0);
        parts[(int)RagdollParts.Torso].Position = new JVector(0, -0.46f, 0);
        parts[(int)RagdollParts.UpperLegLeft].Position = new JVector(0.11f, -0.85f, 0);
        parts[(int)RagdollParts.UpperLegRight].Position = new JVector(-0.11f, -0.85f, 0);
        parts[(int)RagdollParts.LowerLegLeft].Position = new JVector(0.11f, -1.2f, 0);
        parts[(int)RagdollParts.LowerLegRight].Position = new JVector(-0.11f, -1.2f, 0);

        parts[(int)RagdollParts.UpperArmLeft].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0f);
        parts[(int)RagdollParts.UpperArmRight].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0f);
        parts[(int)RagdollParts.LowerArmLeft].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0f);
        parts[(int)RagdollParts.LowerArmRight].Orientation = JQuaternion.CreateRotationZ(MathF.PI / 2.0f);

        parts[(int)RagdollParts.UpperArmLeft].Position = new JVector(0.30f, -0.2f, 0);
        parts[(int)RagdollParts.UpperArmRight].Position = new JVector(-0.30f, -0.2f, 0);

        parts[(int)RagdollParts.LowerArmLeft].Position = new JVector(0.55f, -0.2f, 0);
        parts[(int)RagdollParts.LowerArmRight].Position = new JVector(-0.55f, -0.2f, 0);

        var spine0 = world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.Head], parts[(int)RagdollParts.Torso]);
        spine0.Initialize(new JVector(0, -0.15f, 0));

        var spine1 = world.CreateConstraint<ConeLimit>(parts[(int)RagdollParts.Head], parts[(int)RagdollParts.Torso]);
        spine1.Initialize(-JVector.UnitZ, AngularLimit.FromDegree(0, 45));

        var hipLeft0 =
            world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegLeft]);
        hipLeft0.Initialize(new JVector(0.11f, -0.7f, 0));

        var hipLeft1 =
            world.CreateConstraint<TwistAngle>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegLeft]);
        hipLeft1.Initialize(JVector.UnitY, JVector.UnitY, AngularLimit.FromDegree(-80, +80));

        var hipLeft2 =
            world.CreateConstraint<ConeLimit>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegLeft]);
        hipLeft2.Initialize(-JVector.UnitY, AngularLimit.FromDegree(0, 60));

        var hipRight0 =
            world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegRight]);
        hipRight0.Initialize(new JVector(-0.11f, -0.7f, 0));

        var hipRight1 =
            world.CreateConstraint<TwistAngle>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegRight]);
        hipRight1.Initialize(JVector.UnitY, JVector.UnitY, AngularLimit.FromDegree(-80, +80));

        var hipRight2 =
            world.CreateConstraint<ConeLimit>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperLegRight]);
        hipRight2.Initialize(-JVector.UnitY, AngularLimit.FromDegree(0, 60));

        var kneeLeft = new HingeJoint(world, parts[(int)RagdollParts.UpperLegLeft],
            parts[(int)RagdollParts.LowerLegLeft],
            new JVector(0.11f, -1.05f, 0), JVector.UnitX, AngularLimit.FromDegree(-120, 0));

        var kneeRight = new HingeJoint(world, parts[(int)RagdollParts.UpperLegRight],
            parts[(int)RagdollParts.LowerLegRight],
            new JVector(-0.11f, -1.05f, 0), JVector.UnitX, AngularLimit.FromDegree(-120, 0));

        var armLeft = new HingeJoint(world, parts[(int)RagdollParts.LowerArmLeft],
            parts[(int)RagdollParts.UpperArmLeft],
            new JVector(0.42f, -0.2f, 0), JVector.UnitY, AngularLimit.FromDegree(-160, 0));

        var armRight = new HingeJoint(world, parts[(int)RagdollParts.LowerArmRight],
            parts[(int)RagdollParts.UpperArmRight],
            new JVector(-0.42f, -0.2f, 0), JVector.UnitY, AngularLimit.FromDegree(0, 160));

        var shoulderLeft0 =
            world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.UpperArmLeft], parts[(int)RagdollParts.Torso]);
        shoulderLeft0.Initialize(new JVector(0.20f, -0.2f, 0));

        var shoulderLeft1 =
            world.CreateConstraint<TwistAngle>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperArmLeft]);
        shoulderLeft1.Initialize(JVector.UnitX, JVector.UnitX, AngularLimit.FromDegree(-20, 60));

        var shoulderRight0 =
            world.CreateConstraint<BallSocket>(parts[(int)RagdollParts.UpperArmRight], parts[(int)RagdollParts.Torso]);
        shoulderRight0.Initialize(new JVector(-0.20f, -0.2f, 0));

        var shoulderRight1 =
            world.CreateConstraint<TwistAngle>(parts[(int)RagdollParts.Torso], parts[(int)RagdollParts.UpperArmRight]);
        shoulderRight1.Initialize(JVector.UnitX, JVector.UnitX, AngularLimit.FromDegree(-20, 60));

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
                body.Position = position + new JVector((e - i * 0.5f) * 1.01f, 0.5f + i * 1.0f, 0);
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

        JQuaternion halfRotationStep = JQuaternion.CreateRotationY(MathF.PI * 2.0f / 64.0f);
        JQuaternion fullRotationStep = halfRotationStep * halfRotationStep;
        JQuaternion orientation = JQuaternion.Identity;

        for (int e = 0; e < height; e++)
        {
            orientation *= halfRotationStep;

            for (int i = 0; i < 32; i++)
            {
                JVector position = pos + JVector.Transform(
                    new JVector(0, 0.5f + e, 19.5f), orientation);

                Shape shape = new BoxShape(3f, 1, 0.2f);
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

        position += new JVector(0, 0.5f, 0);

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
                body.Position = position + new JVector((i % 2 == 0 ? 0.5f : 0.0f) + e * 2.01f, 0.5f + i * 1.0f, 0.0f);
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
                body.Position = position + new JVector((e - i * 0.5f) * 1.01f, 0.5f + i * 1f, 0.0f);
                var shape = new CylinderShape(1f, 0.5f);
                body.AddShape(shape);
                action?.Invoke(body);
            }
        }
    }
}