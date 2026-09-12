using Jitter2.Dynamics.Constraints;

namespace JitterTests.Robustness;

public class DisposedWorldTests
{
    [TestCase]
    public void IsDisposed_ReflectsDisposalState()
    {
        var world = new World();

        Assert.That(world.IsDisposed, Is.False);

        world.Dispose();

        Assert.That(world.IsDisposed, Is.True);

        world.Dispose();

        Assert.That(world.IsDisposed, Is.True);
    }

    [TestCase]
    public void Dispose_InvalidatesBodiesAndConstraints()
    {
        var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = world.CreateConstraint<BallSocket>(bodyA, bodyB);

        Assert.That(bodyA.IsValid, Is.True);
        Assert.That(bodyB.IsValid, Is.True);
        Assert.That(constraint.IsValid, Is.True);

        world.Dispose();

        Assert.That(bodyA.IsValid, Is.False);
        Assert.That(bodyB.IsValid, Is.False);
        Assert.That(constraint.IsValid, Is.False);
    }

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
    public void Remove_AfterDispose_ThrowsObjectDisposedException()
    {
        var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = world.CreateConstraint<BallSocket>(bodyA, bodyB);
        world.GetOrCreateArbiter(1, 2, bodyA, bodyB, out var arbiter);
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.Remove(bodyA));
        Assert.Throws<ObjectDisposedException>(() => world.Remove(constraint));
        Assert.Throws<ObjectDisposedException>(() => world.Remove(arbiter));
    }

    [TestCase]
    public void RawData_AfterDispose_ThrowsObjectDisposedException()
    {
        var world = new World();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = world.RawData);
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
