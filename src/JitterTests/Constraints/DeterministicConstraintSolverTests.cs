using Jitter2.Dynamics.Constraints;

namespace JitterTests.Constraints;

public class DeterministicConstraintSolverTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void DeterministicSingleThreadPathSolvesIslandsOnCallingThread(bool stabilize)
    {
        const int islandCount = 16;
        const int solverIterations = 2;

        var threadPool = Jitter2.Parallelization.ThreadPool.Instance;
        int previousThreadCount = threadPool.ThreadCount;
        threadPool.ChangeThreadCount(Math.Max(previousThreadCount, 2));

        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SolveMode = SolveMode.Deterministic,
            SubstepCount = 2,
            SolverIterations = (solverIterations, 0)
        };

        try
        {
            for (int i = 0; i < islandCount; i++)
            {
                CreateCountingConstraintIsland(world, i);
            }

            CountingConstraint.ResetCounters();
            CountingConstraint.ExpectCallbacksOnCurrentThread();

            if (stabilize)
            {
                world.Stabilize(1f / 60f, solverIterations, multiThread: false);
            }
            else
            {
                world.Step(1f / 60f, multiThread: false);
            }

            Assert.That(CountingConstraint.UnexpectedThreadCount, Is.EqualTo(0));
            Assert.That(CountingConstraint.PrepareCount, Is.EqualTo(world.SubstepCount * islandCount));
            Assert.That(CountingConstraint.IterateCount, Is.EqualTo(world.SubstepCount * islandCount * solverIterations));
        }
        finally
        {
            CountingConstraint.ResetCounters();
            world.Dispose();
            threadPool.ChangeThreadCount(previousThreadCount);
            threadPool.PauseWorkers();
        }
    }

    [TestCase]
    public void Stabilize_UsesRequestedIterationsInDeterministicSolver()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SolveMode = SolveMode.Deterministic,
            SubstepCount = 3
        };

        world.SolverIterations = (12, 6);

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));

        CountingConstraint.ResetCounters();
        world.CreateConstraint<CountingConstraint>(bodyA, bodyB);

        world.Stabilize(1f / 60f, solverIterations: 2, relaxationIterations: 0, multiThread: false);

        Assert.That(CountingConstraint.PrepareCount, Is.EqualTo(world.SubstepCount));
        Assert.That(CountingConstraint.IterateCount, Is.EqualTo(world.SubstepCount * 2));
        world.Dispose();
    }

    [TestCase]
    public void DistanceLimit_WorksInDeterministicSolver()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4,
            SolveMode = SolveMode.Deterministic
        };

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));
        bodyA.Position = new JVector(-3, 0, 0);

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(3, 0, 0);

        var limit = world.CreateConstraint<DistanceLimit>(bodyA, bodyB);
        limit.Initialize(bodyA.Position, bodyB.Position);
        limit.TargetDistance = (Real)2.0;

        Helper.AdvanceWorld(world, 2, 1f / 100f, true);

        Assert.That(limit.Distance, Is.EqualTo((Real)2.0).Within((Real)0.1));
        world.Dispose();
    }

    [TestCase]
    public void BallSocket_WorksInDeterministicSolver()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4,
            SolveMode = SolveMode.Deterministic
        };

        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));
        bodyA.Position = new JVector(0, 0, 0);

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector(3, 1, 0);

        var anchor = new JVector(1, 0, 0);
        var socket = world.CreateConstraint<BallSocket>(bodyA, bodyB);
        socket.Initialize(anchor);

        Helper.AdvanceWorld(world, 2, 1f / 100f, true);

        var delta = socket.Anchor2 - socket.Anchor1;
        Assert.That(delta.Length(), Is.LessThan((Real)0.1));
        world.Dispose();
    }

    [TestCase]
    public void FixedAngle_WorksInDeterministicSolver()
    {
        static Real Run(bool withConstraint)
        {
            var world = new World
            {
                Gravity = JVector.Zero,
                AllowDeactivation = false,
                SubstepCount = 4,
                SolveMode = SolveMode.Deterministic
            };

            var bodyA = world.CreateRigidBody();
            bodyA.AddShape(new SphereShape(1));

            var bodyB = world.CreateRigidBody();
            bodyB.AddShape(new SphereShape(1));
            bodyB.Orientation = JQuaternion.CreateRotationY((Real)(MathR.PI / 4.0));

            if (withConstraint)
            {
                var fixedAngle = world.CreateConstraint<FixedAngle>(bodyA, bodyB);
                fixedAngle.Initialize();
            }

            bodyB.AngularVelocity = new JVector(0, (Real)8.0, 0);
            Helper.AdvanceWorld(world, 1, 1f / 100f, true);

            var result = MathR.Abs(JQuaternion.Dot(bodyA.Orientation, bodyB.Orientation));
            world.Dispose();
            return result;
        }

        var unconstrainedDot = Run(false);
        var constrainedDot = Run(true);

        Assert.That(constrainedDot, Is.GreaterThan(unconstrainedDot));
    }

    [TestCase]
    public void LinearMotor_WorksInDeterministicSolver()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4,
            SolveMode = SolveMode.Deterministic
        };

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));

        var motor = world.CreateConstraint<LinearMotor>(world.NullBody, body);
        motor.Initialize(JVector.UnitX, JVector.UnitX);
        motor.TargetVelocity = (Real)2.0;
        motor.MaximumForce = (Real)100.0;

        Helper.AdvanceWorld(world, 1, 1f / 100f, true);

        Assert.That(body.Velocity.X, Is.EqualTo((Real)2.0).Within((Real)0.2));
        world.Dispose();
    }

    [TestCase]
    public void AngularMotor_WorksInDeterministicSolver()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4,
            SolveMode = SolveMode.Deterministic
        };

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));

        var motor = world.CreateConstraint<AngularMotor>(world.NullBody, body);
        motor.Initialize(JVector.UnitY, JVector.UnitY);
        motor.TargetVelocity = (Real)3.0;
        motor.MaximumTorque = (Real)100.0;

        Helper.AdvanceWorld(world, 1, 1f / 100f, true);

        Assert.That(body.AngularVelocity.Y, Is.EqualTo((Real)3.0).Within((Real)0.3));
        world.Dispose();
    }

    [TestCase]
    public void PointOnPlane_WorksInDeterministicSolver()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4,
            SolveMode = SolveMode.Deterministic
        };

        var planeBody = world.CreateRigidBody();
        planeBody.AddShape(new SphereShape(1));
        planeBody.MotionType = MotionType.Static;

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Position = new JVector(1, 3, 2);

        var pointOnPlane = world.CreateConstraint<PointOnPlane>(planeBody, body);
        pointOnPlane.Initialize(JVector.UnitY, JVector.Zero, body.Position, LinearLimit.Fixed);

        var before = MathR.Abs(body.Position.Y);
        Helper.AdvanceWorld(world, 2, 1f / 100f, true);
        var after = MathR.Abs(body.Position.Y);

        Assert.That(after, Is.LessThan(before));
        Assert.That(after, Is.LessThan((Real)0.2));
        world.Dispose();
    }

    [TestCase]
    public void PointOnLine_WorksInDeterministicSolver()
    {
        var world = new World
        {
            Gravity = JVector.Zero,
            AllowDeactivation = false,
            SubstepCount = 4,
            SolveMode = SolveMode.Deterministic
        };

        var lineBody = world.CreateRigidBody();
        lineBody.AddShape(new SphereShape(1));
        lineBody.MotionType = MotionType.Static;

        var body = world.CreateRigidBody();
        body.AddShape(new SphereShape(1));
        body.Position = new JVector(0, 3, 2);

        var pointOnLine = world.CreateConstraint<PointOnLine>(lineBody, body);
        pointOnLine.Initialize(JVector.UnitX, JVector.Zero, body.Position, LinearLimit.Full);

        var before = MathR.Sqrt(body.Position.Y * body.Position.Y + body.Position.Z * body.Position.Z);
        Helper.AdvanceWorld(world, 2, 1f / 100f, true);
        var after = MathR.Sqrt(body.Position.Y * body.Position.Y + body.Position.Z * body.Position.Z);

        Assert.That(after, Is.LessThan(before));
        Assert.That(after, Is.LessThan((Real)2.1));
        Assert.That(pointOnLine.Impulse.Length(), Is.GreaterThan((Real)0.0));
        world.Dispose();
    }

    [TestCase]
    public void HingeAngle_WorksInDeterministicSolver()
    {
        static Real Run(bool constrained)
        {
            var world = new World
            {
                Gravity = JVector.Zero,
                AllowDeactivation = false,
                SubstepCount = 4,
                SolveMode = SolveMode.Deterministic
            };

            var body = world.CreateRigidBody();
            body.AddShape(new SphereShape(1));

            if (constrained)
            {
                var hinge = world.CreateConstraint<HingeAngle>(world.NullBody, body);
                hinge.Initialize(JVector.UnitY, AngularLimit.FromDegree(-10, 10));
            }

            body.AngularVelocity = new JVector(0, (Real)8.0, 0);
            for (int i = 0; i < 10; i++)
            {
                world.Step(1f / 100f, true);
            }

            var clampedW = Math.Clamp(MathR.Abs(body.Orientation.W), (Real)0.0, (Real)1.0);
            var angle = (Real)2.0 * MathR.Acos(clampedW);
            world.Dispose();
            return angle;
        }

        var unconstrained = Run(false);
        var constrained = Run(true);

        Assert.That(unconstrained, Is.GreaterThan((Real)0.4));
        Assert.That(constrained, Is.LessThan(unconstrained));
        Assert.That(constrained, Is.LessThan((Real)0.4));
    }

    private static void CreateCountingConstraintIsland(World world, int index)
    {
        var bodyA = world.CreateRigidBody();
        bodyA.AddShape(new SphereShape(1));
        bodyA.Position = new JVector((Real)(index * 10.0), 0, 0);

        var bodyB = world.CreateRigidBody();
        bodyB.AddShape(new SphereShape(1));
        bodyB.Position = new JVector((Real)(index * 10.0 + 3.0), 0, 0);

        world.CreateConstraint<CountingConstraint>(bodyA, bodyB);
    }

    public unsafe class CountingConstraint : Constraint<CountingConstraint.CountingConstraintData>
    {
        public struct CountingConstraintData
        {
            public ConstraintData FullSizeMarker;
        }

        private static readonly uint RegisteredDispatchId =
            RegisterFullConstraint(&PrepareForIterationCountingConstraint, &IterateCountingConstraint);

        private static int prepareCount;
        private static int iterateCount;
        private static int unexpectedThreadCount;
        private static int expectedThreadId = -1;

        public static int PrepareCount => prepareCount;
        public static int IterateCount => iterateCount;
        public static int UnexpectedThreadCount => unexpectedThreadCount;

        public static void ResetCounters()
        {
            prepareCount = 0;
            iterateCount = 0;
            unexpectedThreadCount = 0;
            expectedThreadId = -1;
        }

        public static void ExpectCallbacksOnCurrentThread()
        {
            unexpectedThreadCount = 0;
            expectedThreadId = Environment.CurrentManagedThreadId;
        }

        protected override void Create()
        {
            base.Create();
            DispatchId = RegisteredDispatchId;
        }

        public static void PrepareForIterationCountingConstraint(ref ConstraintData constraint, Real idt)
        {
            RecordCallbackThread();
            System.Threading.Interlocked.Increment(ref prepareCount);
        }

        public static void IterateCountingConstraint(ref ConstraintData constraint, Real idt)
        {
            RecordCallbackThread();
            System.Threading.Interlocked.Increment(ref iterateCount);
        }

        private static void RecordCallbackThread()
        {
            if (expectedThreadId < 0) return;

            if (Environment.CurrentManagedThreadId != expectedThreadId)
            {
                System.Threading.Interlocked.Increment(ref unexpectedThreadCount);
                return;
            }

            System.Threading.Thread.Sleep(1);
        }
    }
}
