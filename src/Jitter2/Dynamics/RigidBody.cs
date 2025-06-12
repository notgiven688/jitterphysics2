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
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.DataStructures;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics;

[StructLayout(LayoutKind.Explicit, Size = Precision.RigidBodyDataSize)]
public struct RigidBodyData
{
    [FieldOffset(0)]
    public int _index;

    [FieldOffset(4)]
    public int _lockFlag;

    [FieldOffset(8 + 0*sizeof(Real))]
    public JVector Position;

    [FieldOffset(8 + 3*sizeof(Real))]
    public JVector Velocity;

    [FieldOffset(8 + 6*sizeof(Real))]
    public JVector AngularVelocity;

    [FieldOffset(8 + 9*sizeof(Real))]
    public JVector DeltaVelocity;

    [FieldOffset(8 + 12*sizeof(Real))]
    public JVector DeltaAngularVelocity;

    [FieldOffset(8 + 15*sizeof(Real))]
    public JQuaternion Orientation;

    [FieldOffset(8 + 19*sizeof(Real))]
    public JMatrix InverseInertiaWorld;

    [FieldOffset(8 + 28*sizeof(Real))]
    public Real InverseMass;

    [FieldOffset(8 + 29*sizeof(Real) + 0)]
    public bool IsActive;

    [FieldOffset(8 + 29*sizeof(Real) + 1)]
    public bool IsStatic;

    public readonly bool IsStaticOrInactive => !IsActive || IsStatic;
}

/// <summary>
/// Represents the primary entity in the Jitter physics world.
/// </summary>
public sealed class RigidBody : IPartitionedSetIndex, IDebugDrawable
{
    private JHandle<RigidBodyData> handle;

    public readonly ulong RigidBodyId;

    private Real restitution = (Real)0.0;
    private Real friction = (Real)0.2;

    /// <summary>
    /// Due to performance considerations, the data used to simulate this body (e.g., velocity or position)
    /// is stored within a contiguous block of unmanaged memory. This refers to the raw memory location
    /// and should seldom, if ever, be utilized outside the engine. Instead, use the properties provided
    /// by the <see cref="RigidBody"/> class itself.
    /// </summary>
    public ref RigidBodyData Data => ref handle.Data;

    /// <summary>
    /// Gets the handle to the rigid body data, see <see cref="Data"/>.
    /// </summary>
    public JHandle<RigidBodyData> Handle
    {
        get => handle; internal set => handle = value;
    }

    // There is only one way to create a body: world.CreateRigidBody. There, we add an island
    // to the new body. This should never be null.
    internal Island InternalIsland = null!;

    internal readonly List<RigidBodyShape> InternalShapes = new(capacity: 1);
    internal readonly List<RigidBody> InternalConnections = new(capacity: 0);
    internal readonly HashSet<Arbiter> InternalContacts = new(capacity: 0);
    internal readonly HashSet<Constraint> InternalConstraints = new(capacity: 0);

    internal int InternalIslandMarker;
    internal Real InternalSleepTime = (Real)0.0;

    /// <summary>
    /// Gets the collision island associated with this rigid body.
    /// </summary>
    public Island Island => InternalIsland;

    /// <summary>
    /// Event triggered when a new arbiter is created, indicating that two bodies have begun colliding.
    /// </summary>
    /// <remarks>
    /// This event provides an <see cref="Arbiter"/> object which contains details about the collision.
    /// Use this event to handle logic that should occur at the start of a collision between two bodies.
    /// </remarks>
    public event Action<Arbiter>? BeginCollide;

    /// <summary>
    /// Event triggered when an arbiter is destroyed, indicating that two bodies have stopped colliding.
    /// The reference to this arbiter becomes invalid after this call.
    /// </summary>
    /// <remarks>
    /// This event provides an <see cref="Arbiter"/> object which contains details about the collision that has ended.
    /// Use this event to handle logic that should occur when the collision between two bodies ends.
    /// </remarks>
    public event Action<Arbiter>? EndCollide;

