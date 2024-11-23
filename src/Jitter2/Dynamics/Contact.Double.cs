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


// Creating two of these Contact scripts one for Double and one for Float isn't really great
// But its the best way I could find that would allow me to keep the original float performance


#if USE_DOUBLE_PRECISION

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    /// <see cref="World.Step(double, bool)"/> and no intact contact after the call. However, the correspondig bit for the
    /// solver-phase will be set in this scenario.
    /// </example>
    public uint UsageMask;

    public JHandle<RigidBodyData> Body1;
    public JHandle<RigidBodyData> Body2;

    public ArbiterKey Key;

    public double Restitution;
    public double Friction;

    public bool IsSpeculative;

    public Contact Contact0;
    public Contact Contact1;
    public Contact Contact2;
    public Contact Contact3;

    public unsafe void PrepareForIteration(double dt)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if ((UsageMask & MaskContact0) != 0) Contact0.PrepareForIteration(ptr, dt);
        if ((UsageMask & MaskContact1) != 0) Contact1.PrepareForIteration(ptr, dt);
        if ((UsageMask & MaskContact2) != 0) Contact2.PrepareForIteration(ptr, dt);
        if ((UsageMask & MaskContact3) != 0) Contact3.PrepareForIteration(ptr, dt);
    }

    public unsafe void Iterate(bool applyBias)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if ((UsageMask & MaskContact0) != 0) Contact0.Iterate(ptr, applyBias);
        if ((UsageMask & MaskContact1) != 0) Contact1.Iterate(ptr, applyBias);
        if ((UsageMask & MaskContact2) != 0) Contact2.Iterate(ptr, applyBias);
        if ((UsageMask & MaskContact3) != 0) Contact3.Iterate(ptr, applyBias);
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

        Friction = Math.Max(body1.Friction, body2.Friction);
        Restitution = Math.Max(body1.Restitution, body2.Restitution);

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
    // https://github.com/bulletphysics/bullet3/blob/39b8de74df93721add193e5b3d9ebee579aebf8/
    // src/Bullet3OpenCL/NarrowphaseCollision/b3ContactCache.cpp

    /// <summary>
    /// Adds a new collision result to the contact manifold. Keeps at most four points.
    /// </summary>
    public unsafe void AddContact(in JVector point1, in JVector point2, in JVector normal, double penetration)
    {
        if ((UsageMask & MaskContactAll) == MaskContactAll)
        {
            // All four contacts are in use. Find one candidate to be replaced by the new one.
            SortCachedPoints(point1, point2, normal, penetration);
            return;
        }

        // Not all contacts are in use, but the new contact point is close enough
        // to an already existing point. Replace this point by the new one.

        Contact* closest = (Contact*)IntPtr.Zero;
        double distanceSq = double.MaxValue;

        JVector relP1 = point1 - Body1.Data.Position;

        if ((UsageMask & MaskContact0) != 0)
        {
            double distSq = (Contact0.RelativePosition1 - relP1).LengthSquared();
            if (distSq < distanceSq)
            {
                distanceSq = distSq;
                closest = (Contact*)Unsafe.AsPointer(ref Contact0);
            }
        }

        if ((UsageMask & MaskContact1) != 0)
        {
            double distSq = (Contact1.RelativePosition1 - relP1).LengthSquared();
            if (distSq < distanceSq)
            {
                distanceSq = distSq;
                closest = (Contact*)Unsafe.AsPointer(ref Contact1);
            }
        }

        if ((UsageMask & MaskContact2) != 0)
        {
            double distSq = (Contact2.RelativePosition1 - relP1).LengthSquared();
            if (distSq < distanceSq)
            {
                distanceSq = distSq;
                closest = (Contact*)Unsafe.AsPointer(ref Contact2);
            }
        }

        if ((UsageMask & MaskContact3) != 0)
        {
            double distSq = (Contact3.RelativePosition1 - relP1).LengthSquared();
            if (distSq < distanceSq)
            {
                distanceSq = distSq;
                closest = (Contact*)Unsafe.AsPointer(ref Contact3);
            }
        }

        if (distanceSq < Contact.BreakThreshold * Contact.BreakThreshold)
        {
            closest->Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, false, Restitution);
            return;
        }

        // It is a completely new contact.

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

    private static double CalcArea4Points(in JVector p0, in JVector p1, in JVector p2, in JVector p3)
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

        return Math.Max(Math.Max(tmp0.LengthSquared(), tmp1.LengthSquared()), tmp2.LengthSquared());
    }

    private void SortCachedPoints(in JVector point1, in JVector point2, in JVector normal, double penetration)
    {
        JVector.Subtract(point1, Body1.Data.Position, out JVector rp1);

        // calculate 4 possible cases areas, and take the biggest area
        // int maxPenetrationIndex = -1;
        // double maxPenetration = penetration;

        // always prefer the new point
        const double epsilon = -0.0001;

        double biggestArea = 0;

        ref Contact cref = ref Contact0;
        uint index = 0;

        double clsq = CalcArea4Points(rp1, Contact1.RelativePosition1, Contact2.RelativePosition1, Contact3.RelativePosition1);

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

    [StructLayout(LayoutKind.Sequential)] // Changing this to Sequential seems to work
    public struct Contact
    {
        public const double MaximumBias = 100.0;
        public const double BiasFactor = 0.2;
        public const double AllowedPenetration = 0.01;
        public const double BreakThreshold = 0.02;

        [Flags]
        public enum Flags
        {
            NewContact = 1 << 1,
        }

        public Flags Flag;

        public double Bias;

        public double PenaltyBias;

        public double Penetration;

        internal JVector NormalTangentX;

        internal JVector NormalTangentY;

        internal JVector NormalTangentZ;

        internal JVector MassNormalTangent;

        internal JVector Accumulated;

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

        [ReferenceFrame(ReferenceFrame.World)] public JVector Normal => new JVector(NormalTangentX.X, NormalTangentY.X, NormalTangentZ.X);

        [ReferenceFrame(ReferenceFrame.World)] public JVector Tangent1 => new JVector(NormalTangentX.Y, NormalTangentY.Y, NormalTangentZ.Y);

        [ReferenceFrame(ReferenceFrame.World)] public JVector Tangent2 => new JVector(NormalTangentX.Z, NormalTangentY.Z, NormalTangentZ.Z);

        public double Impulse => Accumulated.X;

        public double TangentImpulse1 => Accumulated.Y;

        public double TangentImpulse2 => Accumulated.Z;

        public void Initialize(ref RigidBodyData b1, ref RigidBodyData b2, in JVector point1, in JVector point2, in JVector n,
            double penetration, bool newContact, double restitution)
        {
            Debug.Assert(Math.Abs(n.LengthSquared() - 1.0) < 1e-3);

            JVector.Subtract(point1, b1.Position, out RelativePosition1);
            JVector.Subtract(point2, b2.Position, out RelativePosition2);
            JVector.ConjugatedTransform(RelativePosition1, b1.Orientation, out Position1);
            JVector.ConjugatedTransform(RelativePosition2, b2.Orientation, out Position2);

            Penetration = penetration;

            // Material Properties
            if (!newContact) return;

            Flag = Flags.NewContact;
            Accumulated = new JVector(0.0);

            JVector dv = b2.Velocity + b2.AngularVelocity % RelativePosition2;
            dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;

            double relNormalVel = JVector.Dot(dv, n);

            Bias = 0;

            // Fake restitution
            if (relNormalVel < -1.0)
            {
                Bias = -restitution * relNormalVel;
            }

            var tangent1 = dv - n * relNormalVel;

            double num = tangent1.LengthSquared();

            if (num > 1e-12)
            {
                num = 1.0 / Math.Sqrt(num);
                tangent1 *= num;
            }
            else
            {
                tangent1 = MathHelper.CreateOrthonormal(n);
            }

            var tangent2 = tangent1 % n;

            NormalTangentX = new JVector(n.X, tangent1.X, tangent2.X);
            NormalTangentY = new JVector(n.Y, tangent1.Y, tangent2.Y);
            NormalTangentZ = new JVector(n.Z, tangent1.Z, tangent2.Z);
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
                NormalTangentX.X,
                NormalTangentY.X,
                NormalTangentZ.X);

            Penetration = JVector.Dot(dist, n);

            if (Penetration < -BreakThreshold * 0.1)
            {
                return false;
            }

            dist -= Penetration * n;
            double tangentialOffsetSq = dist.LengthSquared();

            if (tangentialOffsetSq > BreakThreshold * BreakThreshold)
            {
                return false;
            }

            return true;
        }

        // Fallback for missing hardware acceleration
        #region public unsafe void PrepareForIteration(ContactData* cd, double idt)
        public unsafe void PrepareForIteration(ContactData* cd, double idt)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            double accumulatedNormalImpulse = Accumulated.X;
            double accumulatedTangentImpulse1 = Accumulated.Y;
            double accumulatedTangentImpulse2 = Accumulated.Z;

            var normal = new JVector(NormalTangentX.X, NormalTangentY.X, NormalTangentZ.X);
            var tangent1 = new JVector(NormalTangentX.Y, NormalTangentY.Y, NormalTangentZ.Y);
            var tangent2 = new JVector(NormalTangentX.Z, NormalTangentY.Z, NormalTangentZ.Z);

            JVector.Transform(Position1, b1.Orientation, out RelativePosition1);
            JVector.Transform(Position2, b2.Orientation, out RelativePosition2);
            JVector.Add(RelativePosition1, b1.Position, out JVector p1);
            JVector.Add(RelativePosition2, b2.Position, out JVector p2);
            JVector.Subtract(p1, p2, out JVector dist);

            double inverseMass = b1.InverseMass + b2.InverseMass;

            double kTangent1 = inverseMass;
            double kTangent2 = inverseMass;
            double kNormal = inverseMass;

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

            double massTangent1 = 1.0 / kTangent1;
            double massTangent2 = 1.0 / kTangent2;
            double massNormal = 1.0 / kNormal;

            JVector mass = new JVector(massNormal, massTangent1, massTangent2);
            Unsafe.CopyBlock(Unsafe.AsPointer(ref MassNormalTangent), Unsafe.AsPointer(ref mass), 24);

            if ((Flag & Flags.NewContact) == 0)
            {
                Bias = 0;
            }

            if (Penetration < -BreakThreshold)
            {
                Bias = Penetration * idt;
            }

            PenaltyBias = BiasFactor * idt * Math.Max(0.0, Penetration - AllowedPenetration);
            PenaltyBias = Math.Clamp(PenaltyBias, 0.0, MaximumBias);

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

            double massNormal = MassNormalTangent.X;
            double massTangent1 = MassNormalTangent.Y;
            double massTangent2 = MassNormalTangent.Z;
            double accumulatedNormalImpulse = Accumulated.X;
            double accumulatedTangentImpulse1 = Accumulated.Y;
            double accumulatedTangentImpulse2 = Accumulated.Z;

            var normal = new JVector(NormalTangentX.X, NormalTangentY.X, NormalTangentZ.X);
            var tangent1 = new JVector(NormalTangentX.Y, NormalTangentY.Y, NormalTangentZ.Y);
            var tangent2 = new JVector(NormalTangentX.Z, NormalTangentY.Z, NormalTangentZ.Z);

            JVector dv = b2.Velocity + b2.AngularVelocity % RelativePosition2;
            dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;

            double vn = JVector.Dot(normal, dv);
            double vt1 = JVector.Dot(tangent1, dv);
            double vt2 = JVector.Dot(tangent2, dv);

            double normalImpulse = -vn;

            if (applyBias) normalImpulse += Math.Max(PenaltyBias, Bias);
            else normalImpulse += Bias;

            normalImpulse *= massNormal;

            double oldNormalImpulse = accumulatedNormalImpulse;
            accumulatedNormalImpulse = Math.Max(oldNormalImpulse + normalImpulse, 0.0);
            normalImpulse = accumulatedNormalImpulse - oldNormalImpulse;

            double maxTangentImpulse = cd->Friction * accumulatedNormalImpulse;
            double tangentImpulse1 = massTangent1 * -vt1;
            double tangentImpulse2 = massTangent2 * -vt2;

            double oldTangentImpulse1 = accumulatedTangentImpulse1;
            accumulatedTangentImpulse1 = oldTangentImpulse1 + tangentImpulse1;
            accumulatedTangentImpulse1 = Math.Clamp(accumulatedTangentImpulse1, -maxTangentImpulse, maxTangentImpulse);
            tangentImpulse1 = accumulatedTangentImpulse1 - oldTangentImpulse1;

            double oldTangentImpulse2 = accumulatedTangentImpulse2;
            accumulatedTangentImpulse2 = oldTangentImpulse2 + tangentImpulse2;
            accumulatedTangentImpulse2 = Math.Clamp(accumulatedTangentImpulse2, -maxTangentImpulse, maxTangentImpulse);
            tangentImpulse2 = accumulatedTangentImpulse2 - oldTangentImpulse2;

            Accumulated = new JVector(accumulatedNormalImpulse, accumulatedTangentImpulse1, accumulatedTangentImpulse2);

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
    }
}

#endif