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
public struct ContactData
{
#pragma warning disable CS0649
    // Accessed in unsafe code.
    internal int _internal;
#pragma warning restore CS0649

    public int UsageMask;

    public JHandle<RigidBodyData> Body1;
    public JHandle<RigidBodyData> Body2;

    public ArbiterKey Key;

    private float Friction;
    private float Restitution;

    public bool IsSpeculative;

    public Contact Contact0;
    public Contact Contact1;
    public Contact Contact2;
    public Contact Contact3;

    public void PrepareForIteration(float dt)
    {
        if ((UsageMask & 0b0001) != 0) Contact0.PrepareForIteration(ref Body1.Data, ref Body2.Data, dt, IsSpeculative);
        if ((UsageMask & 0b0010) != 0) Contact1.PrepareForIteration(ref Body1.Data, ref Body2.Data, dt, IsSpeculative);
        if ((UsageMask & 0b0100) != 0) Contact2.PrepareForIteration(ref Body1.Data, ref Body2.Data, dt, IsSpeculative);
        if ((UsageMask & 0b1000) != 0) Contact3.PrepareForIteration(ref Body1.Data, ref Body2.Data, dt, IsSpeculative);
    }

    public void Iterate()
    {
        if ((UsageMask & 0b0001) != 0) Contact0.Iterate(ref Body1.Data, ref Body2.Data, IsSpeculative);
        if ((UsageMask & 0b0010) != 0) Contact1.Iterate(ref Body1.Data, ref Body2.Data, IsSpeculative);
        if ((UsageMask & 0b0100) != 0) Contact2.Iterate(ref Body1.Data, ref Body2.Data, IsSpeculative);
        if ((UsageMask & 0b1000) != 0) Contact3.Iterate(ref Body1.Data, ref Body2.Data, IsSpeculative);
    }

