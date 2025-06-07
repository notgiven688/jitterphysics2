using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using Jitter2.SoftBodies;

public static class Conversion
{
	public static Vector3 FromJitter(in JVector vec) => new Vector3(vec.X, vec.Y, vec.Z);
}

public class CubedSoftBody(World world) : SoftBody(world)
{
	public int MaterialIndex { get; set; } = 0;

	public struct BuildVertex(int x, int y, int z)
	{
		public int X = x, Y = y, Z = z;

		public static BuildVertex operator +(BuildVertex a, BuildVertex b)
			=> new BuildVertex(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	}

	public struct BuildTetrahedron(int a, int b, int c, int d)
	{
		public int A = a, B = b, C = c, D = d;
	}

	public class BuildQuad(int a, int b, int c, int d, bool diagLeft = false) : IEquatable<BuildQuad>
	{
		public int A = a, B = b, C = c, D = d;
		public bool DiagLeft = diagLeft;

		public Vector2 UvA;
		public Vector2 UvB;
		public Vector2 UvC;
		public Vector2 UvD;

		public void SetUvCoordinates(BuildVertex[] vertices)
		{
			int minX = Math.Min(Math.Min(Math.Min(vertices[A].X, vertices[B].X), vertices[C].X), vertices[D].X);
			int minY = Math.Min(Math.Min(Math.Min(vertices[A].Y, vertices[B].Y), vertices[C].Y), vertices[D].Y);
			int minZ = Math.Min(Math.Min(Math.Min(vertices[A].Z, vertices[B].Z), vertices[C].Z), vertices[D].Z);

			if (vertices[A].X == vertices[B].X && vertices[A].X == vertices[C].X && vertices[A].X == vertices[D].X)
			{
				UvA = new Vector2(vertices[A].Y - minY, vertices[A].Z - minZ) * 0.5f;
				UvB = new Vector2(vertices[B].Y - minY, vertices[B].Z - minZ) * 0.5f;
				UvC = new Vector2(vertices[C].Y - minY, vertices[C].Z - minZ) * 0.5f;
				UvD = new Vector2(vertices[D].Y - minY, vertices[D].Z - minZ) * 0.5f;
			}
			else if (vertices[A].Y == vertices[B].Y && vertices[A].Y == vertices[C].Y && vertices[A].Y == vertices[D].Y)
			{
				UvA = new Vector2(vertices[A].X - minX, vertices[A].Z - minZ) * 0.5f;
				UvB = new Vector2(vertices[B].X - minX, vertices[B].Z - minZ) * 0.5f;
				UvC = new Vector2(vertices[C].X - minX, vertices[C].Z - minZ) * 0.5f;
				UvD = new Vector2(vertices[D].X - minX, vertices[D].Z - minZ) * 0.5f;
			}
			else if (vertices[A].Z == vertices[B].Z && vertices[A].Z == vertices[C].Z && vertices[A].Z == vertices[D].Z)
			{
				UvA = new Vector2(vertices[A].X - minX, vertices[A].Y - minY) * 0.5f;
				UvB = new Vector2(vertices[B].X - minX, vertices[B].Y - minY) * 0.5f;
				UvC = new Vector2(vertices[C].X - minX, vertices[C].Y - minY) * 0.5f;
				UvD = new Vector2(vertices[D].X - minX, vertices[D].Y - minY) * 0.5f;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public override bool Equals(object obj)
		{
			return obj is BuildQuad other && Equals(other);
		}

		public bool Equals(BuildQuad other)
		{
			bool bA = (A == other.A || A == other.B || A == other.C || A == other.D);
			bool bB = (B == other.A || B == other.B || B == other.C || B == other.D);
			bool bC = (C == other.A || C == other.B || C == other.C || C == other.D);
			bool bD = (D == other.A || D == other.B || D == other.C || D == other.D);
			return bA && bB && bC && bD;
		}

		public override int GetHashCode()
		{
			return (A * B * C * D) + (A + B + C + D);
		}
	}

	private bool isFinished = false;

	private readonly Dictionary<BuildVertex, int> vertexIndices = new();

	private List<BuildTetrahedron> tetrahedra = new();
	private List<BuildVertex> cubeCenters = new();

	public HashSet<BuildQuad> Quads { get; } = new();

	private BuildVertex[] cubeVertices = new[]
	{
		new BuildVertex(+1, -1, +1),
		new BuildVertex(+1, -1, -1),
		new BuildVertex(-1, -1, -1),
		new BuildVertex(-1, -1, +1),
		new BuildVertex(+1, +1, +1),
		new BuildVertex(+1, +1, -1),
		new BuildVertex(-1, +1, -1),
		new BuildVertex(-1, +1, +1)
	};

	private int PushVertex(BuildVertex vertex)
	{
		if (vertexIndices.TryGetValue(vertex, out int result))
			return result;
		result = vertexIndices.Count;
		vertexIndices.Add(vertex, result);
		return result;
	}

	public void AddCube(int x, int y, int z)
	{
		if (isFinished) throw new InvalidOperationException();

		var origin = new BuildVertex(x, y, z);

		cubeCenters.Add(origin);

		Span<int> idx = stackalloc int[8];

		for (int i = 0; i < 8; i++)
		{
			idx[i] = PushVertex(origin + cubeVertices[i]);
		}

		tetrahedra.Add(new BuildTetrahedron(idx[0], idx[1], idx[5], idx[2]));
		tetrahedra.Add(new BuildTetrahedron(idx[5], idx[2], idx[6], idx[7]));
		tetrahedra.Add(new BuildTetrahedron(idx[0], idx[3], idx[2], idx[7]));
		tetrahedra.Add(new BuildTetrahedron(idx[4], idx[0], idx[5], idx[7]));
		tetrahedra.Add(new BuildTetrahedron(idx[0], idx[2], idx[5], idx[7]));

		void PushQuad(BuildQuad bq)
		{
			if (!Quads.Add(bq)) Quads.Remove(bq);
		}

		PushQuad(new BuildQuad(idx[5], idx[4], idx[7], idx[6], true));  // top
		PushQuad(new BuildQuad(idx[2], idx[3], idx[0], idx[1], false)); // bottom
		PushQuad(new BuildQuad(idx[6], idx[7], idx[3], idx[2], true));  // left
		PushQuad(new BuildQuad(idx[1], idx[0], idx[4], idx[5], false)); // right
		PushQuad(new BuildQuad(idx[3], idx[7], idx[4], idx[0], true));  // front
		PushQuad(new BuildQuad(idx[1], idx[5], idx[6], idx[2], false)); // back
	}

	public void Finalize(float scale = 0.25f)
	{
		if (isFinished) throw new InvalidOperationException();

		BuildVertex[] vertices = vertexIndices.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToArray();

		foreach (var quad in Quads)
		{
			quad.SetUvCoordinates(vertices);
		}

		foreach (var vertex in vertices)
		{
			var rb = world.CreateRigidBody();
			rb.SetMassInertia(JMatrix.Zero, 8f, true);
			rb.Position = new JVector(vertex.X, vertex.Y, vertex.Z) * scale;
			rb.Damping = (0.02f, 0.002f);
			this.Vertices.Add(rb);
		}

		foreach (var trh in tetrahedra)
		{
			SoftBodyTetrahedron sbt = new(this,
				Vertices[trh.A], Vertices[trh.B],
				Vertices[trh.C], Vertices[trh.D]);

			world.DynamicTree.AddProxy(sbt);
			this.Shapes.Add(sbt);
		}

		for (int i = 0; i < cubeCenters.Count; i++)
		{
			var centerCube = world.CreateRigidBody();

			centerCube.Position = new JVector(cubeCenters[i].X, cubeCenters[i].Y, cubeCenters[i].Z) * scale;
			centerCube.SetMassInertia(JMatrix.Identity * 1f, 1f);

			List<BallSocket> ct = new(8);

			ct.Add(world.CreateConstraint<BallSocket>(centerCube, Vertices[tetrahedra[5 * i + 0].A])); // 0
			ct.Add(world.CreateConstraint<BallSocket>(centerCube, Vertices[tetrahedra[5 * i + 0].B])); // 1
			ct.Add(world.CreateConstraint<BallSocket>(centerCube, Vertices[tetrahedra[5 * i + 0].C])); // 5
			ct.Add(world.CreateConstraint<BallSocket>(centerCube, Vertices[tetrahedra[5 * i + 0].D])); // 2
			ct.Add(world.CreateConstraint<BallSocket>(centerCube, Vertices[tetrahedra[5 * i + 1].C])); // 6
			ct.Add(world.CreateConstraint<BallSocket>(centerCube, Vertices[tetrahedra[5 * i + 1].D])); // 7
			ct.Add(world.CreateConstraint<BallSocket>(centerCube, Vertices[tetrahedra[5 * i + 2].B])); // 3
			ct.Add(world.CreateConstraint<BallSocket>(centerCube, Vertices[tetrahedra[5 * i + 3].A])); // 4

			foreach (var c in ct)
			{
				c.Initialize(c.Body2.Position);
				c.Softness = 0.1f;
				c.Bias = 0.3f;
			}
		}

		isFinished = true;
	}
}

public partial class JitterSoftBodyCubeDrawer : MeshInstance3D
{
	private ImmediateMesh immediateMesh = new();

