using Jitter2.Collision;

namespace JitterTests.Behavior;

public class DynamicTreeFilterTests
{
    [Test]
    public void ChangingFilterUpdatesPairsForStationaryProxies()
    {
        var tree = new DynamicTree((_, _) => false);
        tree.AddProxy(new SphereShape(1));
        tree.AddProxy(new SphereShape(1));

        Assert.That(tree.HashSetInfo.Count, Is.Zero);

        tree.Filter = (_, _) => true;

        int overlapCount = 0;
        tree.EnumerateOverlaps((_, _) => overlapCount++);
        Assert.That(tree.HashSetInfo.Count, Is.EqualTo(1));
        Assert.That(overlapCount, Is.EqualTo(1));

        tree.Filter = (_, _) => false;

        Assert.That(tree.HashSetInfo.Count, Is.Zero);
    }

    [Test]
    public void ThrowingReplacementFilterPreservesExistingPairs()
    {
        var tree = new DynamicTree((_, _) => true);
        tree.AddProxy(new SphereShape(1));
        tree.AddProxy(new SphereShape(1));
        tree.AddProxy(new SphereShape(1));
        var originalFilter = tree.Filter;
        int filterCalls = 0;

        Assert.Throws<InvalidOperationException>(() =>
            tree.Filter = (_, _) => ++filterCalls == 1
                ? true
                : throw new InvalidOperationException());

        Assert.That(tree.Filter, Is.SameAs(originalFilter));
        Assert.That(tree.HashSetInfo.Count, Is.EqualTo(3));

        int overlapCount = 0;
        tree.EnumerateOverlaps((_, _) => overlapCount++);
        Assert.That(overlapCount, Is.EqualTo(3));
    }
}
