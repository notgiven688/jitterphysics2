/*
 * Copyright (c) 2009-2023 Thorben Linneweber and others
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
using Jitter2.UnmanagedMemory;

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

    public JMatrix Orientation;
    public JMatrix InverseInertiaWorld;

    public float InverseMass;
    public bool IsActive;
    public bool IsStatic;

    public readonly bool IsStaticOrInactive => !IsActive || IsStatic;
}

/// <summary>
/// Rigid body class. Represents the main entity in the Jitter <see cref="Jitter2.World"/>.
/// </summary>
public sealed class RigidBody : IListIndex, IDebugDrawable
{
    internal JHandle<RigidBodyData> handle;

    /// <summary>
    /// For performance reasons data used to simulate this body (e.g. velocity or position) is
    /// stored within a continuous block of unmanaged memory. This points to the raw memory location
    /// and should rarely or never be used outside of the engine. Use the properties provided by the
    /// <see cref="RigidBody"/> class itself.
    /// </summary>
    public ref RigidBodyData Data => ref handle.Data;

    /// <summary>
    /// Access the handle to the rigid body data, <see cref="Data"/>.
    /// </summary>
    public JHandle<RigidBodyData> Handle => handle;

    internal readonly List<Shape> shapes = new(1);

    // There is only one way to create a body: world.CreateRigidBody. There we add an island
    // to the new body. Should never be null.
    internal Island island = null!;

    /// <summary>
    /// The collision island associated with this rigid body.
    /// </summary>
    public Island Island => island;

    /// <summary>
    /// Contains all bodies this body is in contact with. Should only
    /// be modified within Jitter.
    /// </summary>
    public readonly List<RigidBody> Connections = new();

    /// <summary>
    /// All contacts the body is involved in. Should only
    /// be modified within Jitter.
    /// </summary>
    public readonly HashSet<Arbiter> Contacts = new(5);

    /// <summary>
    /// All constraints the body is connected to. Should only
    /// be modified within Jitter.
    /// </summary>
    public readonly HashSet<Constraint> Constraints = new(5);

    internal int islandMarker;

    internal float sleepTime = 0.0f;

    internal float inactiveThresholdLinearSq = 0.1f;
    internal float inactiveThresholdAngularSq = 0.1f;
    internal float deactivationTimeThreshold = 1.0f;

    internal float linearDamping = 0.995f;
    internal float angularDamping = 0.995f;

    internal JMatrix inverseInertia = JMatrix.Identity;
    internal float mass = 1.0f;

    /// <summary>
    /// List of shapes added to this rigid body.
    /// </summary>
    public ReadOnlyList<Shape> Shapes { get; }

    public float Friction { get; set; } = 0.2f;
    public float Restitution { get; set; } = 0.0f;

    private readonly int hashCode;

    private static int hashCounter;

    /// <summary>
    /// The world assigned to this body.
    /// </summary>
    public World World { get; }

    internal RigidBody(JHandle<RigidBodyData> handle, World world)
    {
        this.handle = handle;
        World = world;

        Shapes = new ReadOnlyList<Shape>(shapes);

        Data.Orientation = JMatrix.Identity;
        SetDefaultMassInertia();

        int h = hashCounter++;

        // The rigid body is used in hash-based data structures, provide a
        // good hash - Thomas Wang, Jan 1997
        h = h ^ 61 ^ (h >>> 16);
        h += h << 3;
        h ^= h >>> 4;
        h *= 0x27d4eb2d;
        h ^= h >>> 15;

        hashCode = h;

        Data._lockFlag = 0;
    }

    /// <summary>
    /// If the rigid body's angular and linear velocity magnitude are both below
    /// <see cref="DeactivationThreshold"/> for the specified time the body gets deactivated.
    /// </summary>
    public TimeSpan DeactivationTime
    {
        get => TimeSpan.FromSeconds(deactivationTimeThreshold);
        set => deactivationTimeThreshold = (float)value.TotalSeconds;
    }

    /// <summary>
    /// If the rigid body's angular and linear velocity magnitude are both below the specified values
    /// for <see cref="DeactivationTime" /> the body gets deactivated.
    /// </summary>
    /// <value>Velocity threshold in rad/s and length units/s, respectively.</value>
    public (float angular, float linear) DeactivationThreshold
    {
        set
        {
            inactiveThresholdLinearSq = value.linear * value.linear;
            inactiveThresholdAngularSq = value.angular * value.angular;
        }
    }

    /// <summary>
    /// Specify damping factors.
    /// </summary>
    /// <value>
    /// Angular and linear velocities get multiplied by these values every step.
    /// The values are not scaled by time, i.e. a smaller timestep in
    /// <see cref="World.Step(float, bool)"/> results in increased damping.
    /// </value>
    public (float angular, float linear) Damping
    {
        get => (linearDamping, angularDamping);
        set => (linearDamping, angularDamping) = value;
    }

    public override int GetHashCode()
    {
        return hashCode;
    }

    private void SetDefaultMassInertia()
    {
        inverseInertia = JMatrix.Identity;
        Data.InverseMass = 1.0f;
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

    public JMatrix Orientation
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
        foreach (Shape shape in shapes)
        {
            World.UpdateShape(shape);
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
    /// A managed pointer to custom user data. Not used by the engine.
    /// </summary>
    public object? Tag { get; set; }

    public bool EnableSpeculativeContacts { get; set; } = false;

    private void UpdateWorldInertia()
    {
        if (Data.IsStatic)
        {
            Data.InverseInertiaWorld = JMatrix.Zero;
            Data.InverseMass = 0.0f;
        }
        else
        {
            JMatrix.Multiply(Data.Orientation, inverseInertia, out Data.InverseInertiaWorld);
            JMatrix.MultiplyTransposed(Data.InverseInertiaWorld, Data.Orientation, out Data.InverseInertiaWorld);
            Data.InverseMass = 1.0f / mass;
        }
    }

    public bool IsStatic
    {
        set
        {
            if ((!Data.IsStatic && value) || (Data.IsStatic && !value))
            {
                Data.Velocity = JVector.Zero;
                Data.AngularVelocity = JVector.Zero;
            }

            Data.IsStatic = value;
            UpdateWorldInertia();

            if (value) World.DeactivateBodyNextStep(this);
            else World.ActivateBodyNextStep(this);
        }
        get => Data.IsStatic;
    }

    /// <summary>
    /// Returns if the rigid body is active or considered as sleeping.
    /// Call <see cref="SetActivationState"/> to change the activation state.
    /// </summary>
    public bool IsActive => Data.IsActive;

    /// <summary>
    /// Tells jitter to activate or deactivate the body at the beginning of
    /// the next time step. The current state is not changed immediately.
    /// </summary>
    public void SetActivationState(bool active)
    {
        if (active) World.ActivateBodyNextStep(this);
        else World.DeactivateBodyNextStep(this);
    }

    private void AttachToShape(Shape shape)
    {
        if (!shape.AttachRigidBody(this))
        {
            throw new InvalidOperationException("Shape has already been added to another body.");
        }

        if (shape.Mass == 0)
        {
            throw new ArgumentException("Tried to add a shape with zero mass to a rigid body. " +
                                        $"If you are using custom shapes make sure to call {nameof(Shape.UpdateShape)}.",
                nameof(shape));
        }

        shape.UpdateWorldBoundingBox();
        World.AddShape(shape);
    }

    /// <summary>
    /// Add several shapes add once to the rigid body. Mass properties are
    /// only recalculated once (if requested).
    /// </summary>
    /// <param name="shapes">Shapes to add.</param>
    /// <param name="setMassInertia">If true, uses the shape's mass properties to get the bodies
    /// mass properties. Assumes unit density for the shapes. If false, inertia and mass are not
    /// changed.</param>
    public void AddShape(IEnumerable<Shape> shapes, bool setMassInertia = true)
    {
        foreach (Shape shape in shapes)
        {
            AttachToShape(shape);
        }

        this.shapes.AddRange(shapes);
        if (setMassInertia) SetMassInertia();
    }

    /// <summary>
    /// Adds a shape to the body.
    /// </summary>
    /// <param name="shape">Shape to add.</param>
    /// <param name="setMassInertia">If true, uses the shape's mass properties to get the bodies
    /// mass properties. Assumes unit density for the shape. If false, inertia and mass are not
    /// changed.</param>
    public void AddShape(Shape shape, bool setMassInertia = true)
    {
        AttachToShape(shape);
        shapes.Add(shape);
        if (setMassInertia) SetMassInertia();
    }

    /// <summary>
    /// Force added to the body with the next call to <see cref="World.Step(float, bool)"/>.
    /// The value is automatically set to zero afterwards.
    /// </summary>
    public JVector Force { get; set; }

    /// <summary>
    /// Torque added to the body with the next call to <see cref="World.Step(float, bool)"/>.
    /// The value is automatically set to zero afterwards.
    /// </summary>
    public JVector Torque { get; set; }

    /// <summary>
    /// Adds force to the rigid body which changes the body's velocity. The force
    /// is added for one frame only, i.e. it is set to zero with the next call
    /// to <see cref="World.Step(float, bool)"/>.
    /// </summary>
    /// <param name="force">Force to add.</param>
    public void AddForce(in JVector force)
    {
        Force += force;
    }

    /// <summary>
    /// Adds force to the rigid body which changes the body's velocity. The force
    /// is added for one frame only, i.e. it is set to zero with the next call
    /// to <see cref="World.Step(float, bool)"/>.
    /// </summary>
    /// <param name="force">Force to add.</param>
    /// <param name="pos">Position where the force is added.</param>
    public void AddForce(in JVector force, in JVector pos)
    {
        ref RigidBodyData data = ref Data;

        if (data.IsStatic) return;

        JVector.Subtract(pos, data.Position, out JVector torque);
        JVector.Cross(torque, force, out torque);

        Force += force;
        Torque += torque;
    }

    /// <summary>
    /// Removes a shape from the rigid body.
    /// </summary>
    public void RemoveShape(Shape shape, bool setMassInertia = true)
    {
        if (!shapes.Remove(shape))
        {
            throw new InvalidOperationException(
                "Shape is not part of this body.");
        }

        Stack<Arbiter> toRemoveArbiter = new();

        foreach (var contact in Contacts)
        {
            if (contact.Shape1 == shape || contact.Shape2 == shape)
            {
                toRemoveArbiter.Push(contact);
            }
        }

        while (toRemoveArbiter.Count > 0)
        {
            var tr = toRemoveArbiter.Pop();
            World.Remove(tr);
        }

        shape.DetachRigidBody();
        World.RemoveShape(shape);

        if (setMassInertia) SetMassInertia();
    }

    /// <summary>
    /// Removes all shapes from the rigid body.
    /// </summary>
    /// <param name="setMassInertia">Does not change the mass properties of the rigid body if set to
    /// false.</param>
    public void ClearShapes(bool setMassInertia = true)
    {
        foreach (Shape shape in shapes)
            shape.DetachRigidBody();
        shapes.Clear();
        if (setMassInertia) SetMassInertia();
    }

    /// <summary>
    /// Uses the shape mass properties to calculate the mass properties of the rigid body.
    /// </summary>
    public void SetMassInertia()
    {
        if (shapes.Count == 0)
        {
            inverseInertia = JMatrix.Identity;
            Data.InverseMass = 1.0f;
            return;
        }

        JMatrix inertia = JMatrix.Zero;
        float mass = 0.0f;

        for (int i = 0; i < shapes.Count; i++)
        {
            inertia += shapes[i].Inertia;
            mass += shapes[i].Mass;
        }

        JMatrix.Inverse(inertia, out inverseInertia);
        this.mass = mass;

        UpdateWorldInertia();
    }

    /// <summary>
    /// Sets a new mass and scales the inertia by the ratio of the old mass and the new mass.
    /// </summary>
    public void SetMassInertia(float mass)
    {
        SetMassInertia();
        inverseInertia = JMatrix.Multiply(inverseInertia, 1.0f / (Data.InverseMass * mass));
        this.mass = mass;
        UpdateWorldInertia();
    }

    /// <summary>
    /// Sets new mass properties for this body by specifying inertia and mass directly.
    /// </summary>
    public void SetMassInertia(in JMatrix inertia, float mass)
    {
        JMatrix.Inverse(inertia, out JMatrix invinertia);
        inverseInertia = invinertia;
        this.mass = mass;
        UpdateWorldInertia();
    }

    public void DebugDraw(IDebugDrawer drawer)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the mass of the rigid body. Use <see cref="RigidBody.SetMassInertia(float)"/>,
    /// <see cref="RigidBody.SetMassInertia(in JMatrix, float)"/> to modify the mass.
    /// </summary>
    public float Mass => mass;

    int IListIndex.ListIndex { get; set; } = -1;
}