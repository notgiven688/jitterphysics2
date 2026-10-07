using System;
using System.Collections.Generic;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

/*
 * Realtime Fracture Demo
 * Code generated and optimized with OpenAI Codex
 *
 * Features:
 * - Runtime Voronoi fracture for convex bodies
 * - Recursive fracture for large enough fragments
 * - PointCloudShape fragments with center-of-mass shifted local frames
 * - Dynamic mesh rendering for fractured pieces
 */

public sealed class FractureFragment(RigidBody body, JVector[] localVertices, Vector3 color)
{
    public RigidBody Body { get; } = body;
    public JVector[] LocalVertices { get; } = localVertices;
    public Vector3 Color { get; } = color;
    public int VertexOffset { get; set; }
}

public sealed class FractureFragmentsDrawable : MutableMeshDrawable
{
    private static readonly Vector3 InactiveTintBoost = new(0.2f, 0.2f, 0.2f);

    private readonly List<FractureFragment> fragments = new();

    public FractureFragmentsDrawable()
    {
        Material = new Material
        {
            Tint = Vector3.Zero,
            Specular = new Vector3(0.14f, 0.14f, 0.14f),
            Shininess = 96f,
            Alpha = 1f,
            VertexColorWeight = 0f,
            TextureWeight = 0f
        };
    }

    public void SetFragments(IReadOnlyList<FractureFragment> source)
    {
        fragments.Clear();
        fragments.AddRange(source);

        int triangleCount = 0;
        for (int i = 0; i < fragments.Count; i++)
        {
            fragments[i].VertexOffset = triangleCount * 3;
            triangleCount += fragments[i].LocalVertices.Length / 3;
        }

        TriangleVertexIndex[] triangles = new TriangleVertexIndex[triangleCount];
        MeshGroup[] meshGroups = new MeshGroup[fragments.Count];
        MaterialSlot[] materialSlots = new MaterialSlot[fragments.Count];

        int triangleIndex = 0;
        for (int i = 0; i < fragments.Count; i++)
        {
            FractureFragment fragment = fragments[i];
            int from = triangleIndex;
            int localTriangleCount = fragment.LocalVertices.Length / 3;

            for (int j = 0; j < localTriangleCount; j++)
            {
                uint vertex = (uint)((from + j) * 3);
                triangles[triangleIndex++] = new TriangleVertexIndex(vertex + 0, vertex + 1, vertex + 2);
            }

            meshGroups[i] = new MeshGroup
            {
                Name = $"fragment_{i}",
                FromInclusive = from,
                ToExclusive = triangleIndex
            };

            Material material = Material.Default;
            material.VertexColorWeight = 0f;
            material.Tint = fragment.Color;
            material.Specular = new Vector3(0.12f, 0.12f, 0.12f);
            material.Shininess = 80f;
            materialSlots[i] = new MaterialSlot(i, material);
        }

        SetTriangles(triangles);
        Mesh.Groups = meshGroups;
        Groups = materialSlots;
    }

    public void PushFragments()
    {
        if (fragments.Count == 0) return;

        Vertex[] vertices = Mesh.Vertices;

        for (int i = 0; i < fragments.Count; i++)
        {
            FractureFragment fragment = fragments[i];
            if (fragment.Body.Handle.IsZero) continue;

            ref RigidBodyData data = ref fragment.Body.Data;
            UpdateFragmentTint(i, data.IsActive ? fragment.Color : fragment.Color + InactiveTintBoost);

            int offset = fragment.VertexOffset;

            for (int j = 0; j < fragment.LocalVertices.Length; j++)
            {
                JVector world = JVector.Transform(fragment.LocalVertices[j], data.Orientation) + data.Position;
                vertices[offset + j].Position = Conversion.FromJitter(world);
            }
        }

        RefreshGeometry();
        Push(Matrix4.Identity, Vector3.Zero);
    }

    private void UpdateFragmentTint(int fragmentIndex, Vector3 tint)
    {
        if (Groups == null || fragmentIndex >= Groups.Length) return;

        MaterialSlot slot = Groups[fragmentIndex];
        slot.Material.Tint = tint;
        Groups[fragmentIndex] = slot;
    }
}

public sealed class Demo31 : IDemo, IDrawUpdate, ICleanDemo
{
    private sealed class BreakableBodyTag
    {
    }

    private sealed class Breakable
    {
        public RigidBody Body = null!;
        public JVector Size;
        public JVector BoundsMin;
        public JVector BoundsMax;
        public float Radius;
        public int Seed;
        public int Generation;
        public float TimeSinceCreated;
        public bool Queued;
        public JVector ImpactPoint;
        public JVector ImpactDirection;
        public ConvexPolyhedron Source = null!;
        public bool SphericalSites;
    }

