using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

/*
 * Infinite Voxel World Demo
 * Code generated and optimized with Google Gemini
 * * Features:
 * - Infinite procedural terrain using 3D Perlin Noise
 * - Multi-threaded Chunk Generation
 * - Frustum Culling & Face Culling for high performance
 * - Object Pooling to minimize Garbage Collection
 * - Custom Voxel Collision system for Jitter2
 */

// -------------------------------------------------------------------------
// 1. DATA STRUCTURES (Optimized for Memory)
// -------------------------------------------------------------------------

public enum VoxelType : byte
{
    Grass = 0,
    Rock = 1,
    Snow = 2
}

// Lightweight struct for rendering data.
// We use simple ints/bytes to keep memory footprint low in large lists.
public readonly struct VoxelRenderData(int x, int y, int z, VoxelType type)
{
    public readonly int X = x, Y = y, Z = z;
    public readonly VoxelType Type = type;
}

// Key for the Chunk Dictionary (Spatial Hashing)
public readonly struct ChunkKey(int x, int z) : IEquatable<ChunkKey>
{
    public readonly int X = x;
    public readonly int Z = z;

    public bool Equals(ChunkKey other) => X == other.X && Z == other.Z;
    public override bool Equals(object? obj) => obj is ChunkKey other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Z);

    public static bool operator ==(ChunkKey left, ChunkKey right) => left.Equals(right);
    public static bool operator !=(ChunkKey left, ChunkKey right) => !(left == right);
}

// Temporary struct passed to the Physics Engine (NarrowPhase)
public readonly struct CollisionVoxel(JVector position) : ISupportMappable
{
    private const float HalfSize = 0.5f;

    public void SupportMap(in JVector direction, out JVector result)
    {
        result.X = position.X + MathHelper.SignBit(direction.X) * HalfSize;
        result.Y = position.Y + MathHelper.SignBit(direction.Y) * HalfSize;
        result.Z = position.Z + MathHelper.SignBit(direction.Z) * HalfSize;
    }

    public void GetCenter(out JVector point) => point = position;
}

// -------------------------------------------------------------------------
// 2. NOISE GENERATOR (Standard Perlin Implementation)
// -------------------------------------------------------------------------
public static class Noise
{
    private static readonly int[] p = new int[512];
    private static readonly int[] permutation = { 151,160,137,91,90,15,
    131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
    190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
    88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
    77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
    102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
    135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
    5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
    223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
    129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
    251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
    49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
    138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    static Noise()
    {
        for (int i = 0; i < 256; i++) p[256 + i] = p[i] = permutation[i];
    }

    public static float Calc(float x, float y)
    {
        int X = (int)MathF.Floor(x) & 255;
        int Y = (int)MathF.Floor(y) & 255;
        x -= MathF.Floor(x); y -= MathF.Floor(y);
        float u = Fade(x), v = Fade(y);
        int A = p[X] + Y, B = p[X + 1] + Y;
        return Lerp(v, Lerp(u, Grad(p[A], x, y, 0), Grad(p[B], x - 1, y, 0)),
                       Lerp(u, Grad(p[A + 1], x, y - 1, 0), Grad(p[B + 1], x - 1, y - 1, 0)));
    }

    public static float Calc3D(float x, float y, float z)
    {
        int X = (int)MathF.Floor(x) & 255;
        int Y = (int)MathF.Floor(y) & 255;
        int Z = (int)MathF.Floor(z) & 255;
        x -= MathF.Floor(x); y -= MathF.Floor(y); z -= MathF.Floor(z);
        float u = Fade(x), v = Fade(y), w = Fade(z);
        int A = p[X] + Y, AA = p[A] + Z, AB = p[A + 1] + Z;
        int B = p[X + 1] + Y, BA = p[B] + Z, BB = p[B + 1] + Z;

        return Lerp(w, Lerp(v, Lerp(u, Grad(p[AA], x, y, z), Grad(p[BA], x - 1, y, z)),
                               Lerp(u, Grad(p[AB], x, y - 1, z), Grad(p[BB], x - 1, y - 1, z))),
                       Lerp(v, Lerp(u, Grad(p[AA + 1], x, y, z - 1), Grad(p[BA + 1], x - 1, y, z - 1)),
                               Lerp(u, Grad(p[AB + 1], x, y - 1, z - 1), Grad(p[BB + 1], x - 1, y - 1, z - 1))));
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float t, float a, float b) => a + t * (b - a);
    private static float Grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y, v = h < 4 ? y : h == 12 || h == 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}

// -------------------------------------------------------------------------
// 3. WORLD LOGIC (Procedural Generation)
// -------------------------------------------------------------------------
public class VoxelWorld : IDynamicTreeProxy, IRayCastable
{
    // Global minimum height for optimizing loops
    public const int MinHeight = -40;

