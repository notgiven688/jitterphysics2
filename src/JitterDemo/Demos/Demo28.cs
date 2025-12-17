using System;
using Jitter2;
using Jitter2.Collision.Shapes;
using Jitter2.LinearMath;
using JitterDemo.Renderer;

namespace JitterDemo;

/*
 * Jitter2 Colosseum Demo
 * This demo logic is ported from BepuPhysics2
 * https://github.com/bepu/bepuphysics2/blob/cfb5daa1837aef30a5437ac347ac583f2ffaf2b0/Demos/Demos/ColosseumDemo.cs
 * Original Copyright Ross Nordby.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * * http://www.apache.org/licenses/LICENSE-2.0
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

public class Demo28: IDemo
{
    public string Name => "Colosseum";

    private World world = null!;

    private static void CreateRingWall(World world, JVector position, JVector size, int height, float radius)
    {
        var circumference = MathF.PI * 2 * radius;
        var boxCountPerRing = (int)(0.9f * circumference / size.Z);
        float increment = (2.0f*MathF.PI) / boxCountPerRing;
        for (int ringIndex = 0; ringIndex < height; ringIndex++)
        {
            for (int i = 0; i < boxCountPerRing; i++)
            {
                var body = world.CreateRigidBody();
                body.AddShape(new BoxShape(size));

                var angle = ((ringIndex & 1) == 0 ? i + 0.5f : i) * increment;
                body.Position = position + new JVector(-MathF.Cos(angle) * radius, (ringIndex + 0.5f) * size.Y, MathF.Sin(angle) * radius);
                body.Orientation = JQuaternion.CreateFromAxisAngle(JVector.UnitY, angle);
            }
        }
    }

    private static void CreateRingPlatform(World world, JVector position, JVector size, float radius)
    {
        var innerCircumference = MathF.PI * 2 * (radius - 0.5f * size.Z);
        var boxCount = (int)(0.95f * innerCircumference / size.Y);
        float increment = (2.0f*MathF.PI)/ boxCount;
        for (int i = 0; i < boxCount; i++)
        {
            var angle = i * increment;

            var body = world.CreateRigidBody();
            body.AddShape(new BoxShape(size));

            body.Position = position + new JVector(-MathF.Cos(angle) * radius, 0.5f * size.X, MathF.Sin(angle) * radius);
            body.Orientation = JQuaternion.CreateFromAxisAngle(JVector.UnitY, angle + MathF.PI * 0.5f)*JQuaternion.CreateFromAxisAngle(JVector.UnitZ, MathF.PI * 0.5f);
        }
    }

    private static JVector CreateRing(World world, JVector position, JVector size, float radius, int heightPerPlatformLevel, int platformLevels)
    {
        for (int platformIndex = 0; platformIndex < platformLevels; ++platformIndex)
        {
            var wallOffset = 0.5f * size.Z - 0.5f * size.X;
            CreateRingWall(world, position, size, heightPerPlatformLevel, radius + wallOffset);
            CreateRingWall(world, position, size, heightPerPlatformLevel, radius - wallOffset);

            CreateRingPlatform(world, position + new JVector(0, heightPerPlatformLevel * size.Y, 0), size, radius);
            position.Y += heightPerPlatformLevel * size.Y + size.X;
        }
        return position;
    }

    public void Build()
    {
        Playground pg = (Playground)RenderWindow.Instance;
        world = pg.World;

        pg.ResetScene();

        var size = new JVector(0.5f, 1, 3);
        var layerPosition = new JVector();
        const int layerCount = 6;
        var innerRadius = 15f;
        var heightPerPlatform = 3;
        var platformsPerLayer = 1;
        var ringSpacing = 0.5f;

        for (int layerIndex = 0; layerIndex < layerCount; ++layerIndex)
        {
            var ringCount = layerCount - layerIndex;
            for (int ringIndex = 0; ringIndex < ringCount; ++ringIndex)
            {
                CreateRing(world, layerPosition, size, innerRadius + ringIndex * (size.Z + ringSpacing) + layerIndex * (size.Z - size.X), heightPerPlatform, platformsPerLayer);
            }
            layerPosition.Y += platformsPerLayer * (size.Y * heightPerPlatform + size.X);
        }
    }

    public void Draw()
    {

    }
}