    public int UpdatePosition()
    {
        int delc = 0;
        if ((UsageMask & 0b0001) != 0)
        {
            if (!Contact0.UpdatePosition(ref Body1.Data, ref Body2.Data))
            {
                delc++;
                UsageMask &= ~(1 << 0);
            }
        }

        if ((UsageMask & 0b0010) != 0)
        {
            if (!Contact1.UpdatePosition(ref Body1.Data, ref Body2.Data))
            {
                delc++;
                UsageMask &= ~(1 << 1);
            }
        }

        if ((UsageMask & 0b0100) != 0)
        {
            if (!Contact2.UpdatePosition(ref Body1.Data, ref Body2.Data))
            {
                delc++;
                UsageMask &= ~(1 << 2);
            }
        }

        if ((UsageMask & 0b1000) != 0)
        {
            if (!Contact3.UpdatePosition(ref Body1.Data, ref Body2.Data))
            {
                delc++;
                UsageMask &= ~(1 << 3);
            }
        }

        return delc;
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
        if (UsageMask == 0b1111)
        {
            // All four contacts are in use. Find one candidate to be replaced by the new one.
            SortCachedPoints(point1, point2, normal, penetration);
            return;
        }

        // Not all contacts are in use, but the new contact point is close enough
        // to an already existing point. Replace this point by the new one.

        // Neither of the above.
        if ((UsageMask & 0b0001) == 0)
        {
            Contact0.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, true, Restitution, Friction);
            UsageMask |= 1 << 0;
        }
        else if ((UsageMask & 0b0010) == 0)
        {
            Contact1.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, true, Restitution, Friction);
            UsageMask |= 1 << 1;
        }
        else if ((UsageMask & 0b0100) == 0)
        {
            Contact2.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, true, Restitution, Friction);
            UsageMask |= 1 << 2;
        }
        else if ((UsageMask & 0b1000) == 0)
        {
            Contact3.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, true, Restitution, Friction);
            UsageMask |= 1 << 3;
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

        // calculate 4 possible cases areas, and take biggest area
        // int maxPenetrationIndex = -1;
        // float maxPenetration = penetration;

        // always prefer the new point
        const float epsilon = -0.0001f;

        float biggestArea = 0;

        ref Contact cref = ref Contact0;
        int index = -1;

        float clsq = CalcArea4Points(rp1,
            Contact1.RelativePos1,
            Contact2.RelativePos1,
            Contact3.RelativePos1);

        if (clsq > biggestArea + epsilon)
        {
            biggestArea = clsq;
            cref = ref Contact0;
            index = 0;
        }

        clsq = CalcArea4Points(rp1,
            Contact0.RelativePos1,
            Contact2.RelativePos1,
            Contact3.RelativePos1);

        if (clsq > biggestArea + epsilon)
        {
            biggestArea = clsq;
            cref = ref Contact1;
            index = 1;
        }

        clsq = CalcArea4Points(rp1,
            Contact0.RelativePos1,
            Contact1.RelativePos1,
            Contact3.RelativePos1);

        if (clsq > biggestArea + epsilon)
        {
            biggestArea = clsq;
            cref = ref Contact2;
            index = 2;
        }

        clsq = CalcArea4Points(rp1,
            Contact0.RelativePos1,
            Contact1.RelativePos1,
            Contact2.RelativePos1);

        if (clsq > biggestArea + epsilon)
        {
            // not necessary: biggestArea = clsq;
            cref = ref Contact3;
            index = 3;
        }

        cref.Initialize(ref Body1.Data, ref Body2.Data, point1, point2, normal, penetration, false, Restitution, Friction);
        UsageMask |= 1 << index;
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
            Body1Static = 1 << 2,
            Body2Static = 1 << 3
        }

        public float Friction;
        public float Penetration;
        public float RestitutionBias;
        public float Bias;

        internal Vector128<float> NormalTangentX;
        internal Vector128<float> NormalTangentY;
        internal Vector128<float> NormalTangentZ;
        internal Vector128<float> MassNormalTangent;
        internal Vector128<float> Accumulated;

        internal JVector RealRelPos1;
        internal JVector RealRelPos2;

        public JVector RelativePos1;
        public JVector RelativePos2;

        public Flags Flag;

        public JVector Normal => new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));
        public JVector Tangent1 => new JVector(NormalTangentX.GetElement(1), NormalTangentY.GetElement(1), NormalTangentZ.GetElement(1));
        public JVector Tangent2 => new JVector(NormalTangentX.GetElement(2), NormalTangentY.GetElement(2), NormalTangentZ.GetElement(2));

        public float AccumulatedNormalImpulse => Accumulated.GetElement(0);
        public float AccumulatedTangent1Impulse => Accumulated.GetElement(1);
        public float AccumulatedTangent2Impulse => Accumulated.GetElement(2);

        public void Initialize(ref RigidBodyData b1, ref RigidBodyData b2, in JVector point1, in JVector point2, in JVector n,
            float penetration, bool newContact, float restitution, float friction)
        {
            Debug.Assert(Math.Abs(n.LengthSquared() - 1.0f) < 1e-3);

            JVector.Subtract(point1, b1.Position, out RelativePos1);
            JVector.Subtract(point2, b2.Position, out RelativePos2);
            JVector.ConjugatedTransform(RelativePos1, b1.Orientation, out RealRelPos1);
            JVector.ConjugatedTransform(RelativePos2, b2.Orientation, out RealRelPos2);

            Penetration = penetration;

            // Material Properties
            if (newContact)
            {
                Flag = Flags.NewContact;
                if (b1.IsStatic || !b1.IsActive) Flag |= Flags.Body1Static;
                if (b2.IsStatic || !b2.IsActive) Flag |= Flags.Body2Static;

                Accumulated = Vector128.Create(0.0f);

                JVector dv = b2.Velocity + b2.AngularVelocity % RelativePos2;
                dv -= b1.Velocity + b1.AngularVelocity % RelativePos1;

                float relNormalVel = JVector.Dot(dv, n);

                // Fake restitution
                if (relNormalVel < -1.0f && (Flag & Flags.NewContact) != 0)
                {
                    Bias = Math.Max(-restitution * relNormalVel, Bias);
                }

                var Tangent1 = dv - n * relNormalVel;

                float num = Tangent1.LengthSquared();

                if (num > 1e-12f)
                {
                    num = 1.0f / (float)Math.Sqrt(num);
                    Tangent1 *= num;
                }
                else
                {
                    Tangent1 = MathHelper.CreateOrthonormal(n);
                }

                var Tangent2 = Tangent1 % n;

                NormalTangentX = Vector128.Create(n.X, Tangent1.X, Tangent2.X, 0);
                NormalTangentY = Vector128.Create(n.Y, Tangent1.Y, Tangent2.Z, 0);
                NormalTangentZ = Vector128.Create(n.Z, Tangent1.Z, Tangent2.Z, 0);

                RestitutionBias = 0.0f;

                // Fake restitution
                if (relNormalVel < -1.0f)
                {
                    RestitutionBias = -restitution * relNormalVel;
                }

                Friction = friction;
            }
        }

        public bool UpdatePosition(ref RigidBodyData b1, ref RigidBodyData b2)
        {
            JVector.Transform(RealRelPos1, b1.Orientation, out RelativePos1);
            JVector.Add(RelativePos1, b1.Position, out JVector p1);

            JVector.Transform(RealRelPos2, b2.Orientation, out RelativePos2);
            JVector.Add(RelativePos2, b2.Position, out JVector p2);

            JVector.Subtract(p1, p2, out JVector dist);

            JVector n = new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));

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

        public void PrepareForIteration(ref RigidBodyData b1, ref RigidBodyData b2,
            float idt, bool speculative = false)
        {
            if ((Flag & Flags.NewContact) != 0) Flag = Flags.NewContact;
            else Flag = 0;

            // redundant if contact has just been initialized or updateposition was called
            // before <=> it is redundant the first time it is called in world.step <=> it is
            // redundant if no sub-stepping is used. but it does not seem to slow anything down,
            // since we are memory bound anyway.
            JVector.Transform(RealRelPos1, b1.Orientation, out RelativePos1);
            JVector.Transform(RealRelPos2, b2.Orientation, out RelativePos2);
            JVector.Add(RelativePos1, b1.Position, out JVector p1);
            JVector.Add(RelativePos2, b2.Position, out JVector p2);
            JVector.Subtract(p1, p2, out JVector dist);

            JVector n = new JVector(NormalTangentX.GetElement(0), NormalTangentY.GetElement(0), NormalTangentZ.GetElement(0));

            if (!speculative) Penetration = JVector.Dot(dist, n);

            Vector128<float> e1, e2, e3;
            Vector128<float> f1, f2, f3;

            if (!speculative)
            {
                var rrx = Vector128.Subtract(Vector128.Multiply(RelativePos1.Y, NormalTangentZ), Vector128.Multiply(RelativePos1.Z, NormalTangentY));
                var rry = Vector128.Subtract(Vector128.Multiply(RelativePos1.Z, NormalTangentX), Vector128.Multiply(RelativePos1.X, NormalTangentZ));
                var rrz = Vector128.Subtract(Vector128.Multiply(RelativePos1.X, NormalTangentY), Vector128.Multiply(RelativePos1.Y, NormalTangentX));

                e1 = b1.InverseInertiaWorld.M11 * rrx + b1.InverseInertiaWorld.M12 * rry + b1.InverseInertiaWorld.M13 * rrz;
                e2 = b1.InverseInertiaWorld.M21 * rrx + b1.InverseInertiaWorld.M22 * rry + b1.InverseInertiaWorld.M23 * rrz;
                e3 = b1.InverseInertiaWorld.M31 * rrx + b1.InverseInertiaWorld.M32 * rry + b1.InverseInertiaWorld.M33 * rrz;

                rrx = Vector128.Subtract(Vector128.Multiply(RelativePos2.Y, NormalTangentZ), Vector128.Multiply(RelativePos2.Z, NormalTangentY));
                rry = Vector128.Subtract(Vector128.Multiply(RelativePos2.Z, NormalTangentX), Vector128.Multiply(RelativePos2.X, NormalTangentZ));
                rrz = Vector128.Subtract(Vector128.Multiply(RelativePos2.X, NormalTangentY), Vector128.Multiply(RelativePos2.Y, NormalTangentX));

                f1 = b2.InverseInertiaWorld.M11 * rrx + b2.InverseInertiaWorld.M12 * rry + b2.InverseInertiaWorld.M13 * rrz;
                f2 = b2.InverseInertiaWorld.M21 * rrx + b2.InverseInertiaWorld.M22 * rry + b2.InverseInertiaWorld.M23 * rrz;
                f3 = b2.InverseInertiaWorld.M31 * rrx + b2.InverseInertiaWorld.M32 * rry + b2.InverseInertiaWorld.M33 * rrz;
            }
            else
            {
                e1 = Vector128.Create(0.0f);
                e2 = Vector128.Create(0.0f);
                e3 = Vector128.Create(0.0f);
                f1 = Vector128.Create(0.0f);
                f2 = Vector128.Create(0.0f);
                f3 = Vector128.Create(0.0f);
            }

            Vector128<float> kNormalTangent = Vector128.Create(b1.InverseMass + b2.InverseMass);

            var ktnx = e2 * RelativePos1.Z - e3 * RelativePos1.Y + f2 * RelativePos2.Z - f3 * RelativePos2.Y;
            var knty = e3 * RelativePos1.X - e1 * RelativePos1.Z + f3 * RelativePos2.X - f1 * RelativePos2.Z;
            var kntz = e1 * RelativePos1.Y - e2 * RelativePos1.X + f1 * RelativePos2.Y - f2 * RelativePos2.X;

            var kres = NormalTangentX * ktnx + NormalTangentY * knty + NormalTangentZ * kntz;
            kNormalTangent += kres;

            MassNormalTangent = Vector128.Divide(Vector128.Create(1.0f), kNormalTangent);

            Bias = RestitutionBias;
            RestitutionBias = 0;

            if (Penetration > AllowedPenetration)
            {
                Bias = Math.Max(Bias, BiasFactor * idt * Math.Max(0.0f, Penetration - AllowedPenetration));
                Bias = Math.Clamp(Bias, 0.0f, MaximumBias);
            }

            // warmstarting
            Unsafe.SkipInit(out JVector fimpulse);
            fimpulse.X = Vector128.Sum(Vector128.Multiply(Accumulated, NormalTangentX));
            fimpulse.Y = Vector128.Sum(Vector128.Multiply(Accumulated, NormalTangentY));
            fimpulse.Z = Vector128.Sum(Vector128.Multiply(Accumulated, NormalTangentZ));

            Unsafe.SkipInit(out JVector aimpulse1);
            aimpulse1.X = Vector128.Sum(Vector128.Multiply(Accumulated, e1));
            aimpulse1.Y = Vector128.Sum(Vector128.Multiply(Accumulated, e2));
            aimpulse1.Z = Vector128.Sum(Vector128.Multiply(Accumulated, e3));

            Unsafe.SkipInit(out JVector aimpulse2);
            aimpulse2.X = Vector128.Sum(Vector128.Multiply(Accumulated, f1));
            aimpulse2.Y = Vector128.Sum(Vector128.Multiply(Accumulated, f2));
            aimpulse2.Z = Vector128.Sum(Vector128.Multiply(Accumulated, f3));

            b1.Velocity -= fimpulse * b1.InverseMass;
            b1.AngularVelocity -= aimpulse1;

            b2.Velocity += fimpulse * b2.InverseMass;
            b2.AngularVelocity += aimpulse2;

            Flag &= ~Flags.NewContact;
        }

        public void Iterate(ref RigidBodyData b1, ref RigidBodyData b2, bool speculative)
        {
            JVector dv = b2.Velocity + b2.AngularVelocity % RelativePos2;
            dv -= b1.Velocity + b1.AngularVelocity % RelativePos1;

            Vector128<float> e1, e2, e3;
            Vector128<float> f1, f2, f3;

            if (!speculative)
            {
                var rrx = Vector128.Subtract(Vector128.Multiply(RelativePos1.Y, NormalTangentZ), Vector128.Multiply(RelativePos1.Z, NormalTangentY));
                var rry = Vector128.Subtract(Vector128.Multiply(RelativePos1.Z, NormalTangentX), Vector128.Multiply(RelativePos1.X, NormalTangentZ));
                var rrz = Vector128.Subtract(Vector128.Multiply(RelativePos1.X, NormalTangentY), Vector128.Multiply(RelativePos1.Y, NormalTangentX));

                e1 = b1.InverseInertiaWorld.M11 * rrx + b1.InverseInertiaWorld.M12 * rry + b1.InverseInertiaWorld.M13 * rrz;
                e2 = b1.InverseInertiaWorld.M21 * rrx + b1.InverseInertiaWorld.M22 * rry + b1.InverseInertiaWorld.M23 * rrz;
                e3 = b1.InverseInertiaWorld.M31 * rrx + b1.InverseInertiaWorld.M32 * rry + b1.InverseInertiaWorld.M33 * rrz;

                rrx = Vector128.Subtract(Vector128.Multiply(RelativePos2.Y, NormalTangentZ), Vector128.Multiply(RelativePos2.Z, NormalTangentY));
                rry = Vector128.Subtract(Vector128.Multiply(RelativePos2.Z, NormalTangentX), Vector128.Multiply(RelativePos2.X, NormalTangentZ));
                rrz = Vector128.Subtract(Vector128.Multiply(RelativePos2.X, NormalTangentY), Vector128.Multiply(RelativePos2.Y, NormalTangentX));

                f1 = b2.InverseInertiaWorld.M11 * rrx + b2.InverseInertiaWorld.M12 * rry + b2.InverseInertiaWorld.M13 * rrz;
                f2 = b2.InverseInertiaWorld.M21 * rrx + b2.InverseInertiaWorld.M22 * rry + b2.InverseInertiaWorld.M23 * rrz;
                f3 = b2.InverseInertiaWorld.M31 * rrx + b2.InverseInertiaWorld.M32 * rry + b2.InverseInertiaWorld.M33 * rrz;
            }
            else
            {
                e1 = Vector128.Create(0.0f);
                e2 = Vector128.Create(0.0f);
                e3 = Vector128.Create(0.0f);
                f1 = Vector128.Create(0.0f);
                f2 = Vector128.Create(0.0f);
                f3 = Vector128.Create(0.0f);
            }

            var vdots = NormalTangentX * dv.X + NormalTangentY * dv.Y + NormalTangentZ * dv.Z;

            Vector128<float> biasMinusvdots = Vector128.Create(Bias, 0, 0, 0) - vdots;

            var impulse = MassNormalTangent * biasMinusvdots;
            var oldImpulse = Accumulated;

            float maxTangentImpulse = Friction * Accumulated.GetElement(0);

            Accumulated = oldImpulse + impulse;

            var minImpulse = Vector128.Create(0, -maxTangentImpulse, -maxTangentImpulse, 0);
            var maxImpulse = Vector128.Create(float.MaxValue, maxTangentImpulse, maxTangentImpulse, 0);

            Accumulated = Vector128.Min(Vector128.Max(Accumulated, minImpulse), maxImpulse);
            impulse = Accumulated - oldImpulse;

            Unsafe.SkipInit(out JVector fimpulse);
            fimpulse.X = Vector128.Sum(Vector128.Multiply(impulse, NormalTangentX));
            fimpulse.Y = Vector128.Sum(Vector128.Multiply(impulse, NormalTangentY));
            fimpulse.Z = Vector128.Sum(Vector128.Multiply(impulse, NormalTangentZ));

            Unsafe.SkipInit(out JVector aimpulse1);
            aimpulse1.X = Vector128.Sum(Vector128.Multiply(impulse, e1));
            aimpulse1.Y = Vector128.Sum(Vector128.Multiply(impulse, e2));
            aimpulse1.Z = Vector128.Sum(Vector128.Multiply(impulse, e3));

            Unsafe.SkipInit(out JVector aimpulse2);
            aimpulse2.X = Vector128.Sum(Vector128.Multiply(impulse, f1));
            aimpulse2.Y = Vector128.Sum(Vector128.Multiply(impulse, f2));
            aimpulse2.Z = Vector128.Sum(Vector128.Multiply(impulse, f3));

            b1.Velocity -= b1.InverseMass * fimpulse;
            b1.AngularVelocity -= aimpulse1;

            b2.Velocity += b2.InverseMass * fimpulse;
            b2.AngularVelocity += aimpulse2;
        }
    }
}