    private sealed class Face
    {
        public Face(IEnumerable<JVector> vertices)
        {
            Vertices.AddRange(vertices);
        }

        public readonly List<JVector> Vertices = new();

        public Face Clone() => new(Vertices);
    }

    private sealed class ConvexPolyhedron
    {
        public readonly List<Face> Faces = new();

        public ConvexPolyhedron Clone()
        {
            ConvexPolyhedron result = new();
            for (int i = 0; i < Faces.Count; i++) result.Faces.Add(Faces[i].Clone());
            return result;
        }
    }

    public string Name => "Realtime Fracture";
    public string Description => "Convex bodies fractured into smaller convex hulls at runtime.";
    public string Controls => "B - Break aimed body";

    private const int FractureSiteCount = 16;
    private const int MaxFractureGeneration = 2;
    private const float BreakSpeed = 8.0f;
    private const float MinPieceMass = 0.015f;
    private const float MinRefractureMass = 0.08f;
    private const float MinRefractureRadius = 0.38f;
    private const float RefractureArmDelay = 0.04f;
    private const float PlaneEpsilon = 1e-5f;
    private const float MergeEpsilonSquared = 1e-8f;

    private World world = null!;
    private FractureFragmentsDrawable fragmentsDrawable = null!;

    private readonly Dictionary<RigidBody, Breakable> breakables = new();
    private readonly List<Breakable> pendingBreaks = new();
    private readonly List<FractureFragment> fragments = new();

    public void Build(Playground pg, World world)
    {
        this.world = world;
        fragmentsDrawable = pg.GetDrawable<FractureFragmentsDrawable>();
        fragmentsDrawable.SetFragments(fragments);

        pg.AddFloor();
        world.SolverIterations = (10, 5);
        world.AllowDeactivation = true;

        BuildBreakableWall(new JVector(0, 0, -12));
        BuildBreakableSphere(new JVector(-4.05f, 10.0f, 0.0f), 0.95f, seed: 4000);
        BuildBreakablePane(new JVector(0.0f, 0.0f, -4.0f), seed: 5000);

        world.PostStep += OnPostStep;
    }

    public void CleanUp()
    {
        if (world != null) world.PostStep -= OnPostStep;

        foreach (Breakable breakable in breakables.Values)
        {
            breakable.Body.BeginCollide -= OnBreakableCollide;
        }

        breakables.Clear();
        pendingBreaks.Clear();
        fragments.Clear();
        fragmentsDrawable?.SetFragments(fragments);
    }

    public void DrawUpdate()
    {
        Keyboard keyboard = Keyboard.Instance;

        if (keyboard.KeyPressBegin(Keyboard.Key.B))
        {
            QueueNearestBreakableToCamera();
        }

        fragmentsDrawable.PushFragments();
    }

    private void BuildBreakableWall(JVector origin)
    {
        JVector blockSize = new(1.7f, 1.0f, 0.9f);
        const int columns = 6;
        const int rows = 5;
        int seed = 1000;

        for (int y = 0; y < rows; y++)
        {
            float rowOffset = (y & 1) == 0 ? 0.0f : blockSize.X * 0.5f;

            for (int x = 0; x < columns; x++)
            {
                float px = (x - (columns - 1) * 0.5f) * blockSize.X + rowOffset;
                float py = blockSize.Y * 0.5f + y * blockSize.Y;

                RegisterBreakableBox(
                    origin + new JVector(px, py, 0.0f),
                    blockSize,
                    seed++,
                    friction: 0.55f,
                    restitution: 0.04f,
                    orientation: JQuaternion.Identity);
            }
        }
    }

    private void BuildBreakablePane(JVector origin, int seed)
    {
        JVector paneSize = new(4.4f, 2.6f, 0.24f);
        RegisterBreakableBox(
            origin + new JVector(0.0f, 7.5f, 0.0f),
            paneSize,
            seed,
            friction: 0.75f,
            restitution: 0.03f,
            orientation: JQuaternion.CreateRotationZ(0.18f) * JQuaternion.CreateRotationX(0.08f));
    }

