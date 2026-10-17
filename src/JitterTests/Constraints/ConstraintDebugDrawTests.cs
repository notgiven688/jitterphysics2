using System.Runtime.InteropServices;
using Jitter2.SoftBodies;

namespace JitterTests.Constraints;

[TestFixture]
public class ConstraintDebugDrawTests
{
    private const uint Reference = 0x3182CEFF, Lower = 0xFF0000FF, Upper = 0x00FF00FF, Current = 0xFFFFFFFF;
    private static readonly DebugDrawSettings Settings = new()
    {
        Scale = 3,
        ReferenceColor = Reference,
        LowerLimitColor = Lower,
        UpperLimitColor = Upper,
        CurrentColor = Current
    };

    [Test]
    public void UncoloredSegmentsUseWhite()
    {
        var drawer = new RecordingDrawer();
        drawer.DrawSegment(JVector.Zero, JVector.UnitX);
        Assert.That(drawer.Segments, Is.EqualTo(new[] { (JVector.Zero, JVector.UnitX, DebugDrawColors.White) }));
    }

    [Test]
    public void PointMarkersUseTheRequestedRadiusAndColor()
    {
        var drawer = new RecordingDrawer();
        JVector center = new(3, -4, 5);
        const uint color = 0x12345678;
        Real radius = (Real)0.25;
        drawer.DrawPoint(center, radius, color);
        Assert.That(drawer.Segments.Count, Is.EqualTo(3));
        AssertSphere(drawer, color, center, radius);
        foreach (var segment in drawer.Segments)
        {
            AssertVector((segment.From + segment.To) * (Real)0.5, center);
            Assert.That(segment.Color, Is.EqualTo(color));
        }
        for (int i = 0; i < drawer.Segments.Count; i++)
            for (int j = i + 1; j < drawer.Segments.Count; j++)
            {
                var first = drawer.Segments[i];
                var second = drawer.Segments[j];
                Assert.That(JVector.Dot(first.To - first.From, second.To - second.From), Is.EqualTo(0).Within((Real)1e-5));
            }
    }

    [TestCase(1, 0, 0)]
    [TestCase(0, 1, 0)]
    [TestCase(0, 0, 1)]
    [TestCase(1, -2, 3)]
    public void ArrowHeadsHaveCircularBasesPerpendicularToTheirShafts(int x, int y, int z)
    {
        var drawer = new RecordingDrawer();
        JVector origin = new(3, -4, 5), vector = new(x, y, z), tip = origin + vector;
        drawer.DrawArrow(origin, vector, Lower);
        AssertVector(drawer.Segments[0].From, origin);
        AssertVector(drawer.Segments[0].To, tip);
        Assert.That(drawer.Segments.All(s => s.Color == Lower), Is.True);
        var baseVertices = drawer.Segments.Skip(1).SelectMany(s => new[] { s.From, s.To })
            .Where(p => p != tip).Distinct().ToArray();
        Assert.That(baseVertices.Length, Is.GreaterThanOrEqualTo(12));
        Real length = vector.Length();
        JVector axis = vector * (1 / length), root = origin + vector * (Real)0.8;
        foreach (JVector vertex in baseVertices)
        {
            Assert.That(JVector.Dot(vertex - tip, axis), Is.EqualTo(-length * (Real)0.2).Within((Real)1e-5));
            Assert.That((vertex - root).Length(), Is.EqualTo(length * (Real)0.08).Within((Real)1e-5));
        }
        Assert.That(JVector.Cross(baseVertices[1] - baseVertices[0], baseVertices[2] - baseVertices[0]).Length(),
            Is.GreaterThan((Real)1e-5));
    }

    [TestCase(0)]
    [TestCase(1e-13)]
    public void ZeroAndVeryShortArrowsProduceFiniteGeometry(double length)
    {
        var drawer = new RecordingDrawer();
        JVector origin = new(3, -4, 5), vector = JVector.UnitX * (Real)length;
        drawer.DrawArrow(origin, vector, Lower);
        Assert.That(drawer.Segments.Count, Is.EqualTo(1));
        AssertVector(drawer.Segments[0].From, origin);
        AssertVector(drawer.Segments[0].To, origin + vector);
    }

