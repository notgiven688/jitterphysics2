using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Unmanaged;

namespace JitterTests.Behavior;

public class ConstraintMotionTypeTests
{
    private const Real Dt = (Real)0.01;

    private static SmallProbeConstraint CreateProbe(World world, RigidBody bodyA, RigidBody bodyB, bool small)
    {
        return small
            ? world.CreateConstraint<SmallProbeConstraint>(bodyA, bodyB)
            : world.CreateConstraint<FullProbeConstraint>(bodyA, bodyB);
    }

    private static void AssertBufferCounts(World world, bool small, int active, int inactive)
    {
        Assert.That(world.RawData.ActiveSmallConstraints.Length, Is.EqualTo(small ? active : 0));
        Assert.That(world.RawData.InactiveSmallConstraints.Length, Is.EqualTo(small ? inactive : 0));
        Assert.That(world.RawData.ActiveConstraints.Length, Is.EqualTo(small ? 0 : active));
        Assert.That(world.RawData.InactiveConstraints.Length, Is.EqualTo(small ? 0 : inactive));
    }

    [Test]
    public void MotionTypeChanges_PreserveConstraints_AndOnlySolveWithADynamicBody(
        [Values] SolveMode mode, [Values] bool multiThread, [Values] bool small)
    {
        using var world = new World { SolveMode = mode, Gravity = JVector.Zero };
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = CreateProbe(world, bodyA, bodyB, small);

        foreach (MotionType typeA in Enum.GetValues<MotionType>())
        foreach (MotionType typeB in Enum.GetValues<MotionType>())
        {
            bodyA.MotionType = typeA;
            bodyB.MotionType = typeB;
            int previousPrepareCount = constraint.Data.PrepareCount;
            int previousIterateCount = constraint.Data.IterateCount;

            // Several steps let static bodies leave the active body partition.
            for (int i = 0; i < 10; i++) world.Step(Dt, multiThread);

            Assert.That(constraint.IsValid, Is.True);
            Assert.That(constraint.IsEnabled, Is.True);
            Assert.That(bodyA.Constraints, Does.Contain(constraint));
            Assert.That(bodyB.Constraints, Does.Contain(constraint));

            bool shouldSolve = typeA == MotionType.Dynamic || typeB == MotionType.Dynamic;
            AssertBufferCounts(world, small, shouldSolve ? 1 : 0, shouldSolve ? 0 : 1);
            Assert.That(constraint.Data.PrepareCount - previousPrepareCount,
                shouldSolve ? Is.GreaterThan(0) : Is.Zero);
            Assert.That(constraint.Data.IterateCount - previousIterateCount,
                shouldSolve ? Is.GreaterThan(0) : Is.Zero);
        }
    }

    [Test]
    public void CreationBetweenNonDynamicBodies_LeavesConstraintInactiveUntilDynamic(
        [Values] SolveMode mode, [Values] bool multiThread, [Values] bool small,
        [Values(MotionType.Static, MotionType.Kinematic)] MotionType typeA,
        [Values(MotionType.Static, MotionType.Kinematic)] MotionType typeB)
    {
        using var world = new World { SolveMode = mode, Gravity = JVector.Zero };
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        bodyA.MotionType = typeA;
        bodyB.MotionType = typeB;
        var constraint = CreateProbe(world, bodyA, bodyB, small);

        AssertBufferCounts(world, small, 0, 1);
        world.Stabilize(Dt, 2, multiThread: multiThread);
        for (int i = 0; i < 10; i++) world.Step(Dt, multiThread);
        Assert.That(constraint.Data.PrepareCount, Is.Zero);
        Assert.That(constraint.Data.IterateCount, Is.Zero);

        bodyA.MotionType = MotionType.Dynamic;
        world.Step(Dt, multiThread);

        AssertBufferCounts(world, small, 1, 0);
        Assert.That(constraint.Data.PrepareCount, Is.GreaterThan(0));
        Assert.That(constraint.Data.IterateCount, Is.GreaterThan(0));
    }

