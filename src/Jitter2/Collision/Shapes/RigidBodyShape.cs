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

using Jitter2.Dynamics;
using Jitter2.LinearMath;

namespace Jitter2.Collision.Shapes;

public abstract class RigidBodyShape : Shape
{
    /// <summary>
    /// The instance of <see cref="RigidBody"/> to which this shape is attached.
    /// </summary>
    public RigidBody RigidBody { get; internal set; } = null!;

    public sealed override JVector Velocity => RigidBody?.Velocity ?? JVector.Zero;

    public sealed override void UpdateWorldBoundingBox(float dt = 0.0f)
    {
        JBBox box;

        if (RigidBody == null)
        {
            CalculateBoundingBox(JQuaternion.Identity, JVector.Zero, out box);
            WorldBoundingBox = box;
            return;
        }

        ref var data = ref RigidBody.Data;
        CalculateBoundingBox(data.Orientation, data.Position, out box);
        WorldBoundingBox = box;
        if (RigidBody.EnableSpeculativeContacts) SweptExpandBoundingBox(dt);
    }

    public virtual void CalculateBoundingBox(in JQuaternion orientation, in JVector position, out JBBox box)
    {
        ShapeHelper.CalculateBoundingBox(this, orientation, position, out box);
    }

    /// <summary>
    /// Calculates the mass and inertia of the shape. Can be overridden by child classes to improve
    /// performance or accuracy. The default implementation relies on an approximation of the shape
    /// constructed using the support map function.
    /// </summary>
    [ReferenceFrame(ReferenceFrame.Local)]
    public virtual void CalculateMassInertia(out JMatrix inertia, out JVector com, out float mass)
    {
        ShapeHelper.CalculateMassInertia(this, out inertia, out com, out mass);
    }

    [ReferenceFrame(ReferenceFrame.Local)]
    public virtual bool LocalRayCast(in JVector origin, in JVector direction, out JVector normal, out float lambda)
    {
        return NarrowPhase.RayCast(this, origin, direction, out lambda, out normal);
    }

    [ReferenceFrame(ReferenceFrame.World)]
    public sealed override bool RayCast(in JVector origin, in JVector direction, out JVector normal, out float lambda)
    {
        ref var data = ref RigidBody.Data;

        // rotate the ray into the reference frame of bodyA..
        JVector tdirection = JVector.TransposedTransform(direction, data.Orientation);
        JVector torigin = JVector.TransposedTransform(origin - data.Position, data.Orientation);

        bool result = LocalRayCast(torigin, tdirection, out normal, out lambda);

        // ..rotate back.
        JVector.Transform(normal, data.Orientation, out normal);

        return result;
    }
}