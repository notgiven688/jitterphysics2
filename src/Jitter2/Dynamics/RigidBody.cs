/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.DataStructures;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics;

/// <summary>
/// Specifies how a rigid body participates in the simulation.
/// </summary>
public enum MotionType
{
    /// <summary>
    /// Fully simulated; responds to forces, impulses, contacts and constraints.
    /// </summary>
    Dynamic = 0,

    /// <summary>
    /// User-controlled body that is not affected by forces or collisions, but can affect dynamic bodies.
    /// Treated as having infinite mass in the solver. May have a non-zero velocity set by user code.
    /// Takes part in collision island building.
    /// </summary>
    Kinematic = 1,

    /// <summary>
    /// Immovable body (zero velocity) treated as having infinite mass by the solver.
    /// The position and orientation may be changed directly by user code, which will update the
    /// broadphase and may affect contacts on the next step.
    /// </summary>
    Static = 2
}

/// <summary>
/// Low-level simulation state for a <see cref="RigidBody"/>, stored in unmanaged memory.
/// </summary>
/// <remarks>
/// This structure is layout-sensitive and intended for internal engine use. Prefer using
/// <see cref="RigidBody"/> properties instead of accessing fields directly.
/// All spatial values (position, velocity, orientation, inertia) are in world space.
/// The <see cref="Flags"/> field is a bitfield: bits 0–1 encode <see cref="MotionType"/>,
/// bit 2 indicates active state, and bit 3 enables gyroscopic forces.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = Precision.RigidBodyDataSize)]
public struct RigidBodyData
{
    /// <summary>
    /// Internal index used by the engine for handle management. Not stable across frames.
    /// </summary>
    [FieldOffset(0)]
    public int _index;

    /// <summary>
    /// Internal synchronization flag used by the engine. Do not modify.
    /// </summary>
    [FieldOffset(4)]
    public int _lockFlag;

    /// <summary>
    /// World-space position of the rigid body (center of mass).
    /// </summary>
    [FieldOffset(8 + 0*sizeof(Real))]
    public JVector Position;

    /// <summary>
    /// Linear velocity in world space, measured in units per second.
    /// </summary>
    [FieldOffset(8 + 3*sizeof(Real))]
    public JVector Velocity;

    /// <summary>
    /// Angular velocity in world space, measured in radians per second. The vector direction
    /// is the rotation axis, and its magnitude is the rotation speed.
    /// </summary>
    [FieldOffset(8 + 6*sizeof(Real))]
    public JVector AngularVelocity;

    /// <summary>
    /// Accumulated linear velocity change for the current substep (from forces and gravity).
    /// Internal use only.
    /// </summary>
    [FieldOffset(8 + 9*sizeof(Real))]
    public JVector DeltaVelocity;

    /// <summary>
    /// Accumulated angular velocity change for the current substep (from torques).
    /// Internal use only.
    /// </summary>
    [FieldOffset(8 + 12*sizeof(Real))]
    public JVector DeltaAngularVelocity;

    /// <summary>
    /// World-space orientation of the rigid body.
    /// </summary>
    [FieldOffset(8 + 15*sizeof(Real))]
    public JQuaternion Orientation;

    /// <summary>
    /// Inverse inertia tensor in world space. For dynamic bodies, this is recomputed each step
    /// from the body-space inverse inertia and current orientation. For static and kinematic
    /// bodies, this is zero (representing infinite inertia).
    /// </summary>
    [FieldOffset(8 + 19*sizeof(Real))]
    public JMatrix InverseInertiaWorld;

    /// <summary>
    /// Inverse mass of the body. A value of zero represents infinite mass (used for static
    /// and kinematic bodies in the solver).
    /// </summary>
    [FieldOffset(8 + 28*sizeof(Real))]
    public Real InverseMass;

