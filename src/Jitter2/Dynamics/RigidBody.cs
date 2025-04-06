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
using System.Runtime.InteropServices;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.DataStructures;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics;

[StructLayout(LayoutKind.Sequential)]
public struct RigidBodyData
{
    public int _index;
    public int _lockFlag;

    public JVector Position;
    public JVector Velocity;
    public JVector AngularVelocity;

    public JVector DeltaVelocity;
    public JVector DeltaAngularVelocity;

    public JQuaternion Orientation;
    public JMatrix InverseInertiaWorld;

    public Real InverseMass;
    public bool IsActive;
    public bool IsStatic;

    public readonly bool IsStaticOrInactive => !IsActive || IsStatic;
}

/// <summary>
/// Represents the primary entity in the Jitter physics world.
/// </summary>
public sealed class RigidBody : IPartitionedSetIndex, IDebugDrawable
{
    internal JHandle<RigidBodyData> handle;

    public readonly ulong RigidBodyId;

    private Real restitution = (Real)0.0;
    private Real friction = (Real)0.2;

    /// <summary>
    /// Due to performance considerations, the data used to simulate this body (e.g., velocity or position)
    /// is stored within a contiguous block of unmanaged memory. This refers to the raw memory location
    /// and should seldom, if ever, be utilized outside of the engine. Instead, use the properties provided
    /// by the <see cref="RigidBody"/> class itself.
    /// </summary>
    public ref RigidBodyData Data => ref handle.Data;

    /// <summary>
    /// Gets the handle to the rigid body data, see <see cref="Data"/>.
    /// </summary>
    public JHandle<RigidBodyData> Handle => handle;

    internal readonly List<RigidBodyShape> shapes = new(1);

    // There is only one way to create a body: world.CreateRigidBody. There, we add an island
    // to the new body. This should never be null.
    internal Island island = null!;

    /// <summary>
    /// Gets the collision island associated with this rigid body.
    /// </summary>
    public Island Island => island;

    internal readonly List<RigidBody> connections = new(0);
    internal readonly HashSet<Arbiter> contacts = new(0);
    internal readonly HashSet<Constraint> constraints = new(0);

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
    /// Contains all bodies this body is in contact with.
    /// </summary>
    public ReadOnlyList<RigidBody> Connections => new(connections);

    /// <summary>
    /// Contains all contacts in which this body is involved.
    /// </summary>
    public ReadOnlyHashSet<Arbiter> Contacts => new(contacts);

    /// <summary>
    /// Contains all constraints connected to this body.
    /// </summary>
    public ReadOnlyHashSet<Constraint> Constraints => new(constraints);

    /// <summary>
    /// Gets the list of shapes added to this rigid body.
    /// </summary>
    public ReadOnlyList<RigidBodyShape> Shapes => new(shapes);

    internal int islandMarker;

    internal Real sleepTime = (Real)0.0;

    internal Real inactiveThresholdLinearSq = (Real)0.1;
    internal Real inactiveThresholdAngularSq = (Real)0.1;
    internal Real deactivationTimeThreshold = (Real)1.0;

    internal Real linearDampingMultiplier = (Real)0.998;
    internal Real angularDampingMultiplier = (Real)0.995;

    internal JMatrix inverseInertia = JMatrix.Identity;
    internal Real inverseMass = (Real)1.0;

