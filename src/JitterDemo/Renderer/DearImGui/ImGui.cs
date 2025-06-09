using System;
using System.Runtime.InteropServices;
using System.Text;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer.DearImGui;

public static unsafe class ImGui
{
    // Allocate three temporary buffers on the heap. This is slower than on the stack but saves us
    // from calling stackalloc in every method. - "1K is more memory than anyone will ever need"
    private static readonly byte** strPtr;
    private const int strPtrSize = 1024;

    static ImGui()
    {
        strPtr = (byte**)NativeMemory.Alloc((nuint)(sizeof(IntPtr) * 3));

        for (int i = 0; i < 3; i++)
        {
            strPtr[i] = (byte*)NativeMemory.Alloc(strPtrSize);
        }
    }

    private static void PushStr(in string str, int index)
    {
        fixed (char* c = str)
        {
            int bc0 = Encoding.UTF8.GetByteCount(c, str.Length);
            if (bc0 + 1 > strPtrSize) throw new ArgumentException("UTF8 endcoded string too long.");
            Encoding.UTF8.GetBytes(c, str.Length, strPtr[index], bc0);
            strPtr[index][bc0] = 0;
        }
    }

    private static string PopStr(int index)
    {
        int len = 0;
        byte* ptr = strPtr[index];

        while (*ptr != 0)
        {
            if (++len == strPtrSize)
            {
                return string.Empty;
            }

            ptr += 1;
        }

        return Encoding.UTF8.GetString(strPtr[index], len);
    }

    public static bool BeginTable(string str_id, int column, ImGuiTableFlags flags, Vector2 outerSize, float innerWidth)
    {
        PushStr(str_id, 0);
        byte result = ImGuiNative.igBeginTable(strPtr[0], column, flags, outerSize, innerWidth);
        return Convert.ToBoolean(result);
    }

    public static bool Slider(string label, ref int value, int min, int max, string format, ImGuiSliderFlags flags)
    {
        PushStr(label, 0);
        PushStr(format, 1);

        fixed (int* ptr = &value)
        {
            byte result = ImGuiNative.igSliderInt(strPtr[0], ptr, min, max, strPtr[1], flags);
            return Convert.ToBoolean(result);
        }
    }

    public static void Begin(string name, ref bool open, ImGuiWindowFlags flags)
    {
        PushStr(name, 0);
        byte cb = 0b0;
        if (open) cb = 0b1;
        ImGuiNative.igBegin(strPtr[0], &cb, flags);
        open = cb == 0b1;
    }

    public static void DisableIni()
    {
        var io = ImGuiNative.igGetIO();
        io->IniFilename = (byte*)0;
    }

    public static void TableNextColumn()
    {
        ImGuiNative.igTableNextColumn();
    }

    public static void TableNextRow()
    {
        ImGuiNative.igTableNextRow(ImGuiTableRowFlags.None, 0);
    }

    public static void TableSetColumnIndex(int index)
    {
        ImGuiNative.igTableSetColumnIndex(index);
    }

    public static void EndTable()
    {
        ImGuiNative.igEndTable();
    }

    public static void NewFrame()
    {
        ImGuiNative.igNewFrame();
    }

    public static void SetNextWindowsPos(in Vector2 pos, ImGuiCond cond, in Vector2 pivot)
    {
        ImGuiNative.igSetNextWindowPos(pos, cond, pivot);
    }

    public static void SetNextWindowBgAlpha(float alpha)
    {
        ImGuiNative.igSetNextWindowBgAlpha(alpha);
    }

    public static void SetStyle(float windowBorderSize = 1.0f, float frameBordersize = 1.0f,
        float indentSpacing = 1.0f, float windowRounding = 6.0f, float popupRounding = 6.0f)
    {
        ImGuiStyle* style = ImGuiNative.igGetStyle();
        style->WindowBorderSize = windowBorderSize;
        style->FrameBorderSize = frameBordersize;
        style->IndentSpacing = indentSpacing;
        style->WindowRounding = windowRounding;
        style->PopupRounding = popupRounding;
    }

