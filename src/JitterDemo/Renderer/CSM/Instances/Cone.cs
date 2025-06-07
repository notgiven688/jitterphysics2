using System;
using System.Collections.Generic;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class Cone : CSMInstance
{
    public const int Tesselation = 20;

    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        static Vector3 GetSpherical(double alpha, double beta)
        {
            return new Vector3((float)(Math.Cos(alpha) * Math.Sin(beta)),
                (float)Math.Sin(beta),
                (float)(Math.Sin(alpha) * Math.Sin(beta)));
        }

        List<Vertex> vertices = new();
        List<TriangleVertexIndex> indices = new();

        float p1o4 = 1.0f / 4.0f;
        float p2o4 = 3.0f / 4.0f;

        vertices.Add(new Vertex(-p1o4 * Vector3.UnitY, -Vector3.UnitY));

        for (int i = 0; i < Tesselation; i++)
        {
            double alpha1 = 2.0d * Math.PI / Tesselation * i;
            double alpha2 = alpha1 + Math.PI / Tesselation;
            float beta = (float)(2.0d / Math.Sqrt(5.0d));

            Vector3 v1 = new()
            {
                X = 0.5f * (float)Math.Cos(alpha1),
                Z = 0.5f * (float)Math.Sin(alpha1)
            };

            Vector3 v2 = GetSpherical(alpha1, beta);
            Vector3 v3 = GetSpherical(alpha2, beta);

            vertices.Add(new Vertex(v1 - p1o4 * Vector3.UnitY, -Vector3.UnitY));
            vertices.Add(new Vertex(v1 - p1o4 * Vector3.UnitY, v2));
            vertices.Add(new Vertex(p2o4 * Vector3.UnitY, v3));
        }

        int t3 = 3 * Tesselation;

        for (int i = 0; i < Tesselation; i++)
        {
            indices.Add(new TriangleVertexIndex(0, 1 + 3 * i, 1 + (3 * i + 3) % t3));
            indices.Add(new TriangleVertexIndex(2 + 3 * i, 3 + 3 * i, 2 + (3 * i + 3) % t3));
        }

        return (vertices.ToArray(), indices.ToArray());
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();

        base.LightPass(shader);
    }
}