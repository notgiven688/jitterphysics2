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
    private const double SolverRelativeTolerance = 2e-14;
#else
    private const double Tolerance = 2e-5;
    private const double SolverRelativeTolerance = 1e-6;
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

    private static void CopyContact(JHandle<ContactData> source, JHandle<ContactData> destination)
    {
        var body1 = destination.Data.Body1;
        var body2 = destination.Data.Body2;
        int index = destination.Data._internal;
        destination.Data = source.Data;
        destination.Data._internal = index;
        destination.Data.Body1 = body1;
        destination.Data.Body2 = body2;
        CopyBody(source.Data.Body1, body1);
        CopyBody(source.Data.Body2, body2);
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
        AssertSolverVector(actual.Data.Body1.Data.Velocity, expected.Data.Body1.Data.Velocity);
        AssertSolverVector(actual.Data.Body1.Data.AngularVelocity, expected.Data.Body1.Data.AngularVelocity);
        AssertSolverVector(actual.Data.Body2.Data.Velocity, expected.Data.Body2.Data.Velocity);
        AssertSolverVector(actual.Data.Body2.Data.AngularVelocity, expected.Data.Body2.Data.AngularVelocity);
        for (int slot = 0; slot < 4; slot++)
        {
            ref var actualContact = ref GetContact(ref actual.Data, slot);
            ref var expectedContact = ref GetContact(ref expected.Data, slot);
            AssertSolverValue(actualContact.Impulse, expectedContact.Impulse);
            AssertSolverValue(actualContact.TangentImpulse1, expectedContact.TangentImpulse1);
            AssertSolverValue(actualContact.TangentImpulse2, expectedContact.TangentImpulse2);
        }
    }

    private static void AssertSolverValue(Real actual, Real expected)
    {
        // Combined-impulse recomputation changes rounding order. Angular-only constraints
        // can produce large impulses, so bound relative error as well as error near zero.
        double tolerance = Tolerance + SolverRelativeTolerance * Math.Abs(expected);
        Assert.That(actual, Is.EqualTo(expected).Within(tolerance));
    }

    private static void AssertSolverVector(in JVector actual, in JVector expected)
    {
        AssertSolverValue(actual.X, expected.X);
        AssertSolverValue(actual.Y, expected.Y);
        AssertSolverValue(actual.Z, expected.Z);
    }

    private static void InitializeUnpreparedContact(ref ContactData data, int mask, int mode, int seed)
    {
        data.UsageMask = (uint)mask | 0xf0;
        data.Mode = (ContactData.SolveMode)mode;
        data.Friction = (Real)(0.1 + (seed % 5) * 0.2);
        data.SpeculativeRelaxationFactor = (Real)0.5;
        ref var body1 = ref data.Body1.Data;
        ref var body2 = ref data.Body2.Data;
        InitializePreparationBody(ref body1, seed);
        InitializePreparationBody(ref body2, seed + 1);
        body1.Velocity = new JVector((Real)0.1, 2, -(Real)0.2);
        body2.Velocity = new JVector(-(Real)0.3, -1, (Real)0.4);

        for (int slot = 0; slot < 4; slot++)
        {
            ref var contact = ref GetContact(ref data, slot);
            contact = default;
            if ((mask & (1 << slot)) == 0)
            {
                // A recycled manifold may contain unusable data outside its active slots.
                contact.Bias = contact.PenaltyBias = Real.NaN;
                contact.RelativePosition1 = contact.RelativePosition2 = new JVector(Real.NaN);
                contact.NormalTangentX = contact.NormalTangentY = contact.NormalTangentZ = Vector.Create(Real.NaN);
                contact.MassNormalTangent = contact.Accumulated = Vector.Create(Real.NaN);
                contact.Flag = ContactData.Contact.Flags.NewContact | ContactData.Contact.Flags.SkipWarmStart;
                continue;
            }

            JVector point = new((Real)(0.1 + slot * 0.2), -(Real)0.5, (Real)(0.3 - slot * 0.15));
            JVector normal = JVector.Normalize(new JVector((Real)(0.03 * seed), 1, (Real)(0.04 * slot)));
            Real depth = slot == 1 ? -(Real)0.08 : (Real)(0.01 + slot * 0.015);
            contact.Initialize(ref body1, ref body2, point, point - normal * depth, normal, true, (Real)0.6);
            contact.Accumulated = Vector.Create((Real)(0.1 + 0.1 * slot), (Real)0.12, -(Real)0.06, 0);
            contact.Flag = (slot % 2 == 0 ? ContactData.Contact.Flags.NewContact : 0) |
                           (slot is 1 or 2 ? ContactData.Contact.Flags.SkipWarmStart : 0);
        }

        // Cached local anchors must be transformed with the pose at preparation time.
        body1.Orientation = JQuaternion.CreateFromAxisAngle(JVector.UnitX, (Real)0.12);
        body2.Orientation = JQuaternion.CreateFromAxisAngle(JVector.UnitZ, -(Real)0.17);
        body2.Position += new JVector((Real)0.003, -(Real)0.002, (Real)0.001);
    }

    private static void InitializePreparationBody(ref RigidBodyData body, int seed)
    {
        body.Position = new JVector((Real)(seed * 0.005), (Real)0.01, -(Real)0.02);
        body.Orientation = JQuaternion.CreateFromAxisAngle(JVector.UnitY, (Real)(seed * 0.03));
        body.AngularVelocity = new JVector((Real)0.11, (Real)(seed * 0.013), -(Real)0.17);
        body.AllowedMotion = MotionAxes.All;
        body.InverseMassVector = new JVector((Real)0.7);
        body.InverseInertiaWorld = new JSymmetricMatrix((Real)0.8, (Real)0.07, -(Real)0.03,
            (Real)0.6, (Real)0.04, (Real)0.9);
        if (seed % 3 == 1)
        {
            body.AllowedMotion = MotionAxes.LinearX | MotionAxes.LinearZ | MotionAxes.AngularX | MotionAxes.AngularZ;
            body.InverseMassVector.Y = 0;
            // Schur complement restricting world Y rotation, with off-diagonal XZ response retained.
            body.InverseInertiaWorld = new JSymmetricMatrix((Real)(0.8 - 0.07 * 0.07 / 0.6), 0,
                (Real)(-0.03 - 0.07 * 0.04 / 0.6), 0, 0, (Real)(0.9 - 0.04 * 0.04 / 0.6));
        }
        else if (seed % 3 == 2)
        {
            body.InverseMassVector = JVector.Zero;
            body.InverseInertiaWorld = default;
        }
    }

    private static void AssertPreparedEquivalent(JHandle<ContactData> actual, JHandle<ContactData> expected)
    {
        Assert.That(actual.Data.UsageMask, Is.EqualTo(expected.Data.UsageMask));
        for (int slot = 0; slot < 4; slot++)
        {
            ref var actualContact = ref GetContact(ref actual.Data, slot);
            ref var expectedContact = ref GetContact(ref expected.Data, slot);
            if ((expected.Data.UsageMask & (1u << slot)) == 0)
            {
                Assert.That(actualContact.Equals(expectedContact), Is.True, $"Inactive slot {slot} changed");
                continue;
            }
            Assert.That(actualContact.Flag, Is.EqualTo(expectedContact.Flag));
            Assert.That(actualContact.Bias, Is.EqualTo(expectedContact.Bias).Within(Tolerance));
            Assert.That(actualContact.PenaltyBias, Is.EqualTo(expectedContact.PenaltyBias).Within(Tolerance));
            AssertVector(actualContact.RelativePosition1, expectedContact.RelativePosition1);
            AssertVector(actualContact.RelativePosition2, expectedContact.RelativePosition2);
            AssertVector(actualContact.Normal, expectedContact.Normal);
            AssertVector(actualContact.Tangent1, expectedContact.Tangent1);
            AssertVector(actualContact.Tangent2, expectedContact.Tangent2);
            for (int axis = 0; axis < 3; axis++)
            {
                Assert.That(actualContact.MassNormalTangent.GetElement(axis),
                    Is.EqualTo(expectedContact.MassNormalTangent.GetElement(axis)).Within(Tolerance));
            }
        }
        AssertEquivalent(actual, expected);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void NativePreparationAndWarmStart_MatchLegacyAcrossMasksModesAndBundleReuse(
        bool alternateTail, bool acceleratedOracle)
    {
        int width = ContactSolverWide.Width;
        using var bodies = new PartitionedBuffer<RigidBodyData>(4 * width);
        using var contacts = new PartitionedBuffer<ContactData>(2 * width);
        var scalar = new JHandle<ContactData>[width];
        var wide = new JHandle<ContactData>[width];
        var solver = new ContactSolverWide();
        var timeStep = new TimeStep((Real)0.04, 4);
        for (int lane = 0; lane < width; lane++)
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
            int count = alternateTail && mask % 2 == 0 ? Math.Max(1, width - 1) : width;
            for (int lane = 0; lane < count; lane++)
            {
                // Uniform modes also cover bundles that disable an entire angular response side.
                int laneMode = alternateTail ? mode : (mode + lane) % 16;
                InitializeUnpreparedContact(ref scalar[lane].Data, (mask + lane) % 16, laneMode, lane + 1);
                CopyContact(scalar[lane], wide[lane]);
                if (acceleratedOracle) scalar[lane].Data.PrepareForIterationAccelerated(timeStep);
                else scalar[lane].Data.PrepareForIterationScalar(timeStep);
            }
            solver.Prepare(wide.AsSpan(0, count), timeStep);
            Assert.That(solver.Count, Is.EqualTo(count));
            for (int lane = 0; lane < count; lane++) AssertPreparedEquivalent(wide[lane], scalar[lane]);

            // The relaxation pass keeps the prepared geometry and impulses, and drops only penalty bias.
            for (int iteration = 0; iteration < 3; iteration++)
            {
                for (int lane = 0; lane < count; lane++)
                {
                    if (acceleratedOracle) scalar[lane].Data.IterateAccelerated(iteration == 0);
                    else scalar[lane].Data.IterateScalar(iteration == 0);
                }
                solver.Iterate(iteration == 0);
                solver.WriteBack();
                for (int lane = 0; lane < count; lane++) AssertPreparedEquivalent(wide[lane], scalar[lane]);
            }
        }
    }

    [Test]
    public void Relaxation_UsesFrozenGeometryAndInertiaUntilExplicitRefresh()
    {
        using var bodies = new PartitionedBuffer<RigidBodyData>(4);
        using var contacts = new PartitionedBuffer<ContactData>(2);
        var scalar = contacts.Allocate(clear: true);
        var wide = contacts.Allocate(clear: true);
        scalar.Data.Body1 = CreateBody(bodies, 1);
        scalar.Data.Body2 = CreateBody(bodies, 2);
        wide.Data.Body1 = CreateBody(bodies, 1);
        wide.Data.Body2 = CreateBody(bodies, 2);
        InitializeUnpreparedContact(ref scalar.Data, 15, (int)ContactData.SolveMode.Full, 3);
        CopyContact(scalar, wide);
        var timeStep = new TimeStep((Real)0.02, 2);
        scalar.Data.PrepareForIterationScalar(timeStep);
        var solver = new ContactSolverWide();
        solver.Prepare(new[] { wide }, timeStep);
        scalar.Data.IterateScalar(true);
        solver.Iterate(true);

        // Mimic position integration before relaxation. The scalar oracle retains the prepared pose/inertia.
        wide.Data.Body1.Data.Orientation = JQuaternion.CreateFromAxisAngle(JVector.UnitZ, (Real)0.8);
        wide.Data.Body2.Data.Orientation = JQuaternion.CreateFromAxisAngle(JVector.UnitX, -(Real)0.7);
        wide.Data.Body1.Data.Position += JVector.One;
        wide.Data.Body2.Data.Position -= JVector.One;
        wide.Data.Body1.Data.InverseInertiaWorld = JSymmetricMatrix.CreateScale(new JVector(3));
        wide.Data.Body2.Data.InverseInertiaWorld = JSymmetricMatrix.CreateScale(new JVector(4));
        for (int iteration = 0; iteration < 3; iteration++)
        {
            scalar.Data.IterateScalar(false);
            solver.Iterate(false);
            solver.WriteBack();
            AssertPreparedEquivalent(wide, scalar);
        }
    }

    [TestCase(0)]
    [TestCase(0.01)]
    public void NativePreparation_HandlesZeroStepAndCappedPenalty(double dt)
    {
        using var bodies = new PartitionedBuffer<RigidBodyData>(4);
        using var contacts = new PartitionedBuffer<ContactData>(2);
        var scalar = contacts.Allocate(clear: true);
        var wide = contacts.Allocate(clear: true);
        scalar.Data.Body1 = CreateBody(bodies, 1);
        scalar.Data.Body2 = CreateBody(bodies, 2);
        wide.Data.Body1 = CreateBody(bodies, 1);
        wide.Data.Body2 = CreateBody(bodies, 2);
        InitializeUnpreparedContact(ref scalar.Data, 15, (int)ContactData.SolveMode.Full, 3);
        // No lane has linear locks: exercise the uniform linear-mass preparation path.
        scalar.Data.Body1.Data.AllowedMotion = scalar.Data.Body2.Data.AllowedMotion = MotionAxes.All;
        scalar.Data.Body1.Data.InverseMassVector = new JVector((Real)0.7);
        scalar.Data.Body2.Data.InverseMassVector = new JVector((Real)0.4);
        scalar.Data.Body1.Data.Position += JVector.UnitY * (Real)100;
        CopyContact(scalar, wide);
        var timeStep = new TimeStep((Real)dt, 4);
        scalar.Data.PrepareForIterationScalar(timeStep);
        var solver = new ContactSolverWide();
        solver.Prepare(new[] { wide }, timeStep);
        AssertPreparedEquivalent(wide, scalar);
        for (int slot = 0; slot < 4; slot++)
            Assert.That(GetContact(ref wide.Data, slot).PenaltyBias,
                Is.EqualTo(dt == 0 ? 0 : ContactData.Contact.MaximumBias));
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

            // A caller changing world inertia must refresh the frozen inverse inertia
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
        solver.Prepare(ReadOnlySpan<JHandle<ContactData>>.Empty, new TimeStep((Real)0.01));
        solver.Iterate(false);
        solver.WriteBack();
        Assert.That(solver.Count, Is.Zero);
    }
}