    public static float GetHeight(int x, int z)
    {
        float h = 0;
        h += Noise.Calc(x * 0.01f, z * 0.01f) * 30.0f; // Mountains
        h += Noise.Calc(x * 0.05f, z * 0.05f) * 5.0f;  // Hills
        return h;
    }

    public static bool IsSolid(int x, int y, int z, float terrainHeight)
    {
        // 1. Bedrock Layer
        if (y < MinHeight) return true;

        // 2. 3D Noise for Caves
        // Returns true if solid, false if air (cave)
        float density = Noise.Calc3D(x * 0.05f, y * 0.08f, z * 0.05f);

        // 3. Ground Logic
        // If below terrain height, solid UNLESS noise creates a cave (density > 0.3)
        if (y < terrainHeight) return density <= 0.3f;

        return false;
    }

    public static bool IsSolid(int x, int y, int z) => IsSolid(x, y, z, GetHeight(x, z));

    // IBroadPhaseProxy implementation
    public JVector Velocity => JVector.Zero;
    public JBoundingBox WorldBoundingBox => new (new JVector(-1e10f), new JVector(1e10f));
    public int NodePtr { get; set; }
    public int SetIndex { get; set; } = -1;

    public bool RayCast(in JVector origin, in JVector direction, out JVector normal, out float lambda)
    { normal = JVector.Zero; lambda = 0; return false; }
}

// -------------------------------------------------------------------------
// 4. COLLISION FILTER (Physics Integration)
// -------------------------------------------------------------------------
public class VoxelCollisionFilter : IBroadPhaseFilter
{
    private readonly World world;
    private readonly VoxelWorld voxelProxy;
    private readonly ulong minIndex;

    private const float NormalThreshold = 0.5f;
    private const float EdgeThreshold = 0.01f;

    public VoxelCollisionFilter(World world, VoxelWorld voxelProxy)
    {
        this.world = world;
        this.voxelProxy = voxelProxy;
        // Reserve a range of IDs for voxel contacts
        (minIndex, _) = World.RequestId(1_000_000);
    }

    public bool Filter(IDynamicTreeProxy shapeA, IDynamicTreeProxy shapeB)
    {
        // Only process collisions involving the VoxelWorld proxy
        if (shapeA != voxelProxy && shapeB != voxelProxy) return true;

        var bodyShape = shapeA == voxelProxy ? shapeB : shapeA;
        if (bodyShape is not RigidBodyShape rbs || !rbs.RigidBody.IsActive) return false;

        // Iterate through all integer coordinates intersecting the body's AABB
        JBoundingBox box = bodyShape.WorldBoundingBox;
        int minX = (int)MathF.Floor(box.Min.X);
        int minY = (int)MathF.Floor(box.Min.Y);
        int minZ = (int)MathF.Floor(box.Min.Z);
        int maxX = (int)MathF.Ceiling(box.Max.X);
        int maxY = (int)MathF.Ceiling(box.Max.Y);
        int maxZ = (int)MathF.Ceiling(box.Max.Z);

        float closeToEdge = 0.5f - EdgeThreshold;

        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    if (VoxelWorld.IsSolid(x, y, z))
                    {
                        // Generate a unique ID based on coordinates
                        ulong voxelId = (ulong)((x * 73856093) ^ (y * 19349663) ^ (z * 83492791));

                        JVector voxelPos = new JVector(x + 0.5f, y + 0.5f, z + 0.5f);
                        CollisionVoxel voxel = new CollisionVoxel(voxelPos);

                        // NarrowPhase: Box vs Voxel
                        bool hit = NarrowPhase.MprEpa(voxel, rbs,
                            rbs.RigidBody!.Orientation, rbs.RigidBody!.Position,
                            out JVector pointA, out JVector pointB, out JVector normal, out float penetration);

                        if (hit)
                        {
                            // "Internal Edge" Smoothing
                            // Discard contacts that push against internal edges of the mesh
                            JVector relPos = pointA - voxelPos;
                            if (relPos.X > closeToEdge && normal.X > NormalThreshold && VoxelWorld.IsSolid(x + 1, y, z)) continue;
                            if (relPos.X < -closeToEdge && normal.X < -NormalThreshold &&  VoxelWorld.IsSolid(x - 1, y, z)) continue;
                            if (relPos.Y > closeToEdge && normal.Y > NormalThreshold && VoxelWorld.IsSolid(x, y + 1, z)) continue;
                            if (relPos.Y < -closeToEdge && normal.Y < -NormalThreshold && VoxelWorld.IsSolid(x, y - 1, z)) continue;
                            if (relPos.Z > closeToEdge && normal.Z > NormalThreshold && VoxelWorld.IsSolid(x, y, z + 1)) continue;
                            if (relPos.Z < -closeToEdge && normal.Z < -NormalThreshold && VoxelWorld.IsSolid(x, y, z - 1)) continue;

                            world.RegisterContact(rbs.ShapeId, minIndex + (voxelId % 1000000),
                                world.NullBody, rbs.RigidBody, pointA, pointB, normal);
                        }
                    }
                }
            }
        }
        return false;
    }
}