    [Test]
    public void Stabilize_SuspendsAndRestoresConstraintsWithoutStepping(
        [Values] SolveMode mode, [Values] bool multiThread, [Values] bool small)
    {
        using var world = new World { SolveMode = mode };
        var body = world.CreateRigidBody();
        var constraint = CreateProbe(world, world.NullBody, body, small);
        world.Stabilize(Dt, 2, multiThread: multiThread);
        int previousPrepareCount = constraint.Data.PrepareCount;
        Assert.That(previousPrepareCount, Is.GreaterThan(0));

        body.MotionType = MotionType.Kinematic;
        world.Stabilize(Dt, 2, multiThread: multiThread);
        AssertBufferCounts(world, small, 0, 1);
        Assert.That(constraint.Data.PrepareCount, Is.EqualTo(previousPrepareCount));

        body.MotionType = MotionType.Dynamic;
        world.Stabilize(Dt, 2, multiThread: multiThread);
        AssertBufferCounts(world, small, 1, 0);
        Assert.That(constraint.Data.PrepareCount, Is.GreaterThan(previousPrepareCount));
    }

    [Test]
    public void WakingKinematicIsland_DoesNotReactivateSuspendedConstraint(
        [Values] SolveMode mode, [Values] bool multiThread, [Values] bool small)
    {
        using var world = new World { SolveMode = mode, Gravity = JVector.Zero };
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = CreateProbe(world, bodyA, bodyB, small);
        bodyA.MotionType = MotionType.Kinematic;
        bodyB.MotionType = MotionType.Kinematic;
        world.ForceSleepIsland(bodyA.Island);

        bodyA.SetActivationState(true);
        world.Step(Dt, multiThread);

        Assert.That(bodyA.IsActive, Is.True);
        Assert.That(bodyB.IsActive, Is.True);
        AssertBufferCounts(world, small, 0, 1);
        Assert.That(constraint.Data.PrepareCount, Is.Zero);
        Assert.That(constraint.Data.IterateCount, Is.Zero);

        // Already-awake bodies must restore the constraint immediately.
        bodyB.MotionType = MotionType.Dynamic;
        AssertBufferCounts(world, small, 1, 0);
        world.Step(Dt, multiThread);
        Assert.That(constraint.Data.IterateCount, Is.GreaterThan(0));
    }

