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

using System.Runtime.CompilerServices;
using Jitter2.LinearMath;
using Jitter2.UnmanagedMemory;

[assembly: InternalsVisibleTo("JitterTests")]

namespace Jitter2.Dynamics.Constraints;

internal unsafe struct QMatrix
{
    public static JMatrix ProjectMultiplyLeftRight(in JQuaternion left, in JQuaternion right)
    {
        Unsafe.SkipInit(out JMatrix res);

        res.M11 = -left.X * right.X + left.W * right.W + left.Z * right.Z + left.Y * right.Y;
        res.M12 = -left.X * right.Y + left.W * right.Z - left.Z * right.W - left.Y * right.X;
        res.M13 = -left.X * right.Z - left.W * right.Y - left.Z * right.X + left.Y * right.W;
        res.M21 = -left.Y * right.X + left.Z * right.W - left.W * right.Z - left.X * right.Y;
        res.M22 = -left.Y * right.Y + left.Z * right.Z + left.W * right.W + left.X * right.X;
        res.M23 = -left.Y * right.Z - left.Z * right.Y + left.W * right.X - left.X * right.W;
        res.M31 = -left.Z * right.X - left.Y * right.W - left.X * right.Z + left.W * right.Y;
        res.M32 = -left.Z * right.Y - left.Y * right.Z + left.X * right.W - left.W * right.X;
        res.M33 = -left.Z * right.Z + left.Y * right.Y + left.X * right.X + left.W * right.W;

        return res;
    }

}