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

/// <summary>
/// A reusable structure-of-arrays bundle of independent contact manifolds. One SIMD lane
/// represents one manifold; its contact points retain their sequential Gauss-Seidel order.
/// </summary>
/// <remarks>
/// The caller must exclude body read/write conflicts between lanes and write impulses
/// back before discarding or preparing the bundle again.
/// Handles, rather than pointers into the movable body/contact buffers, are retained.
/// </remarks>
internal sealed partial class ContactSolverWide
{
    internal static int Width => Wide.Count;

    internal int Count { get; private set; }

    private readonly JHandle<ContactData>[] contacts = new JHandle<ContactData>[Width];
    private readonly JHandle<RigidBodyData>[] bodies1 = new JHandle<RigidBodyData>[Width];
    private readonly JHandle<RigidBodyData>[] bodies2 = new JHandle<RigidBodyData>[Width];
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

    private struct ContactWide
    {
        internal Vector3Wide Normal, Tangent1, Tangent2;
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

    private void InitializeBundle(ReadOnlySpan<JHandle<ContactData>> source)
    {
        if (source.Length > Width) throw new ArgumentOutOfRangeException(nameof(source));
        Count = source.Length;
        occupiedSlots = 0;
        Array.Clear(points);
        inverseMass1 = inverseMass2 = default;
        friction = default;

        for (int lane = 0; lane < Count; lane++)
        {
            contacts[lane] = source[lane];
            ref ContactData data = ref source[lane].Data;
            bodies1[lane] = data.Body1;
            bodies2[lane] = data.Body2;
            modes[lane] = data.Mode;
            usageMasks[lane] = data.UsageMask & ContactData.MaskContactAll;
            occupiedSlots |= usageMasks[lane];
            SetLane(ref friction, lane, data.Friction);
            if ((data.Mode & ContactData.SolveMode.LinearBody1) != 0)
                inverseMass1.SetLane(lane, data.Body1.Data.InverseMassVector);
            if ((data.Mode & ContactData.SolveMode.LinearBody2) != 0)
                inverseMass2.SetLane(lane, data.Body2.Data.InverseMassVector);
        }

        RefreshInertia();
    }

    /// <summary>Packs coefficients after the regular preparation and warm-start pass.</summary>
    internal void Prepare(ReadOnlySpan<JHandle<ContactData>> source)
    {
        InitializeBundle(source);
        for (int lane = 0; lane < Count; lane++)
        {
            ref ContactData data = ref source[lane].Data;
            for (int slot = 0; slot < 4; slot++)
            {
                if ((usageMasks[lane] & (1u << slot)) == 0) continue;
                ref ContactData.Contact contact = ref GetContact(ref data, slot);
                ref ContactWide point = ref points[slot];
                point.Normal.SetLane(lane, contact.Normal);
                point.Tangent1.SetLane(lane, contact.Tangent1);
                point.Tangent2.SetLane(lane, contact.Tangent2);
                point.RelativePosition1.SetLane(lane, contact.RelativePosition1);
                point.RelativePosition2.SetLane(lane, contact.RelativePosition2);
                SetLane(ref point.MassNormal, lane, contact.MassNormalTangent.GetElement(0));
                SetLane(ref point.MassTangent1, lane, contact.MassNormalTangent.GetElement(1));
                SetLane(ref point.MassTangent2, lane, contact.MassNormalTangent.GetElement(2));
                SetLane(ref point.AccumulatedNormal, lane, contact.Accumulated.GetElement(0));
                SetLane(ref point.AccumulatedTangent1, lane, contact.Accumulated.GetElement(1));
                SetLane(ref point.AccumulatedTangent2, lane, contact.Accumulated.GetElement(2));
                SetLane(ref point.Bias, lane, contact.Bias);
                SetLane(ref point.BiasedTarget, lane, contact.PenaltyBias > 0
                    ? MathR.Max(contact.PenaltyBias, contact.Bias) : contact.Bias);
            }
        }
    }

    /// <summary>
    /// Captures the current world inertia, including angular motion restrictions.
    /// Effective masses, anchors and accumulated impulses retain their prepared values.
    /// </summary>
    internal void RefreshInertia()
    {
        inverseInertia1 = inverseInertia2 = default;
        for (int lane = 0; lane < Count; lane++)
        {
            if ((modes[lane] & ContactData.SolveMode.AngularBody1) != 0)
                inverseInertia1.SetLane(lane, bodies1[lane].Data.InverseInertiaWorld);
            if ((modes[lane] & ContactData.SolveMode.AngularBody2) != 0)
                inverseInertia2.SetLane(lane, bodies2[lane].Data.InverseInertiaWorld);
        }
    }

