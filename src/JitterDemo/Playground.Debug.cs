using System;
using Jitter2.Collision;
using Jitter2.Dynamics;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public partial class Playground : RenderWindow
{
    private bool debugDrawIslands;
    private bool debugDrawContacts;
    private bool debugDrawShapes;
    private bool debugDrawTree;
    private int debugDrawTreeDepth = 1;

    private readonly Action<TreeBox, int> drawBox;

    private void DrawBox(TreeBox treeBBox, int depth)
    {
        if (depth != debugDrawTreeDepth) return;
        DebugRenderer.PushBox(DebugRenderer.Color.Green, Conversion.FromJitter(treeBBox.Min),
            Conversion.FromJitter(treeBBox.Max));
    }

    public Vector3 rayHitPoint = Vector3.Zero;

    public void DebugDraw()
    {
        //DebugRenderer.PushPoint(DebugRenderer.Color.White, rayHitPoint);

        if (debugDrawTree)
        {
            World.DynamicTree.EnumerateTreeBoxes(drawBox);
        }

        if (debugDrawShapes)
        {
            foreach (var shape in World.DynamicTree.Proxies)
            {
                var bb = shape.WorldBoundingBox;
                DebugRenderer.PushBox(DebugRenderer.Color.Green, Conversion.FromJitter(bb.Min),
                    Conversion.FromJitter(bb.Max));
            }
        }

        if (debugDrawIslands)
        {
            for (int i = 0; i < World.Islands.Count; i++)
            {
                Island island = World.Islands[i];

                bool active = false;
                JBoundingBox box = JBoundingBox.SmallBox;
                foreach (RigidBody body in island.Bodies)
                {
                    if (body.Shapes.Count == 0)
                    {
                        // mass point
                        JBoundingBox.AddPointInPlace(ref box, body.Position);
                    }
                    else
                    {
                        foreach (var shape in body.Shapes)
                            JBoundingBox.CreateMerged(box, shape.WorldBoundingBox, out box);
                    }

                    active = body.IsActive;
                }

                DebugRenderer.PushBox(active ? DebugRenderer.Color.Green : DebugRenderer.Color.Red,
                    Conversion.FromJitter(box.Min),
                    Conversion.FromJitter(box.Max));
            }
        }

        if (debugDrawContacts)
        {
            var contacts = World.RawData.ActiveContacts;

            for (int i = 0; i < contacts.Length; i++)
            {
                ref var cq = ref contacts[i];

                void DrawContact(in ContactData cq, in ContactData.Contact c)
                {
                    JVector v1 = c.RelativePosition1 + cq.Body1.Data.Position;
                    JVector v2 = c.RelativePosition2 + cq.Body2.Data.Position;

                    DebugRenderer.PushPoint(DebugRenderer.Color.Green, Conversion.FromJitter(v1), 0.1f);
                    DebugRenderer.PushPoint(DebugRenderer.Color.White, Conversion.FromJitter(v2), 0.1f);
                }

                uint umask = cq.UsageMask >> 4;

                if ((umask & ContactData.MaskContact0) != 0) DrawContact(cq, cq.Contact0);
                if ((umask & ContactData.MaskContact1) != 0) DrawContact(cq, cq.Contact1);
                if ((umask & ContactData.MaskContact2) != 0) DrawContact(cq, cq.Contact2);
                if ((umask & ContactData.MaskContact3) != 0) DrawContact(cq, cq.Contact3);
            }
        }
    }
}