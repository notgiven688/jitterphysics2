using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;
using Vellum;

namespace JitterDemo;

/// <summary>
/// Convex hull of two spheres on the local y-axis (radius <c>radiusTop</c> at +halfLength,
/// <c>radiusBottom</c> at -halfLength), optionally scaled along x and z. Used for the
/// egg- and limb-shaped segments of the wooden mannequins.
/// </summary>
public class TaperedCapsuleShape : RigidBodyShape
{
    public readonly float RadiusTop, RadiusBottom, HalfLength, ScaleX, ScaleZ;

    public TaperedCapsuleShape(float radiusTop, float radiusBottom, float halfLength,
        float scaleX = 1, float scaleZ = 1)
    {
        RadiusTop = radiusTop;
        RadiusBottom = radiusBottom;
        HalfLength = halfLength;
        ScaleX = scaleX;
        ScaleZ = scaleZ;
        UpdateWorldBoundingBox();
    }

    public override void SupportMap(in JVector direction, out JVector result)
    {
        // Support of a linearly transformed shape: S(d) = A * S0(A^T * d), with A = diag(sx, 1, sz).
        JVector d = new(direction.X * ScaleX, direction.Y, direction.Z * ScaleZ);
        JVector.NormalizeInPlace(ref d);

        float top = HalfLength * d.Y + RadiusTop;
        float bottom = -HalfLength * d.Y + RadiusBottom;

        result = top >= bottom
            ? new JVector(0, HalfLength, 0) + d * RadiusTop
            : new JVector(0, -HalfLength, 0) + d * RadiusBottom;

        result.X *= ScaleX;
        result.Z *= ScaleZ;
    }

    public override void GetCenter(out JVector point)
    {
        point = JVector.Zero;
    }

    /// Appends a tessellation of this shape (in its local frame) with UVs that wrap around the axis.
    public void Tessellate(List<Vertex> vertices, List<TriangleVertexIndex> indices, int segments = 24)
    {
        float alpha = HalfLength > 0 ? MathF.Asin(Math.Clamp((RadiusTop - RadiusBottom) / (2 * HalfLength), -1, 1)) : 0;
        float tangent = MathF.PI / 2 - alpha;

        // Latitude rings from the top pole to the bottom pole: (polar angle, sphere).
        var rings = new List<(float theta, bool top)>();
        const int capRings = 10;
        for (int k = 0; k <= capRings; k++) rings.Add((tangent * k / capRings, true));
        for (int k = 0; k <= capRings; k++) rings.Add((tangent + (MathF.PI - tangent) * k / capRings, false));

        int first = vertices.Count;
        int columns = segments + 1;
        float v = 0;
        Vector3 previous = default;

        for (int r = 0; r < rings.Count; r++)
        {
            (float theta, bool top) = rings[r];
            float radius = top ? RadiusTop : RadiusBottom;
            float center = top ? HalfLength : -HalfLength;

            float ringRadius = MathF.Sin(theta) * radius;
            float ringY = center + MathF.Cos(theta) * radius;

            // The texture coordinate along the axis follows the profile's arc length.
            Vector3 profile = new(ringRadius, ringY, 0);
            if (r > 0) v += (profile - previous).Length() * 4f;
            previous = profile;

            for (int i = 0; i < columns; i++)
            {
                float a = 2 * MathF.PI * i / segments;
                float c = MathF.Cos(a), s = MathF.Sin(a);

                Vector3 position = new(ringRadius * c * ScaleX, ringY, ringRadius * s * ScaleZ);
                Vector3 normal = Vector3.Normalize(new Vector3(MathF.Sin(theta) * c / ScaleX,
                    MathF.Cos(theta), MathF.Sin(theta) * s / ScaleZ));

                vertices.Add(new Vertex(position, normal, new Vector2((float)i / segments, v)));
            }
        }

        // Same winding as Mesh.Sphere: counter-clockwise seen from outside.
        for (int r = 0; r < rings.Count - 1; r++)
        {
            for (int i = 0; i < segments; i++)
            {
                int i0 = first + r * columns + i;
                int i1 = i0 + columns;
                indices.Add(new TriangleVertexIndex(i0, i0 + 1, i1));
                indices.Add(new TriangleVertexIndex(i0 + 1, i1 + 1, i1));
            }
        }
    }
}