// -------------------------------------------------------------------------
// 5. THE DEMO (Chunk Management & Rendering)
// -------------------------------------------------------------------------
public class Demo21 : IDemo, ICleanDemo
{
    public string Name => "Voxel World (Custom Collision)";

    private Playground pg = null!;
    private World world = null!;
    private VoxelWorld voxelProxy = null!;

    // Config
    private const int ChunkSize = 16;
    private const int RenderRadius = 8; // Chunks to render in each direction

    // Cache
    private readonly ConcurrentDictionary<ChunkKey, List<VoxelRenderData>> chunkCache = new();
    private readonly ConcurrentStack<List<VoxelRenderData>> listPool = new();

    private int frameCount = 0;

    private readonly Vector3[] palette =
    [
        new Vector3(0.1f, 0.5f, 0.1f), // Grass
        new Vector3(0.3f, 0.3f, 0.3f), // Rock (Darker)
        new Vector3(0.9f, 0.9f, 0.9f)  // Snow
    ];

    public void Build()
    {
        pg = (Playground)RenderWindow.Instance;
        world = pg.World;
        pg.ResetScene(false);

        voxelProxy = new VoxelWorld();
        world.DynamicTree.AddProxy(voxelProxy, false);
        world.BroadPhaseFilter = new VoxelCollisionFilter(world, voxelProxy);
    }

    public void Draw()
    {
        var cam = RenderWindow.Instance.Camera;
        Vector3 camPos = cam.Position;
        Vector3 camDir = cam.Direction;

        // Current chunk coordinates
        int camChunkX = (int)MathF.Floor(camPos.X / ChunkSize);
        int camChunkZ = (int)MathF.Floor(camPos.Z / ChunkSize);

        // 1. GENERATION PHASE (Parallel)
        // Identify which chunks are missing and generate them on threads
        var chunksNeeded = new List<ChunkKey>();
        for (int x = -RenderRadius; x <= RenderRadius; x++)
        {
            for (int z = -RenderRadius; z <= RenderRadius; z++)
            {
                if (x * x + z * z > RenderRadius * RenderRadius) continue;
                ChunkKey key = new ChunkKey(camChunkX + x, camChunkZ + z);
                if (!chunkCache.ContainsKey(key)) chunksNeeded.Add(key);
            }
        }

        if (chunksNeeded.Count > 0)
        {
            Parallel.ForEach(chunksNeeded, key =>
            {
                chunkCache.TryAdd(key, BuildChunk(key.X, key.Z));
            });
        }

        // 2. RENDER PHASE (Main Thread)
        // Submit geometry to the GPU using Instancing
        var cr = pg.CSMRenderer.GetInstance<Cube>();

        for (int x = -RenderRadius; x <= RenderRadius; x++)
        {
            for (int z = -RenderRadius; z <= RenderRadius; z++)
            {
                if (x * x + z * z > RenderRadius * RenderRadius) continue;

                ChunkKey key = new ChunkKey(camChunkX + x, camChunkZ + z);

                // Frustum Culling (Approximate using Dot Product)
                Vector3 chunkCenter = new Vector3(
                    key.X * ChunkSize + (ChunkSize * 0.5f),
                    camPos.Y, // Ignore Y for horizontal FOV check
                    key.Z * ChunkSize + (ChunkSize * 0.5f)
                );

                if (!IsChunkInView(chunkCenter, camPos, camDir)) continue;

                if (chunkCache.TryGetValue(key, out var voxels))
                {
                    foreach (var v in voxels)
                    {
                        Vector3 pos = new Vector3(v.X + 0.5f, v.Y + 0.5f, v.Z + 0.5f);
                        cr.PushMatrix(MatrixHelper.CreateTranslation(pos), palette[(int)v.Type]);
                    }
                }
            }
        }

        // 3. MAINTENANCE (Cleanup)
        // Every 60 frames, remove far-away chunks to free memory
        frameCount++;
        if (frameCount > 60)
        {
            frameCount = 0;
            CleanupCache(camChunkX, camChunkZ);
        }
    }

