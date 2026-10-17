using System.Runtime.InteropServices;

namespace JitterTests.Api;

public class RigidBodyDebugDrawTests
{
    [TestCase(1, 60)]
    [TestCase(2, 240)]
    [TestCase(3, 960)]
    [TestCase(4, 3840)]
    public void SettingsControlShapeDetailAndColorThroughTheInterface(int subdivisions, int expectedSegments)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new SphereShape(2));
        body.Position = new JVector(5, 6, 7);
        body.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        byte[] before = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref body.Data, 1)).ToArray();
        const uint color = 0x3182CE80;
        var settings = new DebugDrawSettings { ShapeSubdivisions = subdivisions, CurrentColor = color, Scale = 7 };
        var drawer = new RecordingDrawer();
        IDebugDrawable drawable = body;
        drawable.DebugDraw(drawer, settings);
        Assert.That(drawer.Segments.Count, Is.EqualTo(expectedSegments));
        foreach (var segment in drawer.Segments)
        {
            Assert.That(segment.Color, Is.EqualTo(color));
            Assert.That((segment.From - body.Position).Length(), Is.EqualTo(2).Within((Real)1e-5));
            Assert.That((segment.To - body.Position).Length(), Is.EqualTo(2).Within((Real)1e-5));
        }
        Assert.That(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref body.Data, 1)).ToArray(), Is.EqualTo(before));
    }

    [Test]
    public void OriginalOverloadUsesDefaultSettings()
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        var original = new RecordingDrawer();
        var configured = new RecordingDrawer();
        body.DebugDraw(original);
        body.DebugDraw(configured, DebugDrawSettings.Default);
        Assert.That(configured.Segments, Is.EqualTo(original.Segments));
        Assert.That(original.Segments.Count, Is.EqualTo(960));
        Assert.That(original.Segments.All(s => s.Color == DebugDrawColors.White), Is.True);
    }

    private sealed class RecordingDrawer : IDebugDrawer
    {
        public readonly List<(JVector From, JVector To, uint Color)> Segments = [];
        public void DrawSegment(in JVector from, in JVector to, uint color) => Segments.Add((from, to, color));
    }
}
