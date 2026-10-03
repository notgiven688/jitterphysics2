using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Jitter2.SoftBodies;

namespace JitterTests.Behavior;

[TestFixture]
public class AllowedMotionTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-12;
#else
    private const Real Tolerance = 1e-5f;
#endif

    private static World CreateWorld() => new()
    {
        Gravity = JVector.Zero,
        AllowDeactivation = false,
        SubstepCount = 1,
        SolverIterations = (1, 0)
    };

    private static RigidBody CreateBody(World world)
    {
        RigidBody body = world.CreateRigidBody();
        body.SetMassInertia(JSymmetricMatrix.Zero, (Real)0.5, true);
        body.Damping = (0, 0);
        return body;
    }

    [Test]
    public void BodyLayout_ReusesThreeInertiaScalarsWithoutGrowing()
    {
        Assert.That(Unsafe.SizeOf<RigidBodyData>(), Is.EqualTo(Precision.RigidBodyDataSize));
        Assert.That(Marshal.OffsetOf<RigidBodyData>(nameof(RigidBodyData.InverseInertiaWorld)).ToInt32(),
            Is.EqualTo(8 + 19 * Unsafe.SizeOf<Real>()));
        Assert.That(Marshal.OffsetOf<RigidBodyData>(nameof(RigidBodyData.InverseMassVector)).ToInt32(),
            Is.EqualTo(8 + 25 * Unsafe.SizeOf<Real>()));
        Assert.That(Marshal.OffsetOf<RigidBodyData>(nameof(RigidBodyData.Flags)).ToInt32(),
            Is.EqualTo(8 + 28 * Unsafe.SizeOf<Real>()));
    }

    [TestCase(MotionAxes.PlaneXY, 1, 1, 0, 0, 0, 1)]
    [TestCase(MotionAxes.PlaneXZ, 1, 0, 1, 0, 1, 0)]
    [TestCase(MotionAxes.PlaneYZ, 0, 1, 1, 1, 0, 0)]
    public void PlanePresets_RestrictWorldSpaceMotion(MotionAxes axes,
        int linearX, int linearY, int linearZ, int angularX, int angularY, int angularZ)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.SetMassInertia(JSymmetricMatrix.Identity, (Real)0.5, true);
        body.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        body.AllowedMotion = axes;
        JVector linear = new(linearX, linearY, linearZ);
        JVector angular = new(angularX, angularY, angularZ);
        Assert.That(axes, Is.EqualTo((MotionAxes)(linearX | (linearY << 1) | (linearZ << 2) |
                                                (angularX << 3) | (angularY << 4) | (angularZ << 5))));

        body.Velocity = body.AngularVelocity = JVector.One;
        Assert.That(body.Velocity, Is.EqualTo(linear));
        Assert.That(body.AngularVelocity, Is.EqualTo(angular));
        body.Velocity = body.AngularVelocity = JVector.Zero;
        body.ApplyImpulse(new JVector(2, 3, 4), new JVector(1, 2, 3));
        Assert.That(body.Velocity, Is.EqualTo(JVector.Multiply(linear, new JVector(1, (Real)1.5, 2))));
        Assert.That(JVector.MaxAbs(body.AngularVelocity - JVector.Multiply(angular, new JVector(-1, 2, -1))),
            Is.LessThan(Tolerance));
    }

    [Test]
    public void Locks_UpdateVelocityMassAndWarmStartImmediately()
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        Assert.That(body.AllowedMotion, Is.EqualTo(MotionAxes.All));
        Assert.That(body.Data.InverseMassVector, Is.EqualTo(new JVector((Real)0.5)));

        var joint = world.CreateConstraint<BallSocket>(world.NullBody, body);
        joint.Initialize(JVector.Zero);
        joint.Data.AccumulatedImpulse = JVector.One;
        body.Velocity = new JVector(2, 3, 4);
        body.AngularVelocity = new JVector(5, 6, 7);
        body.Data.DeltaVelocity = new JVector(8, 9, 10);
        body.AllowedMotion = MotionAxes.LinearY | MotionAxes.Angular;

        Assert.That(body.Velocity, Is.EqualTo(new JVector(0, 3, 0)));
        Assert.That(body.Data.DeltaVelocity, Is.EqualTo(new JVector(0, 9, 0)));
        Assert.That(body.AngularVelocity, Is.EqualTo(new JVector(5, 6, 7)));
        Assert.That(body.Mass, Is.EqualTo((Real)2));
        Assert.That(body.Data.InverseMassVector, Is.EqualTo(new JVector(0, (Real)0.5, 0)));
        Assert.That(joint.Data.AccumulatedImpulse, Is.EqualTo(JVector.Zero));

        body.Velocity = new JVector(8, 9, 10);
        Assert.That(body.Velocity, Is.EqualTo(new JVector(0, 9, 0)));
        body.AllowedMotion = MotionAxes.All;
        body.ApplyImpulse(new JVector(2, 0, 2));
        Assert.That(body.Velocity, Is.EqualTo(new JVector(1, 9, 1)));
    }

    [TestCase(-1)]
    [TestCase(64)]
    [TestCase(127)]
    [TestCase(int.MaxValue)]
    public void Locks_RejectUndefinedBitsWithoutChangingState(int value)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        Assert.Throws<ArgumentOutOfRangeException>(() => body.AllowedMotion = (MotionAxes)value);
        Assert.That(body.AllowedMotion, Is.EqualTo(MotionAxes.All));
        Assert.That(body.Data.InverseMassVector, Is.EqualTo(new JVector((Real)0.5)));
    }

    [TestCase(0, 0, 0)]
    [TestCase(0, 0, 1)]
    [TestCase(0, 1, 0)]
    [TestCase(0, 1, 1)]
    [TestCase(1, 0, 0)]
    [TestCase(1, 0, 1)]
    [TestCase(1, 1, 0)]
    [TestCase(1, 1, 1)]
    public void Impulse_RespectsEveryAxisCombination(int x, int y, int z)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.AllowedMotion = MotionAxes.Angular | (MotionAxes)(x | (y << 1) | (z << 2));
        body.ApplyImpulse(new JVector(2, 4, 6));
        Assert.That(body.Velocity, Is.EqualTo(new JVector(x, 2 * y, 3 * z)));
    }

    [Test]
    public void OffCenterImpulse_KeepsTorqueFromLockedLinearDirection()
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.SetMassInertia(JSymmetricMatrix.Identity, (Real)0.5, true);
        body.AllowedMotion = MotionAxes.Angular;
        body.ApplyImpulse(new JVector(0, 3, 0), new JVector(2, 0, 0));
        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        Assert.That(body.AngularVelocity, Is.EqualTo(new JVector(0, 0, 6)));
    }

    [TestCase(false, SolveMode.Regular)]
    [TestCase(true, SolveMode.Regular)]
    [TestCase(false, SolveMode.Deterministic)]
    [TestCase(true, SolveMode.Deterministic)]
    public void ForcesAndGravity_RespectWorldAxesAcrossSubsteps(bool multiThread, SolveMode mode)
    {
        using World world = CreateWorld();
        world.SolveMode = mode;
        world.SubstepCount = 4;
        world.Gravity = new JVector(3, 4, 5);
        RigidBody body = CreateBody(world);
        body.Position = new JVector(1, 2, 3);
        body.Orientation = JQuaternion.CreateRotationZ((Real)1.2);
        body.AllowedMotion = MotionAxes.All & ~MotionAxes.LinearY;
        body.AddForce(new JVector(2, 4, 6));
        Real dt = (Real)(1.0 / 60.0);
        // Force deltas are prepared at the end of a step and applied in the next step.
        world.Step(dt, multiThread);
        world.Step(dt, multiThread);

        Assert.That(body.Velocity.X, Is.EqualTo(4 * dt).Within(Tolerance));
        Assert.That(body.Velocity.Y, Is.Zero);
        Assert.That(body.Velocity.Z, Is.EqualTo(8 * dt).Within(Tolerance));
        Assert.That(body.Position.Y, Is.EqualTo((Real)2));
    }

    [Test]
    public void MassAndMotionTypeChanges_PreserveLocksAndRestoreEffectiveMass()
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.AllowedMotion = MotionAxes.All & ~MotionAxes.LinearY;
        body.SetMassInertia(JSymmetricMatrix.Identity, 4);
        Assert.That(body.Data.InverseMassVector, Is.EqualTo(new JVector((Real)0.25, 0, (Real)0.25)));
        body.MotionType = MotionType.Kinematic;
        Assert.That(body.Data.InverseMassVector, Is.EqualTo(JVector.Zero));
        body.Velocity = new JVector(2, 3, 4);
        world.Step((Real)0.01, false);
        Assert.That(body.Velocity, Is.EqualTo(new JVector(2, 0, 4)));
        Assert.That(body.Position.Y, Is.Zero);
        body.MotionType = MotionType.Static;
        Assert.That(body.Data.InverseMassVector, Is.EqualTo(JVector.Zero));
        body.MotionType = MotionType.Dynamic;
        Assert.That(body.Data.InverseMassVector, Is.EqualTo(new JVector((Real)0.25, 0, (Real)0.25)));
        Assert.That(body.AllowedMotion, Is.EqualTo(MotionAxes.All & ~MotionAxes.LinearY));
    }

    [Test]
    public void ReusedBodyStorage_StartsWithUnrestrictedMotion()
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.AllowedMotion = MotionAxes.None;
        world.Remove(body);
        RigidBody replacement = world.CreateRigidBody();
        Assert.That(replacement.AllowedMotion, Is.EqualTo(MotionAxes.All));
        Assert.That(replacement.Data.HasLinearLocks, Is.False);
        Assert.That(replacement.Data.InverseMassVector, Is.EqualTo(JVector.One));
    }

    private static ContactData CreateContact(World world, RigidBody body, JVector normal, JVector point = default)
    {
        ContactData contact = default;
        contact.Init(world.NullBody, body);
        contact.ResetMode();
        contact.Friction = 0;
        contact.Contact0.Initialize(ref world.NullBody.Data, ref body.Data,
            point, point, normal, true, 0);
        contact.UsageMask = ContactData.MaskContact0;
        return contact;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ObliqueContact_UsesDirectionalMassInOneIteration(bool accelerated)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.AllowedMotion = MotionAxes.LinearY | MotionAxes.Angular;
        body.Velocity = new JVector(0, -3, 0);
        ContactData contact = CreateContact(world, body, JVector.Normalize(new JVector(1, 1, 0)));
        if (accelerated)
        {
            contact.PrepareForIterationAccelerated(100);
            contact.IterateAccelerated(false);
        }
        else
        {
            contact.PrepareForIterationScalar(100);
            contact.IterateScalar(false);
        }

        Assert.That(JVector.MaxAbs(body.Velocity), Is.LessThan(Tolerance));
        Assert.That(contact.Contact0.MassNormalTangent.GetElement(0), Is.EqualTo((Real)4).Within(Tolerance));
        Assert.That(contact.Contact0.MassNormalTangent.GetElement(2), Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CompletelyBlockedContact_RemainsFinite(bool accelerated)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.AllowedMotion = MotionAxes.None;
        ContactData contact = CreateContact(world, body, JVector.UnitY);
        contact.Contact0.PenaltyBias = 1;
        if (accelerated) contact.PrepareForIterationAccelerated(100);
        else contact.PrepareForIterationScalar(100);
        for (int i = 0; i < 3; i++)
        {
            if (accelerated) contact.IterateAccelerated(true);
            else contact.IterateScalar(true);
        }

        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
        Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Zero));
        Assert.That(contact.Contact0.Impulse, Is.Zero);
        Assert.That(contact.Contact0.TangentImpulse1, Is.Zero);
        Assert.That(contact.Contact0.TangentImpulse2, Is.Zero);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void ContactModes_UseOnlyParticipatingLinearMass(bool accelerated, bool linearLocks)
    {
        using World world = CreateWorld();
        RigidBody body1 = CreateBody(world);
        RigidBody body2 = CreateBody(world);
        body2.SetMassInertia(JSymmetricMatrix.Zero, (Real)0.25, true);
        if (linearLocks)
        {
            body1.AllowedMotion = body2.AllowedMotion = MotionAxes.All & ~MotionAxes.LinearY;
        }

        JVector normal = linearLocks ? JVector.Normalize(new JVector(1, 1, 0)) : JVector.UnitX;
        for (int bits = 0; bits < 16; bits++)
        {
            body1.Velocity = JVector.UnitX;
            body2.Velocity = JVector.Zero;
            ContactData contact = default;
            contact.Init(body1, body2);
            contact.Mode = (ContactData.SolveMode)bits;
            contact.Friction = 0;
            contact.Contact0.Initialize(ref body1.Data, ref body2.Data,
                JVector.Zero, JVector.Zero, normal, true, 0);
            contact.UsageMask = ContactData.MaskContact0;
            if (accelerated)
            {
                contact.PrepareForIterationAccelerated(100);
                contact.IterateAccelerated(false);
            }
            else
            {
                contact.PrepareForIterationScalar(100);
                contact.IterateScalar(false);
            }

            bool linear1 = (contact.Mode & ContactData.SolveMode.LinearBody1) != 0;
            bool linear2 = (contact.Mode & ContactData.SolveMode.LinearBody2) != 0;
            Real inverseMass = (linear1 ? (Real)0.5 : 0) + (linear2 ? (Real)0.25 : 0);
            Real impulseX = inverseMass > 0 ? (Real)1 / inverseMass : 0;
            JVector expected1 = new(1 - (linear1 ? (Real)0.5 * impulseX : 0), 0, 0);
            JVector expected2 = new(linear2 ? (Real)0.25 * impulseX : 0, 0, 0);
            Assert.That(JVector.MaxAbs(body1.Velocity - expected1), Is.LessThan(Tolerance), $"Mode {contact.Mode}");
            Assert.That(JVector.MaxAbs(body2.Velocity - expected2), Is.LessThan(Tolerance), $"Mode {contact.Mode}");
            Assert.That(body1.AngularVelocity, Is.EqualTo(JVector.Zero));
            Assert.That(body2.AngularVelocity, Is.EqualTo(JVector.Zero));
        }
    }

    [TestCase("DistanceLimit")]
    [TestCase("PointOnPlane")]
    [TestCase("SpringConstraint")]
    [TestCase("LinearMotor")]
    [TestCase("BallSocket")]
    [TestCase("PointOnLine")]
    public void Joint_StopsPermittedVelocityInOneIteration(string kind)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.AllowedMotion = MotionAxes.LinearY | MotionAxes.Angular;
        body.Velocity = new JVector(0, -3, 0);
        JVector axis = JVector.Normalize(new JVector(1, 2, 3));

        switch (kind)
        {
            case "DistanceLimit":
                body.Position = axis;
                var distance = world.CreateConstraint<DistanceLimit>(world.NullBody, body);
                distance.Initialize(JVector.Zero, body.Position);
                distance.Softness = 0;
                distance.Bias = 0;
                break;
            case "PointOnPlane":
                var plane = world.CreateConstraint<PointOnPlane>(world.NullBody, body);
                plane.Initialize(axis, JVector.Zero, JVector.Zero);
                plane.Softness = 0;
                plane.Bias = 0;
                break;
            case "SpringConstraint":
                body.Position = axis;
                var spring = world.CreateConstraint<SpringConstraint>(world.NullBody, body);
                spring.Initialize(JVector.Zero, body.Position);
                spring.Softness = 0;
                spring.Bias = 0;
                break;
            case "LinearMotor":
                var motor = world.CreateConstraint<LinearMotor>(world.NullBody, body);
                motor.Initialize(axis, axis);
                motor.MaximumForce = 10000;
                break;
            case "BallSocket":
                var socket = world.CreateConstraint<BallSocket>(world.NullBody, body);
                socket.Initialize(JVector.Zero);
                socket.Softness = 0;
                socket.Bias = 0;
                break;
            case "PointOnLine":
                var line = world.CreateConstraint<PointOnLine>(world.NullBody, body);
                line.Initialize(axis, JVector.Zero, JVector.Zero);
                line.Softness = 0;
                line.LimitSoftness = 0;
                line.Bias = 0;
                line.LimitBias = 0;
                break;
        }

        world.Step((Real)0.01, false);
        Assert.That(body.Velocity.X, Is.Zero);
        Assert.That(body.Velocity.Z, Is.Zero);
        Assert.That(MathR.Abs(body.Velocity.Y), Is.LessThan(Tolerance));
    }

    [Test]
    public void AllFlagCombinations_MaskVelocitiesDeltasAndImpulseResponse()
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.SetMassInertia(JSymmetricMatrix.Identity, (Real)0.5, true);
        body.EnableGyroscopicForces = true;
        for (int bits = 0; bits < 64; bits++)
        {
            body.AllowedMotion = MotionAxes.All;
            body.Velocity = body.AngularVelocity = JVector.One;
            body.Data.DeltaVelocity = body.Data.DeltaAngularVelocity = JVector.One;
            body.AllowedMotion = (MotionAxes)bits;
            JVector linear = new((bits & 1) != 0 ? 1 : 0, (bits & 2) != 0 ? 1 : 0, (bits & 4) != 0 ? 1 : 0);
            JVector angular = new((bits & 8) != 0 ? 1 : 0, (bits & 16) != 0 ? 1 : 0, (bits & 32) != 0 ? 1 : 0);

            Assert.That(body.AllowedMotion, Is.EqualTo((MotionAxes)bits));
            Assert.That(body.Data.AllowedMotion, Is.EqualTo((MotionAxes)bits));
            Assert.That((body.Data.Flags >> 4) & 63, Is.EqualTo(63 ^ bits));
            Assert.That(body.Data.HasLinearLocks, Is.EqualTo((bits & 7) != 7));
            Assert.That(body.Data.HasAngularLocks, Is.EqualTo((bits & 56) != 56));
            Assert.That(body.Data.HasMotionLocks, Is.EqualTo(bits != 63));
            Assert.That(body.Velocity, Is.EqualTo(linear));
            Assert.That(body.AngularVelocity, Is.EqualTo(angular));
            Assert.That(body.Data.DeltaVelocity, Is.EqualTo(linear));
            Assert.That(body.Data.DeltaAngularVelocity, Is.EqualTo(angular));
            Assert.That(body.MotionType, Is.EqualTo(MotionType.Dynamic));
            Assert.That(body.IsActive, Is.True);
            Assert.That(body.EnableGyroscopicForces, Is.True);

            body.Velocity = body.AngularVelocity = JVector.Zero;
            body.ApplyImpulse(new JVector(2, 3, 4), new JVector(1, 2, 3));
            Assert.That(body.Velocity, Is.EqualTo(JVector.Multiply(linear, new JVector(1, (Real)1.5, 2))));
            Assert.That(body.AngularVelocity, Is.EqualTo(JVector.Multiply(angular, new JVector(-1, 2, -1))));

            body.Velocity = body.AngularVelocity = JVector.One;
            Assert.That(body.Velocity, Is.EqualTo(linear));
            Assert.That(body.AngularVelocity, Is.EqualTo(angular));
        }
    }

    [Test]
    public void AngularLocks_RestrictWorldTensorAndUnlockWithoutLosingPhysicalInertia()
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.SetMassInertia(JSymmetricMatrix.CreateScale(2, 3, 4), (Real)0.5, true);
        body.AllowedMotion = MotionAxes.All & ~MotionAxes.AngularY;
        body.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        JSymmetricMatrix full = JSymmetricMatrix.Transform(JSymmetricMatrix.CreateScale(2, 3, 4),
            JMatrix.CreateFromQuaternion(body.Orientation));
        Assert.That(JMatrix.Inverse(full.ToMatrix(), out JMatrix constrainedInertia), Is.True);
        constrainedInertia.M12 = constrainedInertia.M21 = constrainedInertia.M23 = constrainedInertia.M32 = 0;
        constrainedInertia.M22 = 1;
        Assert.That(JMatrix.Inverse(constrainedInertia, out JMatrix expected), Is.True);
        expected.M22 = 0;
        for (int axis = 0; axis < 3; axis++)
        {
            Assert.That(JVector.MaxAbs(body.Data.InverseInertiaWorld.ToMatrix().GetColumn(axis) - expected.GetColumn(axis)),
                Is.LessThan(Tolerance));
        }
        Assert.That(body.InverseInertia, Is.EqualTo(JSymmetricMatrix.CreateScale(2, 3, 4)));

        body.MotionType = MotionType.Kinematic;
        Assert.That(body.Data.InverseInertiaWorld, Is.EqualTo(JSymmetricMatrix.Zero));
        body.AngularVelocity = new JVector(2, 3, 4);
        world.Step((Real)0.01, false);
        Assert.That(body.AngularVelocity, Is.EqualTo(new JVector(2, 0, 4)));
        body.MotionType = MotionType.Dynamic;
        full = JSymmetricMatrix.Transform(JSymmetricMatrix.CreateScale(2, 3, 4),
            JMatrix.CreateFromQuaternion(body.Orientation));
        body.AllowedMotion = MotionAxes.All;
        Assert.That(body.Data.InverseInertiaWorld, Is.EqualTo(full));
    }

    [TestCase(MotionAxes.All)]
    [TestCase(MotionAxes.All & ~MotionAxes.AngularX)]
    [TestCase(MotionAxes.All & ~MotionAxes.AngularY)]
    [TestCase(MotionAxes.All & ~MotionAxes.AngularZ)]
    [TestCase(MotionAxes.Linear | MotionAxes.AngularZ)]
    [TestCase(MotionAxes.Linear | MotionAxes.AngularY)]
    [TestCase(MotionAxes.Linear | MotionAxes.AngularX)]
    [TestCase(MotionAxes.Linear)]
    public void AngularLocks_RespondWithConstrainedPhysicalInertia(MotionAxes axes)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        JSymmetricMatrix physicalInertia = new(4, 1, 2, 5, 1, 6);
        body.SetMassInertia(physicalInertia, 1);
        body.AllowedMotion = axes;

        // Invert the permitted block of physical inertia independently of the Schur-complement implementation.
        JMatrix constrainedInertia = physicalInertia.ToMatrix();
        if ((axes & MotionAxes.AngularX) == 0)
        {
            constrainedInertia.M12 = constrainedInertia.M21 = constrainedInertia.M13 = constrainedInertia.M31 = 0;
            constrainedInertia.M11 = 1;
        }
        if ((axes & MotionAxes.AngularY) == 0)
        {
            constrainedInertia.M12 = constrainedInertia.M21 = constrainedInertia.M23 = constrainedInertia.M32 = 0;
            constrainedInertia.M22 = 1;
        }
        if ((axes & MotionAxes.AngularZ) == 0)
        {
            constrainedInertia.M13 = constrainedInertia.M31 = constrainedInertia.M23 = constrainedInertia.M32 = 0;
            constrainedInertia.M33 = 1;
        }
        Assert.That(JMatrix.Inverse(constrainedInertia, out JMatrix expected), Is.True);
        if ((axes & MotionAxes.AngularX) == 0) expected.M11 = 0;
        if ((axes & MotionAxes.AngularY) == 0) expected.M22 = 0;
        if ((axes & MotionAxes.AngularZ) == 0) expected.M33 = 0;

        JVector impulse = new(2, -1, 1);
        JVector position = new(1, 2, 3);
        body.ApplyImpulse(impulse, position);
        JVector expectedVelocity = JVector.Transform(position % impulse, expected);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - expectedVelocity), Is.LessThan(Tolerance));
    }

    [TestCase(MotionAxes.All)]
    [TestCase(MotionAxes.All & ~MotionAxes.LinearX)]
    [TestCase(MotionAxes.All & ~MotionAxes.AngularX)]
    public void TorquePreparation_UsesCurrentOrientation(MotionAxes axes)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.SetMassInertia(JSymmetricMatrix.CreateScale(1, 4, 9), 1, true);
        body.AllowedMotion = axes;
        body.AngularVelocity = JVector.UnitZ;
        JVector torque = new(1, 2, 0);
        body.Torque = torque;
        Real dt = (Real)0.25;
        world.Step(dt, false);

        JVector expected = JVector.Transform(torque, body.Data.InverseInertiaWorld) * dt;
        Assert.That(JVector.MaxAbs(body.Data.DeltaAngularVelocity - expected), Is.LessThan(Tolerance));
        world.Step(dt, false);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - (JVector.UnitZ + expected)), Is.LessThan(Tolerance));
    }

    [TestCase(false, SolveMode.Regular)]
    [TestCase(true, SolveMode.Regular)]
    [TestCase(false, SolveMode.Deterministic)]
    [TestCase(true, SolveMode.Deterministic)]
    public void Torque_RespectsWorldAngularLocksAcrossSubsteps(bool multiThread, SolveMode mode)
    {
        using World world = CreateWorld();
        world.SubstepCount = 4;
        world.SolveMode = mode;
        RigidBody body = CreateBody(world);
        body.SetMassInertia(JSymmetricMatrix.CreateScale(2, 3, 4), 1, true);
        body.Orientation = JQuaternion.Normalize(new JQuaternion(1, 2, 3, 4));
        body.AllowedMotion = MotionAxes.All & ~MotionAxes.AngularY;
        JVector torque = new(2, 3, 4);
        Real dt = (Real)0.01;
        JVector expected = JVector.Transform(torque, body.Data.InverseInertiaWorld) * dt;
        body.Torque = torque;
        world.Step(dt, multiThread);
        world.Step(dt, multiThread);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - expected), Is.LessThan(Tolerance));
        Assert.That(body.AngularVelocity.Y, Is.Zero);
    }

    [TestCase(MotionAxes.All & ~MotionAxes.LinearY)]
    [TestCase(MotionAxes.All & ~MotionAxes.AngularX)]
    public void Gyro_SkipsLockedBodiesAndResumesWhenUnlocked(MotionAxes axes)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.SetMassInertia(JSymmetricMatrix.CreateScale(2, 3, 4), 1);
        body.EnableGyroscopicForces = true;
        body.AllowedMotion = axes;
        body.AngularVelocity = new JVector(1, 2, 3);
        JVector omega = body.AngularVelocity;
        world.Step((Real)0.01, false);
        Assert.That(body.AngularVelocity, Is.EqualTo(omega));
        body.AllowedMotion = MotionAxes.All;
        world.Step((Real)0.01, false);
        Assert.That(JVector.MaxAbs(body.AngularVelocity - omega), Is.GreaterThan(Tolerance));
    }

    [TestCase("FixedAngle", false)]
    [TestCase("FixedAngle", true)]
    [TestCase("HingeAngle", false)]
    [TestCase("HingeAngle", true)]
    [TestCase("TwistAngle", false)]
    [TestCase("TwistAngle", true)]
    [TestCase("ConeLimit", false)]
    [TestCase("ConeLimit", true)]
    [TestCase("AngularMotor", false)]
    [TestCase("AngularMotor", true)]
    public void AngularJoint_HandlesPartiallyAndFullyBlockedRotation(string kind, bool fullyBlocked)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.SetMassInertia(JSymmetricMatrix.Identity, 1, true);
        body.AllowedMotion = MotionAxes.Linear | (fullyBlocked ? MotionAxes.None : MotionAxes.AngularY);
        body.AngularVelocity = new JVector(0, -3, 0);
        JVector axis = JVector.Normalize(new JVector(1, 2, 3));
        switch (kind)
        {
            case "FixedAngle":
                var fixedAngle = world.CreateConstraint<FixedAngle>(world.NullBody, body);
                fixedAngle.Initialize();
                fixedAngle.Softness = fixedAngle.Bias = 0;
                break;
            case "HingeAngle":
                var hinge = world.CreateConstraint<HingeAngle>(world.NullBody, body);
                hinge.Initialize(axis, AngularLimit.Fixed);
                hinge.Softness = hinge.LimitSoftness = hinge.Bias = hinge.LimitBias = 0;
                break;
            case "TwistAngle":
                var twist = world.CreateConstraint<TwistAngle>(world.NullBody, body);
                twist.Initialize(axis, axis, AngularLimit.Fixed);
                twist.Softness = twist.Bias = 0;
                break;
            case "ConeLimit":
                var cone = world.CreateConstraint<ConeLimit>(world.NullBody, body);
                cone.Initialize(JVector.UnitX, JVector.UnitZ, AngularLimit.FromDegree(90, 90));
                cone.Softness = cone.Bias = 0;
                break;
            case "AngularMotor":
                var motor = world.CreateConstraint<AngularMotor>(world.NullBody, body);
                motor.Initialize(axis);
                motor.MaximumForce = 10000;
                break;
        }
        world.Step((Real)0.01, false);
        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan(Tolerance));
        Assert.That(Real.IsFinite(body.Orientation.LengthSquared()), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OffCenterContact_StopsUnlockedRotationWithTranslationBlocked(bool accelerated)
    {
        using World world = CreateWorld();
        RigidBody body = CreateBody(world);
        body.SetMassInertia(JSymmetricMatrix.Identity, 1, true);
        body.AllowedMotion = MotionAxes.AngularY;
        body.AngularVelocity = new JVector(0, 3, 0);
        ContactData contact = CreateContact(world, body, JVector.UnitZ, JVector.UnitX);
        if (accelerated)
        {
            contact.PrepareForIterationAccelerated(100);
            contact.IterateAccelerated(false);
        }
        else
        {
            contact.PrepareForIterationScalar(100);
            contact.IterateScalar(false);
        }
        Assert.That(JVector.MaxAbs(body.AngularVelocity), Is.LessThan(Tolerance));
        Assert.That(body.Velocity, Is.EqualTo(JVector.Zero));
    }
}