	private List<CubedSoftBody> cubes = new();

	private Material[] materials = new Material[5];

	public void Clear()
	{
		foreach (var cube in cubes)
		{
			cube.Destroy();
		}

		cubes.Clear();
	}

	public void AddCubedSoftBody(CubedSoftBody body)
	{
		cubes.Add(body);
	}

	public override void _Ready()
	{
		this.Mesh = immediateMesh;

		var mat = ResourceLoader.Load<Material>("res://box.material");

		for (int i = 0; i < 5; i++)
		{
			materials[i] = mat.Duplicate(false) as Material;
		}

		((StandardMaterial3D)materials[0]).AlbedoColor = new Color(0, 0.94f, 0.94f);  // I
		((StandardMaterial3D)materials[1]).AlbedoColor = new Color(0.94f, 0.94f, 0);  // O
		((StandardMaterial3D)materials[2]).AlbedoColor = new Color(0, 0, 0.94f);       // L
		((StandardMaterial3D)materials[3]).AlbedoColor = new Color(0.94f ,0, 0);      // Z
		((StandardMaterial3D)materials[4]).AlbedoColor = new Color(0.63f, 0, 0.94f);  // T

		base._Ready();
	}

	public override void _Process(double delta)
	{
		int surfaces = 0;

		immediateMesh.ClearSurfaces();

		foreach (var cube in cubes)
		{
			immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

			foreach (var quad in cube.Quads)
			{
				if (quad.DiagLeft)
				{
					immediateMesh.SurfaceSetUV(quad.UvA);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.A].Position));
					immediateMesh.SurfaceSetUV(quad.UvB);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.B].Position));
					immediateMesh.SurfaceSetUV(quad.UvC);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.C].Position));
					immediateMesh.SurfaceSetUV(quad.UvA);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.A].Position));
					immediateMesh.SurfaceSetUV(quad.UvC);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.C].Position));
					immediateMesh.SurfaceSetUV(quad.UvD);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.D].Position));
				}
				else
				{
					immediateMesh.SurfaceSetUV(quad.UvA);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.A].Position));
					immediateMesh.SurfaceSetUV(quad.UvB);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.B].Position));
					immediateMesh.SurfaceSetUV(quad.UvD);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.D].Position));
					immediateMesh.SurfaceSetUV(quad.UvB);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.B].Position));
					immediateMesh.SurfaceSetUV(quad.UvC);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.C].Position));
					immediateMesh.SurfaceSetUV(quad.UvD);
					immediateMesh.SurfaceAddVertex(Conversion.FromJitter(cube.Vertices[quad.D].Position));
				}
			}

			immediateMesh.SurfaceEnd();
			immediateMesh.SurfaceSetMaterial(surfaces++, materials[cube.MaterialIndex]);
		}

		base._Process(delta);
	}
}

