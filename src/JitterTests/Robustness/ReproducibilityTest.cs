using System.Runtime.Intrinsics;

#if USE_DOUBLE_PRECISION
using VectorReal = System.Runtime.Intrinsics.Vector256<double>;
#else
using VectorReal = System.Runtime.Intrinsics.Vector128<float>;
#endif

namespace JitterTests.Robustness;

public class ReproducibilityTest
{
    [TestCase]
    public static void BasicReproducibilityTest()
    {
        // This is intentionally a single-threaded reproducibility check on the regular solver.
        // Given the exact same input/world construction order, the engine should still produce
        // the same result. The main purpose is to catch hidden state such as uninitialized
        // memory, stale pooled data, or similar history-dependent bugs.
        var worldA = new World();
        worldA.SolverIterations = (2, 2);
        var last = Helper.BuildSimpleStack(worldA);
        Helper.AdvanceWorld(worldA, 10, (Real)(1.0 / 100.0), false);
        worldA.Remove(last);
        Helper.BuildSimpleStack(worldA);
        Helper.AdvanceWorld(worldA, 10, (Real)(1.0 / 100.0), false);
        worldA.Clear();
        last = Helper.BuildSimpleStack(worldA);
        last.Velocity = new JVector(10);
        Helper.AdvanceWorld(worldA, 10, (Real)(1.0 / 100.0), false);

        var worldB = new World();
        worldB.SolverIterations = (2, 2);
        last = Helper.BuildSimpleStack(worldB);
        Helper.AdvanceWorld(worldB, 10, (Real)(1.0 / 100.0), false);
        worldB.Remove(last);
        Helper.BuildSimpleStack(worldB);
        Helper.AdvanceWorld(worldB, 10, (Real)(1.0 / 100.0), false);
        worldB.Clear();
        last = Helper.BuildSimpleStack(worldB);
        last.Velocity = new JVector(10);
        Helper.AdvanceWorld(worldB, 10, (Real)(1.0 / 100.0), false);

        var positionsA = worldA.RigidBodies.Select(body => body.Position);
        var positionsB = worldB.RigidBodies.Select(body => body.Position);

        Assert.That(positionsA.SequenceEqual(positionsB));

        worldA.Dispose();
        worldB.Dispose();
    }

    [Test]
    public static void SingleContactMatchesWithAndWithoutContactSimd()
    {
        using var simdWorld = new World();
        using var scalarWorld = new World();

        var (simdBody1, simdBody2, simdContact) = CreateSingleContactCase(simdWorld);
        var (scalarBody1, scalarBody2, scalarContact) = CreateSingleContactCase(scalarWorld);

        const Real dt = (Real)(1.0 / 100.0);
        Real idt = (Real)1.0 / dt;

        simdContact.PrepareForIterationAccelerated(idt);
        scalarContact.PrepareForIterationScalar(idt);

        AssertSolverStateEqual(simdContact, scalarContact, simdBody1, scalarBody1, simdBody2, scalarBody2, "prepare");

        for (int i = 0; i < 8; i++)
        {
            simdContact.IterateAccelerated(true);
            scalarContact.IterateScalar(true);
        }

        for (int i = 0; i < 4; i++)
        {
            simdContact.IterateAccelerated(false);
            scalarContact.IterateScalar(false);
        }

        AssertSolverStateEqual(simdContact, scalarContact, simdBody1, scalarBody1, simdBody2, scalarBody2, "solve");
    }

    [Test]
    public static void DeterministicStackedColumnsWithSleeping_MatchesAcrossThreadingForTwentySeconds()
    {
        const Real dt = (Real)(1.0 / 100.0);
        const int totalSteps = 2000;

        using var singleThreadWorld = CreateDeterministicSleepingWorld();
        var singleThreadBodies = BuildStackedColumnsScene(singleThreadWorld);

        using var multiThreadWorld = CreateDeterministicSleepingWorld();
        var multiThreadBodies = BuildStackedColumnsScene(multiThreadWorld);

        bool sawSleepingBodies = false;

        for (int step = 1; step <= totalSteps; step++)
        {
            singleThreadWorld.Step(dt, false);
            multiThreadWorld.Step(dt, true);

            AssertIslandActivationInvariant(singleThreadWorld, step, "single-thread");
            AssertIslandActivationInvariant(multiThreadWorld, step, "multi-thread");
            AssertBodyStatesEqual(singleThreadBodies, multiThreadBodies, step);
            sawSleepingBodies |= singleThreadBodies.Any(body => body.Data.MotionType == MotionType.Dynamic && !body.IsActive);
        }

        Assert.That(sawSleepingBodies, Is.True,
            "At least one dynamic body should go to sleep during the repro.");
    }

