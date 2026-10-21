namespace JitterTests.Regression;

public class ContactSubstepTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-12;
#else
    private const Real Tolerance = 1e-5f;
#endif

    [TestCase(false)]
    [TestCase(true)]
    public void RotationWithinStep_UpdatesPenetrationWithoutAddingNormalTorque(bool accelerated)
    {
        using World world = new();
        RigidBody body = world.CreateRigidBody();
        body.AddShape(new SphereShape((Real)0.5));
        body.Position = new JVector(0, (Real)0.45, 0);
        body.Velocity = -JVector.UnitY;

        ContactData contact = default;
        contact.Init(world.NullBody, body);
        contact.Friction = contact.Restitution = 0;
        contact.AddContact(JVector.Zero, new JVector(0, (Real)(-0.05), 0), JVector.UnitY);
        contact.ResetMode();

        body.Orientation = JQuaternion.CreateRotationZ((Real)0.3);
        TimeStep timeStep = new((Real)0.01, 3);
        if (accelerated) contact.PrepareForIterationAccelerated(timeStep);
        else contact.PrepareForIterationScalar(timeStep);

        Real penetration = (Real)0.5 * MathR.Cos((Real)0.3) - body.Position.Y;
        Real expectedBias = ContactData.Contact.BiasFactor * timeStep.InverseStepDt
            * (penetration - ContactData.Contact.AllowedPenetration);

        Assert.That(contact.Contact0.PenaltyBias, Is.EqualTo(expectedBias).Within(Tolerance));
        Assert.That(JVector.MaxAbs(contact.Contact0.RelativePosition2 + (Real)0.5 * JVector.UnitY),
            Is.LessThan(Tolerance));

        if (accelerated) contact.IterateAccelerated(true);
        else contact.IterateScalar(true);

        Assert.That(body.Velocity.Y, Is.EqualTo(expectedBias).Within(Tolerance));
        Assert.That(body.AngularVelocity.Length(), Is.LessThan(Tolerance));
    }

    [TestCase(SolveMode.Regular, false)]
    [TestCase(SolveMode.Regular, true)]
    [TestCase(SolveMode.Deterministic, false)]
    [TestCase(SolveMode.Deterministic, true)]
    public void CachedContact_KeepsOffsetsFixedUntilSolvingFinishes(SolveMode solveMode, bool multiThread)
    {
        using World world = new();
        world.Gravity = JVector.Zero;
        world.SubstepCount = 3;
        world.SolveMode = solveMode;
        world.NarrowPhaseFilter = new RejectContacts();

        RigidBody floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(20, 1, 20));
        floor.Position = -JVector.UnitY;
        floor.MotionType = MotionType.Static;

        RigidBody body = world.CreateRigidBody();
        body.AddShape(new SphereShape((Real)0.5));
        body.Damping = (0, 0);
        body.AngularVelocity = JVector.UnitZ;

        JVector point = -(Real)0.5 * JVector.UnitY;
        world.RegisterContact(floor.Shapes[0].ShapeId, body.Shapes[0].ShapeId,
            floor, body, point, point, JVector.UnitY);
        Assert.That(world.GetArbiter(floor.Shapes[0].ShapeId, body.Shapes[0].ShapeId, out Arbiter? arbiter), Is.True);

        JVector expectedOffset = default;
        int substeps = 0;
        world.PreStep += _ => expectedOffset = JVector.Transform(point, body.Orientation);
        world.PostSubStep += _ =>
        {
            Assert.That(JVector.MaxAbs(arbiter!.Handle.Data.Contact0.RelativePosition2 - expectedOffset),
                Is.LessThan(Tolerance));
            substeps++;
        };

        for (int i = 0; i < 2; i++)
        {
            world.Step((Real)0.01, multiThread);
            JVector currentOffset = JVector.Transform(point, body.Orientation);
            Assert.That(JVector.MaxAbs(arbiter!.Handle.Data.Contact0.RelativePosition2 - currentOffset),
                Is.LessThan(Tolerance), "After solving, cached offsets must be current for contact matching.");
        }

        Assert.That(substeps, Is.EqualTo(6));
        Assert.That(body.Contacts, Has.Count.EqualTo(1));
    }

    [TestCase(SolveMode.Regular, 1, false)]
    [TestCase(SolveMode.Regular, 3, false)]
    [TestCase(SolveMode.Regular, 10, false)]
    [TestCase(SolveMode.Deterministic, 1, false)]
    [TestCase(SolveMode.Deterministic, 3, false)]
    [TestCase(SolveMode.Deterministic, 10, false)]
    [TestCase(SolveMode.Regular, 1, true)]
    [TestCase(SolveMode.Regular, 3, true)]
    [TestCase(SolveMode.Regular, 10, true)]
    [TestCase(SolveMode.Deterministic, 1, true)]
    [TestCase(SolveMode.Deterministic, 3, true)]
    [TestCase(SolveMode.Deterministic, 10, true)]
    public void RollingSphere_DoesNotAccelerateWithSolverSubsteps(
        SolveMode solveMode, int substeps, bool auxiliaryContacts)
    {
        using World world = new();
        world.SubstepCount = substeps;
        world.SolverIterations = (4, 2);
        world.SolveMode = solveMode;
        world.AllowDeactivation = false;
        world.EnableAuxiliaryContactPoints = auxiliaryContacts;

        RigidBody floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(200, 2, 200));
        floor.Position = -JVector.UnitY;
        floor.MotionType = MotionType.Static;

        RigidBody sphere = world.CreateRigidBody();
        sphere.AddShape(new SphereShape((Real)0.5));
        sphere.Position = new JVector(0, (Real)0.495, 0);
        sphere.Velocity = JVector.UnitX;
        sphere.AngularVelocity = -2 * JVector.UnitZ;
        sphere.Damping = (0, 0);

        // Supply an exact sphere/floor contact to isolate contact generation and solving
        // from narrowphase accuracy.
        ExactSphereFloorContact filter = new(floor);
        world.NarrowPhaseFilter = filter;
        const int steps = 1000;
        for (int i = 0; i < steps; i++) world.Step((Real)0.01, false);

        Assert.That(JVector.MaxAbs(sphere.Velocity - JVector.UnitX), Is.LessThan(20 * Tolerance));
        Assert.That(JVector.MaxAbs(sphere.AngularVelocity + 2 * JVector.UnitZ), Is.LessThan(20 * Tolerance));
        Assert.That(sphere.Position.Y, Is.EqualTo((Real)0.495).Within(20 * Tolerance));
        Assert.That(filter.Queries, Is.EqualTo(steps), "Collision detection must still run once per full step.");
    }

    [TestCase(1, false, false)]
    [TestCase(1, false, true)]
    [TestCase(1, true, false)]
    [TestCase(1, true, true)]
    [TestCase(4, false, false)]
    [TestCase(4, false, true)]
    [TestCase(4, true, false)]
    [TestCase(4, true, true)]
    public void RollingSphere_KeepsSpeedWithGeneratedContacts(int substeps, bool auxiliaryContacts, bool reversed)
    {
        using World world = new();
        world.Gravity = new JVector(0, (Real)(-9.81), 0);
        world.SubstepCount = substeps;
        world.SolverIterations = (6, 4);
        world.AllowDeactivation = false;
        world.EnableAuxiliaryContactPoints = auxiliaryContacts;

        RigidBody floor, sphere;
        if (reversed)
        {
            sphere = world.CreateRigidBody();
            sphere.AddShape(new SphereShape((Real)0.5));
            floor = world.CreateRigidBody();
            floor.AddShape(new BoxShape(20));
        }
        else
        {
            floor = world.CreateRigidBody();
            floor.AddShape(new BoxShape(20));
            sphere = world.CreateRigidBody();
            sphere.AddShape(new SphereShape((Real)0.5));
        }

        floor.Position = new JVector(0, -10, 0);
        floor.MotionType = MotionType.Static;
        floor.Damping = (0, 0);
        floor.Friction = sphere.Friction = (Real)0.2;
        floor.Restitution = sphere.Restitution = 0;
        sphere.Position = new JVector(0, (Real)0.5, 0);
        sphere.SetMassInertia(1);
        sphere.Damping = (0, 0);
        for (int step = 0; step < 100; step++) world.Step((Real)0.01, false);

        sphere.Velocity = new JVector((Real)0.1, 0, 0);
        sphere.AngularVelocity = new JVector(0, 0, (Real)(-0.2));
        for (int step = 0; step < 6000; step++) world.Step((Real)0.01, false);

        Assert.That((sphere.Velocity - new JVector((Real)0.1, 0, 0)).Length(), Is.LessThan((Real)1e-4));
        Assert.That((sphere.AngularVelocity - new JVector(0, 0, (Real)(-0.2))).Length(), Is.LessThan((Real)2e-4));
    }

    [TestCase(SolveMode.Regular, 1)]
    [TestCase(SolveMode.Regular, 3)]
    [TestCase(SolveMode.Deterministic, 1)]
    [TestCase(SolveMode.Deterministic, 3)]
    public void RestingCylinder_DoesNotStartRollingWithAuxiliaryContacts(SolveMode solveMode, int substeps)
    {
        using World world = new();
        world.SubstepCount = substeps;
        world.SolverIterations = (4, 2);
        world.SolveMode = solveMode;
        world.AllowDeactivation = false;
        world.EnableAuxiliaryContactPoints = true;

        RigidBody floor = world.CreateRigidBody();
        floor.AddShape(new BoxShape(200));
        floor.Position = new JVector(0, -100, 0);
        floor.MotionType = MotionType.Static;

        RigidBody cylinder = world.CreateRigidBody();
        cylinder.AddShape(new CylinderShape(2, (Real)0.5));
        cylinder.Position = new JVector(10, (Real)0.495, 0);
        cylinder.Orientation = JQuaternion.CreateRotationZ(MathR.PI / 2);
        cylinder.Damping = (0, 0);
        cylinder.Restitution = floor.Restitution = 0;

        for (int i = 0; i < 1000; i++) world.Step((Real)0.01, false);

        Assert.That(cylinder.Velocity.Length(), Is.LessThan((Real)0.02));
        Assert.That(cylinder.AngularVelocity.Length(), Is.LessThan((Real)0.04));
        Assert.That(cylinder.Position.Y, Is.EqualTo((Real)0.495).Within((Real)0.001));
    }

    private sealed class RejectContacts : INarrowPhaseFilter
    {
        public bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB, ref JVector pointA,
            ref JVector pointB, ref JVector normal, ref Real penetration) => false;
    }

    private sealed class ExactSphereFloorContact(RigidBody floor) : INarrowPhaseFilter
    {
        public int Queries { get; private set; }

        public bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB, ref JVector pointA,
            ref JVector pointB, ref JVector normal, ref Real penetration)
        {
            Queries++;
            if (shapeA.RigidBody == floor)
            {
                normal = JVector.UnitY;
                pointB = shapeB.RigidBody.Position - (Real)0.5 * JVector.UnitY;
                pointA = new JVector(pointB.X, 0, pointB.Z);
                penetration = -pointB.Y;
            }
            else
            {
                normal = -JVector.UnitY;
                pointA = shapeA.RigidBody.Position - (Real)0.5 * JVector.UnitY;
                pointB = new JVector(pointA.X, 0, pointA.Z);
                penetration = -pointA.Y;
            }

            return true;
        }
    }
}
