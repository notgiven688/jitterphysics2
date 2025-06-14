#pragma warning disable CS8602

using System;
using System.Collections.Generic;
using System.Linq;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;
using JitterDemo.Renderer;

namespace JitterDemo;

public class SoftBodySphere : SoftBodyCloth
{
    public double Pressure { get; set; } = 400.0d;

    private class UnitSphere : ISupportMappable
    {
        public void SupportMap(in JVector direction, out JVector result)
        {
            result = JVector.Normalize(direction);
        }

        public void GetCenter(out JVector point)
        {
            throw new NotImplementedException();
        }
    }

    private static IEnumerable<JTriangle> GenSphereTriangles(JVector offset)
    {
        return ShapeHelper.MakeHull(new UnitSphere(), 4)
            .Select(t => new JTriangle(t.V0 + offset, t.V1 + offset, t.V2 + offset));
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
        foreach (var rb in Vertices)
        {
            rb.SetMassInertia(JMatrix.Zero, 100.0d, true);
            rb.Damping = (0.001d, 0);
        }

        foreach (var spring in Springs)
        {
            (spring as SpringConstraint).Softness = 0.5d;
        }
    }

    protected override void WorldOnPostStep(double dt)
    {
        base.WorldOnPostStep(dt);

        if (!IsActive) return;

        double volume = 0.0d;

        foreach (SoftBodyTriangle sbt in Shapes)
        {
            JVector v1 = sbt.Vertex1.Position;
            JVector v2 = sbt.Vertex2.Position;
            JVector v3 = sbt.Vertex3.Position;

            volume += ((v2.Y - v1.Y) * (v3.Z - v1.Z) -
                       (v2.Z - v1.Z) * (v3.Y - v1.Y)) * (v1.X + v2.X + v3.X);
        }

        double invVol = 1.0d / Math.Max(0.1d, volume);

        foreach (SoftBodyTriangle sbt in Shapes)
        {
            JVector p0 = sbt.Vertex1.Position;
            JVector p1 = sbt.Vertex2.Position;
            JVector p2 = sbt.Vertex3.Position;

            JVector normal = (p1 - p0) % (p2 - p0);
            JVector force = normal * Pressure * invVol;

            // Limit the maximum force
            const double maxForce = 2.0d;

            double fl2 = force.LengthSquared();

            if (fl2 > maxForce * maxForce)
            {
                force *= 1.0d / Math.Sqrt(fl2) * maxForce;
            }

            sbt.Vertex1.AddForce(force);
            sbt.Vertex2.AddForce(force);
            sbt.Vertex3.AddForce(force);
        }
    }
}