    private void RegisterBreakableBox(
        JVector position, JVector size, int seed, float friction, float restitution, JQuaternion orientation)
    {
        RigidBody body = world.CreateRigidBody();
        body.Position = position;
        body.Orientation = orientation;
        body.Friction = friction;
        body.Restitution = restitution;
        body.AddShape(new BoxShape(size.X, size.Y, size.Z));
        body.Tag = new BreakableBodyTag();

        Breakable breakable = new()
        {
            Body = body,
            Size = size,
            BoundsMin = size * -0.5f,
            BoundsMax = size * 0.5f,
            Radius = size.Length() * 0.5f,
            Seed = seed,
            Source = CreateBoxPolyhedron(size)
        };

        body.BeginCollide += OnBreakableCollide;
        breakables.Add(body, breakable);
    }

    private void BuildBreakableSphere(JVector position, float radius, int seed)
    {
        RigidBody body = world.CreateRigidBody();
        body.Position = position;
        body.Friction = 0.38f;
        body.Restitution = 0.08f;
        body.AddShape(new SphereShape(radius));
        body.Tag = new BreakableBodyTag();

        Breakable breakable = new()
        {
            Body = body,
            Size = new JVector(radius * 2.0f),
            BoundsMin = new JVector(-radius),
            BoundsMax = new JVector(radius),
            Radius = radius,
            Seed = seed,
            Source = CreateSpherePolyhedron(radius),
            SphericalSites = true
        };

        body.BeginCollide += OnBreakableCollide;
        breakables.Add(body, breakable);
    }

    private void OnBreakableCollide(Arbiter arbiter)
    {
        RigidBody body;
        RigidBody other;
        Breakable? breakable;

        if (breakables.TryGetValue(arbiter.Body1, out breakable))
        {
            body = arbiter.Body1;
            other = arbiter.Body2;
        }
        else if (breakables.TryGetValue(arbiter.Body2, out breakable))
        {
            body = arbiter.Body2;
            other = arbiter.Body1;
        }
        else
        {
            return;
        }

        if (breakable == null) return;
        if (breakable.Generation > 0 && breakable.TimeSinceCreated < RefractureArmDelay) return;

        JVector relativeVelocity = body.Velocity - other.Velocity;
        if (relativeVelocity.LengthSquared() < BreakSpeed * BreakSpeed) return;

        JVector impactPoint = EstimateImpactPoint(arbiter);
        JVector impactDirection = JVector.NormalizeSafe(body.Position - other.Position);
        if (impactDirection.LengthSquared() < 0.5f) impactDirection = JVector.UnitY;

        QueueBreak(breakable, impactPoint, impactDirection);
    }

    private static JVector EstimateImpactPoint(Arbiter arbiter)
    {
        ref ContactData data = ref arbiter.Handle.Data;
        uint mask = data.UsageMask;

        JVector sum = JVector.Zero;
        int count = 0;

        AddContact(ContactData.MaskContact0, in data.Contact0);
        AddContact(ContactData.MaskContact1, in data.Contact1);
        AddContact(ContactData.MaskContact2, in data.Contact2);
        AddContact(ContactData.MaskContact3, in data.Contact3);

        return count == 0 ? (arbiter.Body1.Position + arbiter.Body2.Position) * 0.5f : sum * (1.0f / count);

        void AddContact(uint contactMask, in ContactData.Contact contact)
        {
            if ((mask & contactMask) == 0) return;

            JVector p1 = arbiter.Body1.Position + contact.RelativePosition1;
            JVector p2 = arbiter.Body2.Position + contact.RelativePosition2;
            sum += (p1 + p2) * 0.5f;
            count++;
        }
    }

    private void QueueNearestBreakableToCamera()
    {
        Camera camera = RenderWindow.Instance.Camera;
        JVector cameraPosition = Conversion.ToJitterVector(camera.Position);
        JVector cameraDirection = JVector.NormalizeSafe(Conversion.ToJitterVector(camera.Direction));

        Breakable? best = null;
        float bestScore = float.MaxValue;

        foreach (Breakable breakable in breakables.Values)
        {
            JVector toBody = breakable.Body.Position - cameraPosition;
            float along = toBody * cameraDirection;
            if (along < 0.0f) continue;

            JVector lateral = toBody - cameraDirection * along;
            float score = lateral.LengthSquared() + along * 0.02f;

            if (score < bestScore)
            {
                bestScore = score;
                best = breakable;
            }
        }

        if (best == null) return;

        JVector impactPoint = best.Body.Position - cameraDirection * best.Radius;
        QueueBreak(best, impactPoint, cameraDirection);
    }

    private void QueueBreak(Breakable breakable, JVector impactPoint, JVector impactDirection)
    {
        if (breakable.Queued) return;

        breakable.Queued = true;
        breakable.ImpactPoint = impactPoint;
        breakable.ImpactDirection = impactDirection;
        pendingBreaks.Add(breakable);
    }