    internal void RaiseBeginCollide(Arbiter arbiter)
    {
        BeginCollide?.Invoke(arbiter);
    }

    internal void RaiseEndCollide(Arbiter arbiter)
    {
        EndCollide?.Invoke(arbiter);
    }

    /// <summary>
    /// Contains all bodies this body is in contact with or shares a constraint with.
    /// </summary>
    public ReadOnlyList<RigidBody> Connections => new(InternalConnections);

    /// <summary>
    /// Contains all contacts in which this body is involved.
    /// </summary>
    public ReadOnlyHashSet<Arbiter> Contacts => new(InternalContacts);

    /// <summary>
    /// Contains all constraints connected to this body.
    /// </summary>
    public ReadOnlyHashSet<Constraint> Constraints => new(InternalConstraints);

    /// <summary>
    /// Gets the list of shapes added to this rigid body.
    /// </summary>
    public ReadOnlyList<RigidBodyShape> Shapes => new(InternalShapes);

    private Real inactiveThresholdLinearSq = (Real)0.1;
    private Real inactiveThresholdAngularSq = (Real)0.1;
    private Real deactivationTimeThreshold = (Real)1.0;

    private Real linearDampingMultiplier = (Real)0.998;
    private Real angularDampingMultiplier = (Real)0.995;

    private JMatrix inverseInertia = JMatrix.Identity;
    private Real inverseMass = (Real)1.0;

