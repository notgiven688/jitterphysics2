using Jitter2.Dynamics.Constraints;

namespace JitterTests.Robustness;

public class DisposedWorldTests
{
    [TestCase]
    public void Step_AfterDispose_ThrowsObjectDisposedException()
    {
        var world = new World();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.Step(1f / 60f, false));
    }

    [TestCase]
    public void CreateRigidBody_AfterDispose_ThrowsObjectDisposedException()
    {
        var world = new World();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.CreateRigidBody());
    }

    [TestCase]
    public void CreateConstraint_AfterDispose_ThrowsObjectDisposedException()
    {
        var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.CreateConstraint<BallSocket>(bodyA, bodyB));
    }

    [TestCase]
    public void Dispose_CanBeCalledTwice()
    {
        var world = new World();

        Assert.DoesNotThrow(() =>
        {
            world.Dispose();
            world.Dispose();
        });
    }
}
