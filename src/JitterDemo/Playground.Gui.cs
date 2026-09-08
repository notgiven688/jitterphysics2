using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vellum;
using Vellum.Rendering;
using Jitter2;
using Jitter2.Collision;
using JitterDemo.Renderer;
using JitterDemo.Renderer.OpenGL;
using Color = Vellum.Rendering.Color;

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

    private int selectedDemoIndex = -1;
    private bool demoMenuHintDismissed;
    private readonly double[] debugTimes = new double[(int)World.Timings.Last];
    private readonly StringBuilder gcText = new();
    private readonly float[] physicsTime = new float[100];
    private readonly float[] treeCost = new float[100];
    private readonly HashSet<UiKey> uiPressedKeys = new();
    private readonly HashSet<UiMouseButton> uiDownMouseButtons = new();
    private readonly StringBuilder uiTextInput = new();
    private readonly WindowState statsWindowState = new()
    {
        Position = new System.Numerics.Vector2(16f, 16f),
        Size = new System.Numerics.Vector2(0f, 760f),
        MaxSize = new System.Numerics.Vector2(260f, 0f)
    };

    private double totalTime;
    private int samplingRate = 5;
    private int accSteps;
    private bool treeCostSampleDue;
    private int treeDepth;
    private int maximumTreeImbalance;
    private double treeAreaRatio;
    private double lastTime;
    private ushort frameCount;
    private ushort fps = 100;
    private bool overlayFramePrepared;
    private bool objectsSectionOpen = true;
    private bool optionsSectionOpen = true;
    private bool debugDrawSectionOpen;
    private bool broadphaseSectionOpen;
    private bool treeQualitySectionOpen;
    private bool timingsSectionOpen = true;
    private bool gcSectionOpen = true;

    private void ConfigureGuiTheme()
    {
        Gui.Theme = new Theme
        {
            PanelBg = new Color(20, 20, 20, 80),
            PanelBorder = new Color(156, 156, 156, 72),
            ButtonBg = new Color(68, 68, 68, 120),
            ButtonBgHover = new Color(92, 92, 92, 152),
            ButtonBgPressed = new Color(60, 60, 60, 136),
            ButtonBorder = new Color(148, 148, 148, 72),
            ButtonBorderHover = new Color(188, 188, 188, 96),
            ButtonBorderPressed = new Color(140, 140, 140, 84),
            ToggleBg = new Color(72, 72, 72, 132),
            ToggleBgHover = new Color(72, 104, 156, 176),
            ToggleBgPressed = new Color(60, 90, 140, 188),
            ToggleBgActive = new Color(88, 78, 58, 176),
            ToggleBorder = new Color(148, 148, 148, 72),
            ToggleBorderHover = new Color(154, 184, 224, 124),
            ToggleBorderPressed = new Color(132, 166, 210, 132),
            ToggleBorderActive = new Color(255, 200, 90, 216),
            PlotBg = new Color(54, 84, 132, 108),
            PlotBorder = new Color(136, 170, 222, 132),
            PlotFill = new Color(255, 186, 52, 240),
            SliderBg = new Color(40, 48, 60, 188),
            SliderBgHover = new Color(48, 58, 72, 204),
            SliderBgActive = new Color(34, 42, 54, 216),
            SliderFill = new Color(77, 121, 186, 222),
            SliderFillActive = new Color(96, 145, 216, 234),
            SliderBorder = new Color(142, 152, 168, 108),
            Separator = new Color(196, 196, 196, 112),
            SelectableBg = Color.Transparent,
            SelectableBgHover = new Color(72, 118, 188, 76),
            SelectableBgPressed = new Color(72, 118, 188, 112),
            SelectableBgSelected = new Color(72, 118, 188, 188),
            SelectableBorder = Color.Transparent,
            SelectableBorderHover = Color.Transparent,
            SelectableBorderPressed = Color.Transparent,
            SelectableBorderSelected = new Color(128, 170, 226, 136),
            CollapsingHeaderBg = Color.Transparent,
            CollapsingHeaderBgHover = new Color(72, 118, 188, 64),
            CollapsingHeaderBgPressed = new Color(72, 118, 188, 96),
            CollapsingHeaderBgOpen = new Color(20,20,20, 22),
            CollapsingHeaderPadding = new EdgeInsets(2f, 2f),
            ScrollbarTrack = new Color(80, 80, 80, 150),
            ScrollbarThumb = new Color(40, 40, 40, 150),
            ScrollbarThumbHover = new Color(120, 166, 232, 150),
            ScrollbarThumbActive = new Color(120, 166, 232, 208),
            WindowTitleText = new Color(255, 200, 90),
            TextPrimary = Color.White,
            TextSecondary = new Color(228, 228, 228),
            TreeIndent = 0f,
            UseLcdText = true,
            SliderBlockWidthFactor = 0.2f,
            SliderFillText = Color.White
        };
    }

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

        World.DebugTimings.CopyTo(debugTimes);
        totalTime = debugTimes.Sum();

        for (int i = physicsTime.Length; i-- > 1;)
            physicsTime[i] = physicsTime[i - 1];

        physicsTime[0] = (float)totalTime;
        treeCostSampleDue = true;

        gcText.Append("gen0: ").Append(GC.CollectionCount(0))
              .Append("; gen1: ").Append(GC.CollectionCount(1))
              .Append("; gen2: ").AppendLine(GC.CollectionCount(2).ToString());
        gcText.Append("pause total: ").Append(GC.GetTotalPauseDuration().TotalSeconds).AppendLine(" s");
    }

    private void SampleTreeQuality()
    {
        for (int i = treeCost.Length; i-- > 1;)
            treeCost[i] = treeCost[i - 1];

        DynamicTree tree = World.DynamicTree;
        double cost = tree.CalculateCost();
        treeCost[0] = (float)cost;
        treeDepth = 0;
        maximumTreeImbalance = 0;
        treeAreaRatio = 0.0d;

        if (tree.Root != DynamicTree.NullNode)
        {
            ReadOnlySpan<DynamicTree.Node> nodes = tree.Nodes;
            DynamicTree.Node rootNode = nodes[tree.Root];
            treeDepth = rootNode.Height + 1;

            double rootArea = rootNode.ExpandedBox.GetSurfaceArea();
            if (rootArea > 0.0d) treeAreaRatio = cost / rootArea;

            for (int i = 0; i < nodes.Length; i++)
            {
                DynamicTree.Node node = nodes[i];
                if (node.Left == DynamicTree.NullNode || node.Right == DynamicTree.NullNode)
                    continue;

                int imbalance = int.Abs(nodes[node.Left].Height - nodes[node.Right].Height);
                maximumTreeImbalance = int.Max(maximumTreeImbalance, imbalance);
            }
        }

        treeCostSampleDue = false;
    }

    private void PrepareCustomOverlayFrame(int logicalWidth, int logicalHeight,
        int framebufferWidth, int framebufferHeight)
    {
        var mousePosition = Mouse.Position;
        var logicalMouse = new System.Numerics.Vector2((float)mousePosition.X, (float)mousePosition.Y);

        var frame = new RenderFrameInfo(logicalWidth, logicalHeight, framebufferWidth, framebufferHeight);
        Gui.BeginFrame(frame, logicalMouse, BuildUiInputState());
        BuildDemoOverlay(Gui);

        overlayFramePrepared = true;
        WantsCaptureKeyboard = Gui.WantsCaptureKeyboard;
        WantsCaptureMouse = Gui.WantsCaptureMouse;
    }

    private UiInputState BuildUiInputState()
    {
        uiPressedKeys.Clear();
        AddPressedKey(Keyboard.Key.Left, UiKey.Left);
        AddPressedKey(Keyboard.Key.Right, UiKey.Right);
        AddPressedKey(Keyboard.Key.Up, UiKey.Up);
        AddPressedKey(Keyboard.Key.Down, UiKey.Down);
        AddPressedKey(Keyboard.Key.Home, UiKey.Home);
        AddPressedKey(Keyboard.Key.End, UiKey.End);
        AddPressedKey(Keyboard.Key.Tab, UiKey.Tab);
        AddPressedKey(Keyboard.Key.Enter, UiKey.Enter);
        AddPressedKey(Keyboard.Key.KpEnter, UiKey.Enter);
        AddPressedKey(Keyboard.Key.Escape, UiKey.Escape);
        AddPressedKey(Keyboard.Key.Space, UiKey.Space);
        AddPressedKey(Keyboard.Key.Backspace, UiKey.Backspace);
        AddPressedKey(Keyboard.Key.Delete, UiKey.Delete);
        AddPressedKey(Keyboard.Key.A, UiKey.A);
        AddPressedKey(Keyboard.Key.C, UiKey.C);
        AddPressedKey(Keyboard.Key.V, UiKey.V);
        AddPressedKey(Keyboard.Key.X, UiKey.X);

        uiDownMouseButtons.Clear();
        if (Mouse.IsButtonDown(Mouse.Button.Left)) uiDownMouseButtons.Add(UiMouseButton.Left);
        if (Mouse.IsButtonDown(Mouse.Button.Right)) uiDownMouseButtons.Add(UiMouseButton.Right);
        if (Mouse.IsButtonDown(Mouse.Button.Middle)) uiDownMouseButtons.Add(UiMouseButton.Middle);

        uiTextInput.Clear();
        foreach (uint codepoint in Keyboard.CharInput)
        {
            if (codepoint is > 0 and <= 0x10ffff)
                uiTextInput.Append(char.ConvertFromUtf32((int)codepoint));
        }

        bool shift = Keyboard.IsKeyDown(Keyboard.Key.LeftShift) || Keyboard.IsKeyDown(Keyboard.Key.RightShift);
        bool ctrl = Keyboard.IsKeyDown(Keyboard.Key.LeftControl) || Keyboard.IsKeyDown(Keyboard.Key.RightControl);
        bool alt = Keyboard.IsKeyDown(Keyboard.Key.LeftAlt) || Keyboard.IsKeyDown(Keyboard.Key.RightAlt);
        bool meta = Keyboard.IsKeyDown(Keyboard.Key.LeftSuper) || Keyboard.IsKeyDown(Keyboard.Key.RightSuper);

        return new UiInputState(
            textInput: uiTextInput.Length > 0 ? uiTextInput.ToString() : null,
            pressedKeys: uiPressedKeys.Count > 0 ? uiPressedKeys : null,
            wheelDelta: new System.Numerics.Vector2((float)Mouse.ScrollWheel.X, (float)Mouse.ScrollWheel.Y),
            shift: shift,
            ctrl: ctrl,
            alt: alt,
            meta: meta,
            downMouseButtons: uiDownMouseButtons.Count > 0 ? uiDownMouseButtons : null,
            timeSeconds: Time);
    }

    private void AddPressedKey(Keyboard.Key key, UiKey uiKey)
    {
        if (Keyboard.KeyPressBegin(key))
            uiPressedKeys.Add(uiKey);
    }

    private void BuildDemoOverlay(Ui root)
    {
        float maxPhysicsTime = 0f;
        for (int i = 0; i < physicsTime.Length; i++)
            maxPhysicsTime = MathF.Max(maxPhysicsTime, physicsTime[i]);

        root.Window("Jitter2 Demo", statsWindowState, 260f, content =>
        {
            content.ItemSpacing(0f);
            content.Label($"{fps} fps", color: content.Theme.WindowTitleText);

            string demoMenuLabel = BuildDemoMenuLabel();
            Color originalMenuTextColor = content.Theme.TextPrimary;
            if (!demoMenuHintDismissed)
            {
                float pulse = 0.5f + 0.5f * MathF.Sin((float)Time * 4f);
                Color hintBaseColor = new(110, 110, 110, originalMenuTextColor.A);
                byte r = (byte)(hintBaseColor.R + (originalMenuTextColor.R - hintBaseColor.R) * pulse);
                byte g = (byte)(hintBaseColor.G + (originalMenuTextColor.G - hintBaseColor.G) * pulse);
                byte b = (byte)(hintBaseColor.B + (originalMenuTextColor.B - hintBaseColor.B) * pulse);
                content.Theme.TextPrimary = new Color(r, g, b, originalMenuTextColor.A);
            }

            var demoMenu = content.Menu(demoMenuLabel, popup =>
            {
                popup.ItemSpacing(0);

                for (int i = 0; i < demos.Count; i++)
                {
                    var demo = demos[i];
                    var item = popup.MenuItem($"{MaterialSymbols.ViewInAr} Demo {i:00} - {demo.Name}",
                        selected: i == selectedDemoIndex, closeOnActivate: true, id: i);
                    if (item.Activated)
                        SwitchDemo(i);

                    string tooltip = BuildDemoTooltip(demo);
                    if (!string.IsNullOrWhiteSpace(tooltip))
                        popup.Tooltip(item, tooltip, maxWidth: 420f);
                }
            }, width: content.AvailableWidth, maxPopupHeight: 720f, openOnHover: true, openToSide: true,
                id: "demo-menu");
            content.Theme.TextPrimary = originalMenuTextColor;

            if (!demoMenuHintDismissed && (demoMenu.Hovered || demoMenu.Opened))
                demoMenuHintDismissed = true;

            content.Separator();

            content.CollapsingHeader($"{MaterialSymbols.Numbers} Objects", ref objectsSectionOpen, width: content.AvailableWidth);
            if (objectsSectionOpen)
            {
                World.SpanData data = World.RawData;
                content.Column(labels =>
                {
                    labels.ItemSpacing(1f);
                    TableRow(labels, "Islands", $"{World.Islands.Count}/{World.Islands.ActiveCount}", 82f);
                    TableRow(labels, "Bodies", $"{data.RigidBodies.Length}/{data.ActiveRigidBodies.Length}", 82f);
                    TableRow(labels, "Arbiter", $"{data.Contacts.Length}/{data.ActiveContacts.Length}", 82f);
                    TableRow(labels, "Constraints", $"{data.Constraints.Length}/{data.ActiveConstraints.Length}", 82f);
                    TableRow(labels, "SmallConstraints", $"{data.SmallConstraints.Length}/{data.ActiveSmallConstraints.Length}", 82f);
                    TableRow(labels, "Proxies", $"{World.DynamicTree.Proxies.Count}/{World.DynamicTree.Proxies.ActiveCount}", 82f);
                });
            }

            content.CollapsingHeader($"{MaterialSymbols.Tune} Options", ref optionsSectionOpen, width: content.AvailableWidth);
            if (optionsSectionOpen)
            {
                content.Column(options =>
                {
                    options.ItemSpacing(4f);

                    bool allowDeactivation = World.AllowDeactivation;
                    if (options.Checkbox("Allow Deactivation", ref allowDeactivation).Changed)
                        World.AllowDeactivation = allowDeactivation;

                    bool auxiliaryContacts = World.EnableAuxiliaryContactPoints;
                    if (options.Checkbox("Auxiliary Flat Surface", ref auxiliaryContacts).Changed)
                        World.EnableAuxiliaryContactPoints = auxiliaryContacts;

                    options.Checkbox("Multithreading", ref multiThread);
                });
            }

            content.CollapsingHeader($"{MaterialSymbols.Shapes} Debug Draw", ref debugDrawSectionOpen, width: content.AvailableWidth);
            if (debugDrawSectionOpen)
            {
                content.Column(debugDraw =>
                {
                    debugDraw.ItemSpacing(4f);
                    debugDraw.Checkbox("Islands", ref debugDrawIslands);
                    debugDraw.Checkbox("Contacts", ref debugDrawContacts);
                    debugDraw.Checkbox("Shapes", ref debugDrawShapes);
                });
            }

            bool broadphaseWasOpen = broadphaseSectionOpen;
            content.CollapsingHeader($"{MaterialSymbols.AccountTree} Broadphase", ref broadphaseSectionOpen, width: content.AvailableWidth);
            if (broadphaseSectionOpen)
            {
                content.Column(labels =>
                {
                    labels.ItemSpacing(0f);
                    TableRow(labels, "PairHashSet Size", World.DynamicTree.HashSetInfo.TotalSize.ToString(), 72f);
                    TableRow(labels, "PairHashSet Count", World.DynamicTree.HashSetInfo.Count.ToString(), 72f);
                    TableRow(labels, "Proxies Updated", World.DynamicTree.UpdatedProxyCount.ToString(), 72f);

                    for (int i = 0; i < (int)DynamicTree.Timings.Last; i++)
                        TableRow(labels, ((DynamicTree.Timings)i).ToString(), $"{World.DynamicTree.DebugTimings[i]:N2}", 72f);
                });

                content.Spacing(4f);
                content.Column(controls =>
                {
                    controls.ItemSpacing(4f);
                    controls.Checkbox("Debug draw tree", ref debugDrawTree);

                    if (controls.SliderInt("tree depth", ref debugDrawTreeDepth, 1, 64, controls.AvailableWidth,
                        id: "tree-depth").Changed)
                        debugDrawTree = true;
                });

                content.Spacing(4f);
                bool treeQualityWasOpen = treeQualitySectionOpen;
                content.CollapsingHeader($"{MaterialSymbols.QueryStats} Tree quality", ref treeQualitySectionOpen,
                    width: content.AvailableWidth, id: "tree-quality");
                if (treeQualitySectionOpen)
                {
                    if (treeCostSampleDue || !broadphaseWasOpen || !treeQualityWasOpen)
                        SampleTreeQuality();

                    content.Column(labels =>
                    {
                        labels.ItemSpacing(0f);
                        TableRow(labels, "Tree Depth", treeDepth.ToString(), 72f);
                        TableRow(labels, "Max Imbalance", maximumTreeImbalance.ToString(), 72f);
                        TableRow(labels, "Area Ratio", $"{treeAreaRatio:N2}", 72f);
                    });

                    float maxTreeCost = 0f;
                    for (int i = 0; i < treeCost.Length; i++)
                        maxTreeCost = MathF.Max(maxTreeCost, treeCost[i]);

                    content.Spacing(4f);
                    content.Histogram(treeCost, content.AvailableWidth, 80f,
                        $"SAH current {treeCost[0] / 1000f:N0}k; max {maxTreeCost / 1000f:N0}k",
                        scaleMin: 0f);
                }
                
            }

            content.CollapsingHeader($"{MaterialSymbols.AvgTime} Timings", ref timingsSectionOpen, width: content.AvailableWidth);
            if (timingsSectionOpen)
            {
                content.Column(labels =>
                {
                    labels.ItemSpacing(0f);

                    for (int i = 0; i < (int)World.Timings.Last; i++)
                        TableRow(labels, ((World.Timings)i).ToString(), $"{debugTimes[i]:N2}", 72f);
                });
                
                content.Spacing(4);

                content.Histogram(physicsTime, content.AvailableWidth, 80f,
                    $"max. {maxPhysicsTime:0.00} ms", scaleMin: 0f);
                content.Column(labels =>
                {
                    labels.ItemSpacing(0f);
                    labels.Label($"Total: {totalTime:0.00}ms ({(totalTime > 0 ? 1000.0d / totalTime : 0d):0} fps)",
                        maxWidth: labels.AvailableWidth,
                        overflow: TextOverflowMode.Ellipsis);
                });
                
                content.Spacing(4f);
                
                content.SliderInt("sampling rate", ref samplingRate, 1, 10, content.AvailableWidth,
                    id: "sample-rate");
            }

            content.CollapsingHeader($"{MaterialSymbols.Memory} GC statistics", ref gcSectionOpen, width: content.AvailableWidth);
            if (gcSectionOpen)
            {
                content.Label(gcText.ToString(), maxWidth: content.AvailableWidth, wrap: TextWrapMode.WordWrap);
            }
        }, resizable: true, closable: false, header: false, id: "stats");
    }

    private string BuildDemoTooltip(IDemo demo)
    {
        if (string.IsNullOrWhiteSpace(demo.Description) && string.IsNullOrWhiteSpace(demo.Controls))
            return string.Empty;

        if (string.IsNullOrWhiteSpace(demo.Controls))
            return $"{demo.Description}\n\n{GlobalControls}";

        if (string.IsNullOrWhiteSpace(demo.Description))
            return $"{GlobalControls}\n{demo.Controls}";

        return $"{demo.Description}\n\n{GlobalControls}\n{demo.Controls}";
    }

    private string BuildDemoMenuLabel()
    {
        if (selectedDemoIndex < 0 || selectedDemoIndex >= demos.Count)
            return $"Select Demo Scene";

        const int maxNameLength = 18;
        string name = demos[selectedDemoIndex].Name;
        if (name.Length > maxNameLength)
            name = name[..(maxNameLength - 3)] + "...";

        return $"Demo {selectedDemoIndex:00} - {name}";
    }

    private static void TableRow(Ui ui, string label, string value, float valueWidth = 96f)
    {
        const float columnGap = 8f;

        using (ui.Row())
        {
            ui.ItemSpacing(columnGap);

            float rowWidth = ui.AvailableWidth;
            float resolvedValueWidth = MathF.Min(valueWidth, MathF.Max(0f, rowWidth));
            float labelWidth = MathF.Max(0f, rowWidth - resolvedValueWidth - columnGap);

            using (ui.FixedWidth(labelWidth))
            {
                ui.Label(label,
                    maxWidth: ui.AvailableWidth,
                    overflow: TextOverflowMode.Ellipsis);
            }

            using (ui.FixedWidth(resolvedValueWidth))
            {
                ui.Label(value,
                    color: ui.Theme.TextSecondary,
                    maxWidth: ui.AvailableWidth,
                    overflow: TextOverflowMode.Ellipsis,
                    width: ui.AvailableWidth,
                    align: UiAlign.End);
            }
        }
    }
}
