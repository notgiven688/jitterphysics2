using System;
using Jitter2;
using Jitter2.Collision;
using Jitter2.Dynamics;
using Jitter2.Dynamics.Constraints;
using Jitter2.LinearMath;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public partial class Playground : RenderWindow
{
    private bool debugDrawIslands;
    private bool debugDrawContacts;
    private bool debugDrawShapeBounds;
    private bool debugDrawBodies;
    private bool debugDrawConstraints;
    private float debugDrawConstraintSize = 0.5f;
    private bool debugDrawTree;
    private int debugDrawTreeDepth = 1;

    public bool IsConstraintDebugDrawEnabled => debugDrawConstraints;

    private readonly Action<TreeBox, int> drawBox;

    private void DrawBox(TreeBox treeBBox, int depth)
    {
        if (depth != debugDrawTreeDepth) return;
        DebugRenderer.PushBox(DebugRenderer.Color.Green, Conversion.FromJitter(treeBBox.Min),
            Conversion.FromJitter(treeBBox.Max));
    }

    private void DrawContact(in ContactData cq, in ContactData.Contact c)
    {
        JVector v1 = c.RelativePosition1 + cq.Body1.Data.Position;
        JVector v2 = c.RelativePosition2 + cq.Body2.Data.Position;

        DebugRenderer.PushPoint(DebugRenderer.Color.Green, Conversion.FromJitter(v1), 0.1f);
        DebugRenderer.PushPoint(DebugRenderer.Color.White, Conversion.FromJitter(v2), 0.1f);
    }

    private void DebugDraw()
    {
        if (debugDrawBodies)
        {
            bool wireframe = DebugRenderer.WireframeTriangles;
            DebugRenderer.WireframeTriangles = true;
            try
            {
                var settings = new DebugDrawSettings { Segments = 16, ShapeSubdivisions = 2 };
                foreach (RigidBody body in World.RigidBodies)
                    body.DebugDraw(DebugRenderer, settings);
            }
            finally
            {
                DebugRenderer.WireframeTriangles = wireframe;
            }
        }

        if (debugDrawConstraints)
        {
            var settings = new DebugDrawSettings
            {
                GuideSize = debugDrawConstraintSize,
                FrameSize = debugDrawConstraintSize,
                PointRadius = 0.06f * debugDrawConstraintSize,
                ArrowHeadSize = 0.2f * debugDrawConstraintSize,
                ArrowShaftRadius = 0.025f * debugDrawConstraintSize,
                Segments = 24,
                ArrowSegments = 8
            };
            foreach (RigidBody body in World.RigidBodies)
            {
                foreach (Constraint constraint in body.Constraints)
                {
                    // Each constraint belongs to both bodies. NullBody is also in World.RigidBodies.
                    if (constraint.Body1 != body || !constraint.IsValid || !constraint.IsEnabled) continue;
                    DrawConstraint(constraint, settings);
                }
            }
        }

        if (debugDrawTree)
        {
            World.DynamicTree.EnumerateTreeBoxes(drawBox);
        }

        if (debugDrawShapeBounds)
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
            var contacts = World.RawData.Contacts;

            for (int i = 0; i < contacts.Length; i++)
            {
                ref var cq = ref contacts[i];
                uint umask = cq.UsageMask >> 4;

                if ((umask & ContactData.MaskContact0) != 0) DrawContact(cq, cq.Contact0);
                if ((umask & ContactData.MaskContact1) != 0) DrawContact(cq, cq.Contact1);
                if ((umask & ContactData.MaskContact2) != 0) DrawContact(cq, cq.Contact2);
                if ((umask & ContactData.MaskContact3) != 0) DrawContact(cq, cq.Contact3);
            }
        }
    }

    private void DrawConstraint(Constraint constraint, DebugDrawSettings settings)
    {
        // Composite joints use their ball socket's anchor for the angular guides.
        // Search the smaller body's list, since NullBody can have many unrelated constraints.
        var candidates = constraint.Body1.Constraints.Count <= constraint.Body2.Constraints.Count
            ? constraint.Body1.Constraints
            : constraint.Body2.Constraints;
        foreach (Constraint candidate in candidates)
        {
            if (candidate is not BallSocket socket || !socket.IsValid || !socket.IsEnabled) continue;
            if (socket.Body1 == constraint.Body1 && socket.Body2 == constraint.Body2)
            {
                settings = settings with { Origin = socket.Anchor1 };
                break;
            }
            if (socket.Body2 == constraint.Body1 && socket.Body1 == constraint.Body2)
            {
                settings = settings with { Origin = socket.Anchor2 };
                break;
            }
        }
        constraint.DebugDraw(DebugRenderer, settings);
    }
}
