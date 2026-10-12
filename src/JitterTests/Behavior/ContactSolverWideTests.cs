using System.Runtime.Intrinsics;
using Jitter2.Unmanaged;
#if USE_DOUBLE_PRECISION
using Vector = System.Runtime.Intrinsics.Vector256;
#else
using Vector = System.Runtime.Intrinsics.Vector128;
#endif

namespace JitterTests.Behavior;

public class ContactSolverWideTests
{
#if USE_DOUBLE_PRECISION
    private const double Tolerance = 2e-11;
#else
    private const double Tolerance = 2e-5;
#endif

    private static JHandle<RigidBodyData> CreateBody(PartitionedBuffer<RigidBodyData> bodies, int seed)
    {
        var handle = bodies.Allocate(clear: true);
        ref var body = ref handle.Data;
        body.Orientation = JQuaternion.Identity;
        body.Velocity = new JVector((Real)(seed * 0.03), (Real)(-0.7 + seed * 0.01), (Real)(0.2 - seed * 0.02));
        body.AngularVelocity = new JVector((Real)0.11, (Real)(seed * 0.013), -(Real)0.17);
        body.InverseMassVector = new JVector((Real)0.7, seed % 3 == 0 ? 0 : (Real)0.7, (Real)0.7);
        body.InverseInertiaWorld = new JSymmetricMatrix((Real)0.8, (Real)0.07, -(Real)0.03,
            (Real)0.6, (Real)0.04, (Real)0.9);
        return handle;
    }

    private static void CopyBody(JHandle<RigidBodyData> source, JHandle<RigidBodyData> destination)
    {
        int index = destination.Data._index;
        destination.Data = source.Data;
        destination.Data._index = index;
    }

    private static ref ContactData.Contact GetContact(ref ContactData data, int slot)
    {
        switch (slot)
        {
            case 0: return ref data.Contact0;
            case 1: return ref data.Contact1;
            case 2: return ref data.Contact2;
            default: return ref data.Contact3;
        }
    }

    private static void InitializeContact(ref ContactData data, int mask, int mode, int seed)
    {
        data.UsageMask = (uint)mask | 0xf0;
        data.Mode = (ContactData.SolveMode)mode;
        data.Friction = (Real)(0.1 + (seed % 5) * 0.2);
        data.SpeculativeRelaxationFactor = (Real)0.5;
        ref var body1 = ref data.Body1.Data;
        ref var body2 = ref data.Body2.Data;
        for (int slot = 0; slot < 4; slot++)
        {
            ref var contact = ref GetContact(ref data, slot);
            JVector point = new((Real)(0.1 + slot * 0.2), -(Real)0.5, (Real)(0.3 - slot * 0.15));
            JVector normal = JVector.Normalize(new JVector((Real)(0.03 * seed), 1, (Real)(0.04 * slot)));
            contact.Initialize(ref body1, ref body2, point, point - normal * (Real)0.02, normal, true, 0);
            contact.Accumulated = Vector.Create((Real)(0.1 + 0.1 * slot), (Real)0.12, -(Real)0.06, 0);
        }
        data.PrepareForIterationScalar(new TimeStep((Real)0.01));
        for (int slot = 0; slot < 4; slot++)
        {
            ref var contact = ref GetContact(ref data, slot);
            contact.Bias = (Real)(slot * 0.11 - 0.1);
            contact.PenaltyBias = slot % 2 == 0 ? 0 : (Real)0.3;
        }
    }

