namespace JitterTests.Robustness;

public class DynamicTreeRegistrationTests
{
    [Test]
    public void UnregisteredProxyOperationsLeaveTreeUnchanged()
    {
        var tree = new DynamicTree((_, _) => true);
        var registered = new SphereShape((Real)1.0);
        var unregistered = new SphereShape((Real)1.0);
        tree.AddProxy(registered);

        int root = tree.Root;
        Assert.Throws<InvalidOperationException>(() => tree.Update(unregistered));
        Assert.Throws<InvalidOperationException>(() => tree.ActivateProxy(unregistered));
        Assert.Throws<InvalidOperationException>(() => tree.DeactivateProxy(unregistered));
        Assert.That(tree.IsActive(unregistered), Is.False);

        Assert.That(tree.Root, Is.EqualTo(root));
        Assert.That(tree.Proxies, Has.Count.EqualTo(1));
        Assert.That(tree.Proxies[0], Is.SameAs(registered));
        Assert.That(tree.IsActive(registered), Is.True);

        var otherTree = new DynamicTree((_, _) => true);
        var foreign = new SphereShape((Real)1.0);
        otherTree.AddProxy(foreign);
        Assert.Throws<InvalidOperationException>(() => tree.Update(foreign));
        Assert.That(tree.Proxies[0], Is.SameAs(registered));
    }
}