    /// <summary>
    /// Bitfield encoding motion type (bits 0–1), active state (bit 2), and gyroscopic forces (bit 3).
    /// Use the corresponding properties instead of manipulating this directly.
    /// </summary>
    [FieldOffset(8 + 29*sizeof(Real))]
    public int Flags;

    /// <summary>
    /// Gets or sets whether the body is active (awake) and participating in simulation.
    /// Inactive bodies are considered sleeping and skip integration until reactivated.
    /// </summary>
    public bool IsActive
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (Flags & 4) != 0;
        set
        {
            if (value) Flags |= 4;
            else Flags &= ~4;
        }
    }

    /// <summary>
    /// Gets or sets whether the implicit gyroscopic torque solver is enabled.
    /// See <see cref="RigidBody.EnableGyroscopicForces"/> for details.
    /// </summary>
    public bool EnableGyroscopicForces
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => (Flags & 8) != 0;
        set
        {
            if (value) Flags |= 8;
            else Flags &= ~8;
        }
    }

    /// <summary>
    /// Returns true if the body is static or currently inactive (sleeping).
    /// </summary>
    [Obsolete($"Use {nameof(MotionType)} directly.")]
    public bool IsStaticOrInactive => MotionType != MotionType.Dynamic || !IsActive;

    /// <summary>
    /// Gets or sets how this body participates in the simulation.
    /// Encoded in bits 0–1 of <see cref="Flags"/>.
    /// </summary>
    public MotionType MotionType
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (MotionType)(Flags & 0b11);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Flags = (Flags & ~0b11) | (int)value;
    }
}

/// <summary>
/// Represents the primary entity in the Jitter physics world.
/// </summary>
public sealed class RigidBody : IPartitionedSetIndex, IDebugDrawable
{
    private JHandle<RigidBodyData> handle;

    /// <summary>
    /// Unique identifier for this rigid body, assigned by the <see cref="World"/> upon creation.
    /// This ID remains stable for the lifetime of the body.
    /// </summary>
    public readonly ulong RigidBodyId;

    private Real restitution = (Real)0.0;
    private Real friction = (Real)0.2;

    /// <summary>
    /// Returns a by-ref view of the unmanaged simulation state for this body.
    /// </summary>
    /// <remarks>
    /// Due to performance considerations, simulation data (position, velocity, etc.) is stored
    /// in a contiguous block of unmanaged memory. This property provides direct access to that data.
    /// <para>
    /// <strong>Usage notes:</strong>
    /// <list type="bullet">
    /// <item><description>Prefer using <see cref="RigidBody"/> properties instead of accessing fields directly.</description></item>
    /// <item><description>Do not cache the returned reference across simulation steps.</description></item>
    /// <item><description>Modifying fields directly can break invariants (e.g., world-space inertia) unless you know what you're doing.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
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
    [CallbackThread(ThreadContext.MainThread)]
    public event Action<Arbiter>? BeginCollide;

    /// <summary>
    /// Event triggered when an arbiter is destroyed, indicating that two bodies have stopped colliding.
    /// The reference to this arbiter becomes invalid after this call.
    /// </summary>
    /// <remarks>
    /// This event provides an <see cref="Arbiter"/> object which contains details about the collision that has ended.
    /// Use this event to handle logic that should occur when the collision between two bodies ends.
    /// </remarks>
    [CallbackThread(ThreadContext.MainThread)]
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

