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

public partial class JitterCubes : MultiMeshInstance3D
{
	private List<RigidBody> cubes = new();

	public void AddCube(RigidBody body)
	{
		cubes.Add(body);
	}

	public void Clear() => cubes.Clear();
	
	public override void _Ready()
	{
		Multimesh = new MultiMesh();
		Multimesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform3D;
		Multimesh.Mesh = new BoxMesh();
		Multimesh.Mesh.SurfaceSetMaterial(0, ResourceLoader.Load<Material>("res://box.material"));
		base._Ready();
	}

	public override void _Process(double delta)
	{
		Multimesh.InstanceCount = cubes.Count;

		for (int i = 0; i < cubes.Count; i++)
		{
			ref JMatrix mat = ref cubes[i].Data.Orientation;
			ref JVector pos = ref cubes[i].Data.Position;

			Transform3D trans = Transform3D.Identity;
			trans[0] = Conversion.FromJitter(mat.GetColumn(0));
			trans[1] = Conversion.FromJitter(mat.GetColumn(1));
			trans[2] = Conversion.FromJitter(mat.GetColumn(2));
			trans[3] = Conversion.FromJitter(pos);

			Multimesh.SetInstanceTransform(i, trans);
		}

		base._Process(delta);
	}
}

public partial class Main : Node3D
{
	private World world = null!;
	private JitterCubes jitterCubes = null!;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var button = new Button();
		button.Pressed += ResetScene;
		button.Position = new Vector2(4, 4);
		button.Text = "Reset scene";
		
		jitterCubes = new JitterCubes();
		
		AddChild(jitterCubes);
		AddChild(button);

		world = new World();
		
		ResetScene();
	}

	private void ResetScene()
	{
		jitterCubes.Clear();
		world.Clear();
		
		// floor shape
		RigidBody floor = world.CreateRigidBody();
		floor.AddShape(new BoxShape(20));
		floor.Position = new JVector(0, -10, 0);
		floor.IsStatic = true;
		
		for (int i = 0; i < 30; i++)
		{
			RigidBody body = world.CreateRigidBody();
			body.AddShape(new BoxShape(1));
			body.Position = new JVector(1, 0.5f + i * 4, 0);
			body.Friction = 0.2f;
			jitterCubes.AddCube(body);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		world.Step(1.0f / 100.0f);
	}
}
