/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;
using WideMath = System.Numerics.Vector;
#if USE_DOUBLE_PRECISION
using Wide = System.Numerics.Vector<double>;
#else
using Wide = System.Numerics.Vector<float>;
#endif

namespace Jitter2.Dynamics;

internal sealed partial class ContactSolverWide
{
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrepareAngularSide(in SymmetricMatrixWide inertia, in Vector3Wide position,
        in ContactWide point, Wide warmNormal, Wide warmTangent1, Wide warmTangent2,
        out Vector3Wide normalContribution, out Vector3Wide tangent1Contribution,
        out Vector3Wide tangent2Contribution, out Vector3Wide warmImpulse)
    {
        // Consume each response immediately so it need not stay live alongside the other
        // directions. These responses are temporary preparation data, never bundle state.
        Vector3Wide response = inertia.Transform(Vector3Wide.Cross(position, point.Normal));
        normalContribution = Vector3Wide.Cross(response, position);
        warmImpulse = response * warmNormal;

        response = inertia.Transform(Vector3Wide.Cross(position, point.Tangent1));
        tangent1Contribution = Vector3Wide.Cross(response, position);
        warmImpulse += response * warmTangent1;

        response = inertia.Transform(Vector3Wide.Cross(position, point.Tangent2));
        tangent2Contribution = Vector3Wide.Cross(response, position);
        warmImpulse += response * warmTangent2;
    }