/// <summary>
/// A wooden artist's mannequin: fifteen tapered segments held together by ball and hinge joints.
/// Every joint carries a torque-limited servo that pulls it towards a relaxed posture, so a
/// dropped mannequin collapses but keeps trying to return to it.
/// </summary>
public class Mannequin
{
    public enum Bone
    {
        Pelvis, Chest, Head,
        UpperArmLeft, ForearmLeft, HandLeft,
        UpperArmRight, ForearmRight, HandRight,
        ThighLeft, ShinLeft, FootLeft,
        ThighRight, ShinRight, FootRight
    }

    public const int BoneCount = (int)Bone.FootRight + 1;

    private const float Density = 700f; // kg/m^3, roughly beech
    private const float Gravity = 9.81f;

    private readonly struct Segment(Bone bone, Bone parent, JVector a, JVector b, float ra, float rb,
        float sx = 1, float sz = 1)
    {
        // Sphere centres a (radius ra) and b (radius rb) in the standing pose, feet at y = 0, facing +z.
        public readonly Bone Bone = bone, Parent = parent;
        public readonly JVector A = a, B = b;
        public readonly float RadiusA = ra, RadiusB = rb, ScaleX = sx, ScaleZ = sz;
    }

    private static readonly Segment[] Segments = BuildSegments();

    private static Segment[] BuildSegments()
    {
        var list = new Segment[BoneCount];

        void Add(Bone bone, Bone parent, JVector a, JVector b, float ra, float rb, float sx = 1, float sz = 1)
            => list[(int)bone] = new Segment(bone, parent, a, b, ra, rb, sx, sz);

        Add(Bone.Pelvis, Bone.Pelvis, new(0, 1.00f, 0), new(0, 0.94f, 0), 0.10f, 0.11f, 1.3f, 0.8f);
        Add(Bone.Chest, Bone.Pelvis, new(0, 1.36f, 0), new(0, 1.20f, 0), 0.125f, 0.095f, 1.2f, 0.72f);
        Add(Bone.Head, Bone.Chest, new(0, 1.67f, 0.01f), new(0, 1.60f, 0.02f), 0.095f, 0.072f, 0.85f);

        for (int side = 0; side < 2; side++)
        {
            float s = side == 0 ? 1 : -1;
            int o = side * 3;

            Add(Bone.UpperArmLeft + o, Bone.Chest, new(s * 0.21f, 1.355f, 0), new(s * 0.235f, 1.15f, 0), 0.048f, 0.038f);
            Add(Bone.ForearmLeft + o, Bone.UpperArmLeft + o, new(s * 0.245f, 1.07f, 0), new(s * 0.258f, 0.865f, 0.005f), 0.038f, 0.03f);
            Add(Bone.HandLeft + o, Bone.ForearmLeft + o, new(s * 0.263f, 0.79f, 0.005f), new(s * 0.27f, 0.705f, 0.01f), 0.032f, 0.024f, 0.55f, 1.25f);

            Add(Bone.ThighLeft + o, Bone.Pelvis, new(s * 0.09f, 0.88f, 0), new(s * 0.09f, 0.56f, 0), 0.07f, 0.05f);
            Add(Bone.ShinLeft + o, Bone.ThighLeft + o, new(s * 0.09f, 0.465f, 0), new(s * 0.09f, 0.13f, 0), 0.052f, 0.036f);
            Add(Bone.FootLeft + o, Bone.ShinLeft + o, new(s * 0.09f, 0.035f, 0.165f), new(s * 0.09f, 0.045f, -0.03f), 0.035f, 0.045f, 1.15f);
        }

        return list;
    }

