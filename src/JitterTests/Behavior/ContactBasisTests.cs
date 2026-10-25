#if USE_DOUBLE_PRECISION
using Vector = System.Runtime.Intrinsics.Vector256;
#else
using Vector = System.Runtime.Intrinsics.Vector128;
#endif

namespace JitterTests.Behavior;

public class ContactBasisTests
{
#if USE_DOUBLE_PRECISION
    private const Real Tolerance = 1e-12;
#else
    private const Real Tolerance = 1e-5f;
#endif

    [TestCase(false)]
    [TestCase(true)]
    public void RefreshedContact_PreservesWarmStartImpulseInNewBasis(bool accelerated)
    {
        using var world = new World();
        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape((Real)0.5));
        body.SetMassInertia(1);
        body.Velocity = JVector.UnitX;
        ContactData contact = default;
        contact.Init(world.NullBody, body);
        contact.ResetMode();
        contact.Friction = (Real)0.5;
        contact.AddContact(JVector.Zero, JVector.Zero, JVector.UnitY);
        contact.Contact0.Flag = 0;
        contact.Contact0.Accumulated = Vector.Create((Real)8, (Real)0.5, (Real)(-0.25), (Real)0);
        JVector expectedImpulse = WorldImpulse(contact, contact.Contact0);
        var normal = JVector.Normalize(new JVector((Real)0.2, 1, (Real)0.1));

        contact.AddContact(JVector.Zero, JVector.Zero, normal);