    [TestCase(3, 4)]
    [TestCase(4, 4)]
    [TestCase(5, 3)]
    [TestCase(12, 8)]
    [TestCase(20, 5)]
    public void ArrowDetailChangesTheClosedBaseAndSpokesMeetItsVertices(int segments, int spokes)
    {
        var drawer = new RecordingDrawer();
        JVector origin = new(3, -4, 5), vector = new(1, -2, 3), tip = origin + vector;
        drawer.DrawArrow(origin, vector, Lower, segments, spokes);
        var head = drawer.Segments.Skip(1).ToArray();
        var edges = head.Where(s => s.From != tip).ToArray();
        var ribs = head.Where(s => s.From == tip).ToArray();
        Assert.That(edges.Length, Is.EqualTo(segments));
        Assert.That(ribs.Length, Is.EqualTo(Math.Min(segments, spokes)));
        for (int i = 0; i < edges.Length; i++)
            Assert.That(edges[i].To, Is.EqualTo(edges[(i + 1) % edges.Length].From));
        foreach (var rib in ribs)
            Assert.That(edges.Any(edge => edge.From == rib.To), Is.True);
        Assert.That(ribs.Select(rib => rib.To).Distinct().Count(), Is.EqualTo(ribs.Length));
    }

    [TestCase("AngularMotor", 0, 3)]
    [TestCase("LinearMotor", 0, 2)]
    [TestCase("FixedAngle", 0, 6)]
    [TestCase("ConeLimit", 0, 2)]
    [TestCase("EllipticalConeLimit", 0, 2)]
    [TestCase("HingeAngle", 0, 3)]
    [TestCase("HingeAngle", 1, 4)]
    [TestCase("TwistAngle", 0, 3)]
    [TestCase("TwistAngle", 1, 4)]
    [TestCase("PointOnLine", 2, 2)]
    [TestCase("PointOnPlane", 0, 1)]
    public void EveryConstraintArrowUsesTheIndependentBaseAndSpokeSettings(string name, int range, int arrows)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        second.Position = new JVector(2, 3, 4);
        Constraint constraint = Create(world, first, second, name, range);
        var coarse = new RecordingDrawer();
        var detailedBase = new RecordingDrawer();
        var detailedSpokes = new RecordingDrawer();
        constraint.DebugDraw(coarse, Settings with { ArrowHeadSegments = 4, ArrowHeadSpokes = 2 });
        constraint.DebugDraw(detailedBase, Settings with { ArrowHeadSegments = 20, ArrowHeadSpokes = 2 });
        constraint.DebugDraw(detailedSpokes, Settings with { ArrowHeadSegments = 20, ArrowHeadSpokes = 8 });
        Assert.That(detailedBase.Segments.Count - coarse.Segments.Count, Is.EqualTo(16 * arrows));
        Assert.That(detailedSpokes.Segments.Count - detailedBase.Segments.Count, Is.EqualTo(6 * arrows));
    }

    [Test]
    public void SettingsOverloadFallsBackForExistingCustomDrawablesAndConstraints()
    {
        var legacy = new LegacyDrawable();
        IDebugDrawable drawable = legacy;
        drawable.DebugDraw(new RecordingDrawer(), Settings);
        Assert.That(legacy.Calls, Is.EqualTo(1));
        Constraint constraint = new LegacyConstraint();
        constraint.DebugDraw(new RecordingDrawer(), Settings);
        Assert.That(((LegacyConstraint)constraint).Calls, Is.EqualTo(1));
    }

    [Test]
    public void DefaultSettingsHaveUsefulValuesAndAllowTransparentBlack()
    {
        DebugDrawSettings settings = default;
        Assert.That(settings.Scale, Is.EqualTo(1));
        Assert.That(settings.Segments, Is.EqualTo(48));
        Assert.That(settings.ArrowHeadSegments, Is.EqualTo(12));
        Assert.That(settings.ArrowHeadSpokes, Is.EqualTo(8));
        Assert.That(settings.ShapeSubdivisions, Is.EqualTo(3));
        Assert.That(settings.Origin, Is.Null);
        Assert.That(settings.CurrentColor, Is.EqualTo(DebugDrawColors.White));
        Assert.That(settings.ReferenceColor, Is.EqualTo(DebugDrawColors.Green));
        Assert.That(settings.LowerLimitColor, Is.EqualTo(DebugDrawColors.Red));
        Assert.That((settings with { CurrentColor = 0 }).CurrentColor, Is.Zero);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void InvalidScalesAreRejected(double scale) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        _ = new DebugDrawSettings { Scale = (Real)scale });

    [TestCase(0)]
    [TestCase(7)]
    [TestCase(257)]
    public void InvalidDetailIsRejected(int segments) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        _ = new DebugDrawSettings { Segments = segments });

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(257)]
    public void InvalidArrowHeadDetailIsRejected(int segments) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        _ = new DebugDrawSettings { ArrowHeadSegments = segments });

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(257)]
    public void InvalidArrowHeadSpokesAreRejected(int spokes) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        _ = new DebugDrawSettings { ArrowHeadSpokes = spokes });

    [TestCase(-1)]
    [TestCase(0)]
    public void InvalidShapeSubdivisionsAreRejected(int subdivisions) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        _ = new DebugDrawSettings { ShapeSubdivisions = subdivisions });

    [Test]
    public void InvalidOriginIsRejected() => Assert.Throws<ArgumentException>(() =>
        _ = new DebugDrawSettings { Origin = new JVector(Real.NaN, 0, 0) });

    private static IEnumerable<TestCaseData> ConstraintCases()
    {
        string[] names = ["BallSocket", "DistanceLimit", "PointOnLine", "PointOnPlane", "ConeLimit", "EllipticalConeLimit",
            "FixedAngle", "HingeAngle", "TwistAngle", "AngularMotor", "LinearMotor", "SpringConstraint"];
        foreach (string name in names)
            for (int range = 0; range < 4; range++) yield return new TestCaseData(name, range);
    }

    [TestCaseSource(nameof(ConstraintCases))]
    public void DrawingUsesFiniteSegmentsAndDoesNotChangePhysics(string name, int range)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Position = new JVector(-4, 2, 1);
        second.Position = new JVector(1, 3, 2);
        first.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        second.Orientation = JQuaternion.Normalize(new JQuaternion(-2, 1, 3, 5));
        Constraint constraint = Create(world, first, second, name, range);
        var drawer = new RecordingDrawer();
        for (int step = 0; step < 2; step++)
        {
            byte[] constraintBefore = Snapshot(constraint);
            byte[] firstBefore = Snapshot(ref first.Data), secondBefore = Snapshot(ref second.Data);
            ((IDebugDrawable)constraint).DebugDraw(drawer, Settings);
            constraint.DebugDraw(new RecordingDrawer());
            Assert.That(Snapshot(constraint), Is.EqualTo(constraintBefore));
            Assert.That(Snapshot(ref first.Data), Is.EqualTo(firstBefore));
            Assert.That(Snapshot(ref second.Data), Is.EqualTo(secondBefore));
            Assert.That(drawer.Segments, Is.Not.Empty);
            world.Step((Real)0.01, false);
        }
    }

    [TestCase("BallSocket")]
    [TestCase("DistanceLimit")]
    [TestCase("PointOnLine")]
    [TestCase("PointOnPlane")]
    [TestCase("SpringConstraint")]
    public void AnchorMarkersFollowBodyTransformsAndDrawingScale(string name)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Position = new JVector(2, 3, 4);
        second.Position = new JVector(-3, 7, 9);
        JVector secondLocalAnchor = name == "BallSocket" ? first.Position - second.Position : JVector.Zero;
        Constraint constraint = Create(world, first, second, name, 0);
        first.Orientation = JQuaternion.CreateRotationX((Real)0.4);
        second.Orientation = JQuaternion.CreateRotationZ((Real)0.7);
        first.Position += new JVector(5, 2, -3);
        second.Position += new JVector(-1, 4, 2);
        JVector secondAnchor = second.Position + JVector.Transform(secondLocalAnchor, second.Orientation);
        foreach (int scale in new[] { 1, 4 })
        {
            var drawer = new RecordingDrawer();
            constraint.DebugDraw(drawer, Settings with { Scale = scale });
            AssertPointMarker(drawer, Reference, first.Position, scale * (Real)0.06);
            AssertPointMarker(drawer, Current, secondAnchor, scale * (Real)0.06);
        }
    }

    [TestCase(1)]
    [TestCase(13)]
    public void DistanceShellsUseTargetDistancePlusOffsetsRegardlessOfDrawingScale(int scale)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        JVector anchor = new(4, -3, 2);
        var constraint = world.CreateConstraint<DistanceLimit>(world.NullBody, body);
        constraint.Initialize(anchor, anchor + JVector.UnitX * 5, new LinearLimit(-2, 1));
        body.Position += JVector.UnitY * 2;
        var drawer = new RecordingDrawer();
        constraint.DebugDraw(drawer, Settings with { Scale = scale });
        AssertSphere(drawer, Lower, anchor, 3);
        AssertSphere(drawer, Upper, anchor, 6);
    }

    [Test]
    public void LineStopsFollowTheRotatedAnchorFrameAndKeepTheirPhysicalPositions()
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Orientation = JQuaternion.CreateRotationY((Real)0.3);
        JVector anchor = new(2, 4, 3);
        var line = world.CreateConstraint<PointOnLine>(first, second);
        line.Initialize(JVector.UnitX, anchor, anchor, new LinearLimit(-2, 5));
        JQuaternion rotation = JQuaternion.CreateRotationZ((Real)0.7);
        first.Orientation = rotation * first.Orientation;
        first.Position = new JVector(5, 1, 2);
        JVector transformedAnchor = first.Position + JVector.Transform(anchor, rotation);
        JVector axis = JVector.Transform(JVector.UnitX, rotation);
        var drawer = new RecordingDrawer();
        line.DebugDraw(drawer, Settings with { Scale = 12 });
        AssertStop(drawer, Lower, transformedAnchor - axis * 2);
        AssertStop(drawer, Upper, transformedAnchor + axis * 5);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PlaneBoundariesUseSignedOffsetsOrTheFixedMidpoint(bool fixedLimit)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        JVector anchor = new(2, 3, 4);
        var plane = world.CreateConstraint<PointOnPlane>(first, second);
        plane.Initialize(JVector.UnitY, anchor, anchor, fixedLimit ? new LinearLimit(4, 2) : new LinearLimit(-2, 4));
        JQuaternion rotation = JQuaternion.CreateRotationZ((Real)0.7);
        first.Orientation = rotation;
        JVector origin = JVector.Transform(anchor, rotation), normal = JVector.Transform(JVector.UnitY, rotation);
        var drawer = new RecordingDrawer();
        plane.DebugDraw(drawer, Settings);
        foreach (var segment in drawer.Segments.Where(s => s.Color == Upper || s.Color == Lower))
        {
            Real expected = fixedLimit ? 3 : segment.Color == Lower ? -2 : 4;
            Assert.That(JVector.Dot(segment.From - origin, normal), Is.EqualTo(expected).Within((Real)1e-5));
            Assert.That(JVector.Dot(segment.To - origin, normal), Is.EqualTo(expected).Within((Real)1e-5));
        }
        Assert.That(drawer.Segments.Count(s => s.Color == Upper), Is.EqualTo(14));
        Assert.That(drawer.Segments.Count(s => s.Color == Lower), Is.EqualTo(fixedLimit ? 0 : 14));
    }

    [TestCase(-0.7)]
    [TestCase(0.7)]
    public void HingeCurrentRayMatchesBodyRotationWithArbitraryInitialOrientations(double angle)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        second.Orientation = JQuaternion.Normalize(new JQuaternion(-2, 1, 3, 5));
        JVector axis = JVector.Normalize(new JVector(1, 2, 3));
        JVector localAxis = JVector.ConjugatedTransform(axis, second.Orientation);
        JVector localReference = -MathHelper.CreateOrthonormal(localAxis);
        var hinge = world.CreateConstraint<HingeAngle>(first, second);
        hinge.Initialize(axis, AngularLimit.FromDegree(-60, 60));
        JQuaternion common = JQuaternion.CreateRotationX((Real)0.4);
        first.Orientation = common * first.Orientation;
        axis = JVector.Transform(axis, common);
        second.Orientation = JQuaternion.CreateFromAxisAngle(axis, (Real)angle) * common * second.Orientation;
        var drawer = new RecordingDrawer();
        JVector origin = new(4, 5, 6);
        hinge.DebugDraw(drawer, Settings with { Origin = origin });
        var ray = drawer.Segments.First(s => s.Color == Current);
        AssertVector(ray.From, origin);
        AssertVector(ray.To, origin + JVector.Transform(localReference, second.Orientation) * Settings.Scale);
        Assert.That(hinge.Angle.Radian, Is.EqualTo(angle).Within((Real)1e-5));
    }

    [Test]
    public void FixedAngleReferenceUsesTheStoredRelativeOrientation()
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Orientation = JQuaternion.CreateRotationY((Real)0.4);
        second.Orientation = JQuaternion.CreateRotationZ((Real)0.7);
        JQuaternion initialSecond = second.Orientation;
        var constraint = world.CreateConstraint<FixedAngle>(first, second);
        constraint.Initialize();
        JQuaternion rotation = JQuaternion.CreateRotationX((Real)0.5);
        first.Orientation = rotation * first.Orientation;
        var drawer = new RecordingDrawer();
        JVector origin = new(4, 5, 6);
        constraint.DebugDraw(drawer, Settings with { Origin = origin });
        AssertVector(drawer.Segments[0].To, origin + (rotation * initialSecond).GetBasisX() * Settings.Scale);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void JointUsesItsAnchorOrExplicitOriginForAngularGuides(bool explicitOrigin)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Position = new JVector(-10, 0, 0);
        JVector anchor = new(3, 4, 5), custom = new(6, 7, 8);
        var joint = new HingeJoint(world, first, second, anchor, JVector.UnitZ, AngularLimit.FromDegree(-60, 60));
        var drawer = new RecordingDrawer();
        joint.DebugDraw(drawer, Settings with { Origin = explicitOrigin ? custom : null });
        JVector origin = explicitOrigin ? custom : anchor;
        Assert.That((drawer.Segments[0].From - origin).Length(), Is.EqualTo(Settings.Scale).Within((Real)1e-5));
        Assert.That((drawer.Segments[0].To - origin).Length(), Is.EqualTo(Settings.Scale).Within((Real)1e-5));
        joint.Remove();
        Assert.DoesNotThrow(() => joint.DebugDraw(drawer));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void WorldAnchorsDoNotDrawSpuriousLinesFromTheNullBody(bool distanceLimit)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.Position = new JVector(5, 6, 7);
        JVector anchor = new(4, 3, 2);
        Constraint constraint;
        if (distanceLimit)
        {
            var distance = world.CreateConstraint<DistanceLimit>(world.NullBody, body);
            distance.Initialize(anchor, body.Position);
            constraint = distance;
        }
        else
        {
            var socket = world.CreateConstraint<BallSocket>(world.NullBody, body);
            socket.Initialize(anchor);
            constraint = socket;
        }
        var drawer = new RecordingDrawer();
        constraint.DebugDraw(drawer, Settings);
        Assert.That(drawer.Segments.Any(s => s.From == JVector.Zero || s.To == JVector.Zero), Is.False);
    }

    [TestCase("ConeLimit")]
    [TestCase("EllipticalConeLimit")]
    [TestCase("HingeAngle")]
    [TestCase("TwistAngle")]
    [TestCase("FixedAngle")]
    [TestCase("AngularMotor")]
    [TestCase("LinearMotor")]
    public void WorldRelativeOrientationAndMotorGuidesStayNearThePhysicalBody(string name)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.Position = new JVector(30, 40, 50);
        Constraint constraint = Create(world, world.NullBody, body, name, 0);
        var drawer = new RecordingDrawer();
        constraint.DebugDraw(drawer, Settings);
        Assert.That(drawer.Segments, Is.Not.Empty);
        foreach (var segment in drawer.Segments)
        {
            Assert.That((segment.From - body.Position).Length(), Is.LessThanOrEqualTo(2 * Settings.Scale));
            Assert.That((segment.To - body.Position).Length(), Is.LessThanOrEqualTo(2 * Settings.Scale));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void JointMatchesAngularGuidesToTheCorrectBodyPair(bool reverseSocketBodies)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody(), third = world.CreateRigidBody();
        var unrelated = world.CreateConstraint<BallSocket>(first, third);
        unrelated.Initialize(new JVector(100, 0, 0));
        var hinge = world.CreateConstraint<HingeAngle>(first, second);
        hinge.Initialize(JVector.UnitZ, AngularLimit.FromDegree(-60, 60));
        var socket = world.CreateConstraint<BallSocket>(reverseSocketBodies ? second : first, reverseSocketBodies ? first : second);
        JVector anchor = new(3, 4, 5);
        socket.Initialize(anchor);
        var joint = new DrawingJoint(hinge, unrelated, socket);
        var drawer = new RecordingDrawer();
        joint.DebugDraw(drawer, Settings);
        Assert.That((drawer.Segments[0].From - anchor).Length(), Is.EqualTo(Settings.Scale).Within((Real)1e-5));
    }

    [Test]
    public void SpringCoilRetainsContinuityAndExactEndpoints()
    {
        var drawer = new RecordingDrawer();
        JVector reference = JVector.UnitX;
        JVector[]? previous = null;
        for (int i = 0; i <= 1200; i++)
        {
            Real angle = 2 * MathR.PI * i / 1200;
            JVector end = new(MathR.Cos(angle), (Real)0.8 * MathR.Sin(angle), 1);
            drawer.Segments.Clear();
            drawer.DrawSpring(JVector.Zero, end, ref reference, (Real)0.12, Current);
            AssertVector(drawer.Segments[0].From, JVector.Zero);
            Assert.That(drawer.Segments[^1].To, Is.EqualTo(end));
            JVector[] vertices = drawer.Segments.Select(s => s.To).ToArray();
            if (previous != null)
                Assert.That(vertices.Zip(previous, (a, b) => (a - b).Length()).Max(), Is.LessThan((Real)0.02));
            previous = vertices;
        }
    }

    [TestCase(0)]
    [TestCase(180)]
    public void DegenerateConeBoundariesProduceFiniteGeometry(int degrees)
    {
        var drawer = new RecordingDrawer();
        drawer.DrawCone(JVector.Zero, JVector.UnitZ, JVector.UnitX, JAngle.FromDegree(degrees),
            JAngle.FromDegree(degrees), 1, Upper);
        foreach (var segment in drawer.Segments)
            Assert.That(segment.To.Length(), Is.EqualTo(1).Within((Real)1e-5));
    }

    [TestCase(0)]
    [TestCase(1e-8)]
    public void CoincidentAndVeryShortSpringsCanBeDrawn(double distance)
    {
        using World world = new();
        var spring = world.CreateConstraint<SpringConstraint>(world.NullBody, world.CreateRigidBody());
        spring.Initialize(JVector.Zero, JVector.UnitX * (Real)distance);
        var drawer = new RecordingDrawer();
        spring.DebugDraw(drawer);
        AssertPointMarker(drawer, DebugDrawColors.Green, spring.Anchor1, (Real)0.06);
        AssertPointMarker(drawer, DebugDrawColors.White, spring.Anchor2, (Real)0.06);
    }

    private static Constraint Create(World world, RigidBody first, RigidBody second, string name, int range)
    {
        JVector axis = JVector.Normalize(new JVector(1, 2, 3));
        LinearLimit linear = range switch
        {
            0 => new(-2, 3),
            1 => LinearLimit.Fixed,
            2 => LinearLimit.Full,
            _ => new(Real.NegativeInfinity, 2)
        };
        AngularLimit angular = range switch
        {
            0 => AngularLimit.FromDegree(-30, 60),
            1 => AngularLimit.Fixed,
            _ => AngularLimit.Full
        };
        switch (name)
        {
            case "BallSocket":
                var socket = world.CreateConstraint<BallSocket>(first, second);
                socket.Initialize(first.Position); return socket;
            case "DistanceLimit":
                var distance = world.CreateConstraint<DistanceLimit>(first, second);
                distance.Initialize(first.Position, second.Position, linear); return distance;
            case "PointOnLine":
                var line = world.CreateConstraint<PointOnLine>(first, second);
                line.Initialize(axis, first.Position, second.Position, linear); return line;
            case "PointOnPlane":
                var plane = world.CreateConstraint<PointOnPlane>(first, second);
                plane.Initialize(axis, first.Position, second.Position, linear); return plane;
            case "ConeLimit":
                var cone = world.CreateConstraint<ConeLimit>(first, second);
                cone.Initialize(axis, range == 0 ? AngularLimit.FromDegree(20, 60) :
                    range == 1 ? AngularLimit.FromDegree(45, 45) : AngularLimit.FromDegree(0, 180)); return cone;
            case "EllipticalConeLimit":
                var ellipse = world.CreateConstraint<EllipticalConeLimit>(first, second);
                ellipse.Initialize(axis, MathHelper.CreateOrthonormal(axis), AngularLimit.FromDegree(range == 0 ? 20 : 0, 60),
                    AngularLimit.FromDegree(range == 0 ? 10 : 0, 30)); return ellipse;
            case "FixedAngle":
                var fixedAngle = world.CreateConstraint<FixedAngle>(first, second);
                fixedAngle.Initialize(); return fixedAngle;
            case "HingeAngle":
                var hinge = world.CreateConstraint<HingeAngle>(first, second);
                hinge.Initialize(axis, angular); return hinge;
            case "TwistAngle":
                var twist = world.CreateConstraint<TwistAngle>(first, second);
                twist.Initialize(axis, JVector.UnitX, angular); return twist;
            case "AngularMotor":
                var angularMotor = world.CreateConstraint<AngularMotor>(first, second);
                angularMotor.Initialize(axis); angularMotor.TargetVelocity = range - 1; return angularMotor;
            case "LinearMotor":
                var linearMotor = world.CreateConstraint<LinearMotor>(first, second);
                linearMotor.Initialize(axis, JVector.UnitX); linearMotor.TargetVelocity = range - 1; return linearMotor;
            default:
                var spring = world.CreateConstraint<SpringConstraint>(first, second);
                spring.Initialize(first.Position, second.Position); return spring;
        }
    }

    private static byte[] Snapshot<T>(ref T data) where T : unmanaged =>
        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref data, 1)).ToArray();
    private static byte[] Snapshot(Constraint constraint) => constraint.IsSmallConstraint
        ? Snapshot(ref constraint.SmallHandle.Data) : Snapshot(ref constraint.Handle.Data);
    private static void AssertVector(JVector actual, JVector expected) =>
        Assert.That((actual - expected).Length(), Is.LessThan((Real)1e-5));
    private static void AssertPointMarker(RecordingDrawer drawer, uint color, JVector center, Real radius) =>
        Assert.That(drawer.Segments.Count(s => s.Color == color &&
            MathR.Abs((s.From - center).Length() - radius) < (Real)1e-5 &&
            MathR.Abs((s.To - center).Length() - radius) < (Real)1e-5), Is.EqualTo(3));
    private static void AssertSphere(RecordingDrawer drawer, uint color, JVector origin, Real radius)
    {
        var segments = drawer.Segments.Where(s => s.Color == color).ToArray();
        Assert.That(segments, Is.Not.Empty);
        foreach (var segment in segments)
        {
            Assert.That((segment.From - origin).Length(), Is.EqualTo(radius).Within((Real)1e-5));
            Assert.That((segment.To - origin).Length(), Is.EqualTo(radius).Within((Real)1e-5));
        }
    }
    private static void AssertStop(RecordingDrawer drawer, uint color, JVector expected)
    {
        var segments = drawer.Segments.Where(s => s.Color == color).ToArray();
        Assert.That(segments.Length, Is.EqualTo(2));
        foreach (var segment in segments) AssertVector((segment.From + segment.To) * (Real)0.5, expected);
    }

    private sealed class RecordingDrawer : IDebugDrawer
    {
        public readonly List<(JVector From, JVector To, uint Color)> Segments = [];
        public void DrawSegment(in JVector a, in JVector b, uint color)
        {
            AssertFinite(a); AssertFinite(b); Segments.Add((a, b, color));
        }
        private static void AssertFinite(JVector value) =>
            Assert.That(Real.IsFinite(value.X) && Real.IsFinite(value.Y) && Real.IsFinite(value.Z), Is.True);
    }
    private sealed class LegacyDrawable : IDebugDrawable
    {
        public int Calls;
        public void DebugDraw(IDebugDrawer drawer) => Calls++;
    }
    private sealed class LegacyConstraint : Constraint
    {
        public int Calls;
        public override void DebugDraw(IDebugDrawer drawer) => Calls++;
    }

    private sealed class DrawingJoint : Joint
    {
        public DrawingJoint(params Constraint[] constraints)
        {
            foreach (Constraint constraint in constraints) Register(constraint);
        }
    }
}
