using System;
using System.Linq;
using System.Text;
using Jitter2;
using Jitter2.Collision;
using JitterDemo.Renderer;
using JitterDemo.Renderer.DearImGui;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo;

public partial class Playground : RenderWindow
{
    private const string GlobalControls =
        "[Controls]\n" +
        "WASD - Move camera\n" +
        "Right Mouse (hold) - Rotate camera\n" +
        "Left Mouse (hold) - Grab object\n" +
        "Scroll Wheel - Adjust grab distance\n" +
        "Space - Shoot cube\n" +
        "M - Toggle multi-threading";

    private bool demoSelected;
    private readonly double[] debugTimes = new double[(int)World.Timings.Last];
    private readonly StringBuilder gcText = new();

    private readonly float[] physicsTime = new float[100];
    private double totalTime;

    private int samplingRate = 5;
    private int accSteps;

    private double lastTime;
    private ushort frameCount;
    private ushort fps = 100;

    private void UpdateDisplayText()
    {
        if (Time - lastTime > 1.0d)
        {
            lastTime = Time;
            fps = frameCount;
            frameCount = 0;
        }

        frameCount++;

        accSteps += 1;
        if (accSteps < samplingRate) return;

        accSteps = 0;

        gcText.Clear();

        World.DebugTimings.CopyTo(debugTimes, 0);
        totalTime = debugTimes.Sum();

        for (int i = physicsTime.Length; i-- > 1;)
        {
            physicsTime[i] = physicsTime[i - 1];
        }

        physicsTime[0] = (float)totalTime;

        gcText.Append("gen0: ").Append(GC.CollectionCount(0))
              .Append("; gen1: ").Append(GC.CollectionCount(1))
              .Append("; gen2: ").AppendLine(GC.CollectionCount(2).ToString());
        gcText.Append("pause total: ").Append(GC.GetTotalPauseDuration().TotalSeconds).AppendLine(" s");
    }