    private static void AssertVector(in JVector actual, in JVector expected)
    {
        Assert.That(actual.X, Is.EqualTo(expected.X).Within(Tolerance));
        Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(Tolerance));
        Assert.That(actual.Z, Is.EqualTo(expected.Z).Within(Tolerance));
    }

    private static void AssertEquivalent(JHandle<ContactData> actual, JHandle<ContactData> expected)
    {
        AssertVector(actual.Data.Body1.Data.Velocity, expected.Data.Body1.Data.Velocity);
        AssertVector(actual.Data.Body1.Data.AngularVelocity, expected.Data.Body1.Data.AngularVelocity);
        AssertVector(actual.Data.Body2.Data.Velocity, expected.Data.Body2.Data.Velocity);
        AssertVector(actual.Data.Body2.Data.AngularVelocity, expected.Data.Body2.Data.AngularVelocity);
        for (int slot = 0; slot < 4; slot++)
        {
            ref var actualContact = ref GetContact(ref actual.Data, slot);
            ref var expectedContact = ref GetContact(ref expected.Data, slot);
            Assert.That(actualContact.Impulse, Is.EqualTo(expectedContact.Impulse).Within(Tolerance));
            Assert.That(actualContact.TangentImpulse1, Is.EqualTo(expectedContact.TangentImpulse1).Within(Tolerance));
            Assert.That(actualContact.TangentImpulse2, Is.EqualTo(expectedContact.TangentImpulse2).Within(Tolerance));
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void PackedIterations_MatchScalarAcrossMasksModesAndTails(bool applyBias, bool tail)
    {
        int count = tail ? Math.Max(1, ContactSolverWide.Width - 1) : ContactSolverWide.Width;
        using var bodies = new PartitionedBuffer<RigidBodyData>(4 * count);
        using var contacts = new PartitionedBuffer<ContactData>(2 * count);
        var scalar = new JHandle<ContactData>[count];
        var wide = new JHandle<ContactData>[count];
        var wideSolver = new ContactSolverWide();
        for (int lane = 0; lane < count; lane++)
        {
            scalar[lane] = contacts.Allocate(clear: true);
            wide[lane] = contacts.Allocate(clear: true);
            scalar[lane].Data.Body1 = CreateBody(bodies, lane);
            scalar[lane].Data.Body2 = CreateBody(bodies, lane + 2);
            wide[lane].Data.Body1 = CreateBody(bodies, lane);
            wide[lane].Data.Body2 = CreateBody(bodies, lane + 2);
        }

        for (int mode = 0; mode < 16; mode++)
        for (int mask = 0; mask < 16; mask++)
        {
            for (int lane = 0; lane < count; lane++)
            {
                ref var source = ref scalar[lane].Data;
                InitializeContact(ref source, (mask + lane) % 16, (mode + lane) % 16, lane + 1);
                ref var target = ref wide[lane].Data;
                var body1 = target.Body1;
                var body2 = target.Body2;
                int index = target._internal;
                target = source;
                target._internal = index;
                target.Body1 = body1;
                target.Body2 = body2;
                CopyBody(source.Body1, target.Body1);
                CopyBody(source.Body2, target.Body2);
            }
            wideSolver.Prepare(wide);
            for (int iteration = 0; iteration < 3; iteration++)
            {
                foreach (var contact in scalar) contact.Data.IterateScalar(applyBias);
                wideSolver.Iterate(applyBias);
                wideSolver.WriteBack();
                for (int lane = 0; lane < count; lane++) AssertEquivalent(wide[lane], scalar[lane]);
            }

            // A caller changing world inertia must refresh the cached angular response
            // while prepared mass and accumulated impulses remain unchanged.
            for (int lane = 0; lane < count; lane++)
            {
                var newInertia = new JSymmetricMatrix((Real)0.3, -(Real)0.08, (Real)0.12,
                    (Real)0.95, -(Real)0.03, (Real)0.55);
                scalar[lane].Data.Body2.Data.InverseInertiaWorld = newInertia;
                wide[lane].Data.Body2.Data.InverseInertiaWorld = newInertia;
                // A constraint may also alter velocities between contact iterations.
                scalar[lane].Data.Body1.Data.Velocity += new JVector((Real)0.01, -(Real)0.03, (Real)0.02);
                wide[lane].Data.Body1.Data.Velocity = scalar[lane].Data.Body1.Data.Velocity;
            }
            wideSolver.RefreshInertia();
            foreach (var contact in scalar) contact.Data.IterateScalar(false);
            wideSolver.Iterate(false);
            wideSolver.WriteBack();
            for (int lane = 0; lane < count; lane++) AssertEquivalent(wide[lane], scalar[lane]);
        }
    }

    [Test]
    public void EmptyBundle_CanBePreparedIteratedAndWrittenBack()
    {
        var solver = new ContactSolverWide();
        solver.Prepare(ReadOnlySpan<JHandle<ContactData>>.Empty);
        solver.Iterate(true);
        solver.WriteBack();
        Assert.That(solver.Count, Is.Zero);
    }
}