    private void OnPostStep(TimeStep timeStep)
    {
        foreach (Breakable breakable in breakables.Values)
        {
            breakable.TimeSinceCreated += timeStep.StepDt;
        }

        if (pendingBreaks.Count == 0) return;

        for (int i = 0; i < pendingBreaks.Count; i++)
        {
            Breakable breakable = pendingBreaks[i];
            if (!breakables.ContainsKey(breakable.Body)) continue;
            Fracture(breakable);
        }

        pendingBreaks.Clear();
        fragmentsDrawable.SetFragments(fragments);
    }

    private void Fracture(Breakable breakable)
    {
        RigidBody source = breakable.Body;

        JVector sourcePosition = source.Position;
        JQuaternion sourceOrientation = source.Orientation;
        JVector sourceVelocity = source.Velocity;
        JVector sourceAngularVelocity = source.AngularVelocity;
        float sourceFriction = source.Friction;
        float sourceRestitution = source.Restitution;

        JVector localImpact = JVector.ConjugatedTransform(breakable.ImpactPoint - sourcePosition, sourceOrientation);
        JVector localDirection = JVector.ConjugatedTransform(breakable.ImpactDirection, sourceOrientation);

        List<JVector> sites = GenerateSites(breakable, localImpact, localDirection, breakable.Seed);
        ConvexPolyhedron original = breakable.Source;
        List<PendingFragment> pending = new();
        int nextGeneration = breakable.Generation + 1;

        for (int i = 0; i < sites.Count; i++)
        {
            ConvexPolyhedron cell = original.Clone();

            for (int j = 0; j < sites.Count; j++)
            {
                if (i == j) continue;

                JVector normal = sites[j] - sites[i];
                JVector midpoint = (sites[i] + sites[j]) * 0.5f;
                float offset = normal * midpoint;

                if (!Clip(cell, normal, offset))
                {
                    cell.Faces.Clear();
                    break;
                }
            }

            List<JTriangle> triangles = Triangulate(cell);
            WeldTriangles(triangles);
            if (triangles.Count < 4) continue;

            try
            {
                JVector[] points = ExtractUniqueVertices(triangles);
                if (points.Length < 4) continue;

                PointCloudShape measureShape = new(points);
                measureShape.CalculateMassInertia(out _, out JVector centerOfMass, out float mass);
                if (mass < MinPieceMass || !IsFinite(centerOfMass)) continue;

                ConvexPolyhedron shiftedSource = ShiftPolyhedron(cell, centerOfMass);
                Bounds bounds = CalculateBounds(shiftedSource);
                JVector[] localVertices = ShiftTriangles(triangles, centerOfMass);
                pending.Add(new PendingFragment(
                    centerOfMass,
                    points,
                    localVertices,
                    shiftedSource,
                    bounds.Min,
                    bounds.Max,
                    bounds.Radius,
                    mass));
            }
            catch (Exception)
            {
                // Degenerate cells can happen when fracture sites nearly coincide.
            }
        }

        if (pending.Count == 0)
        {
            breakable.Queued = false;
            return;
        }

        source.BeginCollide -= OnBreakableCollide;
        breakables.Remove(source);
        fragments.RemoveAll(fragment => fragment.Body == source);
        world.Remove(source);

        Random random = new(breakable.Seed * 17 + 5);

        for (int i = 0; i < pending.Count; i++)
        {
            PendingFragment fragment = pending[i];
            int fragmentSeed = random.Next();
            PointCloudShape shape = new(fragment.Points);
            shape.Shift = -fragment.CenterOfMass;

            JVector worldOffset = JVector.Transform(fragment.CenterOfMass, sourceOrientation);
            RigidBody body = world.CreateRigidBody();
            body.Position = sourcePosition + worldOffset;
            body.Orientation = sourceOrientation;
            body.Velocity = sourceVelocity + sourceAngularVelocity % worldOffset;
            body.AngularVelocity = sourceAngularVelocity + RandomVector(random, 4.0f);
            body.Friction = sourceFriction;
            body.Restitution = sourceRestitution;
            body.Tag = new RigidBodyTag();
            body.AddShape(shape);

            if (CanRefracture(fragment, nextGeneration))
            {
                body.BeginCollide += OnBreakableCollide;
                breakables.Add(body, new Breakable
                {
                    Body = body,
                    Size = fragment.BoundsMax - fragment.BoundsMin,
                    BoundsMin = fragment.BoundsMin,
                    BoundsMax = fragment.BoundsMax,
                    Radius = fragment.Radius,
                    Seed = fragmentSeed,
                    Generation = nextGeneration,
                    Source = fragment.Source
                });
            }

            JVector localBurst = JVector.NormalizeSafe(fragment.CenterOfMass - localImpact);
            if (localBurst.LengthSquared() > 0.5f)
            {
                JVector burst = JVector.Transform(localBurst, sourceOrientation);
                body.ApplyImpulse(burst * (0.08f + 0.02f * fragment.Mass));
            }

            fragments.Add(new FractureFragment(body, fragment.LocalVertices, ColorGenerator.GetColor(body.GetHashCode())));
        }
    }

