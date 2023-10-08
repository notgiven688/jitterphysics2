#pragma warning disable CS8602

using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;

namespace JitterDemo;

public class SoftBodySphere : SoftBodyCloth
{
    public float Pressure { get; set; } = 400.0f;

    private class UnitSphere : ISupportMap
    {
        public UnitSphere(JVector center)
        {
            GeometricCenter = center;
        }

        public void SupportMap(in JVector direction, out JVector result)
        {
            result = GeometricCenter + JVector.Normalize(direction);
        }

        public JVector GeometricCenter { get; }
    }

    private static IEnumerable<JTriangle> GenSphereTriangles(JVector offset)
    {
        return ShapeHelper.MakeHull(new UnitSphere(offset));
    }
    
    private static IEnumerable<JTriangle> GenSphereTrianglesFromMesh(JVector offset, string filename)
    {
        Mesh m = Mesh.LoadMesh(filename);
        foreach (var tri in m.Indices)
        {
            yield return new JTriangle(Conversion.ToJitterVector(m.Vertices[tri.T1].Position) + offset,
                Conversion.ToJitterVector(m.Vertices[tri.T2].Position) + offset,
                Conversion.ToJitterVector(m.Vertices[tri.T3].Position) + offset
            );
        }
    }

    public SoftBodySphere(World world, JVector offset) : base(world, GenSphereTriangles(offset))
    {
        foreach (var point in Points)
        {
            point.SetMassInertia(JMatrix.Identity * 1000, 0.01f);
            point.Damping = (1, 0.99f);
        }

        foreach (var spring in Springs)
        {
            (spring as SpringConstraint).Softness = 0.5f;
        }
    }

    protected override void WorldOnPostStep(float dt)
    {
        base.WorldOnPostStep(dt);

        if (!IsActive) return;

        float volume = 0.0f;

        foreach (SoftBodyTriangle sbt in Shapes)
        {
            JVector v1 = sbt.Body1.Position;
            JVector v2 = sbt.Body2.Position;
            JVector v3 = sbt.Body3.Position;

            volume += ((v2.Y - v1.Y) * (v3.Z - v1.Z) -
                       (v2.Z - v1.Z) * (v3.Y - v1.Y)) * (v1.X + v2.X + v3.X);
        }

        float invVol = 1.0f / MathF.Max(0.1f, volume);

        foreach (SoftBodyTriangle sbt in Shapes)
        {
            JVector p0 = sbt.Body1.Position;
            JVector p1 = sbt.Body2.Position;
            JVector p2 = sbt.Body3.Position;

            JVector normal = (p1 - p0) % (p2 - p0);
            JVector force = normal * Pressure * invVol;

            sbt.Body1.AddForce(force);
            sbt.Body2.AddForce(force);
            sbt.Body3.AddForce(force);
        }
    }
}