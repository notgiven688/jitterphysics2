using Jitter2.Dynamics.Constraints;

namespace JitterTests;

public class MiscTests
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase]
    public static void RequestId()
    {
        Assert.That(World.RequestId() == 1);
        Assert.That(World.RequestId(1) == (2,3));
        Assert.That(World.RequestId() == 3);
        Assert.That(World.RequestId(2) == (4,6));
        Assert.That(World.RequestId() == 6);
        Assert.That(World.RequestId(3) == (7,10));
        Assert.That(World.RequestId(3) == (10,13));
    }
}