    /// <summary>
    /// Prepares geometry, effective masses and warm starts directly across independent
    /// manifolds. The prepared anchors and masses remain fixed until the next substep.
    /// </summary>
    internal void Prepare(ReadOnlySpan<JHandle<ContactData>> source, in TimeStep timeStep)
    {
        InitializeBundle(source);

        Vector3Wide position1 = default, position2 = default;
        Vector3Wide velocity1 = default, angular1 = default, velocity2 = default, angular2 = default;
        QuaternionWide orientation1 = default, orientation2 = default;
        Wide linearLocks = default, ccdRelaxation = default;
        ContactData.SolveMode combinedMode = ContactData.SolveMode.None;
        bool anyLinearLocks = false;

        for (int lane = 0; lane < Count; lane++)
        {
            ref RigidBodyData body1 = ref bodies1[lane].Data;
            ref RigidBodyData body2 = ref bodies2[lane].Data;
            position1.SetLane(lane, body1.Position);
            position2.SetLane(lane, body2.Position);
            orientation1.SetLane(lane, body1.Orientation);
            orientation2.SetLane(lane, body2.Orientation);
            velocity1.SetLane(lane, body1.Velocity);
            angular1.SetLane(lane, body1.AngularVelocity);
            velocity2.SetLane(lane, body2.Velocity);
            angular2.SetLane(lane, body2.AngularVelocity);
            bool hasLinearLocks = body1.HasLinearLocks || body2.HasLinearLocks;
            SetLane(ref linearLocks, lane, hasLinearLocks ? 1 : 0);
            anyLinearLocks |= hasLinearLocks;
            combinedMode |= modes[lane];
            SetLane(ref ccdRelaxation, lane, contacts[lane].Data.CcdRelaxationFactor);
        }

        Vector3Wide inverseMassSum = inverseMass1 + inverseMass2;
        var lockedLanes = WideMath.GreaterThan(linearLocks, Wide.Zero);
        Wide inverseStepDt = new(timeStep.InverseStepDt);
        Wide biasFactor = new(ContactData.Contact.BiasFactor * timeStep.InverseStepDt);

        for (int slot = 0; slot < 4; slot++)
        {
            if ((occupiedSlots & (1u << slot)) == 0) continue;
            ref ContactWide point = ref points[slot];
            Vector3Wide localPosition1 = default, localPosition2 = default;
            Wide active = default, warmNormal = default, warmTangent1 = default, warmTangent2 = default;

            for (int lane = 0; lane < Count; lane++)
            {
                if ((usageMasks[lane] & (1u << slot)) == 0) continue;
                ref ContactData.Contact contact = ref GetContact(ref contacts[lane].Data, slot);
                point.Normal.SetLane(lane, contact.Normal);
                point.Tangent1.SetLane(lane, contact.Tangent1);
                point.Tangent2.SetLane(lane, contact.Tangent2);
                point.RelativePosition1.SetLane(lane, contact.RelativePosition1);
                point.RelativePosition2.SetLane(lane, contact.RelativePosition2);
                localPosition1.SetLane(lane, contact.Position1);
                localPosition2.SetLane(lane, contact.Position2);
                Real normal = contact.Accumulated.GetElement(0);
                Real tangent1 = contact.Accumulated.GetElement(1);
                Real tangent2 = contact.Accumulated.GetElement(2);
                SetLane(ref point.AccumulatedNormal, lane, normal);
                SetLane(ref point.AccumulatedTangent1, lane, tangent1);
                SetLane(ref point.AccumulatedTangent2, lane, tangent2);
                SetLane(ref active, lane, 1);
                if ((contact.Flag & ContactData.Contact.Flags.NewContact) != 0)
                    SetLane(ref point.Bias, lane, contact.Bias);
                if ((contact.Flag & ContactData.Contact.Flags.SkipWarmStart) == 0)
                {
                    SetLane(ref warmNormal, lane, normal);
                    SetLane(ref warmTangent1, lane, tangent1);
                    SetLane(ref warmTangent2, lane, tangent2);
                }
                contact.Flag &= ~(ContactData.Contact.Flags.NewContact | ContactData.Contact.Flags.SkipWarmStart);
            }

            // Impulse offsets stay fixed for the whole step. Only separation follows
            // the current poses during substeps, matching Contact.PrepareForIteration.
            Vector3Wide currentRelativePosition1 = orientation1.Transform(localPosition1);
            Vector3Wide currentRelativePosition2 = orientation2.Transform(localPosition2);
            Vector3Wide separation = (currentRelativePosition1 + position1) - (currentRelativePosition2 + position2);
            Wide penetration = Vector3Wide.Dot(separation, point.Normal);
            point.Bias = WideMath.ConditionalSelect(
                WideMath.LessThan(penetration, new Wide(-ContactData.Contact.BreakThreshold)),
                (penetration * inverseStepDt) * ccdRelaxation, point.Bias);
            Wide penaltyBias = WideMath.Min(
                biasFactor * WideMath.Max(Wide.Zero, penetration - new Wide(ContactData.Contact.AllowedPenetration)),
                new Wide(ContactData.Contact.MaximumBias));
            point.BiasedTarget = WideMath.ConditionalSelect(WideMath.GreaterThan(penaltyBias, Wide.Zero),
                WideMath.Max(penaltyBias, point.Bias), point.Bias);

            // The old preparation uses the scalar inverse mass when no axis is locked.
            // Preserve that choice instead of assuming the stored basis is bit-exactly unit length.
            Wide kNormal = inverseMassSum.X, kTangent1 = inverseMassSum.X, kTangent2 = inverseMassSum.X;
            if (anyLinearLocks)
            {
                kNormal = WideMath.ConditionalSelect(lockedLanes,
                    Vector3Wide.Dot(Vector3Wide.Multiply(inverseMassSum, point.Normal), point.Normal), inverseMassSum.X);
                kTangent1 = WideMath.ConditionalSelect(lockedLanes,
                    Vector3Wide.Dot(Vector3Wide.Multiply(inverseMassSum, point.Tangent1), point.Tangent1), inverseMassSum.X);
                kTangent2 = WideMath.ConditionalSelect(lockedLanes,
                    Vector3Wide.Dot(Vector3Wide.Multiply(inverseMassSum, point.Tangent2), point.Tangent2), inverseMassSum.X);
            }

            Vector3Wide normalContribution = default, tangent1Contribution = default, tangent2Contribution = default;
            Vector3Wide warmAngular1 = default, warmAngular2 = default;
            // Ground contacts often share a read-only body side across the entire bundle.
            // Skip its angular preparation altogether; mixed bundles retain masked inertia.
            if ((combinedMode & ContactData.SolveMode.AngularBody1) != 0)
            {
                PrepareAngularSide(inverseInertia1, point.RelativePosition1, point,
                    warmNormal, warmTangent1, warmTangent2, out normalContribution,
                    out tangent1Contribution, out tangent2Contribution, out warmAngular1);
            }
            if ((combinedMode & ContactData.SolveMode.AngularBody2) != 0)
            {
                PrepareAngularSide(inverseInertia2, point.RelativePosition2, point,
                    warmNormal, warmTangent1, warmTangent2, out Vector3Wide normalContribution2,
                    out Vector3Wide tangent1Contribution2, out Vector3Wide tangent2Contribution2, out warmAngular2);
                normalContribution += normalContribution2;
                tangent1Contribution += tangent1Contribution2;
                tangent2Contribution += tangent2Contribution2;
            }

            kNormal += Vector3Wide.Dot(point.Normal, normalContribution);
            kTangent1 += Vector3Wide.Dot(point.Tangent1, tangent1Contribution);
            kTangent2 += Vector3Wide.Dot(point.Tangent2, tangent2Contribution);

            // An absent contact must remain a neutral lane even when its bodies are dynamic.
            var activeLanes = WideMath.GreaterThan(active, Wide.Zero);
            point.MassNormal = WideMath.ConditionalSelect(activeLanes & WideMath.GreaterThan(kNormal, Wide.Zero),
                Wide.One / kNormal, Wide.Zero);
            point.MassTangent1 = WideMath.ConditionalSelect(activeLanes & WideMath.GreaterThan(kTangent1, Wide.Zero),
                Wide.One / kTangent1, Wide.Zero);
            point.MassTangent2 = WideMath.ConditionalSelect(activeLanes & WideMath.GreaterThan(kTangent2, Wide.Zero),
                Wide.One / kTangent2, Wide.Zero);

            Vector3Wide impulse = (point.Normal * warmNormal + point.Tangent1 * warmTangent1) + point.Tangent2 * warmTangent2;
            velocity1 -= Vector3Wide.Multiply(impulse, inverseMass1);
            velocity2 += Vector3Wide.Multiply(impulse, inverseMass2);
            angular1 -= warmAngular1;
            angular2 += warmAngular2;

            for (int lane = 0; lane < Count; lane++)
            {
                if ((usageMasks[lane] & (1u << slot)) == 0) continue;
                ref ContactData.Contact contact = ref GetContact(ref contacts[lane].Data, slot);
                contact.Bias = point.Bias[lane];
                contact.PenaltyBias = penaltyBias[lane];
            }
        }

        for (int lane = 0; lane < Count; lane++)
        {
            // Read-only components may be shared by other lanes or concurrent bundles.
            ContactData.SolveMode mode = modes[lane];
            ref RigidBodyData body1 = ref bodies1[lane].Data;
            ref RigidBodyData body2 = ref bodies2[lane].Data;
            if ((mode & ContactData.SolveMode.LinearBody1) != 0) body1.Velocity = velocity1.GetLane(lane);
            if ((mode & ContactData.SolveMode.AngularBody1) != 0) body1.AngularVelocity = angular1.GetLane(lane);
            if ((mode & ContactData.SolveMode.LinearBody2) != 0) body2.Velocity = velocity2.GetLane(lane);
            if ((mode & ContactData.SolveMode.AngularBody2) != 0) body2.AngularVelocity = angular2.GetLane(lane);
        }
    }
}
