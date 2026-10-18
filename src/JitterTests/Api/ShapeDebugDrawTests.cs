namespace JitterTests.Api;

public class ShapeDebugDrawTests
{
    private const uint Color = 0x3182CE80;
    private static readonly DebugDrawSettings Settings = new() { Segments = 16, CurrentColor = Color };

    private static IEnumerable<object[]> PrimitiveCases()
    {
        foreach (string shape in new[] { "box", "sphere", "capsule", "zero-length capsule", "cylinder", "cone" })
        foreach (int segments in new[] { 8, 9, 24 })
            yield return [shape, segments];
    }

    private static RigidBodyShape Primitive(string name) => name switch
    {
        "box" => new BoxShape(2, 3, 4),
        "sphere" => new SphereShape(2),
        "capsule" => new CapsuleShape(1, 3),
        "zero-length capsule" => new CapsuleShape(1, 0),
        "cylinder" => new CylinderShape(3, 1),
        "cone" => new ConeShape(1, 3),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    [TestCaseSource(nameof(PrimitiveCases))]
    public void PrimitiveSurfacesAreClosedAndOutwardFacing(string name, int segments)
    {
        var shape = Primitive(name);
        var drawer = new TriangleDrawer();
        ((IDebugDrawable)shape).DebugDraw(drawer, Settings with { Segments = segments, Scale = 7 });
        Assert.That(drawer.Triangles, Is.Not.Empty);
        shape.CalculateBoundingBox(JQuaternion.Identity, JVector.Zero, out var bounds);
        var edges = new Dictionary<(JVector, JVector), int>();
        Real volume = 0;
        foreach (var (a, b, c, color) in drawer.Triangles)
        {
            JVector normal = (b - a) % (c - a);
            Assert.That(JVector.Dot(normal, a), Is.GreaterThan(0), "Inward or degenerate face");
            Assert.That(color, Is.EqualTo(Color));
            foreach (var p in new[] { a, b, c })
            {
                Assert.That(p.X, Is.InRange(bounds.Min.X - (Real)1e-5, bounds.Max.X + (Real)1e-5));
                Assert.That(p.Y, Is.InRange(bounds.Min.Y - (Real)1e-5, bounds.Max.Y + (Real)1e-5));
                Assert.That(p.Z, Is.InRange(bounds.Min.Z - (Real)1e-5, bounds.Max.Z + (Real)1e-5));
            }
            foreach (var edge in new[] { (a, b), (b, c), (c, a) }) edges[edge] = edges.GetValueOrDefault(edge) + 1;
            volume += JVector.Dot(a, b % c) / 6;
        }
        foreach (var (edge, count) in edges)
        {
            Assert.That(count, Is.EqualTo(1), "Duplicate directed edge");
            Assert.That(edges.GetValueOrDefault((edge.Item2, edge.Item1)), Is.EqualTo(1), "Open mesh edge");
        }
        shape.CalculateMassInertia(out _, out _, out Real expectedVolume);
        Assert.That(volume, Is.InRange(expectedVolume * (Real)0.5, expectedVolume * (Real)1.00001));
        if (shape is BoxShape) Assert.That(drawer.Triangles.Count, Is.EqualTo(12));
    }

    [Test]
    public void FallbackEmitsTheOriginalTessellatedTriangles()
    {
        var shape = new FallbackSphere();
        var drawer = new TriangleDrawer();
        var settings = Settings with { ShapeSubdivisions = 3 };
        shape.DebugDraw(drawer, settings);
        var expected = ShapeHelper.Tessellate(shape, settings.ShapeSubdivisions);
        Assert.That(drawer.Triangles.Count, Is.EqualTo(expected.Count));
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.That(drawer.Triangles[i], Is.EqualTo((expected[i].V0, expected[i].V1, expected[i].V2, Color)));
        }
    }

    [Test]
    public void TriangleShapePreservesTheMeshVertexOrder()
    {
        var a = new JVector(1, 2, 3);
        var b = new JVector(4, 2, 3);
        var c = new JVector(1, 2, 6);
        var mesh = new TriangleMesh([new JTriangle(a, b, c)]);
        var shape = new TriangleShape(mesh, 0);
        var drawer = new TriangleDrawer();
        shape.DebugDraw(drawer, Settings);
        Assert.That(drawer.Triangles, Is.EqualTo(new[] { (a, b, c, Color) }));
    }

