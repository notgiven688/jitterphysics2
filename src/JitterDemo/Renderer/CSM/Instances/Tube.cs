using System;
using System.Collections.Generic;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class Tube : CSMInstance
{
    public const int Tesselation = 30;

    public override (Vertex[] vertices, TriangleVertexIndex[] indices) ProvideVertices()
    {
        List<Vertex> vertices = new();
        List<TriangleVertexIndex> indices = new();

        vertices.Add(new Vertex(-0.5f * Vector3.UnitY, -Vector3.UnitY));
        vertices.Add(new Vertex(+0.5f * Vector3.UnitY, +Vector3.UnitY));

        for (int i = 0; i < Tesselation; i++)
        {
            double alpha = 2.0d * Math.PI / Tesselation * i;

            Vector3 vertex = new()
            {
                X = (float)Math.Cos(alpha),
                Z = (float)Math.Sin(alpha)
            };

            vertices.Add(new Vertex(0.5f * (vertex - Vector3.UnitY), vertex));
            vertices.Add(new Vertex(0.5f * (vertex + Vector3.UnitY), vertex));
        }

        int t2 = 2 * Tesselation;

        for (int i = 0; i < Tesselation; i++)
        {
            indices.Add(new TriangleVertexIndex(3 + 2 * i, 2 + 2 * i, 2 + (2 * i + 2) % t2));
            indices.Add(new TriangleVertexIndex(3 + (2 * i + 2) % t2, 3 + 2 * i, 2 + (2 * i + 2) % t2));
        }

        return (vertices.ToArray(), indices.ToArray());
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();

        base.LightPass(shader);
    }
}