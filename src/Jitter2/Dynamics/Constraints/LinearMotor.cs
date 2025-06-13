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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// A motor constraint that drives relative translational movement along two axes fixed
/// in the reference frames of the bodies.
/// </summary>
public unsafe class LinearMotor : Constraint
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LinearMotorData
    {
        internal int _internal;
        public delegate*<ref ConstraintData, void> Iterate;
        public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

        public JHandle<RigidBodyData> Body1;
        public JHandle<RigidBodyData> Body2;

        public JVector LocalAxis1;
        public JVector LocalAxis2;

        public Real Velocity;
        public Real MaxForce;
        public Real MaxLambda;

        public Real EffectiveMass;

        public Real AccumulatedImpulse;
    }

    private JHandle<LinearMotorData> handle;

    protected override void Create()
    {
        CheckDataSize<LinearMotorData>();

        Iterate = &IterateLinearMotor;
        PrepareForIteration = &PrepareForIterationLinearMotor;
        handle = JHandle<ConstraintData>.AsHandle<LinearMotorData>(Handle);
    }

    public JVector LocalAxis1
    {
        get => handle.Data.LocalAxis1;
        set => handle.Data.LocalAxis1 = value;
    }

    public JVector LocalAxis2
    {
        get => handle.Data.LocalAxis2;
        set => handle.Data.LocalAxis2 = value;
    }

    /// <summary>
    /// Initializes the constraint.
    /// </summary>
    /// <param name="axis1">Axis on the first body in world space.</param>
    /// <param name="axis2">Axis on the second body in world space.</param>
    public void Initialize(JVector axis1, JVector axis2)
    {
        ref LinearMotorData data = ref handle.Data;
        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.NormalizeInPlace(ref axis1);
        JVector.NormalizeInPlace(ref axis2);

        JVector.ConjugatedTransform(axis1, body1.Orientation, out data.LocalAxis1);
        JVector.ConjugatedTransform(axis2, body2.Orientation, out data.LocalAxis2);

        data.MaxForce = 0;
        data.Velocity = 0;
    }

    public Real TargetVelocity
    {
        get => handle.Data.Velocity;
        set => handle.Data.Velocity = value;
    }

    public Real MaximumForce
    {
        get => handle.Data.MaxForce;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            handle.Data.MaxForce = value;
        }
    }

    public Real Impulse => handle.Data.AccumulatedImpulse;

    public static void PrepareForIterationLinearMotor(ref ConstraintData constraint, Real idt)
    {
        ref LinearMotorData data = ref Unsafe.AsRef<LinearMotorData>(Unsafe.AsPointer(ref constraint));

        ref RigidBodyData body1 = ref data.Body1.Data;
        ref RigidBodyData body2 = ref data.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector j1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector j2);

        data.EffectiveMass = body1.InverseMass + body2.InverseMass;
        data.EffectiveMass = (Real)1.0 / data.EffectiveMass;
        data.MaxLambda = ((Real)1.0 / idt) * data.MaxForce;

        body1.Velocity -= j1 * data.AccumulatedImpulse * body1.InverseMass;
        body2.Velocity += j2 * data.AccumulatedImpulse * body2.InverseMass;
    }

    public override void DebugDraw(IDebugDrawer drawer)
    {
    }

    public static void IterateLinearMotor(ref ConstraintData constraint, Real idt)
    {
        ref LinearMotorData data = ref Unsafe.AsRef<LinearMotorData>(Unsafe.AsPointer(ref constraint));
        ref RigidBodyData body1 = ref constraint.Body1.Data;
        ref RigidBodyData body2 = ref constraint.Body2.Data;

        JVector.Transform(data.LocalAxis1, body1.Orientation, out JVector j1);
        JVector.Transform(data.LocalAxis2, body2.Orientation, out JVector j2);

        Real jv = -j1 * body1.Velocity + j2 * body2.Velocity;

        Real lambda = -(jv - data.Velocity) * data.EffectiveMass;

        Real oldAccumulated = data.AccumulatedImpulse;

        data.AccumulatedImpulse += lambda;

        data.AccumulatedImpulse = Math.Clamp(data.AccumulatedImpulse, -data.MaxLambda, data.MaxLambda);

        lambda = data.AccumulatedImpulse - oldAccumulated;

        body1.Velocity -= j1 * lambda * body1.InverseMass;
        body2.Velocity += j2 * lambda * body2.InverseMass;
    }
}