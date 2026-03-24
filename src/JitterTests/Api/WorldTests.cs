namespace JitterTests.Api;

/// <summary>
/// Tests for World properties and lifecycle management.
/// </summary>
public class WorldTests
{
    // -------------------------------------------------------------------------
    // Default properties
    // -------------------------------------------------------------------------

    [TestCase]
    public void DefaultGravity_IsNegativeY()
    {
        var world = new World();
        Assert.That(world.Gravity.Y, Is.LessThan(0));
        Assert.That(world.Gravity.X, Is.EqualTo(0));
        Assert.That(world.Gravity.Z, Is.EqualTo(0));
        world.Dispose();
    }

    [TestCase]
    public void DefaultAllowDeactivation_IsTrue()
    {
        var world = new World();
        Assert.That(world.AllowDeactivation, Is.True);
        world.Dispose();
    }

    [TestCase]
    public void DefaultRigidBodies_ContainsOnlyNullBody()
    {
        var world = new World();
        // NullBody is always present
        Assert.That(world.RigidBodies, Has.Count.EqualTo(1));
        Assert.That(world.RigidBodies, Does.Contain(world.NullBody));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Property setters
    // -------------------------------------------------------------------------

    [TestCase]
    public void Gravity_RoundTrip()
    {
        var world = new World();
        var expected = new JVector(0, -20, 0);
        world.Gravity = expected;
        Assert.That(world.Gravity, Is.EqualTo(expected));
        world.Dispose();
    }

    [TestCase]
    public void Gravity_SetToZero()
    {
        var world = new World();
        world.Gravity = JVector.Zero;
        Assert.That(world.Gravity, Is.EqualTo(JVector.Zero));
        world.Dispose();
    }

    [TestCase]
    public void AllowDeactivation_RoundTrip()
    {
        var world = new World();
        world.AllowDeactivation = false;
        Assert.That(world.AllowDeactivation, Is.False);
        world.AllowDeactivation = true;
        Assert.That(world.AllowDeactivation, Is.True);
        world.Dispose();
    }

    [TestCase]
    public void SubstepCount_RoundTrip()
    {
        var world = new World();
        world.SubstepCount = 4;
        Assert.That(world.SubstepCount, Is.EqualTo(4));
        world.Dispose();
    }

    [TestCase]
    public void SolverIterations_RoundTrip()
    {
        var world = new World();
        world.SolverIterations = (10, 4);
        Assert.That(world.SolverIterations.solver, Is.EqualTo(10));
        Assert.That(world.SolverIterations.relaxation, Is.EqualTo(4));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Body management
    // -------------------------------------------------------------------------

    [TestCase]
    public void CreateRigidBody_IncrementsBodyCount()
    {
        var world = new World();
        var countBefore = world.RigidBodies.Count;
        world.CreateRigidBody();
        Assert.That(world.RigidBodies.Count, Is.EqualTo(countBefore + 1));
        world.Dispose();
    }

    [TestCase]
    public void Remove_DecrementsBodyCount()
    {
        var world = new World();
        var body = world.CreateRigidBody();
        var countAfterCreate = world.RigidBodies.Count;
        world.Remove(body);
        Assert.That(world.RigidBodies.Count, Is.EqualTo(countAfterCreate - 1));
        world.Dispose();
    }

    [TestCase]
    public void Clear_RemovesAllBodiesExceptNullBody()
    {
        var world = new World();
        for (int i = 0; i < 10; i++)
            world.CreateRigidBody();
        world.Clear();
        Assert.That(world.RigidBodies, Has.Count.EqualTo(1));
        Assert.That(world.RigidBodies, Does.Contain(world.NullBody));
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Step
    // -------------------------------------------------------------------------

    [TestCase]
    public void Step_NegativeDt_Throws()
    {
        var world = new World();
        Assert.Throws<ArgumentException>(() => world.Step(-1f / 60f, false));
        world.Dispose();
    }

    [TestCase]
    public void Step_ZeroDt_DoesNotThrow()
    {
        var world = new World();
        Assert.DoesNotThrow(() => world.Step(0f, false));
        world.Dispose();
    }

    [TestCase]
    public void Step_WithNoBody_DoesNotThrow()
    {
        var world = new World();
        Assert.DoesNotThrow(() => world.Step(1f / 60f, false));
        world.Dispose();
    }

    [TestCase]
    public void Stabilize_NegativeDt_Throws()
    {
        var world = new World();
        Assert.Throws<ArgumentException>(() => world.Stabilize(-1f / 60f, 1, 0, false));
        world.Dispose();
    }

    [TestCase]
    public void Stabilize_ZeroDt_DoesNotThrow()
    {
        var world = new World();
        Assert.DoesNotThrow(() => world.Stabilize(0f, 1, 0, false));
        world.Dispose();
    }

    [TestCase]
    public void Stabilize_SolverIterationsBelowOne_Throws()
    {
        var world = new World();
        Assert.Throws<ArgumentException>(() => world.Stabilize(1f / 60f, 0, 0, false));
        world.Dispose();
    }

    [TestCase]
    public void Stabilize_RelaxationIterationsBelowZero_Throws()
    {
        var world = new World();
        Assert.Throws<ArgumentException>(() => world.Stabilize(1f / 60f, 1, -1, false));
        world.Dispose();
    }

    [TestCase]
    public void Stabilize_WithConstraintError_SolvesWithoutChangingPositions()
    {
        var world = new World();
        world.AllowDeactivation = false;
        world.Gravity = JVector.Zero;

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));

        var socket = world.CreateConstraint<BallSocket>(bodyA, bodyB);
        socket.Initialize(JVector.Zero);

        bodyB.Position = new JVector(1, 0, 0);
        bodyA.Velocity = JVector.Zero;
        bodyB.Velocity = JVector.Zero;
        bodyA.AngularVelocity = JVector.Zero;
        bodyB.AngularVelocity = JVector.Zero;

        JVector positionA = bodyA.Position;
        JVector positionB = bodyB.Position;

        world.Stabilize(1f / 60f, 4, 2, false);

        Assert.That(bodyA.Position.X, Is.EqualTo(positionA.X).Within(1e-6f));
        Assert.That(bodyA.Position.Y, Is.EqualTo(positionA.Y).Within(1e-6f));
        Assert.That(bodyA.Position.Z, Is.EqualTo(positionA.Z).Within(1e-6f));

        Assert.That(bodyB.Position.X, Is.EqualTo(positionB.X).Within(1e-6f));
        Assert.That(bodyB.Position.Y, Is.EqualTo(positionB.Y).Within(1e-6f));
        Assert.That(bodyB.Position.Z, Is.EqualTo(positionB.Z).Within(1e-6f));
        Assert.That(bodyA.Velocity.LengthSquared() + bodyB.Velocity.LengthSquared(), Is.GreaterThan((Real)0.0));
        Assert.That(socket.Impulse.LengthSquared(), Is.GreaterThan((Real)0.0));

        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    [TestCase]
    public void PreStep_IsFiredOnStep()
    {
        var world = new World();
        bool fired = false;
        world.PreStep += _ => fired = true;
        world.Step(1f / 60f, false);
        Assert.That(fired, Is.True);
        world.Dispose();
    }

    [TestCase]
    public void PostStep_IsFiredOnStep()
    {
        var world = new World();
        bool fired = false;
        world.PostStep += _ => fired = true;
        world.Step(1f / 60f, false);
        Assert.That(fired, Is.True);
        world.Dispose();
    }

    [TestCase]
    public void PreStep_ReceivesCorrectDt()
    {
        var world = new World();
        Real receivedDt = 0;
        world.PreStep += dt => receivedDt = dt;
        const Real expectedDt = 1f / 60f;
        world.Step(expectedDt, false);
        Assert.That(receivedDt, Is.EqualTo(expectedDt).Within(1e-6f));
        world.Dispose();
    }

    [TestCase]
    public void Stabilize_DoesNotFireStepEvents()
    {
        var world = new World();
        bool preFired = false;
        bool postFired = false;

        world.PreStep += _ => preFired = true;
        world.PostStep += _ => postFired = true;

        world.Stabilize(1f / 60f, 1, 0, false);

        Assert.That(preFired, Is.False);
        Assert.That(postFired, Is.False);
        world.Dispose();
    }

    // -------------------------------------------------------------------------
    // Gravity effect on bodies
    // -------------------------------------------------------------------------

    [TestCase]
    public void Gravity_AffectsFallingBody()
    {
        var world = new World();
        world.AllowDeactivation = false;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        var startY = body.Position.Y;
        Helper.AdvanceWorld(world, 1, 1f / 100f, false);
        Assert.That(body.Position.Y, Is.LessThan(startY));
        world.Dispose();
    }

    [TestCase]
    public void Gravity_Zero_BodyDoesNotFall()
    {
        var world = new World();
        world.AllowDeactivation = false;
        world.Gravity = JVector.Zero;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        var startY = body.Position.Y;
        Helper.AdvanceWorld(world, 1, 1f / 100f, false);
        Assert.That(body.Position.Y, Is.EqualTo(startY).Within(1e-4f));
        world.Dispose();
    }

    [TestCase]
    public void AffectedByGravity_False_BodyDoesNotFall()
    {
        // Gravity is on, but the specific body opts out.
        var world = new World();
        world.AllowDeactivation = false;
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.AffectedByGravity = false;
        var startY = body.Position.Y;
        Helper.AdvanceWorld(world, 1, 1f / 100f, false);
        Assert.That(body.Position.Y, Is.EqualTo(startY).Within(1e-4f));
        world.Dispose();
    }

    [TestCase]
    public void AffectedByGravity_OnlyAffectsOptedOutBody()
    {
        // One body opts out; a second body far away still falls.
        var world = new World();
        world.AllowDeactivation = false;
        var floating = world.CreateRigidBody();
        floating.AddShape(new SphereShape(1));
        floating.AffectedByGravity = false;
        floating.Position = new JVector(0, 0, 0);
        var falling = world.CreateRigidBody();
        falling.AddShape(new SphereShape(1));
        falling.Position = new JVector(100, 0, 0);  // far away, no interaction
        var startY = floating.Position.Y;
        Helper.AdvanceWorld(world, 1, 1f / 100f, false);
        Assert.That(floating.Position.Y, Is.EqualTo(startY).Within(1e-4f));
        Assert.That(falling.Position.Y, Is.LessThan(startY));
        world.Dispose();
    }
}
