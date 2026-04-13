/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitter2.Unmanaged;

namespace Jitter2.Dynamics.Constraints;

/// <summary>
/// Low-level data for constraints that fit within <see cref="Precision.ConstraintSizeSmall"/> bytes.
/// </summary>
/// <remarks>
/// <para>
/// This structure is stored in unmanaged memory and accessed via <see cref="Constraint.SmallHandle"/>.
/// It contains a solver dispatch id, a stable constraint identifier, and handles to the connected bodies.
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
    /// <summary>Identifier of the registered solver dispatch pair for this constraint.</summary>
    public uint DispatchId;
    /// <summary>Stable creation-order identifier used for deterministic sorting.</summary>
    public ulong ConstraintId;

    /// <summary>Handle to the first body's simulation data.</summary>
    public JHandle<RigidBodyData> Body1;
    /// <summary>Handle to the second body's simulation data.</summary>
    public JHandle<RigidBodyData> Body2;

    /// <summary>Gets whether this constraint is enabled.</summary>
    public readonly bool IsEnabled => DispatchId != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void PrepareForIteration(ref SmallConstraintData constraint, Real idt)
    {
        ref readonly var dispatch = ref ConstraintDispatchTable.Get(DispatchId);
        ((delegate*<ref SmallConstraintData, Real, void>)dispatch.Prepare)(ref constraint, idt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Iterate(ref SmallConstraintData constraint, Real idt)
    {
        ref readonly var dispatch = ref ConstraintDispatchTable.Get(DispatchId);
        ((delegate*<ref SmallConstraintData, Real, void>)dispatch.Iterate)(ref constraint, idt);
    }
}

/// <summary>
/// Low-level data for constraints, stored in unmanaged memory.
/// </summary>
/// <remarks>
/// <para>
/// This structure is stored in unmanaged memory and accessed via <see cref="Constraint.Handle"/>.
/// It contains a solver dispatch id, a stable constraint identifier, and handles to the connected bodies.
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
    /// <summary>Identifier of the registered solver dispatch pair for this constraint.</summary>
    public uint DispatchId;
    /// <summary>Stable creation-order identifier used for deterministic sorting.</summary>
    public ulong ConstraintId;

    /// <summary>Handle to the first body's simulation data.</summary>
    public JHandle<RigidBodyData> Body1;
    /// <summary>Handle to the second body's simulation data.</summary>
    public JHandle<RigidBodyData> Body2;

    /// <summary>Gets whether this constraint is enabled.</summary>
    public readonly bool IsEnabled => DispatchId != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void PrepareForIteration(ref ConstraintData constraint, Real idt)
    {
        ref readonly var dispatch = ref ConstraintDispatchTable.Get(DispatchId);
        ((delegate*<ref ConstraintData, Real, void>)dispatch.Prepare)(ref constraint, idt);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Iterate(ref ConstraintData constraint, Real idt)
    {
        ref readonly var dispatch = ref ConstraintDispatchTable.Get(DispatchId);
        ((delegate*<ref ConstraintData, Real, void>)dispatch.Iterate)(ref constraint, idt);
    }
}

internal static unsafe class ConstraintDispatchTable
{
    internal readonly struct Entry(nint prepare, nint iterate)
    {
        public readonly nint Prepare = prepare;
        public readonly nint Iterate = iterate;
    }

    private static readonly object Sync = new();
    private static readonly List<Entry> Entries = [default];

    public static uint Register(
        delegate*<ref ConstraintData, Real, void> prepare,
        delegate*<ref ConstraintData, Real, void> iterate)
    {
        if (prepare == null) throw new ArgumentNullException(nameof(prepare));
        if (iterate == null) throw new ArgumentNullException(nameof(iterate));

        lock (Sync)
        {
            if (Entries.Count == int.MaxValue)
            {
                throw new InvalidOperationException("Too many registered constraint dispatch entries.");
            }

            Entries.Add(new Entry((nint)prepare, (nint)iterate));
            return (uint)(Entries.Count - 1);
        }
    }

    public static uint Register(
        delegate*<ref SmallConstraintData, Real, void> prepare,
        delegate*<ref SmallConstraintData, Real, void> iterate)
    {
        if (prepare == null) throw new ArgumentNullException(nameof(prepare));
        if (iterate == null) throw new ArgumentNullException(nameof(iterate));

        lock (Sync)
        {
            if (Entries.Count == int.MaxValue)
            {
                throw new InvalidOperationException("Too many registered constraint dispatch entries.");
            }

            Entries.Add(new Entry((nint)prepare, (nint)iterate));
            return (uint)(Entries.Count - 1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly Entry Get(uint dispatchId)
    {
        return ref CollectionsMarshal.AsSpan(Entries)[(int)dispatchId];
    }
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
    /// <summary>Default softness (compliance) for angular constraints.</summary>
    public const Real DefaultAngularSoftness = (Real)0.001;

    /// <summary>Default bias factor for angular error correction.</summary>
    public const Real DefaultAngularBias = (Real)0.2;

    /// <summary>Default softness for angular limit enforcement.</summary>
    public const Real DefaultAngularLimitSoftness = (Real)0.001;

    /// <summary>Default bias factor for angular limit correction.</summary>
    public const Real DefaultAngularLimitBias = (Real)0.1;

    /// <summary>Default softness (compliance) for linear constraints.</summary>
    public const Real DefaultLinearSoftness = (Real)0.00001;

    /// <summary>Default bias factor for linear error correction.</summary>
    public const Real DefaultLinearBias = (Real)0.2;

    /// <summary>Default softness for linear limit enforcement.</summary>
    public const Real DefaultLinearLimitSoftness = (Real)0.0001;

    /// <summary>Default bias factor for linear limit correction.</summary>
    public const Real DefaultLinearLimitBias = (Real)0.2;


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
    /// Sets the solver dispatch id used by this instance. Override this in derived classes
    /// to assign the correct registered solver pair. The id is later written into the
    /// unmanaged header when the constraint is enabled.
    /// </summary>
    protected virtual void Create()
    {
    }

    /// <summary>
    /// Resets the cached warm-start state used by the solver for this constraint.
    /// </summary>
    /// <remarks>
    /// This clears only persistent solver impulses. Constraint configuration remains unchanged.
    /// Useful after restoring snapshots or other discontinuous state changes where preserving
    /// warm-starting is undesirable.
    /// </remarks>
    public virtual void ResetWarmStart()
    {
    }

    protected uint DispatchId { get; set; }

    protected static unsafe uint RegisterFullConstraint(
        delegate*<ref ConstraintData, Real, void> prepare,
        delegate*<ref ConstraintData, Real, void> iterate)
    {
        return ConstraintDispatchTable.Register(prepare, iterate);
    }

    protected static unsafe uint RegisterSmallConstraint(
        delegate*<ref SmallConstraintData, Real, void> prepare,
        delegate*<ref SmallConstraintData, Real, void> iterate)
    {
        return ConstraintDispatchTable.Register(prepare, iterate);
    }

    /// <summary>
    /// Gets or sets whether this constraint is enabled.
    /// </summary>
    /// <remarks>
    /// Use <see cref="World.Remove(Constraint)"/> for permanent removal.
    /// </remarks>
    public unsafe bool IsEnabled
    {
        get => Handle.Data.DispatchId != 0;
        set
        {
            if (value && DispatchId == 0)
            {
                throw new InvalidOperationException(
                    $"The constraint has no registered solver dispatch. " +
                    $"Set {nameof(DispatchId)} in {nameof(Create)}().");
            }

            Handle.Data.DispatchId = value ? DispatchId : 0u;
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