    // Joint centres in the standing pose, keyed by the child bone.
    private static JVector JointCenter(Bone child, float s) => child switch
    {
        Bone.Chest => new(0, 1.105f, 0),
        Bone.Head => new(0, 1.50f, 0),
        Bone.UpperArmLeft or Bone.UpperArmRight => new(s * 0.20f, 1.40f, 0),
        Bone.ForearmLeft or Bone.ForearmRight => new(s * 0.24f, 1.11f, 0),
        Bone.HandLeft or Bone.HandRight => new(s * 0.261f, 0.828f, 0.005f),
        Bone.ThighLeft or Bone.ThighRight => new(s * 0.09f, 0.94f, 0),
        Bone.ShinLeft or Bone.ShinRight => new(s * 0.09f, 0.51f, 0),
        Bone.FootLeft or Bone.FootRight => new(s * 0.09f, 0.09f, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(child))
    };

    private static float SideOf(Bone bone) => bone switch
    {
        Bone.UpperArmRight or Bone.ForearmRight or Bone.HandRight or
            Bone.ThighRight or Bone.ShinRight or Bone.FootRight => -1,
        _ => 1
    };

    private static void SegmentFrame(in Segment segment, out JVector position, out JQuaternion orientation)
    {
        JVector y = JVector.Normalize(segment.A - segment.B);
        JVector x = JVector.UnitX - y * JVector.Dot(y, JVector.UnitX);
        JVector.NormalizeInPlace(ref x);
        JVector z = JVector.Cross(x, y);

        position = 0.5f * (segment.A + segment.B);
        orientation = JQuaternion.CreateFromMatrix(JMatrix.FromColumns(x, y, z));
    }

    private static TaperedCapsuleShape SegmentShape(in Segment segment)
    {
        float halfLength = 0.5f * (float)(segment.A - segment.B).Length();
        return new TaperedCapsuleShape(segment.RadiusA, segment.RadiusB, halfLength, segment.ScaleX, segment.ScaleZ);
    }

    // Hinge angles of the relaxed pose. Positive angles swing the lower end backwards.
    private static float RelaxedHingeDegrees(Bone bone) => bone switch
    {
        Bone.ForearmLeft or Bone.ForearmRight => -55,
        Bone.ShinLeft or Bone.ShinRight => 30,
        Bone.FootLeft or Bone.FootRight => 12, // toes pointing slightly down
        _ => 0
    };

    /// <summary>
    /// Rotation of a bone relative to its parent that takes the standing pose to the relaxed pose, expressed
    /// in standing-pose world axes (x: left, y: up, z: forward). The relaxed pose is a toned-down version of
    /// the neutral posture a body takes when no muscle is working: back and neck slightly bent, arms a little
    /// forward and out with elbows half bent, hips and knees flexed, feet pointing down.
    /// </summary>
    private static JQuaternion RelaxedRotation(Bone bone)
    {
        float s = SideOf(bone);
        const float deg = MathF.PI / 180;

        return bone switch
        {
            Bone.Chest => JQuaternion.CreateRotationX(8 * deg),
            Bone.Head => JQuaternion.CreateRotationX(12 * deg),
            // Turn inwards about the arm axis, lift it sideways, then forwards.
            Bone.UpperArmLeft or Bone.UpperArmRight => JQuaternion.CreateRotationX(-25 * deg) *
                                                       JQuaternion.CreateRotationZ(s * 18 * deg) *
                                                       JQuaternion.CreateRotationY(-s * 25 * deg),
            // Fingers curl slightly towards the palm, which faces the body.
            Bone.HandLeft or Bone.HandRight => JQuaternion.CreateRotationZ(-s * 12 * deg),
            // Turn outwards, spread a little, then flex forwards.
            Bone.ThighLeft or Bone.ThighRight => JQuaternion.CreateRotationX(-22 * deg) *
                                                 JQuaternion.CreateRotationZ(s * 6 * deg) *
                                                 JQuaternion.CreateRotationY(s * 8 * deg),
            _ => JQuaternion.CreateRotationX(RelaxedHingeDegrees(bone) * deg)
        };
    }

    // Body frames of the relaxed pose, built outwards from the pelvis.
    private static readonly (JVector Position, JQuaternion Orientation)[] RelaxedPose = BuildRelaxedPose();

