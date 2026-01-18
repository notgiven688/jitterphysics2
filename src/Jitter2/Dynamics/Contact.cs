/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */


using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics;

/// <summary>
/// Holds four <see cref="Contact"/> structs. The <see cref="ContactData.UsageMask"/>
/// indicates which contacts are actually in use. Every shape-to-shape collision in Jitter is managed
/// by one of these structs.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ContactData
{
    [Flags]
    public enum SolveMode
    {
        None = 0,
        LinearBody1 = 1 << 0,
        AngularBody1 = 1 << 1,
        LinearBody2 = 1 << 2,
        AngularBody2 = 1 << 3,
        FullBody1 = LinearBody1 | AngularBody1,
        FullBody2 = LinearBody2 | AngularBody2,
        Linear = LinearBody1 | LinearBody2,
        Angular = AngularBody1 | AngularBody2,
        Full = Linear | Angular,
    }

    public const uint MaskContact0 = 0b0001;
    public const uint MaskContact1 = 0b0010;
    public const uint MaskContact2 = 0b0100;
    public const uint MaskContact3 = 0b1000;

    public const uint MaskContactAll = MaskContact0 | MaskContact1 | MaskContact2 | MaskContact3;

    // Accessed in unsafe code.
#pragma warning disable CS0649
    internal int _internal;
#pragma warning restore CS0649

    /// <summary>
    /// The least four significant bits indicate which contacts are considered intact (bit set), broken (bit unset).
    /// Bits 5-8 indicate which contacts were intact/broken during the solving-phase.
    /// </summary>
    /// <example>
    /// A sphere may slide down a ramp. Within one timestep Jitter may detect the collision, create the contact,
    /// solve the contact, integrate velocities and positions and then consider the contact as broken, since the
    /// movement orthogonal to the contact normal exceeds a threshold. This results in no intact contact before calling
    /// <see cref="World.Step(Real, bool)"/> and no intact contact after the call. However, the corresponding bit for the
    /// solver-phase will be set in this scenario.
    /// </example>
    public uint UsageMask;

    public JHandle<RigidBodyData> Body1;
    public JHandle<RigidBodyData> Body2;

    public ArbiterKey Key;

    public Real Restitution;
    public Real Friction;
    public Real SpeculativeRelaxationFactor;

    public SolveMode Mode;

    public Contact Contact0;
    public Contact Contact1;
    public Contact Contact2;
    public Contact Contact3;

    public unsafe void PrepareForIteration(Real idt)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if (Vector.IsHardwareAccelerated)
        {
            if ((UsageMask & MaskContact0) != 0) Contact0.PrepareForIterationAccelerated(ptr, idt);
            if ((UsageMask & MaskContact1) != 0) Contact1.PrepareForIterationAccelerated(ptr, idt);
            if ((UsageMask & MaskContact2) != 0) Contact2.PrepareForIterationAccelerated(ptr, idt);
            if ((UsageMask & MaskContact3) != 0) Contact3.PrepareForIterationAccelerated(ptr, idt);
        }
        else
        {
            if ((UsageMask & MaskContact0) != 0) Contact0.PrepareForIteration(ptr, idt);
            if ((UsageMask & MaskContact1) != 0) Contact1.PrepareForIteration(ptr, idt);
            if ((UsageMask & MaskContact2) != 0) Contact2.PrepareForIteration(ptr, idt);
            if ((UsageMask & MaskContact3) != 0) Contact3.PrepareForIteration(ptr, idt);
        }
    }

    public unsafe void Iterate(bool applyBias)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if (Vector.IsHardwareAccelerated)
        {
            if ((UsageMask & MaskContact0) != 0) Contact0.IterateAccelerated(ptr, applyBias);
            if ((UsageMask & MaskContact1) != 0) Contact1.IterateAccelerated(ptr, applyBias);
            if ((UsageMask & MaskContact2) != 0) Contact2.IterateAccelerated(ptr, applyBias);
            if ((UsageMask & MaskContact3) != 0) Contact3.IterateAccelerated(ptr, applyBias);
        }
        else
        {
            if ((UsageMask & MaskContact0) != 0) Contact0.Iterate(ptr, applyBias);
            if ((UsageMask & MaskContact1) != 0) Contact1.Iterate(ptr, applyBias);
            if ((UsageMask & MaskContact2) != 0) Contact2.Iterate(ptr, applyBias);
            if ((UsageMask & MaskContact3) != 0) Contact3.Iterate(ptr, applyBias);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current system supports hardware acceleration
    /// for SIMD (Single Instruction, Multiple Data) operations.
    /// </summary>
    public static bool IsHardwareAccelerated => Vector.IsHardwareAccelerated;

    public unsafe void UpdatePosition()
    {
        UsageMask &= MaskContactAll;
        UsageMask |= UsageMask << 4;

        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if ((UsageMask & MaskContact0) != 0)
        {
            if (!Contact0.UpdatePosition(ptr)) UsageMask &= ~MaskContact0;
        }

        if ((UsageMask & MaskContact1) != 0)
        {
            if (!Contact1.UpdatePosition(ptr)) UsageMask &= ~MaskContact1;
        }

        if ((UsageMask & MaskContact2) != 0)
        {
            if (!Contact2.UpdatePosition(ptr)) UsageMask &= ~MaskContact2;
        }

        if ((UsageMask & MaskContact3) != 0)
        {
            if (!Contact3.UpdatePosition(ptr)) UsageMask &= ~MaskContact3;
        }
    }

    public void Init(RigidBody body1, RigidBody body2)
    {
        Body1 = body1.Handle;
        Body2 = body2.Handle;

        Friction = MathR.Max(body1.Friction, body2.Friction);
        Restitution = MathR.Max(body1.Restitution, body2.Restitution);
        SpeculativeRelaxationFactor = body1.World.SpeculativeRelaxationFactor;

        Debug.Assert(body1.World == body2.World);

        Mode = SolveMode.None;
        UsageMask = 0;
    }

    /// <summary>
    /// Resets the solve mode based on the current motion types of the bodies, then removes the specified flags.
    /// </summary>
    /// <param name="removeFlags">Flags to remove from the full solve mode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetMode(SolveMode removeFlags = SolveMode.None)
    {
        Mode = (Body1.Data.MotionType == MotionType.Dynamic) ? SolveMode.FullBody1 : 0;
        Mode |= (Body2.Data.MotionType == MotionType.Dynamic) ? SolveMode.FullBody2 : 0;
        Mode &= ~removeFlags;
    }

    // ---------------------------------------------------------------------------------------------------------
    //
    // The following contact caching code is heavily influenced / a direct copy of the great
    // Bullet Physics Engine:

    //
    // Bullet Continuous Collision Detection and Physics Library Copyright (c) 2003-2006 Erwin
    // Coumans  https://bulletphysics.org

    // This software is provided 'as-is', without any express or implied warranty. In no event will
    // the authors be held liable for any damages arising from the use of this software. Permission
    // is granted to anyone to use this software for any purpose, including commercial applications,
    // and to alter it and redistribute it freely, subject to the following restrictions:

    // 1. The origin of this software must not be misrepresented; you must not claim that you wrote
    //    the original software. If you use this software in a product, an acknowledgment in the
    //    product documentation would be appreciated but is not required.
    // 2. Altered source versions must be plainly marked as such, and must not be misrepresented as
    //    being the original software.
    // 3. This notice may not be removed or altered from any source distribution.
    //
    // https://github.com/bulletphysics/bullet3/blob/39b8de74df93721add193e5b3d9ebee579faebf8/
    // src/Bullet3OpenCL/NarrowphaseCollision/b3ContactCache.cpp

    /// <summary>
    /// Adds a new collision result to the contact manifold. Keeps at most four points.
    /// </summary>
    public unsafe void AddContact(in JVector point1, in JVector point2, in JVector normal)
    {
        if ((UsageMask & MaskContactAll) == MaskContactAll)
        {
            // All four contacts are in use. Find one candidate to be replaced by the new one.
            SortCachedPoints(point1, point2, normal);
            return;
        }

        // Not all contacts are in use, but the new contact point is close enough
        // to an already existing point. Replace this point by the new one.

        Contact* closest = (Contact*)IntPtr.Zero;
        Real distanceSq = Real.MaxValue;

        JVector relP1 = point1 - Body1.Data.Position;

        if ((UsageMask & MaskContact0) != 0)
        {
            Real distSq = (Contact0.RelativePosition1 - relP1).LengthSquared();
            if (distSq < distanceSq)
            {
                distanceSq = distSq;
                closest = (Contact*)Unsafe.AsPointer(ref Contact0);
            }
        }

        if ((UsageMask & MaskContact1) != 0)
        {
            Real distSq = (Contact1.RelativePosition1 - relP1).LengthSquared();
            if (distSq < distanceSq)
            {
                distanceSq = distSq;
                closest = (Contact*)Unsafe.AsPointer(ref Contact1);
            }
        }

        if ((UsageMask & MaskContact2) != 0)
        {
            Real distSq = (Contact2.RelativePosition1 - relP1).LengthSquared();
            if (distSq < distanceSq)
            {
                distanceSq = distSq;
                closest = (Contact*)Unsafe.AsPointer(ref Contact2);
            }
        }

        if ((UsageMask & MaskContact3) != 0)
        {
            Real distSq = (Contact3.RelativePosition1 - relP1).LengthSquared();
            if (distSq < distanceSq)
            {
                distanceSq = distSq;
                closest = (Contact*)Unsafe.AsPointer(ref Contact3);
            }
        }

        if (distanceSq < Contact.BreakThreshold * Contact.BreakThreshold)
        {
            closest->Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, false, Restitution);
            return;
        }

        // It is a completely new contact.

        if ((UsageMask & MaskContact0) == 0)
        {
            Contact0.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, true, Restitution);
            UsageMask |= MaskContact0;
        }
        else if ((UsageMask & MaskContact1) == 0)
        {
            Contact1.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, true, Restitution);
            UsageMask |= MaskContact1;
        }
        else if ((UsageMask & MaskContact2) == 0)
        {
            Contact2.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, true, Restitution);
            UsageMask |= MaskContact2;
        }
        else if ((UsageMask & MaskContact3) == 0)
        {
            Contact3.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, true, Restitution);
            UsageMask |= MaskContact3;
        }
    }

    private static Real CalcArea4Points(in JVector p0, in JVector p1, in JVector p2, in JVector p3)
    {
        JVector a0 = p0 - p1;
        JVector a1 = p0 - p2;
        JVector a2 = p0 - p3;
        JVector b0 = p2 - p3;
        JVector b1 = p1 - p3;
        JVector b2 = p1 - p2;

        JVector tmp0 = a0 % b0;
        JVector tmp1 = a1 % b1;
        JVector tmp2 = a2 % b2;

        return MathR.Max(MathR.Max(tmp0.LengthSquared(), tmp1.LengthSquared()), tmp2.LengthSquared());
    }

    private void SortCachedPoints(in JVector point1, in JVector point2, in JVector normal)
    {
        JVector.Subtract(point1, Body1.Data.Position, out JVector rp1);

        // calculate 4 possible cases areas, and take the biggest area
        // int maxPenetrationIndex = -1;
        // Real maxPenetration = penetration;

        // always prefer the new point
        const Real epsilon = -(Real)0.0001;

        Real biggestArea = 0;

        ref Contact cref = ref Contact0;
        uint index = 0;

        Real area = CalcArea4Points(rp1, Contact1.RelativePosition1, Contact2.RelativePosition1, Contact3.RelativePosition1);

        if (area > biggestArea + epsilon)
        {
            biggestArea = area;
            cref = ref Contact0;
            index = MaskContact0;
        }

        area = CalcArea4Points(rp1, Contact0.RelativePosition1, Contact2.RelativePosition1, Contact3.RelativePosition1);

        if (area > biggestArea + epsilon)
        {
            biggestArea = area;
            cref = ref Contact1;
            index = MaskContact1;
        }

        area = CalcArea4Points(rp1, Contact0.RelativePosition1, Contact1.RelativePosition1, Contact3.RelativePosition1);

        if (area > biggestArea + epsilon)
        {
            biggestArea = area;
            cref = ref Contact2;
            index = MaskContact2;
        }

        area = CalcArea4Points(rp1, Contact0.RelativePosition1, Contact1.RelativePosition1, Contact2.RelativePosition1);

        if (area > biggestArea + epsilon)
        {
            cref = ref Contact3;
            index = MaskContact3;
        }

        cref.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, false, Restitution);
        UsageMask |= index;
    }

    // ---------------------------------------------------------------------------------------------------------

    [StructLayout(LayoutKind.Explicit)]
    public struct Contact
    {
        public const Real MaximumBias = (Real)100.0;
        public const Real BiasFactor = (Real)0.2;
        public const Real AllowedPenetration = (Real)0.01;
        public const Real BreakThreshold = (Real)0.02;

        [Flags]
        public enum Flags
        {
            NewContact = 1 << 1,
        }

        [FieldOffset(0)] public Flags Flag;

        [FieldOffset(4)] public Real Bias;

        [FieldOffset(4 + 1 * sizeof(Real))] public Real PenaltyBias;

        // ―――――― The following 4-component vectors overlap ――――――――――――――――――――――――――――――

        [FieldOffset(4 + 2 * sizeof(Real))] internal VectorReal NormalTangentX;

        [FieldOffset(4 + 5 * sizeof(Real))] internal VectorReal NormalTangentY;

        [FieldOffset(4 + 8 * sizeof(Real))] internal VectorReal NormalTangentZ;

        [FieldOffset(4 + 11 * sizeof(Real))] internal VectorReal MassNormalTangent;

        [FieldOffset(4 + 14 * sizeof(Real))] internal VectorReal Accumulated;

        [FieldOffset(4 + 18 * sizeof(Real))] [ReferenceFrame(ReferenceFrame.Local)]
        internal JVector Position1;

        [FieldOffset(4 + 21 * sizeof(Real))] [ReferenceFrame(ReferenceFrame.Local)]
        internal JVector Position2;

        /// <summary>
        /// Position of the contact relative to the center of mass on the first body.
        /// </summary>
        [FieldOffset(4+25*sizeof(Real))]
        [ReferenceFrame(ReferenceFrame.World)] public JVector RelativePosition1;

        /// <summary>
        /// Position of the contact relative to the center of mass on the second body.
        /// </summary>
        [FieldOffset(4+28*sizeof(Real))]
        [ReferenceFrame(ReferenceFrame.World)] public JVector RelativePosition2;

        /// <summary>
        /// Normal direction (normalized) of the contact.
        /// Pointing from the collision point on the surface of <see cref="ContactData.Body2"/> to the collision point
        /// on the surface of <see cref="ContactData.Body1"/>.
        /// </summary>
        [ReferenceFrame(ReferenceFrame.World)] public readonly JVector Normal => new (NormalTangentX.GetElement(0),
            NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));

        /// <summary>
        /// Tangent (normalized) to the contact <see cref="Normal"/> in the direction of the relative movement of
        /// both bodies, at the time when the contact is created.
        /// </summary>
        [ReferenceFrame(ReferenceFrame.World)] public readonly JVector Tangent1 => new (NormalTangentX.GetElement(1),
            NormalTangentY.GetElement(1), NormalTangentZ.GetElement(1));

        /// <summary>
        /// A second tangent forming an orthonormal basis with <see cref="Normal"/> and <see cref="Tangent1"/>.
        /// </summary>
        [ReferenceFrame(ReferenceFrame.World)] public readonly JVector Tangent2 => new JVector(NormalTangentX.GetElement(2),
            NormalTangentY.GetElement(2), NormalTangentZ.GetElement(2));

        /// <summary>
        /// The impulse applied in the normal direction which has been used to solve the contact.
        /// </summary>
        public readonly Real Impulse => Accumulated.GetElement(0);

        /// <summary>
        /// The impulse applied in the first tangent direction which has been used to solve the contact.
        /// </summary>
        public readonly Real TangentImpulse1 => Accumulated.GetElement(1);

        /// <summary>
        /// The impulse applied in the second tangent direction which has been used to solve the contact.
        /// </summary>
        public readonly Real TangentImpulse2 => Accumulated.GetElement(2);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Initialize(ref RigidBodyData b1, ref RigidBodyData b2, in JVector point1, in JVector point2,
            in JVector n, bool newContact, Real restitution)
        {
            Debug.Assert(Math.Abs(n.LengthSquared() - (Real)1.0) < 1e-3);

            JVector.Subtract(point1, b1.Position, out RelativePosition1);
            JVector.Subtract(point2, b2.Position, out RelativePosition2);
            JVector.ConjugatedTransform(RelativePosition1, b1.Orientation, out Position1);
            JVector.ConjugatedTransform(RelativePosition2, b2.Orientation, out Position2);

            // Material Properties
            if (!newContact) return;

            Flag = Flags.NewContact;
            Accumulated = Vector.Create((Real)0.0);

            JVector dv = b2.Velocity + b2.AngularVelocity % RelativePosition2;
            dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;

            Real relNormalVel = JVector.Dot(dv, n);

            Bias = 0;

            // Fake restitution
            if (relNormalVel < (Real)(-1.0))
            {
                Bias = -restitution * relNormalVel;
            }

            var tangent1 = dv - n * relNormalVel;

            Real num = tangent1.LengthSquared();

            if (num > (Real)1e-12)
            {
                num = (Real)1.0 / MathR.Sqrt(num);
                tangent1 *= num;
            }
            else
            {
                tangent1 = MathHelper.CreateOrthonormal(n);
            }

            var tangent2 = tangent1 % n;

            NormalTangentX = Vector.Create(n.X, tangent1.X, tangent2.X, 0);
            NormalTangentY = Vector.Create(n.Y, tangent1.Y, tangent2.Y, 0);
            NormalTangentZ = Vector.Create(n.Z, tangent1.Z, tangent2.Z, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public readonly unsafe bool UpdatePosition(ContactData* cd)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector.Transform(Position1, b1.Orientation, out var relativePos1);
            JVector.Add(relativePos1, b1.Position, out JVector p1);

            JVector.Transform(Position2, b2.Orientation, out var relativePos2);
            JVector.Add(relativePos2, b2.Position, out JVector p2);

            JVector.Subtract(p1, p2, out JVector dist);

            JVector n = new JVector(NormalTangentX.GetElement(0),
                NormalTangentY.GetElement(0),
                NormalTangentZ.GetElement(0));

            Real penetration = JVector.Dot(dist, n);

            if (penetration < -BreakThreshold * (Real)0.1)
            {
                return false;
            }

            dist -= penetration * n;
            Real tangentialOffsetSq = dist.LengthSquared();

            if (tangentialOffsetSq > BreakThreshold * BreakThreshold)
            {
                return false;
            }

            return true;
        }

        // Fallback for missing hardware acceleration
        #region public unsafe void PrepareForIteration(ContactData* cd, Real idt)

        public unsafe void PrepareForIteration(ContactData* cd, Real idt)
        {
            // Begin read from VectorReal
            Real accumulatedNormalImpulse = Accumulated.GetElement(0);
            Real accumulatedTangentImpulse1 = Accumulated.GetElement(1);
            Real accumulatedTangentImpulse2 = Accumulated.GetElement(2);

            var normal = new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));
            var tangent1 = new JVector(NormalTangentX.GetElement(1), NormalTangentY.GetElement(1), NormalTangentZ.GetElement(1));
            var tangent2 = new JVector(NormalTangentX.GetElement(2), NormalTangentY.GetElement(2), NormalTangentZ.GetElement(2));
            // End read from VectorReal

            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector.Transform(Position1, b1.Orientation, out RelativePosition1);
            JVector.Transform(Position2, b2.Orientation, out RelativePosition2);

            Real inverseMass = b1.InverseMass + b2.InverseMass;

            if ((Flag & Flags.NewContact) == 0)
            {
                Bias = 0;
            }

            JVector.Add(RelativePosition1, b1.Position, out JVector p1);
            JVector.Add(RelativePosition2, b2.Position, out JVector p2);
            JVector.Subtract(p1, p2, out JVector dist);
            Real penetration = JVector.Dot(dist, normal);

            if (penetration < -BreakThreshold)
            {
                // Speculative contact
                Bias = penetration * idt * cd->SpeculativeRelaxationFactor;
            }

            Flag &= ~Flags.NewContact;

            Real kTangent1 = inverseMass;
            Real kTangent2 = inverseMass;
            Real kNormal = inverseMass;

            JVector impulse = normal * accumulatedNormalImpulse + tangent1 * accumulatedTangentImpulse1 + tangent2 * accumulatedTangentImpulse2;

            if ((cd->Mode & SolveMode.LinearBody1) != 0)
            {
                b1.Velocity -= impulse * b1.InverseMass;
            }

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                // prepare
                JVector.Cross(RelativePosition1, normal, out JVector tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mN1);

                JVector.Cross(RelativePosition1, tangent1, out tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mT1);

                JVector.Cross(RelativePosition1, tangent2, out tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mTt1);

                JVector.Cross(mT1, RelativePosition1, out JVector rantra);
                kTangent1 += JVector.Dot(rantra, tangent1);

                JVector.Cross(mTt1, RelativePosition1, out rantra);
                kTangent2 += JVector.Dot(rantra, tangent2);

                JVector.Cross(mN1, RelativePosition1, out rantra);
                kNormal += JVector.Dot(rantra, normal);

                b1.AngularVelocity -= accumulatedNormalImpulse * mN1 + accumulatedTangentImpulse1 * mT1 +
                                      accumulatedTangentImpulse2 * mTt1;
            }


            if ((cd->Mode & SolveMode.LinearBody2) != 0)
            {
                b2.Velocity += impulse * b2.InverseMass;
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                JVector.Cross(RelativePosition2, normal, out JVector tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mN2);

                JVector.Cross(RelativePosition2, tangent1, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mT2);

                JVector.Cross(RelativePosition2, tangent2, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mTt2);

                JVector.Cross(mT2, RelativePosition2, out JVector rbntrb);
                kTangent1 += JVector.Dot(rbntrb, tangent1);

                JVector.Cross(mTt2, RelativePosition2, out rbntrb);
                kTangent2 += JVector.Dot(rbntrb, tangent2);

                JVector.Cross(mN2, RelativePosition2, out rbntrb);
                kNormal += JVector.Dot(rbntrb, normal);

                b2.AngularVelocity += accumulatedNormalImpulse * mN2 + accumulatedTangentImpulse1 * mT2 +
                                      accumulatedTangentImpulse2 * mTt2;
            }

            Real massTangent1 = (Real)1.0 / kTangent1;
            Real massTangent2 = (Real)1.0 / kTangent2;
            Real massNormal = (Real)1.0 / kNormal;

            JVector mass = new (massNormal, massTangent1, massTangent2);
            Unsafe.CopyBlock(Unsafe.AsPointer(ref MassNormalTangent), Unsafe.AsPointer(ref mass), 3 * sizeof(Real));

            PenaltyBias = BiasFactor * idt * Math.Max((Real)0.0, penetration - AllowedPenetration);
            PenaltyBias = Math.Min(PenaltyBias, MaximumBias);
        }

        #endregion

        // Fallback for missing hardware acceleration
        #region public unsafe void Iterate(ContactData* cd, bool applyBias)

        public unsafe void Iterate(ContactData* cd, bool applyBias)
        {
            // Begin read from VectorReal
            Real massNormal = MassNormalTangent.GetElement(0);
            Real massTangent1 = MassNormalTangent.GetElement(1);
            Real massTangent2 = MassNormalTangent.GetElement(2);
            Real accumulatedNormalImpulse = Accumulated.GetElement(0);
            Real accumulatedTangentImpulse1 = Accumulated.GetElement(1);
            Real accumulatedTangentImpulse2 = Accumulated.GetElement(2);

            var normal = new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));
            var tangent1 = new JVector(NormalTangentX.GetElement(1), NormalTangentY.GetElement(1), NormalTangentZ.GetElement(1));
            var tangent2 = new JVector(NormalTangentX.GetElement(2), NormalTangentY.GetElement(2), NormalTangentZ.GetElement(2));
            // End read from VectorReal

            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector dv = b2.Velocity + b2.AngularVelocity % RelativePosition2;
            dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;

            Real bias = (applyBias) ? MathR.Max(PenaltyBias, Bias) : Bias;

            Real vn = JVector.Dot(normal, dv);
            Real vt1 = JVector.Dot(tangent1, dv);
            Real vt2 = JVector.Dot(tangent2, dv);

            Real normalImpulse = -vn + bias;

            normalImpulse *= massNormal;

            Real oldNormalImpulse = accumulatedNormalImpulse;
            accumulatedNormalImpulse = MathR.Max(oldNormalImpulse + normalImpulse, (Real)0.0);
            normalImpulse = accumulatedNormalImpulse - oldNormalImpulse;

            Real maxTangentImpulse = cd->Friction * accumulatedNormalImpulse;
            Real tangentImpulse1 = massTangent1 * -vt1;
            Real tangentImpulse2 = massTangent2 * -vt2;

            Real oldTangentImpulse1 = accumulatedTangentImpulse1;
            accumulatedTangentImpulse1 = oldTangentImpulse1 + tangentImpulse1;
            accumulatedTangentImpulse1 = Math.Clamp(accumulatedTangentImpulse1, -maxTangentImpulse, maxTangentImpulse);
            tangentImpulse1 = accumulatedTangentImpulse1 - oldTangentImpulse1;

            Real oldTangentImpulse2 = accumulatedTangentImpulse2;
            accumulatedTangentImpulse2 = oldTangentImpulse2 + tangentImpulse2;
            accumulatedTangentImpulse2 = Math.Clamp(accumulatedTangentImpulse2, -maxTangentImpulse, maxTangentImpulse);
            tangentImpulse2 = accumulatedTangentImpulse2 - oldTangentImpulse2;

            Accumulated = Vector.Create(accumulatedNormalImpulse, accumulatedTangentImpulse1, accumulatedTangentImpulse2, 0);

            JVector impulse = normalImpulse * normal + tangentImpulse1 * tangent1 + tangentImpulse2 * tangent2;

            if ((cd->Mode & SolveMode.LinearBody1) != 0)
            {
                b1.Velocity -= b1.InverseMass * impulse;
            }

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                JVector.Cross(RelativePosition1, normal, out JVector tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mN1);

                JVector.Cross(RelativePosition1, tangent1, out tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mT1);

                JVector.Cross(RelativePosition1, tangent2, out tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mTt1);

                b1.AngularVelocity -= normalImpulse * mN1 + tangentImpulse1 * mT1 + tangentImpulse2 * mTt1;
            }

            if ((cd->Mode & SolveMode.LinearBody2) != 0)
            {
                b2.Velocity += b2.InverseMass * impulse;
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                JVector.Cross(RelativePosition2, normal, out JVector tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mN2);

                JVector.Cross(RelativePosition2, tangent1, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mT2);

                JVector.Cross(RelativePosition2, tangent2, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mTt2);

                b2.AngularVelocity += normalImpulse * mN2 + tangentImpulse1 * mT2 + tangentImpulse2 * mTt2;
            }

        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Real GetSum3(VectorReal vector)
        {
            return vector.GetElement(0) + vector.GetElement(1) + vector.GetElement(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void PrepareForIterationAccelerated(ContactData* cd, Real idt)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector.Transform(Position1, b1.Orientation, out RelativePosition1);
            JVector.Transform(Position2, b2.Orientation, out RelativePosition2);

            VectorReal kNormalTangent = Vector.Create(b1.InverseMass + b2.InverseMass);

            if ((Flag & Flags.NewContact) == 0)
            {
                Bias = 0;
            }

            JVector.Add(RelativePosition1, b1.Position, out JVector r1);
            JVector.Add(RelativePosition2, b2.Position, out JVector r2);
            JVector.Subtract(r1, r2, out JVector dist);

            JVector n = new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));
            Real penetration = JVector.Dot(dist, n);

            if (penetration < -BreakThreshold)
            {
                // Speculative contact
                Bias = penetration * idt * cd->SpeculativeRelaxationFactor;
            }

            Flag &= ~Flags.NewContact;

            var ktnx = Vector.Create((Real)0.0);
            var ktny = Vector.Create((Real)0.0);
            var ktnz = Vector.Create((Real)0.0);

            // warm-starting, linear
            Unsafe.SkipInit(out JVector linearImpulse);
            linearImpulse.X = GetSum3(Vector.Multiply(Accumulated, NormalTangentX));
            linearImpulse.Y = GetSum3(Vector.Multiply(Accumulated, NormalTangentY));
            linearImpulse.Z = GetSum3(Vector.Multiply(Accumulated, NormalTangentZ));

            if ((cd->Mode & SolveMode.LinearBody1) != 0)
            {
                b1.Velocity -= b1.InverseMass * linearImpulse;
            }

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                var rp1X = Vector.Create(RelativePosition1.X);
                var rp1Y = Vector.Create(RelativePosition1.Y);
                var rp1Z = Vector.Create(RelativePosition1.Z);

                var rrx = Vector.Subtract(Vector.Multiply(rp1Y, NormalTangentZ), Vector.Multiply(rp1Z, NormalTangentY));
                var rry = Vector.Subtract(Vector.Multiply(rp1Z, NormalTangentX), Vector.Multiply(rp1X, NormalTangentZ));
                var rrz = Vector.Subtract(Vector.Multiply(rp1X, NormalTangentY), Vector.Multiply(rp1Y, NormalTangentX));

                var ixx = Vector.Create(b1.InverseInertiaWorld.M11);
                var ixy = Vector.Create(b1.InverseInertiaWorld.M21);
                var ixz = Vector.Create(b1.InverseInertiaWorld.M31);
                var iyy = Vector.Create(b1.InverseInertiaWorld.M22);
                var iyz = Vector.Create(b1.InverseInertiaWorld.M23);
                var izz = Vector.Create(b1.InverseInertiaWorld.M33);

                var e1 = Vector.Add(Vector.Add(Vector.Multiply(ixx, rrx), Vector.Multiply(ixy, rry)), Vector.Multiply(ixz, rrz));
                var e2 = Vector.Add(Vector.Add(Vector.Multiply(ixy, rrx), Vector.Multiply(iyy, rry)), Vector.Multiply(iyz, rrz));
                var e3 = Vector.Add(Vector.Add(Vector.Multiply(ixz, rrx), Vector.Multiply(iyz, rry)), Vector.Multiply(izz, rrz));

                Unsafe.SkipInit(out JVector angularImpulse1);
                angularImpulse1.X = GetSum3(Vector.Multiply(Accumulated, e1));
                angularImpulse1.Y = GetSum3(Vector.Multiply(Accumulated, e2));
                angularImpulse1.Z = GetSum3(Vector.Multiply(Accumulated, e3));

                b1.AngularVelocity -= angularImpulse1;

                ktnx = Vector.Subtract(Vector.Multiply(e2, rp1Z), Vector.Multiply(e3, rp1Y));
                ktny = Vector.Subtract(Vector.Multiply(e3, rp1X), Vector.Multiply(e1, rp1Z));
                ktnz = Vector.Subtract(Vector.Multiply(e1, rp1Y), Vector.Multiply(e2, rp1X));
            }

            if ((cd->Mode & SolveMode.LinearBody2) != 0)
            {
                b2.Velocity += b2.InverseMass * linearImpulse;
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                var rp2X = Vector.Create(RelativePosition2.X);
                var rp2Y = Vector.Create(RelativePosition2.Y);
                var rp2Z = Vector.Create(RelativePosition2.Z);

                var rrx = Vector.Subtract(Vector.Multiply(rp2Y, NormalTangentZ), Vector.Multiply(rp2Z, NormalTangentY));
                var rry = Vector.Subtract(Vector.Multiply(rp2Z, NormalTangentX), Vector.Multiply(rp2X, NormalTangentZ));
                var rrz = Vector.Subtract(Vector.Multiply(rp2X, NormalTangentY), Vector.Multiply(rp2Y, NormalTangentX));

                var ixx = Vector.Create(b2.InverseInertiaWorld.M11);
                var ixy = Vector.Create(b2.InverseInertiaWorld.M21);
                var ixz = Vector.Create(b2.InverseInertiaWorld.M31);
                var iyy = Vector.Create(b2.InverseInertiaWorld.M22);
                var iyz = Vector.Create(b2.InverseInertiaWorld.M23);
                var izz = Vector.Create(b2.InverseInertiaWorld.M33);

                var f1 = Vector.Add(Vector.Add(Vector.Multiply(ixx, rrx), Vector.Multiply(ixy, rry)), Vector.Multiply(ixz, rrz));
                var f2 = Vector.Add(Vector.Add(Vector.Multiply(ixy, rrx), Vector.Multiply(iyy, rry)), Vector.Multiply(iyz, rrz));
                var f3 = Vector.Add(Vector.Add(Vector.Multiply(ixz, rrx), Vector.Multiply(iyz, rry)), Vector.Multiply(izz, rrz));

                Unsafe.SkipInit(out JVector angularImpulse2);
                angularImpulse2.X = GetSum3(Vector.Multiply(Accumulated, f1));
                angularImpulse2.Y = GetSum3(Vector.Multiply(Accumulated, f2));
                angularImpulse2.Z = GetSum3(Vector.Multiply(Accumulated, f3));

                b2.AngularVelocity += angularImpulse2;

                ktnx = Vector.Add(ktnx, Vector.Subtract(Vector.Multiply(f2, rp2Z), Vector.Multiply(f3, rp2Y)));
                ktny = Vector.Add(ktny, Vector.Subtract(Vector.Multiply(f3, rp2X), Vector.Multiply(f1, rp2Z)));
                ktnz = Vector.Add(ktnz, Vector.Subtract(Vector.Multiply(f1, rp2Y), Vector.Multiply(f2, rp2X)));
            }

            var kres = Vector.Add(Vector.Add(Vector.Multiply(NormalTangentX, ktnx),
                Vector.Multiply(NormalTangentY, ktny)), Vector.Multiply(NormalTangentZ, ktnz));

            kNormalTangent = Vector.Add(kNormalTangent, kres);

            var mnt = Vector.Divide(Vector.Create((Real)1.0), kNormalTangent);
            Unsafe.CopyBlock(Unsafe.AsPointer(ref MassNormalTangent), Unsafe.AsPointer(ref mnt), 3 * sizeof(Real));

            PenaltyBias = BiasFactor * idt * Math.Max((Real)0.0, penetration - AllowedPenetration);
            PenaltyBias = Math.Min(PenaltyBias, MaximumBias);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void IterateAccelerated(ContactData* cd, bool applyBias)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector dv = b2.Velocity + b2.AngularVelocity % RelativePosition2;
            dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;

            Real bias = applyBias ? MathR.Max(PenaltyBias, Bias) : Bias;

            var vdots = Vector.Add(Vector.Add(Vector.Multiply(NormalTangentX, Vector.Create(dv.X)),
                Vector.Multiply(NormalTangentY, Vector.Create(dv.Y))), Vector.Multiply(NormalTangentZ, Vector.Create(dv.Z)));

            var impulse = Vector.Multiply(MassNormalTangent, (Vector.Subtract(Vector.Create(bias, 0, 0, 0), vdots)));
            var oldImpulse = Accumulated;

            Real maxTangentImpulse = cd->Friction * Accumulated.GetElement(0);

            Accumulated = Vector.Add(oldImpulse, impulse);

            var minImpulse = Vector.Create(0, -maxTangentImpulse, -maxTangentImpulse, 0);
            var maxImpulse = Vector.Create(Real.MaxValue, maxTangentImpulse, maxTangentImpulse, 0);

            Accumulated = Vector.Min(Vector.Max(Accumulated, minImpulse), maxImpulse);
            impulse = Vector.Subtract(Accumulated, oldImpulse);

            Unsafe.SkipInit(out JVector linearImpulse);
            linearImpulse.X = GetSum3(Vector.Multiply(impulse, NormalTangentX));
            linearImpulse.Y = GetSum3(Vector.Multiply(impulse, NormalTangentY));
            linearImpulse.Z = GetSum3(Vector.Multiply(impulse, NormalTangentZ));

            if ((cd->Mode & SolveMode.LinearBody1) != 0)
            {
                b1.Velocity -= b1.InverseMass * linearImpulse;
            }

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                var rp1X = Vector.Create(RelativePosition1.X);
                var rp1Y = Vector.Create(RelativePosition1.Y);
                var rp1Z = Vector.Create(RelativePosition1.Z);

                var rrx = Vector.Subtract(Vector.Multiply(rp1Y, NormalTangentZ), Vector.Multiply(rp1Z, NormalTangentY));
                var rry = Vector.Subtract(Vector.Multiply(rp1Z, NormalTangentX), Vector.Multiply(rp1X, NormalTangentZ));
                var rrz = Vector.Subtract(Vector.Multiply(rp1X, NormalTangentY), Vector.Multiply(rp1Y, NormalTangentX));

                var ixx = Vector.Create(b1.InverseInertiaWorld.M11);
                var ixy = Vector.Create(b1.InverseInertiaWorld.M21);
                var ixz = Vector.Create(b1.InverseInertiaWorld.M31);
                var iyy = Vector.Create(b1.InverseInertiaWorld.M22);
                var iyz = Vector.Create(b1.InverseInertiaWorld.M23);
                var izz = Vector.Create(b1.InverseInertiaWorld.M33);

                var e1 = Vector.Add(Vector.Add(Vector.Multiply(ixx, rrx), Vector.Multiply(ixy, rry)), Vector.Multiply(ixz, rrz));
                var e2 = Vector.Add(Vector.Add(Vector.Multiply(ixy, rrx), Vector.Multiply(iyy, rry)), Vector.Multiply(iyz, rrz));
                var e3 = Vector.Add(Vector.Add(Vector.Multiply(ixz, rrx), Vector.Multiply(iyz, rry)), Vector.Multiply(izz, rrz));

                Unsafe.SkipInit(out JVector angularImpulse1);
                angularImpulse1.X = GetSum3(Vector.Multiply(impulse, e1));
                angularImpulse1.Y = GetSum3(Vector.Multiply(impulse, e2));
                angularImpulse1.Z = GetSum3(Vector.Multiply(impulse, e3));

                b1.AngularVelocity -= angularImpulse1;
            }

            if ((cd->Mode & SolveMode.LinearBody2) != 0)
            {
                b2.Velocity += b2.InverseMass * linearImpulse;
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                var rp2X = Vector.Create(RelativePosition2.X);
                var rp2Y = Vector.Create(RelativePosition2.Y);
                var rp2Z = Vector.Create(RelativePosition2.Z);

                var rrx = Vector.Subtract(Vector.Multiply(rp2Y, NormalTangentZ), Vector.Multiply(rp2Z, NormalTangentY));
                var rry = Vector.Subtract(Vector.Multiply(rp2Z, NormalTangentX), Vector.Multiply(rp2X, NormalTangentZ));
                var rrz = Vector.Subtract(Vector.Multiply(rp2X, NormalTangentY), Vector.Multiply(rp2Y, NormalTangentX));

                var ixx = Vector.Create(b2.InverseInertiaWorld.M11);
                var ixy = Vector.Create(b2.InverseInertiaWorld.M21);
                var ixz = Vector.Create(b2.InverseInertiaWorld.M31);
                var iyy = Vector.Create(b2.InverseInertiaWorld.M22);
                var iyz = Vector.Create(b2.InverseInertiaWorld.M23);
                var izz = Vector.Create(b2.InverseInertiaWorld.M33);

                var f1 = Vector.Add(Vector.Add(Vector.Multiply(ixx, rrx), Vector.Multiply(ixy, rry)), Vector.Multiply(ixz, rrz));
                var f2 = Vector.Add(Vector.Add(Vector.Multiply(ixy, rrx), Vector.Multiply(iyy, rry)), Vector.Multiply(iyz, rrz));
                var f3 = Vector.Add(Vector.Add(Vector.Multiply(ixz, rrx), Vector.Multiply(iyz, rry)), Vector.Multiply(izz, rrz));

                Unsafe.SkipInit(out JVector angularImpulse2);
                angularImpulse2.X = GetSum3(Vector.Multiply(impulse, f1));
                angularImpulse2.Y = GetSum3(Vector.Multiply(impulse, f2));
                angularImpulse2.Z = GetSum3(Vector.Multiply(impulse, f3));

                b2.AngularVelocity += angularImpulse2;
            }

        }
    }
}