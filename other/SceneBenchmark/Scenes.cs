using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;

namespace SceneBenchmark;

// These scene definitions are identical for both libraries, including old tags.
public sealed class Scene : IDisposable
{
    public World World { get; }
    public RigidBody? Container { get; private set; }
    public List<BallSocket> Anchors { get; } = [];

    public Scene(string name, int iterations, int relaxations, int substeps)
    {
        World = new World
        {
            Gravity = new JVector(0, -9.81f, 0), AllowDeactivation = false,
            SolverIterations = (iterations, relaxations), SubstepCount = substeps
        };
        switch (name)
        {
            case "colosseum": Colosseum.Build(World); break;
            case "ragdolls":
                BuildContainer();
                // 10 x 5 x 10: 500 ten-body ragdolls, 10,000 constraints.
                for (int x = 0; x < 10; x++)
                for (int y = 0; y < 5; y++)
                for (int z = 0; z < 10; z++)
                    Anchors.AddRange(Ragdolls.BuildRagdoll(World,
                        new JVector(-18 + x * 4, -14 + y * 6, -18 + z * 4)));
                break;
            default: throw new ArgumentException($"Unknown scene: {name}");
        }
    }

    private void BuildContainer()
    {
        const float size = 50;
        Container = World.CreateRigidBody();
        Container.AddShapes([
            new TransformedShape(new BoxShape(size, 1, size), new JVector(0, size / 2, 0)),
            new TransformedShape(new BoxShape(size, 1, size), new JVector(0, -size / 2, 0)),
            new TransformedShape(new BoxShape(1, size, size), new JVector(size / 2, 0, 0)),
            new TransformedShape(new BoxShape(1, size, size), new JVector(-size / 2, 0, 0)),
            new TransformedShape(new BoxShape(size, size, 1), new JVector(0, 0, size / 2)),
            new TransformedShape(new BoxShape(size, size, 1), new JVector(0, 0, -size / 2))
        ]);
        Container.MotionType = MotionType.Kinematic;
        Container.DeactivationTime = TimeSpan.MaxValue;
    }

    public void BeforeStep()
    {
        if (Container != null) Container.AngularVelocity = new JVector(0.14f, 0.02f, 0.03f);
    }

    public (double rms, double max) AnchorError()
    {
        double sum = 0, max = 0;
        foreach (var anchor in Anchors)
        {
            double squared = (anchor.Anchor1 - anchor.Anchor2).LengthSquared();
            sum += squared;
            max = Math.Max(max, squared);
        }
        return (Anchors.Count == 0 ? 0 : Math.Sqrt(sum / Anchors.Count), Math.Sqrt(max));
    }

    public void Dispose() => World.Dispose();
}
