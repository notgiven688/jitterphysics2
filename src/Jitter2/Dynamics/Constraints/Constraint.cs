/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Runtime.InteropServices;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// Low-level data for constraints that fit within <see cref="Precision.ConstraintSizeSmall"/> bytes.
/// </summary>
/// <remarks>
/// <para>
/// This structure is stored in unmanaged memory and accessed via <see cref="Constraint.SmallHandle"/>.
/// It contains function pointers for the solver and handles to the connected bodies.
/// </para>
/// <para>
/// The data is valid only while the constraint is registered with the world. Do not cache references
/// across simulation steps. Not safe to access concurrently with <see cref="World.Step(Real, bool)"/>.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = Precision.ConstraintSizeSmall)]
public unsafe struct SmallConstraintData
{
    internal int _internal;
    /// <summary>Function pointer to the constraint's iteration solver.</summary>
    public delegate*<ref SmallConstraintData, Real, void> Iterate;
    /// <summary>Function pointer to the constraint's pre-iteration setup.</summary>
    public delegate*<ref SmallConstraintData, Real, void> PrepareForIteration;

    /// <summary>Handle to the first body's simulation data.</summary>
    public JHandle<RigidBodyData> Body1;
    /// <summary>Handle to the second body's simulation data.</summary>
    public JHandle<RigidBodyData> Body2;
}

/// <summary>
/// Low-level data for constraints, stored in unmanaged memory.
/// </summary>
/// <remarks>
/// <para>
/// This structure is stored in unmanaged memory and accessed via <see cref="Constraint.Handle"/>.
/// It contains function pointers for the solver and handles to the connected bodies.
/// </para>
/// <para>
/// The data is valid only while the constraint is registered with the world. Do not cache references
/// across simulation steps. Not safe to access concurrently with <see cref="World.Step(Real, bool)"/>.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = Precision.ConstraintSizeFull)]
public unsafe struct ConstraintData
{
    internal int _internal;
    /// <summary>Function pointer to the constraint's iteration solver.</summary>
    public delegate*<ref ConstraintData, Real, void> Iterate;
    /// <summary>Function pointer to the constraint's pre-iteration setup.</summary>
    public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

    /// <summary>Handle to the first body's simulation data.</summary>
    public JHandle<RigidBodyData> Body1;
    /// <summary>Handle to the second body's simulation data.</summary>
    public JHandle<RigidBodyData> Body2;
}

/// <summary>
/// Generic base class for constraints that store custom data of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">
/// The unmanaged data structure containing constraint-specific state. Must fit within
/// <see cref="ConstraintData"/> (i.e., <see cref="Precision.ConstraintSizeFull"/> bytes).
/// </typeparam>
/// <remarks>
/// Derive from this class to create constraints with custom data layouts. The <see cref="Data"/>
/// property provides typed access to the constraint's unmanaged memory.
/// </remarks>
public abstract class Constraint<T> : Constraint where T : unmanaged
{
    /// <summary>
    /// Gets a reference to the constraint's typed data stored in unmanaged memory.
    /// </summary>
    public ref T Data => ref JHandle<ConstraintData>.AsHandle<T>(Handle).Data;

    /// <inheritdoc />
    public override unsafe bool IsSmallConstraint => sizeof(T) <= sizeof(SmallConstraintData);

    /// <inheritdoc />
    protected override void Create() => Constraint.CheckDataSize<T>();
}

/// <summary>
/// Base class for constraints that connect two rigid bodies and restrict their relative motion.
/// </summary>
public abstract class Constraint : IDebugDrawable
{
    /// <summary>Gets the first rigid body connected by this constraint.</summary>
    public RigidBody Body1 { private set; get; } = null!;

    /// <summary>Gets the second rigid body connected by this constraint.</summary>
    public RigidBody Body2 { private set; get; } = null!;

    /// <summary>Gets whether this constraint uses the smaller data layout.</summary>
    public virtual bool IsSmallConstraint { get; } = false;

    /// <summary>
    /// A handle for accessing the raw constraint data.
    /// </summary>
    public JHandle<ConstraintData> Handle { internal set; get; }

    /// <summary>
    /// Gets a handle to the constraint data reinterpreted as <see cref="SmallConstraintData"/>.
    /// </summary>
    public JHandle<SmallConstraintData> SmallHandle => JHandle<ConstraintData>.AsHandle<SmallConstraintData>(Handle);

    /// <summary>
    /// Checks that the constraint data type fits within <see cref="ConstraintData"/>.
    /// </summary>
    /// <typeparam name="T">The constraint-specific data type.</typeparam>
    /// <exception cref="InvalidOperationException">Thrown if the data type is too large.</exception>
    protected static unsafe void CheckDataSize<T>() where T : unmanaged
    {
        if (sizeof(T) > sizeof(ConstraintData))
        {
            throw new InvalidOperationException("The size of the constraint data is too large.");
        }
    }

    /// <summary>
    /// Verifies that this constraint has been properly created via the World class.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the constraint was instantiated directly instead of through
    /// <see cref="World.CreateConstraint{T}(RigidBody, RigidBody)"/>.
    /// </exception>
    protected void VerifyNotZero()
    {
        if (Handle.IsZero)
        {
            throw new InvalidOperationException(
                $"The constraint has not been created by the world. " +
                $"Use World.CreateConstraint<{GetType().Name}>() to create constraints.");
        }
    }

    /// <summary>
    /// Sets the <see cref="Iterate"/> and <see cref="PrepareForIteration"/> function pointer
    /// fields on this instance. Override this in derived classes to assign the correct solver
    /// methods. The pointers are later written into <see cref="ConstraintData"/> when the
    /// constraint is enabled.
    /// </summary>
    protected virtual void Create()
    {
    }

    protected unsafe delegate*<ref ConstraintData, Real, void> Iterate = null;
    protected unsafe delegate*<ref ConstraintData, Real, void> PrepareForIteration = null;

    /// <summary>
    /// Gets or sets whether this constraint is enabled.
    /// </summary>
    /// <remarks>
    /// Use <see cref="World.Remove(Constraint)"/> for permanent removal.
    /// </remarks>
    public unsafe bool IsEnabled
    {
        get => Handle.Data.Iterate != null;
        set
        {
            Handle.Data.Iterate = value ? Iterate : null;
            Handle.Data.PrepareForIteration = value ? PrepareForIteration : null;
        }
    }

    internal void Create(JHandle<SmallConstraintData> handle, RigidBody body1, RigidBody body2)
    {
        var cd = JHandle<SmallConstraintData>.AsHandle<ConstraintData>(handle);
        Create(cd, body1, body2);
    }

    internal void Create(JHandle<ConstraintData> handle, RigidBody body1, RigidBody body2)
    {
        Body1 = body1;
        Body2 = body2;
        Handle = handle;

        handle.Data.Body1 = body1.Handle;
        handle.Data.Body2 = body2.Handle;

        Create();

        IsEnabled = true;
    }

    /// <summary>
    /// Draws a debug visualization of this constraint.
    /// </summary>
    /// <param name="drawer">The debug drawer to receive visualization primitives.</param>
    /// <exception cref="NotImplementedException">Thrown if the derived class does not override this method.</exception>
    public virtual void DebugDraw(IDebugDrawer drawer)
    {
        throw new NotImplementedException();
    }
}