    /// <remarks>
    /// The friction coefficient determines the resistance to sliding motion.
    /// Values typically range from 0 (no friction) upwards.
    /// Higher values represent strong friction or adhesion effects.
    /// Default is 0.2.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is negative.</exception>
    public Real Friction
    {
        get => friction;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            friction = value;
        }
    }

    /// <summary>
    /// Gets or sets the restitution (bounciness) of this object.
    /// </summary>
    /// <remarks>
    /// The restitution value determines how much energy is retained after a collision,
    /// with 0 representing an inelastic collision (no bounce) and 1 representing a perfectly elastic collision (full bounce).
    /// Values between 0 and 1 create a partially elastic collision effect.
    /// Default is 0.0.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is not between 0 and 1.</exception>
    public Real Restitution
    {
        get => restitution;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, (Real)1.0, nameof(value));

            if (value < (Real)0.0 || value > (Real)1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Restitution must be between 0 and 1.");
            }

            restitution = value;
        }
    }

    private readonly int hashCode;

    /// <summary>
    /// Gets or sets the world assigned to this body.
    /// </summary>
    public World World { get; }

    internal RigidBody(JHandle<RigidBodyData> handle, World world)
    {
        this.handle = handle;
        World = world;

        Data.Orientation = JQuaternion.Identity;
        SetDefaultMassInertia();

        RigidBodyId = World.RequestId();
        uint h = (uint)RigidBodyId;

        // The rigid body is used in hash-based data structures, provide a
        // good hash - Thomas Wang, Jan 1997
        h = h ^ 61 ^ (h >> 16);
        h += h << 3;
        h ^= h >> 4;
        h *= 0x27d4eb2d;
        h ^= h >> 15;

        hashCode = unchecked((int)h);

        Data._lockFlag = 0;
    }

    /// <summary>
    /// Gets or sets the deactivation time. If the magnitudes of both the angular and linear velocity of the rigid body
    /// remain below the <see cref="DeactivationThreshold"/> for the specified time, the body is deactivated.
    /// </summary>
    public TimeSpan DeactivationTime
    {
        get => TimeSpan.FromSeconds(deactivationTimeThreshold);
        set => deactivationTimeThreshold = (Real)value.TotalSeconds;
    }

    /// <summary>
    /// Gets or sets the deactivation threshold. If the magnitudes of both the angular and linear velocity of the rigid body
    /// remain below the specified values for the duration of <see cref="DeactivationTime"/>, the body is deactivated.
    /// The threshold values are given in rad/s and length units/s, respectively.
    /// </summary>
    /// <remarks>
    /// Values must be non-negative. This property stores the squared thresholds internally,
    /// so the input values are automatically squared when set.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if either the linear or angular threshold is negative.
    /// </exception>
    public (Real angular, Real linear) DeactivationThreshold
    {
        get => (MathR.Sqrt(inactiveThresholdAngularSq), MathR.Sqrt(inactiveThresholdLinearSq));
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value.linear, nameof(value));
            ArgumentOutOfRangeException.ThrowIfNegative(value.angular, nameof(value));

            inactiveThresholdLinearSq = value.linear * value.linear;
            inactiveThresholdAngularSq = value.angular * value.angular;
        }
    }

    /// <summary>
    /// Gets or sets the damping factors for linear and angular motion.
    /// A damping factor of 0 means the body is not damped, while 1 brings
    /// the body to a halt immediately. Damping is applied when calling
    /// <see cref="World.Step(Real, bool)"/>. Jitter multiplies the respective
    /// velocity each step by 1 minus the damping factor. Note that the values
    /// are not scaled by time; a smaller time-step in
    /// <see cref="World.Step(Real, bool)"/> results in increased damping.
    /// </summary>
    /// <remarks>
    /// The damping factors must be within the range [0, 1].
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if either the linear or angular damping value is less than 0 or greater than 1.
    /// </exception>
    public (Real linear, Real angular) Damping
    {
        get => ((Real)1.0 - linearDampingMultiplier, (Real)1.0 - angularDampingMultiplier);
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value.linear, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.linear, (Real)1.0, nameof(value));

            ArgumentOutOfRangeException.ThrowIfNegative(value.angular, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.angular, (Real)1.0, nameof(value));

            linearDampingMultiplier = (Real)1.0 - value.linear;
            angularDampingMultiplier = (Real)1.0 - value.angular;
        }
    }

    public override int GetHashCode()
    {
        return hashCode;
    }

    private void SetDefaultMassInertia()
    {
        inverseInertia = JMatrix.Identity;
        Data.InverseMass = (Real)1.0;
        UpdateWorldInertia();
    }

    public JMatrix InverseInertia => inverseInertia;

    public JVector Position
    {
        get => handle.Data.Position;
        set
        {
            handle.Data.Position = value;
            Move();
        }
    }

    public JQuaternion Orientation
    {
        get => Data.Orientation;
        set
        {
            Data.Orientation = value;
            Move();
        }
    }

    private void Move()
    {
        UpdateWorldInertia();

        foreach (var shape in InternalShapes)
        {
            World.DynamicTree.Update(shape);
        }

        World.ActivateBodyNextStep(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Update(Real stepDt, Real substepDt)
    {
        ref RigidBodyData rigidBody = ref Data;

        if (rigidBody.AngularVelocity.LengthSquared() < inactiveThresholdAngularSq &&
            rigidBody.Velocity.LengthSquared() < inactiveThresholdLinearSq)
        {
            InternalSleepTime += stepDt;
        }
        else
        {
            InternalSleepTime = 0;
        }

        if (InternalSleepTime < deactivationTimeThreshold)
        {
            InternalIsland.MarkedAsActive = true;
        }

        if (!rigidBody.IsStaticOrInactive)
        {
            rigidBody.AngularVelocity *= angularDampingMultiplier;
            rigidBody.Velocity *= linearDampingMultiplier;

            rigidBody.DeltaVelocity = Force * rigidBody.InverseMass * substepDt;
            rigidBody.DeltaAngularVelocity = JVector.Transform(Torque, rigidBody.InverseInertiaWorld) * substepDt;

            if (AffectedByGravity)
            {
                rigidBody.DeltaVelocity += World.Gravity * substepDt;
            }

            Force = JVector.Zero;
            Torque = JVector.Zero;

            var bodyOrientation = JMatrix.CreateFromQuaternion(rigidBody.Orientation);

            JMatrix.Multiply(bodyOrientation, inverseInertia, out rigidBody.InverseInertiaWorld);
            JMatrix.MultiplyTransposed(rigidBody.InverseInertiaWorld, bodyOrientation, out rigidBody.InverseInertiaWorld);

            rigidBody.InverseMass = inverseMass;
        }
    }

    public JVector Velocity
    {
        get => handle.Data.Velocity;
        set
        {
            if (!MathHelper.CloseToZero(value))
            {
                World.ActivateBodyNextStep(this);
            }

            handle.Data.Velocity = value;
        }
    }

    public JVector AngularVelocity
    {
        get => handle.Data.AngularVelocity;
        set
        {
            if (!MathHelper.CloseToZero(value))
            {
                World.ActivateBodyNextStep(this);
            }

            handle.Data.AngularVelocity = value;
        }
    }

    public bool AffectedByGravity { get; set; } = true;

    /// <summary>
    /// A managed pointer to custom user data. This is not utilized by the engine.
    /// </summary>
    public object? Tag { get; set; }

    public bool EnableSpeculativeContacts { get; set; } = false;

    private void UpdateWorldInertia()
    {
        if (Data.IsStatic)
        {
            Data.InverseInertiaWorld = JMatrix.Zero;
            Data.InverseMass = (Real)0.0;
        }
        else
        {
            var bodyOrientation = JMatrix.CreateFromQuaternion(Data.Orientation);
            JMatrix.Multiply(bodyOrientation, inverseInertia, out Data.InverseInertiaWorld);
            JMatrix.MultiplyTransposed(Data.InverseInertiaWorld, bodyOrientation, out Data.InverseInertiaWorld);
            Data.InverseMass = inverseMass;
        }
    }

    public bool IsStatic
    {
        set
        {
            if (Data.IsStatic == value) return;

            if (value) World.MakeBodyStatic(this);
            else
            {
                Data.IsStatic = false;
                World.ActivateBodyNextStep(this);
            }

            UpdateWorldInertia();
        }
        get => Data.IsStatic;
    }

    /// <summary>
    /// Indicates whether the rigid body is active or considered to be in a sleeping state.
    /// Use <see cref="SetActivationState"/> to alter the activation state.
    /// </summary>
    public bool IsActive => Data.IsActive;

    /// <summary>
    /// Instructs Jitter to activate or deactivate the body at the commencement of
    /// the next time step. The current state does not change immediately.
    /// </summary>
    public void SetActivationState(bool active)
    {
        if (active) World.ActivateBodyNextStep(this);
        else World.DeactivateBodyNextStep(this);
    }

    private void AttachToShape(RigidBodyShape shape)
    {
        if (shape.RigidBody != null)
        {
            throw new ArgumentException("Shape has already been added to a body.", nameof(shape));
        }

        shape.RigidBody = this;
        shape.UpdateWorldBoundingBox();
        World.DynamicTree.AddProxy(shape, IsActive);
    }

    /// <summary>
    /// Adds several shapes to the rigid body at once. Mass properties are
    /// recalculated only once, if requested.
    /// </summary>
    /// <param name="shapes">The Shapes to add.</param>
    /// <param name="setMassInertia">If true, uses the mass properties of the Shapes to determine the
    /// body's mass properties, assuming unit density for the Shapes. If false, the inertia and mass remain
    /// unchanged.</param>
    public void AddShape(IEnumerable<RigidBodyShape> shapes, bool setMassInertia = true)
    {
        foreach (RigidBodyShape shape in shapes)
        {
            AttachToShape(shape);
            this.InternalShapes.Add(shape);
        }

        if (setMassInertia) SetMassInertia();
    }

    /// <summary>
    /// Adds a shape to the body.
    /// </summary>
    /// <param name="shape">The shape to be added.</param>
    /// <param name="setMassInertia">If true, utilizes the shape's mass properties to determine the body's
    /// mass properties, assuming a unit density for the shape. If false, the inertia and mass remain unchanged.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if the shape is already registered elsewhere.
    /// </exception>
    public void AddShape(RigidBodyShape shape, bool setMassInertia = true)
    {
        if (shape.IsRegistered)
        {
            throw new ArgumentException("Shape can not be added. Shape already registered elsewhere.", nameof(shape));
        }

        AttachToShape(shape);
        InternalShapes.Add(shape);
        if (setMassInertia) SetMassInertia();
    }

    /// <summary>
    /// Represents the force to be applied to the body during the next call to <see cref="World.Step(Real, bool)"/>.
    /// This value is automatically reset to zero after the call.
    /// </summary>
    public JVector Force { get; set; }

    /// <summary>
    /// Represents the torque to be applied to the body during the next call to <see cref="World.Step(Real, bool)"/>.
    /// This value is automatically reset to zero after the call.
    /// </summary>
    public JVector Torque { get; set; }

    /// <summary>
    /// Applies a force to the rigid body, thereby altering its velocity. This force is effective for a single frame only and is reset to zero during the next call to <see cref="World.Step(Real, bool)"/>.
    /// </summary>
    /// <param name="force">The force to be applied.</param>
    public void AddForce(in JVector force)
    {
        if (IsStatic || MathHelper.CloseToZero(force)) return;
        SetActivationState(true);
        Force += force;
    }

    /// <summary>
    /// Applies a force to the rigid body, altering its velocity. This force is applied for a single frame only and is
    /// reset to zero with the subsequent call to <see cref="World.Step(Real, bool)"/>.
    /// </summary>
    /// <param name="force">The force to be applied.</param>
    /// <param name="position">The position where the force will be applied.</param>
    [ReferenceFrame(ReferenceFrame.World)]
    public void AddForce(in JVector force, in JVector position)
    {
        if (IsStatic || MathHelper.CloseToZero(force)) return;

        SetActivationState(true);

        ref RigidBodyData data = ref Data;
        JVector.Subtract(position, data.Position, out JVector torque);
        JVector.Cross(torque, force, out torque);

        Force += force;
        Torque += torque;
    }

    /// <summary>
    /// Predicts the position of the body after a given time step using linear extrapolation.
    /// This does not simulate forces or collisions — it assumes constant velocity.
    /// </summary>
    /// <param name="dt">The time step to extrapolate forward.</param>
    /// <returns>The predicted position after <paramref name="dt"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JVector PredictPosition(Real dt) => Data.Position + Data.Velocity * dt;

    /// <summary>
    /// Predicts the orientation of the body after a given time step using angular velocity.
    /// This does not simulate forces or collisions — it assumes constant angular velocity.
    /// </summary>
    /// <param name="dt">The time step to extrapolate forward.</param>
    /// <returns>The predicted orientation after <paramref name="dt"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JQuaternion PredictOrientation(Real dt) =>
        JQuaternion.Normalize(MathHelper.RotationQuaternion(Data.AngularVelocity, dt) * Data.Orientation);

    /// <summary>
    /// Predicts the pose (position and orientation) of the body after a given time step using simple extrapolation.
    /// This method is intended for rendering purposes and does not modify the simulation state.
    /// </summary>
    /// <param name="dt">The time step to extrapolate forward.</param>
    /// <param name="position">The predicted position after <paramref name="dt"/>.</param>
    /// <param name="orientation">The predicted orientation after <paramref name="dt"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PredictPose(Real dt, out JVector position, out JQuaternion orientation)
    {
        position = PredictPosition(dt);
        orientation = PredictOrientation(dt);
    }

    /// <summary>
    /// Removes a specified shape from the rigid body.
    /// </summary>
    /// <remarks>This operation has a time complexity of O(n), where n is the number of shapes attached to the body.</remarks>
    /// <param name="shape">The shape to remove from the rigid body.</param>
    /// <param name="setMassInertia">Specifies whether to adjust the mass inertia properties of the rigid body after removing the shape. The default value is true.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if the specified shape is not part of this rigid body.
    /// </exception>
    public void RemoveShape(RigidBodyShape shape, bool setMassInertia = true)
    {
        if (!InternalShapes.Remove(shape))
        {
            throw new ArgumentException("Shape is not part of this body.", nameof(shape));
        }

        foreach (var arbiter in InternalContacts)
        {
            if (arbiter.Handle.Data.Key.Key1 == shape.ShapeId || arbiter.Handle.Data.Key.Key2 == shape.ShapeId)
            {
                // Removes the current element we are iterating over from Contacts, i.e. the HashSet
                // we are iterating over is altered. This is allowed.
                World.Remove(arbiter);
            }
        }

        World.DynamicTree.RemoveProxy(shape);
        shape.RigidBody = null!;

        if (setMassInertia) SetMassInertia();
    }

    /// <summary>
    /// Removes several shapes from the body.
    /// </summary>
    /// <remarks>This operation has a time complexity of O(n), where n is the number of shapes attached to the body.</remarks>
    /// <param name="shapes">The shapes to remove from the rigid body.</param>
    /// <param name="setMassInertia">Specifies whether to adjust the mass inertia properties of the rigid body after removal. The default value is true.</param>
    /// <exception cref="ArgumentException">Thrown if at least one shape is not part of this rigid body.</exception>
    public void RemoveShape(IEnumerable<RigidBodyShape> shapes, bool setMassInertia = true)
    {
        HashSet<ulong> sids = new();

        foreach (var shape in shapes)
        {
            if (shape.RigidBody != this)
            {
                throw new ArgumentException($"Shape {shape} is not attached to this body.", nameof(shapes));
            }

            sids.Add(shape.ShapeId);
        }

        foreach (var arbiter in InternalContacts)
        {
            if (sids.Contains(arbiter.Handle.Data.Key.Key1) || sids.Contains(arbiter.Handle.Data.Key.Key2))
            {
                // Removes the current element we are iterating over from Contacts, i.e. the HashSet
                // we are iterating over is altered. This is allowed.
                World.Remove(arbiter);
            }
        }

        for (int i = this.InternalShapes.Count; i-- > 0;)
        {
            var shape = this.InternalShapes[i];

            if (sids.Contains(shape.ShapeId))
            {
                World.DynamicTree.RemoveProxy(shape);
                shape.RigidBody = null!;
                this.InternalShapes.RemoveAt(i);
            }
        }

        if (setMassInertia) SetMassInertia();

        sids.Clear();
    }

    /// <summary>
    /// Removes all shapes associated with the rigid body.
    /// </summary>
    /// <remarks>This operation has a time complexity of O(n), where n is the number of shapes attached to the body.</remarks>
    /// <param name="setMassInertia">If set to false, the mass properties of the rigid body remain unchanged.</param>
    [Obsolete($"{nameof(ClearShapes)} is deprecated, please use {nameof(RemoveShape)} instead.")]
    public void ClearShapes(bool setMassInertia = true)
    {
        RemoveShape(InternalShapes, setMassInertia);
    }

    /// <summary>
    /// Utilizes the mass properties of the shape to determine the mass properties of the rigid body.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the computed inertia matrix is not invertible. This may occur if a shape has invalid mass properties.
    /// </exception>
    public void SetMassInertia()
    {
        if (InternalShapes.Count == 0)
        {
            inverseInertia = JMatrix.Identity;
            Data.InverseMass = (Real)1.0;
            return;
        }

        JMatrix inertia = JMatrix.Zero;
        Real mass = (Real)0.0;

        for (int i = 0; i < InternalShapes.Count; i++)
        {
            InternalShapes[i].CalculateMassInertia(out var shapeInertia, out _, out var shapeMass);

            inertia += shapeInertia;
            mass += shapeMass;
        }

        if (!JMatrix.Inverse(inertia, out inverseInertia))
        {
            throw new InvalidOperationException("Inertia matrix is not invertible. This might happen if a shape has " +
                                                "invalid mass properties. If you encounter this while calling " +
                                                "RigidBody.AddShape, call AddShape with setMassInertia set to false.");
        }

        inverseMass = (Real)1.0 / mass;

        UpdateWorldInertia();
    }

    /// <summary>
    /// Sets a new mass value and scales the inertia according to the ratio of the old mass to the new mass.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the specified mass is zero or negative.</exception>
    public void SetMassInertia(Real mass)
    {
        if (mass <= (Real)0.0)
        {
            // we do not protect against NaN here, since it is the users responsibility
            // to not feed NaNs to the engine.
            throw new ArgumentException("Mass can not be zero or negative.", nameof(mass));
        }

        SetMassInertia();
        inverseInertia = JMatrix.Multiply(inverseInertia, (Real)1.0 / (Data.InverseMass * mass));
        inverseMass = (Real)1.0 / mass;
        UpdateWorldInertia();
    }

    /// <summary>
    /// Sets the new mass properties of this body by specifying both inertia and mass directly.
    /// </summary>
    /// <param name="setAsInverse">Set the inverse values.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if:
    /// <list type="bullet">
    /// <item><description><paramref name="mass"/> is zero or negative when <paramref name="setAsInverse"/> is false.</description></item>
    /// <item><description><paramref name="mass"/> is negative or infinite when <paramref name="setAsInverse"/> is true.</description></item>
    /// <item><description><paramref name="inertia"/> is not invertible when <paramref name="setAsInverse"/> is false.</description></item>
    /// </list>
    /// </exception>
    public void SetMassInertia(in JMatrix inertia, Real mass, bool setAsInverse = false)
    {
        if (setAsInverse)
        {
            if (Real.IsInfinity(mass) || mass < (Real)0.0)
            {
                throw new ArgumentException("Inverse mass must be finite and not negative.", nameof(mass));
            }

            inverseInertia = inertia;
            inverseMass = mass;
        }
        else
        {
            if (mass <= (Real)0.0)
            {
                throw new ArgumentException("Mass can not be zero or negative.", nameof(mass));
            }

            if (!JMatrix.Inverse(inertia, out inverseInertia))
            {
                throw new ArgumentException("Inertia matrix is not invertible.", nameof(inertia));
            }

            inverseMass = (Real)1.0 / mass;
        }

        UpdateWorldInertia();
    }

    private static List<JTriangle>? _debugTriangles;

    /// <summary>
    /// Generates a rough triangle approximation of the shapes of the body.
    /// Since the generation is slow this should only be used for debugging
    /// purposes.
    /// </summary>
    public void DebugDraw(IDebugDrawer drawer)
    {
        _debugTriangles ??= new List<JTriangle>();

        foreach (var shape in InternalShapes)
        {
            ShapeHelper.MakeHull(shape, _debugTriangles);

            foreach (var tri in _debugTriangles)
            {
                drawer.DrawTriangle(
                    JVector.Transform(tri.V0, Data.Orientation) + Data.Position,
                    JVector.Transform(tri.V1, Data.Orientation) + Data.Position,
                    JVector.Transform(tri.V2, Data.Orientation) + Data.Position);
            }
        }

        _debugTriangles.Clear();
    }

    /// <summary>
    /// Gets the mass of the rigid body. To modify the mass, use
    /// <see cref="RigidBody.SetMassInertia(Real)"/> or
    /// <see cref="RigidBody.SetMassInertia(in JMatrix, Real, bool)"/>.
    /// </summary>
    public Real Mass => (Real)1.0 / inverseMass;

    int IPartitionedSetIndex.SetIndex { get; set; } = -1;
}