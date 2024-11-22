namespace JitterTests;

public static class Helper
{
    public static void AdvanceWorld(World world, int seconds, double dt, bool multiThread)
    {
        int total = (int)(seconds / dt);
        for (int i = 0; i < total; i++)
            world.Step(dt, multiThread);
    }

    public static RigidBody BuildTower(World world, JVector pos, int size = 40)
    {
        JQuaternion halfRotationStep = JQuaternion.CreateRotationY(Math.PI * 2.0 / 64.0);
        JQuaternion fullRotationStep = halfRotationStep * halfRotationStep;
        JQuaternion orientation = JQuaternion.Identity;

        RigidBody last = null!;

        for (int e = 0; e < size; e++)
        {
            orientation *= halfRotationStep;

            for (int i = 0; i < 32; i++)
            {
                JVector position = pos + JVector.Transform(
                    new JVector(0, 0.5 + e, 19.5), orientation);

                var shape = new BoxShape(3, 1, 0.5);

                last = world.CreateRigidBody();

                last.Orientation = orientation;
                last.Position = position;
                last.AddShape(shape);

                orientation *= fullRotationStep;

                if (e == 0) last.IsStatic = true;
            }
        }

        return last;
    }

    public static RigidBody BuildSimpleStack(World world, int size = 12)
    {
        RigidBody last = null!;

        for (int i = 0; i < size; i++)
        {
            last = world.CreateRigidBody();
            //body.AddShape(new BoxShape(1));
            last.Position = new JVector(0, 0.5 + i * 0.99, 0);
            last.AddShape(new BoxShape(1));
            last.Damping = (0.002, 0.002);

            if (i == 0) last.IsStatic = true;
        }

        return last;
    }

    public static RigidBody BuildPyramidBox(World world, JVector position, int size = 20)
    {
        RigidBody last = null!;
        for (int i = 0; i < size; i++)
        {
            for (int e = i; e < size; e++)
            {
                last = world.CreateRigidBody();
                last.Position = position + new JVector((e - i * 0.5) * 1.01, 0.5 + i * 1.0, 0.0);
                var shape = new BoxShape(1);
                last.AddShape(shape);

                if (i == 0) last.IsStatic = true;
            }
        }

        return last;
    }

    public static RigidBody BuildPyramidCylinder(World world, JVector position, int size = 20)
    {
        RigidBody last = null!;
        for (int i = 0; i < size; i++)
        {
            for (int e = i; e < size; e++)
            {
                last = world.CreateRigidBody();
                last.Position = position + new JVector((e - i * 0.5) * 1.01, 0.5 + i * 1.0, 0.0);
                var shape = new CylinderShape(1.0, 0.5);
                last.AddShape(shape);

                if (i == 0) last.IsStatic = true;
            }
        }

        return last;
    }
}