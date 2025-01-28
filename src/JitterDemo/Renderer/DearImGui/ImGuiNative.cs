using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer.DearImGui;

[SuppressMessage("Usage", "CA1401: P/Invokes should not be visible")]
[SuppressMessage("Usage", "CA1069: Enums should not have duplicate values")]
[SuppressMessage("Usage",
    "SYSLIB1054: Use LibraryImportAttribute instead of DllImportAttribute to generate p/invoke marshalling code at compile time.")]
public static unsafe class ImGuiNative
{
    public const string LIBCIMGUI = "cimgui";

    static ImGuiNative()
    {
        ImportResolver.Load();
    }

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiPayload* igAcceptDragDropPayload(byte* type, ImGuiDragDropFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igAlignTextToFramePadding();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igArrowButton(byte* str_id, ImGuiDir dir);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBegin(byte* name, byte* p_open, ImGuiWindowFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginChild_Str(byte* str_id, Vector2 size, ImGuiChildFlags child_flags, ImGuiWindowFlags window_flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginChild_ID(uint id, Vector2 size, ImGuiChildFlags child_flags, ImGuiWindowFlags window_flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginCombo(byte* label, byte* preview_value, ImGuiComboFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igBeginDisabled(byte disabled);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginDragDropSource(ImGuiDragDropFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginDragDropTarget();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igBeginGroup();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginItemTooltip();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginListBox(byte* label, Vector2 size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginMainMenuBar();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginMenu(byte* label, byte enabled);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginMenuBar();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiMultiSelectIO* igBeginMultiSelect(ImGuiMultiSelectFlags flags, int selection_size, int items_count);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginPopup(byte* str_id, ImGuiWindowFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginPopupContextItem(byte* str_id, ImGuiPopupFlags popup_flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginPopupContextVoid(byte* str_id, ImGuiPopupFlags popup_flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginPopupContextWindow(byte* str_id, ImGuiPopupFlags popup_flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginPopupModal(byte* name, byte* p_open, ImGuiWindowFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginTabBar(byte* str_id, ImGuiTabBarFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginTabItem(byte* label, byte* p_open, ImGuiTabItemFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginTable(byte* str_id, int columns, ImGuiTableFlags flags, Vector2 outer_size,
        float inner_width);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igBeginTooltip();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igBullet();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igBulletText(byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igButton(byte* label, Vector2 size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igCalcItemWidth();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igCalcTextSize(Vector2* pOut, byte* text, byte* text_end,
        byte hide_text_after_double_hash, float wrap_width);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igCheckbox(byte* label, byte* v);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igCheckboxFlags_IntPtr(byte* label, int* flags, int flags_value);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igCheckboxFlags_UintPtr(byte* label, uint* flags, uint flags_value);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igCloseCurrentPopup();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igCollapsingHeader_TreeNodeFlags(byte* label, ImGuiTreeNodeFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igCollapsingHeader_BoolPtr(byte* label, byte* p_visible, ImGuiTreeNodeFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igColorButton(byte* desc_id, Vector4 col, ImGuiColorEditFlags flags, Vector2 size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igColorConvertFloat4ToU32(Vector4 @in);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igColorConvertHSVtoRGB(float h, float s, float v, float* out_r, float* out_g,
        float* out_b);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igColorConvertRGBtoHSV(float r, float g, float b, float* out_h, float* out_s,
        float* out_v);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igColorConvertU32ToFloat4(Vector4* pOut, uint @in);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igColorEdit3(byte* label, Vector3* col, ImGuiColorEditFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igColorEdit4(byte* label, Vector4* col, ImGuiColorEditFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igColorPicker3(byte* label, Vector3* col, ImGuiColorEditFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igColorPicker4(byte* label, Vector4* col, ImGuiColorEditFlags flags, float* ref_col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igColumns(int count, byte* id, byte borders);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igCombo_Str_arr(byte* label, int* current_item, byte** items, int items_count,
        int popup_max_height_in_items);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igCombo_Str(byte* label, int* current_item, byte* items_separated_by_zeros,
        int popup_max_height_in_items);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr igCreateContext(ImFontAtlas* shared_font_atlas);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDebugCheckVersionAndDataLayout(byte* version_str, uint sz_io, uint sz_style,
        uint sz_vec2, uint sz_vec4, uint sz_drawvert, uint sz_drawidx);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igDebugFlashStyleColor(ImGuiCol idx);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igDebugLog(byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igDebugStartItemPicker();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igDebugTextEncoding(byte* text);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igDestroyContext(IntPtr ctx);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragFloat(byte* label, float* v, float v_speed, float v_min, float v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragFloat2(byte* label, Vector2* v, float v_speed, float v_min, float v_max,
        byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragFloat3(byte* label, Vector3* v, float v_speed, float v_min, float v_max,
        byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragFloat4(byte* label, Vector4* v, float v_speed, float v_min, float v_max,
        byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragFloatRange2(byte* label, float* v_current_min, float* v_current_max, float v_speed,
        float v_min, float v_max, byte* format, byte* format_max, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragInt(byte* label, int* v, float v_speed, int v_min, int v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragInt2(byte* label, int* v, float v_speed, int v_min, int v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragInt3(byte* label, int* v, float v_speed, int v_min, int v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragInt4(byte* label, int* v, float v_speed, int v_min, int v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragIntRange2(byte* label, int* v_current_min, int* v_current_max, float v_speed,
        int v_min, int v_max, byte* format, byte* format_max, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragScalar(byte* label, ImGuiDataType data_type, void* p_data, float v_speed,
        void* p_min, void* p_max, byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igDragScalarN(byte* label, ImGuiDataType data_type, void* p_data, int components,
        float v_speed, void* p_min, void* p_max, byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igDummy(Vector2 size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEnd();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndChild();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndCombo();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndDisabled();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndDragDropSource();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndDragDropTarget();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndFrame();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndGroup();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndListBox();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndMainMenuBar();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndMenu();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndMenuBar();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiMultiSelectIO* igEndMultiSelect();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndPopup();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndTabBar();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndTabItem();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndTable();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igEndTooltip();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetAllocatorFunctions(IntPtr* p_alloc_func, IntPtr* p_free_func, void** p_user_data);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImDrawList* igGetBackgroundDrawList_Nil();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* igGetClipboardText();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igGetColorU32_Col(ImGuiCol idx, float alpha_mul);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igGetColorU32_Vec4(Vector4 col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igGetColorU32_U32(uint col, float alpha_mul);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int igGetColumnIndex();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetColumnOffset(int column_index);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int igGetColumnsCount();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetColumnWidth(int column_index);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetContentRegionAvail(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr igGetCurrentContext();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetCursorPos(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetCursorPosX();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetCursorPosY();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetCursorScreenPos(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetCursorStartPos(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiPayload* igGetDragDropPayload();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImDrawData* igGetDrawData();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr igGetDrawListSharedData();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFont* igGetFont();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetFontSize();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetFontTexUvWhitePixel(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImDrawList* igGetForegroundDrawList_Nil();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int igGetFrameCount();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetFrameHeight();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetFrameHeightWithSpacing();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igGetID_Str(byte* str_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igGetID_StrStr(byte* str_id_begin, byte* str_id_end);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igGetID_Ptr(void* ptr_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igGetID_Int(int int_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiIO* igGetIO();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint igGetItemID();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetItemRectMax(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetItemRectMin(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetItemRectSize(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* igGetKeyName(ImGuiKey key);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int igGetKeyPressedAmount(ImGuiKey key, float repeat_delay, float rate);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiViewport* igGetMainViewport();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int igGetMouseClickedCount(ImGuiMouseButton button);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiMouseCursor igGetMouseCursor();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetMouseDragDelta(Vector2* pOut, ImGuiMouseButton button, float lock_threshold);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetMousePos(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetMousePosOnOpeningCurrentPopup(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiPlatformIO* igGetPlatformIO();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetScrollMaxX();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetScrollMaxY();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetScrollX();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetScrollY();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiStorage* igGetStateStorage();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiStyle* igGetStyle();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* igGetStyleColorName(ImGuiCol idx);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector4* igGetStyleColorVec4(ImGuiCol idx);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetTextLineHeight();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetTextLineHeightWithSpacing();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern double igGetTime();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetTreeNodeToLabelSpacing();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* igGetVersion();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImDrawList* igGetWindowDrawList();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetWindowHeight();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetWindowPos(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igGetWindowSize(Vector2* pOut);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float igGetWindowWidth();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igImage(IntPtr user_texture_id, Vector2 image_size, Vector2 uv0, Vector2 uv1,
        Vector4 tint_col, Vector4 border_col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igImageButton(byte* str_id, IntPtr user_texture_id, Vector2 image_size, Vector2 uv0,
        Vector2 uv1, Vector4 bg_col, Vector4 tint_col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igIndent(float indent_w);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputDouble(byte* label, double* v, double step, double step_fast, byte* format,
        ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputFloat(byte* label, float* v, float step, float step_fast, byte* format,
        ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputFloat2(byte* label, Vector2* v, byte* format, ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputFloat3(byte* label, Vector3* v, byte* format, ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputFloat4(byte* label, Vector4* v, byte* format, ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputInt(byte* label, int* v, int step, int step_fast, ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputInt2(byte* label, int* v, ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputInt3(byte* label, int* v, ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputInt4(byte* label, int* v, ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputScalar(byte* label, ImGuiDataType data_type, void* p_data, void* p_step,
        void* p_step_fast, byte* format, ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputScalarN(byte* label, ImGuiDataType data_type, void* p_data, int components,
        void* p_step, void* p_step_fast, byte* format, ImGuiInputTextFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputText(byte* label, byte* buf, uint buf_size, ImGuiInputTextFlags flags,
        ImGuiInputTextCallback? callback, void* user_data);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputTextMultiline(byte* label, byte* buf, uint buf_size, Vector2 size,
        ImGuiInputTextFlags flags, ImGuiInputTextCallback callback, void* user_data);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInputTextWithHint(byte* label, byte* hint, byte* buf, uint buf_size,
        ImGuiInputTextFlags flags, ImGuiInputTextCallback callback, void* user_data);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igInvisibleButton(byte* str_id, Vector2 size, ImGuiButtonFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsAnyItemActive();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsAnyItemFocused();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsAnyItemHovered();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsAnyMouseDown();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemActivated();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemActive();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemClicked(ImGuiMouseButton mouse_button);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemDeactivated();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemDeactivatedAfterEdit();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemEdited();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemFocused();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemHovered(ImGuiHoveredFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemToggledOpen();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemToggledSelection();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsItemVisible();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsKeyChordPressed_Nil(ImGuiKey key_chord);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsKeyDown_Nil(ImGuiKey key);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsKeyPressed_Bool(ImGuiKey key, byte repeat);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsKeyReleased_Nil(ImGuiKey key);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsMouseClicked_Bool(ImGuiMouseButton button, byte repeat);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsMouseDoubleClicked_Nil(ImGuiMouseButton button);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsMouseDown_Nil(ImGuiMouseButton button);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsMouseDragging(ImGuiMouseButton button, float lock_threshold);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsMouseHoveringRect(Vector2 r_min, Vector2 r_max, byte clip);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsMousePosValid(Vector2* mouse_pos);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsMouseReleased_Nil(ImGuiMouseButton button);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsPopupOpen_Str(byte* str_id, ImGuiPopupFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsRectVisible_Nil(Vector2 size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsRectVisible_Vec2(Vector2 rect_min, Vector2 rect_max);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsWindowAppearing();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsWindowCollapsed();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsWindowFocused(ImGuiFocusedFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igIsWindowHovered(ImGuiHoveredFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igLabelText(byte* label, byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igListBox_Str_arr(byte* label, int* current_item, byte** items, int items_count,
        int height_in_items);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igLoadIniSettingsFromDisk(byte* ini_filename);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igLoadIniSettingsFromMemory(byte* ini_data, uint ini_size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igLogButtons();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igLogFinish();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igLogText(byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igLogToClipboard(int auto_open_depth);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igLogToFile(int auto_open_depth, byte* filename);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igLogToTTY(int auto_open_depth);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* igMemAlloc(uint size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igMemFree(void* ptr);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igMenuItem_Bool(byte* label, byte* shortcut, byte selected, byte enabled);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igMenuItem_BoolPtr(byte* label, byte* shortcut, byte* p_selected, byte enabled);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igNewFrame();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igNewLine();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igNextColumn();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igOpenPopup_Str(byte* str_id, ImGuiPopupFlags popup_flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igOpenPopup_ID(uint id, ImGuiPopupFlags popup_flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igOpenPopupOnItemClick(byte* str_id, ImGuiPopupFlags popup_flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPlotHistogram_FloatPtr(byte* label, float* values, int values_count, int values_offset,
        byte* overlay_text, float scale_min, float scale_max, Vector2 graph_size, int stride);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPlotLines_FloatPtr(byte* label, float* values, int values_count, int values_offset,
        byte* overlay_text, float scale_min, float scale_max, Vector2 graph_size, int stride);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPopClipRect();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPopFont();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPopID();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPopItemFlag();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPopItemWidth();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPopStyleColor(int count);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPopStyleVar(int count);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPopTextWrapPos();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igProgressBar(float fraction, Vector2 size_arg, byte* overlay);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushClipRect(Vector2 clip_rect_min, Vector2 clip_rect_max,
        byte intersect_with_current_clip_rect);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushFont(ImFont* font);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushID_Str(byte* str_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushID_StrStr(byte* str_id_begin, byte* str_id_end);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushID_Ptr(void* ptr_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushID_Int(int int_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushItemFlag(ImGuiItemFlags option, byte enabled);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushItemWidth(float item_width);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushStyleColor_U32(ImGuiCol idx, uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushStyleColor_Vec4(ImGuiCol idx, Vector4 col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushStyleVar_Float(ImGuiStyleVar idx, float val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushStyleVar_Vec2(ImGuiStyleVar idx, Vector2 val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushStyleVarX(ImGuiStyleVar idx, float val_x);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushStyleVarY(ImGuiStyleVar idx, float val_y);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igPushTextWrapPos(float wrap_local_pos_x);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igRadioButton_Bool(byte* label, byte active);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igRadioButton_IntPtr(byte* label, int* v, int v_button);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igRender();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igResetMouseDragDelta(ImGuiMouseButton button);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSameLine(float offset_from_start_x, float spacing);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSaveIniSettingsToDisk(byte* ini_filename);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* igSaveIniSettingsToMemory(uint* out_ini_size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSelectable_Bool(byte* label, byte selected, ImGuiSelectableFlags flags, Vector2 size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSelectable_BoolPtr(byte* label, byte* p_selected, ImGuiSelectableFlags flags,
        Vector2 size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSeparator();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSeparatorText(byte* label);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetAllocatorFunctions(IntPtr alloc_func, IntPtr free_func, void* user_data);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetClipboardText(byte* text);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetColorEditOptions(ImGuiColorEditFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetColumnOffset(int column_index, float offset_x);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetColumnWidth(int column_index, float width);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetCurrentContext(IntPtr ctx);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetCursorPos(Vector2 local_pos);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetCursorPosX(float local_x);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetCursorPosY(float local_y);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetCursorScreenPos(Vector2 pos);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSetDragDropPayload(byte* type, void* data, uint sz, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetItemDefaultFocus();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetItemKeyOwner_Nil(ImGuiKey key);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetItemTooltip(byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetKeyboardFocusHere(int offset);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetMouseCursor(ImGuiMouseCursor cursor_type);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNavCursorVisible(byte visible);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextFrameWantCaptureKeyboard(byte want_capture_keyboard);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextFrameWantCaptureMouse(byte want_capture_mouse);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextItemAllowOverlap();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextItemOpen(byte is_open, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextItemSelectionUserData(long selection_user_data);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextItemShortcut(ImGuiKey key_chord, ImGuiInputFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextItemStorageID(uint storage_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextItemWidth(float item_width);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextWindowBgAlpha(float alpha);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextWindowCollapsed(byte collapsed, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextWindowContentSize(Vector2 size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextWindowFocus();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextWindowPos(Vector2 pos, ImGuiCond cond, Vector2 pivot);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextWindowScroll(Vector2 scroll);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextWindowSize(Vector2 size, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetNextWindowSizeConstraints(Vector2 size_min, Vector2 size_max,
        ImGuiSizeCallback custom_callback, void* custom_callback_data);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetScrollFromPosX_Float(float local_x, float center_x_ratio);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetScrollFromPosY_Float(float local_y, float center_y_ratio);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetScrollHereX(float center_x_ratio);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetScrollHereY(float center_y_ratio);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetScrollX_Float(float scroll_x);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetScrollY_Float(float scroll_y);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetStateStorage(ImGuiStorage* storage);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetTabItemClosed(byte* tab_or_docked_window_label);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetTooltip(byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowCollapsed_Bool(byte collapsed, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowCollapsed_Str(byte* name, byte collapsed, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowFocus_Nil();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowFocus_Str(byte* name);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowFontScale(float scale);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowPos_Vec2(Vector2 pos, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowPos_Str(byte* name, Vector2 pos, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowSize_Vec2(Vector2 size, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSetWindowSize_Str(byte* name, Vector2 size, ImGuiCond cond);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igShortcut_Nil(ImGuiKey key_chord, ImGuiInputFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igShowAboutWindow(byte* p_open);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igShowDebugLogWindow(byte* p_open);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igShowDemoWindow(byte* p_open);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igShowFontSelector(byte* label);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igShowIDStackToolWindow(byte* p_open);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igShowMetricsWindow(byte* p_open);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igShowStyleEditor(ImGuiStyle* @ref);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igShowStyleSelector(byte* label);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igShowUserGuide();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderAngle(byte* label, float* v_rad, float v_degrees_min, float v_degrees_max,
        byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderFloat(byte* label, float* v, float v_min, float v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderFloat2(byte* label, Vector2* v, float v_min, float v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderFloat3(byte* label, Vector3* v, float v_min, float v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderFloat4(byte* label, Vector4* v, float v_min, float v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderInt(byte* label, int* v, int v_min, int v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderInt2(byte* label, int* v, int v_min, int v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderInt3(byte* label, int* v, int v_min, int v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderInt4(byte* label, int* v, int v_min, int v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderScalar(byte* label, ImGuiDataType data_type, void* p_data, void* p_min,
        void* p_max, byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSliderScalarN(byte* label, ImGuiDataType data_type, void* p_data, int components,
        void* p_min, void* p_max, byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igSmallButton(byte* label);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igSpacing();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igStyleColorsClassic(ImGuiStyle* dst);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igStyleColorsDark(ImGuiStyle* dst);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igStyleColorsLight(ImGuiStyle* dst);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTabItemButton(byte* label, ImGuiTabItemFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTableAngledHeadersRow();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int igTableGetColumnCount();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiTableColumnFlags igTableGetColumnFlags(int column_n);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int igTableGetColumnIndex();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* igTableGetColumnName_Int(int column_n);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int igTableGetHoveredColumn();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int igTableGetRowIndex();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiTableSortSpecs* igTableGetSortSpecs();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTableHeader(byte* label);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTableHeadersRow();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTableNextColumn();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTableNextRow(ImGuiTableRowFlags row_flags, float min_row_height);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTableSetBgColor(ImGuiTableBgTarget target, uint color, int column_n);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTableSetColumnEnabled(int column_n, byte v);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTableSetColumnIndex(int column_n);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTableSetupColumn(byte* label, ImGuiTableColumnFlags flags, float init_width_or_weight,
        uint user_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTableSetupScrollFreeze(int cols, int rows);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igText(byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTextColored(Vector4 col, byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTextDisabled(byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTextLink(byte* label);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTextLinkOpenURL(byte* label, byte* url);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTextUnformatted(byte* text, byte* text_end);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTextWrapped(byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTreeNode_Str(byte* label);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTreeNode_StrStr(byte* str_id, byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTreeNode_Ptr(void* ptr_id, byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTreeNodeEx_Str(byte* label, ImGuiTreeNodeFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTreeNodeEx_StrStr(byte* str_id, ImGuiTreeNodeFlags flags, byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igTreeNodeEx_Ptr(void* ptr_id, ImGuiTreeNodeFlags flags, byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTreePop();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTreePush_Str(byte* str_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igTreePush_Ptr(void* ptr_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igUnindent(float indent_w);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igValue_Bool(byte* prefix, byte b);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igValue_Int(byte* prefix, int v);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igValue_Uint(byte* prefix, uint v);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void igValue_Float(byte* prefix, float v, byte* float_format);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igVSliderFloat(byte* label, Vector2 size, float* v, float v_min, float v_max,
        byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igVSliderInt(byte* label, Vector2 size, int* v, int v_min, int v_max, byte* format,
        ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte igVSliderScalar(byte* label, Vector2 size, ImGuiDataType data_type, void* p_data,
        void* p_min, void* p_max, byte* format, ImGuiSliderFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImColor_destroy(ImColor* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImColor_HSV(ImColor* pOut, float h, float s, float v, float a);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImColor* ImColor_ImColor_Nil();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImColor* ImColor_ImColor_Float(float r, float g, float b, float a);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImColor* ImColor_ImColor_Vec4(Vector4 col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImColor* ImColor_ImColor_Int(int r, int g, int b, int a);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImColor* ImColor_ImColor_U32(uint rgba);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImColor_SetHSV(ImColor* self, float h, float s, float v, float a);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawCmd_destroy(ImDrawCmd* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ImDrawCmd_GetTexID(ImDrawCmd* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImDrawCmd* ImDrawCmd_ImDrawCmd();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawData_AddDrawList(ImDrawData* self, ImDrawList* draw_list);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawData_Clear(ImDrawData* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawData_DeIndexAllBuffers(ImDrawData* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawData_destroy(ImDrawData* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImDrawData* ImDrawData_ImDrawData();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawData_ScaleClipRects(ImDrawData* self, Vector2 fb_scale);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ImDrawList__CalcCircleAutoSegmentCount(ImDrawList* self, float radius);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__ClearFreeMemory(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__OnChangedClipRect(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__OnChangedTextureID(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__OnChangedVtxOffset(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__PathArcToFastEx(ImDrawList* self, Vector2 center, float radius,
        int a_min_sample, int a_max_sample, int a_step);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__PathArcToN(ImDrawList* self, Vector2 center, float radius, float a_min,
        float a_max, int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__PopUnusedDrawCmd(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__ResetForNewFrame(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__SetTextureID(ImDrawList* self, IntPtr texture_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList__TryMergeDrawCmds(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddBezierCubic(ImDrawList* self, Vector2 p1, Vector2 p2, Vector2 p3,
        Vector2 p4, uint col, float thickness, int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddBezierQuadratic(ImDrawList* self, Vector2 p1, Vector2 p2, Vector2 p3,
        uint col, float thickness, int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddCallback(ImDrawList* self, IntPtr callback, void* userdata,
        uint userdata_size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddCircle(ImDrawList* self, Vector2 center, float radius, uint col,
        int num_segments, float thickness);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddCircleFilled(ImDrawList* self, Vector2 center, float radius, uint col,
        int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddConcavePolyFilled(ImDrawList* self, Vector2* points, int num_points,
        uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddConvexPolyFilled(ImDrawList* self, Vector2* points, int num_points,
        uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddDrawCmd(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddEllipse(ImDrawList* self, Vector2 center, Vector2 radius, uint col,
        float rot, int num_segments, float thickness);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddEllipseFilled(ImDrawList* self, Vector2 center, Vector2 radius, uint col,
        float rot, int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddImage(ImDrawList* self, IntPtr user_texture_id, Vector2 p_min,
        Vector2 p_max, Vector2 uv_min, Vector2 uv_max, uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddImageQuad(ImDrawList* self, IntPtr user_texture_id, Vector2 p1, Vector2 p2,
        Vector2 p3, Vector2 p4, Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4, uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddImageRounded(ImDrawList* self, IntPtr user_texture_id, Vector2 p_min,
        Vector2 p_max, Vector2 uv_min, Vector2 uv_max, uint col, float rounding, ImDrawFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddLine(ImDrawList* self, Vector2 p1, Vector2 p2, uint col, float thickness);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddNgon(ImDrawList* self, Vector2 center, float radius, uint col,
        int num_segments, float thickness);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddNgonFilled(ImDrawList* self, Vector2 center, float radius, uint col,
        int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddPolyline(ImDrawList* self, Vector2* points, int num_points, uint col,
        ImDrawFlags flags, float thickness);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddQuad(ImDrawList* self, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4,
        uint col, float thickness);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddQuadFilled(ImDrawList* self, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4,
        uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddRect(ImDrawList* self, Vector2 p_min, Vector2 p_max, uint col,
        float rounding, ImDrawFlags flags, float thickness);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddRectFilled(ImDrawList* self, Vector2 p_min, Vector2 p_max, uint col,
        float rounding, ImDrawFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddRectFilledMultiColor(ImDrawList* self, Vector2 p_min, Vector2 p_max,
        uint col_upr_left, uint col_upr_right, uint col_bot_right, uint col_bot_left);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddText_Vec2(ImDrawList* self, Vector2 pos, uint col, byte* text_begin,
        byte* text_end);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddText_FontPtr(ImDrawList* self, ImFont* font, float font_size, Vector2 pos,
        uint col, byte* text_begin, byte* text_end, float wrap_width, Vector4* cpu_fine_clip_rect);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddTriangle(ImDrawList* self, Vector2 p1, Vector2 p2, Vector2 p3, uint col,
        float thickness);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_AddTriangleFilled(ImDrawList* self, Vector2 p1, Vector2 p2, Vector2 p3,
        uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_ChannelsMerge(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_ChannelsSetCurrent(ImDrawList* self, int n);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_ChannelsSplit(ImDrawList* self, int count);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImDrawList* ImDrawList_CloneOutput(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_destroy(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_GetClipRectMax(Vector2* pOut, ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_GetClipRectMin(Vector2* pOut, ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImDrawList* ImDrawList_ImDrawList(IntPtr shared_data);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathArcTo(ImDrawList* self, Vector2 center, float radius, float a_min,
        float a_max, int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathArcToFast(ImDrawList* self, Vector2 center, float radius, int a_min_of_12,
        int a_max_of_12);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathBezierCubicCurveTo(ImDrawList* self, Vector2 p2, Vector2 p3, Vector2 p4,
        int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathBezierQuadraticCurveTo(ImDrawList* self, Vector2 p2, Vector2 p3,
        int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathClear(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathEllipticalArcTo(ImDrawList* self, Vector2 center, Vector2 radius, float rot, float a_min, float a_max, int num_segments);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathFillConcave(ImDrawList* self, uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathFillConvex(ImDrawList* self, uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathLineTo(ImDrawList* self, Vector2 pos);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathLineToMergeDuplicate(ImDrawList* self, Vector2 pos);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathRect(ImDrawList* self, Vector2 rect_min, Vector2 rect_max, float rounding,
        ImDrawFlags flags);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PathStroke(ImDrawList* self, uint col, ImDrawFlags flags, float thickness);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PopClipRect(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PopTextureID(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PrimQuadUV(ImDrawList* self, Vector2 a, Vector2 b, Vector2 c, Vector2 d,
        Vector2 uv_a, Vector2 uv_b, Vector2 uv_c, Vector2 uv_d, uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PrimRect(ImDrawList* self, Vector2 a, Vector2 b, uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PrimRectUV(ImDrawList* self, Vector2 a, Vector2 b, Vector2 uv_a, Vector2 uv_b,
        uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PrimReserve(ImDrawList* self, int idx_count, int vtx_count);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PrimUnreserve(ImDrawList* self, int idx_count, int vtx_count);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PrimVtx(ImDrawList* self, Vector2 pos, Vector2 uv, uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PrimWriteIdx(ImDrawList* self, ushort idx);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PrimWriteVtx(ImDrawList* self, Vector2 pos, Vector2 uv, uint col);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PushClipRect(ImDrawList* self, Vector2 clip_rect_min, Vector2 clip_rect_max,
        byte intersect_with_current_clip_rect);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PushClipRectFullScreen(ImDrawList* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawList_PushTextureID(ImDrawList* self, IntPtr texture_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawListSplitter_Clear(ImDrawListSplitter* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawListSplitter_ClearFreeMemory(ImDrawListSplitter* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawListSplitter_destroy(ImDrawListSplitter* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImDrawListSplitter* ImDrawListSplitter_ImDrawListSplitter();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawListSplitter_Merge(ImDrawListSplitter* self, ImDrawList* draw_list);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawListSplitter_SetCurrentChannel(ImDrawListSplitter* self, ImDrawList* draw_list,
        int channel_idx);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImDrawListSplitter_Split(ImDrawListSplitter* self, ImDrawList* draw_list, int count);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_AddGlyph(ImFont* self, ImFontConfig* src_cfg, ushort c, float x0, float y0,
        float x1, float y1, float u0, float v0, float u1, float v1, float advance_x);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_AddRemapChar(ImFont* self, ushort dst, ushort src, byte overwrite_dst);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_BuildLookupTable(ImFont* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_CalcTextSizeA(Vector2* pOut, ImFont* self, float size, float max_width,
        float wrap_width, byte* text_begin, byte* text_end, byte** remaining);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* ImFont_CalcWordWrapPositionA(ImFont* self, float scale, byte* text, byte* text_end,
        float wrap_width);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_ClearOutputData(ImFont* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_destroy(ImFont* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFontGlyph* ImFont_FindGlyph(ImFont* self, ushort c);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFontGlyph* ImFont_FindGlyphNoFallback(ImFont* self, ushort c);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float ImFont_GetCharAdvance(ImFont* self, ushort c);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* ImFont_GetDebugName(ImFont* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_GrowIndex(ImFont* self, int new_size);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFont* ImFont_ImFont();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImFont_IsGlyphRangeUnused(ImFont* self, uint c_begin, uint c_last);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImFont_IsLoaded(ImFont* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_RenderChar(ImFont* self, ImDrawList* draw_list, float size, Vector2 pos, uint col,
        ushort c);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_RenderText(ImFont* self, ImDrawList* draw_list, float size, Vector2 pos, uint col,
        Vector4 clip_rect, byte* text_begin, byte* text_end, float wrap_width, byte cpu_fine_clip);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFont_SetGlyphVisible(ImFont* self, ushort c, byte visible);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ImFontAtlas_AddCustomRectFontGlyph(ImFontAtlas* self, ImFont* font, ushort id, int width,
        int height, float advance_x, Vector2 offset);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ImFontAtlas_AddCustomRectRegular(ImFontAtlas* self, int width, int height);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFont* ImFontAtlas_AddFont(ImFontAtlas* self, ImFontConfig* font_cfg);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFont* ImFontAtlas_AddFontDefault(ImFontAtlas* self, ImFontConfig* font_cfg);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFont* ImFontAtlas_AddFontFromFileTTF(ImFontAtlas* self, byte* filename, float size_pixels,
        ImFontConfig* font_cfg, ushort* glyph_ranges);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFont* ImFontAtlas_AddFontFromMemoryCompressedBase85TTF(ImFontAtlas* self,
        byte* compressed_font_data_base85, float size_pixels, ImFontConfig* font_cfg, ushort* glyph_ranges);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFont* ImFontAtlas_AddFontFromMemoryCompressedTTF(ImFontAtlas* self,
        void* compressed_font_data, int compressed_font_data_size, float size_pixels, ImFontConfig* font_cfg,
        ushort* glyph_ranges);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFont* ImFontAtlas_AddFontFromMemoryTTF(ImFontAtlas* self, void* font_data,
        int font_data_size, float size_pixels, ImFontConfig* font_cfg, ushort* glyph_ranges);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImFontAtlas_Build(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_CalcCustomRectUV(ImFontAtlas* self, ImFontAtlasCustomRect* rect,
        Vector2* out_uv_min, Vector2* out_uv_max);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_Clear(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_ClearFonts(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_ClearInputData(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_ClearTexData(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_destroy(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFontAtlasCustomRect* ImFontAtlas_GetCustomRectByIndex(ImFontAtlas* self, int index);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort* ImFontAtlas_GetGlyphRangesChineseFull(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort* ImFontAtlas_GetGlyphRangesChineseSimplifiedCommon(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort* ImFontAtlas_GetGlyphRangesCyrillic(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort* ImFontAtlas_GetGlyphRangesDefault(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort* ImFontAtlas_GetGlyphRangesGreek(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort* ImFontAtlas_GetGlyphRangesJapanese(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort* ImFontAtlas_GetGlyphRangesKorean(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort* ImFontAtlas_GetGlyphRangesThai(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ushort* ImFontAtlas_GetGlyphRangesVietnamese(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImFontAtlas_GetMouseCursorTexData(ImFontAtlas* self, ImGuiMouseCursor cursor,
        Vector2* out_offset, Vector2* out_size, Vector2* out_uv_border, Vector2* out_uv_fill);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_GetTexDataAsAlpha8(ImFontAtlas* self, byte** out_pixels, int* out_width,
        int* out_height, int* out_bytes_per_pixel);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_GetTexDataAsAlpha8(ImFontAtlas* self, IntPtr* out_pixels, int* out_width,
        int* out_height, int* out_bytes_per_pixel);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_GetTexDataAsRGBA32(ImFontAtlas* self, byte** out_pixels, int* out_width,
        int* out_height, int* out_bytes_per_pixel);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_GetTexDataAsRGBA32(ImFontAtlas* self, IntPtr* out_pixels, int* out_width,
        int* out_height, int* out_bytes_per_pixel);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFontAtlas* ImFontAtlas_ImFontAtlas();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImFontAtlas_IsBuilt(ImFontAtlas* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlas_SetTexID(ImFontAtlas* self, IntPtr id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontAtlasCustomRect_destroy(ImFontAtlasCustomRect* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFontAtlasCustomRect* ImFontAtlasCustomRect_ImFontAtlasCustomRect();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImFontAtlasCustomRect_IsPacked(ImFontAtlasCustomRect* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontConfig_destroy(ImFontConfig* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFontConfig* ImFontConfig_ImFontConfig();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontGlyphRangesBuilder_AddChar(ImFontGlyphRangesBuilder* self, ushort c);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontGlyphRangesBuilder_AddRanges(ImFontGlyphRangesBuilder* self, ushort* ranges);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontGlyphRangesBuilder_AddText(ImFontGlyphRangesBuilder* self, byte* text,
        byte* text_end);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void
        ImFontGlyphRangesBuilder_BuildRanges(ImFontGlyphRangesBuilder* self, ImVector* out_ranges);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontGlyphRangesBuilder_Clear(ImFontGlyphRangesBuilder* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontGlyphRangesBuilder_destroy(ImFontGlyphRangesBuilder* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImFontGlyphRangesBuilder_GetBit(ImFontGlyphRangesBuilder* self, uint n);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImFontGlyphRangesBuilder* ImFontGlyphRangesBuilder_ImFontGlyphRangesBuilder();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImFontGlyphRangesBuilder_SetBit(ImFontGlyphRangesBuilder* self, uint n);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr* ImGuiFreeType_GetBuilderForFreeType();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiInputTextCallbackData_ClearSelection(ImGuiInputTextCallbackData* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiInputTextCallbackData_DeleteChars(ImGuiInputTextCallbackData* self, int pos,
        int bytes_count);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiInputTextCallbackData_destroy(ImGuiInputTextCallbackData* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiInputTextCallbackData_HasSelection(ImGuiInputTextCallbackData* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiInputTextCallbackData* ImGuiInputTextCallbackData_ImGuiInputTextCallbackData();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiInputTextCallbackData_InsertChars(ImGuiInputTextCallbackData* self, int pos,
        byte* text, byte* text_end);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiInputTextCallbackData_SelectAll(ImGuiInputTextCallbackData* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddFocusEvent(ImGuiIO* self, byte focused);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddInputCharacter(ImGuiIO* self, uint c);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddInputCharactersUTF8(ImGuiIO* self, byte* str);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddInputCharacterUTF16(ImGuiIO* self, ushort c);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddKeyAnalogEvent(ImGuiIO* self, ImGuiKey key, byte down, float v);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddKeyEvent(ImGuiIO* self, ImGuiKey key, byte down);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddMouseButtonEvent(ImGuiIO* self, int button, byte down);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddMousePosEvent(ImGuiIO* self, float x, float y);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddMouseSourceEvent(ImGuiIO* self, ImGuiMouseSource source);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_AddMouseWheelEvent(ImGuiIO* self, float wheel_x, float wheel_y);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_ClearEventsQueue(ImGuiIO* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_ClearInputKeys(ImGuiIO* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_ClearInputMouse(ImGuiIO* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_destroy(ImGuiIO* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiIO* ImGuiIO_ImGuiIO();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_SetAppAcceptingEvents(ImGuiIO* self, byte accepting_events);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiIO_SetKeyEventNativeData(ImGuiIO* self, ImGuiKey key, int native_keycode,
        int native_scancode, int native_legacy_index);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiListClipper_Begin(ImGuiListClipper* self, int items_count, float items_height);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiListClipper_destroy(ImGuiListClipper* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiListClipper_End(ImGuiListClipper* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiListClipper* ImGuiListClipper_ImGuiListClipper();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiListClipper_IncludeItemByIndex(ImGuiListClipper* self, int item_index);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void
        ImGuiListClipper_IncludeItemsByIndex(ImGuiListClipper* self, int item_begin, int item_end);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiListClipper_SeekCursorForItem(ImGuiListClipper* self, int item_index);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiListClipper_Step(ImGuiListClipper* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiOnceUponAFrame_destroy(ImGuiOnceUponAFrame* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiOnceUponAFrame* ImGuiOnceUponAFrame_ImGuiOnceUponAFrame();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiPayload_Clear(ImGuiPayload* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiPayload_destroy(ImGuiPayload* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiPayload* ImGuiPayload_ImGuiPayload();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiPayload_IsDataType(ImGuiPayload* self, byte* type);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiPayload_IsDelivery(ImGuiPayload* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiPayload_IsPreview(ImGuiPayload* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiPlatformImeData_destroy(ImGuiPlatformImeData* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiPlatformImeData* ImGuiPlatformImeData_ImGuiPlatformImeData();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiPlatformIO_destroy(ImGuiPlatformIO* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiPlatformIO* ImGuiPlatformIO_ImGuiPlatformIO();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiSelectionBasicStorage_ApplyRequests(ImGuiSelectionBasicStorage* self,
        ImGuiMultiSelectIO* ms_io);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiSelectionBasicStorage_Clear(ImGuiSelectionBasicStorage* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiSelectionBasicStorage_Contains(ImGuiSelectionBasicStorage* self, uint id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiSelectionBasicStorage_destroy(ImGuiSelectionBasicStorage* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiSelectionBasicStorage_GetNextSelectedItem(ImGuiSelectionBasicStorage* self,
        void** opaque_it, uint* out_id);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ImGuiSelectionBasicStorage_GetStorageIdFromIndex(ImGuiSelectionBasicStorage* self,
        int idx);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiSelectionBasicStorage* ImGuiSelectionBasicStorage_ImGuiSelectionBasicStorage();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiSelectionBasicStorage_SetItemSelected(ImGuiSelectionBasicStorage* self, uint id,
        byte selected);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiSelectionBasicStorage_Swap(ImGuiSelectionBasicStorage* self,
        ImGuiSelectionBasicStorage* r);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiSelectionExternalStorage_ApplyRequests(ImGuiSelectionExternalStorage* self, ImGuiMultiSelectIO* ms_io);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiSelectionExternalStorage_destroy(ImGuiSelectionExternalStorage* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiSelectionExternalStorage* ImGuiSelectionExternalStorage_ImGuiSelectionExternalStorage();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStorage_BuildSortByKey(ImGuiStorage* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStorage_Clear(ImGuiStorage* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiStorage_GetBool(ImGuiStorage* self, uint key, byte default_val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* ImGuiStorage_GetBoolRef(ImGuiStorage* self, uint key, byte default_val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float ImGuiStorage_GetFloat(ImGuiStorage* self, uint key, float default_val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern float* ImGuiStorage_GetFloatRef(ImGuiStorage* self, uint key, float default_val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ImGuiStorage_GetInt(ImGuiStorage* self, uint key, int default_val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int* ImGuiStorage_GetIntRef(ImGuiStorage* self, uint key, int default_val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void* ImGuiStorage_GetVoidPtr(ImGuiStorage* self, uint key);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void** ImGuiStorage_GetVoidPtrRef(ImGuiStorage* self, uint key, void* default_val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStorage_SetAllInt(ImGuiStorage* self, int val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStorage_SetBool(ImGuiStorage* self, uint key, byte val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStorage_SetFloat(ImGuiStorage* self, uint key, float val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStorage_SetInt(ImGuiStorage* self, uint key, int val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStorage_SetVoidPtr(ImGuiStorage* self, uint key, void* val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStoragePair_destroy(ImGuiStoragePair* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiStoragePair* ImGuiStoragePair_ImGuiStoragePair_Int(uint _key, int _val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiStoragePair* ImGuiStoragePair_ImGuiStoragePair_Float(uint _key, float _val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiStoragePair* ImGuiStoragePair_ImGuiStoragePair_Ptr(uint _key, void* _val);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStyle_destroy(ImGuiStyle* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiStyle* ImGuiStyle_ImGuiStyle();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiStyle_ScaleAllSizes(ImGuiStyle* self, float scale_factor);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTableColumnSortSpecs_destroy(ImGuiTableColumnSortSpecs* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiTableColumnSortSpecs* ImGuiTableColumnSortSpecs_ImGuiTableColumnSortSpecs();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTableSortSpecs_destroy(ImGuiTableSortSpecs* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiTableSortSpecs* ImGuiTableSortSpecs_ImGuiTableSortSpecs();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextBuffer_append(ImGuiTextBuffer* self, byte* str, byte* str_end);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextBuffer_appendf(ImGuiTextBuffer* self, byte* fmt);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* ImGuiTextBuffer_begin(ImGuiTextBuffer* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* ImGuiTextBuffer_c_str(ImGuiTextBuffer* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextBuffer_clear(ImGuiTextBuffer* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextBuffer_destroy(ImGuiTextBuffer* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiTextBuffer_empty(ImGuiTextBuffer* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte* ImGuiTextBuffer_end(ImGuiTextBuffer* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiTextBuffer* ImGuiTextBuffer_ImGuiTextBuffer();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextBuffer_reserve(ImGuiTextBuffer* self, int capacity);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ImGuiTextBuffer_size(ImGuiTextBuffer* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextFilter_Build(ImGuiTextFilter* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextFilter_Clear(ImGuiTextFilter* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextFilter_destroy(ImGuiTextFilter* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiTextFilter_Draw(ImGuiTextFilter* self, byte* label, float width);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiTextFilter* ImGuiTextFilter_ImGuiTextFilter(byte* default_filter);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiTextFilter_IsActive(ImGuiTextFilter* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiTextFilter_PassFilter(ImGuiTextFilter* self, byte* text, byte* text_end);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextRange_destroy(ImGuiTextRange* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern byte ImGuiTextRange_empty(ImGuiTextRange* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiTextRange* ImGuiTextRange_ImGuiTextRange_Nil();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiTextRange* ImGuiTextRange_ImGuiTextRange_Str(byte* _b, byte* _e);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiTextRange_split(ImGuiTextRange* self, byte separator, ImVector* @out);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiViewport_destroy(ImGuiViewport* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiViewport_GetCenter(Vector2* pOut, ImGuiViewport* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImGuiViewport_GetWorkCenter(Vector2* pOut, ImGuiViewport* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern ImGuiViewport* ImGuiViewport_ImGuiViewport();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImVec2_destroy(Vector2* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector2* ImVec2_ImVec2_Nil();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector2* ImVec2_ImVec2_Float(float _x, float _y);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern void ImVec4_destroy(Vector4* self);

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector4* ImVec4_ImVec4_Nil();

    [DllImport(LIBCIMGUI, CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector4* ImVec4_ImVec4_Float(float _x, float _y, float _z, float _w);
}