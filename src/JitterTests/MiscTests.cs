using Jitter2.DataStructures;
using Jitter2.Dynamics.Constraints;

namespace JitterTests;

public class MiscTests
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase]
    public static void SlimBagTest()
    {
        var bag= new SlimBag<object>();

        bag.Add(new object());
        bag.Add(new object());
        bag.Add(new object());

        bag.Clear();
        Assert.That(bag[0], Is.Not.EqualTo(null));

        bag.NullOut();
        Assert.That(bag[0], Is.EqualTo(null));

        bag.Add(new object());
        bag.Add(new object());

        bag.RemoveAt(1);
        Assert.That(bag[0], Is.Not.EqualTo(null));
        Assert.That(bag[1], Is.Not.EqualTo(null));

        bag.NullOut();
        Assert.That(bag[0], Is.Not.EqualTo(null));
        Assert.That(bag[1], Is.EqualTo(null));
    }

    [TestCase]
    public static void RequestId()
    {
        ulong id0 = World.RequestId();
        Assert.That(World.RequestId() == id0 + 1);
        Assert.That(World.RequestId(1) == (id0 + 2, id0 + 3));
        Assert.That(World.RequestId() == id0 + 3);
        Assert.That(World.RequestId(2) == (id0 + 4, id0 + 6));
        Assert.That(World.RequestId() == id0 + 6);
        Assert.That(World.RequestId(3) == (id0 + 7, id0 + 10));
        Assert.That(World.RequestId(3) == (id0 + 10, id0 + 13));
    }
}