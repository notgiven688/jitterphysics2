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

namespace Jitter2;

public class InvalidCollisionTypeException : Exception
{
    public InvalidCollisionTypeException(Type proxyA, Type proxyB)
        : base($"Don't know how to handle collision between {proxyA} and {proxyB}." +
               $" Register a BroadPhaseFilter to handle and/or filter out these collision types.")
    {
    }
}

/// <summary>
/// Represents an exception thrown when a degenerate triangle is detected.
/// </summary>
public class DegenerateTriangleException : Exception
{
    public DegenerateTriangleException()
    {
    }

    public DegenerateTriangleException(string message) : base(message)
    {
    }

    public DegenerateTriangleException(string message, Exception inner) : base(message, inner)
    {
    }
}