    private bool IsChunkInView(Vector3 chunkCenter, Vector3 camPos, Vector3 camDir)
    {
        Vector3 toChunk = chunkCenter - camPos;
        // Always draw chunks very close to camera to avoid clipping issues
        if (toChunk.LengthSquared() < 32 * 32) return true;
        // Simple Cone Check (>60 degrees)
        return Vector3.Dot(camDir, Vector3.Normalize(toChunk)) > 0.5f;
    }

    private List<VoxelRenderData> BuildChunk(int cx, int cz)
    {
        // Object Pooling: Reuse list from pool if available
        if (!listPool.TryPop(out var list)) list = new List<VoxelRenderData>(512);
        else list.Clear();

        int startX = cx * ChunkSize;
        int startZ = cz * ChunkSize;
        int endX = startX + ChunkSize;
        int endZ = startZ + ChunkSize;

        for (int x = startX; x < endX; x++)
        {
            for (int z = startZ; z < endZ; z++)
            {
                float h = VoxelWorld.GetHeight(x, z);

                // Loop Optimization: Scan from just below bedrock up to surface
                int maxY = (int)h + 2;
                int minY = VoxelWorld.MinHeight - 2;

                for (int y = minY; y <= maxY; y++)
                {
                    if (VoxelWorld.IsSolid(x, y, z, h))
                    {
                        // Face Culling: Only draw blocks adjacent to Air
                        bool topAir = !VoxelWorld.IsSolid(x, y + 1, z, h);
                        bool bottomAir = !VoxelWorld.IsSolid(x, y - 1, z, h);
                        bool exposed = topAir || bottomAir;

                        if (!exposed)
                        {
                            if (!VoxelWorld.IsSolid(x + 1, y, z) || !VoxelWorld.IsSolid(x - 1, y, z) ||
                                !VoxelWorld.IsSolid(x, y, z + 1) || !VoxelWorld.IsSolid(x, y, z - 1))
                            {
                                exposed = true;
                            }
                        }

                        if (exposed)
                        {
                            VoxelType type = VoxelType.Rock;

                            if (topAir) // Surface blocks get snow/grass
                            {
                                if (y > 10) type = VoxelType.Snow;
                                else if (y < -5) type = VoxelType.Rock;
                                else type = VoxelType.Grass;
                            }

                            list.Add(new VoxelRenderData(x, y, z, type));
                        }
                    }
                }
            }
        }
        return list;
    }

    private void CleanupCache(int camX, int camZ)
    {
        int cleanupRadius = RenderRadius + 4;
        int distSq = cleanupRadius * cleanupRadius;
        var toRemove = new List<ChunkKey>();

        foreach (var key in chunkCache.Keys)
        {
            int dx = key.X - camX;
            int dz = key.Z - camZ;
            if (dx * dx + dz * dz > distSq) toRemove.Add(key);
        }

        foreach (var key in toRemove)
        {
            // Return old lists to the pool instead of garbage collecting them
            if (chunkCache.TryRemove(key, out var list)) listPool.Push(list);
        }
    }

    public void CleanUp()
    {
        world.DynamicTree.RemoveProxy(voxelProxy);
    }
}