    private static bool CanRefracture(PendingFragment fragment, int generation)
    {
        return generation <= MaxFractureGeneration &&
               fragment.Mass >= MinRefractureMass &&
               fragment.Radius >= MinRefractureRadius &&
               fragment.Source.Faces.Count >= 4;
    }

    private static List<JVector> GenerateSites(Breakable breakable, JVector impact, JVector direction, int seed)
    {
        return breakable.SphericalSites
            ? GenerateSphereSites(breakable.Radius, impact, direction, seed)
            : GenerateBoxSites(breakable.BoundsMin, breakable.BoundsMax, impact, direction, seed);
    }

    private static List<JVector> GenerateBoxSites(JVector boundsMin, JVector boundsMax, JVector impact, JVector direction, int seed)
    {
        Random random = new(seed);
        JVector center = (boundsMin + boundsMax) * 0.5f;
        JVector half = (boundsMax - boundsMin) * 0.5f;
        JVector size = half * 2.0f;
        JVector innerMin = center - half * 0.92f;
        JVector innerMax = center + half * 0.92f;
        JVector randomMin = center - half * 0.94f;
        JVector randomMax = center + half * 0.94f;
        List<JVector> sites = new(FractureSiteCount);

        sites.Add(ClampToBounds(impact, innerMin, innerMax));

        for (int i = 1; i < FractureSiteCount; i++)
        {
            JVector point;

            if (i < FractureSiteCount * 2 / 3)
            {
                JVector offset = RandomVector(random, 0.35f + 0.75f * NextFloat(random));
                offset += direction * ((NextFloat(random) - 0.5f) * 0.5f);
                point = impact + new JVector(offset.X * size.X, offset.Y * size.Y, offset.Z * size.Z);
            }
            else
            {
                point = new JVector(
                    randomMin.X + NextFloat(random) * (randomMax.X - randomMin.X),
                    randomMin.Y + NextFloat(random) * (randomMax.Y - randomMin.Y),
                    randomMin.Z + NextFloat(random) * (randomMax.Z - randomMin.Z));
            }

            sites.Add(ClampToBounds(point, randomMin, randomMax));
        }

        return sites;
    }

    private static List<JVector> GenerateSphereSites(float radius, JVector impact, JVector direction, int seed)
    {
        Random random = new(seed);
        List<JVector> sites = new(FractureSiteCount);

        sites.Add(ClampToSphere(impact, radius * 0.92f));

        for (int i = 1; i < FractureSiteCount; i++)
        {
            JVector point;

            if (i < FractureSiteCount * 2 / 3)
            {
                JVector offset = RandomVector(random, radius * (0.25f + 0.65f * NextFloat(random)));
                offset += direction * ((NextFloat(random) - 0.5f) * radius * 0.45f);
                point = impact + offset;
            }
            else
            {
                point = RandomPointInSphere(random, radius * 0.9f);
            }

            sites.Add(ClampToSphere(point, radius * 0.94f));
        }

        return sites;
    }

    private static ConvexPolyhedron CreateBoxPolyhedron(JVector size)
    {
        JVector h = size * 0.5f;

        JVector nnn = new(-h.X, -h.Y, -h.Z);
        JVector nnp = new(-h.X, -h.Y, +h.Z);
        JVector npn = new(-h.X, +h.Y, -h.Z);
        JVector npp = new(-h.X, +h.Y, +h.Z);
        JVector pnn = new(+h.X, -h.Y, -h.Z);
        JVector pnp = new(+h.X, -h.Y, +h.Z);
        JVector ppn = new(+h.X, +h.Y, -h.Z);
        JVector ppp = new(+h.X, +h.Y, +h.Z);

        ConvexPolyhedron poly = new();
        poly.Faces.Add(new Face(new[] { pnn, ppn, ppp, pnp }));
        poly.Faces.Add(new Face(new[] { nnn, nnp, npp, npn }));
        poly.Faces.Add(new Face(new[] { npn, npp, ppp, ppn }));
        poly.Faces.Add(new Face(new[] { nnn, pnn, pnp, nnp }));
        poly.Faces.Add(new Face(new[] { nnp, pnp, ppp, npp }));
        poly.Faces.Add(new Face(new[] { nnn, npn, ppn, pnn }));
        return poly;
    }

