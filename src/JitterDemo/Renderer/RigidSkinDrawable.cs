using System;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer;

/// <summary>
/// A mesh rigidly skinned to a set of bones: every vertex follows exactly one bone.
/// The bind mesh is given in bone-local coordinates and replicated for a fixed number of
/// skins (characters). Each frame, call <see cref="SetPose"/> for every skin and then
/// <see cref="Commit"/>; the vertices are posed on the CPU and streamed like a
/// <see cref="MutableMeshDrawable"/>, so the regular lit and shadow passes draw them.
/// </summary>
public class RigidSkinDrawable : InstancedDrawable
{
    private Vertex[] bindVertices = Array.Empty<Vertex>();
    private int[] vertexBone = Array.Empty<int>();

    public int BoneCount { get; private set; }
    public int SkinCount { get; private set; }

    public RigidSkinDrawable() : base(new Mesh(new Vertex[4], new TriangleVertexIndex[0]))
    {
    }

    /// Sets the bind mesh (bone-local vertices), the bone of every vertex and the number of skins.
    public void Configure(Mesh bindMesh, int[] vertexBone, int boneCount, int skinCount)
    {
        if (vertexBone.Length != bindMesh.Vertices.Length)
            throw new ArgumentException("Every vertex needs a bone.", nameof(vertexBone));

        bindVertices = bindMesh.Vertices;
        this.vertexBone = vertexBone;
        BoneCount = boneCount;
        SkinCount = skinCount;

        int vertexCount = bindVertices.Length;
        var triangles = bindMesh.Indices;
        var indices = new TriangleVertexIndex[triangles.Length * skinCount];
        var vertices = new Vertex[vertexCount * skinCount];

        for (int skin = 0; skin < skinCount; skin++)
        {
            uint offset = (uint)(skin * vertexCount);
            int baseIndex = skin * triangles.Length;

            for (int i = 0; i < triangles.Length; i++)
            {
                indices[baseIndex + i] = new TriangleVertexIndex(triangles[i].T1 + offset,
                    triangles[i].T2 + offset, triangles[i].T3 + offset);
            }

            Array.Copy(bindVertices, 0, vertices, skin * vertexCount, vertexCount);
        }

        Mesh.Vertices = vertices;
        Mesh.Indices = indices;

        // Element-buffer binding is VAO state, see InstancedDrawable.CreateBuffers.
        Vao.Bind();
        Ibo.Upload<TriangleVertexIndex>(indices);
        Vbo.Upload<Vertex>(vertices, BufferUsage.DynamicDraw);
    }

    /// Poses one skin. The bone matrices must be rigid (rotation and translation only).
    public void SetPose(int skin, ReadOnlySpan<Matrix4> bones)
    {
        if (bones.Length < BoneCount) throw new ArgumentException("Too few bones.", nameof(bones));

        var verts = Mesh.Vertices;
        int offset = skin * bindVertices.Length;

        for (int i = 0; i < bindVertices.Length; i++)
        {
            ref readonly Matrix4 m = ref bones[vertexBone[i]];
            ref readonly Vertex src = ref bindVertices[i];
            ref Vertex dst = ref verts[offset + i];

            Vector3 p = src.Position;
            Vector3 n = src.Normal;

            dst.Position = new Vector3(
                m.M11 * p.X + m.M12 * p.Y + m.M13 * p.Z + m.M14,
                m.M21 * p.X + m.M22 * p.Y + m.M23 * p.Z + m.M24,
                m.M31 * p.X + m.M32 * p.Y + m.M33 * p.Z + m.M34);

            dst.Normal = new Vector3(
                m.M11 * n.X + m.M12 * n.Y + m.M13 * n.Z,
                m.M21 * n.X + m.M22 * n.Y + m.M23 * n.Z,
                m.M31 * n.X + m.M32 * n.Y + m.M33 * n.Z);

            dst.Texture = src.Texture;
        }
    }

    /// Uploads the posed vertices and queues the mesh for this frame's draw passes.
    public void Commit(in Vector3 color)
    {
        Vbo.Stream<Vertex>(Mesh.Vertices, Mesh.Vertices.Length);
        Push(Matrix4.Identity, color);
    }
}
