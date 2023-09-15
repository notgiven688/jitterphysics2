using System;
using System.Collections.Generic;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

public class Cylinder : CSMInstance
{
    public const int Tesselation = 20;

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

            vertices.Add(new Vertex(vertex - 0.5f * Vector3.UnitY, -Vector3.UnitY));
            vertices.Add(new Vertex(vertex + 0.5f * Vector3.UnitY, +Vector3.UnitY));

            vertices.Add(new Vertex(vertex - 0.5f * Vector3.UnitY, vertex));
            vertices.Add(new Vertex(vertex + 0.5f * Vector3.UnitY, vertex));
        }

        int t4 = 4 * Tesselation;

        for (int i = 0; i < Tesselation; i++)
        {
            indices.Add(new TriangleVertexIndex(2 + 4 * i, 0, 2 + (4 * i + 4) % t4));
            indices.Add(new TriangleVertexIndex(1, 3 + 4 * i, 3 + (4 * i + 4) % t4));
            indices.Add(new TriangleVertexIndex(5 + 4 * i, 4 + 4 * i, 4 + (4 * i + 4) % t4));
            indices.Add(new TriangleVertexIndex(5 + (4 * i + 4) % t4, 5 + 4 * i, 4 + (4 * i + 4) % t4));
        }

        return (vertices.ToArray(), indices.ToArray());
    }

    public override void LightPass(PhongShader shader)
    {
        shader.MaterialProperties.SetDefaultMaterial();

        base.LightPass(shader);
    }
}