    /// <summary>Runs an iteration with live velocities, without allocating or repacking coefficients.</summary>
    internal void Iterate(bool applyBias)
    {
        Vector3Wide velocity1 = default, angular1 = default, velocity2 = default, angular2 = default;
        for (int lane = 0; lane < Count; lane++)
        {
            ref RigidBodyData body1 = ref bodies1[lane].Data;
            ref RigidBodyData body2 = ref bodies2[lane].Data;
            // Read both bodies even when their response is disabled: kinematic motion and
            // other read-only velocity components still contribute to relative velocity.
            velocity1.SetLane(lane, body1.Velocity);
            angular1.SetLane(lane, body1.AngularVelocity);
            velocity2.SetLane(lane, body2.Velocity);
            angular2.SetLane(lane, body2.AngularVelocity);
        }

        for (int slot = 0; slot < 4; slot++)
        {
            if ((occupiedSlots & (1u << slot)) == 0) continue;
            ref ContactWide point = ref points[slot];
            Vector3Wide relative = (velocity2 + Vector3Wide.Cross(angular2, point.RelativePosition2)) -
                                   (velocity1 + Vector3Wide.Cross(angular1, point.RelativePosition1));
            Wide bias = applyBias ? point.BiasedTarget : point.Bias;
            Wide normal = point.MassNormal * (bias - Vector3Wide.Dot(point.Normal, relative));
            Wide tangent1 = point.MassTangent1 * -Vector3Wide.Dot(point.Tangent1, relative);
            Wide tangent2 = point.MassTangent2 * -Vector3Wide.Dot(point.Tangent2, relative);

            // Match Contact.Iterate: friction uses the normal impulse before this update.
            Wide maximumFriction = friction * point.AccumulatedNormal;
            Wide accumulatedNormal = WideMath.Min(WideMath.Max(point.AccumulatedNormal + normal, Wide.Zero),
                new Wide(Real.MaxValue));
            Wide accumulatedTangent1 = WideMath.Min(WideMath.Max(point.AccumulatedTangent1 + tangent1,
                -maximumFriction), maximumFriction);
            Wide accumulatedTangent2 = WideMath.Min(WideMath.Max(point.AccumulatedTangent2 + tangent2,
                -maximumFriction), maximumFriction);
            normal = accumulatedNormal - point.AccumulatedNormal;
            tangent1 = accumulatedTangent1 - point.AccumulatedTangent1;
            tangent2 = accumulatedTangent2 - point.AccumulatedTangent2;
            point.AccumulatedNormal = accumulatedNormal;
            point.AccumulatedTangent1 = accumulatedTangent1;
            point.AccumulatedTangent2 = accumulatedTangent2;

            Vector3Wide impulse = (point.Normal * normal + point.Tangent1 * tangent1) + point.Tangent2 * tangent2;
            velocity1 -= Vector3Wide.Multiply(impulse, inverseMass1);
            velocity2 += Vector3Wide.Multiply(impulse, inverseMass2);
            // Recompute the angular response to the combined impulse rather than loading
            // six cached response vectors per point. Frozen anchors and inertia preserve
            // the same constraint through biased solving and velocity relaxation.
            angular1 -= inverseInertia1.Transform(Vector3Wide.Cross(point.RelativePosition1, impulse));
            angular2 += inverseInertia2.Transform(Vector3Wide.Cross(point.RelativePosition2, impulse));
        }

        for (int lane = 0; lane < Count; lane++)
        {
            // Read-only bodies may appear in many simultaneous bundles. Never write them,
            // including stores of their unchanged value, because those stores race readers.
            ContactData.SolveMode mode = modes[lane];
            ref RigidBodyData body1 = ref bodies1[lane].Data;
            ref RigidBodyData body2 = ref bodies2[lane].Data;
            if ((mode & ContactData.SolveMode.LinearBody1) != 0) body1.Velocity = velocity1.GetLane(lane);
            if ((mode & ContactData.SolveMode.AngularBody1) != 0) body1.AngularVelocity = angular1.GetLane(lane);
            if ((mode & ContactData.SolveMode.LinearBody2) != 0) body2.Velocity = velocity2.GetLane(lane);
            if ((mode & ContactData.SolveMode.AngularBody2) != 0) body2.AngularVelocity = angular2.GetLane(lane);
        }
    }

    /// <summary>Publishes accumulated impulses for warm starts, skipping checks and contact queries.</summary>
    internal void WriteBack()
    {
        for (int lane = 0; lane < Count; lane++)
        {
            ref ContactData data = ref contacts[lane].Data;
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
