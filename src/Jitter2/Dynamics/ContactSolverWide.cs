/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Jitter2.LinearMath;
using WideMath = System.Numerics.Vector;
#if USE_DOUBLE_PRECISION
using Wide = System.Numerics.Vector<double>;
#else
using Wide = System.Numerics.Vector<float>;
#endif

namespace Jitter2.Dynamics;

/// <summary>
/// A reusable structure-of-arrays bundle of contact manifolds that share no body which any of
/// them writes. One SIMD lane holds one manifold; its contact points keep their sequential
/// Gauss-Seidel order.
/// </summary>
/// <remarks>
/// The bundle is a transient copy. <see cref="Prepare"/> gathers it from the contact records,
/// <see cref="Iterate"/> solves it against the live body velocities, and <see cref="WriteBack"/>
/// publishes the accumulated impulses for the next warm start. The caller must keep the
/// contact and body buffers in place between those calls.
/// </remarks>
internal sealed unsafe class ContactSolverWide
{
    internal static int Width => Wide.Count;

    internal int Count { get; private set; }

    private readonly ContactData*[] contacts = new ContactData*[Width];
    private readonly RigidBodyData*[] bodies1 = new RigidBodyData*[Width];
    private readonly RigidBodyData*[] bodies2 = new RigidBodyData*[Width];
    private readonly ContactData.SolveMode[] modes = new ContactData.SolveMode[Width];
    private readonly uint[] usageMasks = new uint[Width];
    private readonly ContactWide[] points = new ContactWide[4];
    private Vector3Wide inverseMass1, inverseMass2;
    private SymmetricMatrixWide inverseInertia1, inverseInertia2;
    private Wide friction;
    private uint occupiedSlots;

