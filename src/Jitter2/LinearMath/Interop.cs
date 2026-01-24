/*
 * Jitter2 Physics Library
 * (c) Thorben Linneweber and contributors
 * SPDX-License-Identifier: MIT
 */

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jitter2.LinearMath;

/// <summary>
/// Interop helpers for JVector and JQuaternion.
/// Includes safe implicit conversions to System.Numerics types, and opt-in unsafe
/// bit reinterpretation for high-performance interop with layout-compatible structs.
/// </summary>
internal static class UnsafeInterop
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowSizeMismatch<TTo, TFrom>()
        => throw new InvalidOperationException(
            $"UnsafeAs size mismatch: sizeof({typeof(TFrom).Name})={Unsafe.SizeOf<TFrom>()}, " +
            $"sizeof({typeof(TTo).Name})={Unsafe.SizeOf<TTo>()}.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowSizeMismatch(string from, int fromSize, string to, int toSize)
        => throw new InvalidOperationException(
            $"UnsafeAs size mismatch: sizeof({from})={fromSize}, sizeof({to})={toSize}.");
}

public partial struct JVector
{
    /// <summary>
    /// Reinterprets the bits of this <see cref="JVector"/> as <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Valid only if <typeparamref name="T"/> has identical size and compatible layout.
    /// </remarks>
    /// <typeparam name="T">The target unmanaged type.</typeparam>
    /// <returns>The reinterpreted value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T UnsafeAs<T>() where T : unmanaged
    {
        if (Unsafe.SizeOf<T>() != Unsafe.SizeOf<JVector>())
        {
            UnsafeInterop.ThrowSizeMismatch<T, JVector>();
        }

        return Unsafe.As<JVector, T>(ref this);
    }

    /// <summary>
    /// Reinterprets the bits of <paramref name="value"/> as a <see cref="JVector"/>.
    /// </summary>
    /// <remarks>
    /// Valid only if <typeparamref name="T"/> has identical size and compatible layout.
    /// </remarks>
    /// <typeparam name="T">The source unmanaged type.</typeparam>
    /// <param name="value">The value to reinterpret.</param>
    /// <returns>The reinterpreted vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JVector UnsafeFrom<T>(in T value) where T : unmanaged
    {
        if (Unsafe.SizeOf<T>() != Unsafe.SizeOf<JVector>())
        {
            UnsafeInterop.ThrowSizeMismatch<JVector, T>();
        }

        return Unsafe.As<T, JVector>(ref Unsafe.AsRef(in value));
    }

    public static implicit operator JVector((Real x, Real y, Real z) tuple) => new(tuple.x, tuple.y, tuple.z);

    public static implicit operator Vector3(in JVector v) => new((float)v.X, (float)v.Y, (float)v.Z);

    public static implicit operator JVector(in Vector3 v) => new(v.X, v.Y, v.Z);
}

public partial struct JQuaternion
{
    /// <summary>
    /// Reinterprets the bits of this <see cref="JQuaternion"/> as <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Valid only if <typeparamref name="T"/> has identical size and compatible layout.
    /// Memory order is X, Y, Z, W (W is last).
    /// </remarks>
    /// <typeparam name="T">The target unmanaged type.</typeparam>
    /// <returns>The reinterpreted value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T UnsafeAs<T>() where T : unmanaged
    {
        if (Unsafe.SizeOf<T>() != Unsafe.SizeOf<JQuaternion>())
        {
            UnsafeInterop.ThrowSizeMismatch<T, JQuaternion>();
        }

        return Unsafe.As<JQuaternion, T>(ref this);
    }

    /// <summary>
    /// Reinterprets the bits of <paramref name="value"/> as a <see cref="JQuaternion"/>.
    /// </summary>
    /// <remarks>
    /// Valid only if <typeparamref name="T"/> has identical size and compatible layout.
    /// Expects memory order X, Y, Z, W (W is last).
    /// </remarks>
    /// <typeparam name="T">The source unmanaged type.</typeparam>
    /// <param name="value">The value to reinterpret.</param>
    /// <returns>The reinterpreted quaternion.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static JQuaternion UnsafeFrom<T>(in T value) where T : unmanaged
    {
        if (Unsafe.SizeOf<T>() != Unsafe.SizeOf<JQuaternion>())
        {
            UnsafeInterop.ThrowSizeMismatch<JQuaternion, T>();
        }

        return Unsafe.As<T, JQuaternion>(ref Unsafe.AsRef(in value));
    }

    public static implicit operator JQuaternion((Real x, Real y, Real z, Real w) tuple) => new(tuple.x, tuple.y, tuple.z, tuple.w);

    public static implicit operator Quaternion(in JQuaternion q) => new((float)q.X, (float)q.Y, (float)q.Z, (float)q.W);

    public static implicit operator JQuaternion(in Quaternion q) => new(q.X, q.Y, q.Z, q.W);
}

#if DEBUG

public static class UnsafeBase64Serializer<T> where T : unmanaged
{
    public static string Serialize(in T value)
    {
        int size = Unsafe.SizeOf<T>();
        byte[] buffer = new byte[size];

        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                *(T*)ptr = value;
            }
        }

        return Convert.ToBase64String(buffer);
    }

    public static T Deserialize(string base64)
    {
        byte[] buffer = Convert.FromBase64String(base64);

        if (buffer.Length != Unsafe.SizeOf<T>())
        {
            UnsafeInterop.ThrowSizeMismatch("Base64Data", buffer.Length, typeof(T).Name, Unsafe.SizeOf<T>());
        }

        unsafe
        {
            fixed (byte* ptr = buffer)
            {
                return *(T*)ptr;
            }
        }
    }
}

#endif