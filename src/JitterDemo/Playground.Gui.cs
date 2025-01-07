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
    private readonly double[] debugTimes = new double[(int)World.Timings.Last];
    private readonly StringBuilder gcText = new();

    public float[] physicsTime = new float[100];
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

        gcText.AppendLine(
            $"gen0: {GC.CollectionCount(0)}; gen1: {GC.CollectionCount(1)}; gen2: {GC.CollectionCount(2)}");
        gcText.AppendLine($"pause total: {GC.GetTotalPauseDuration().TotalSeconds} s");
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

        ImGui.Begin("overlay", ref opened, windowFlags);
        ImGui.SetStyle();

        ImGui.Text($"{fps} fps", new Vector4(1, 1, 0, 1));

        if (ImGui.BeginMenu("Select Demo Scene", true))
        {
            for (int i = 0; i < demos.Count; i++)
            {
                if (ImGui.MenuItem($"Demo {i:00} - {demos[i].Name}", string.Empty, false, true))
                {
                    SwitchDemo(i);
                }
            }

            ImGui.EndMenu();
        }

        ImGui.Separator();

        ImGui.NextTreeNodeOpen(true);
        if (ImGui.TreeNode("Objects"))
        {
            ImGui.BeginTable(string.Empty, 3,
                ImGuiTableFlags.NoBordersInBody |
                ImGuiTableFlags.SizingFixedFit |
                ImGuiTableFlags.Resizable,
                Vector2.Zero, 0);

            ImGui.SetupColumn(string.Empty, ImGuiTableColumnFlags.WidthStretch, 0, 0);
            ImGui.SetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 0, 0);
            ImGui.SetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 0, 0);

            void AddRow(string a, string b, string c)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(a);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(b.PadLeft(5));
                ImGui.TableSetColumnIndex(2);
                ImGui.Text(c.PadLeft(5));
            }

            World.SpanData data = World.RawData;

            AddRow("Islands", $"{World.Islands.Count}", $"{World.Islands.Active}");
            AddRow("Bodies", $"{data.RigidBodies.Length}", $"{data.ActiveRigidBodies.Length}");
            AddRow("Arbiter", $"{data.Contacts.Length}", $"{data.ActiveContacts.Length}");
            AddRow("Constraints", $"{data.Constraints.Length}", $"{data.ActiveConstraints.Length}");
            AddRow("SmallConstraints", $"{data.SmallConstraints.Length}", $"{data.ActiveSmallConstraints.Length}");
            AddRow("Proxies", $"{World.DynamicTree.Proxies.Count}", $"{World.DynamicTree.Proxies.Active}");

            ImGui.EndTable();
            ImGui.TreePop();
        }

        ImGui.NextTreeNodeOpen(true);
        if (ImGui.TreeNode("Options"))
        {
            bool ufes = World.AllowDeactivation;
            ImGui.Checkbox("Allow Deactivation", ref ufes);
            World.AllowDeactivation = ufes;

            ufes = World.EnableAuxiliaryContactPoints;
            ImGui.Checkbox("Auxiliary Flat Surface", ref ufes);
            World.EnableAuxiliaryContactPoints = ufes;

            ImGui.Checkbox("Multithreading", ref multiThread);
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
            ImGui.BeginTable(string.Empty, 2,
                ImGuiTableFlags.NoBordersInBody |
                ImGuiTableFlags.SizingFixedFit |
                ImGuiTableFlags.Resizable,
                Vector2.Zero, 0);

            ImGui.SetupColumn(string.Empty, ImGuiTableColumnFlags.WidthStretch, 0, 0);
            ImGui.SetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 0, 0);

            void AddRow(string a, string b)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(a);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(b.PadLeft(6));
            }

            AddRow("PairHashSet Size", World.DynamicTree.HashSetInfo.TotalSize.ToString());
            AddRow("PairHashSet Count", World.DynamicTree.HashSetInfo.Count.ToString());
            AddRow("Proxies updated", World.DynamicTree.UpdatedProxies.ToString());

            for (int i = 0; i < (int)DynamicTree.Timings.Last; i++)
            {
                AddRow($"{(DynamicTree.Timings)i} (ms)",
                    $"{World.DynamicTree.DebugTimings[i],0:N2}");
            }

            ImGui.EndTable();

            ImGui.Checkbox("Debug draw tree", ref debugDrawTree);

            if (ImGui.Slider("##foo2", ref debugDrawTreeDepth, 1, 64, "Tree depth (%d)", ImGuiSliderFlags.None))
            {
                debugDrawTree = true;
            }

            ImGui.TreePop();
        }

        ImGui.NextTreeNodeOpen(true);
        if (ImGui.TreeNode("Timings"))
        {
            ImGui.BeginTable(string.Empty, 2,
                ImGuiTableFlags.NoBordersInBody |
                ImGuiTableFlags.SizingFixedFit |
                ImGuiTableFlags.Resizable,
                Vector2.Zero, 0);

            ImGui.SetupColumn(string.Empty, ImGuiTableColumnFlags.WidthStretch, 0, 0);
            ImGui.SetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 0, 0);

            void AddRow(string a, string b)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(a);
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(b.PadLeft(6));
            }

            for (int i = 0; i < (int)World.Timings.Last; i++)
            {
                AddRow(((World.Timings)i).ToString(), $"{debugTimes[i],0:N2}");
            }

            ImGui.EndTable();

            float max = physicsTime.Max();
            ImGui.PlotHistogram(physicsTime, string.Empty, $"max. {max:f2} ms", 0, max * 1.0f, 200, 80);

            ImGui.Text($"Total: {totalTime,0:N2} ms ({1000.0d / totalTime,0:N0} fps)");
            ImGui.Slider("##foo1", ref samplingRate, 1, 10, "sampling rate (%d)", ImGuiSliderFlags.None);

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
}