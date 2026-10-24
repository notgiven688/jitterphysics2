using System.Threading;
using System.Threading.Tasks;

namespace JitterTests.Robustness
{
    public class LockTests
    {
        [TestCase(MotionType.Static, false)]
        [TestCase(MotionType.Static, true)]
        [TestCase(MotionType.Kinematic, false)]
        [TestCase(MotionType.Kinematic, true)]
        public void FailedSecondLock_DoesNotWriteToNonDynamicBody(MotionType motionType, bool reverseArguments)
        {
            Span<RigidBodyData> bodies = stackalloc RigidBodyData[2];
            // Locks are acquired in descending address order, so index 1 is visited first.
            bodies[1].MotionType = motionType;
            bodies[1]._lockFlag = 7; // A sentinel makes even an otherwise harmless zero write observable.
            bodies[0].MotionType = MotionType.Dynamic;
            bodies[0]._lockFlag = 1;
            ref RigidBodyData first = ref bodies[reverseArguments ? 0 : 1];
            ref RigidBodyData second = ref bodies[reverseArguments ? 1 : 0];

            Assert.That(World.TryLockTwoBody(ref first, ref second), Is.False);

            Assert.That(bodies[1]._lockFlag, Is.EqualTo(7));
            Assert.That(bodies[0]._lockFlag, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedSecondLock_ReleasesAcquiredDynamicLock(bool reverseArguments)
        {
            Span<RigidBodyData> bodies = stackalloc RigidBodyData[2];
            bodies[1].MotionType = MotionType.Dynamic;
            bodies[1]._lockFlag = 0;
            bodies[0].MotionType = MotionType.Dynamic;
            bodies[0]._lockFlag = 1;
            ref RigidBodyData first = ref bodies[reverseArguments ? 0 : 1];
            ref RigidBodyData second = ref bodies[reverseArguments ? 1 : 0];

            Assert.That(World.TryLockTwoBody(ref first, ref second), Is.False);

            Assert.That(bodies[1]._lockFlag, Is.Zero);
            Assert.That(bodies[0]._lockFlag, Is.EqualTo(1));
        }

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
