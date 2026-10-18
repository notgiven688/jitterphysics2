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
        CurrentColor = Current,
        SolidArrows = false
    };

    [Test]
    public void UncoloredSegmentsUseWhite()
    {
        var drawer = new RecordingDrawer();
        drawer.DrawSegment(JVector.Zero, JVector.UnitX);
        Assert.That(drawer.Segments, Is.EqualTo(new[] { (JVector.Zero, JVector.UnitX, DebugDrawColors.White) }));
    }

    [Test]
    public void TriangleFallbackPreservesVertexOrderAndColorForSegmentOnlyDrawers()
    {
        var recorder = new RecordingDrawer();
        IDebugDrawer drawer = recorder;
        JVector a = new(2, -3, 4), b = new(-5, 6, 7), c = new(8, 9, -1);
        const uint color = 0x12345678;
        drawer.DrawTriangle(a, b, c, color);
        Assert.That(recorder.Segments, Is.EqualTo(new[] { (a, b, color), (b, c, color), (c, a, color) }));
    }

    [Test]
    public void TriangleOverridesReceiveTheOriginalVerticesAndColorThroughTheInterface()
    {
        var recorder = new TriangleDrawer();
        IDebugDrawer drawer = recorder;
        JVector a = new(2, -3, 4), b = new(-5, 6, 7), c = new(8, 9, -1);
        const uint color = 0x12345678;
        drawer.DrawTriangle(a, b, c, color);
        Assert.That(recorder.Triangles, Is.EqualTo(new[] { (a, b, c, color) }));
        Assert.That(recorder.Segments, Is.Empty);
    }

    [Test]
    public void PointMarkersUseTheRequestedRadiusAndColor()
    {
        var drawer = new TriangleDrawer();
        JVector center = new(3, -4, 5);
        const uint color = 0x12345678;
        Real radius = (Real)0.25;
        drawer.DrawPoint(center, radius, color);
        Assert.That(drawer.Segments, Is.Empty);
        Assert.That(drawer.Triangles.Count, Is.EqualTo(80));
        var edges = new Dictionary<(JVector, JVector), int>();
        var vertices = new HashSet<JVector>();
        foreach (var (a, b, c, triangleColor) in drawer.Triangles)
        {
            Assert.That(triangleColor, Is.EqualTo(color));
            Assert.That(JVector.Dot((b - a) % (c - a), (a + b + c) * ((Real)1 / 3) - center), Is.GreaterThan(0));
            foreach (JVector vertex in new[] { a, b, c })
            {
                Assert.That((vertex - center).Length(), Is.EqualTo(radius).Within((Real)1e-5));
                vertices.Add(vertex);
            }
            foreach (var edge in new[] { (a, b), (b, c), (c, a) })
                edges[edge] = edges.GetValueOrDefault(edge) + 1;
        }
        Assert.That(vertices.Count, Is.EqualTo(42));
        foreach (var (edge, count) in edges)
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(edges.GetValueOrDefault((edge.Item2, edge.Item1)), Is.EqualTo(1));
        }
    }

    [Test]
    public void PointMarkersFallBackToTriangleEdgesForSegmentOnlyDrawers()
    {
        var solid = new TriangleDrawer();
        var wire = new RecordingDrawer();
        JVector center = new(-2, 3, 4);
        solid.DrawPoint(center, (Real)0.1, Current);
        wire.DrawPoint(center, (Real)0.1, Current);
        var expected = solid.Triangles.SelectMany(t => new[]
            { (t.A, t.B, t.Color), (t.B, t.C, t.Color), (t.C, t.A, t.Color) });
        Assert.That(wire.Segments, Is.EqualTo(expected));
    }

    [Test]
    public void ZeroRadiusPointMarkersEmitNoGeometry()
    {
        var drawer = new TriangleDrawer();
        drawer.DrawPoint(JVector.One, 0, Current);
        Assert.That(drawer.Triangles, Is.Empty);
        Assert.That(drawer.Segments, Is.Empty);
    }

    [Test]
    public void DrawingPointMarkersDoesNotAllocateAfterInitialization()
    {
        var drawer = new CountingDrawer();
        for (int i = 0; i < 10; i++) drawer.DrawPoint(JVector.One, (Real)0.1, Current);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10; i++) drawer.DrawPoint(JVector.One, (Real)0.1, Current);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.Zero);
        Assert.That(drawer.Count, Is.GreaterThan(0));
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
        Real headSize = MathR.Min(DebugDrawSettings.Default.ArrowHeadSize, length * (Real)0.5);
        JVector axis = vector * (1 / length), root = tip - axis * headSize;
        foreach (JVector vertex in baseVertices)
        {
            Assert.That(JVector.Dot(vertex - tip, axis), Is.EqualTo(-headSize).Within((Real)1e-5));
            Assert.That((vertex - root).Length(), Is.EqualTo(headSize * (Real)0.4).Within((Real)1e-5));
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

    [TestCase(false)]
    [TestCase(true)]
    public void FrameColorsFollowLocalAxesThroughRotation(bool useSettings)
    {
        var drawer = new RecordingDrawer();
        JVector center = new(3, -4, 5);
        JQuaternion orientation = JQuaternion.Normalize(new JQuaternion(2, -1, 3, 4));
        var settings = new DebugDrawSettings
        {
            Scale = 2,
            FrameSize = 2,
            AxisXColor = 0xC864325A,
            AxisYColor = 0x2864C880,
            AxisZColor = 0x64C82800,
            ArrowHeadSegments = 5,
            ArrowHeadSpokes = 3,
            SolidArrows = false
        };
        if (useSettings) drawer.DrawFrame(center, orientation, settings);
        else drawer.DrawFrame(center, orientation, settings.FrameSize,
            settings.AxisXColor, settings.AxisYColor, settings.AxisZColor, 5, 3);

        var axes = new[] { JVector.UnitX, JVector.UnitY, JVector.UnitZ };
        var colors = new[] { settings.AxisXColor, settings.AxisYColor, settings.AxisZColor };
        for (int i = 0; i < axes.Length; i++)
        {
            var segments = drawer.Segments.Where(s => s.Color == colors[i]).ToArray();
            Assert.That(segments.Length, Is.EqualTo(9));
            AssertVector(segments[0].From, center);
            AssertVector(segments[0].To, center + JVector.Transform(axes[i], orientation) * 2);
        }
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

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void JointPreservesInheritedDrawingOverridesIncludingCallsToBase(bool useSettings, bool callBase)
    {
        using World world = new();
        var socket = world.CreateConstraint<InheritedLegacyBallSocket>(world.NullBody, world.CreateRigidBody());
        socket.Initialize(new JVector(3, 4, 5));
        socket.CallBase = callBase;
        var joint = new DrawingJoint(socket);
        var drawer = new RecordingDrawer();
        if (useSettings) joint.DebugDraw(drawer, Settings);
        else joint.DebugDraw(drawer);
        Assert.That(socket.Calls, Is.EqualTo(1));
        Assert.That(drawer.Segments.Count > 0, Is.EqualTo(callBase));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void JointPrefersTheSettingsOverrideWhenTheSubclassProvidesBothOverloads(bool useSettings)
    {
        using World world = new();
        var socket = world.CreateConstraint<ConfiguredBallSocket>(world.NullBody, world.CreateRigidBody());
        JVector anchor = new(3, 4, 5);
        socket.Initialize(anchor);
        var joint = new DrawingJoint(socket);
        var drawer = new RecordingDrawer();
        if (useSettings) joint.DebugDraw(drawer, Settings);
        else joint.DebugDraw(drawer);
        Assert.That(socket.ConfiguredCalls, Is.EqualTo(1));
        Assert.That(socket.Calls, Is.Zero);
        Assert.That(socket.LastSettings.Scale, Is.EqualTo(useSettings ? Settings.Scale : (Real)1));
        Assert.That(socket.LastSettings.Origin, Is.EqualTo(anchor));
        Assert.That(drawer.Segments, Is.Not.Empty);
    }

    [Test]
    public void JointKeepsSettingsForSubclassesWithoutDrawingOverrides()
    {
        using World world = new();
        var socket = world.CreateConstraint<UnmodifiedBallSocket>(world.NullBody, world.CreateRigidBody());
        JVector anchor = new(3, 4, 5);
        socket.Initialize(anchor);
        var joint = new DrawingJoint(socket);
        var drawer = new RecordingDrawer();
        joint.DebugDraw(drawer, Settings);
        AssertPointMarker(drawer, Reference, anchor, Settings.Scale * (Real)0.06);
        AssertPointMarker(drawer, Current, anchor, Settings.Scale * (Real)0.06);
    }

    [Test]
    public void JointDrawingDoesNotAllocateAfterDispatchInitialization()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        var socket = world.CreateConstraint<UnmodifiedBallSocket>(world.NullBody, body);
        socket.Initialize(JVector.One);
        var customized = world.CreateConstraint<InheritedLegacyBallSocket>(world.NullBody, body);
        customized.Initialize(JVector.One);
        customized.CallBase = true;
        var joint = new DrawingJoint(socket, customized);
        var drawer = new CountingDrawer();
        for (int i = 0; i < 10; i++) joint.DebugDraw(drawer, Settings);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10; i++) joint.DebugDraw(drawer, Settings);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.Zero);
        Assert.That(drawer.Count, Is.GreaterThan(0));
    }

    [Test]
    public void DefaultSettingsHaveUsefulValuesAndAllowTransparentBlack()
    {
        DebugDrawSettings settings = default;
        Assert.That(settings.Scale, Is.EqualTo(1));
        Assert.That(settings.FrameSize, Is.EqualTo(1));
        Assert.That(settings.Segments, Is.EqualTo(48));
        Assert.That(settings.ArrowHeadSegments, Is.EqualTo(12));
        Assert.That(settings.ArrowHeadSpokes, Is.EqualTo(8));
        Assert.That(settings.ArrowHeadSize, Is.EqualTo((Real)0.2));
        Assert.That(settings.ShapeSubdivisions, Is.EqualTo(3));
        Assert.That(settings.Origin, Is.Null);
        Assert.That(settings.CurrentColor, Is.EqualTo(DebugDrawColors.White));
        Assert.That(settings.ReferenceColor, Is.EqualTo(DebugDrawColors.Green));
        Assert.That(settings.LowerLimitColor, Is.EqualTo(DebugDrawColors.Red));
        Assert.That((settings with { CurrentColor = 0 }).CurrentColor, Is.Zero);
        Assert.That(settings.AxisXColor, Is.EqualTo(DebugDrawColors.AxisX));
        Assert.That(settings.AxisYColor, Is.EqualTo(DebugDrawColors.AxisY));
        Assert.That(settings.AxisZColor, Is.EqualTo(DebugDrawColors.AxisZ));
        Assert.That(settings.SolidArrows, Is.True);
        Assert.That(settings.ArrowShaftRadius, Is.EqualTo((Real)0.025));
        Assert.That((settings with { AxisXColor = 0 }).AxisXColor, Is.Zero);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void InvalidScalesAreRejected(double scale) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        _ = new DebugDrawSettings { Scale = (Real)scale });

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void InvalidFrameSizesAreRejected(double size) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        _ = new DebugDrawSettings { FrameSize = (Real)size });

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

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void InvalidArrowShaftRadiusIsRejected(double radius) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        _ = new DebugDrawSettings { ArrowShaftRadius = (Real)radius });

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void InvalidArrowHeadSizesAreRejected(double size) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        _ = new DebugDrawSettings { ArrowHeadSize = (Real)size });

    [TestCase(1, 0, 0, 3)]
    [TestCase(0, 1, 0, 12)]
    [TestCase(0, 0, 1, 20)]
    [TestCase(1, -2, 3, 12)]
    public void SolidArrowsAreClosedOutwardFacingMeshes(int x, int y, int z, int segments)
    {
        var drawer = new TriangleDrawer();
        JVector origin = new(3, -4, 5), vector = new(x, y, z);
        Real radius = (Real)0.04;
        drawer.DrawSolidArrow(origin, vector, radius, Current, segments);
        Assert.That(drawer.Segments, Is.Empty);
        Assert.That(drawer.Triangles.Count, Is.EqualTo(6 * segments));
        var edges = new Dictionary<(JVector, JVector), int>();
        Real volume = 0, length = vector.Length();
        JVector axis = vector * (1 / length);
        Real headSize = MathR.Min(DebugDrawSettings.Default.ArrowHeadSize, length * (Real)0.5);
        Real headRadius = headSize * (Real)0.4;
        Real shaftLength = length - headSize;
        foreach (var (a, b, c, color) in drawer.Triangles)
        {
            Assert.That(color, Is.EqualTo(Current));
            JVector normal = (b - a) % (c - a), center = (a + b + c) * ((Real)1 / 3) - origin;
            Assert.That(normal.LengthSquared(), Is.GreaterThan((Real)1e-14));
            Real height = JVector.Dot(center, axis);
            if (MathR.Abs(height) < (Real)1e-5 || MathR.Abs(height - shaftLength) < (Real)1e-5)
                Assert.That(JVector.Dot(normal, axis), Is.LessThan(0));
            else Assert.That(JVector.Dot(normal, center - axis * height), Is.GreaterThan(0));
            volume += JVector.Dot(a - origin, (b - origin) % (c - origin)) / 6;
            foreach (var edge in new[] { (a, b), (b, c), (c, a) })
                edges[edge] = edges.GetValueOrDefault(edge) + 1;
        }
        foreach (var (edge, count) in edges)
        {
            Assert.That(count, Is.EqualTo(1));
            Assert.That(edges.GetValueOrDefault((edge.Item2, edge.Item1)), Is.EqualTo(1));
        }
        Real polygonArea = segments * (Real)0.5 * MathR.Sin(2 * MathR.PI / segments);
        Real expectedVolume = polygonArea * (radius * radius * shaftLength + headRadius * headRadius * headSize / 3);
        Assert.That(volume, Is.EqualTo(expectedVolume).Within(expectedVolume * (Real)1e-4));
    }

    [Test]
    public void SolidArrowThicknessIgnoresGuideScaleAndClampsToFitTheHead()
    {
        foreach (Real scale in new[] { (Real)1, (Real)2 })
        {
            var drawer = new TriangleDrawer();
            var settings = new DebugDrawSettings { Scale = scale, ArrowShaftRadius = (Real)0.04 };
            drawer.DrawArrow(JVector.Zero, JVector.UnitZ, Current, settings);
            Real expected = MathR.Min(settings.ArrowShaftRadius,
                MathR.Min((Real)0.05, settings.ArrowHeadSize * (Real)0.2));
            var rear = drawer.Triangles.SelectMany(t => new[] { t.A, t.B, t.C })
                .Where(p => p.Z == 0 && p != JVector.Zero).ToArray();
            Assert.That(rear, Is.Not.Empty);
            foreach (var vertex in rear) Assert.That(vertex.Length(), Is.EqualTo(expected).Within((Real)1e-6));
        }
        var zero = new RecordingDrawer();
        zero.DrawArrow(JVector.UnitX, JVector.Zero, Current, DebugDrawSettings.Default);
        Assert.That(zero.Segments, Is.EqualTo(new[] { (JVector.UnitX, JVector.UnitX, Current) }));
    }

    [Test]
    public void ShaftThicknessIsIndependentOfArrowLengthAndGuideScale()
    {
        foreach (Real length in new[] { (Real)0.6, (Real)1, (Real)3.2 })
        {
            var drawer = new TriangleDrawer();
            var settings = new DebugDrawSettings { Scale = length };
            drawer.DrawArrow(JVector.Zero, JVector.UnitZ * length, Current, settings);
            var rear = ArrowVertices(drawer, Current).Where(p => p.Z == 0 && p != JVector.Zero).ToArray();
            Assert.That(rear, Is.Not.Empty);
            foreach (JVector p in rear) Assert.That(p.Length(), Is.EqualTo(settings.ArrowShaftRadius).Within((Real)1e-6));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ArrowHeadDimensionsAreIndependentOfLengthScaleAndShaftThickness(bool solid)
    {
        foreach (var (length, scale, radius) in new[]
                 { ((Real)1, (Real)1, (Real)0.01), ((Real)4, (Real)3.2, (Real)0.02), ((Real)2, (Real)0.4, (Real)0.03) })
        {
            var drawer = new TriangleDrawer();
            var settings = new DebugDrawSettings
            {
                Scale = scale, ArrowShaftRadius = radius, ArrowHeadSize = (Real)0.3, SolidArrows = solid
            };
            drawer.DrawArrow(JVector.Zero, JVector.UnitZ * length, Current, settings);
            JVector[] vertices = ArrowVertices(drawer, Current);
            Real headRadius = settings.ArrowHeadSize * (Real)0.4;
            var rim = vertices.Where(p => MathR.Abs(MathR.Sqrt(p.X * p.X + p.Y * p.Y) - headRadius) < (Real)1e-5).ToArray();
            Assert.That(rim, Is.Not.Empty);
            Assert.That(vertices.Max(p => MathR.Sqrt(p.X * p.X + p.Y * p.Y)), Is.EqualTo(headRadius).Within((Real)1e-5));
            foreach (JVector p in rim) Assert.That(p.Z, Is.EqualTo(length - settings.ArrowHeadSize).Within((Real)1e-5));
            Assert.That(vertices.Any(p => (p - JVector.UnitZ * length).Length() < (Real)1e-5), Is.True);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ShortArrowsClampTheirHeadsAndKeepTheTip(bool solid)
    {
        var drawer = new TriangleDrawer();
        var settings = new DebugDrawSettings { ArrowHeadSize = 2, SolidArrows = solid, ArrowShaftRadius = 1 };
        Real length = (Real)0.1;
        drawer.DrawArrow(JVector.Zero, JVector.UnitZ * length, Current, settings);
        var vertices = ArrowVertices(drawer, Current);
        foreach (var p in vertices)
        {
            Assert.That(p.Z, Is.InRange(0, length));
            Assert.That(Real.IsFinite(p.X) && Real.IsFinite(p.Y) && Real.IsFinite(p.Z), Is.True);
        }
        Assert.That(vertices.Max(p => p.Z), Is.EqualTo(length));
        Assert.That(vertices.Max(p => MathR.Sqrt(p.X * p.X + p.Y * p.Y)), Is.EqualTo(length * (Real)0.2).Within((Real)1e-6));
        Assert.That(vertices.Any(p => MathR.Abs(p.Z - length * (Real)0.5) < (Real)1e-6), Is.True);
    }

    private static JVector[] ArrowVertices(TriangleDrawer drawer, uint color) =>
        drawer.Triangles.Where(t => t.Color == color).SelectMany(t => new[] { t.A, t.B, t.C })
            .Concat(drawer.Segments.Where(s => s.Color == color).SelectMany(s => new[] { s.From, s.To })).Distinct().ToArray();

    [Test]
    public void SolidFramesUseTheConfiguredAxisColors()
    {
        var drawer = new TriangleDrawer();
        var settings = new DebugDrawSettings
        {
            AxisXColor = 0xC864325A,
            AxisYColor = 0x2864C880,
            AxisZColor = 0x64C82800
        };
        drawer.DrawFrame(JVector.Zero, JQuaternion.CreateRotationY((Real)0.4), settings);
        Assert.That(drawer.Segments, Is.Empty);
        Assert.That(drawer.Triangles.Select(t => t.Color).Distinct(),
            Is.EqualTo(new[] { settings.AxisXColor, settings.AxisYColor, settings.AxisZColor }));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FrameSizeChangesOnlyAxisLength(bool solid)
    {
        var small = new TriangleDrawer();
        var same = new TriangleDrawer();
        var large = new TriangleDrawer();
        var settings = new DebugDrawSettings { Scale = 2, FrameSize = (Real)0.5, SolidArrows = solid };
        JQuaternion orientation = JQuaternion.Normalize(new JQuaternion(2, -1, 3, 4));
        small.DrawFrame(JVector.Zero, orientation, settings);
        same.DrawFrame(JVector.Zero, orientation, settings with { Scale = 10 });
        large.DrawFrame(JVector.Zero, orientation, settings with { FrameSize = 1 });

        Assert.That(same.Triangles, Is.EqualTo(small.Triangles));
        Assert.That(same.Segments, Is.EqualTo(small.Segments));
        Assert.That(large.Triangles.Count, Is.EqualTo(small.Triangles.Count));
        Assert.That(large.Segments.Count, Is.EqualTo(small.Segments.Count));
        var axes = new[] { orientation.GetBasisX(), orientation.GetBasisY(), orientation.GetBasisZ() };
        var colors = new[] { settings.AxisXColor, settings.AxisYColor, settings.AxisZColor };
        for (int i = 0; i < axes.Length; i++)
        {
            JVector axis = axes[i];
            var smallVertices = ArrowVertices(small, colors[i]);
            var largeVertices = ArrowVertices(large, colors[i]);
            Assert.That(smallVertices.Max(p => JVector.Dot(p, axis)), Is.EqualTo((Real)0.5).Within((Real)1e-5));
            Assert.That(largeVertices.Max(p => JVector.Dot(p, axis)), Is.EqualTo(1).Within((Real)1e-5));
            if (!solid) continue;
            Real smallRadius = smallVertices.Where(p => MathR.Abs(JVector.Dot(p, axis)) < (Real)1e-5).Max(p => p.Length());
            Real largeRadius = largeVertices.Where(p => MathR.Abs(JVector.Dot(p, axis)) < (Real)1e-5).Max(p => p.Length());
            Assert.That(smallRadius, Is.EqualTo(settings.ArrowShaftRadius).Within((Real)1e-5));
            Assert.That(largeRadius, Is.EqualTo(smallRadius).Within((Real)1e-5));
        }
    }

    [TestCase("AngularMotor")]
    [TestCase("LinearMotor")]
    [TestCase("FixedAngle")]
    [TestCase("ConeLimit")]
    [TestCase("EllipticalConeLimit")]
    [TestCase("HingeAngle")]
    [TestCase("TwistAngle")]
    [TestCase("PointOnLine")]
    [TestCase("PointOnPlane")]
    public void ConstraintArrowsUseTheSolidStyleAndThicknessSettings(string name)
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        second.Position = new JVector(2, 3, 4);
        Constraint constraint = Create(world, first, second, name, name == "PointOnLine" ? 2 : 0);
        var thin = new TriangleDrawer();
        var thick = new TriangleDrawer();
        var wire = new TriangleDrawer();
        var settings = new DebugDrawSettings { Scale = 2, ArrowShaftRadius = (Real)0.005 };
        constraint.DebugDraw(thin, settings);
        constraint.DebugDraw(thick, settings with { ArrowShaftRadius = (Real)0.01 });
        constraint.DebugDraw(wire, settings with { SolidArrows = false });
        Assert.That(thin.Triangles, Is.Not.Empty);
        Assert.That(thick.Triangles.Count, Is.EqualTo(thin.Triangles.Count));
        Assert.That(thick.Triangles.Select(t => (t.A, t.B, t.C)), Is.Not.EqualTo(thin.Triangles.Select(t => (t.A, t.B, t.C))));
        Assert.That(wire.Triangles.Count, Is.LessThan(thin.Triangles.Count));
        Assert.That(wire.Triangles, Is.SubsetOf(thin.Triangles));
        Assert.That(wire.Segments, Is.Not.Empty);
    }

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

    [Test]
    public void BallSocketDrawsAnchorMarkersAndConnectionsWithoutAnExtraWireSphere()
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Position = new JVector(-2, 1, 0);
        second.Position = new JVector(2, 3, 0);
        var socket = world.CreateConstraint<BallSocket>(first, second);
        socket.Initialize(new JVector(0, 2, 0));
        var drawer = new TriangleDrawer();
        socket.DebugDraw(drawer, Settings);
        Assert.That(drawer.Triangles.Count, Is.EqualTo(160));
        Assert.That(drawer.Segments, Is.EqualTo(new[]
        {
            (first.Position, socket.Anchor1, Reference),
            (second.Position, socket.Anchor2, Current),
            (socket.Anchor1, socket.Anchor2, Current)
        }));
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
        AssertVector(drawer.Segments[0].To, origin + (rotation * initialSecond).GetBasisX() * Settings.FrameSize);
    }

    [Test]
    public void FixedAngleFrameSizeAndAutomaticSpacingIgnoreGuideScale()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.Position = new JVector(3, 4, 5);
        body.Orientation = JQuaternion.Normalize(new JQuaternion(2, -1, 3, 4));
        var constraint = world.CreateConstraint<FixedAngle>(world.NullBody, body);
        constraint.Initialize();
        var first = new RecordingDrawer();
        var second = new RecordingDrawer();
        var settings = Settings with { FrameSize = (Real)0.75, Scale = (Real)0.1 };
        constraint.DebugDraw(first, settings);
        constraint.DebugDraw(second, settings with { Scale = 10 });

        Assert.That(second.Segments, Is.EqualTo(first.Segments));
        AssertVector(first.Segments[0].From,
            body.Position - body.Orientation.GetBasisX() * (settings.FrameSize * (Real)1.5));
        Assert.That((first.Segments[0].To - first.Segments[0].From).Length(),
            Is.EqualTo(settings.FrameSize).Within((Real)1e-5));
    }

    [Test]
    public void WeldJointForwardsTheFramePaletteAndKeepsAnchorColors()
    {
        using World world = new();
        RigidBody first = world.CreateRigidBody(), second = world.CreateRigidBody();
        first.Position = new JVector(-2, 1, 0);
        second.Position = new JVector(2, 1, 0);
        second.Orientation = JQuaternion.CreateRotationZ((Real)0.7);
        JQuaternion initialSecond = second.Orientation;
        JVector anchor = new(0, 1, 0);
        var joint = new WeldJoint(world, first, second, anchor);
        JQuaternion rotation = JQuaternion.CreateRotationX((Real)0.5);
        first.Orientation = rotation;
        var settings = Settings with
        {
            FrameSize = (Real)0.75,
            AxisXColor = 0xC864325A,
            AxisYColor = 0x2864C880,
            AxisZColor = 0x64C82800
        };
        var drawer = new RecordingDrawer();
        joint.DebugDraw(drawer, settings);

        var referenceX = drawer.Segments.First(s => s.Color == settings.AxisXColor && s.From == joint.BallSocket.Anchor1);
        AssertVector(referenceX.From, joint.BallSocket.Anchor1);
        AssertVector(referenceX.To - referenceX.From, (rotation * initialSecond).GetBasisX() * settings.FrameSize);
        var currentX = drawer.Segments.First(s => s.Color == settings.AxisXColor && s.From == second.Position);
        AssertVector(currentX.From, second.Position);
        AssertVector(currentX.To, second.Position + second.Orientation.GetBasisX() * settings.FrameSize);
        AssertPointMarker(drawer, Reference, joint.BallSocket.Anchor1, settings.Scale * (Real)0.06);
        AssertPointMarker(drawer, Current, joint.BallSocket.Anchor2, settings.Scale * (Real)0.06);
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

    [TestCase("BallSocket", false)]
    [TestCase("BallSocket", true)]
    [TestCase("DistanceLimit", false)]
    [TestCase("DistanceLimit", true)]
    [TestCase("PointOnLine", false)]
    [TestCase("PointOnLine", true)]
    [TestCase("PointOnPlane", false)]
    [TestCase("PointOnPlane", true)]
    [TestCase("SpringConstraint", false)]
    [TestCase("SpringConstraint", true)]
    public void WorldAnchorsDoNotDrawSpuriousLinesFromTheNullBody(string name, bool nullBodySecond)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.Position = new JVector(5, 6, 7);
        JVector anchor = new(4, 3, 2);
        RigidBody first = nullBodySecond ? body : world.NullBody, second = nullBodySecond ? world.NullBody : body;
        Constraint constraint;
        if (name == "DistanceLimit")
        {
            var distance = world.CreateConstraint<DistanceLimit>(first, second);
            distance.Initialize(anchor, body.Position);
            constraint = distance;
        }
        else if (name == "BallSocket")
        {
            var socket = world.CreateConstraint<BallSocket>(first, second);
            socket.Initialize(anchor);
            constraint = socket;
        }
        else if (name == "PointOnLine")
        {
            var line = world.CreateConstraint<PointOnLine>(first, second);
            line.Initialize(JVector.UnitX, anchor, body.Position);
            constraint = line;
        }
        else if (name == "PointOnPlane")
        {
            var plane = world.CreateConstraint<PointOnPlane>(first, second);
            plane.Initialize(JVector.UnitY, anchor, body.Position);
            constraint = plane;
        }
        else
        {
            var spring = world.CreateConstraint<SpringConstraint>(first, second);
            spring.Initialize(anchor, body.Position);
            constraint = spring;
        }
        var drawer = new RecordingDrawer();
        constraint.DebugDraw(drawer, Settings);
        Assert.That(drawer.Segments.Any(s => s.From == JVector.Zero || s.To == JVector.Zero), Is.False);
    }

    [TestCase("ConeLimit", false)]
    [TestCase("ConeLimit", true)]
    [TestCase("EllipticalConeLimit", false)]
    [TestCase("EllipticalConeLimit", true)]
    [TestCase("HingeAngle", false)]
    [TestCase("HingeAngle", true)]
    [TestCase("TwistAngle", false)]
    [TestCase("TwistAngle", true)]
    [TestCase("FixedAngle", false)]
    [TestCase("FixedAngle", true)]
    [TestCase("AngularMotor", false)]
    [TestCase("AngularMotor", true)]
    [TestCase("LinearMotor", false)]
    [TestCase("LinearMotor", true)]
    public void WorldRelativeOrientationAndMotorGuidesStayNearThePhysicalBody(string name, bool nullBodySecond)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.Position = new JVector(30, 40, 50);
        Constraint constraint = Create(world, nullBodySecond ? body : world.NullBody,
            nullBodySecond ? world.NullBody : body, name, 0);
        var drawer = new RecordingDrawer();
        constraint.DebugDraw(drawer, Settings);
        Assert.That(drawer.Segments, Is.Not.Empty);
        foreach (var segment in drawer.Segments)
        {
            Assert.That((segment.From - body.Position).Length(), Is.LessThanOrEqualTo(2 * Settings.Scale));
            Assert.That((segment.To - body.Position).Length(), Is.LessThanOrEqualTo(2 * Settings.Scale));
        }
    }

    [TestCase("FixedAngle")]
    [TestCase("AngularMotor")]
    public void ReversedWorldConnectionsKeepExplicitAngularGuideOrigins(string name)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.Position = new JVector(30, 40, 50);
        body.Orientation = JQuaternion.CreateRotationY((Real)0.7);
        Constraint constraint = Create(world, body, world.NullBody, name, 0);
        JVector origin = new(12, 23, 34);
        var settings = Settings with { Origin = origin };
        var drawer = new RecordingDrawer();
        constraint.DebugDraw(drawer, settings);
        if (name == "FixedAngle")
        {
            var xAxes = drawer.Segments.Where(s => s.Color == settings.AxisXColor &&
                (s.From == origin || s.From == body.Position)).ToArray();
            Assert.That(xAxes.Length, Is.EqualTo(2));
            AssertVector(xAxes[0].From, origin);
            AssertVector(xAxes[1].From, body.Position);
            AssertVector(xAxes[1].To, body.Position + JVector.UnitX * settings.FrameSize);
        }
        else
        {
            Assert.That(drawer.Segments.Any(s => s.Color == Current && s.From == origin), Is.True);
            foreach (var segment in drawer.Segments)
            {
                Assert.That((segment.From - origin).Length(), Is.LessThanOrEqualTo(2 * settings.Scale));
                Assert.That((segment.To - origin).Length(), Is.LessThanOrEqualTo(2 * settings.Scale));
            }
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
            MathR.Abs((s.To - center).Length() - radius) < (Real)1e-5), Is.EqualTo(240));
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

    private sealed class TriangleDrawer : IDebugDrawer
    {
        public readonly List<(JVector A, JVector B, JVector C, uint Color)> Triangles = [];
        public readonly List<(JVector From, JVector To, uint Color)> Segments = [];
        public void DrawSegment(in JVector a, in JVector b, uint color) =>
            Segments.Add((a, b, color));
        public void DrawTriangle(in JVector a, in JVector b, in JVector c, uint color) =>
            Triangles.Add((a, b, c, color));
    }

    private sealed class CountingDrawer : IDebugDrawer
    {
        public int Count;
        public void DrawSegment(in JVector a, in JVector b, uint color) => Count++;
        public void DrawTriangle(in JVector a, in JVector b, in JVector c, uint color) => Count++;
    }

    private sealed class LegacyConstraint : Constraint
    {
        public int Calls;
        public override void DebugDraw(IDebugDrawer drawer) => Calls++;
    }

    private class LegacyBallSocket : BallSocket
    {
        public int Calls;
        public bool CallBase;
        public override void DebugDraw(IDebugDrawer drawer)
        {
            Calls++;
            if (CallBase) base.DebugDraw(drawer);
        }
    }

    private sealed class InheritedLegacyBallSocket : LegacyBallSocket;

    private sealed class ConfiguredBallSocket : LegacyBallSocket
    {
        public int ConfiguredCalls;
        public DebugDrawSettings LastSettings;
        public override void DebugDraw(IDebugDrawer drawer, in DebugDrawSettings settings)
        {
            ConfiguredCalls++;
            LastSettings = settings;
            base.DebugDraw(drawer, settings);
        }
    }

    private sealed class UnmodifiedBallSocket : BallSocket;

    private sealed class DrawingJoint : Joint
    {
        public DrawingJoint(params Constraint[] constraints)
        {
            foreach (Constraint constraint in constraints) Register(constraint);
        }
    }
}