    [Test]
    public void ConvexHullUsesStoredFacesAndCorrectsTheirWinding()
    {
        JVector a = new(0, 0, 0), b = new(2, 0, 0), c = new(0, 3, 0), d = new(0, 0, 4);
        JTriangle[] faces = [new(a, b, c), new(a, b, d), new(a, c, d), new(b, c, d)];
        var shape = new ConvexHullShape(faces) { Shift = new JVector(3, -4, 5) };
        shape.CalculateMassInertia(out _, out var center, out _);
        var drawer = new TriangleDrawer();
        shape.DebugDraw(drawer, Settings);
        Assert.That(drawer.Triangles.Count, Is.EqualTo(faces.Length));
        for (int i = 0; i < faces.Length; i++)
        {
            var (p, q, r, _) = drawer.Triangles[i];
            Assert.That(new[] { p, q, r }, Is.EquivalentTo(new[]
                { faces[i].V0 + shape.Shift, faces[i].V1 + shape.Shift, faces[i].V2 + shape.Shift }));
            Assert.That(JVector.Dot((q - p) % (r - p), p - center), Is.GreaterThan(0));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NestedAffineAndBodyTransformsPreserveGeometryAndWinding(bool twoReflections)
    {
        using World world = new();
        var source = new BoxShape(2, 3, 4);
        var local = new TriangleDrawer();
        source.DebugDraw(local, Settings);
        var unrelated = world.CreateRigidBody();
        unrelated.Position = new JVector(100, 200, 300);
        unrelated.AddShape(source);
        var first = new JMatrix(-2, (Real)0.4, 0, 0, 3, 0, 0, 0, (Real)0.5);
        var second = new JMatrix(twoReflections ? -1 : 1, 0, 0, 0, (Real)0.8, 0, (Real)0.2, 0, 2);
        var shift1 = new JVector(1, 2, -3);
        var shift2 = new JVector(-2, 1, 4);
        var shape = new TransformedShape(new TransformedShape(source, shift1, first), shift2, second);
        var body = world.CreateRigidBody();
        body.AddShape(shape, MassInertiaUpdateMode.Preserve);
        body.Position = new JVector(5, -6, 7);
        body.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        var actual = new TriangleDrawer();
        var all = new TriangleDrawer();
        shape.DebugDraw(actual, Settings);
        body.DebugDraw(all, Settings);
        Assert.That(all.Triangles, Is.EqualTo(actual.Triangles));
        Assert.That(actual.Triangles.Count, Is.EqualTo(local.Triangles.Count));
        JVector Expected(JVector p) => JVector.Transform(
            JVector.Transform(JVector.Transform(p, first) + shift1, second) + shift2, body.Orientation) + body.Position;
        for (int i = 0; i < local.Triangles.Count; i++)
        {
            var (a, b, c, _) = local.Triangles[i];
            if (!twoReflections) (b, c) = (c, b);
            var (p, q, r, color) = actual.Triangles[i];
            Assert.That((p - Expected(a)).Length(), Is.LessThan((Real)1e-5));
            Assert.That((q - Expected(b)).Length(), Is.LessThan((Real)1e-5));
            Assert.That((r - Expected(c)).Length(), Is.LessThan((Real)1e-5));
            Assert.That(color, Is.EqualTo(Color));
            Assert.That(JVector.Dot((q - p) % (r - p), p - Expected(JVector.Zero)), Is.GreaterThan(0));
        }
    }

    [Test]
    public void BodyAndTransformedShapeUseCustomLocalGeometry()
    {
        using World world = new();
        var custom = new CustomSurface();
        var shape = new TransformedShape(custom, new JVector(2, 0, 0));
        var body = world.CreateRigidBody();
        body.Position = new JVector(3, 4, 5);
        body.AddShape(shape, MassInertiaUpdateMode.Preserve);
        body.AddShape(new BoxShape(1));
        var drawer = new TriangleDrawer();
        body.DebugDraw(drawer, Settings);
        Assert.That(custom.Calls, Is.EqualTo(1));
        Assert.That(drawer.Triangles.Count, Is.EqualTo(13));
        JVector offset = body.Position + new JVector(2, 0, 0);
        Assert.That(drawer.Triangles[0], Is.EqualTo((offset, offset + JVector.UnitX, offset + JVector.UnitZ, Color)));
    }

    [Test]
    public void DrawingBuiltInAndNestedShapesDoesNotAllocate()
    {
        Shape[] shapes = [new BoxShape(1), new SphereShape(1), new CapsuleShape(), new CylinderShape(2, 1),
            new ConeShape(), new TransformedShape(new TransformedShape(new BoxShape(1), JVector.One), JVector.One)];
        var drawer = new CountingDrawer();
        for (int i = 0; i < 10; i++) foreach (var shape in shapes) shape.DebugDraw(drawer, Settings);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10; i++) foreach (var shape in shapes) shape.DebugDraw(drawer, Settings);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.Zero);
        Assert.That(drawer.Count, Is.GreaterThan(0));
    }

    private sealed class TriangleDrawer : IDebugDrawer
    {
        public readonly List<(JVector A, JVector B, JVector C, uint Color)> Triangles = [];
        public void DrawSegment(in JVector a, in JVector b, uint color) => Assert.Fail("Expected triangle emission");
        public void DrawTriangle(in JVector a, in JVector b, in JVector c, uint color) => Triangles.Add((a, b, c, color));
    }

    private sealed class CountingDrawer : IDebugDrawer
    {
        public int Count;
        public void DrawSegment(in JVector a, in JVector b, uint color) => Count++;
        public void DrawTriangle(in JVector a, in JVector b, in JVector c, uint color) => Count++;
    }

    private sealed class FallbackSphere : RigidBodyShape
    {
        public override void SupportMap(in JVector direction, out JVector result) => result = JVector.Normalize(direction);
        public override void GetCenter(out JVector point) => point = JVector.Zero;
    }

    private sealed class CustomSurface : SphereShape
    {
        public int Calls;
        protected internal override void DebugDrawLocal<TSink>(ref TSink triangleSink, in DebugDrawSettings settings)
        {
            Calls++;
            triangleSink.Add(new JTriangle(JVector.Zero, JVector.UnitX, JVector.UnitZ));
        }
    }
}