    private static (JVector, JQuaternion)[] BuildRelaxedPose()
    {
        var pose = new (JVector Position, JQuaternion Orientation)[BoneCount];
        SegmentFrame(Segments[0], out pose[0].Position, out pose[0].Orientation);

        // Segments are ordered parents first.
        for (int i = 1; i < BoneCount; i++)
        {
            Bone bone = (Bone)i;
            int parent = (int)Segments[i].Parent;
            SegmentFrame(Segments[parent], out JVector parentPosition, out JQuaternion parentOrientation);
            SegmentFrame(Segments[i], out JVector position, out JQuaternion orientation);
            JVector center = JointCenter(bone, SideOf(bone));

            JQuaternion relative = parentOrientation.Conjugate() * RelaxedRotation(bone) * orientation;
            JQuaternion relaxedOrientation = pose[parent].Orientation * relative;

            JVector relaxedCenter = pose[parent].Position + JVector.Transform(
                JVector.ConjugatedTransform(center - parentPosition, parentOrientation), pose[parent].Orientation);

            pose[i].Orientation = relaxedOrientation;
            pose[i].Position = relaxedCenter + JVector.Transform(
                JVector.ConjugatedTransform(position - center, orientation), relaxedOrientation);
        }

        return pose;
    }

    /// <summary>Builds the bind mesh in bone-local coordinates: one segment per bone, plus joint balls and a neck.</summary>
    public static Mesh BuildMesh(out int[] vertexBone)
    {
        var vertices = new List<Vertex>();
        var indices = new List<TriangleVertexIndex>();
        var bones = new List<int>();

        // Appends a shape posed in the standing pose to the given bone.
        void AddPiece(TaperedCapsuleShape shape, JVector position, JQuaternion orientation, Bone bone)
        {
            int start = vertices.Count;
            shape.Tessellate(vertices, indices);

            SegmentFrame(Segments[(int)bone], out JVector bonePosition, out JQuaternion boneOrientation);
            JQuaternion toBone = boneOrientation.Conjugate() * orientation;
            JVector offset = JVector.ConjugatedTransform(position - bonePosition, boneOrientation);

            for (int i = start; i < vertices.Count; i++)
            {
                Vertex vertex = vertices[i];
                JVector p = JVector.Transform(Conversion.ToJitterVector(vertex.Position), toBone) + offset;
                JVector n = JVector.Transform(Conversion.ToJitterVector(vertex.Normal), toBone);
                vertices[i] = new Vertex(Conversion.FromJitter(p), Conversion.FromJitter(n), vertex.Texture);
                bones.Add((int)bone);
            }
        }

        for (int i = 0; i < BoneCount; i++)
        {
            SegmentFrame(Segments[i], out JVector position, out JQuaternion orientation);
            AddPiece(SegmentShape(Segments[i]), position, orientation, (Bone)i);

            Bone bone = (Bone)i;
            if (bone == Bone.Pelvis) continue;

            // Joint ball, carried by the parent.
            float radius = bone switch
            {
                Bone.Chest => 0.075f,
                Bone.Head => 0.04f,
                Bone.UpperArmLeft or Bone.UpperArmRight => 0.045f,
                Bone.ThighLeft or Bone.ThighRight => 0.055f,
                Bone.ShinLeft or Bone.ShinRight => 0.045f,
                Bone.FootLeft or Bone.FootRight => 0.032f,
                _ => 0.032f
            };

            AddPiece(new TaperedCapsuleShape(radius, radius, 0), JointCenter(bone, SideOf(bone)),
                JQuaternion.Identity, Segments[i].Parent);
        }

        // Neck peg, carried by the head.
        AddPiece(new TaperedCapsuleShape(0.036f, 0.04f, 0.04f), new JVector(0, 1.53f, 0.005f),
            JQuaternion.Identity, Bone.Head);

        vertexBone = bones.ToArray();
        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    private struct Drive
    {
        public AngularServo? Angular;
        public TwistServo? Twist;
        public float BaseTorque;
    }

    public readonly RigidBody[] Bodies = new RigidBody[BoneCount];
    private readonly List<Drive> drives = new();

    public Mannequin(World world, Common.IgnoreCollisionBetweenFilter filter, JVector position, JQuaternion orientation)
    {
        for (int i = 0; i < BoneCount; i++)
        {
            var body = world.CreateRigidBody();
            SegmentFrame(Segments[i], out JVector p, out JQuaternion q);
            body.Position = p;
            body.Orientation = q;
            body.AddShape(SegmentShape(Segments[i]));
            body.SetMassInertia(body.Mass * Density);
            body.Friction = 0.7f;
            body.Damping = (0.002f, 0.02f);
            body.Tag = new RigidBodyTag();
            Bodies[i] = body;
        }

        // Joints are created in the standing pose, so every limit is relative to it.
        // The servos pull towards the relaxed pose instead.
        for (int i = 1; i < BoneCount; i++)
        {
            Bone bone = (Bone)i;
            var parent = Bodies[(int)Segments[i].Parent];
            var child = Bodies[i];
            float s = SideOf(bone);
            JVector center = JointCenter(bone, s);

            filter.IgnoreCollisionBetween(parent.Shapes[0], child.Shapes[0]);

            switch (bone)
            {
                case Bone.Chest:
                    BallJoint(world, bone, parent, child, center, JVector.UnitY, JVector.UnitY, 30, 35, 0.6f);
                    break;
                case Bone.Head:
                    BallJoint(world, bone, parent, child, center, JVector.UnitY, JVector.UnitY, 45, 70, 1.2f);
                    break;
                case Bone.UpperArmLeft or Bone.UpperArmRight:
                    BallJoint(world, bone, parent, child, center, JVector.Normalize(new JVector(s, -1, 0.5f)),
                        -JVector.UnitY, 100, 60, 0.6f);
                    break;
                case Bone.HandLeft or Bone.HandRight:
                    BallJoint(world, bone, parent, child, center, -JVector.UnitY, -JVector.UnitY, 60, 50, 0.8f);
                    break;
                case Bone.ThighLeft or Bone.ThighRight:
                    // Cone tilted forwards: lots of flexion, little extension.
                    BallJoint(world, bone, parent, child, center, JVector.Normalize(new JVector(0, -1, 1)),
                        -JVector.UnitY, 70, 35, 0.6f);
                    break;
                case Bone.ForearmLeft or Bone.ForearmRight:
                    // Positive hinge angles rotate the lower end backwards, so elbows flex negatively.
                    HingeJoint(world, parent, child, center, -145, 5, RelaxedHingeDegrees(bone), 0.8f);
                    break;
                case Bone.ShinLeft or Bone.ShinRight:
                    HingeJoint(world, parent, child, center, -5, 140, RelaxedHingeDegrees(bone), 0.7f);
                    break;
                case Bone.FootLeft or Bone.FootRight:
                    HingeJoint(world, parent, child, center, -35, 40, RelaxedHingeDegrees(bone), 1.0f);
                    break;
            }
        }

        // Hands do not need to collide with the thighs they rest against.
        for (int side = 0; side < 2; side++)
        {
            int o = side * 3;
            filter.IgnoreCollisionBetween(Bodies[(int)Bone.HandLeft + o].Shapes[0], Bodies[(int)Bone.ThighLeft + o].Shapes[0]);
            filter.IgnoreCollisionBetween(Bodies[(int)Bone.UpperArmLeft + o].Shapes[0], Bodies[(int)Bone.Pelvis].Shapes[0]);
        }

        Place(position, orientation);
    }

    private void BallJoint(World world, Bone bone, RigidBody parent, RigidBody child, JVector center,
        JVector coneAxisParent, JVector coneAxisChild, float coneDegrees, float twistDegrees, float strength)
    {
        var socket = world.CreateConstraint<BallSocket>(parent, child);
        socket.Initialize(center);

        var cone = world.CreateConstraint<ConeLimit>(parent, child);
        cone.Initialize(coneAxisParent, coneAxisChild, AngularLimit.FromDegree(0, coneDegrees));

        var twist = world.CreateConstraint<TwistAngle>(parent, child);
        twist.Initialize(coneAxisChild, coneAxisChild, AngularLimit.FromDegree(-twistDegrees, twistDegrees));

        var servo = world.CreateConstraint<AngularServo>(parent, child);
        servo.Initialize(parent.Orientation.Conjugate() * RelaxedRotation(bone) * child.Orientation);
        drives.Add(new Drive { Angular = servo, BaseTorque = strength * GravityTorque(child, center) });
    }

    private void HingeJoint(World world, RigidBody parent, RigidBody child, JVector center,
        float minDegrees, float maxDegrees, float restDegrees, float strength)
    {
        var hinge = new HingeJoint(world, parent, child, center, JVector.UnitX,
            AngularLimit.FromDegree(minDegrees, maxDegrees), JointDrive.Servo);
        hinge.Servo!.TargetAngle = JAngle.FromDegree(restDegrees);
        drives.Add(new Drive { Twist = hinge.Servo, BaseTorque = strength * GravityTorque(child, center) });
    }

    // Torque needed to hold the child's whole subtree horizontally against gravity in the standing pose.
    private float GravityTorque(RigidBody child, JVector center)
    {
        int index = Array.IndexOf(Bodies, child);
        float torque = 0;

        for (int i = 0; i < BoneCount; i++)
        {
            int j = i;
            while (j != index && j != 0) j = (int)Segments[j].Parent;
            if (j != index) continue;

            JVector lever = Bodies[i].Position - center;
            torque += (float)Bodies[i].Mass * Gravity * (float)lever.Length();
        }

        return torque;
    }

    /// Scales every servo's maximum torque; one means each joint can just hold its limbs out straight.
    public void SetStrength(float strength)
    {
        foreach (var drive in drives)
        {
            float torque = drive.BaseTorque * strength;
            if (drive.Angular != null) drive.Angular.MaximumTorque = torque;
            if (drive.Twist != null) drive.Twist.MaximumTorque = torque;
        }
    }

    /// Puts the mannequin into its relaxed pose, transformed rigidly, and stops it.
    public void Place(JVector position, JQuaternion orientation)
    {
        for (int i = 0; i < BoneCount; i++)
        {
            (JVector p, JQuaternion q) = RelaxedPose[i];
            var body = Bodies[i];
            body.Position = position + JVector.Transform(p, orientation);
            body.Orientation = orientation * q;
            body.Velocity = JVector.Zero;
            body.AngularVelocity = JVector.Zero;
        }
    }

    public void GetPose(Span<Matrix4> bones)
    {
        for (int i = 0; i < BoneCount; i++) bones[i] = Conversion.FromJitter(Bodies[i]);
    }
}

public class Demo37 : IDemo
{
    public string Name => "Wooden Mannequins";

