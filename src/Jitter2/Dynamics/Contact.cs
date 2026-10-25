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
/// Holds up to four <see cref="Contact"/> structs for a single shape-to-shape collision.
/// </summary>
/// <remarks>
/// <para>
/// This structure is stored in unmanaged memory and accessed via <see cref="Arbiter.Handle"/>.
/// The <see cref="UsageMask"/> bitfield indicates which of the four contact slots are active.
/// </para>
/// <para>
/// The returned data is valid only while the associated arbiter exists and must not be accessed
/// concurrently with <see cref="World.Step(Real, bool)"/>. Do not cache references to this structure.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct ContactData
{
    /// <summary>
    /// Specifies which velocity components the solver should update for each body.
    /// </summary>
    [Flags]
    public enum SolveMode
    {
        /// <summary>No velocity updates.</summary>
        None = 0,
        /// <summary>Update linear velocity of body 1.</summary>
        LinearBody1 = 1 << 0,
        /// <summary>Update angular velocity of body 1.</summary>
        AngularBody1 = 1 << 1,
        /// <summary>Update linear velocity of body 2.</summary>
        LinearBody2 = 1 << 2,
        /// <summary>Update angular velocity of body 2.</summary>
        AngularBody2 = 1 << 3,
        /// <summary>Update both linear and angular velocity of body 1.</summary>
        FullBody1 = LinearBody1 | AngularBody1,
        /// <summary>Update both linear and angular velocity of body 2.</summary>
        FullBody2 = LinearBody2 | AngularBody2,
        /// <summary>Update linear velocities of both bodies.</summary>
        Linear = LinearBody1 | LinearBody2,
        /// <summary>Update angular velocities of both bodies.</summary>
        Angular = AngularBody1 | AngularBody2,
        /// <summary>Update all velocity components of both bodies.</summary>
        Full = Linear | Angular,
    }

    /// <summary>Bit mask for contact slot 0.</summary>
    public const uint MaskContact0 = 0b0001;
    /// <summary>Bit mask for contact slot 1.</summary>
    public const uint MaskContact1 = 0b0010;
    /// <summary>Bit mask for contact slot 2.</summary>
    public const uint MaskContact2 = 0b0100;
    /// <summary>Bit mask for contact slot 3.</summary>
    public const uint MaskContact3 = 0b1000;

    /// <summary>Bit mask indicating all four contact slots are in use.</summary>
    public const uint MaskContactAll = MaskContact0 | MaskContact1 | MaskContact2 | MaskContact3;

    // Accessed in unsafe code.
#pragma warning disable CS0649
    internal int _internal;
#pragma warning restore CS0649

    /// <summary>
    /// Bits 0–3 mark contacts that are currently valid; bits 4–7 mark contacts used in the last solve.
    /// </summary>
    /// <remarks>
    /// A contact can be used by the solver and then break as the bodies move during the same step.
    /// Its solver bit remains set even though its current-validity bit has been cleared.
    /// </remarks>
    public uint UsageMask;

    /// <summary>Handle to the first body's simulation data.</summary>
    public JHandle<RigidBodyData> Body1;
    /// <summary>Handle to the second body's simulation data.</summary>
    public JHandle<RigidBodyData> Body2;

    /// <summary>Unique key identifying this arbiter's shape pair.</summary>
    public ArbiterKey Key;

    /// <summary>Combined restitution coefficient for this contact pair.</summary>
    public Real Restitution;
    /// <summary>Combined friction coefficient for this contact pair.</summary>
    public Real Friction;
    /// <summary>Relaxation factor for predicted contact separation.</summary>
    public Real CcdRelaxationFactor;

    /// <summary>Determines which velocity components are updated by the solver.</summary>
    public SolveMode Mode;

    /// <summary>
    /// Normalized contact direction shared by all points of the manifold. A positive normal impulse
    /// pushes <see cref="Body2"/> along this direction and <see cref="Body1"/> against it. Collision
    /// detection refreshes it together with the points.
    /// </summary>
    [ReferenceFrame(ReferenceFrame.World)] public JVector Normal;

    /// <summary>
    /// First friction direction shared by all points, orthonormal to <see cref="Normal"/>. The first
    /// point of a manifold aligns it with its tangential velocity; afterwards it turns with the normal
    /// as far as the contact plane allows, keeping friction warm starts meaningful.
    /// </summary>
    [ReferenceFrame(ReferenceFrame.World)] public JVector Tangent1;

    /// <summary>Second friction direction, completing the orthonormal basis.</summary>
    public readonly JVector Tangent2 => Tangent1 % Normal;

    /// <summary>
    /// Offset from the second body's center of mass to the first body's when the point offsets were
    /// last fixed. With a point's <see cref="Contact.RelativePosition1"/> and <see cref="Contact.Depth"/>
    /// it yields the point's offset from the second body, see <see cref="Contact.GetRelativePosition2"/>.
    /// </summary>
    [ReferenceFrame(ReferenceFrame.World)] public JVector CenterDelta;

    /// <summary>First contact point.</summary>
    public Contact Contact0;
    /// <summary>Second contact point.</summary>
    public Contact Contact1;
    /// <summary>Third contact point.</summary>
    public Contact Contact2;
    /// <summary>Fourth contact point.</summary>
    public Contact Contact3;

    /// <summary>
    /// Prepares all active contacts for solver iterations by updating contact geometry, computing effective masses and warm-starting impulses.
    /// </summary>
    /// <param name="timeStep">The full step and substep timing information.</param>
    public unsafe void PrepareForIteration(in TimeStep timeStep)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if (Vector.IsHardwareAccelerated)
        {
            if ((UsageMask & MaskContact0) != 0) Contact0.PrepareForIterationAccelerated(ptr, timeStep);
            if ((UsageMask & MaskContact1) != 0) Contact1.PrepareForIterationAccelerated(ptr, timeStep);
            if ((UsageMask & MaskContact2) != 0) Contact2.PrepareForIterationAccelerated(ptr, timeStep);
            if ((UsageMask & MaskContact3) != 0) Contact3.PrepareForIterationAccelerated(ptr, timeStep);
        }
        else
        {
            if ((UsageMask & MaskContact0) != 0) Contact0.PrepareForIteration(ptr, timeStep);
            if ((UsageMask & MaskContact1) != 0) Contact1.PrepareForIteration(ptr, timeStep);
            if ((UsageMask & MaskContact2) != 0) Contact2.PrepareForIteration(ptr, timeStep);
            if ((UsageMask & MaskContact3) != 0) Contact3.PrepareForIteration(ptr, timeStep);
        }
    }

    /// <summary>Clears the accumulated normal and friction impulses of all active contacts.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetImpulses()
    {
        if ((UsageMask & MaskContact0) != 0) Contact0.ResetImpulses();
        if ((UsageMask & MaskContact1) != 0) Contact1.ResetImpulses();
        if ((UsageMask & MaskContact2) != 0) Contact2.ResetImpulses();
        if ((UsageMask & MaskContact3) != 0) Contact3.ResetImpulses();
    }

    /// <summary>Skips replaying accumulated impulses on the next preparation of each active contact.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SkipWarmStart()
    {
        const Contact.Flags flag = Contact.Flags.SkipWarmStart;
        if ((UsageMask & MaskContact0) != 0) Contact0.Flag |= flag;
        if ((UsageMask & MaskContact1) != 0) Contact1.Flag |= flag;
        if ((UsageMask & MaskContact2) != 0) Contact2.Flag |= flag;
        if ((UsageMask & MaskContact3) != 0) Contact3.Flag |= flag;
    }

    /// <summary>
    /// Performs one solver iteration over all active contacts, applying corrective impulses.
    /// </summary>
    /// <param name="applyBias">If <see langword="true"/>, applies position-correction bias.</param>
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

    internal unsafe void PrepareForIterationAccelerated(in TimeStep timeStep)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if ((UsageMask & MaskContact0) != 0) Contact0.PrepareForIterationAccelerated(ptr, timeStep);
        if ((UsageMask & MaskContact1) != 0) Contact1.PrepareForIterationAccelerated(ptr, timeStep);
        if ((UsageMask & MaskContact2) != 0) Contact2.PrepareForIterationAccelerated(ptr, timeStep);
        if ((UsageMask & MaskContact3) != 0) Contact3.PrepareForIterationAccelerated(ptr, timeStep);
    }

    internal unsafe void PrepareForIterationScalar(in TimeStep timeStep)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if ((UsageMask & MaskContact0) != 0) Contact0.PrepareForIteration(ptr, timeStep);
        if ((UsageMask & MaskContact1) != 0) Contact1.PrepareForIteration(ptr, timeStep);
        if ((UsageMask & MaskContact2) != 0) Contact2.PrepareForIteration(ptr, timeStep);
        if ((UsageMask & MaskContact3) != 0) Contact3.PrepareForIteration(ptr, timeStep);
    }

    internal unsafe void IterateAccelerated(bool applyBias)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if ((UsageMask & MaskContact0) != 0) Contact0.IterateAccelerated(ptr, applyBias);
        if ((UsageMask & MaskContact1) != 0) Contact1.IterateAccelerated(ptr, applyBias);
        if ((UsageMask & MaskContact2) != 0) Contact2.IterateAccelerated(ptr, applyBias);
        if ((UsageMask & MaskContact3) != 0) Contact3.IterateAccelerated(ptr, applyBias);
    }

    internal unsafe void IterateScalar(bool applyBias)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

        if ((UsageMask & MaskContact0) != 0) Contact0.Iterate(ptr, applyBias);
        if ((UsageMask & MaskContact1) != 0) Contact1.Iterate(ptr, applyBias);
        if ((UsageMask & MaskContact2) != 0) Contact2.Iterate(ptr, applyBias);
        if ((UsageMask & MaskContact3) != 0) Contact3.Iterate(ptr, applyBias);
    }

    /// <summary>
    /// Gets whether the contact solver can use hardware-accelerated vector operations.
    /// </summary>
    public static bool IsHardwareAccelerated => Vector.IsHardwareAccelerated;

    /// <summary>
    /// Updates contact positions after integration and removes contacts whose normal separation exceeds
    /// one tenth of the break threshold or whose tangential drift exceeds the full threshold.
    /// </summary>
    public unsafe void UpdatePosition()
    {
        UsageMask &= MaskContactAll;
        UsageMask |= UsageMask << 4;

        // Every point's second offset is derived from this delta, fixed at the same time as the offsets.
        CenterDelta = Body1.Data.Position - Body2.Data.Position;
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

    /// <summary>
    /// Initializes this contact data for a pair of rigid bodies.
    /// </summary>
    /// <param name="body1">The first rigid body.</param>
    /// <param name="body2">The second rigid body.</param>
    public void Init(RigidBody body1, RigidBody body2)
    {
        Body1 = body1.Handle;
        Body2 = body2.Handle;

        Friction = MathR.Max(body1.Friction, body2.Friction);
        Restitution = MathR.Max(body1.Restitution, body2.Restitution);
        CcdRelaxationFactor = body1.World.CcdRelaxationFactor;

        Debug.Assert(body1.World == body2.World);

        Mode = SolveMode.None;
        UsageMask = 0;
        Normal = Tangent1 = CenterDelta = JVector.Zero;
    }

    /// <summary>
    /// Adopts a new contact normal for the whole manifold. The friction direction turns with the
    /// normal as far as the new contact plane allows, and the accumulated impulses of every active
    /// point are re-expressed in the new basis so that warm starts survive the change.
    /// </summary>
    internal void UpdateBasis(in JVector normal)
    {
        JVector previousNormal = Normal;
        if (normal == previousNormal) return;

        JVector previousTangent1 = Tangent1;
        JVector previousTangent2 = previousTangent1 % previousNormal;
        JVector tangent1 = previousTangent1 - normal * JVector.Dot(previousTangent1, normal);
        Real lengthSquared = tangent1.LengthSquared();
        if (lengthSquared > (Real)1e-12) tangent1 *= (Real)1.0 / MathR.Sqrt(lengthSquared);
        else tangent1 = MathHelper.CreateOrthonormal(normal);
        JVector tangent2 = tangent1 % normal;

        Normal = normal;
        Tangent1 = tangent1;
        if ((UsageMask & MaskContact0) != 0) Contact0.ReexpressImpulse(Friction, previousNormal, previousTangent1, previousTangent2, normal, tangent1, tangent2);
        if ((UsageMask & MaskContact1) != 0) Contact1.ReexpressImpulse(Friction, previousNormal, previousTangent1, previousTangent2, normal, tangent1, tangent2);
        if ((UsageMask & MaskContact2) != 0) Contact2.ReexpressImpulse(Friction, previousNormal, previousTangent1, previousTangent2, normal, tangent1, tangent2);
        if ((UsageMask & MaskContact3) != 0) Contact3.ReexpressImpulse(Friction, previousNormal, previousTangent1, previousTangent2, normal, tangent1, tangent2);
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
    /// <param name="point1">Contact point on the first body in world space.</param>
    /// <param name="point2">Contact point on the second body in world space.</param>
    /// <param name="normal">Contact normal in the direction of the solver's positive impulse on body 2.
    /// For overlapping contact points, it points from <paramref name="point2"/> toward <paramref name="point1"/>.</param>
    public unsafe void AddContact(in JVector point1, in JVector point2, in JVector normal)
    {
        var ptr = (ContactData*)Unsafe.AsPointer(ref this);

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
            closest->Initialize(ptr, point1, point2, normal, false);
            return;
        }

        // It is a completely new contact.

        if ((UsageMask & MaskContact0) == 0)
        {
            Contact0.Initialize(ptr, point1, point2, normal, true);
            UsageMask |= MaskContact0;
        }
        else if ((UsageMask & MaskContact1) == 0)
        {
            Contact1.Initialize(ptr, point1, point2, normal, true);
            UsageMask |= MaskContact1;
        }
        else if ((UsageMask & MaskContact2) == 0)
        {
            Contact2.Initialize(ptr, point1, point2, normal, true);
            UsageMask |= MaskContact2;
        }
        else if ((UsageMask & MaskContact3) == 0)
        {
            Contact3.Initialize(ptr, point1, point2, normal, true);
            UsageMask |= MaskContact3;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint SelectCachedPointReplacementMask(
        Real area0, Real area1, Real area2, Real area3)
    {
        // always prefer the new point
        const Real epsilon = -(Real)0.0001;

        Real biggestArea = 0;
        uint mask = 0;

        if (area0 > biggestArea + epsilon)
        {
            biggestArea = area0;
            mask = MaskContact0;
        }

        if (area1 > biggestArea + epsilon)
        {
            biggestArea = area1;
            mask = MaskContact1;
        }

        if (area2 > biggestArea + epsilon)
        {
            biggestArea = area2;
            mask = MaskContact2;
        }

        if (area3 > biggestArea + epsilon)
        {
            mask = MaskContact3;
        }

        return mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint SelectCachedPointReplacementMaskScalar(
        in JVector newPoint, in JVector contact0, in JVector contact1, in JVector contact2, in JVector contact3)
    {
        Real area0 = CalcArea4Points(newPoint, contact1, contact2, contact3);
        Real area1 = CalcArea4Points(newPoint, contact0, contact2, contact3);
        Real area2 = CalcArea4Points(newPoint, contact0, contact1, contact3);
        Real area3 = CalcArea4Points(newPoint, contact0, contact1, contact2);

        return SelectCachedPointReplacementMask(area0, area1, area2, area3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static VectorReal CrossLengthSquared(
        VectorReal ax, VectorReal ay, VectorReal az,
        VectorReal bx, VectorReal by, VectorReal bz)
    {
        VectorReal cx = Vector.Subtract(Vector.Multiply(ay, bz), Vector.Multiply(az, by));
        VectorReal cy = Vector.Subtract(Vector.Multiply(az, bx), Vector.Multiply(ax, bz));
        VectorReal cz = Vector.Subtract(Vector.Multiply(ax, by), Vector.Multiply(ay, bx));

        return Vector.Add(Vector.Add(Vector.Multiply(cx, cx), Vector.Multiply(cy, cy)), Vector.Multiply(cz, cz));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint SelectCachedPointReplacementMaskVectorized(
        in JVector newPoint, in JVector contact0, in JVector contact1, in JVector contact2, in JVector contact3)
    {
        VectorReal p0X = Vector.Create(newPoint.X);
        VectorReal p0Y = Vector.Create(newPoint.Y);
        VectorReal p0Z = Vector.Create(newPoint.Z);

        VectorReal p1X = Vector.Create(contact1.X, contact0.X, contact0.X, contact0.X);
        VectorReal p1Y = Vector.Create(contact1.Y, contact0.Y, contact0.Y, contact0.Y);
        VectorReal p1Z = Vector.Create(contact1.Z, contact0.Z, contact0.Z, contact0.Z);

        VectorReal p2X = Vector.Create(contact2.X, contact2.X, contact1.X, contact1.X);
        VectorReal p2Y = Vector.Create(contact2.Y, contact2.Y, contact1.Y, contact1.Y);
        VectorReal p2Z = Vector.Create(contact2.Z, contact2.Z, contact1.Z, contact1.Z);

        VectorReal p3X = Vector.Create(contact3.X, contact3.X, contact3.X, contact2.X);
        VectorReal p3Y = Vector.Create(contact3.Y, contact3.Y, contact3.Y, contact2.Y);
        VectorReal p3Z = Vector.Create(contact3.Z, contact3.Z, contact3.Z, contact2.Z);

        VectorReal a0X = Vector.Subtract(p0X, p1X);
        VectorReal a0Y = Vector.Subtract(p0Y, p1Y);
        VectorReal a0Z = Vector.Subtract(p0Z, p1Z);

        VectorReal a1X = Vector.Subtract(p0X, p2X);
        VectorReal a1Y = Vector.Subtract(p0Y, p2Y);
        VectorReal a1Z = Vector.Subtract(p0Z, p2Z);

        VectorReal a2X = Vector.Subtract(p0X, p3X);
        VectorReal a2Y = Vector.Subtract(p0Y, p3Y);
        VectorReal a2Z = Vector.Subtract(p0Z, p3Z);

        VectorReal b0X = Vector.Subtract(p2X, p3X);
        VectorReal b0Y = Vector.Subtract(p2Y, p3Y);
        VectorReal b0Z = Vector.Subtract(p2Z, p3Z);

        VectorReal b1X = Vector.Subtract(p1X, p3X);
        VectorReal b1Y = Vector.Subtract(p1Y, p3Y);
        VectorReal b1Z = Vector.Subtract(p1Z, p3Z);

        VectorReal b2X = Vector.Subtract(p1X, p2X);
        VectorReal b2Y = Vector.Subtract(p1Y, p2Y);
        VectorReal b2Z = Vector.Subtract(p1Z, p2Z);

        VectorReal area0 = CrossLengthSquared(a0X, a0Y, a0Z, b0X, b0Y, b0Z);
        VectorReal area1 = CrossLengthSquared(a1X, a1Y, a1Z, b1X, b1Y, b1Z);
        VectorReal area2 = CrossLengthSquared(a2X, a2Y, a2Z, b2X, b2Y, b2Z);
        VectorReal areas = Vector.Max(Vector.Max(area0, area1), area2);

        return SelectCachedPointReplacementMask(
            areas.GetElement(0), areas.GetElement(1), areas.GetElement(2), areas.GetElement(3));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint SelectCachedPointReplacementMask(
        in JVector newPoint, in JVector contact0, in JVector contact1, in JVector contact2, in JVector contact3)
    {
        return SelectCachedPointReplacementMaskVectorized(newPoint, contact0, contact1, contact2, contact3);
    }

    private unsafe void SortCachedPoints(in JVector point1, in JVector point2, in JVector normal)
    {
        JVector.Subtract(point1, Body1.Data.Position, out JVector rp1);

        uint mask = SelectCachedPointReplacementMask(
            rp1, Contact0.RelativePosition1, Contact1.RelativePosition1, Contact2.RelativePosition1, Contact3.RelativePosition1);

        ref Contact cref = ref Contact0;

        if (mask == MaskContact1)
        {
            cref = ref Contact1;
        }
        else if (mask == MaskContact2)
        {
            cref = ref Contact2;
        }
        else if (mask == MaskContact3)
        {
            cref = ref Contact3;
        }

        var ptr = (ContactData*)Unsafe.AsPointer(ref this);
        cref.Initialize(ptr, point1, point2, normal, false);
        UsageMask |= mask;
    }

    // ---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Represents a single contact point between two rigid bodies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This structure is layout-sensitive and stored in unmanaged memory as part of a <see cref="ContactData"/>.
    /// It contains geometric information (normal, tangents, positions) and solver state (accumulated impulses, bias).
    /// </para>
    /// <para>
    /// Do not cache or store references to this structure across simulation steps.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    public struct Contact
    {
        /// <summary>Maximum position-correction bias applied per iteration.</summary>
        public const Real MaximumBias = (Real)100.0;
        /// <summary>Fraction of penetration corrected per step (Baumgarte stabilization).</summary>
        public const Real BiasFactor = (Real)0.2;
        /// <summary>Penetration depth below which no position correction is applied.</summary>
        public const Real AllowedPenetration = (Real)0.01;
        /// <summary>Maximum tangential drift before a contact breaks; normal separation is limited to one tenth of this value.</summary>
        public const Real BreakThreshold = (Real)0.02;

        /// <summary>
        /// Flags indicating contact state.
        /// </summary>
        [Flags]
        public enum Flags
        {
            /// <summary>Indicates this contact was created in the current step.</summary>
            NewContact = 1 << 1,
            /// <summary>Skips replaying the accumulated impulse on the next preparation only.</summary>
            SkipWarmStart = 1 << 2,
        }

        /// <summary>Velocity bias for restitution or predicted separation.</summary>
        /// <summary>Velocity bias for restitution or predicted separation.</summary>
        [FieldOffset(0)] public Real Bias;

        /// <summary>Position-correction bias computed from penetration depth.</summary>
        [FieldOffset(1 * sizeof(Real))] public Real PenaltyBias;

        /// <summary>
        /// Separation of the two anchor points along the manifold normal when the offsets were fixed;
        /// positive when overlapping. The second body's offset is derived from it.
        /// </summary>
        [FieldOffset(2 * sizeof(Real))] public Real Depth;

        // Inverse effective masses along the normal and tangents, computed by the scalar preparation
        // and reused by every iteration of the substep. SIMD bundles keep their own copy.
        [FieldOffset(3 * sizeof(Real))] internal Real MassNormal;
        [FieldOffset(4 * sizeof(Real))] internal Real MassTangent1;
        [FieldOffset(5 * sizeof(Real))] internal Real MassTangent2;

        [FieldOffset(6 * sizeof(Real))]
        [ReferenceFrame(ReferenceFrame.Local)]
        internal JVector Position1;

        [FieldOffset(9 * sizeof(Real))]
        [ReferenceFrame(ReferenceFrame.Local)]
        internal JVector Position2;

        /// <summary>
        /// World-space contact offset from the first body's center of mass used to apply impulses.
        /// Held fixed during the substeps of a full simulation step.
        /// </summary>
        [FieldOffset(12 * sizeof(Real))]
        [ReferenceFrame(ReferenceFrame.World)] public JVector RelativePosition1;

        [FieldOffset(16 * sizeof(Real))] internal VectorReal Accumulated;

        // The unused fourth impulse lane holds flags until the next preparation.
        // Preparation clears them before the vector is used by solver arithmetic.
        /// <summary>Current contact state flags.</summary>
        [FieldOffset(19 * sizeof(Real))] public Flags Flag;

        /// <summary>
        /// World-space contact offset from the second body's center of mass used to apply impulses,
        /// derived from <see cref="RelativePosition1"/>, the manifold's <see cref="ContactData.CenterDelta"/>
        /// and <see cref="Depth"/> along the manifold normal. Held fixed during the substeps of a step.
        /// </summary>
        public readonly JVector GetRelativePosition2(in ContactData manifold) =>
            RelativePosition1 + manifold.CenterDelta - Depth * manifold.Normal;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly unsafe JVector RelativePosition2(ContactData* cd) =>
            RelativePosition1 + cd->CenterDelta - Depth * cd->Normal;

        // Re-expresses the cached world impulse in a new basis, within the solver's unilateral
        // normal and per-tangent friction bounds. Restitution is not restarted.
        internal void ReexpressImpulse(Real friction, in JVector previousNormal, in JVector previousTangent1,
            in JVector previousTangent2, in JVector normal, in JVector tangent1, in JVector tangent2)
        {
            Flags pendingFlags = Flag;
            JVector cachedImpulse = Impulse * previousNormal;
            cachedImpulse += TangentImpulse1 * previousTangent1 + TangentImpulse2 * previousTangent2;
            Real normalImpulse = MathR.Max((Real)0.0, JVector.Dot(cachedImpulse, normal));
            Real frictionLimit = friction * normalImpulse;
            Accumulated = Vector.Create(normalImpulse,
                Math.Clamp(JVector.Dot(cachedImpulse, tangent1), -frictionLimit, frictionLimit),
                Math.Clamp(JVector.Dot(cachedImpulse, tangent2), -frictionLimit, frictionLimit), (Real)0.0);
            Flag = pendingFlags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ResetImpulses()
        {
            Flags flags = Flag;
            Accumulated = Vector.Create((Real)0.0);
            Flag = flags;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void GetBasisVectors(ContactData* cd, out VectorReal x, out VectorReal y, out VectorReal z)
        {
            JVector normal = cd->Normal, tangent1 = cd->Tangent1;
            JVector tangent2 = tangent1 % normal;
            x = Vector.Create(normal.X, tangent1.X, tangent2.X, 0);
            y = Vector.Create(normal.Y, tangent1.Y, tangent2.Y, 0);
            z = Vector.Create(normal.Z, tangent1.Z, tangent2.Z, 0);
        }

        /// <summary>
        /// Gets the accumulated normal solver impulse from the last solved substep.
        /// </summary>
        public readonly Real Impulse => Accumulated.GetElement(0);

        /// <summary>
        /// Gets the accumulated solver impulse in the first tangent direction from the last solved substep.
        /// </summary>
        public readonly Real TangentImpulse1 => Accumulated.GetElement(1);

        /// <summary>
        /// Gets the accumulated solver impulse in the second tangent direction from the last solved substep.
        /// </summary>
        public readonly Real TangentImpulse2 => Accumulated.GetElement(2);

        // Relaxation runs after body poses have advanced. Keep using the offsets prepared
        // before that integration, so its Jacobian matches the velocity solve's Jacobian.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly unsafe VectorReal CalculateEffectiveMass(ContactData* cd)
            => CalculateEffectiveMass(cd, RelativePosition2(cd), out _, out _, out _, out _, out _, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly unsafe VectorReal CalculateEffectiveMass(ContactData* cd, in JVector relativePosition2,
            out JVector angularNormalBody1, out JVector angularTangent1Body1, out JVector angularTangent2Body1,
            out JVector angularNormalBody2, out JVector angularTangent1Body2, out JVector angularTangent2Body2)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;
            JVector normal = cd->Normal;
            JVector tangent1 = cd->Tangent1;
            JVector tangent2 = tangent1 % normal;

            JVector inverseMassVector = JVector.Zero;
            if ((cd->Mode & SolveMode.LinearBody1) != 0) inverseMassVector = b1.InverseMassVector;
            if ((cd->Mode & SolveMode.LinearBody2) != 0) inverseMassVector += b2.InverseMassVector;

            Real kNormal = inverseMassVector.X;
            Real kTangent1 = kNormal;
            Real kTangent2 = kNormal;
            if (b1.HasLinearLocks || b2.HasLinearLocks)
            {
                kNormal = JVector.Multiply(inverseMassVector, normal) * normal;
                kTangent1 = JVector.Multiply(inverseMassVector, tangent1) * tangent1;
                kTangent2 = JVector.Multiply(inverseMassVector, tangent2) * tangent2;
            }

            JVector angularNormalContribution = JVector.Zero;
            JVector angularTangent1Contribution = JVector.Zero;
            JVector angularTangent2Contribution = JVector.Zero;
            angularNormalBody1 = angularTangent1Body1 = angularTangent2Body1 = JVector.Zero;
            angularNormalBody2 = angularTangent1Body2 = angularTangent2Body2 = JVector.Zero;

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                angularNormalBody1 = TransformSymmetricInertia(RelativePosition1 % normal, b1.InverseInertiaWorld);
                angularTangent1Body1 = TransformSymmetricInertia(RelativePosition1 % tangent1, b1.InverseInertiaWorld);
                angularTangent2Body1 = TransformSymmetricInertia(RelativePosition1 % tangent2, b1.InverseInertiaWorld);
                angularNormalContribution += angularNormalBody1 % RelativePosition1;
                angularTangent1Contribution += angularTangent1Body1 % RelativePosition1;
                angularTangent2Contribution += angularTangent2Body1 % RelativePosition1;
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                angularNormalBody2 = TransformSymmetricInertia(relativePosition2 % normal, b2.InverseInertiaWorld);
                angularTangent1Body2 = TransformSymmetricInertia(relativePosition2 % tangent1, b2.InverseInertiaWorld);
                angularTangent2Body2 = TransformSymmetricInertia(relativePosition2 % tangent2, b2.InverseInertiaWorld);
                angularNormalContribution += angularNormalBody2 % relativePosition2;
                angularTangent1Contribution += angularTangent1Body2 % relativePosition2;
                angularTangent2Contribution += angularTangent2Body2 % relativePosition2;
            }

            kNormal += JVector.Dot(normal, angularNormalContribution);
            kTangent1 += JVector.Dot(tangent1, angularTangent1Contribution);
            kTangent2 += JVector.Dot(tangent2, angularTangent2Contribution);

            return Vector.Create(
                kNormal > 0 ? (Real)1.0 / kNormal : 0,
                kTangent1 > 0 ? (Real)1.0 / kTangent1 : 0,
                kTangent2 > 0 ? (Real)1.0 / kTangent2 : 0,
                (Real)0.0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly unsafe VectorReal CalculateEffectiveMassAccelerated(ContactData* cd,
            VectorReal normalTangentX, VectorReal normalTangentY, VectorReal normalTangentZ, in JVector relativePosition2,
            out VectorReal e1, out VectorReal e2, out VectorReal e3,
            out VectorReal f1, out VectorReal f2, out VectorReal f3)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector inverseMassVector = JVector.Zero;
            if ((cd->Mode & SolveMode.LinearBody1) != 0) inverseMassVector = b1.InverseMassVector;
            if ((cd->Mode & SolveMode.LinearBody2) != 0) inverseMassVector += b2.InverseMassVector;
            VectorReal kNormalTangent = Vector.Create(inverseMassVector.X);
            if (b1.HasLinearLocks || b2.HasLinearLocks)
            {
                var kx = Vector.Multiply(Vector.Multiply(normalTangentX, Vector.Create(inverseMassVector.X)), normalTangentX);
                var ky = Vector.Multiply(Vector.Multiply(normalTangentY, Vector.Create(inverseMassVector.Y)), normalTangentY);
                var kz = Vector.Multiply(Vector.Multiply(normalTangentZ, Vector.Create(inverseMassVector.Z)), normalTangentZ);
                kNormalTangent = Vector.Add(Vector.Add(kx, ky), kz);
            }

            var ktnx = Vector.Create((Real)0.0);
            var ktny = Vector.Create((Real)0.0);
            var ktnz = Vector.Create((Real)0.0);
            e1 = e2 = e3 = Vector.Create((Real)0.0);
            f1 = f2 = f3 = Vector.Create((Real)0.0);

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                var rp1X = Vector.Create(RelativePosition1.X);
                var rp1Y = Vector.Create(RelativePosition1.Y);
                var rp1Z = Vector.Create(RelativePosition1.Z);

                var rrx = Vector.Subtract(Vector.Multiply(rp1Y, normalTangentZ), Vector.Multiply(rp1Z, normalTangentY));
                var rry = Vector.Subtract(Vector.Multiply(rp1Z, normalTangentX), Vector.Multiply(rp1X, normalTangentZ));
                var rrz = Vector.Subtract(Vector.Multiply(rp1X, normalTangentY), Vector.Multiply(rp1Y, normalTangentX));

                var ixx = Vector.Create(b1.InverseInertiaWorld.M11);
                var ixy = Vector.Create(b1.InverseInertiaWorld.M21);
                var ixz = Vector.Create(b1.InverseInertiaWorld.M31);
                var iyy = Vector.Create(b1.InverseInertiaWorld.M22);
                var iyz = Vector.Create(b1.InverseInertiaWorld.M23);
                var izz = Vector.Create(b1.InverseInertiaWorld.M33);

                e1 = Vector.Add(Vector.Add(Vector.Multiply(ixx, rrx), Vector.Multiply(ixy, rry)), Vector.Multiply(ixz, rrz));
                e2 = Vector.Add(Vector.Add(Vector.Multiply(ixy, rrx), Vector.Multiply(iyy, rry)), Vector.Multiply(iyz, rrz));
                e3 = Vector.Add(Vector.Add(Vector.Multiply(ixz, rrx), Vector.Multiply(iyz, rry)), Vector.Multiply(izz, rrz));

                ktnx = Vector.Subtract(Vector.Multiply(e2, rp1Z), Vector.Multiply(e3, rp1Y));
                ktny = Vector.Subtract(Vector.Multiply(e3, rp1X), Vector.Multiply(e1, rp1Z));
                ktnz = Vector.Subtract(Vector.Multiply(e1, rp1Y), Vector.Multiply(e2, rp1X));
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                var rp2X = Vector.Create(relativePosition2.X);
                var rp2Y = Vector.Create(relativePosition2.Y);
                var rp2Z = Vector.Create(relativePosition2.Z);

                var rrx = Vector.Subtract(Vector.Multiply(rp2Y, normalTangentZ), Vector.Multiply(rp2Z, normalTangentY));
                var rry = Vector.Subtract(Vector.Multiply(rp2Z, normalTangentX), Vector.Multiply(rp2X, normalTangentZ));
                var rrz = Vector.Subtract(Vector.Multiply(rp2X, normalTangentY), Vector.Multiply(rp2Y, normalTangentX));

                var ixx = Vector.Create(b2.InverseInertiaWorld.M11);
                var ixy = Vector.Create(b2.InverseInertiaWorld.M21);
                var ixz = Vector.Create(b2.InverseInertiaWorld.M31);
                var iyy = Vector.Create(b2.InverseInertiaWorld.M22);
                var iyz = Vector.Create(b2.InverseInertiaWorld.M23);
                var izz = Vector.Create(b2.InverseInertiaWorld.M33);

                f1 = Vector.Add(Vector.Add(Vector.Multiply(ixx, rrx), Vector.Multiply(ixy, rry)), Vector.Multiply(ixz, rrz));
                f2 = Vector.Add(Vector.Add(Vector.Multiply(ixy, rrx), Vector.Multiply(iyy, rry)), Vector.Multiply(iyz, rrz));
                f3 = Vector.Add(Vector.Add(Vector.Multiply(ixz, rrx), Vector.Multiply(iyz, rry)), Vector.Multiply(izz, rrz));

                ktnx = Vector.Add(ktnx, Vector.Subtract(Vector.Multiply(f2, rp2Z), Vector.Multiply(f3, rp2Y)));
                ktny = Vector.Add(ktny, Vector.Subtract(Vector.Multiply(f3, rp2X), Vector.Multiply(f1, rp2Z)));
                ktnz = Vector.Add(ktnz, Vector.Subtract(Vector.Multiply(f1, rp2Y), Vector.Multiply(f2, rp2X)));
            }

            var kres = Vector.Add(Vector.Add(Vector.Multiply(normalTangentX, ktnx),
                Vector.Multiply(normalTangentY, ktny)), Vector.Multiply(normalTangentZ, ktnz));

            kNormalTangent = Vector.Add(kNormalTangent, kres);
            return Vector.ConditionalSelect(Vector.GreaterThan(kNormalTangent, Vector.Create((Real)0.0)),
                Vector.Divide(Vector.Create((Real)1.0), kNormalTangent), Vector.Create((Real)0.0));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void Initialize(ContactData* cd, in JVector point1, in JVector point2,
            in JVector n, bool newContact)
        {
            Debug.Assert(Math.Abs(n.LengthSquared() - (Real)1.0) < 1e-3);

            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector.Subtract(point1, b1.Position, out RelativePosition1);
            JVector.Subtract(point2, b2.Position, out JVector relativePosition2);
            JVector.ConjugatedTransform(RelativePosition1, b1.Orientation, out Position1);
            JVector.ConjugatedTransform(relativePosition2, b2.Orientation, out Position2);

            // The second offset is derived from the first, the center delta and the depth along the
            // normal. All points registered in one pass share the same body positions.
            cd->CenterDelta = b1.Position - b2.Position;
            Depth = JVector.Dot(point1 - point2, n);

            if (newContact)
            {
                Accumulated = Vector.Create((Real)0.0);
                Flag = Flags.NewContact;

                // Linear-only contacts also omit rotation from restitution and the friction basis.
                JVector dv = b2.Velocity - b1.Velocity;
                if (cd->Mode == SolveMode.None || (cd->Mode & SolveMode.Angular) != 0)
                {
                    dv = b2.Velocity + b2.AngularVelocity % relativePosition2;
                    dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;
                }

                Real relNormalVel = JVector.Dot(dv, n);
                Bias = 0;

                // Fake restitution
                if (relNormalVel < (Real)(-1.0))
                {
                    Bias = -cd->Restitution * relNormalVel;
                }

                if ((cd->UsageMask & MaskContactAll) == 0)
                {
                    // The first point of a manifold aligns the friction basis with its tangential velocity.
                    JVector tangent1 = dv - n * relNormalVel;
                    Real lengthSquared = tangent1.LengthSquared();
                    if (lengthSquared > (Real)1e-12) tangent1 *= (Real)1.0 / MathR.Sqrt(lengthSquared);
                    else tangent1 = MathHelper.CreateOrthonormal(n);
                    cd->Normal = n;
                    cd->Tangent1 = tangent1;
                    return;
                }
            }

            // Later points, and refreshed ones, share the manifold basis and turn it with the normal.
            cd->UpdateBasis(n);
        }

        public unsafe bool UpdatePosition(ContactData* cd)
        {
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            // Solving has finished. Refresh the offsets for contact matching in the next step.
            JVector.Transform(Position1, b1.Orientation, out RelativePosition1);
            JVector.Add(RelativePosition1, b1.Position, out JVector p1);

            JVector.Transform(Position2, b2.Orientation, out JVector relativePosition2);
            JVector.Add(relativePosition2, b2.Position, out JVector p2);

            JVector.Subtract(p1, p2, out JVector dist);

            JVector n = cd->Normal;

            Real penetration = JVector.Dot(dist, n);
            Depth = penetration;

            // use a tenth of the actual threshold value here.
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

        #region public unsafe void PrepareForIteration(ContactData* cd, in TimeStep timeStep)

        public unsafe void PrepareForIteration(ContactData* cd, in TimeStep timeStep)
        {
            // Begin read from VectorReal
            Real accumulatedNormalImpulse = Accumulated.GetElement(0);
            Real accumulatedTangentImpulse1 = Accumulated.GetElement(1);
            Real accumulatedTangentImpulse2 = Accumulated.GetElement(2);

            bool skipWarmStart = (Flag & Flags.SkipWarmStart) != 0;
            Flag &= ~Flags.SkipWarmStart;
            if (skipWarmStart)
            {
                // Keep Accumulated for Iterate; only omit its replay into velocities.
                accumulatedNormalImpulse = 0;
                accumulatedTangentImpulse1 = 0;
                accumulatedTangentImpulse2 = 0;
            }

            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;
            JVector normal = cd->Normal;
            JVector tangent1 = cd->Tangent1;
            JVector tangent2 = tangent1 % normal;
            JVector relativePosition2 = RelativePosition2(cd);

            JVector.Transform(Position1, b1.Orientation, out JVector currentRelativePosition1);
            JVector.Transform(Position2, b2.Orientation, out JVector currentRelativePosition2);

            if ((Flag & Flags.NewContact) == 0)
            {
                Bias = 0;
            }

            JVector.Add(currentRelativePosition1, b1.Position, out JVector p1);
            JVector.Add(currentRelativePosition2, b2.Position, out JVector p2);
            JVector.Subtract(p1, p2, out JVector dist);
            Real penetration = JVector.Dot(dist, normal);

            if (penetration < -BreakThreshold)
            {
                // The contact points are still separated.
                Bias = penetration * timeStep.InverseStepDt * cd->CcdRelaxationFactor;
            }

            Flag &= ~Flags.NewContact;

            // The effective masses stay fixed for the substep. Their angular responses also
            // replay the accumulated impulses for the warm start.
            VectorReal mass = CalculateEffectiveMass(cd, relativePosition2, out JVector angularNormal1,
                out JVector angularTangent1Body1, out JVector angularTangent2Body1, out JVector angularNormal2,
                out JVector angularTangent1Body2, out JVector angularTangent2Body2);
            MassNormal = mass.GetElement(0);
            MassTangent1 = mass.GetElement(1);
            MassTangent2 = mass.GetElement(2);

            JVector impulse = accumulatedNormalImpulse * normal;
            impulse += accumulatedTangentImpulse1 * tangent1;
            impulse += accumulatedTangentImpulse2 * tangent2;

            if ((cd->Mode & SolveMode.LinearBody1) != 0)
            {
                b1.Velocity -= JVector.Multiply(impulse, b1.InverseMassVector);
            }

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                JVector angularImpulse = accumulatedNormalImpulse * angularNormal1;
                angularImpulse += accumulatedTangentImpulse1 * angularTangent1Body1;
                angularImpulse += accumulatedTangentImpulse2 * angularTangent2Body1;
                b1.AngularVelocity -= angularImpulse;
            }

            if ((cd->Mode & SolveMode.LinearBody2) != 0)
            {
                b2.Velocity += JVector.Multiply(impulse, b2.InverseMassVector);
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                JVector angularImpulse = accumulatedNormalImpulse * angularNormal2;
                angularImpulse += accumulatedTangentImpulse1 * angularTangent1Body2;
                angularImpulse += accumulatedTangentImpulse2 * angularTangent2Body2;
                b2.AngularVelocity += angularImpulse;
            }

            PenaltyBias = BiasFactor * timeStep.InverseStepDt * Math.Max((Real)0.0, penetration - AllowedPenetration);
            PenaltyBias = Math.Min(PenaltyBias, MaximumBias);
        }

        #endregion

        // Fallback for missing hardware acceleration
        #region public unsafe void Iterate(ContactData* cd, bool applyBias)

        public unsafe void Iterate(ContactData* cd, bool applyBias)
        {
            Flags pendingFlags = Flag;
            Real massNormal = MassNormal;
            Real massTangent1 = MassTangent1;
            Real massTangent2 = MassTangent2;
            // Begin read from VectorReal
            Real accumulatedNormalImpulse = Accumulated.GetElement(0);
            Real accumulatedTangentImpulse1 = Accumulated.GetElement(1);
            Real accumulatedTangentImpulse2 = Accumulated.GetElement(2);

            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;
            JVector normal = cd->Normal;
            JVector tangent1 = cd->Tangent1;
            JVector tangent2 = tangent1 % normal;
            JVector relativePosition2 = RelativePosition2(cd);

            JVector dv = b2.Velocity - b1.Velocity;
            if ((cd->Mode & SolveMode.Angular) != 0)
            {
                dv = b2.Velocity + b2.AngularVelocity % relativePosition2;
                dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;
            }

            Real bias = Bias;
            if (applyBias && PenaltyBias > 0)
            {
                bias = MathR.Max(PenaltyBias, Bias);
            }

            Real vn = JVector.Dot(normal, dv);
            Real vt1 = JVector.Dot(tangent1, dv);
            Real vt2 = JVector.Dot(tangent2, dv);

            Real normalImpulse = -vn + bias;

            normalImpulse *= massNormal;

            Real oldNormalImpulse = accumulatedNormalImpulse;
            accumulatedNormalImpulse = MathR.Max(oldNormalImpulse + normalImpulse, (Real)0.0);
            normalImpulse = accumulatedNormalImpulse - oldNormalImpulse;

            // Use the pre-update normal impulse for the friction cone to stay consistent
            // with the SIMD path, where this avoids breaking the vectorized clamp.
            Real maxTangentImpulse = cd->Friction * oldNormalImpulse;
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
            if (pendingFlags != 0) Flag = pendingFlags;

            JVector impulse = normalImpulse * normal;
            impulse += tangentImpulse1 * tangent1;
            impulse += tangentImpulse2 * tangent2;

            if ((cd->Mode & SolveMode.LinearBody1) != 0)
            {
                b1.Velocity -= JVector.Multiply(impulse, b1.InverseMassVector);
            }

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                JVector angularImpulse = TransformSymmetricInertia(
                    RelativePosition1 % impulse, b1.InverseInertiaWorld);
                b1.AngularVelocity -= angularImpulse;
            }

            if ((cd->Mode & SolveMode.LinearBody2) != 0)
            {
                b2.Velocity += JVector.Multiply(impulse, b2.InverseMassVector);
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                JVector angularImpulse = TransformSymmetricInertia(
                    relativePosition2 % impulse, b2.InverseInertiaWorld);
                b2.AngularVelocity += angularImpulse;
            }

        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Real GetSum3(VectorReal vector)
        {
            return vector.GetElement(0) + vector.GetElement(1) + vector.GetElement(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JVector TransformSymmetricInertia(in JVector vector, in JSymmetricMatrix matrix)
        {
            Unsafe.SkipInit(out JVector result);
            result.X = vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31;
            result.Y = vector.X * matrix.M21 + vector.Y * matrix.M22 + vector.Z * matrix.M23;
            result.Z = vector.X * matrix.M31 + vector.Y * matrix.M23 + vector.Z * matrix.M33;
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void PrepareForIterationAccelerated(ContactData* cd, in TimeStep timeStep)
        {
            GetBasisVectors(cd, out var normalTangentX, out var normalTangentY, out var normalTangentZ);
            JVector relativePosition2 = RelativePosition2(cd);
            bool skipWarmStart = (Flag & Flags.SkipWarmStart) != 0;
            Flag &= ~Flags.SkipWarmStart;
            var warmStartImpulse = skipWarmStart ? Vector.Create((Real)0.0) : Accumulated;

            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector.Transform(Position1, b1.Orientation, out JVector currentRelativePosition1);
            JVector.Transform(Position2, b2.Orientation, out JVector currentRelativePosition2);

            if ((Flag & Flags.NewContact) == 0)
            {
                Bias = 0;
            }

            JVector.Add(currentRelativePosition1, b1.Position, out JVector r1);
            JVector.Add(currentRelativePosition2, b2.Position, out JVector r2);
            JVector.Subtract(r1, r2, out JVector dist);

            JVector n = new JVector(normalTangentX.GetElement(0), normalTangentY.GetElement(0), normalTangentZ.GetElement(0));
            Real penetration = JVector.Dot(dist, n);

            if (penetration < -BreakThreshold)
            {
                // The contact points are still separated.
                Bias = penetration * timeStep.InverseStepDt * cd->CcdRelaxationFactor;
            }

            Flag &= ~Flags.NewContact;

            // The effective masses stay fixed for the substep. Their angular responses also
            // replay the accumulated impulses for the warm start.
            VectorReal mass = CalculateEffectiveMassAccelerated(cd, normalTangentX, normalTangentY, normalTangentZ,
                relativePosition2, out var e1, out var e2, out var e3, out var f1, out var f2, out var f3);
            MassNormal = mass.GetElement(0);
            MassTangent1 = mass.GetElement(1);
            MassTangent2 = mass.GetElement(2);

            // warm-starting, linear
            Unsafe.SkipInit(out JVector linearImpulse);
            linearImpulse.X = GetSum3(Vector.Multiply(warmStartImpulse, normalTangentX));
            linearImpulse.Y = GetSum3(Vector.Multiply(warmStartImpulse, normalTangentY));
            linearImpulse.Z = GetSum3(Vector.Multiply(warmStartImpulse, normalTangentZ));

            if ((cd->Mode & SolveMode.LinearBody1) != 0)
            {
                b1.Velocity -= JVector.Multiply(linearImpulse, b1.InverseMassVector);
            }

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                Unsafe.SkipInit(out JVector angularImpulse1);
                angularImpulse1.X = GetSum3(Vector.Multiply(warmStartImpulse, e1));
                angularImpulse1.Y = GetSum3(Vector.Multiply(warmStartImpulse, e2));
                angularImpulse1.Z = GetSum3(Vector.Multiply(warmStartImpulse, e3));

                b1.AngularVelocity -= angularImpulse1;
            }

            if ((cd->Mode & SolveMode.LinearBody2) != 0)
            {
                b2.Velocity += JVector.Multiply(linearImpulse, b2.InverseMassVector);
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                Unsafe.SkipInit(out JVector angularImpulse2);
                angularImpulse2.X = GetSum3(Vector.Multiply(warmStartImpulse, f1));
                angularImpulse2.Y = GetSum3(Vector.Multiply(warmStartImpulse, f2));
                angularImpulse2.Z = GetSum3(Vector.Multiply(warmStartImpulse, f3));

                b2.AngularVelocity += angularImpulse2;
            }

            PenaltyBias = BiasFactor * timeStep.InverseStepDt * Math.Max((Real)0.0, penetration - AllowedPenetration);
            PenaltyBias = Math.Min(PenaltyBias, MaximumBias);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void IterateAccelerated(ContactData* cd, bool applyBias)
        {
            Flags pendingFlags = Flag;
            GetBasisVectors(cd, out var normalTangentX, out var normalTangentY, out var normalTangentZ);
            JVector relativePosition2 = RelativePosition2(cd);
            ref var b1 = ref cd->Body1.Data;
            ref var b2 = ref cd->Body2.Data;

            JVector dv = b2.Velocity - b1.Velocity;
            if ((cd->Mode & SolveMode.Angular) != 0)
            {
                dv = b2.Velocity + b2.AngularVelocity % relativePosition2;
                dv -= b1.Velocity + b1.AngularVelocity % RelativePosition1;
            }

            Real bias = Bias;
            if (applyBias && PenaltyBias > 0)
            {
                bias = MathR.Max(PenaltyBias, Bias);
            }

            var vdots = Vector.Add(Vector.Add(Vector.Multiply(normalTangentX, Vector.Create(dv.X)),
                Vector.Multiply(normalTangentY, Vector.Create(dv.Y))), Vector.Multiply(normalTangentZ, Vector.Create(dv.Z)));

            var impulse = Vector.Multiply(Vector.Create(MassNormal, MassTangent1, MassTangent2, (Real)0.0),
                Vector.Subtract(Vector.Create(bias, 0, 0, 0), vdots));
            var oldImpulse = Accumulated;

            // Use the pre-update normal impulse for the friction cone; this keeps the
            // vectorized clamp simple and matches the scalar path.
            Real maxTangentImpulse = cd->Friction * Accumulated.GetElement(0);

            Accumulated = Vector.Add(oldImpulse, impulse);

            var minImpulse = Vector.Create(0, -maxTangentImpulse, -maxTangentImpulse, 0);
            var maxImpulse = Vector.Create(Real.MaxValue, maxTangentImpulse, maxTangentImpulse, 0);

            Accumulated = Vector.Min(Vector.Max(Accumulated, minImpulse), maxImpulse);
            if (pendingFlags != 0) Flag = pendingFlags;
            impulse = Vector.Subtract(Accumulated, oldImpulse);

            Unsafe.SkipInit(out JVector linearImpulse);
            linearImpulse.X = GetSum3(Vector.Multiply(impulse, normalTangentX));
            linearImpulse.Y = GetSum3(Vector.Multiply(impulse, normalTangentY));
            linearImpulse.Z = GetSum3(Vector.Multiply(impulse, normalTangentZ));

            if ((cd->Mode & SolveMode.LinearBody1) != 0)
            {
                b1.Velocity -= JVector.Multiply(linearImpulse, b1.InverseMassVector);
            }

            if ((cd->Mode & SolveMode.AngularBody1) != 0)
            {
                JVector angularImpulse = TransformSymmetricInertia(
                    RelativePosition1 % linearImpulse, b1.InverseInertiaWorld);
                b1.AngularVelocity -= angularImpulse;
            }

            if ((cd->Mode & SolveMode.LinearBody2) != 0)
            {
                b2.Velocity += JVector.Multiply(linearImpulse, b2.InverseMassVector);
            }

            if ((cd->Mode & SolveMode.AngularBody2) != 0)
            {
                JVector angularImpulse = TransformSymmetricInertia(
                    relativePosition2 % linearImpulse, b2.InverseInertiaWorld);
                b2.AngularVelocity += angularImpulse;
            }

        }
    }
}
