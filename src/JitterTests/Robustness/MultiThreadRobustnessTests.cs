namespace JitterTests.Robustness;

public class MultiThreadRobustnessTests
{
    [TestCase]
    public void MultiThreadedStepping_KeepsRepresentativeSceneFinite()
    {
        var world = new World
        {
            AllowDeactivation = false,
            Gravity = new JVector(0, -10, 0),
            SubstepCount = 4
        };

        var floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(20, 2, 20));
        floor.Position = new JVector(0, -1, 0);
        floor.MotionType = MotionType.Static;

        for (int i = 0; i < 6; i++)
        {
            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape(1));
            body.Position = new JVector(0, i + 0.5f, 0);
        }

        for (int i = 0; i < 50; i++)
        {
            Assert.DoesNotThrow(() => world.Step(1f / 100f, true));
        }

        foreach (var body in world.RigidBodies)
        {
            Assert.That(Real.IsFinite(body.Position.X));
            Assert.That(Real.IsFinite(body.Position.Y));
            Assert.That(Real.IsFinite(body.Position.Z));
            Assert.That(Real.IsFinite(body.Orientation.X));
            Assert.That(Real.IsFinite(body.Orientation.Y));
            Assert.That(Real.IsFinite(body.Orientation.Z));
            Assert.That(Real.IsFinite(body.Orientation.W));
        }

        world.Dispose();
    }

    [TestCase]
    public void SingleAndMultiThreadedStepping_PreserveBasicSceneInvariants()
    {
        static (Real topY, int contactCount) Run(bool multiThread)
        {
            var world = new World
            {
                AllowDeactivation = false,
                Gravity = new JVector(0, -10, 0),
                SubstepCount = 4
            };

            var floor = world.CreateRigidBody();
            floor.AddShape(new BoxShape(20, 2, 20));
            floor.Position = new JVector(0, -1, 0);
            floor.MotionType = MotionType.Static;

            RigidBody top = null!;

            for (int i = 0; i < 4; i++)
            {
                top = world.CreateRigidBody();
                top.AddShape(new BoxShape(1));
                top.Position = new JVector(0, i + 0.5f, 0);
            }

            for (int i = 0; i < 80; i++)
            {
                world.Step(1f / 100f, multiThread);
            }

            int totalContacts = 0;
            foreach (var body in world.RigidBodies)
            {
                totalContacts += body.Contacts.Count;
                Assert.That(Real.IsFinite(body.Position.Y));
            }

            var result = (top.Position.Y, totalContacts);
            world.Dispose();
            return result;
        }

        var single = Run(false);
        var multi = Run(true);

        Assert.That(single.topY, Is.GreaterThan((Real)0.0));
        Assert.That(multi.topY, Is.GreaterThan((Real)0.0));
        Assert.That(single.topY, Is.EqualTo(multi.topY).Within((Real)0.2));
        Assert.That(Math.Abs(single.contactCount - multi.contactCount), Is.LessThanOrEqualTo(4));
    }
}
