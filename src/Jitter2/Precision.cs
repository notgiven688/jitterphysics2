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

// Uncomment here to build Jitter using double precision
// --------------------------------------------------------
#define USE_DOUBLE_PRECISION
// --------------------------------------------------------
// Or use command line option, e.g.
// dotnet build -c Release -p:DoublePrecision=true

#if USE_DOUBLE_PRECISION

global using Real = System.Double;
global using MathR = System.Math;
global using Vector = System.Runtime.Intrinsics.Vector256;
global using VectorReal = System.Runtime.Intrinsics.Vector256<System.Double>;

#else

global using Real = System.Single;
global using MathR = System.MathF;
global using Vector = System.Runtime.Intrinsics.Vector128;
global using VectorReal = System.Runtime.Intrinsics.Vector128<System.Single>;

#endif

namespace Jitter2;
public static class Precision
{
    #if USE_DOUBLE_PRECISION
        public const int ConstraintSizeFull = 512;
        public const int ConstraintSizeSmall = 256;
    #else
        public const int ConstraintSizeFull = 256;
        public const int ConstraintSizeSmall = 128;
    #endif

    /// <summary>
    /// Gets a value indicating whether the engine is configured to use double-precision floating-point numbers.
    /// </summary>
    public static bool IsDoublePrecision => sizeof(Real) == sizeof(double);
}