    private static ConvexPolyhedron CreateSpherePolyhedron(float radius)
    {
        const int latitudeBands = 8;
        const int longitudeBands = 16;

        JVector top = new(0.0f, radius, 0.0f);
        JVector bottom = new(0.0f, -radius, 0.0f);
        JVector[,] rings = new JVector[latitudeBands - 1, longitudeBands];

        for (int latitude = 1; latitude < latitudeBands; latitude++)
        {
            float theta = MathF.PI * latitude / latitudeBands;
            float y = MathF.Cos(theta) * radius;
            float ringRadius = MathF.Sin(theta) * radius;

            for (int longitude = 0; longitude < longitudeBands; longitude++)
            {
                float phi = 2.0f * MathF.PI * longitude / longitudeBands;
                rings[latitude - 1, longitude] = new JVector(
                    MathF.Cos(phi) * ringRadius,
                    y,
                    MathF.Sin(phi) * ringRadius);
            }
        }

        ConvexPolyhedron poly = new();

        for (int longitude = 0; longitude < longitudeBands; longitude++)
        {
            int next = (longitude + 1) % longitudeBands;
            poly.Faces.Add(new Face(new[] { top, rings[0, next], rings[0, longitude] }));
        }

        for (int latitude = 0; latitude < latitudeBands - 2; latitude++)
        {
            for (int longitude = 0; longitude < longitudeBands; longitude++)
            {
                int next = (longitude + 1) % longitudeBands;

                JVector a = rings[latitude, longitude];
                JVector b = rings[latitude, next];
                JVector c = rings[latitude + 1, next];
                JVector d = rings[latitude + 1, longitude];

                poly.Faces.Add(new Face(new[] { a, b, c }));
                poly.Faces.Add(new Face(new[] { a, c, d }));
            }
        }

        int last = latitudeBands - 2;
        for (int longitude = 0; longitude < longitudeBands; longitude++)
        {
            int next = (longitude + 1) % longitudeBands;
            poly.Faces.Add(new Face(new[] { bottom, rings[last, longitude], rings[last, next] }));
        }

        return poly;
    }


    private static bool Clip(ConvexPolyhedron poly, JVector normal, float offset)
    {
        List<JVector> capVertices = new();

        for (int i = poly.Faces.Count - 1; i >= 0; i--)
        {
            Face face = poly.Faces[i];
            List<JVector> clipped = ClipFace(face.Vertices, normal, offset, capVertices);

            if (clipped.Count < 3)
            {
                poly.Faces.RemoveAt(i);
            }
            else
            {
                face.Vertices.Clear();
                face.Vertices.AddRange(clipped);
            }
        }

        RemoveDuplicatePoints(capVertices);

        if (capVertices.Count >= 3)
        {
            Face cap = CreateCapFace(capVertices, normal);
            if (cap.Vertices.Count >= 3) poly.Faces.Add(cap);
        }

        return poly.Faces.Count >= 4;
    }

    private static List<JVector> ClipFace(
        List<JVector> vertices, JVector normal, float offset, List<JVector> capVertices)
    {
        List<JVector> result = new(vertices.Count + 1);
        if (vertices.Count == 0) return result;

        JVector previous = vertices[^1];
        float previousDistance = normal * previous - offset;
        bool previousInside = previousDistance <= PlaneEpsilon;

        for (int i = 0; i < vertices.Count; i++)
        {
            JVector current = vertices[i];
            float currentDistance = normal * current - offset;
            bool currentInside = currentDistance <= PlaneEpsilon;

            if (currentInside != previousInside)
            {
                float t = previousDistance / (previousDistance - currentDistance);
                JVector intersection = previous + (current - previous) * t;
                AddUnique(result, intersection);
                AddUnique(capVertices, intersection);
            }

            if (currentInside)
            {
                AddUnique(result, current);
            }

            previous = current;
            previousDistance = currentDistance;
            previousInside = currentInside;
        }

        CleanupPolygon(result);
        return result;
    }