    /// <summary>
    /// Gets or sets the coefficient of friction used for contact resolution.
    /// </summary>
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
            restitution = value;
        }
    }

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
    /// Gets or sets the deactivation threshold. If the magnitudes of both the angular and linear velocity
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
            ArgumentOutOfRangeException.ThrowIfNegative(value.linear, nameof(value.linear));
            ArgumentOutOfRangeException.ThrowIfNegative(value.angular, nameof(value.angular));

            inactiveThresholdLinearSq = value.linear * value.linear;
            inactiveThresholdAngularSq = value.angular * value.angular;
        }
    }

    /// <summary>
    /// Gets or sets the damping factors for linear and angular motion. A damping factor of 0 means the body is not
    /// damped, while 1 brings the body to a halt immediately. Damping is applied when calling
    /// <see cref="World.Step(Real, bool)"/>. Jitter multiplies the respective velocity each step by 1 minus the damping
    /// factor. Note that the values are not scaled by time; a smaller time-step in <see cref="World.Step(Real, bool)"/>
    /// results in increased damping.
    /// </summary>
    /// <remarks>The damping factors must be within the range [0, 1].</remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if either the linear or angular damping value is less than 0 or greater than 1.
    /// </exception>
    public (Real linear, Real angular) Damping
    {
        get => ((Real)1.0 - linearDampingMultiplier, (Real)1.0 - angularDampingMultiplier);
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value.linear, nameof(value.linear));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.linear, (Real)1.0, nameof(value.linear));

            ArgumentOutOfRangeException.ThrowIfNegative(value.angular, nameof(value.angular));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value.angular, (Real)1.0, nameof(value.angular));

            linearDampingMultiplier = (Real)1.0 - value.linear;
            angularDampingMultiplier = (Real)1.0 - value.angular;
        }
    }

    private void SetDefaultMassInertia()
    {
        inverseInertia = JMatrix.Identity;
        Data.InverseMass = (Real)1.0;
        UpdateWorldInertia();
    }

    /// <summary>
    /// Gets the inverse inertia tensor in body (local) space.
    /// </summary>
    /// <remarks>
    /// For world-space inverse inertia, see <see cref="RigidBodyData.InverseInertiaWorld"/>.
    /// For non-dynamic bodies, the solver treats inertia as infinite regardless of this value.
    /// </remarks>
    public JMatrix InverseInertia => inverseInertia;

    /// <summary>
    /// Gets or sets the world-space position of the rigid body.
    /// </summary>
    /// <remarks>
    /// Setting this property updates the broadphase proxies for all attached shapes
    /// and schedules the body for activation on the next step.
    /// </remarks>
    public JVector Position
    {
        get => handle.Data.Position;
        set
        {
            handle.Data.Position = value;
            Move();
        }
    }

    /// <summary>
    /// Gets or sets the world-space orientation of the rigid body.
    /// </summary>
    /// <remarks>
    /// Setting this property updates the broadphase proxies for all attached shapes
    /// and schedules the body for activation on the next step.
    /// </remarks>
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

        if (rigidBody.MotionType == MotionType.Dynamic)
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

    /// <summary>
    /// Gets or sets the linear velocity of the rigid body in world space.
    /// </summary>
    /// <remarks>
    /// Measured in units per second. Setting a non-zero velocity schedules the body
    /// for activation on the next step.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the body's <see cref="MotionType"/> is <see cref="MotionType.Static"/>.
    /// </exception>
    public JVector Velocity
    {
        get => handle.Data.Velocity;
        set
        {
            Debug.Assert(handle.Data.MotionType != MotionType.Static);

            if (handle.Data.MotionType == MotionType.Static)
            {
                // Throw an exception here, since we change the behaviour of the engine with version 2.7.4.
                // Maybe return to assert-only later.
                throw new InvalidOperationException(
                    $"Can not set velocity for static objects, objects must be kinematic or dynamic. See {nameof(MotionType)}.");
            }

            if (!MathHelper.CloseToZero(value))
            {
                World.ActivateBodyNextStep(this);
            }

            handle.Data.Velocity = value;
        }
    }

    /// <summary>
    /// Gets or sets the angular velocity of the rigid body in world space.
    /// </summary>
    /// <remarks>
    /// Measured in radians per second. The vector direction is the rotation axis,
    /// and its magnitude is the rotation speed. Setting a non-zero velocity schedules
    /// the body for activation on the next step.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the body's <see cref="MotionType"/> is <see cref="MotionType.Static"/>.
    /// </exception>
    public JVector AngularVelocity
    {
        get => handle.Data.AngularVelocity;
        set
        {
            Debug.Assert(handle.Data.MotionType != MotionType.Static);

            if (handle.Data.MotionType == MotionType.Static)
            {
                // Throw an exception here, since we change the behaviour of the engine with version 2.7.4.
                // Maybe return to assert-only later.
                throw new InvalidOperationException(
                    $"Can not set angular velocity for static objects, objects must be kinematic or dynamic. See {nameof(MotionType)}.");
            }

            if (!MathHelper.CloseToZero(value))
            {
                World.ActivateBodyNextStep(this);
            }

            handle.Data.AngularVelocity = value;
        }
    }

    /// <summary>
    /// Gets or sets whether the body is affected by the world's gravity during integration.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="MotionType"/> is <see cref="MotionType.Dynamic"/>.
    /// Default is <see langword="true"/>.
    /// </remarks>
    public bool AffectedByGravity { get; set; } = true;

    /// <summary>
    /// A managed pointer to custom user data. This is not utilized by the engine.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Gets or sets whether speculative contacts are enabled for this body.
    /// </summary>
    /// <remarks>
    /// Speculative contacts help prevent tunneling for fast-moving bodies by generating
    /// contacts before actual penetration occurs. This may increase contact count and
    /// solver cost. Default is <see langword="false"/>.
    /// </remarks>
    public bool EnableSpeculativeContacts { get; set; } = false;

    private void UpdateWorldInertia()
    {
        if (Data.MotionType == MotionType.Dynamic)
        {
            var bodyOrientation = JMatrix.CreateFromQuaternion(Data.Orientation);
            JMatrix.Multiply(bodyOrientation, inverseInertia, out Data.InverseInertiaWorld);
            JMatrix.MultiplyTransposed(Data.InverseInertiaWorld, bodyOrientation, out Data.InverseInertiaWorld);
            Data.InverseMass = inverseMass;
        }
        else
        {
            Data.InverseInertiaWorld = JMatrix.Zero;
            Data.InverseMass = (Real)0.0;
        }
    }

    /// <summary>
    /// Gets or sets how the rigid body participates in the simulation.
    /// </summary>
    /// <remarks>
    /// Changing this property has immediate side effects:
    /// <list type="bullet">
    /// <item><description>Switching to <see cref="MotionType.Static"/> zeroes velocities, removes connections, and deactivates the body.</description></item>
    /// <item><description>Switching from <see cref="MotionType.Static"/> rebuilds connections from existing contacts.</description></item>
    /// <item><description>Dynamic bodies use their mass and inertia; static and kinematic bodies are treated as having infinite mass by the solver.</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the value is not a valid <see cref="MotionType"/>.</exception>
    public MotionType MotionType
    {
        get => Data.MotionType;
        set
        {
            if (Data.MotionType == value) return;

            switch (value)
            {
                case MotionType.Static:
                    // Switch to static
                    World.RemoveConnections(this);
                    Data.MotionType = MotionType.Static;
                    World.RemoveStaticStaticConstraints(this);
                    World.DeactivateBodyNextStep(this);
                    Data.Velocity = JVector.Zero;
                    Data.AngularVelocity = JVector.Zero;
                    UpdateWorldInertia();
                    break;
                case MotionType.Kinematic:
                {
                    // Switch to kinematic
                    if (Data.MotionType == MotionType.Static)
                    {
                        Data.MotionType = MotionType.Kinematic;
                        World.BuildConnectionsFromExistingContacts(this);
                    }

                    Data.MotionType = MotionType.Kinematic;
                    World.RemoveStaticStaticConstraints(this);
                    World.ActivateBodyNextStep(this);
                    UpdateWorldInertia();
                    break;
                }
                case MotionType.Dynamic:
                {
                    // Switch to dynamic
                    if (Data.MotionType == MotionType.Static)
                    {
                        Data.MotionType = MotionType.Dynamic;
                        World.BuildConnectionsFromExistingContacts(this);
                    }

                    Data.MotionType = MotionType.Dynamic;
                    World.ActivateBodyNextStep(this);
                    UpdateWorldInertia();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }

    [Obsolete($"Use the {nameof(MotionType)} property instead.")]
    public bool IsStatic
    {
        set => MotionType = value ? MotionType.Static : MotionType.Dynamic;
        get => MotionType == MotionType.Static;
    }

    /// <summary>
    /// Indicates whether the rigid body is active or considered to be in a sleeping state.
    /// Use <see cref="SetActivationState"/> to alter the activation state.
    /// </summary>
    public bool IsActive => Data.IsActive;

    /// <summary>
    /// Instructs the engine to activate or deactivate the body at the beginning of
    /// the next time step. The current state does not change immediately.
    /// </summary>
    /// <param name="active">
    /// If <see langword="true"/>, the body will be activated; if <see langword="false"/>, deactivated.
    /// </param>
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
    /// <param name="shapes">The shapes to add.</param>
    /// <param name="setMassInertia">If true, uses the mass properties of the shapes to determine the
    /// body's mass properties, assuming unit density for the shapes. If false, the inertia and mass remain
    /// unchanged.</param>
    /// <exception cref="ArgumentException">Thrown if any shape is already attached to a body.</exception>
    public void AddShape(IEnumerable<RigidBodyShape> shapes, bool setMassInertia = true)
    {
        foreach (RigidBodyShape shape in shapes)
        {
            if (shape.IsRegistered)
            {
                throw new ArgumentException("Shape can not be added. Shape already registered elsewhere.", nameof(shapes));
            }

            AttachToShape(shape);
            this.InternalShapes.Add(shape);
        }

        if (setMassInertia) SetMassInertia();
    }

    /// <summary>
    /// Enables the implicit gyroscopic–torque solver for this <see cref="RigidBody"/>.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, every sub-step performs an extra Newton iteration to solve
    /// <c>ω × (I ω)</c> implicitly.
    ///
    /// The benefit becomes noticeable for bodies with a high inertia anisotropy or very fast
    /// spin-rates. Typical examples are long, thin rods, spinning tops, propellers, and other objects
    /// whose principal inertias differ by an order of magnitude. In those cases the flag eliminates artificial
    /// precession.
    /// </remarks>
    /// <value>
    /// <see langword="true"/> to integrate gyroscopic torque each step; otherwise
    /// <see langword="false"/> (default).
    /// </value>
    public bool EnableGyroscopicForces
    {
        get => Data.EnableGyroscopicForces;
        set => Data.EnableGyroscopicForces = value;
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
    /// Applies a force to the rigid body, thereby altering its velocity.
    /// </summary>
    /// <param name="force">
    /// The force to be applied. This force is effective for a single frame only and is reset
    /// to zero during the next call to <see cref="World.Step(Real, bool)"/>.
    /// </param>
    /// <param name="wakeup">
    /// If <c>true</c> (default), the body will be activated if it is currently sleeping.
    /// If <c>false</c>, the force is only applied if the body is already active; sleeping
    /// bodies will remain asleep and ignore the force.
    /// </param>
    public void AddForce(in JVector force, bool wakeup = true)
    {
        if ((Data.MotionType != MotionType.Dynamic) || MathHelper.CloseToZero(force)) return;

        if(wakeup) SetActivationState(true);
        else if (!IsActive) return;

        Force += force;
    }

    /// <summary>
    /// Applies a force to the rigid body, altering its velocity. This force is applied for a single frame only and is
    /// reset to zero with the following call to <see cref="World.Step(Real, bool)"/>.
    /// </summary>
    /// <param name="force">The force to be applied.</param>
    /// <param name="position">The position where the force will be applied.</param>
    /// <param name="wakeup">
    /// If <c>true</c> (default), the body will be activated if it is currently sleeping.
    /// If <c>false</c>, the force is only applied if the body is already active; sleeping
    /// bodies will remain asleep and ignore the force.
    /// </param>
    [ReferenceFrame(ReferenceFrame.World)]
    public void AddForce(in JVector force, in JVector position, bool wakeup = true)
    {
        if ((Data.MotionType != MotionType.Dynamic) || MathHelper.CloseToZero(force)) return;

        if(wakeup) SetActivationState(true);
        else if (!IsActive) return;

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
    /// Computes the mass and inertia of this body from all attached shapes, assuming unit density.
    /// </summary>
    /// <remarks>
    /// The mass contributions of all shapes are summed. If no shapes are attached, the body
    /// is assigned a mass of 1 and an identity inertia tensor.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the computed inertia matrix is not invertible. This may occur if a shape has invalid mass properties.
    /// </exception>
    public void SetMassInertia()
    {
        if (InternalShapes.Count == 0)
        {
            inverseInertia = JMatrix.Identity;
            inverseMass = (Real)1.0;
            UpdateWorldInertia();
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
    /// Computes the inertia from all attached shapes, then uniformly scales it to match the specified mass.
    /// </summary>
    /// <remarks>
    /// This is equivalent to calling <see cref="SetMassInertia()"/> and then scaling the resulting
    /// inertia tensor so the body has the desired total mass. Use this when you want shape-derived
    /// inertia proportions but a specific total mass (e.g., for gameplay tuning).
    /// </remarks>
    /// <param name="mass">The desired total mass of the body. Must be positive.</param>
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
        inverseInertia = JMatrix.Multiply(inverseInertia, (Real)1.0 / (inverseMass * mass));
        inverseMass = (Real)1.0 / mass;
        UpdateWorldInertia();
    }

    /// <summary>
    /// Sets the new mass properties of this body by specifying both inertia and mass directly.
    /// </summary>
    /// <param name="inertia">
    /// The inertia tensor (or inverse inertia tensor if <paramref name="setAsInverse"/> is true)
    /// in body (local) space, about the center of mass.
    /// </param>
    /// <param name="mass">
    /// The mass (or inverse mass if <paramref name="setAsInverse"/> is true). When setting inverse
    /// mass, a value of zero represents infinite mass.
    /// </param>
    /// <param name="setAsInverse">
    /// If <see langword="true"/>, <paramref name="inertia"/> and <paramref name="mass"/> are
    /// interpreted as inverse values.
    /// </param>
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
    /// <remarks>
    /// This method tessellates all attached shapes and is not suitable for real-time use.
    /// It uses a shared static list internally and is not thread-safe.
    /// </remarks>
    /// <param name="drawer">The debug drawer to receive the generated triangles.</param>
    public void DebugDraw(IDebugDrawer drawer)
    {
        _debugTriangles ??= [];

        foreach (var shape in InternalShapes)
        {
            ShapeHelper.Tessellate(shape, _debugTriangles);

            foreach (var tri in _debugTriangles)
            {
                drawer.DrawTriangle(
                    JVector.Transform(tri.V0, Data.Orientation) + Data.Position,
                    JVector.Transform(tri.V1, Data.Orientation) + Data.Position,
                    JVector.Transform(tri.V2, Data.Orientation) + Data.Position);
            }

            _debugTriangles.Clear();
        }
    }

    /// <summary>
    /// Gets the mass of the rigid body. To modify the mass, use
    /// <see cref="RigidBody.SetMassInertia(Real)"/> or
    /// <see cref="RigidBody.SetMassInertia(in JMatrix, Real, bool)"/>.
    /// </summary>
    /// <remarks>
    /// This value is only meaningful for <see cref="MotionType.Dynamic"/> bodies.
    /// Static and kinematic bodies are treated as having infinite mass by the solver
    /// regardless of this value.
    /// </remarks>
    public Real Mass => (Real)1.0 / inverseMass;

    int IPartitionedSetIndex.SetIndex { get; set; } = -1;
}