    public string Description =>
        "Artist's mannequins whose joints carry torque-limited servos pulling them towards a relaxed posture.";

    public string Controls => "Use the slider to change muscle strength; Drop throws them in again.";

    private const int Count = 12;

    private static readonly Vector3 Wood = new(0.30f, 0.20f, 0.11f);

    private readonly List<Mannequin> mannequins = new();
    private readonly Matrix4[] pose = new Matrix4[Mannequin.BoneCount];
    private readonly WindowState controlsWindow = new();
    private RigidSkinDrawable drawable = null!;
    private float strength = 0.4f;
    private Random random = new(7);

    private static Texture2D? woodTexture;

    public void Build(Playground pg)
    {
        World world = pg.World;
        controlsWindow.Position = new System.Numerics.Vector2(Math.Max(16, pg.Width - 316), 16);

        pg.AddFloor();
        world.SubstepCount = 4;
        world.SolverIterations = (8, 2);

        var filter = new Common.IgnoreCollisionBetweenFilter();
        world.BroadPhaseFilter = filter;

        BuildScenery(world);

        mannequins.Clear();
        random = new Random(7);
        for (int i = 0; i < Count; i++)
        {
            var mannequin = new Mannequin(world, filter, JVector.Zero, JQuaternion.Identity);
            mannequin.SetStrength(strength);
            mannequins.Add(mannequin);
        }

        Drop();

        drawable = pg.GetDrawable<RigidSkinDrawable>();
        var mesh = Mannequin.BuildMesh(out int[] vertexBone);
        drawable.Configure(mesh, vertexBone, Mannequin.BoneCount, Count);
        drawable.Material = new Material
        {
            Tint = Wood,
            Specular = new Vector3(0.12f, 0.1f, 0.08f),
            Shininess = 24f,
            Alpha = 1f,
            VertexColorWeight = 0f,
            TextureWeight = 1f,
            Texture = WoodTexture()
        };
    }