public partial class Program : Node3D
{
	private World world = null!;

	private JitterSoftBodyCubeDrawer softBodyDrawer = new();

	private void Add2x2x2(CubedSoftBody csb, int x, int y, int z)
	{
		csb.AddCube(x + 1, y + 1, z + 1);
		csb.AddCube(x + 1, y + 1, z - 1);
		csb.AddCube(x - 1, y + 1, z + 1);
		csb.AddCube(x - 1, y + 1, z - 1);
		csb.AddCube(x + 1, y - 1, z + 1);
		csb.AddCube(x + 1, y - 1, z - 1);
		csb.AddCube(x - 1, y - 1, z + 1);
		csb.AddCube(x - 1, y - 1, z - 1);
	}

	private void AddI(int x, int y, int z)
	{
		CubedSoftBody csb = new CubedSoftBody(world);

		Add2x2x2(csb, x, y + 0, z);
		Add2x2x2(csb, x, y + 4, z);
		Add2x2x2(csb, x, y + 8, z);
		Add2x2x2(csb, x, y + 12, z);

		csb.Finalize();
		csb.MaterialIndex = 0;

		softBodyDrawer.AddCubedSoftBody(csb);
	}

	private void AddO(int x, int y, int z)
	{
		CubedSoftBody csb = new CubedSoftBody(world);

		Add2x2x2(csb, x + 0, y + 0, z);
		Add2x2x2(csb, x + 0, y + 4, z);
		Add2x2x2(csb, x + 4, y + 0, z);
		Add2x2x2(csb, x + 4, y + 4, z);

		csb.Finalize();
		csb.MaterialIndex = 1;

		softBodyDrawer.AddCubedSoftBody(csb);
	}

