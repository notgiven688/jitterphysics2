namespace JitterTests.Robustness;

public class DynamicTreeAddFailureTests
{
    [Test]
    public void AddProxy_ThrowingFilterLeavesTreeAndPairSetUnchanged()
    {
        var tree = new DynamicTree((_, _) => true);
        tree.AddProxy(new SphereShape((Real)1.0));
        tree.AddProxy(new SphereShape((Real)1.0));

        int originalPairCount = tree.HashSetInfo.Count;
        var third = new SphereShape((Real)1.0);
        int filterCalls = 0;
        tree.Filter = (_, _) => ++filterCalls == 1
            ? true
            : throw new InvalidOperationException("filter failed");

        Assert.Throws<InvalidOperationException>(() => tree.AddProxy(third));

        Assert.That(tree.Proxies, Has.Count.EqualTo(2));
        Assert.That(tree.HashSetInfo.Count, Is.EqualTo(originalPairCount));
        Assert.That(((IDynamicTreeProxy)third).NodePtr, Is.EqualTo(DynamicTree.NullNode));

        tree.Filter = (_, _) => true;
        tree.AddProxy(third);
        Assert.That(tree.Proxies, Has.Count.EqualTo(3));
        Assert.That(tree.HashSetInfo.Count, Is.EqualTo(3));
    }
}