    private static Face CreateCapFace(List<JVector> vertices, JVector normal)
    {
        JVector n = JVector.NormalizeSafe(normal);
        if (n.LengthSquared() < 0.5f) return new Face(Array.Empty<JVector>());

        JVector center = JVector.Zero;
        for (int i = 0; i < vertices.Count; i++) center += vertices[i];
        center *= 1.0f / vertices.Count;

        JVector axis = Math.Abs(n.Y) < 0.8f ? JVector.UnitY : JVector.UnitX;
        JVector u = JVector.NormalizeSafe(axis % n);
        JVector v = n % u;

        vertices.Sort((a, b) =>
        {
            JVector da = a - center;
            JVector db = b - center;
            float aa = MathF.Atan2(da * v, da * u);
            float bb = MathF.Atan2(db * v, db * u);
            return aa.CompareTo(bb);
        });

        CleanupPolygon(vertices);
        return new Face(vertices);
    }

    private static List<JTriangle> Triangulate(ConvexPolyhedron poly)
    {
        List<JTriangle> triangles = new();
        JVector inside = JVector.Zero;
        int vertexCount = 0;

        for (int i = 0; i < poly.Faces.Count; i++)
        {
            List<JVector> vertices = poly.Faces[i].Vertices;

            for (int j = 0; j < vertices.Count; j++)
            {
                inside += vertices[j];
                vertexCount++;
            }
        }

        if (vertexCount == 0) return triangles;
        inside *= 1.0f / vertexCount;

        for (int i = 0; i < poly.Faces.Count; i++)
        {
            List<JVector> vertices = poly.Faces[i].Vertices;
            if (vertices.Count < 3) continue;

            for (int j = 1; j < vertices.Count - 1; j++)
            {
                JVector a = vertices[0];
                JVector b = vertices[j];
                JVector c = vertices[j + 1];

                JVector normal = (b - a) % (c - a);
                if (normal.LengthSquared() < 1e-10f) continue;

                if (normal * (a - inside) < 0.0f)
                {
                    (b, c) = (c, b);
                }

                triangles.Add(new JTriangle
                {
                    V0 = a,
                    V1 = b,
                    V2 = c
                });
            }
        }

        return triangles;
    }

    private static void WeldTriangles(List<JTriangle> triangles)
    {
        List<JVector> vertices = new();
        List<JTriangle> welded = new(triangles.Count);

        for (int i = 0; i < triangles.Count; i++)
        {
            JVector a = GetCanonicalVertex(vertices, triangles[i].V0);
            JVector b = GetCanonicalVertex(vertices, triangles[i].V1);
            JVector c = GetCanonicalVertex(vertices, triangles[i].V2);

            if ((a - b).LengthSquared() < MergeEpsilonSquared ||
                (b - c).LengthSquared() < MergeEpsilonSquared ||
                (c - a).LengthSquared() < MergeEpsilonSquared)
            {
                continue;
            }

            JVector normal = (b - a) % (c - a);
            if (normal.LengthSquared() < 1e-10f) continue;

            welded.Add(new JTriangle
            {
                V0 = a,
                V1 = b,
                V2 = c
            });
        }

        triangles.Clear();
        triangles.AddRange(welded);
    }

    private static JVector[] ExtractUniqueVertices(List<JTriangle> triangles)
    {
        List<JVector> vertices = new();

        for (int i = 0; i < triangles.Count; i++)
        {
            GetCanonicalVertex(vertices, triangles[i].V0);
            GetCanonicalVertex(vertices, triangles[i].V1);
            GetCanonicalVertex(vertices, triangles[i].V2);
        }

        return vertices.ToArray();
    }

