using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

namespace JitterDemo;

/// <summary>
/// Broad-phase filter for ropes built from <see cref="SoftBodySegment"/>s. Works like
/// <see cref="BroadPhaseCollisionFilter"/>, but also creates speculative contacts: contacts for
/// segments that are not touching yet but could within the next step. This keeps thin, fast ropes
/// from passing through each other and through rigid bodies.
/// </summary>
/// <remarks>
/// <para>
/// A separated pair gets a contact when its gap is smaller than the distance the two shapes could
/// cover relative to each other within a step (<see cref="SpeculativeFactor"/> times the full relative
/// speed times the step length), plus both segments' <see cref="SoftBodySegment.ContactMargin"/>.
/// The full relative speed is used, not only its component along the current normal, because ropes
/// wrap around each other within a step and the separating direction turns. The margin covers ropes
/// that move together and then stop abruptly within a step, for example when one snaps taut.
/// </para>
/// <para>
/// The solver lets a speculative contact close the gap but not overshoot it. Segment bounding boxes
/// must reach as far as this test does (<see cref="SoftBodySegment.SweepFactor"/>,
/// <see cref="SoftBodySegment.ContactMargin"/>), or the pairs are never seen.
/// </para>
/// </remarks>
public class RopeCollisionFilter : IBroadPhaseFilter
{
    private readonly World world;
    private readonly World.WorldStep onPreStep;
    private float stepDt = 0.01f;

    /// <summary>
    /// Gets or sets how many steps of relative motion the speculative test looks ahead. Should not
    /// exceed <see cref="SoftBodySegment.SweepFactor"/>. Default is 1.5.
    /// </summary>
    public float SpeculativeFactor { get; set; } = 1.5f;

    public RopeCollisionFilter(World world)
    {
        this.world = world;

        // The step length is needed to predict motion; PreStep runs just before collision detection.
        onPreStep = timeStep => stepDt = timeStep.StepDt;
        world.PreStep += onPreStep;
    }

    /// <summary>
    /// Unsubscribes from the world. Call this when the filter is no longer used.
    /// </summary>
    public void Detach() => world.PreStep -= onPreStep;

    public bool Filter(IDynamicTreeProxy proxyA, IDynamicTreeProxy proxyB)
    {
        SoftBodyShape? softShapeA = proxyA as SoftBodyShape;
        SoftBodyShape? softShapeB = proxyB as SoftBodyShape;

        if (softShapeA != null && softShapeB != null)
        {
            if (softShapeB.ShapeId < softShapeA.ShapeId)
            {
                (softShapeA, softShapeB) = (softShapeB, softShapeA);
            }

            if (!softShapeA.SoftBody.IsActive && !softShapeB.SoftBody.IsActive) return false;

            bool colliding = NarrowPhase.MprEpa(softShapeA, softShapeB,
                JQuaternion.Identity, JVector.Zero,
                out JVector pA, out JVector pB, out JVector normal, out _);

            if (!colliding && !IsSpeculative(softShapeA, softShapeB, JQuaternion.Identity, JVector.Zero,
                    softShapeA.Velocity, softShapeB.Velocity, null,
                    out pA, out pB, out normal)) return false;

            var closestA = softShapeA.GetClosest(pA);
            var closestB = softShapeB.GetClosest(pB);

            world.RegisterContact(closestA.RigidBodyId, closestB.RigidBodyId, closestA, closestB,
                pA, pB, normal);

            return false;
        }

        if (softShapeA != null) return CollideSoftRigid(softShapeA, (proxyB as RigidBodyShape)!);
        if (softShapeB != null) return CollideSoftRigid(softShapeB, (proxyA as RigidBodyShape)!);

        return true;
    }

    private bool CollideSoftRigid(SoftBodyShape softShape, RigidBodyShape rigidShape)
    {
        var rigidBody = rigidShape.RigidBody;

        if (!softShape.SoftBody.IsActive && !rigidBody.Data.IsActive) return false;

        bool colliding = NarrowPhase.MprEpa(softShape, rigidShape, rigidBody.Orientation, rigidBody.Position,
            out JVector pA, out JVector pB, out JVector normal, out _);

        if (!colliding && !IsSpeculative(softShape, rigidShape, rigidBody.Orientation, rigidBody.Position,
                softShape.Velocity, rigidBody.Velocity, rigidBody,
                out pA, out pB, out normal)) return false;

        var closest = softShape.GetClosest(pA);

        world.RegisterContact(closest.RigidBodyId, rigidBody.RigidBodyId, closest, rigidBody,
            pA, pB, normal, ContactData.SolveMode.AngularBody1);

        return false;
    }

    private static float Margin(Shape shape) => shape is SoftBodySegment segment ? segment.ContactMargin : 0.0f;

    /// <summary>
    /// For separated shapes, checks whether they could touch within the next step. Shape A is given
    /// in world space, shape B by its world orientation and position.
    /// </summary>
    private bool IsSpeculative(SoftBodyShape shapeA, Shape shapeB, in JQuaternion orientationB,
        in JVector positionB, in JVector velocityA, in JVector velocityB, RigidBody? rigidB,
        out JVector pA, out JVector pB, out JVector normal)
    {
        // Only ropes get speculative contacts; other soft bodies behave as with BroadPhaseCollisionFilter.
        pA = pB = normal = JVector.Zero;
        if (shapeA is not SoftBodySegment && shapeB is not SoftBodySegment) return false;

        if (!NarrowPhase.Distance(shapeA, shapeB, orientationB, positionB,
                out pA, out pB, out normal, out float distance)) return false;

        JVector vB = velocityB;
        if (rigidB != null) vB += rigidB.AngularVelocity % (pB - rigidB.Position);

        float reach = (vB - velocityA).Length() * stepDt * SpeculativeFactor;
        return distance < reach + Margin(shapeA) + Margin(shapeB);
    }
}