    private static void BuildScenery(World world)
    {
        var dark = new Vector3(0.22f, 0.25f, 0.3f);

        // A staircase and a low bench to drape over.
        var stairs = world.CreateRigidBody();
        for (int i = 0; i < 5; i++)
        {
            stairs.AddShape(new TransformedShape(new BoxShape(3, 0.25f * (i + 1), 0.4f),
                new JVector(0, 0.125f * (i + 1), -0.4f * i)));
        }
        stairs.Position = new JVector(-2.5f, 0, -1);
        stairs.MotionType = MotionType.Static;
        stairs.Tag = new RigidBodyTag(false) { Color = dark };

        var bench = world.CreateRigidBody();
        bench.AddShape(new BoxShape(2.4f, 0.45f, 0.5f));
        bench.Position = new JVector(1.8f, 0.225f, 0);
        bench.MotionType = MotionType.Static;
        bench.Tag = new RigidBodyTag(false) { Color = dark };
    }

    private void Drop()
    {
        for (int i = 0; i < mannequins.Count; i++)
        {
            float x = (i % 4 - 1.5f) * 1.6f;
            float z = (i / 4 - 1) * 1.4f;
            float height = 0.6f + 0.7f * i;

            JQuaternion yaw = JQuaternion.CreateRotationY((float)(random.NextDouble() * 2 * Math.PI));
            JQuaternion tilt = JQuaternion.CreateRotationX((float)(random.NextDouble() - 0.5) * 1.6f);

            mannequins[i].Place(new JVector(x, height, z), yaw * tilt);
        }
    }