	private void AddL(int x, int y, int z)
	{
		CubedSoftBody csb = new CubedSoftBody(world);

		Add2x2x2(csb, x + 0, y + 0, z);
		Add2x2x2(csb, x + 0, y + 4, z);
		Add2x2x2(csb, x + 4, y + 4, z);
		Add2x2x2(csb, x + 8, y + 4, z);

		csb.Finalize();
		csb.MaterialIndex = 2;

		softBodyDrawer.AddCubedSoftBody(csb);
	}

	private void AddZ(int x, int y, int z)
	{
		CubedSoftBody csb = new CubedSoftBody(world);

		Add2x2x2(csb, x + 0, y + 0, z);
		Add2x2x2(csb, x + 4, y + 0, z);
		Add2x2x2(csb, x + 4, y + 4, z);
		Add2x2x2(csb, x + 8, y + 4, z);

		csb.Finalize();
		csb.MaterialIndex = 3;

		softBodyDrawer.AddCubedSoftBody(csb);
	}

	private void AddT(int x, int y, int z)
	{
		CubedSoftBody csb = new CubedSoftBody(world);

		Add2x2x2(csb, x + 0, y + 0, z);
		Add2x2x2(csb, x + 4, y + 0, z);
		Add2x2x2(csb, x + 8, y + 0, z);
		Add2x2x2(csb, x + 4, y + 4, z);

		csb.Finalize();
		csb.MaterialIndex = 4;

		softBodyDrawer.AddCubedSoftBody(csb);
	}

	public override void _Ready()
	{
		var button = new Button();
		button.Pressed += ResetScene;
		button.Position = new Vector2(4, 4);
		button.Text = "Reset scene";

		AddChild(button);
		AddChild(softBodyDrawer);

		world = new World();
		ResetScene();
	}

	private void ResetScene()
	{
		softBodyDrawer.Clear();

		world.Clear();

		// floor shape
		RigidBody floor = world.CreateRigidBody();
		floor.AddShape(new BoxShape(40));
		floor.Position = new JVector(0, -20, 0);
		floor.IsStatic = true;

		world.DynamicTree.Filter = DynamicTreeCollisionFilter.Filter;
		world.BroadPhaseFilter = new BroadPhaseCollisionFilter(world);

		for (int i = 0; i < 5; i++)
		{
			AddI(0, (5 * i + 0) * 30, 0);
			AddL(0, (5 * i + 1) * 30, 0);
			AddZ(0, (5 * i + 2) * 30, 0);
			AddT(0, (5 * i + 3) * 30, 0);
			AddO(0, (5 * i + 4) * 30, 0);
		}

		world.SubstepCount = 4;
		world.SolverIterations = (4, 1);
	}

	float accumulatedTime = 0.0f;

	public override void _Process(double delta)
	{
		const float fixedStep = 1.0f / 100.0f;
		
		int steps = 0;
		accumulatedTime += (float)delta;

		while (accumulatedTime > fixedStep)
		{
			world.Step(fixedStep, true);
			accumulatedTime -= fixedStep;

			// we can not keep up with the real time, i.e. the simulation
			// is running slower than the real time is passing.
			if (++steps >= 4) return;
		}
	}
}
