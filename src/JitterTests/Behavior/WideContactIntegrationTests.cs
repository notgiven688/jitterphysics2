using Jitter2.Unmanaged;
#if USE_DOUBLE_PRECISION
using Vector = System.Runtime.Intrinsics.Vector256;
#else
using Vector = System.Runtime.Intrinsics.Vector128;
#endif

namespace JitterTests.Behavior;

public class WideContactIntegrationTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-11;
#else
    private const Real Tolerance = 2e-5f;
#endif

    [Test]
    public void FullBundle_UpdatesPenetrationWithoutRotatingImpulseOffsets()
    {
        using World world = new();
        var handles = new JHandle<ContactData>[ContactSolverWide.Width];
        var bodies = new RigidBody[handles.Length];
        for (int lane = 0; lane < handles.Length; lane++)
        {
            RigidBody body = bodies[lane] = world.CreateRigidBody();
            body.AddShape(new SphereShape((Real)0.5));
            body.Position = new JVector(3 * lane, (Real)0.45, 0);
            body.Velocity = -JVector.UnitY;
            body.Friction = body.Restitution = 0;
            world.GetOrCreateArbiter((ulong)(2 * lane + 1), (ulong)(2 * lane + 2),
                world.NullBody, body, out Arbiter arbiter);
            world.RegisterContact(arbiter, new JVector(3 * lane, 0, 0),
                new JVector(3 * lane, (Real)(-0.05), 0), JVector.UnitY);
            handles[lane] = arbiter.Handle;
            handles[lane].Data.Friction = 0;
            // Mimic rotation during a substep without clearing the cached contact.
            body.Data.Orientation = JQuaternion.CreateRotationZ((Real)0.3);
        }

        TimeStep timeStep = new((Real)0.01, 3);
        var solver = new ContactSolverWide();
        solver.Prepare(handles, timeStep);
        Real penetration = (Real)0.5 * MathR.Cos((Real)0.3) - (Real)0.45;
        Real expectedBias = ContactData.Contact.BiasFactor * timeStep.InverseStepDt
            * (penetration - ContactData.Contact.AllowedPenetration);
        solver.Iterate(true);
        solver.WriteBack();

        for (int lane = 0; lane < handles.Length; lane++)
        {
            Assert.That(handles[lane].Data.Contact0.PenaltyBias, Is.EqualTo(expectedBias).Within(Tolerance));
            Assert.That(JVector.MaxAbs(handles[lane].Data.Contact0.RelativePosition2 + (Real)0.5 * JVector.UnitY),
                Is.LessThan(Tolerance));
            Assert.That(bodies[lane].Velocity.Y, Is.EqualTo(expectedBias).Within(Tolerance));
            Assert.That(bodies[lane].AngularVelocity.Length(), Is.LessThan(Tolerance));
        }
    }

    [Test]
    public void MixedFullBundle_LinearOnlyLanesIgnoreAngularSurfaceVelocity()
    {
        using World world = new();
        var handles = new JHandle<ContactData>[ContactSolverWide.Width];
        var bodies = new RigidBody[handles.Length];
        for (int lane = 0; lane < handles.Length; lane++)
        {
            RigidBody body = bodies[lane] = world.CreateRigidBody();
            body.AddShape(new SphereShape(1));
            body.SetMassInertia(1);
            body.Position = new JVector(0, 0, 3 * lane);
            body.Velocity = new JVector(-2, 0, 0);
            body.AngularVelocity = new JVector(0, 0, 10);
            body.Restitution = 1;
            world.GetOrCreateArbiter((ulong)(2 * lane + 1), (ulong)(2 * lane + 2),
                world.NullBody, body, out Arbiter arbiter);
            world.RegisterContact(arbiter, new JVector((Real)(-0.005), 1, 3 * lane),
                new JVector(0, 1, 3 * lane), JVector.UnitX,
                lane % 2 == 0 ? ContactData.SolveMode.Angular : ContactData.SolveMode.None);
            handles[lane] = arbiter.Handle;
            handles[lane].Data.Friction = 0;
        }

        var solver = new ContactSolverWide();
        solver.Prepare(handles, new TimeStep((Real)0.1));
        solver.Iterate(true);
        solver.WriteBack();

        for (int lane = 0; lane < handles.Length; lane += 2)
        {
            Assert.That(bodies[lane].Velocity.X, Is.EqualTo((Real)2).Within(Tolerance));
            Assert.That(bodies[lane].AngularVelocity, Is.EqualTo(new JVector(0, 0, 10)));
        }
        for (int lane = 1; lane < handles.Length; lane += 2)
            Assert.That(bodies[lane].AngularVelocity.Z, Is.LessThan((Real)10 - Tolerance));
    }

    [TestCase(ContactData.Contact.Flags.SkipWarmStart)]
    [TestCase(ContactData.Contact.Flags.NewContact | ContactData.Contact.Flags.SkipWarmStart)]
    public void RefreshedNormal_PreservesFlagsStoredInImpulsePadding(ContactData.Contact.Flags flags)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.Velocity = JVector.UnitX;
        ContactData contact = default;
        contact.Init(world.NullBody, body);
        contact.ResetMode();
        contact.Friction = 1;
        contact.AddContact(JVector.Zero, JVector.Zero, JVector.UnitY);
        contact.Contact0.Accumulated = Vector.Create((Real)3, (Real)0.1, (Real)(-0.05), (Real)0);
        contact.Contact0.Flag = flags;
        contact.Contact0.Bias = (Real)0.75;
        JVector previousImpulse = contact.Contact0.Impulse * contact.Contact0.Normal
            + contact.Contact0.TangentImpulse1 * contact.Contact0.Tangent1
            + contact.Contact0.TangentImpulse2 * contact.Contact0.Tangent2;
        JVector normal = JVector.Normalize(new JVector((Real)0.05, 1, 0));

        contact.AddContact(JVector.Zero, JVector.Zero, normal);

        JVector refreshedImpulse = contact.Contact0.Impulse * contact.Contact0.Normal
            + contact.Contact0.TangentImpulse1 * contact.Contact0.Tangent1
            + contact.Contact0.TangentImpulse2 * contact.Contact0.Tangent2;
        Assert.That(contact.Contact0.Flag, Is.EqualTo(flags));
        Assert.That(contact.Contact0.Bias, Is.EqualTo((Real)0.75));
        Assert.That((contact.Contact0.Normal - normal).Length(), Is.LessThan(Tolerance));
        Assert.That((refreshedImpulse - previousImpulse).Length(), Is.LessThan(Tolerance));
    }
}