    public static void Separator()
    {
        ImGuiNative.igSeparator();
    }

    public static void End()
    {
        ImGuiNative.igEnd();
    }

    public static void EndFrame()
    {
        ImGuiNative.igEndFrame();
    }

    public static void Render()
    {
        ImGuiNative.igRender();
    }

    public static void SameLine(float offsetFromStartx, float spacing)
    {
        ImGuiNative.igSameLine(offsetFromStartx, spacing);
    }

    public static void Checkbox(string label, ref bool value)
    {
        PushStr(label, 0);
        byte cb = 0b0;
        if (value) cb = 0b1;
        ImGuiNative.igCheckbox(strPtr[0], &cb);
        value = cb == 0b1;
    }

    public static float GetWindowWidth()
    {
        return ImGuiNative.igGetWindowWidth();
    }

    public static bool Button(string label, Vector2 size)
    {
        PushStr(label, 0);
        var result = ImGuiNative.igButton(strPtr[0], size);
        return Convert.ToBoolean(result);
    }

    public static void InputText(string label, ref string text, ImGuiInputTextFlags flags)
    {
        PushStr(label, 0);
        PushStr(text, 1);
        ImGuiNative.igInputText(strPtr[0], strPtr[1], strPtrSize, flags, null, (void*)0);
        text = PopStr(1);
    }

    public static void PushStyleVar(ImGuiStyleVar var, in Vector2 vec2)
    {
        ImGuiNative.igPushStyleVar_Vec2(var, vec2);
    }

    public static void SetupColumn(string label, ImGuiTableColumnFlags flags, float initWidthOrHeight, uint userId)
    {
        PushStr(label, 0);
        ImGuiNative.igTableSetupColumn(strPtr[0], flags, initWidthOrHeight, userId);
    }

    public static bool BeginMenu(string label, bool enabled)
    {
        PushStr(label, 0);
        return Convert.ToBoolean(ImGuiNative.igBeginMenu(strPtr[0], Convert.ToByte(enabled)));
    }

    public static bool MenuItem(string label, string shortcut, bool selected, bool enabled)
    {
        PushStr(label, 0);
        PushStr(shortcut, 1);
        return Convert.ToBoolean(
            ImGuiNative.igMenuItem_Bool(strPtr[0], strPtr[1],
                Convert.ToByte(selected), Convert.ToByte(enabled)));
    }

    public static void EndMenu()
    {
        ImGuiNative.igEndMenu();
    }

    public static void Text(string text)
    {
        PushStr(text, 0);
        ImGuiNative.igText(strPtr[0]);
    }

    public static void Text(string text, Vector4 color)
    {
        PushStr(text, 0);
        ImGuiNative.igTextColored(color, strPtr[0]);
    }

    public static bool TreeNode(string label)
    {
        PushStr(label, 0);
        return Convert.ToBoolean(ImGuiNative.igTreeNode_Str(strPtr[0]));
    }

    public static void SetNextItemWidth(float width)
    {
        ImGuiNative.igSetNextItemWidth(width);
    }

    public static void TreePop()
    {
        ImGuiNative.igTreePop();
    }

    public static void NextTreeNodeOpen(bool open)
    {
        ImGuiNative.igSetNextItemOpen(Convert.ToByte(open), ImGuiCond.FirstUseEver);
    }

    public static void NextItemOpen(bool open)
    {
        ImGuiNative.igSetNextItemOpen(Convert.ToByte(open), ImGuiCond.FirstUseEver);
    }

    public static void PlotHistogram(float[] array, string label, string overlayText, float min, float max, float sizeX,
        float sizeY)
    {
        PushStr(label, 0);
        PushStr(overlayText, 1);

        fixed (float* ptr = array)
        {
            ImGuiNative.igPlotHistogram_FloatPtr(strPtr[0], ptr, array.Length, 0, strPtr[1], min, max,
                new Vector2(sizeX, sizeY), sizeof(float));
        }
    }

    public static void ShowDemo()
    {
        byte popen = 1;
        ImGuiNative.igShowDemoWindow(&popen);
    }
}