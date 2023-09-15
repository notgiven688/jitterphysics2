using System;
using System.Collections.Generic;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class Sphere : CSMInstance
{
    public const int Tesselation = 20;

    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        List<Vertex> vertices = new();
        List<TriangleVertexIndex> indices = new();

        for (int e = 0; e < Tesselation; e++)
        {
            for (int i = 0; i < Tesselation; i++)
            {
                double alpha = 2.0d * Math.PI / Tesselation * i;
                double beta = Math.PI / (Tesselation + 1) * (e + 1);

                Vector3 vertex;
                vertex.X = (float)(Math.Cos(alpha) * Math.Sin(beta));
                vertex.Y = (float)Math.Cos(beta);
                vertex.Z = (float)(Math.Sin(alpha) * Math.Sin(beta));

                vertices.Add(new Vertex(vertex * 0.5f, vertex));
            }
        }

        vertices.Add(new Vertex(Vector3.UnitY * -0.5f, -Vector3.UnitY));
        vertices.Add(new Vertex(Vector3.UnitY * +0.5f, Vector3.UnitY));

        int t1 = Tesselation;

        for (int e = 0; e < Tesselation - 1; e++)
        {
            for (int i = 0; i < Tesselation; i++)
            {
                indices.Add(new TriangleVertexIndex(e * t1 + (i + 1) % t1, e * t1 + i, (e + 1) * t1 + i));
                indices.Add(new TriangleVertexIndex((e + 1) * t1 + (i + 1) % t1, e * t1 + (i + 1) % t1,
                    (e + 1) * t1 + i));
            }
        }

        for (int i = 0; i < Tesselation; i++)
        {
            int e = t1 - 1;
            indices.Add(new TriangleVertexIndex(e * t1 + (i + 1) % t1, e * t1 + i, t1 * t1 + 0));
            e = 0;
            indices.Add(new TriangleVertexIndex(e * t1 + i, e * t1 + (i + 1) % t1, t1 * t1 + 1));
        }

        return (vertices.ToArray(), indices.ToArray());
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();

        base.LightPass(shader);
    }
}