    private static JVector GetCanonicalVertex(List<JVector> vertices, JVector vertex)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            if ((vertices[i] - vertex).LengthSquared() < MergeEpsilonSquared)
            {
                return vertices[i];
            }
        }

        vertices.Add(vertex);
        return vertex;
    }

    private static JVector[] ShiftTriangles(List<JTriangle> triangles, JVector centerOfMass)
    {
        JVector[] localVertices = new JVector[triangles.Count * 3];
        int vertex = 0;

        for (int i = 0; i < triangles.Count; i++)
        {
            JTriangle tri = triangles[i];
            localVertices[vertex++] = tri.V0 - centerOfMass;
            localVertices[vertex++] = tri.V1 - centerOfMass;
            localVertices[vertex++] = tri.V2 - centerOfMass;
        }

        return localVertices;
    }

    private static ConvexPolyhedron ShiftPolyhedron(ConvexPolyhedron poly, JVector offset)
    {
        ConvexPolyhedron shifted = new();

        for (int i = 0; i < poly.Faces.Count; i++)
        {
            List<JVector> vertices = poly.Faces[i].Vertices;
            JVector[] shiftedVertices = new JVector[vertices.Count];

            for (int j = 0; j < vertices.Count; j++)
            {
                shiftedVertices[j] = vertices[j] - offset;
            }

            shifted.Faces.Add(new Face(shiftedVertices));
        }

        return shifted;
    }

    private static Bounds CalculateBounds(ConvexPolyhedron poly)
    {
        JVector min = new(float.MaxValue);
        JVector max = new(float.MinValue);
        float radiusSquared = 0.0f;
        bool hasVertex = false;

        for (int i = 0; i < poly.Faces.Count; i++)
        {
            List<JVector> vertices = poly.Faces[i].Vertices;

            for (int j = 0; j < vertices.Count; j++)
            {
                JVector vertex = vertices[j];
                if (!IsFinite(vertex)) continue;

                min = new JVector(
                    Math.Min(min.X, vertex.X),
                    Math.Min(min.Y, vertex.Y),
                    Math.Min(min.Z, vertex.Z));

                max = new JVector(
                    Math.Max(max.X, vertex.X),
                    Math.Max(max.Y, vertex.Y),
                    Math.Max(max.Z, vertex.Z));

                radiusSquared = Math.Max(radiusSquared, vertex.LengthSquared());
                hasVertex = true;
            }
        }

        return hasVertex
            ? new Bounds(min, max, MathF.Sqrt(radiusSquared))
            : new Bounds(JVector.Zero, JVector.Zero, 0.0f);
    }

    private static void CleanupPolygon(List<JVector> vertices)
    {
        for (int i = vertices.Count - 1; i >= 0 && vertices.Count > 1; i--)
        {
            JVector a = vertices[i];
            JVector b = vertices[(i + 1) % vertices.Count];
            if ((a - b).LengthSquared() < MergeEpsilonSquared) vertices.RemoveAt(i);
        }
    }

    private static void RemoveDuplicatePoints(List<JVector> vertices)
    {
        for (int i = vertices.Count - 1; i >= 0; i--)
        {
            for (int j = 0; j < i; j++)
            {
                if ((vertices[i] - vertices[j]).LengthSquared() < MergeEpsilonSquared)
                {
                    vertices.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private static void AddUnique(List<JVector> vertices, JVector point)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            if ((vertices[i] - point).LengthSquared() < MergeEpsilonSquared) return;
        }

        vertices.Add(point);
    }

    private static JVector ClampToBox(JVector point, JVector halfSize)
    {
        return new JVector(
            Math.Clamp(point.X, -halfSize.X, +halfSize.X),
            Math.Clamp(point.Y, -halfSize.Y, +halfSize.Y),
            Math.Clamp(point.Z, -halfSize.Z, +halfSize.Z));
    }

    private static JVector ClampToBounds(JVector point, JVector min, JVector max)
    {
        return new JVector(
            Math.Clamp(point.X, min.X, max.X),
            Math.Clamp(point.Y, min.Y, max.Y),
            Math.Clamp(point.Z, min.Z, max.Z));
    }

    private static JVector ClampToSphere(JVector point, float radius)
    {
        if (radius <= 0.0f) return JVector.Zero;

        float lengthSquared = point.LengthSquared();
        float radiusSquared = radius * radius;
        if (lengthSquared <= radiusSquared || lengthSquared < 1e-10f) return point;

        return point * (radius / MathF.Sqrt(lengthSquared));
    }

    private static JVector RandomPointInSphere(Random random, float radius)
    {
        for (int i = 0; i < 16; i++)
        {
            JVector point = new(
                (NextFloat(random) * 2.0f - 1.0f) * radius,
                (NextFloat(random) * 2.0f - 1.0f) * radius,
                (NextFloat(random) * 2.0f - 1.0f) * radius);

            if (point.LengthSquared() <= radius * radius) return point;
        }

        return JVector.Zero;
    }

    private static JVector RandomVector(Random random, float scale)
    {
        for (int i = 0; i < 8; i++)
        {
            JVector v = new(
                NextFloat(random) * 2.0f - 1.0f,
                NextFloat(random) * 2.0f - 1.0f,
                NextFloat(random) * 2.0f - 1.0f);

            if (v.LengthSquared() > 1e-6f) return JVector.Normalize(v) * (scale * NextFloat(random));
        }

        return JVector.Zero;
    }

    private static bool IsFinite(JVector value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }

    private static float NextFloat(Random random) => (float)random.NextDouble();

    private readonly record struct Bounds(JVector Min, JVector Max, float Radius);

    private readonly record struct PendingFragment(
        JVector CenterOfMass,
        JVector[] Points,
        JVector[] LocalVertices,
        ConvexPolyhedron Source,
        JVector BoundsMin,
        JVector BoundsMax,
        float Radius,
        float Mass);
}