    private static (RigidBody body1, RigidBody body2, ContactData contact) CreateSingleContactCase(World world)
    {
        RigidBody body1 = world.CreateRigidBody();
        body1.AddShape(new BoxShape((Real)1.1, (Real)0.9, (Real)1.3));
        body1.SetMassInertia((Real)2.0);
        body1.Position = new JVector((Real)(-0.35), (Real)0.7, (Real)0.1);
        body1.Orientation = JQuaternion.CreateFromAxisAngle(
            JVector.Normalize(new JVector((Real)1.0, (Real)2.0, (Real)(-1.0))), (Real)0.35);
        body1.Velocity = new JVector((Real)1.5, (Real)(-0.35), (Real)0.8);
        body1.AngularVelocity = new JVector((Real)0.2, (Real)(-1.1), (Real)0.45);
        body1.Friction = (Real)0.65;
        body1.Restitution = (Real)0.15;

        RigidBody body2 = world.CreateRigidBody();
        body2.AddShape(new BoxShape((Real)0.8, (Real)1.4, (Real)1.0));
        body2.SetMassInertia((Real)3.5);
        body2.Position = new JVector((Real)0.25, (Real)(-0.15), (Real)0.45);
        body2.Orientation = JQuaternion.CreateRotationZ((Real)(-0.25)) * JQuaternion.CreateRotationX((Real)0.4);
        body2.Velocity = new JVector((Real)(-0.9), (Real)0.55, (Real)(-1.2));
        body2.AngularVelocity = new JVector((Real)(-0.6), (Real)0.3, (Real)1.4);
        body2.Friction = (Real)0.8;
        body2.Restitution = (Real)0.05;

        JVector normal = JVector.Normalize(new JVector((Real)0.35, (Real)0.91, (Real)(-0.21)));
        JVector point1 = body1.Position + new JVector((Real)0.19, (Real)(-0.27), (Real)0.11);
        JVector point2 = point1 - (Real)0.025 * normal;

        ContactData contact = default;
        contact.Init(body1, body2);
        contact.ResetMode();
        contact.Contact0.Initialize(ref body1.Data, ref body2.Data, point1, point2, normal, true, contact.Restitution);
        contact.UsageMask = ContactData.MaskContact0;

        return (body1, body2, contact);
    }

    private static World CreateDeterministicSleepingWorld()
    {
        return new World
        {
            AllowDeactivation = true,
            SolveMode = SolveMode.Deterministic
        };
    }

    private static RigidBody[] BuildStackedColumnsScene(World world)
    {
        var bodies = new List<RigidBody>(capacity: 353);

        var floor = world.CreateRigidBody();
        floor.Position = new JVector(0, -100, 0);
        floor.MotionType = MotionType.Static;
        floor.AddShape(new BoxShape(200, 200, 200));
        bodies.Add(floor);

        for (int i = 0; i < 320; i++)
        {
            var body = world.CreateRigidBody();
            body.Position = new JVector(0, (Real)0.5 + i * (Real)0.999, 0);
            body.AddShape(new BoxShape(1));
            body.Damping = ((Real)0.002, (Real)0.002);
            bodies.Add(body);
        }

        for (int i = 0; i < 32; i++)
        {
            var body = world.CreateRigidBody();
            body.Position = new JVector(10, (Real)0.5 + i * (Real)0.999, 0);
            body.AddShape(new TransformedShape(new ConeShape(), JVector.Zero, JMatrix.CreateScale((Real)0.4, 1, 1)));
            body.Damping = ((Real)0.002, (Real)0.002);
            bodies.Add(body);
        }

        world.SolverIterations = (4, 2);
        world.SubstepCount = 3;

        return [.. bodies];
    }

    private static void AssertIslandActivationInvariant(World world, int step, string label)
    {
        foreach (var island in world.Islands)
        {
            if (island.NeedsUpdate) continue;

            bool shouldBeActive = world.Islands.IsActive(island);

            foreach (RigidBody body in island.InternalBodies)
            {
                if (body.Data.MotionType == MotionType.Static) continue;

                if (body.Data.IsActive != shouldBeActive)
                {
                    Assert.Fail(
                        $"Island activation invariant broken in {label} world at step {step}: " +
                        $"islandActive={shouldBeActive}, bodyActive={body.Data.IsActive}, " +
                        $"bodyState={FormatBodyState(body)}.");
                }
            }
        }
    }

    private static void AssertBodyStatesEqual(IReadOnlyList<RigidBody> singleThreadBodies,
        IReadOnlyList<RigidBody> multiThreadBodies, int step)
    {
        for (int i = 0; i < singleThreadBodies.Count; i++)
        {
            RigidBody single = singleThreadBodies[i];
            RigidBody multi = multiThreadBodies[i];

            if (single.Position != multi.Position ||
                single.Orientation != multi.Orientation ||
                single.Velocity != multi.Velocity ||
                single.AngularVelocity != multi.AngularVelocity ||
                single.IsActive != multi.IsActive)
            {
                Assert.Fail(
                    $"Deterministic mismatch at step {step}, body index {i}: " +
                    $"single={FormatBodyState(single)}, multi={FormatBodyState(multi)}.");
            }
        }
    }

