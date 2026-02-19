using System.Threading;
using System.Threading.Tasks;

namespace JitterTests
{
    public class LockTests
    {
        [Test]
        public void TryLockThenUnlock_AllowsReacquisition()
        {
            var world = new World();
            var bodyA = world.CreateRigidBody();
            var bodyB = world.CreateRigidBody();

            // First TryLock should succeed
            bool first = World.TryLockTwoBody(ref bodyA.Data, ref bodyB.Data);
            Assert.That(first, Is.True, "First TryLock should succeed.");

            // Second should fail while locked
            bool second = World.TryLockTwoBody(ref bodyA.Data, ref bodyB.Data);
            Assert.That(second, Is.False, "Second TryLock should fail while locked.");

            // Unlock, then it should succeed again
            World.UnlockTwoBody(ref bodyA.Data, ref bodyB.Data);

            bool third = World.TryLockTwoBody(ref bodyA.Data, ref bodyB.Data);
            Assert.That(third, Is.True, "TryLock should succeed after unlock.");

            World.UnlockTwoBody(ref bodyA.Data, ref bodyB.Data);

            world.Dispose();
        }
    }
}
