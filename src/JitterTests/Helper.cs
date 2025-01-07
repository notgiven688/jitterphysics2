namespace JitterTests;

public static class Helper
{
    public static void AdvanceWorld(World world, int seconds, Real dt, bool multiThread)
    {
        int total = (int)(seconds / dt);
        for (int i = 0; i < total; i++)
            world.Step(dt, multiThread);
    }

    public static RigidBody BuildTower(World world, JVector pos, int size = 40)
    {
        JQuaternion halfRotationStep = JQuaternion.CreateRotationY(MathR.PI * (Real)(2.0 / 64.0));
        JQuaternion fullRotationStep = halfRotationStep * halfRotationStep;
        JQuaternion orientation = JQuaternion.Identity;

        RigidBody last = null!;

        for (int e = 0; e < size; e++)
        {
            orientation *= halfRotationStep;

            for (int i = 0; i < 32; i++)
            {
                JVector position = pos + JVector.Transform(
                    new JVector(0, (Real)0.5 + e, (Real)19.5), orientation);

                var shape = new BoxShape(3f, 1, (Real)0.5);

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
            last.Position = new JVector(0, (Real)0.5 + i * (Real)0.99, 0);
            last.AddShape(new BoxShape(1));
            last.Damping = ((Real)0.998, (Real)0.998);

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
                last.Position = position + new JVector((e - i * (Real)0.5) * (Real)1.01, (Real)0.5 + i * (Real)1.0, (Real)0.0);
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
                last.Position = position + new JVector((e - i * (Real)0.5) * (Real)1.01, (Real)0.5 + i * (Real)1.0, (Real)0.0);
                var shape = new CylinderShape((Real)1.0, (Real)0.5);
                last.AddShape(shape);

                if (i == 0) last.IsStatic = true;
            }
        }

        return last;
    }
}