// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace FixedMath.Numerics
{
    /// <summary>
    /// Contains various methods useful for creating, manipulating, combining, and converting generic vectors with one another.
    /// </summary>
    public static partial class Vector
    {
        // Every operation must either be a JIT intrinsic or implemented over a JIT intrinsic
        // as a thin wrapper
        // Operations implemented over a JIT intrinsic should be inlined
        // Methods that do not have a <T> type parameter are recognized as intrinsics
        /// <summary>
        /// Returns whether or not vector operations are subject to hardware acceleration through JIT intrinsic support.
        /// </summary>
        public static bool IsHardwareAccelerated
        {
            [Intrinsic]
            get
            {
                return false;
            }
        }
    }
}