    public void DrawUpdate()
    {
        for (int i = 0; i < mannequins.Count; i++)
        {
            mannequins[i].GetPose(pose);
            drawable.SetPose(i, pose);
        }

        drawable.Commit(Wood);
    }

    public void DrawGui(Ui root)
    {
        root.Window(MaterialSymbols.Accessibility + " Mannequins", controlsWindow, 300f, controls =>
        {
            controls.ItemSpacing(8f);
            controls.Spacing(4f);
            if (controls.Slider("Muscle strength", ref strength, 0, 2, controls.AvailableWidth).Changed)
            {
                foreach (var mannequin in mannequins) mannequin.SetStrength(strength);
            }

            if (controls.Button("Drop").Clicked) Drop();
            controls.Spacing(4f);
        }, resizable: false, closable: false, id: "mannequin-controls");
    }

    // Procedural grain: fine streaks along the segment axis (texture v) with a slow wobble.
    private static unsafe Texture2D WoodTexture()
    {
        if (woodTexture != null) return woodTexture;

        const int size = 256;
        var pixels = new byte[size * size * 4];
        var noise = new Random(3);
        var phase = new float[size];
        for (int i = 0; i < size; i++) phase[i] = (float)noise.NextDouble();

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float wobble = 0.35f * MathF.Sin(2 * MathF.PI * (v + 0.3f * MathF.Sin(2 * MathF.PI * u * 2)));
                float grain = 0.5f + 0.5f * MathF.Sin(2 * MathF.PI * (u * 14 + wobble));
                grain = MathF.Pow(grain, 6);
                float fleck = 0.06f * phase[(x * 7 + y / 16 * 31) % size];

                float shade = 1 - 0.28f * grain - fleck;
                int o = (y * size + x) * 4;
                pixels[o + 0] = (byte)(255 * 0.52f * shade); // BGRA
                pixels[o + 1] = (byte)(255 * 0.74f * shade);
                pixels[o + 2] = (byte)(255 * 0.92f * shade);
                pixels[o + 3] = 255;
            }
        }

        woodTexture = new Texture2D();
        fixed (byte* p = pixels) woodTexture.LoadImage((IntPtr)p, size, size);
        woodTexture.SetWrap(TextureWrap.Repeat);
        return woodTexture;
    }
}