        AssertBasis(contact, normal);
        Assert.That((WorldImpulse(contact, contact.Contact0) - expectedImpulse).Length(), Is.LessThan(Tolerance));
        Assert.That(contact.Contact0.Flag, Is.EqualTo((ContactData.Contact.Flags)0),
            "Refreshing a contact must not create a new impact.");
        body.Velocity = JVector.Zero;
        if (accelerated) contact.PrepareForIterationAccelerated(new TimeStep((Real)0.01));
        else contact.PrepareForIterationScalar(new TimeStep((Real)0.01));
        Assert.That((body.Velocity - expectedImpulse).Length(), Is.LessThan(Tolerance));
    }

    [Test]
    public void RefreshedContact_DiscardsWarmStartThatOpposesNewNormal()
    {
        using var world = new World();
        var body = world.CreateRigidBody();
        ContactData contact = default;
        contact.Init(world.NullBody, body);
        contact.AddContact(JVector.Zero, JVector.Zero, JVector.UnitY);
        contact.Contact0.Flag = 0;
        contact.Contact0.Accumulated = Vector.Create((Real)2, (Real)0, (Real)0, (Real)0);

        contact.AddContact(JVector.Zero, JVector.Zero, -JVector.UnitY);

        AssertBasis(contact, -JVector.UnitY);
        Assert.That(contact.Contact0.Impulse, Is.Zero);
        Assert.That(contact.Contact0.TangentImpulse1, Is.Zero);
        Assert.That(contact.Contact0.TangentImpulse2, Is.Zero);
        Assert.That(contact.Contact0.Flag, Is.EqualTo((ContactData.Contact.Flags)0));
    }

    [Test]
    public void RefreshedContact_HandlesParallelOldTangentAndClampsFriction()
    {
        using var world = new World();
        var body = world.CreateRigidBody();
        body.Velocity = JVector.UnitX;
        ContactData contact = default;
        contact.Init(world.NullBody, body);
        contact.Friction = (Real)0.5;
        contact.AddContact(JVector.Zero, JVector.Zero, JVector.UnitY);
        contact.Contact0.Accumulated = Vector.Create((Real)2, (Real)0.5, (Real)0.25, (Real)0);
        JVector normal = contact.Tangent1;

        contact.AddContact(JVector.Zero, JVector.Zero, normal);

        AssertBasis(contact, normal);
        Assert.That(contact.Contact0.Impulse, Is.EqualTo((Real)0.5).Within(Tolerance));
        Real limit = contact.Friction * contact.Contact0.Impulse;
        Assert.That(MathR.Abs(contact.Contact0.TangentImpulse1), Is.LessThanOrEqualTo(limit + Tolerance));
        Assert.That(MathR.Abs(contact.Contact0.TangentImpulse2), Is.LessThanOrEqualTo(limit + Tolerance));
    }

    [Test]
    public void RefreshedContact_UnchangedNormalPreservesBasisAndCachedState()
    {
        using var world = new World();
        var body = world.CreateRigidBody();
        body.Velocity = JVector.UnitX;
        ContactData contact = default;
        contact.Init(world.NullBody, body);
        contact.AddContact(JVector.Zero, JVector.Zero, JVector.UnitY);
        contact.Contact0.Accumulated = Vector.Create((Real)2, (Real)0.5, (Real)(-0.25), (Real)0);
        contact.Contact0.Flag = ContactData.Contact.Flags.SkipWarmStart;
        contact.Contact0.Bias = (Real)0.75;
        ContactData.Contact previous = contact.Contact0;
        JVector previousTangent1 = contact.Tangent1, previousTangent2 = contact.Tangent2;
        body.Velocity = JVector.UnitZ;
        var point = new JVector((Real)0.001, 0, 0);

        contact.AddContact(point, point, JVector.UnitY);

        Assert.That(contact.Tangent1, Is.EqualTo(previousTangent1));
        Assert.That(contact.Tangent2, Is.EqualTo(previousTangent2));
        Assert.That(contact.Contact0.Accumulated, Is.EqualTo(previous.Accumulated));
        Assert.That(contact.Contact0.Flag, Is.EqualTo(previous.Flag));
        Assert.That(contact.Contact0.Bias, Is.EqualTo(previous.Bias));
        Assert.That(contact.Contact0.GetRelativePosition2(contact), Is.EqualTo(point));
    }

    [TestCase(1)]
    [TestCase(4)]
    public void CachedContact_UsesFreshNarrowPhaseNormalForRotatingSurface(int substeps)
    {
        using var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            EnableAuxiliaryContactPoints = false,
            SubstepCount = substeps
        };
        var floor = world.CreateRigidBody();
        var floorShape = new BoxShape(4, 1, 4);
        floor.AddShape(floorShape);
        floor.Position = new JVector(0, (Real)(-0.5), 0);
        floor.MotionType = MotionType.Kinematic;
        floor.AngularVelocity = new JVector(0, 0, (Real)0.2);
        var sphere = world.CreateRigidBody();
        var sphereShape = new SphereShape((Real)0.5);
        sphere.AddShape(sphereShape);
        sphere.Position = new JVector(0, (Real)0.499, 0);
        floor.Friction = sphere.Friction = 0;
        var filter = new RecordNormal(floor);
        world.NarrowPhaseFilter = filter;
        int checks = 0;
        world.PostSubStep += _ =>
        {
            Assert.That(world.GetArbiter(floorShape.ShapeId, sphereShape.ShapeId, out var arbiter), Is.True);
            JVector expected = arbiter!.Body1 == floor ? filter.Normal : -filter.Normal;
            AssertBasis(arbiter.Handle.Data, expected);
            checks++;
        };

        for (int i = 0; i < 20; i++) world.Step((Real)0.01, false);

        Assert.That(filter.Calls, Is.EqualTo(20));
        Assert.That(checks, Is.EqualTo(20 * substeps));
        Assert.That((filter.Normal - JVector.UnitY).Length(), Is.GreaterThan((Real)0.01));
    }

    private static JVector WorldImpulse(in ContactData manifold, in ContactData.Contact contact) =>
        contact.Impulse * manifold.Normal + contact.TangentImpulse1 * manifold.Tangent1
        + contact.TangentImpulse2 * manifold.Tangent2;

    private static void AssertBasis(in ContactData manifold, in JVector normal)
    {
        Assert.That((manifold.Normal - normal).Length(), Is.LessThan(Tolerance));
        Assert.That(manifold.Tangent1.LengthSquared(), Is.EqualTo((Real)1).Within(Tolerance));
        Assert.That(manifold.Tangent2.LengthSquared(), Is.EqualTo((Real)1).Within(Tolerance));
        Assert.That(JVector.Dot(manifold.Normal, manifold.Tangent1), Is.Zero.Within(Tolerance));
        Assert.That(JVector.Dot(manifold.Normal, manifold.Tangent2), Is.Zero.Within(Tolerance));
        Assert.That(JVector.Dot(manifold.Tangent1, manifold.Tangent2), Is.Zero.Within(Tolerance));
    }

    private sealed class RecordNormal(RigidBody floor) : INarrowPhaseFilter
    {
        public JVector Normal;
        public int Calls;

        public bool Filter(RigidBodyShape shapeA, RigidBodyShape shapeB, ref JVector pointA,
            ref JVector pointB, ref JVector normal, ref Real penetration)
        {
            Normal = shapeA.RigidBody == floor ? normal : -normal;
            Calls++;
            return true;
        }
    }
}
