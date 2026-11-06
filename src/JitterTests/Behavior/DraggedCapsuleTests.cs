using Jitter2.Dynamics.Constraints;

namespace JitterTests.Behavior;

/// <summary>
/// A lying capsule grabbed and dragged over a floor, as the demo's mouse does with a distance limit.
/// While it is dragged its axis turns away from the tie between its two ends, and the manifold's
/// probes of one direction can straddle that tie. With the half-tilt probe of the cubic extrapolation
/// on the other end, contacts appeared metres away from the body and carried impulse.
/// </summary>
public class DraggedCapsuleTests
{
    [TestCase(0.0, 0.0)]    // drag along the floor
    [TestCase(0.3, 0.0)]    // lift
    [TestCase(0.1, 0.5)]    // lift slightly and drag sideways
    public void ContactsStayOnTheBody(double lift, double side)
    {
        using World world = new() { AllowDeactivation = false, SolverIterations = (6, 4), NarrowPhaseFilter = new TriangleEdgeCollisionFilter() };

        RigidBody floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(200, 200, 200));
        floor.Position = new JVector(0, -100, 0);
        floor.MotionType = MotionType.Static;

        RigidBody capsule = world.CreateRigidBody();
        capsule.AddShape(new CapsuleShape((Real)0.25, 1));
        capsule.SetMassInertia(1);
        capsule.Friction = (Real)0.2;
        capsule.Restitution = 0;
        capsule.Damping = (0, 0);
        JQuaternion yaw = JQuaternion.CreateRotationY(MathR.PI / (Real)6.1);
        capsule.Orientation = yaw * JQuaternion.CreateRotationX(MathR.PI / 2);
        capsule.Position = new JVector(-8, (Real)0.25, 0);
        capsule.Velocity = JVector.Transform(new JVector(1, 0, 0), yaw);
        capsule.AngularVelocity = JVector.Transform(new JVector(0, 0, -4), yaw);

        for (int i = 0; i < 300; i++) world.Step((Real)0.01, true);

        // Grab at the top of the capsule, a bit off center along its axis.
        JVector hit = capsule.Position + new JVector(0, (Real)0.25, 0) + JVector.Transform(new JVector(0, 0, (Real)0.3), yaw);
        var grab = world.CreateConstraint<DistanceLimit>(capsule, world.NullBody);
        grab.Initialize(hit, hit);
        grab.Softness = (Real)0.01;
        grab.Bias = (Real)0.1;

        Real farthest = 0;
        for (int i = 0; i < 600; i++)
        {
            Real t = i * (Real)0.01;
            grab.Anchor2 = hit + new JVector((Real)side * t, (Real)lift * MathR.Min(1, t), (Real)0.3 * t);
            capsule.Velocity *= (Real)0.98;
            capsule.AngularVelocity *= (Real)0.98;

            world.Step((Real)0.01, true);

            foreach (Arbiter arbiter in capsule.Contacts)
            {
                ref ContactData cd = ref arbiter.Handle.Data;
                for (int slot = 0; slot < 4; slot++)
                {
                    if ((cd.UsageMask & (1u << slot)) == 0) continue;
                    ref ContactData.Contact c = ref (slot == 0 ? ref cd.Contact0 : ref slot == 1 ? ref cd.Contact1 : ref slot == 2 ? ref cd.Contact2 : ref cd.Contact3);
                    JVector w1 = c.RelativePosition1 + cd.Body1.Data.Position;
                    JVector w2 = c.RelativePosition2 + cd.Body2.Data.Position;
                    farthest = MathR.Max(farthest, (w1 - capsule.Position).Length());
                    farthest = MathR.Max(farthest, (w2 - capsule.Position).Length());
                }
            }
        }

        TestContext.Out.WriteLine($"lift {lift} side {side}: farthest contact anchor {farthest:F3} from the center");
        // Half the length plus the radius, with some room for a contact at a cap.
        Assert.That(farthest, Is.LessThan((Real)0.8));
    }
}