    [Test]
    public void SleepingBody_ChangedAwayFromDynamicAndBack_RestoresConstraintOnWake(
        [Values] SolveMode mode, [Values] bool multiThread, [Values] bool small,
        [Values(MotionType.Static, MotionType.Kinematic)] MotionType temporary)
    {
        using var world = new World { SolveMode = mode, Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        var constraint = CreateProbe(world, world.NullBody, body, small);
        world.ForceSleepIsland(body.Island);

        body.MotionType = temporary;
        world.Step(Dt, multiThread);
        AssertBufferCounts(world, small, 0, 1);
        Assert.That(constraint.Data.PrepareCount, Is.Zero);

        world.ForceSleepIsland(body.Island);
        body.MotionType = MotionType.Dynamic;
        AssertBufferCounts(world, small, 0, 1);
        world.Step(Dt, multiThread);

        Assert.That(body.IsActive, Is.True);
        AssertBufferCounts(world, small, 1, 0);
        Assert.That(constraint.Data.PrepareCount, Is.GreaterThan(0));
        Assert.That(constraint.Data.IterateCount, Is.GreaterThan(0));
    }

    [Test]
    public void MotionTypeChange_WakesSleepingDynamicPartner_AndRestoresConstraint(
        [Values] SolveMode mode, [Values] bool multiThread, [Values] bool small)
    {
        using var world = new World { SolveMode = mode, Gravity = JVector.Zero };
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = CreateProbe(world, bodyA, bodyB, small);
        world.ForceSleepIsland(bodyA.Island);

        bodyA.MotionType = MotionType.Kinematic;
        world.Step(Dt, multiThread);

        Assert.That(bodyA.IsActive, Is.True);
        Assert.That(bodyB.IsActive, Is.True);
        AssertBufferCounts(world, small, 1, 0);
        Assert.That(constraint.Data.IterateCount, Is.GreaterThan(0));
    }

    [Test]
    public void IsEnabled_SurvivesSuspensionAndWake_AndCanChangeWhileSuspended(
        [Values] SolveMode mode, [Values] bool multiThread, [Values] bool small)
    {
        using var world = new World { SolveMode = mode, Gravity = JVector.Zero };
        var body = world.CreateRigidBody();
        var constraint = CreateProbe(world, world.NullBody, body, small);
        constraint.IsEnabled = false;

        body.MotionType = MotionType.Kinematic;
        world.Step(Dt, multiThread);
        world.ForceSleepIsland(body.Island);
        body.SetActivationState(true);
        world.Step(Dt, multiThread);
        Assert.That(constraint.IsEnabled, Is.False);
        AssertBufferCounts(world, small, 0, 1);

        body.MotionType = MotionType.Dynamic;
        world.Step(Dt, multiThread);
        Assert.That(constraint.IsEnabled, Is.False);
        Assert.That(constraint.Data.PrepareCount, Is.Zero);
        Assert.That(constraint.Data.IterateCount, Is.Zero);

        body.MotionType = MotionType.Kinematic;
        constraint.IsEnabled = true;
        world.Step(Dt, multiThread);
        Assert.That(constraint.IsEnabled, Is.True);
        AssertBufferCounts(world, small, 0, 1);
        Assert.That(constraint.Data.PrepareCount, Is.Zero);

        body.MotionType = MotionType.Dynamic;
        world.Step(Dt, multiThread);
        Assert.That(constraint.Data.IterateCount, Is.GreaterThan(0));
    }

    [Test]
    public void SuspendedConstraint_CanBeRemovedExplicitlyOrThroughBodyRemoval(
        [Values] bool small, [Values] bool removeBody)
    {
        using var world = new World();
        var bodyA = world.CreateRigidBody();
        var bodyB = world.CreateRigidBody();
        var constraint = CreateProbe(world, bodyA, bodyB, small);
        int removedCount = 0;
        world.ConstraintRemoved += _ => removedCount++;
        bodyA.MotionType = MotionType.Static;
        bodyB.MotionType = MotionType.Kinematic;
        Assert.That(removedCount, Is.Zero);

        if (removeBody) world.Remove(bodyA);
        else world.Remove(constraint);

        Assert.That(constraint.IsValid, Is.False);
        Assert.That(bodyB.Constraints, Is.Empty);
        Assert.That(removedCount, Is.EqualTo(1));
        AssertBufferCounts(world, small, 0, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProbeData
    {
        public int InternalIndex;
        public uint DispatchId;
        public ulong ConstraintId;
        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;
        public int PrepareCount;
        public int IterateCount;
    }

    public unsafe class SmallProbeConstraint : Constraint<ProbeData>
    {
        private static readonly uint RegisteredDispatchId = RegisterSmallConstraint(&Prepare, &Iterate);

        protected override void Create() => DispatchId = RegisteredDispatchId;

        private static void Prepare(ref SmallConstraintData constraint, in TimeStep timeStep)
            => Unsafe.As<SmallConstraintData, ProbeData>(ref constraint).PrepareCount++;

        private static void Iterate(ref SmallConstraintData constraint, in TimeStep timeStep)
            => Unsafe.As<SmallConstraintData, ProbeData>(ref constraint).IterateCount++;
    }

    public unsafe class FullProbeConstraint : SmallProbeConstraint
    {
        public override bool IsSmallConstraint => false;

        private static readonly uint RegisteredDispatchId = RegisterFullConstraint(&Prepare, &Iterate);

        protected override void Create() => DispatchId = RegisteredDispatchId;

        private static void Prepare(ref ConstraintData constraint, in TimeStep timeStep)
            => Unsafe.As<ConstraintData, ProbeData>(ref constraint).PrepareCount++;

        private static void Iterate(ref ConstraintData constraint, in TimeStep timeStep)
            => Unsafe.As<ConstraintData, ProbeData>(ref constraint).IterateCount++;
    }
}
