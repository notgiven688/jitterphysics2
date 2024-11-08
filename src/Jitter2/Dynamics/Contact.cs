/*
 * Copyright (c) Thorben Linneweber and others
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Jitter2.LinearMath;
using Jitter2.UnmanagedMemory;

namespace Jitter2.Dynamics;

/// <summary>
/// Holds four <see cref="Contact"/> structs. The <see cref="ContactData.UsageMask"/>
/// indicates which contacts are actually in use. Every shape-to-shape collision in Jitter is managed
/// by one of these structs.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ContactData
{
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
    /// <see cref="World.Step(float, bool)"/> and no intact contact after the call. However, the correspondig bit for the
    /// solver-phase will be set in this scenario.
    /// </example>
    public uint UsageMask;

    public JHandle<RigidBodyData> Body1;
    public JHandle<RigidBodyData> Body2;

    public ArbiterKey Key;

    public float Restitution;
    public float Friction;

    public bool IsSpeculative;

    public Contact Contact0;
    public Contact Contact1;
    public Contact Contact2;
    public Contact Contact3;

    public unsafe void PrepareForIteration(float dt)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if (Vector128.IsHardwareAccelerated)
        {
            if ((UsageMask & MaskContact0) != 0) Contact0.PrepareForIterationAccelerated(ptr, dt);
            if ((UsageMask & MaskContact1) != 0) Contact1.PrepareForIterationAccelerated(ptr, dt);
            if ((UsageMask & MaskContact2) != 0) Contact2.PrepareForIterationAccelerated(ptr, dt);
            if ((UsageMask & MaskContact3) != 0) Contact3.PrepareForIterationAccelerated(ptr, dt);
        }
        else
        {
            if ((UsageMask & MaskContact0) != 0) Contact0.PrepareForIteration(ptr, dt);
            if ((UsageMask & MaskContact1) != 0) Contact1.PrepareForIteration(ptr, dt);
            if ((UsageMask & MaskContact2) != 0) Contact2.PrepareForIteration(ptr, dt);
            if ((UsageMask & MaskContact3) != 0) Contact3.PrepareForIteration(ptr, dt);
        }
    }

    public unsafe void Iterate(bool applyBias)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if (Vector128.IsHardwareAccelerated)
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
        Body1 = body1.handle;
        Body2 = body2.handle;

        Friction = MathF.Max(body1.Friction, body2.Friction);
        Restitution = MathF.Max(body1.Restitution, body2.Restitution);

        UsageMask = 0;
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
    public void AddContact(in JVector point1, in JVector point2, in JVector normal, float penetration)
    {
        if ((UsageMask & MaskContactAll) == MaskContactAll)
        {
            // All four contacts are in use. Find one candidate to be replaced by the new one.
            SortCachedPoints(point1, point2, normal, penetration);
            return;
        }

        // Not all contacts are in use, but the new contact point is close enough
        // to an already existing point. Replace this point by the new one.

        if ((UsageMask & MaskContact0) == 0)
        {
            Contact0.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, true, Restitution);
            UsageMask |= MaskContact0;
        }
        else if ((UsageMask & MaskContact1) == 0)
        {
            Contact1.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, true, Restitution);
            UsageMask |= MaskContact1;
        }
        else if ((UsageMask & MaskContact2) == 0)
        {
            Contact2.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, true, Restitution);
            UsageMask |= MaskContact2;
        }
        else if ((UsageMask & MaskContact3) == 0)
        {
            Contact3.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, true, Restitution);
            UsageMask |= MaskContact3;
        }
    }

    private static float CalcArea4Points(in JVector p0, in JVector p1, in JVector p2, in JVector p3)
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

        return MathF.Max(MathF.Max(tmp0.LengthSquared(), tmp1.LengthSquared()), tmp2.LengthSquared());
    }

    private void SortCachedPoints(in JVector point1, in JVector point2, in JVector normal, float penetration)
    {
        JVector.Subtract(point1, Body1.Data.Position, out JVector rp1);

        // calculate 4 possible cases areas, and take the biggest area
        // int maxPenetrationIndex = -1;
        // float maxPenetration = penetration;

        // always prefer the new point
        const float epsilon = -0.0001f;

        float biggestArea = 0;

        ref Contact cref = ref Contact0;
        uint index = 0;

        float clsq = CalcArea4Points(rp1, Contact1.RelativePosition1, Contact2.RelativePosition1, Contact3.RelativePosition1);

        if (clsq > biggestArea + epsilon)
        {
            biggestArea = clsq;
            cref = ref Contact0;
            index = MaskContact0;
        }

        clsq = CalcArea4Points(rp1, Contact0.RelativePosition1, Contact2.RelativePosition1, Contact3.RelativePosition1);

        if (clsq > biggestArea + epsilon)
        {
            biggestArea = clsq;
            cref = ref Contact1;
            index = MaskContact1;
        }

        clsq = CalcArea4Points(rp1, Contact0.RelativePosition1, Contact1.RelativePosition1, Contact3.RelativePosition1);

        if (clsq > biggestArea + epsilon)
        {
            biggestArea = clsq;
            cref = ref Contact2;
            index = MaskContact2;
        }

        clsq = CalcArea4Points(rp1, Contact0.RelativePosition1, Contact1.RelativePosition1, Contact2.RelativePosition1);

        if (clsq > biggestArea + epsilon)
        {
            cref = ref Contact3;
            index = MaskContact3;
        }

        cref.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, false, Restitution);
        UsageMask |= index;
    }

    // ---------------------------------------------------------------------------------------------------------
    public struct Contact
    {
        public const float MaximumBias = 100.0f;
        public const float BiasFactor = 0.2f;
        public const float AllowedPenetration = 0.01f;
        public const float BreakThreshold = 0.02f;

        [Flags]
        public enum Flags
        {
            NewContact = 1 << 1,
        }

        public Flags Flag;
        public float Bias;
        public float PenaltyBias;
        public float Penetration;

        internal Vector128<float> NormalTangentX;
        internal Vector128<float> NormalTangentY;
        internal Vector128<float> NormalTangentZ;
        internal Vector128<float> MassNormalTangent;
        internal Vector128<float> Accumulated;

        [ReferenceFrame(ReferenceFrame.Local)] internal JVector Position1;

        [ReferenceFrame(ReferenceFrame.Local)] internal JVector Position2;

        /// <summary>
        /// Position of the contact relative to the center of mass on the first body.
        /// </summary>
        [ReferenceFrame(ReferenceFrame.World)] public JVector RelativePosition1;

        /// <summary>
        /// Position of the contact relative to the center of mass on the second body.
        /// </summary>
        [ReferenceFrame(ReferenceFrame.World)] public JVector RelativePosition2;

        [ReferenceFrame(ReferenceFrame.World)] public JVector Normal => new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));

        [ReferenceFrame(ReferenceFrame.World)] public JVector Tangent1 => new JVector(NormalTangentX.GetElement(1), NormalTangentY.GetElement(1), NormalTangentZ.GetElement(1));

        [ReferenceFrame(ReferenceFrame.World)] public JVector Tangent2 => new JVector(NormalTangentX.GetElement(2), NormalTangentY.GetElement(2), NormalTangentZ.GetElement(2));

        public float Impulse => Accumulated.GetElement(0);

        public float TangentImpulse1 => Accumulated.GetElement(1);

        public float TangentImpulse2 => Accumulated.GetElement(2);

        public void Initialize(ref RigidBodyData b1, ref RigidBodyData b2, in JVector point1, in JVector point2, in JVector n,
            float penetration, bool newContact, float restitution)
        {
            Debug.Assert(Math.Abs(n.LengthSquared() - 1.0f) < 1e-3);

            JVector.Subtract(point1, b1.Position, out RelativePosition1);
            JVector.Subtract(point2, b2.Position, out RelativePosition2);
            JVector.ConjugatedTransform(RelativePosition1, b1.Orientation, out Position1);
            JVector.ConjugatedTransform(RelativePosition2, b2.Orientation, out Position2);

            Penetration = penetration;

            // Material Properties
            if (!newContact) return;

            Flag = Flags.NewContact;
            Accumulated = Vector128.Create(0.0f);

            JVector dv = b2.Velocity + b2.AngularVelocity % RelativePosition2;
            dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;

            float relNormalVel = JVector.Dot(dv, n);

            Bias = 0;

            // Fake restitution
            if (relNormalVel < -1.0f)
            {
                Bias = -restitution * relNormalVel;
            }

            var tangent1 = dv - n * relNormalVel;

            float num = tangent1.LengthSquared();

            if (num > 1e-12f)
            {
                num = 1.0f / MathF.Sqrt(num);
                tangent1 *= num;
            }
            else
            {
                tangent1 = MathHelper.CreateOrthonormal(n);
            }

            var tangent2 = tangent1 % n;

            NormalTangentX = Vector128.Create(n.X, tangent1.X, tangent2.X, 0);
            NormalTangentY = Vector128.Create(n.Y, tangent1.Y, tangent2.Y, 0);
            NormalTangentZ = Vector128.Create(n.Z, tangent1.Z, tangent2.Z, 0);
        }

        public unsafe bool UpdatePosition(ContactData* cd)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector.Transform(Position1, b1.Orientation, out var relativePos1);
            JVector.Add(relativePos1, b1.Position, out JVector p1);

            JVector.Transform(Position2, b2.Orientation, out var relativePos2);
            JVector.Add(relativePos2, b2.Position, out JVector p2);

            JVector.Subtract(p1, p2, out JVector dist);

            JVector n = new JVector(
                NormalTangentX.GetElement(0),
                NormalTangentY.GetElement(0),
                NormalTangentZ.GetElement(0));

            Penetration = JVector.Dot(dist, n);

            if (Penetration < -BreakThreshold * 0.1f)
            {
                return false;
            }

            dist -= Penetration * n;
            float tangentialOffsetSq = dist.LengthSquared();

            if (tangentialOffsetSq > BreakThreshold * BreakThreshold)
            {
                return false;
            }

            return true;
        }

        // Fallback for missing hardware acceleration
        #region public unsafe void PrepareForIteration(ContactData* cd, float idt)
        public unsafe void PrepareForIteration(ContactData* cd, float idt)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            float accumulatedNormalImpulse = Accumulated.GetElement(0);
            float accumulatedTangentImpulse1 = Accumulated.GetElement(1);
            float accumulatedTangentImpulse2 = Accumulated.GetElement(2);

            var normal = new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));
            var tangent1 = new JVector(NormalTangentX.GetElement(1), NormalTangentY.GetElement(1), NormalTangentZ.GetElement(1));
            var tangent2 = new JVector(NormalTangentX.GetElement(2), NormalTangentY.GetElement(2), NormalTangentZ.GetElement(2));

            JVector.Transform(Position1, b1.Orientation, out RelativePosition1);
            JVector.Transform(Position2, b2.Orientation, out RelativePosition2);
            JVector.Add(RelativePosition1, b1.Position, out JVector p1);
            JVector.Add(RelativePosition2, b2.Position, out JVector p2);
            JVector.Subtract(p1, p2, out JVector dist);

            float inverseMass = b1.InverseMass + b2.InverseMass;

            float kTangent1 = inverseMass;
            float kTangent2 = inverseMass;
            float kNormal = inverseMass;

            if (!cd->IsSpeculative)
            {
                Penetration = JVector.Dot(dist, normal);

                // prepare
                JVector.Cross(RelativePosition1, normal, out JVector tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mN1);

                JVector.Cross(RelativePosition2, normal, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mN2);

                JVector.Cross(RelativePosition1, tangent1, out tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mT1);

                JVector.Cross(RelativePosition2, tangent1, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mT2);

                JVector.Cross(RelativePosition1, tangent2, out tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mTt1);

                JVector.Cross(RelativePosition2, tangent2, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mTt2);

                JVector.Cross(mT1, RelativePosition1, out JVector rantra);
                kTangent1 += JVector.Dot(rantra, tangent1);

                JVector.Cross(mTt1, RelativePosition1, out rantra);
                kTangent2 += JVector.Dot(rantra, tangent2);

                JVector.Cross(mN1, RelativePosition1, out rantra);
                kNormal += JVector.Dot(rantra, normal);

                JVector.Cross(mT2, RelativePosition2, out JVector rbntrb);
                kTangent1 += JVector.Dot(rbntrb, tangent1);

                JVector.Cross(mTt2, RelativePosition2, out rbntrb);
                kTangent2 += JVector.Dot(rbntrb, tangent2);

                JVector.Cross(mN2, RelativePosition2, out rbntrb);
                kNormal += JVector.Dot(rbntrb, normal);

                b1.AngularVelocity -= accumulatedNormalImpulse * mN1 + accumulatedTangentImpulse1 * mT1 +
                                      accumulatedTangentImpulse2 * mTt1;

                b2.AngularVelocity += accumulatedNormalImpulse * mN2 + accumulatedTangentImpulse1 * mT2 +
                                      accumulatedTangentImpulse2 * mTt2;
            }

            float massTangent1 = 1.0f / kTangent1;
            float massTangent2 = 1.0f / kTangent2;
            float massNormal = 1.0f / kNormal;

            MassNormalTangent = Vector128.Create(massNormal, massTangent1, massTangent2, 0);

            if ((Flag & Flags.NewContact) == 0)
            {
                Bias = 0;
            }

            if (Penetration < -BreakThreshold)
            {
                Bias = Penetration * idt;
            }

            PenaltyBias = BiasFactor * idt * Math.Max(0.0f, Penetration - AllowedPenetration);
            PenaltyBias = Math.Clamp(PenaltyBias, 0.0f, MaximumBias);

            JVector impulse = normal * accumulatedNormalImpulse +
                              tangent1 * accumulatedTangentImpulse1 +
                              tangent2 * accumulatedTangentImpulse2;

            b1.Velocity -= impulse * b1.InverseMass;
            b2.Velocity += impulse * b2.InverseMass;

            Flag &= ~Flags.NewContact;
        }
        #endregion

        // Fallback for missing hardware acceleration
        #region public unsafe void Iterate(ContactData* cd, bool applyBias)
        public unsafe void Iterate(ContactData* cd, bool applyBias)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            float massNormal = MassNormalTangent.GetElement(0);
            float massTangent1 = MassNormalTangent.GetElement(1);
            float massTangent2 = MassNormalTangent.GetElement(2);
            float accumulatedNormalImpulse = Accumulated.GetElement(0);
            float accumulatedTangentImpulse1 = Accumulated.GetElement(1);
            float accumulatedTangentImpulse2 = Accumulated.GetElement(2);

            var normal = new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));
            var tangent1 = new JVector(NormalTangentX.GetElement(1), NormalTangentY.GetElement(1), NormalTangentZ.GetElement(1));
            var tangent2 = new JVector(NormalTangentX.GetElement(2), NormalTangentY.GetElement(2), NormalTangentZ.GetElement(2));

            JVector dv = b2.Velocity + b2.AngularVelocity % RelativePosition2;
            dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;

            float vn = JVector.Dot(normal, dv);
            float vt1 = JVector.Dot(tangent1, dv);
            float vt2 = JVector.Dot(tangent2, dv);

            float normalImpulse = -vn;

            if (applyBias) normalImpulse += MathF.Max(PenaltyBias, Bias);
            else normalImpulse += Bias;

            normalImpulse *= massNormal;

            float oldNormalImpulse = accumulatedNormalImpulse;
            accumulatedNormalImpulse = MathF.Max(oldNormalImpulse + normalImpulse, 0.0f);
            normalImpulse = accumulatedNormalImpulse - oldNormalImpulse;

            float maxTangentImpulse = cd->Friction * accumulatedNormalImpulse;
            float tangentImpulse1 = massTangent1 * -vt1;
            float tangentImpulse2 = massTangent2 * -vt2;

            float oldTangentImpulse1 = accumulatedTangentImpulse1;
            accumulatedTangentImpulse1 = oldTangentImpulse1 + tangentImpulse1;
            accumulatedTangentImpulse1 = Math.Clamp(accumulatedTangentImpulse1, -maxTangentImpulse, maxTangentImpulse);
            tangentImpulse1 = accumulatedTangentImpulse1 - oldTangentImpulse1;

            float oldTangentImpulse2 = accumulatedTangentImpulse2;
            accumulatedTangentImpulse2 = oldTangentImpulse2 + tangentImpulse2;
            accumulatedTangentImpulse2 = Math.Clamp(accumulatedTangentImpulse2, -maxTangentImpulse, maxTangentImpulse);
            tangentImpulse2 = accumulatedTangentImpulse2 - oldTangentImpulse2;

            Accumulated = Vector128.Create(accumulatedNormalImpulse, accumulatedTangentImpulse1, accumulatedTangentImpulse2, 0);

            if (!cd->IsSpeculative)
            {
                JVector.Cross(RelativePosition1, normal, out JVector tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mN1);

                JVector.Cross(RelativePosition2, normal, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mN2);

                JVector.Cross(RelativePosition1, tangent1, out tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mT1);

                JVector.Cross(RelativePosition2, tangent1, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mT2);

                JVector.Cross(RelativePosition1, tangent2, out tt);
                JVector.Transform(tt, b1.InverseInertiaWorld, out var mTt1);

                JVector.Cross(RelativePosition2, tangent2, out tt);
                JVector.Transform(tt, b2.InverseInertiaWorld, out var mTt2);

                b1.AngularVelocity -= normalImpulse * mN1 + tangentImpulse1 * mT1 + tangentImpulse2 * mTt1;
                b2.AngularVelocity += normalImpulse * mN2 + tangentImpulse1 * mT2 + tangentImpulse2 * mTt2;
            }

            JVector impulse = normalImpulse * normal + tangentImpulse1 * tangent1 + tangentImpulse2 * tangent2;

            b1.Velocity -= b1.InverseMass * impulse;
            b2.Velocity += b2.InverseMass * impulse;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetSum3(Vector128<float> vector)
        {
            return vector.GetElement(0) + vector.GetElement(1) + vector.GetElement(2);
        }

        public unsafe void PrepareForIterationAccelerated(ContactData* cd, float idt)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector.Transform(Position1, b1.Orientation, out RelativePosition1);
            JVector.Transform(Position2, b2.Orientation, out RelativePosition2);

            Vector128<float> kNormalTangent = Vector128.Create(b1.InverseMass + b2.InverseMass);

            if (!cd->IsSpeculative)
            {
                JVector.Add(RelativePosition1, b1.Position, out JVector r1);
                JVector.Add(RelativePosition2, b2.Position, out JVector r2);
                JVector.Subtract(r1, r2, out JVector dist);

                JVector n = new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));

                Penetration = JVector.Dot(dist, n);

                var rp1X = Vector128.Create(RelativePosition1.X);
                var rp1Y = Vector128.Create(RelativePosition1.Y);
                var rp1Z = Vector128.Create(RelativePosition1.Z);

                var rp2X = Vector128.Create(RelativePosition2.X);
                var rp2Y = Vector128.Create(RelativePosition2.Y);
                var rp2Z = Vector128.Create(RelativePosition2.Z);

                var rrx = Vector128.Subtract(Vector128.Multiply(rp1Y, NormalTangentZ), Vector128.Multiply(rp1Z, NormalTangentY));
                var rry = Vector128.Subtract(Vector128.Multiply(rp1Z, NormalTangentX), Vector128.Multiply(rp1X, NormalTangentZ));
                var rrz = Vector128.Subtract(Vector128.Multiply(rp1X, NormalTangentY), Vector128.Multiply(rp1Y, NormalTangentX));

                var ixx = Vector128.Create(b1.InverseInertiaWorld.M11);
                var ixy = Vector128.Create(b1.InverseInertiaWorld.M21);
                var ixz = Vector128.Create(b1.InverseInertiaWorld.M31);
                var iyy = Vector128.Create(b1.InverseInertiaWorld.M22);
                var iyz = Vector128.Create(b1.InverseInertiaWorld.M23);
                var izz = Vector128.Create(b1.InverseInertiaWorld.M33);

                var e1 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixx, rrx), Vector128.Multiply(ixy, rry)), Vector128.Multiply(ixz, rrz));
                var e2 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixy, rrx), Vector128.Multiply(iyy, rry)), Vector128.Multiply(iyz, rrz));
                var e3 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixz, rrx), Vector128.Multiply(iyz, rry)), Vector128.Multiply(izz, rrz));

                rrx = Vector128.Subtract(Vector128.Multiply(rp2Y, NormalTangentZ), Vector128.Multiply(rp2Z, NormalTangentY));
                rry = Vector128.Subtract(Vector128.Multiply(rp2Z, NormalTangentX), Vector128.Multiply(rp2X, NormalTangentZ));
                rrz = Vector128.Subtract(Vector128.Multiply(rp2X, NormalTangentY), Vector128.Multiply(rp2Y, NormalTangentX));

                ixx = Vector128.Create(b2.InverseInertiaWorld.M11);
                ixy = Vector128.Create(b2.InverseInertiaWorld.M21);
                ixz = Vector128.Create(b2.InverseInertiaWorld.M31);
                iyy = Vector128.Create(b2.InverseInertiaWorld.M22);
                iyz = Vector128.Create(b2.InverseInertiaWorld.M23);
                izz = Vector128.Create(b2.InverseInertiaWorld.M33);

                var f1 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixx, rrx), Vector128.Multiply(ixy, rry)), Vector128.Multiply(ixz, rrz));
                var f2 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixy, rrx), Vector128.Multiply(iyy, rry)), Vector128.Multiply(iyz, rrz));
                var f3 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixz, rrx), Vector128.Multiply(iyz, rry)), Vector128.Multiply(izz, rrz));

                var ktnx = Vector128.Subtract(Vector128.Add(Vector128.Subtract(Vector128.Multiply(e2, rp1Z), Vector128.Multiply(e3, rp1Y)), Vector128.Multiply(f2, rp2Z)), Vector128.Multiply(f3, rp2Y));
                var ktny = Vector128.Subtract(Vector128.Add(Vector128.Subtract(Vector128.Multiply(e3, rp1X), Vector128.Multiply(e1, rp1Z)), Vector128.Multiply(f3, rp2X)), Vector128.Multiply(f1, rp2Z));
                var ktnz = Vector128.Subtract(Vector128.Add(Vector128.Subtract(Vector128.Multiply(e1, rp1Y), Vector128.Multiply(e2, rp1X)), Vector128.Multiply(f1, rp2Y)), Vector128.Multiply(f2, rp2X));

                var kres = Vector128.Add(Vector128.Add(Vector128.Multiply(NormalTangentX, ktnx), Vector128.Multiply(NormalTangentY, ktny)), Vector128.Multiply(NormalTangentZ, ktnz));

                kNormalTangent = Vector128.Add(kNormalTangent, kres);

                Unsafe.SkipInit(out JVector angularImpulse1);
                angularImpulse1.X = GetSum3(Vector128.Multiply(Accumulated, e1));
                angularImpulse1.Y = GetSum3(Vector128.Multiply(Accumulated, e2));
                angularImpulse1.Z = GetSum3(Vector128.Multiply(Accumulated, e3));

                Unsafe.SkipInit(out JVector angularImpulse2);
                angularImpulse2.X = GetSum3(Vector128.Multiply(Accumulated, f1));
                angularImpulse2.Y = GetSum3(Vector128.Multiply(Accumulated, f2));
                angularImpulse2.Z = GetSum3(Vector128.Multiply(Accumulated, f3));

                b1.AngularVelocity -= angularImpulse1;
                b2.AngularVelocity += angularImpulse2;
            }

            MassNormalTangent = Vector128.Divide(Vector128.Create(1.0f), kNormalTangent);

            if ((Flag & Flags.NewContact) == 0)
            {
                Bias = 0;
            }

            if (Penetration < -BreakThreshold)
            {
                // no restitution for speculative contacts
                Bias = Penetration * idt;
            }

            PenaltyBias = BiasFactor * idt * Math.Max(0.0f, Penetration - AllowedPenetration);
            PenaltyBias = Math.Clamp(PenaltyBias, 0.0f, MaximumBias);

            // warm-starting, linear
            Unsafe.SkipInit(out JVector linearImpulse);
            linearImpulse.X = GetSum3(Vector128.Multiply(Accumulated, NormalTangentX));
            linearImpulse.Y = GetSum3(Vector128.Multiply(Accumulated, NormalTangentY));
            linearImpulse.Z = GetSum3(Vector128.Multiply(Accumulated, NormalTangentZ));

            b1.Velocity -= b1.InverseMass * linearImpulse;
            b2.Velocity += b2.InverseMass * linearImpulse;

            Flag &= ~Flags.NewContact;
        }

        public unsafe void IterateAccelerated(ContactData* cd, bool applyBias)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector dv = b2.Velocity + b2.AngularVelocity % RelativePosition2;
            dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;

            var vdots = Vector128.Add(Vector128.Add(Vector128.Multiply(NormalTangentX, Vector128.Create(dv.X)), Vector128.Multiply(NormalTangentY, Vector128.Create(dv.Y))), Vector128.Multiply(NormalTangentZ, Vector128.Create(dv.Z)));

            float bias = applyBias ? MathF.Max(PenaltyBias, Bias) : Bias;

            var impulse = Vector128.Multiply(MassNormalTangent, (Vector128.Subtract(Vector128.Create(bias, 0, 0, 0), vdots)));
            var oldImpulse = Accumulated;

            float maxTangentImpulse = cd->Friction * Accumulated.GetElement(0);

            Accumulated = Vector128.Add(oldImpulse, impulse);

            var minImpulse = Vector128.Create(0, -maxTangentImpulse, -maxTangentImpulse, 0);
            var maxImpulse = Vector128.Create(float.MaxValue, maxTangentImpulse, maxTangentImpulse, 0);

            Accumulated = Vector128.Min(Vector128.Max(Accumulated, minImpulse), maxImpulse);
            impulse = Vector128.Subtract(Accumulated, oldImpulse);

            if (!cd->IsSpeculative)
            {
                var rp1X = Vector128.Create(RelativePosition1.X);
                var rp1Y = Vector128.Create(RelativePosition1.Y);
                var rp1Z = Vector128.Create(RelativePosition1.Z);

                var rp2X = Vector128.Create(RelativePosition2.X);
                var rp2Y = Vector128.Create(RelativePosition2.Y);
                var rp2Z = Vector128.Create(RelativePosition2.Z);

                var rrx = Vector128.Subtract(Vector128.Multiply(rp1Y, NormalTangentZ), Vector128.Multiply(rp1Z, NormalTangentY));
                var rry = Vector128.Subtract(Vector128.Multiply(rp1Z, NormalTangentX), Vector128.Multiply(rp1X, NormalTangentZ));
                var rrz = Vector128.Subtract(Vector128.Multiply(rp1X, NormalTangentY), Vector128.Multiply(rp1Y, NormalTangentX));

                var ixx = Vector128.Create(b1.InverseInertiaWorld.M11);
                var ixy = Vector128.Create(b1.InverseInertiaWorld.M21);
                var ixz = Vector128.Create(b1.InverseInertiaWorld.M31);
                var iyy = Vector128.Create(b1.InverseInertiaWorld.M22);
                var iyz = Vector128.Create(b1.InverseInertiaWorld.M23);
                var izz = Vector128.Create(b1.InverseInertiaWorld.M33);

                var e1 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixx, rrx), Vector128.Multiply(ixy, rry)), Vector128.Multiply(ixz, rrz));
                var e2 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixy, rrx), Vector128.Multiply(iyy, rry)), Vector128.Multiply(iyz, rrz));
                var e3 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixz, rrx), Vector128.Multiply(iyz, rry)), Vector128.Multiply(izz, rrz));

                rrx = Vector128.Subtract(Vector128.Multiply(rp2Y, NormalTangentZ), Vector128.Multiply(rp2Z, NormalTangentY));
                rry = Vector128.Subtract(Vector128.Multiply(rp2Z, NormalTangentX), Vector128.Multiply(rp2X, NormalTangentZ));
                rrz = Vector128.Subtract(Vector128.Multiply(rp2X, NormalTangentY), Vector128.Multiply(rp2Y, NormalTangentX));

                ixx = Vector128.Create(b2.InverseInertiaWorld.M11);
                ixy = Vector128.Create(b2.InverseInertiaWorld.M21);
                ixz = Vector128.Create(b2.InverseInertiaWorld.M31);
                iyy = Vector128.Create(b2.InverseInertiaWorld.M22);
                iyz = Vector128.Create(b2.InverseInertiaWorld.M23);
                izz = Vector128.Create(b2.InverseInertiaWorld.M33);

                var f1 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixx, rrx), Vector128.Multiply(ixy, rry)), Vector128.Multiply(ixz, rrz));
                var f2 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixy, rrx), Vector128.Multiply(iyy, rry)), Vector128.Multiply(iyz, rrz));
                var f3 = Vector128.Add(Vector128.Add(Vector128.Multiply(ixz, rrx), Vector128.Multiply(iyz, rry)), Vector128.Multiply(izz, rrz));

                Unsafe.SkipInit(out JVector angularImpulse1);
                angularImpulse1.X = GetSum3(Vector128.Multiply(impulse, e1));
                angularImpulse1.Y = GetSum3(Vector128.Multiply(impulse, e2));
                angularImpulse1.Z = GetSum3(Vector128.Multiply(impulse, e3));

                Unsafe.SkipInit(out JVector angularImpulse2);
                angularImpulse2.X = GetSum3(Vector128.Multiply(impulse, f1));
                angularImpulse2.Y = GetSum3(Vector128.Multiply(impulse, f2));
                angularImpulse2.Z = GetSum3(Vector128.Multiply(impulse, f3));

                b1.AngularVelocity -= angularImpulse1;
                b2.AngularVelocity += angularImpulse2;
            }

            Unsafe.SkipInit(out JVector linearImpulse);
            linearImpulse.X = GetSum3(Vector128.Multiply(impulse, NormalTangentX));
            linearImpulse.Y = GetSum3(Vector128.Multiply(impulse, NormalTangentY));
            linearImpulse.Z = GetSum3(Vector128.Multiply(impulse, NormalTangentZ));

            b1.Velocity -= b1.InverseMass * linearImpulse;
            b2.Velocity += b2.InverseMass * linearImpulse;
        }
    }
}