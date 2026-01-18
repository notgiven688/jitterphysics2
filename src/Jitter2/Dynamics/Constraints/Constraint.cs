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
[StructLayout(LayoutKind.Sequential, Size = Precision.ConstraintSizeSmall)]
public unsafe struct SmallConstraintData
{
    internal int _internal;
    public delegate*<ref SmallConstraintData, Real, void> Iterate;
    public delegate*<ref SmallConstraintData, Real, void> PrepareForIteration;

    public JHandle<RigidBodyData> Body1;
    public JHandle<RigidBodyData> Body2;
}

/// <summary>
/// Low-level data for constraints, stored in unmanaged memory.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = Precision.ConstraintSizeFull)]
public unsafe struct ConstraintData
{
    internal int _internal;
    public delegate*<ref ConstraintData, Real, void> Iterate;
    public delegate*<ref ConstraintData, Real, void> PrepareForIteration;

    public JHandle<RigidBodyData> Body1;
    public JHandle<RigidBodyData> Body2;
}

/// <summary>
/// The base class for constraints.
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
    /// Helper to check if the constraint data is small enough.
    /// </summary>
    protected static unsafe void CheckDataSize<T>() where T : unmanaged
    {
        if (sizeof(T) > sizeof(ConstraintData))
        {
            throw new InvalidOperationException("The size of the constraint data is too large.");
        }
    }

    /// <summary>
    /// This method must be overridden. It initializes the function pointers for
    /// <see cref="ConstraintData.Iterate"/> and <see cref="ConstraintData.PrepareForIteration"/>.
    /// </summary>
    protected virtual void Create()
    {
    }

    protected unsafe delegate*<ref ConstraintData, Real, void> Iterate = null;
    protected unsafe delegate*<ref ConstraintData, Real, void> PrepareForIteration = null;

    /// <summary>
    /// Enables or disables this constraint temporarily. For a complete removal of the constraint,
    /// use <see cref="World.Remove(Constraint)"/>.
    /// </summary>
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

    public virtual void DebugDraw(IDebugDrawer drawer)
    {
        throw new NotImplementedException();
    }
}