    private struct Vector3Wide
    {
        internal Wide X, Y, Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetLane(int lane, in JVector value)
        {
            ContactSolverWide.SetLane(ref X, lane, value.X);
            ContactSolverWide.SetLane(ref Y, lane, value.Y);
            ContactSolverWide.SetLane(ref Z, lane, value.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly JVector GetLane(int lane) => new(X[lane], Y[lane], Z[lane]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Wide operator +(Vector3Wide a, Vector3Wide b) =>
            new() { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Wide operator -(Vector3Wide a, Vector3Wide b) =>
            new() { X = a.X - b.X, Y = a.Y - b.Y, Z = a.Z - b.Z };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3Wide operator *(Vector3Wide a, Wide scale) =>
            new() { X = a.X * scale, Y = a.Y * scale, Z = a.Z * scale };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3Wide Multiply(in Vector3Wide a, in Vector3Wide b) =>
            new() { X = a.X * b.X, Y = a.Y * b.Y, Z = a.Z * b.Z };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Wide Dot(in Vector3Wide a, in Vector3Wide b) =>
            (a.X * b.X + a.Y * b.Y) + a.Z * b.Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3Wide Cross(in Vector3Wide a, in Vector3Wide b) =>
            new()
            {
                X = a.Y * b.Z - a.Z * b.Y,
                Y = a.Z * b.X - a.X * b.Z,
                Z = a.X * b.Y - a.Y * b.X
            };
    }

    private struct SymmetricMatrixWide
    {
        internal Wide M11, M21, M31, M22, M23, M33;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetLane(int lane, in JSymmetricMatrix matrix)
        {
            ContactSolverWide.SetLane(ref M11, lane, matrix.M11);
            ContactSolverWide.SetLane(ref M21, lane, matrix.M21);
            ContactSolverWide.SetLane(ref M31, lane, matrix.M31);
            ContactSolverWide.SetLane(ref M22, lane, matrix.M22);
            ContactSolverWide.SetLane(ref M23, lane, matrix.M23);
            ContactSolverWide.SetLane(ref M33, lane, matrix.M33);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly Vector3Wide Transform(in Vector3Wide value) => new()
        {
            X = (M11 * value.X + M21 * value.Y) + M31 * value.Z,
            Y = (M21 * value.X + M22 * value.Y) + M23 * value.Z,
            Z = (M31 * value.X + M23 * value.Y) + M33 * value.Z
        };
    }

    private struct QuaternionWide
    {
        private Vector3Wide vector;
        private Wide scalar;

        internal void SetLane(int lane, in JQuaternion value)
        {
            vector.SetLane(lane, new JVector(value.X, value.Y, value.Z));
            ContactSolverWide.SetLane(ref scalar, lane, value.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly Vector3Wide Transform(in Vector3Wide value)
        {
            // Keep the same operation order as JVector.Transform.
            Vector3Wide twiceCross = Vector3Wide.Cross(vector, value) * new Wide(2);
            return (value + twiceCross * scalar) + Vector3Wide.Cross(vector, twiceCross);
        }
    }

    // Per-point state. Masses and inertia of the bodies are shared by the points of a lane.
    private struct ContactWide
    {
        internal Vector3Wide Normal, Tangent1;
        internal Vector3Wide RelativePosition1, RelativePosition2;
        internal Wide MassNormal, MassTangent1, MassTangent2;
        internal Wide AccumulatedNormal, AccumulatedTangent1, AccumulatedTangent2;
        internal Wide Bias, BiasedTarget;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetLane(ref Wide vector, int lane, Real value)
    {
        Unsafe.Add(ref Unsafe.As<Wide, Real>(ref vector), lane) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    /// <summary>
    /// Gathers the given contact records as the lanes of this bundle, computes their effective
    /// masses and biases, and replays the accumulated impulses into the body velocities.
    /// Matches <see cref="ContactData.PrepareForIteration"/> for each lane.
    /// </summary>
    /// <param name="records">Up to <see cref="Width"/> consecutive contact records.</param>
    /// <param name="timeStep">The full step and substep timing information.</param>
    internal void Prepare(Span<ContactData> records, in TimeStep timeStep)
    {
        if (records.Length > Width) throw new ArgumentOutOfRangeException(nameof(records));
        Count = records.Length;

        occupiedSlots = 0;
        Array.Clear(points);
        inverseMass1 = inverseMass2 = default;
        inverseInertia1 = inverseInertia2 = default;
        friction = default;

        Vector3Wide position1 = default, position2 = default;
        Vector3Wide velocity1 = default, angular1 = default, velocity2 = default, angular2 = default;
        QuaternionWide orientation1 = default, orientation2 = default;
        Wide linearLocks = default, ccdRelaxation = default, breakThreshold = default;
        Wide biasFactor = default, allowedPenetration = default, maximumBias = default;
        ContactData.SolveMode combinedMode = ContactData.SolveMode.None;
        bool anyLinearLocks = false;

        for (int lane = 0; lane < Count; lane++)
        {
            ContactData* data = (ContactData*)Unsafe.AsPointer(ref records[lane]);
            RigidBodyData* body1 = (RigidBodyData*)Unsafe.AsPointer(ref data->Body1.Data);
            RigidBodyData* body2 = (RigidBodyData*)Unsafe.AsPointer(ref data->Body2.Data);
            ContactData.SolveMode mode = data->Mode;
            contacts[lane] = data;
            bodies1[lane] = body1;
            bodies2[lane] = body2;
            modes[lane] = mode;
            usageMasks[lane] = data->UsageMask & ContactData.MaskContactAll;
            occupiedSlots |= usageMasks[lane];
            combinedMode |= mode;

            SetLane(ref friction, lane, data->Friction);
            if ((mode & ContactData.SolveMode.LinearBody1) != 0) inverseMass1.SetLane(lane, body1->InverseMassVector);
            if ((mode & ContactData.SolveMode.LinearBody2) != 0) inverseMass2.SetLane(lane, body2->InverseMassVector);
            if ((mode & ContactData.SolveMode.AngularBody1) != 0) inverseInertia1.SetLane(lane, body1->InverseInertiaWorld);
            if ((mode & ContactData.SolveMode.AngularBody2) != 0) inverseInertia2.SetLane(lane, body2->InverseInertiaWorld);

            position1.SetLane(lane, body1->Position);
            position2.SetLane(lane, body2->Position);
            orientation1.SetLane(lane, body1->Orientation);
            orientation2.SetLane(lane, body2->Orientation);
            velocity1.SetLane(lane, body1->Velocity);
            angular1.SetLane(lane, body1->AngularVelocity);
            velocity2.SetLane(lane, body2->Velocity);
            angular2.SetLane(lane, body2->AngularVelocity);

            bool hasLinearLocks = body1->HasLinearLocks || body2->HasLinearLocks;
            SetLane(ref linearLocks, lane, hasLinearLocks ? 1 : 0);
            anyLinearLocks |= hasLinearLocks;

            ContactSettings* settings = data->settings;
            SetLane(ref ccdRelaxation, lane, settings->CcdRelaxationFactor);
            SetLane(ref breakThreshold, lane, settings->BreakThreshold);
            SetLane(ref biasFactor, lane, settings->BiasFactor * timeStep.InverseStepDt);
            SetLane(ref allowedPenetration, lane, settings->AllowedPenetration);
            SetLane(ref maximumBias, lane, settings->MaximumBias);
        }

        Vector3Wide inverseMassSum = inverseMass1 + inverseMass2;
        var lockedLanes = WideMath.GreaterThan(linearLocks, Wide.Zero);
        Wide inverseStepDt = new(timeStep.InverseStepDt);

        for (int slot = 0; slot < 4; slot++)
        {
            if ((occupiedSlots & (1u << slot)) == 0) continue;
            ref ContactWide point = ref points[slot];
            Vector3Wide localPosition1 = default, localPosition2 = default;
            Wide active = default, warmNormal = default, warmTangent1 = default, warmTangent2 = default;

            for (int lane = 0; lane < Count; lane++)
            {
                if ((usageMasks[lane] & (1u << slot)) == 0) continue;
                ref ContactData.Contact contact = ref GetContact(ref *contacts[lane], slot);
                point.Normal.SetLane(lane, contact.Normal);
                point.Tangent1.SetLane(lane, contact.Tangent1);
                point.RelativePosition1.SetLane(lane, contact.RelativePosition1);
                point.RelativePosition2.SetLane(lane, contact.RelativePosition2);
                localPosition1.SetLane(lane, contact.Position1);
                localPosition2.SetLane(lane, contact.Position2);
                Real accumulatedNormal = contact.Accumulated.GetElement(0);
                Real accumulatedTangent1 = contact.Accumulated.GetElement(1);
                Real accumulatedTangent2 = contact.Accumulated.GetElement(2);
                SetLane(ref point.AccumulatedNormal, lane, accumulatedNormal);
                SetLane(ref point.AccumulatedTangent1, lane, accumulatedTangent1);
                SetLane(ref point.AccumulatedTangent2, lane, accumulatedTangent2);
                SetLane(ref active, lane, 1);

                ContactData.Contact.Flags flags = contact.Flag;
                // Only a contact created in this step carries a restitution bias.
                if ((flags & ContactData.Contact.Flags.NewContact) != 0) SetLane(ref point.Bias, lane, contact.Bias);
                if ((flags & ContactData.Contact.Flags.SkipWarmStart) == 0)
                {
                    SetLane(ref warmNormal, lane, accumulatedNormal);
                    SetLane(ref warmTangent1, lane, accumulatedTangent1);
                    SetLane(ref warmTangent2, lane, accumulatedTangent2);
                }

                const ContactData.Contact.Flags consumed =
                    ContactData.Contact.Flags.NewContact | ContactData.Contact.Flags.SkipWarmStart;
                if ((flags & consumed) != 0) contact.Flag = flags & ~consumed;
            }

            // The second tangent is derived wherever it is needed instead of being stored: the
            // sweeps are bound by the bytes they read, not by their arithmetic.
            Vector3Wide tangent2 = Vector3Wide.Cross(point.Tangent1, point.Normal);

            // Impulse offsets stay fixed for the whole step. Only the separation follows the
            // current poses during substeps.
            Vector3Wide current1 = orientation1.Transform(localPosition1) + position1;
            Vector3Wide current2 = orientation2.Transform(localPosition2) + position2;
            Wide penetration = Vector3Wide.Dot(current1 - current2, point.Normal);
            point.Bias = WideMath.ConditionalSelect(WideMath.LessThan(penetration, -breakThreshold),
                (penetration * inverseStepDt) * ccdRelaxation, point.Bias);
            Wide penaltyBias = WideMath.Min(
                biasFactor * WideMath.Max(Wide.Zero, penetration - allowedPenetration), maximumBias);
            point.BiasedTarget = WideMath.ConditionalSelect(WideMath.GreaterThan(penaltyBias, Wide.Zero),
                WideMath.Max(penaltyBias, point.Bias), point.Bias);

            // The scalar preparation uses the scalar inverse mass when no axis is locked.
            Wide kNormal = inverseMassSum.X, kTangent1 = inverseMassSum.X, kTangent2 = inverseMassSum.X;
            if (anyLinearLocks)
            {
                kNormal = WideMath.ConditionalSelect(lockedLanes,
                    Vector3Wide.Dot(Vector3Wide.Multiply(inverseMassSum, point.Normal), point.Normal), kNormal);
                kTangent1 = WideMath.ConditionalSelect(lockedLanes,
                    Vector3Wide.Dot(Vector3Wide.Multiply(inverseMassSum, point.Tangent1), point.Tangent1), kTangent1);
                kTangent2 = WideMath.ConditionalSelect(lockedLanes,
                    Vector3Wide.Dot(Vector3Wide.Multiply(inverseMassSum, tangent2), tangent2), kTangent2);
            }

            Vector3Wide warmAngular1 = default, warmAngular2 = default;
            // Ground contacts often share a read-only side across the whole bundle: skip it.
            if ((combinedMode & ContactData.SolveMode.AngularBody1) != 0)
            {
                PrepareAngularSide(inverseInertia1, point.RelativePosition1, point, tangent2, warmNormal, warmTangent1,
                    warmTangent2, ref kNormal, ref kTangent1, ref kTangent2, out warmAngular1);
            }

            if ((combinedMode & ContactData.SolveMode.AngularBody2) != 0)
            {
                PrepareAngularSide(inverseInertia2, point.RelativePosition2, point, tangent2, warmNormal, warmTangent1,
                    warmTangent2, ref kNormal, ref kTangent1, ref kTangent2, out warmAngular2);
            }

            // An absent contact must stay a neutral lane even when its bodies are dynamic.
            var activeLanes = WideMath.GreaterThan(active, Wide.Zero);
            point.MassNormal = WideMath.ConditionalSelect(activeLanes & WideMath.GreaterThan(kNormal, Wide.Zero),
                Wide.One / kNormal, Wide.Zero);
            point.MassTangent1 = WideMath.ConditionalSelect(activeLanes & WideMath.GreaterThan(kTangent1, Wide.Zero),
                Wide.One / kTangent1, Wide.Zero);
            point.MassTangent2 = WideMath.ConditionalSelect(activeLanes & WideMath.GreaterThan(kTangent2, Wide.Zero),
                Wide.One / kTangent2, Wide.Zero);

            Vector3Wide impulse = (point.Normal * warmNormal + point.Tangent1 * warmTangent1) +
                                  tangent2 * warmTangent2;
            velocity1 -= Vector3Wide.Multiply(impulse, inverseMass1);
            velocity2 += Vector3Wide.Multiply(impulse, inverseMass2);
            angular1 -= warmAngular1;
            angular2 += warmAngular2;

            for (int lane = 0; lane < Count; lane++)
            {
                if ((usageMasks[lane] & (1u << slot)) == 0) continue;
                ref ContactData.Contact contact = ref GetContact(ref *contacts[lane], slot);
                contact.Bias = point.Bias[lane];
                contact.PenaltyBias = penaltyBias[lane];
            }
        }

        StoreVelocities(velocity1, angular1, velocity2, angular2);
    }

    // Adds one body's angular share to the effective masses and returns its warm-start change.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrepareAngularSide(in SymmetricMatrixWide inertia, in Vector3Wide position,
        in ContactWide point, in Vector3Wide tangent2, Wide warmNormal, Wide warmTangent1, Wide warmTangent2,
        ref Wide kNormal, ref Wide kTangent1, ref Wide kTangent2, out Vector3Wide warmAngular)
    {
        Vector3Wide response = inertia.Transform(Vector3Wide.Cross(position, point.Normal));
        kNormal += Vector3Wide.Dot(point.Normal, Vector3Wide.Cross(response, position));
        warmAngular = response * warmNormal;

        response = inertia.Transform(Vector3Wide.Cross(position, point.Tangent1));
        kTangent1 += Vector3Wide.Dot(point.Tangent1, Vector3Wide.Cross(response, position));
        warmAngular += response * warmTangent1;

        response = inertia.Transform(Vector3Wide.Cross(position, tangent2));
        kTangent2 += Vector3Wide.Dot(tangent2, Vector3Wide.Cross(response, position));
        warmAngular += response * warmTangent2;
    }

    /// <summary>
    /// Runs one solver iteration on the live body velocities. Matches
    /// <see cref="ContactData.Iterate"/> for each lane.
    /// </summary>
    /// <param name="applyBias">Whether to apply the position-correction bias.</param>
    /// <param name="writeBack">Whether to publish the accumulated impulses afterwards.</param>
    internal void Iterate(bool applyBias, bool writeBack = false)
    {
        Vector3Wide velocity1 = default, angular1 = default, velocity2 = default, angular2 = default;
        for (int lane = 0; lane < Count; lane++)
        {
            RigidBodyData* body1 = bodies1[lane];
            RigidBodyData* body2 = bodies2[lane];
            // Both bodies contribute even when one is read-only. Linear-only contacts omit rotation.
            velocity1.SetLane(lane, body1->Velocity);
            velocity2.SetLane(lane, body2->Velocity);
            if ((modes[lane] & ContactData.SolveMode.Angular) != 0)
            {
                angular1.SetLane(lane, body1->AngularVelocity);
                angular2.SetLane(lane, body2->AngularVelocity);
            }
        }

        for (int slot = 0; slot < 4; slot++)
        {
            if ((occupiedSlots & (1u << slot)) == 0) continue;
            ref ContactWide point = ref points[slot];
            Vector3Wide tangent2 = Vector3Wide.Cross(point.Tangent1, point.Normal);
            Vector3Wide relative = (velocity2 + Vector3Wide.Cross(angular2, point.RelativePosition2)) -
                                   (velocity1 + Vector3Wide.Cross(angular1, point.RelativePosition1));
            Wide bias = applyBias ? point.BiasedTarget : point.Bias;
            Wide normalImpulse = point.MassNormal * (bias - Vector3Wide.Dot(point.Normal, relative));
            Wide tangentImpulse1 = point.MassTangent1 * -Vector3Wide.Dot(point.Tangent1, relative);
            Wide tangentImpulse2 = point.MassTangent2 * -Vector3Wide.Dot(tangent2, relative);

            // Friction is bounded by the normal impulse from before this update, as in the scalar solver.
            Wide maximumFriction = friction * point.AccumulatedNormal;
            Wide accumulatedNormal = WideMath.Max(point.AccumulatedNormal + normalImpulse, Wide.Zero);
            Wide accumulatedTangent1 = WideMath.Min(WideMath.Max(point.AccumulatedTangent1 + tangentImpulse1,
                -maximumFriction), maximumFriction);
            Wide accumulatedTangent2 = WideMath.Min(WideMath.Max(point.AccumulatedTangent2 + tangentImpulse2,
                -maximumFriction), maximumFriction);
            normalImpulse = accumulatedNormal - point.AccumulatedNormal;
            tangentImpulse1 = accumulatedTangent1 - point.AccumulatedTangent1;
            tangentImpulse2 = accumulatedTangent2 - point.AccumulatedTangent2;
            point.AccumulatedNormal = accumulatedNormal;
            point.AccumulatedTangent1 = accumulatedTangent1;
            point.AccumulatedTangent2 = accumulatedTangent2;

            Vector3Wide impulse = (point.Normal * normalImpulse + point.Tangent1 * tangentImpulse1) +
                                  tangent2 * tangentImpulse2;
            velocity1 -= Vector3Wide.Multiply(impulse, inverseMass1);
            velocity2 += Vector3Wide.Multiply(impulse, inverseMass2);
            angular1 -= inverseInertia1.Transform(Vector3Wide.Cross(point.RelativePosition1, impulse));
            angular2 += inverseInertia2.Transform(Vector3Wide.Cross(point.RelativePosition2, impulse));
        }

        StoreVelocities(velocity1, angular1, velocity2, angular2);
        if (writeBack) WriteBack();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StoreVelocities(in Vector3Wide velocity1, in Vector3Wide angular1,
        in Vector3Wide velocity2, in Vector3Wide angular2)
    {
        for (int lane = 0; lane < Count; lane++)
        {
            // A read-only body may appear in many bundles solved at the same time. Never write it,
            // not even its unchanged value: the store would race with the other readers.
            ContactData.SolveMode mode = modes[lane];
            RigidBodyData* body1 = bodies1[lane];
            RigidBodyData* body2 = bodies2[lane];
            if ((mode & ContactData.SolveMode.LinearBody1) != 0) body1->Velocity = velocity1.GetLane(lane);
            if ((mode & ContactData.SolveMode.AngularBody1) != 0) body1->AngularVelocity = angular1.GetLane(lane);
            if ((mode & ContactData.SolveMode.LinearBody2) != 0) body2->Velocity = velocity2.GetLane(lane);
            if ((mode & ContactData.SolveMode.AngularBody2) != 0) body2->AngularVelocity = angular2.GetLane(lane);
        }
    }

    /// <summary>Publishes the accumulated impulses to the contact records for the next warm start.</summary>
    internal void WriteBack()
    {
        for (int lane = 0; lane < Count; lane++)
        {
            ref ContactData data = ref *contacts[lane];
            for (int slot = 0; slot < 4; slot++)
            {
                if ((usageMasks[lane] & (1u << slot)) == 0) continue;
                ref ContactWide point = ref points[slot];
                GetContact(ref data, slot).Accumulated = Vector.Create(point.AccumulatedNormal[lane],
                    point.AccumulatedTangent1[lane], point.AccumulatedTangent2[lane], 0);
            }
        }
    }
}