    private static void AssertSolverStateEqual(ContactData simdContact, ContactData scalarContact,
        RigidBody simdBody1, RigidBody scalarBody1, RigidBody simdBody2, RigidBody scalarBody2, string phase)
    {
        ContactData.Contact simd = simdContact.Contact0;
        ContactData.Contact scalar = scalarContact.Contact0;

        if (simd.Bias != scalar.Bias ||
            simd.PenaltyBias != scalar.PenaltyBias ||
            GetElement(simd.MassNormalTangent, 0) != GetElement(scalar.MassNormalTangent, 0) ||
            GetElement(simd.MassNormalTangent, 1) != GetElement(scalar.MassNormalTangent, 1) ||
            GetElement(simd.MassNormalTangent, 2) != GetElement(scalar.MassNormalTangent, 2) ||
            simd.Impulse != scalar.Impulse ||
            simd.TangentImpulse1 != scalar.TangentImpulse1 ||
            simd.TangentImpulse2 != scalar.TangentImpulse2 ||
            simdBody1.Velocity != scalarBody1.Velocity ||
            simdBody1.AngularVelocity != scalarBody1.AngularVelocity ||
            simdBody2.Velocity != scalarBody2.Velocity ||
            simdBody2.AngularVelocity != scalarBody2.AngularVelocity)
        {
            Assert.Fail(
                $"{phase} mismatch: " +
                $"simd bias={Format(simd.Bias)}, scalar bias={Format(scalar.Bias)}, " +
                $"simd penalty={Format(simd.PenaltyBias)}, scalar penalty={Format(scalar.PenaltyBias)}, " +
                $"simd mass=[{Format(GetElement(simd.MassNormalTangent, 0))}, {Format(GetElement(simd.MassNormalTangent, 1))}, {Format(GetElement(simd.MassNormalTangent, 2))}], " +
                $"scalar mass=[{Format(GetElement(scalar.MassNormalTangent, 0))}, {Format(GetElement(scalar.MassNormalTangent, 1))}, {Format(GetElement(scalar.MassNormalTangent, 2))}], " +
                $"simd impulse=[{Format(simd.Impulse)}, {Format(simd.TangentImpulse1)}, {Format(simd.TangentImpulse2)}], " +
                $"scalar impulse=[{Format(scalar.Impulse)}, {Format(scalar.TangentImpulse1)}, {Format(scalar.TangentImpulse2)}], " +
                $"simd body1 vel={Format(simdBody1.Velocity)}, scalar body1 vel={Format(scalarBody1.Velocity)}, " +
                $"simd body1 angVel={Format(simdBody1.AngularVelocity)}, scalar body1 angVel={Format(scalarBody1.AngularVelocity)}, " +
                $"simd body2 vel={Format(simdBody2.Velocity)}, scalar body2 vel={Format(scalarBody2.Velocity)}, " +
                $"simd body2 angVel={Format(simdBody2.AngularVelocity)}, scalar body2 angVel={Format(scalarBody2.AngularVelocity)}.");
        }
    }

    private static Real GetElement(VectorReal vector, int index)
    {
#if USE_DOUBLE_PRECISION
        return Vector256.GetElement(vector, index);
#else
        return Vector128.GetElement(vector, index);
#endif
    }

    private static string Format(JVector vector)
    {
        return $"[{Format(vector.X)}, {Format(vector.Y)}, {Format(vector.Z)}]";
    }

    private static string Format(JQuaternion quaternion)
    {
        return $"[{Format(quaternion.X)}, {Format(quaternion.Y)}, {Format(quaternion.Z)}, {Format(quaternion.W)}]";
    }

    private static string FormatBodyState(RigidBody body)
    {
        return $"active={body.IsActive}, pos={Format(body.Position)}, orient={Format(body.Orientation)}, " +
               $"vel={Format(body.Velocity)}, angVel={Format(body.AngularVelocity)}, " +
               $"needsUpdate={body.Island.NeedsUpdate}, markedAsActive={body.Island.MarkedAsActive}";
    }

    private static string Format(Real value)
    {
        if (Precision.IsDoublePrecision)
        {
            long bits64 = BitConverter.DoubleToInt64Bits((double)value);
            return $"{value:R} (0x{bits64:X16})";
        }

        int bits32 = BitConverter.SingleToInt32Bits((float)value);
        return $"{value:R} (0x{bits32:X8})";
    }
}
