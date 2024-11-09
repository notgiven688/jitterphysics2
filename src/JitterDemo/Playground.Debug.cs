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

    private readonly Action<JBBox, int> drawBox;

    private void DrawBox(JBBox box, int depth)
    {
        if (depth != debugDrawTreeDepth) return;
        DebugRenderer.PushBox(DebugRenderer.Color.Green, Conversion.FromJitter(box.Min),
            Conversion.FromJitter(box.Max));
    }

    public Vector3 rayHitPoint = Vector3.Zero;

    public void DebugDraw()
    {
        //DebugRenderer.PushPoint(DebugRenderer.Color.White, rayHitPoint);

        if (debugDrawTree)
        {
            World.DynamicTree.EnumerateAABB(drawBox);
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
                JBBox box = JBBox.SmallBox;
                foreach (RigidBody body in island.Bodies)
                {
                    if (body.Shapes.Count == 0)
                    {
                        // mass point
                        box.AddPoint(body.Position);
                    }
                    else
                    {
                        foreach (var shape in body.Shapes)
                            JBBox.CreateMerged(box, shape.WorldBoundingBox, out box);
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

                if ((cq.UsageMask & ContactData.MaskContact0) != 0) DrawContact(cq, cq.Contact0);
                if ((cq.UsageMask & ContactData.MaskContact1) != 0) DrawContact(cq, cq.Contact1);
                if ((cq.UsageMask & ContactData.MaskContact2) != 0) DrawContact(cq, cq.Contact2);
                if ((cq.UsageMask & ContactData.MaskContact3) != 0) DrawContact(cq, cq.Contact3);
            }
        }
    }
}