    private void LayoutGui()
    {
        bool opened = true;

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoDecoration |
                                       ImGuiWindowFlags.AlwaysAutoResize |
                                       ImGuiWindowFlags.NoSavedSettings |
                                       ImGuiWindowFlags.NoFocusOnAppearing;

        ImGui.NewFrame();

        ImGui.SetNextWindowsPos(new Vector2(10, 10), ImGuiCond.Once, Vector2.Zero);
        ImGui.SetNextWindowBgAlpha(0.35f);

        ImGui.Begin("##overlay", ref opened, windowFlags);
        ImGui.SetStyle();

        ImGui.Text($"{fps} fps", new Vector4(1, 1, 0, 1));

        if (!demoSelected)
        {
            float alpha = (float)(0.5 + 0.5 * Math.Sin(ImGui.GetTime() * 4.0));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, alpha));
        }

        if (ImGui.BeginMenu("Select Demo Scene", true))
        {
            if (!demoSelected) ImGui.PopStyleColor();
            demoSelected = true;

            for (int i = 0; i < demos.Count; i++)
            {
                if (ImGui.MenuItem($"Demo {i:00} - {demos[i].Name}", string.Empty, false, true))
                {
                    SwitchDemo(i);
                }

                if (demos[i].Description.Length > 0)
                {
                    ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(1.0f, 1.0f, 0.88f, 0.95f));
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                    if (ImGui.BeginItemTooltip())
                    {
                        ImGui.Text(demos[i].Description);
                        ImGui.Text(string.Empty);
                        ImGui.Text(GlobalControls, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
                        if (demos[i].Controls.Length > 0)
                        {
                            ImGui.Text(demos[i].Controls, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
                        }

                        ImGui.EndTooltip();
                    }

                    ImGui.PopStyleColor(2);
                }
            }

            ImGui.EndMenu();
        }
        else if (!demoSelected)
        {
            ImGui.PopStyleColor();
        }

        ImGui.Separator();

        ImGui.NextTreeNodeOpen(true);
        if (ImGui.TreeNode("Objects"))
        {
            BeginFixedTable("##objects", 3);
            World.SpanData data = World.RawData;

            AddTableRow("Islands", $"{World.Islands.Count,5}", $"{World.Islands.ActiveCount,5}");
            AddTableRow("Bodies", $"{data.RigidBodies.Length,5}", $"{data.ActiveRigidBodies.Length,5}");
            AddTableRow("Arbiter", $"{data.Contacts.Length,5}", $"{data.ActiveContacts.Length,5}");
            AddTableRow("Constraints", $"{data.Constraints.Length,5}", $"{data.ActiveConstraints.Length,5}");
            AddTableRow("SmallConstraints", $"{data.SmallConstraints.Length,5}", $"{data.ActiveSmallConstraints.Length,5}");
            AddTableRow("Proxies", $"{World.DynamicTree.Proxies.Count,5}", $"{World.DynamicTree.Proxies.ActiveCount,5}");

            ImGui.EndTable();
            ImGui.TreePop();
        }

        ImGui.NextTreeNodeOpen(true);
        if (ImGui.TreeNode("Options"))
        {
            bool allowDeactivation = World.AllowDeactivation;
            ImGui.Checkbox("Allow Deactivation", ref allowDeactivation);
            World.AllowDeactivation = allowDeactivation;

            bool auxiliaryContacts = World.EnableAuxiliaryContactPoints;
            ImGui.Checkbox("Auxiliary Flat Surface", ref auxiliaryContacts);
            World.EnableAuxiliaryContactPoints = auxiliaryContacts;

            ImGui.Checkbox("Multithreading", ref multiThread);

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Debug Draw"))
        {
            ImGui.Checkbox("Islands", ref debugDrawIslands);
            ImGui.Checkbox("Contacts", ref debugDrawContacts);
            ImGui.Checkbox("Shapes", ref debugDrawShapes);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Broadphase"))
        {
            BeginFixedTable("##broadphase", 2);

            AddTableRow("PairHashSet Size", $"{World.DynamicTree.HashSetInfo.TotalSize,6}");
            AddTableRow("PairHashSet Count", $"{World.DynamicTree.HashSetInfo.Count,6}");
            AddTableRow("Proxies updated", $"{World.DynamicTree.UpdatedProxyCount,6}");

            for (int i = 0; i < (int)DynamicTree.Timings.Last; i++)
            {
                AddTableRow($"{(DynamicTree.Timings)i}",
                    $"{World.DynamicTree.DebugTimings[i],6:N2}");
            }

            ImGui.EndTable();

            ImGui.Checkbox("Debug draw tree", ref debugDrawTree);

            if (ImGui.Slider("##depthslider", ref debugDrawTreeDepth, 1, 64, "Tree depth (%d)", ImGuiSliderFlags.None))
            {
                debugDrawTree = true;
            }

            ImGui.TreePop();
        }

        ImGui.NextTreeNodeOpen(true);
        if (ImGui.TreeNode("Timings"))
        {
            BeginFixedTable("##timings", 2);

            for (int i = 0; i < (int)World.Timings.Last; i++)
            {
                AddTableRow(((World.Timings)i).ToString(), $"{debugTimes[i],6:N2}");
            }

            ImGui.EndTable();

            float max = physicsTime.Max();
            ImGui.PlotHistogram(physicsTime, "##histogram", $"max. {max:f2} ms", 0, max * 1.0f, 200, 80);

            ImGui.Text($"Total: {totalTime,0:N2} ms ({1000.0d / totalTime,0:N0} fps)");
            ImGui.Slider("##sampleslider", ref samplingRate, 1, 10, "sampling rate (%d)", ImGuiSliderFlags.None);

            ImGui.TreePop();
        }

        ImGui.NextTreeNodeOpen(true);
        if (ImGui.TreeNode("GC statistics"))
        {
            ImGui.Text(gcText.ToString());
            ImGui.TreePop();
        }

        ImGui.End();

        // ImGui.ShowDemo();

        ImGui.EndFrame();
        ImGui.Render();
    }

    private static void BeginFixedTable(string id, int columns)
    {
        ImGui.BeginTable(id, columns,
            ImGuiTableFlags.NoBordersInBody |
            ImGuiTableFlags.SizingFixedFit |
            ImGuiTableFlags.Resizable,
            Vector2.Zero, 0);

        ImGui.SetupColumn(string.Empty, ImGuiTableColumnFlags.WidthStretch, 0, 0);
        for (int i = 1; i < columns; i++)
        {
            ImGui.SetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 0, 0);
        }
    }

    private static void AddTableRow(params string[] columns)
    {
        ImGui.TableNextRow();
        for (int i = 0; i < columns.Length; i++)
        {
            ImGui.TableSetColumnIndex(i);
            ImGui.Text(columns[i]);
        }
    }
}