    /// <remarks>
    /// The friction coefficient determines the resistance to sliding motion.
    /// Values typically range from 0 (no friction) upwards.
    /// Higher values represent strong friction or adhesion effects.
    /// Default is 0.2.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the value is negative.
    /// </exception>
    public Real Friction
    {
        get => friction;
        set
        {
            if (value < (Real)0.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Friction must be non-negative.");
            }

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
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the value is not between 0 and 1.
    /// </exception>
    public Real Restitution
    {
        get => restitution;
        set
        {
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
    public (Real angular, Real linear) DeactivationThreshold
    {
        get => (MathR.Sqrt(inactiveThresholdAngularSq), MathR.Sqrt(inactiveThresholdLinearSq));
        set
        {
            if (value.linear < 0 || value.angular < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Both linear and angular thresholds must be non-negative.");
            }

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
    /// The damping factors should be within the range [0, 1].
    /// </remarks>
    public (Real linear, Real angular) Damping
    {
        get => ((Real)1.0 - linearDampingMultiplier, (Real)1.0 - angularDampingMultiplier);
        set
        {
            if (value.linear < (Real)0.0 || value.linear > (Real)1.0 || value.angular < (Real)0.0 || value.angular > (Real)1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Damping multiplier has to be within [0, 1].");
            }

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

        foreach (var shape in shapes)
        {
            World.DynamicTree.Update(shape);
        }

        World.ActivateBodyNextStep(this);
    }

    public JVector Velocity
    {
        get => handle.Data.Velocity;
        set => handle.Data.Velocity = value;
    }

    public JVector AngularVelocity
    {
        get => handle.Data.AngularVelocity;
        set => handle.Data.AngularVelocity = value;
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
            this.shapes.Add(shape);
        }

        if (setMassInertia) SetMassInertia();
    }

    /// <summary>
    /// Adds a shape to the body.
    /// </summary>
    /// <param name="shape">The shape to be added.</param>
    /// <param name="setMassInertia">If true, utilizes the shape's mass properties to determine the body's
    /// mass properties, assuming a unit density for the shape. If false, the inertia and mass remain unchanged.</param>
    public void AddShape(RigidBodyShape shape, bool setMassInertia = true)
    {
        if (shape.IsRegistered)
        {
            throw new ArgumentException("Shape can not be added. Is the shape already registered?");
        }

        AttachToShape(shape);
        shapes.Add(shape);
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
        ref RigidBodyData data = ref Data;

        if (data.IsStatic) return;

        JVector.Subtract(position, data.Position, out JVector torque);
        JVector.Cross(torque, force, out torque);

        Force += force;
        Torque += torque;
    }

    /// <summary>
    /// Removes a specified shape from the rigid body.
    /// </summary>
    /// <remarks>This operation has a time complexity of O(n), where n is the number of shapes attached to the body.</remarks>
    /// <param name="shape">The shape to remove from the rigid body.</param>
    /// <param name="setMassInertia">Specifies whether to adjust the mass inertia properties of the rigid body after removing the shape. The default value is true.</param>
    public void RemoveShape(RigidBodyShape shape, bool setMassInertia = true)
    {
        if (!shapes.Remove(shape))
        {
            throw new ArgumentException(
                "Shape is not part of this body.");
        }

        foreach (var arbiter in contacts)
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
    public void RemoveShape(IEnumerable<RigidBodyShape> shapes, bool setMassInertia = true)
    {
        HashSet<ulong> sids = new HashSet<ulong>();

        foreach (var shape in shapes)
        {
            if (shape.RigidBody != this)
            {
                throw new ArgumentException($"Shape {shape} is not attached to this body.", nameof(shapes));
            }

            sids.Add(shape.ShapeId);
        }

        foreach (var arbiter in contacts)
        {
            if (sids.Contains(arbiter.Handle.Data.Key.Key1) || sids.Contains(arbiter.Handle.Data.Key.Key2))
            {
                // Removes the current element we are iterating over from Contacts, i.e. the HashSet
                // we are iterating over is altered. This is allowed.
                World.Remove(arbiter);
            }
        }

        for (int i = this.shapes.Count; i-- > 0;)
        {
            var shape = this.shapes[i];

            if (sids.Contains(shape.ShapeId))
            {
                World.DynamicTree.RemoveProxy(shape);
                shape.RigidBody = null!;
                this.shapes.RemoveAt(i);
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
        RemoveShape(shapes, setMassInertia);
    }

    /// <summary>
    /// Utilizes the mass properties of the shape to determine the mass properties of the rigid body.
    /// </summary>
    public void SetMassInertia()
    {
        if (shapes.Count == 0)
        {
            inverseInertia = JMatrix.Identity;
            Data.InverseMass = (Real)1.0;
            return;
        }

        JMatrix inertia = JMatrix.Zero;
        Real mass = (Real)0.0;

        for (int i = 0; i < shapes.Count; i++)
        {
            shapes[i].CalculateMassInertia(out var shapeInertia, out _, out var shapeMass);

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
    /// <exception cref="ArgumentException"></exception>
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

    private static List<JTriangle>? debugTriangles;

    /// <summary>
    /// Generates a rough triangle approximation of the shapes of the body.
    /// Since the generation is slow this should only be used for debugging
    /// purposes.
    /// </summary>
    public void DebugDraw(IDebugDrawer drawer)
    {
        debugTriangles ??= new List<JTriangle>();

        foreach (var shape in shapes)
        {
            ShapeHelper.MakeHull(shape, debugTriangles);

            foreach (var tri in debugTriangles)
            {
                drawer.DrawTriangle(
                    JVector.Transform(tri.V0, Data.Orientation) + Data.Position,
                    JVector.Transform(tri.V1, Data.Orientation) + Data.Position,
                    JVector.Transform(tri.V2, Data.Orientation) + Data.Position);
            }
        }

        debugTriangles.Clear();
    }

    /// <summary>
    /// Gets the mass of the rigid body. To modify the mass, use
    /// <see cref="RigidBody.SetMassInertia(Real)"/> or
    /// <see cref="RigidBody.SetMassInertia(in JMatrix, Real, bool)"/>.
    /// </summary>
    public Real Mass => (Real)1.0 / inverseMass;

    int IPartitionedSetIndex.SetIndex { get; set; } = -1;
}