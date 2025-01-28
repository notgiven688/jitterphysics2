using System;
using System.Runtime.CompilerServices;
using JitterDemo.Renderer.OpenGL;

namespace JitterDemo.Renderer.DearImGui;

public unsafe delegate int ImGuiInputTextCallback(ImGuiInputTextCallbackData* data);

public unsafe delegate void ImGuiSizeCallback(ImGuiSizeCallbackData* data);

public unsafe struct ImGuiStoragePair
{
    public uint key;
    public int val_i;
    public float val_f;
    public void* val_p;
}

public unsafe struct ImVector
{
    public readonly int Size;
    public readonly int Capacity;
    public readonly IntPtr Data;

    public ImVector(int size, int capacity, IntPtr data)
    {
        Size = size;
        Capacity = capacity;
        Data = data;
    }

    public ref T Ref<T>(int index)
    {
        return ref Unsafe.AsRef<T>((byte*)Data + index * Unsafe.SizeOf<T>());
    }

    public IntPtr Address<T>(int index)
    {
        return (IntPtr)((byte*)Data + index * Unsafe.SizeOf<T>());
    }
}
[System.Flags]
public enum ImDrawFlags
{
    None = 0,
    Closed = 1,
    RoundCornersTopLeft = 16,
    RoundCornersTopRight = 32,
    RoundCornersBottomLeft = 64,
    RoundCornersBottomRight = 128,
    RoundCornersNone = 256,
    RoundCornersTop = 48,
    RoundCornersBottom = 192,
    RoundCornersLeft = 80,
    RoundCornersRight = 160,
    RoundCornersAll = 240,
    RoundCornersDefault = 240,
    RoundCornersMask = 496,
}

[System.Flags]
public enum ImDrawListFlags
{
    None = 0,
    AntiAliasedLines = 1,
    AntiAliasedLinesUseTex = 2,
    AntiAliasedFill = 4,
    AllowVtxOffset = 8,
}

[System.Flags]
public enum ImFontAtlasFlags
{
    None = 0,
    NoPowerOfTwoHeight = 1,
    NoMouseCursors = 2,
    NoBakedLines = 4,
}

[System.Flags]
public enum ImGuiBackendFlags
{
    None = 0,
    HasGamepad = 1,
    HasMouseCursors = 2,
    HasSetMousePos = 4,
    RendererHasVtxOffset = 8,
}

[System.Flags]
public enum ImGuiButtonFlags
{
    None = 0,
    MouseButtonLeft = 1,
    MouseButtonRight = 2,
    MouseButtonMiddle = 4,
    MouseButtonMask = 7,
    EnableNav = 8,
}

[System.Flags]
public enum ImGuiChildFlags
{
    None = 0,
    Borders = 1,
    AlwaysUseWindowPadding = 2,
    ResizeX = 4,
    ResizeY = 8,
    AutoResizeX = 16,
    AutoResizeY = 32,
    AlwaysAutoResize = 64,
    FrameStyle = 128,
    NavFlattened = 256,
}

public enum ImGuiCol
{
    Text = 0,
    TextDisabled = 1,
    WindowBg = 2,
    ChildBg = 3,
    PopupBg = 4,
    Border = 5,
    BorderShadow = 6,
    FrameBg = 7,
    FrameBgHovered = 8,
    FrameBgActive = 9,
    TitleBg = 10,
    TitleBgActive = 11,
    TitleBgCollapsed = 12,
    MenuBarBg = 13,
    ScrollbarBg = 14,
    ScrollbarGrab = 15,
    ScrollbarGrabHovered = 16,
    ScrollbarGrabActive = 17,
    CheckMark = 18,
    SliderGrab = 19,
    SliderGrabActive = 20,
    Button = 21,
    ButtonHovered = 22,
    ButtonActive = 23,
    Header = 24,
    HeaderHovered = 25,
    HeaderActive = 26,
    Separator = 27,
    SeparatorHovered = 28,
    SeparatorActive = 29,
    ResizeGrip = 30,
    ResizeGripHovered = 31,
    ResizeGripActive = 32,
    TabHovered = 33,
    Tab = 34,
    TabSelected = 35,
    TabSelectedOverline = 36,
    TabDimmed = 37,
    TabDimmedSelected = 38,
    TabDimmedSelectedOverline = 39,
    PlotLines = 40,
    PlotLinesHovered = 41,
    PlotHistogram = 42,
    PlotHistogramHovered = 43,
    TableHeaderBg = 44,
    TableBorderStrong = 45,
    TableBorderLight = 46,
    TableRowBg = 47,
    TableRowBgAlt = 48,
    TextLink = 49,
    TextSelectedBg = 50,
    DragDropTarget = 51,
    NavCursor = 52,
    NavWindowingHighlight = 53,
    NavWindowingDimBg = 54,
    ModalWindowDimBg = 55,
    COUNT = 56,
}

[System.Flags]
public enum ImGuiColorEditFlags
{
    None = 0,
    NoAlpha = 2,
    NoPicker = 4,
    NoOptions = 8,
    NoSmallPreview = 16,
    NoInputs = 32,
    NoTooltip = 64,
    NoLabel = 128,
    NoSidePreview = 256,
    NoDragDrop = 512,
    NoBorder = 1024,
    AlphaBar = 65536,
    AlphaPreview = 131072,
    AlphaPreviewHalf = 262144,
    HDR = 524288,
    DisplayRGB = 1048576,
    DisplayHSV = 2097152,
    DisplayHex = 4194304,
    Uint8 = 8388608,
    Float = 16777216,
    PickerHueBar = 33554432,
    PickerHueWheel = 67108864,
    InputRGB = 134217728,
    InputHSV = 268435456,
    DefaultOptions = 177209344,
    DisplayMask = 7340032,
    DataTypeMask = 25165824,
    PickerMask = 100663296,
    InputMask = 402653184,
}

[System.Flags]
public enum ImGuiComboFlags
{
    None = 0,
    PopupAlignLeft = 1,
    HeightSmall = 2,
    HeightRegular = 4,
    HeightLarge = 8,
    HeightLargest = 16,
    NoArrowButton = 32,
    NoPreview = 64,
    WidthFitPreview = 128,
    HeightMask = 30,
}

public enum ImGuiCond
{
    None = 0,
    Always = 1,
    Once = 2,
    FirstUseEver = 4,
    Appearing = 8,
}

[System.Flags]
public enum ImGuiConfigFlags
{
    None = 0,
    NavEnableKeyboard = 1,
    NavEnableGamepad = 2,
    NoMouse = 16,
    NoMouseCursorChange = 32,
    NoKeyboard = 64,
    IsSRGB = 1048576,
    IsTouchScreen = 2097152,
}

public enum ImGuiDataType
{
    S8 = 0,
    U8 = 1,
    S16 = 2,
    U16 = 3,
    S32 = 4,
    U32 = 5,
    S64 = 6,
    U64 = 7,
    Float = 8,
    Double = 9,
    Bool = 10,
    String = 11,
    COUNT = 12,
}

public enum ImGuiDir
{
    None = -1,
    Left = 0,
    Right = 1,
    Up = 2,
    Down = 3,
    COUNT = 4,
}

[System.Flags]
public enum ImGuiDragDropFlags
{
    None = 0,
    SourceNoPreviewTooltip = 1,
    SourceNoDisableHover = 2,
    SourceNoHoldToOpenOthers = 4,
    SourceAllowNullID = 8,
    SourceExtern = 16,
    PayloadAutoExpire = 32,
    PayloadNoCrossContext = 64,
    PayloadNoCrossProcess = 128,
    AcceptBeforeDelivery = 1024,
    AcceptNoDrawDefaultRect = 2048,
    AcceptNoPreviewTooltip = 4096,
    AcceptPeekOnly = 3072,
}

[System.Flags]
public enum ImGuiFocusedFlags
{
    None = 0,
    ChildWindows = 1,
    RootWindow = 2,
    AnyWindow = 4,
    NoPopupHierarchy = 8,
    RootAndChildWindows = 3,
}

[System.Flags]
public enum ImGuiFreeTypeBuilderFlags
{
    NoHinting = 1,
    NoAutoHint = 2,
    ForceAutoHint = 4,
    LightHinting = 8,
    MonoHinting = 16,
    Bold = 32,
    Oblique = 64,
    Monochrome = 128,
    LoadColor = 256,
    Bitmap = 512,
}

[System.Flags]
public enum ImGuiHoveredFlags
{
    None = 0,
    ChildWindows = 1,
    RootWindow = 2,
    AnyWindow = 4,
    NoPopupHierarchy = 8,
    AllowWhenBlockedByPopup = 32,
    AllowWhenBlockedByActiveItem = 128,
    AllowWhenOverlappedByItem = 256,
    AllowWhenOverlappedByWindow = 512,
    AllowWhenDisabled = 1024,
    NoNavOverride = 2048,
    AllowWhenOverlapped = 768,
    RectOnly = 928,
    RootAndChildWindows = 3,
    ForTooltip = 4096,
    Stationary = 8192,
    DelayNone = 16384,
    DelayShort = 32768,
    DelayNormal = 65536,
    NoSharedDelay = 131072,
}

[System.Flags]
public enum ImGuiInputFlags
{
    None = 0,
    Repeat = 1,
    RouteActive = 1024,
    RouteFocused = 2048,
    RouteGlobal = 4096,
    RouteAlways = 8192,
    RouteOverFocused = 16384,
    RouteOverActive = 32768,
    RouteUnlessBgFocused = 65536,
    RouteFromRootWindow = 131072,
    Tooltip = 262144,
}

[System.Flags]
public enum ImGuiInputTextFlags
{
    None = 0,
    CharsDecimal = 1,
    CharsHexadecimal = 2,
    CharsScientific = 4,
    CharsUppercase = 8,
    CharsNoBlank = 16,
    AllowTabInput = 32,
    EnterReturnsTrue = 64,
    EscapeClearsAll = 128,
    CtrlEnterForNewLine = 256,
    ReadOnly = 512,
    Password = 1024,
    AlwaysOverwrite = 2048,
    AutoSelectAll = 4096,
    ParseEmptyRefVal = 8192,
    DisplayEmptyRefVal = 16384,
    NoHorizontalScroll = 32768,
    NoUndoRedo = 65536,
    ElideLeft = 131072,
    CallbackCompletion = 262144,
    CallbackHistory = 524288,
    CallbackAlways = 1048576,
    CallbackCharFilter = 2097152,
    CallbackResize = 4194304,
    CallbackEdit = 8388608,
}

[System.Flags]
public enum ImGuiItemFlags
{
    None = 0,
    NoTabStop = 1,
    NoNav = 2,
    NoNavDefaultFocus = 4,
    ButtonRepeat = 8,
    AutoClosePopups = 16,
    AllowDuplicateId = 32,
}

public enum ImGuiKey
{
    None = 0,
    NamedKey_BEGIN = 512,
    Tab = 512,
    LeftArrow = 513,
    RightArrow = 514,
    UpArrow = 515,
    DownArrow = 516,
    PageUp = 517,
    PageDown = 518,
    Home = 519,
    End = 520,
    Insert = 521,
    Delete = 522,
    Backspace = 523,
    Space = 524,
    Enter = 525,
    Escape = 526,
    LeftCtrl = 527,
    LeftShift = 528,
    LeftAlt = 529,
    LeftSuper = 530,
    RightCtrl = 531,
    RightShift = 532,
    RightAlt = 533,
    RightSuper = 534,
    Menu = 535,
    _0 = 536,
    _1 = 537,
    _2 = 538,
    _3 = 539,
    _4 = 540,
    _5 = 541,
    _6 = 542,
    _7 = 543,
    _8 = 544,
    _9 = 545,
    A = 546,
    B = 547,
    C = 548,
    D = 549,
    E = 550,
    F = 551,
    G = 552,
    H = 553,
    I = 554,
    J = 555,
    K = 556,
    L = 557,
    M = 558,
    N = 559,
    O = 560,
    P = 561,
    Q = 562,
    R = 563,
    S = 564,
    T = 565,
    U = 566,
    V = 567,
    W = 568,
    X = 569,
    Y = 570,
    Z = 571,
    F1 = 572,
    F2 = 573,
    F3 = 574,
    F4 = 575,
    F5 = 576,
    F6 = 577,
    F7 = 578,
    F8 = 579,
    F9 = 580,
    F10 = 581,
    F11 = 582,
    F12 = 583,
    F13 = 584,
    F14 = 585,
    F15 = 586,
    F16 = 587,
    F17 = 588,
    F18 = 589,
    F19 = 590,
    F20 = 591,
    F21 = 592,
    F22 = 593,
    F23 = 594,
    F24 = 595,
    Apostrophe = 596,
    Comma = 597,
    Minus = 598,
    Period = 599,
    Slash = 600,
    Semicolon = 601,
    Equal = 602,
    LeftBracket = 603,
    Backslash = 604,
    RightBracket = 605,
    GraveAccent = 606,
    CapsLock = 607,
    ScrollLock = 608,
    NumLock = 609,
    PrintScreen = 610,
    Pause = 611,
    Keypad0 = 612,
    Keypad1 = 613,
    Keypad2 = 614,
    Keypad3 = 615,
    Keypad4 = 616,
    Keypad5 = 617,
    Keypad6 = 618,
    Keypad7 = 619,
    Keypad8 = 620,
    Keypad9 = 621,
    KeypadDecimal = 622,
    KeypadDivide = 623,
    KeypadMultiply = 624,
    KeypadSubtract = 625,
    KeypadAdd = 626,
    KeypadEnter = 627,
    KeypadEqual = 628,
    AppBack = 629,
    AppForward = 630,
    GamepadStart = 631,
    GamepadBack = 632,
    GamepadFaceLeft = 633,
    GamepadFaceRight = 634,
    GamepadFaceUp = 635,
    GamepadFaceDown = 636,
    GamepadDpadLeft = 637,
    GamepadDpadRight = 638,
    GamepadDpadUp = 639,
    GamepadDpadDown = 640,
    GamepadL1 = 641,
    GamepadR1 = 642,
    GamepadL2 = 643,
    GamepadR2 = 644,
    GamepadL3 = 645,
    GamepadR3 = 646,
    GamepadLStickLeft = 647,
    GamepadLStickRight = 648,
    GamepadLStickUp = 649,
    GamepadLStickDown = 650,
    GamepadRStickLeft = 651,
    GamepadRStickRight = 652,
    GamepadRStickUp = 653,
    GamepadRStickDown = 654,
    MouseLeft = 655,
    MouseRight = 656,
    MouseMiddle = 657,
    MouseX1 = 658,
    MouseX2 = 659,
    MouseWheelX = 660,
    MouseWheelY = 661,
    ReservedForModCtrl = 662,
    ReservedForModShift = 663,
    ReservedForModAlt = 664,
    ReservedForModSuper = 665,
    NamedKey_END = 666,
    ModNone = 0,
    ModCtrl = 4096,
    ModShift = 8192,
    ModAlt = 16384,
    ModSuper = 32768,
    ModMask = 61440,
    NamedKey_COUNT = 154,
}

public enum ImGuiMouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2,
    COUNT = 5,
}

public enum ImGuiMouseCursor
{
    None = -1,
    Arrow = 0,
    TextInput = 1,
    ResizeAll = 2,
    ResizeNS = 3,
    ResizeEW = 4,
    ResizeNESW = 5,
    ResizeNWSE = 6,
    Hand = 7,
    NotAllowed = 8,
    COUNT = 9,
}

public enum ImGuiMouseSource
{
    Mouse = 0,
    TouchScreen = 1,
    Pen = 2,
    COUNT = 3,
}

[System.Flags]
public enum ImGuiMultiSelectFlags
{
    None = 0,
    SingleSelect = 1,
    NoSelectAll = 2,
    NoRangeSelect = 4,
    NoAutoSelect = 8,
    NoAutoClear = 16,
    NoAutoClearOnReselect = 32,
    BoxSelect1d = 64,
    BoxSelect2d = 128,
    BoxSelectNoScroll = 256,
    ClearOnEscape = 512,
    ClearOnClickVoid = 1024,
    ScopeWindow = 2048,
    ScopeRect = 4096,
    SelectOnClick = 8192,
    SelectOnClickRelease = 16384,
    NavWrapX = 65536,
}

[System.Flags]
public enum ImGuiPopupFlags
{
    None = 0,
    MouseButtonLeft = 0,
    MouseButtonRight = 1,
    MouseButtonMiddle = 2,
    MouseButtonMask = 31,
    MouseButtonDefault = 1,
    NoReopen = 32,
    NoOpenOverExistingPopup = 128,
    NoOpenOverItems = 256,
    AnyPopupId = 1024,
    AnyPopupLevel = 2048,
    AnyPopup = 3072,
}

[System.Flags]
public enum ImGuiSelectableFlags
{
    None = 0,
    NoAutoClosePopups = 1,
    SpanAllColumns = 2,
    AllowDoubleClick = 4,
    Disabled = 8,
    AllowOverlap = 16,
    Highlight = 32,
}

public enum ImGuiSelectionRequestType
{
    None = 0,
    SetAll = 1,
    SetRange = 2,
}

[System.Flags]
public enum ImGuiSliderFlags
{
    None = 0,
    Logarithmic = 32,
    NoRoundToFormat = 64,
    NoInput = 128,
    WrapAround = 256,
    ClampOnInput = 512,
    ClampZeroRange = 1024,
    NoSpeedTweaks = 2048,
    AlwaysClamp = 1536,
    InvalidMask = 1879048207,
}

public enum ImGuiSortDirection
{
    None = 0,
    Ascending = 1,
    Descending = 2,
}

public enum ImGuiStyleVar
{
    Alpha = 0,
    DisabledAlpha = 1,
    WindowPadding = 2,
    WindowRounding = 3,
    WindowBorderSize = 4,
    WindowMinSize = 5,
    WindowTitleAlign = 6,
    ChildRounding = 7,
    ChildBorderSize = 8,
    PopupRounding = 9,
    PopupBorderSize = 10,
    FramePadding = 11,
    FrameRounding = 12,
    FrameBorderSize = 13,
    ItemSpacing = 14,
    ItemInnerSpacing = 15,
    IndentSpacing = 16,
    CellPadding = 17,
    ScrollbarSize = 18,
    ScrollbarRounding = 19,
    GrabMinSize = 20,
    GrabRounding = 21,
    TabRounding = 22,
    TabBorderSize = 23,
    TabBarBorderSize = 24,
    TabBarOverlineSize = 25,
    TableAngledHeadersAngle = 26,
    TableAngledHeadersTextAlign = 27,
    ButtonTextAlign = 28,
    SelectableTextAlign = 29,
    SeparatorTextBorderSize = 30,
    SeparatorTextAlign = 31,
    SeparatorTextPadding = 32,
    COUNT = 33,
}

[System.Flags]
public enum ImGuiTabBarFlags
{
    None = 0,
    Reorderable = 1,
    AutoSelectNewTabs = 2,
    TabListPopupButton = 4,
    NoCloseWithMiddleMouseButton = 8,
    NoTabListScrollingButtons = 16,
    NoTooltip = 32,
    DrawSelectedOverline = 64,
    FittingPolicyResizeDown = 128,
    FittingPolicyScroll = 256,
    FittingPolicyMask = 384,
    FittingPolicyDefault = 128,
}

[System.Flags]
public enum ImGuiTabItemFlags
{
    None = 0,
    UnsavedDocument = 1,
    SetSelected = 2,
    NoCloseWithMiddleMouseButton = 4,
    NoPushId = 8,
    NoTooltip = 16,
    NoReorder = 32,
    Leading = 64,
    Trailing = 128,
    NoAssumedClosure = 256,
}

public enum ImGuiTableBgTarget
{
    None = 0,
    RowBg0 = 1,
    RowBg1 = 2,
    CellBg = 3,
}

[System.Flags]
public enum ImGuiTableColumnFlags
{
    None = 0,
    Disabled = 1,
    DefaultHide = 2,
    DefaultSort = 4,
    WidthStretch = 8,
    WidthFixed = 16,
    NoResize = 32,
    NoReorder = 64,
    NoHide = 128,
    NoClip = 256,
    NoSort = 512,
    NoSortAscending = 1024,
    NoSortDescending = 2048,
    NoHeaderLabel = 4096,
    NoHeaderWidth = 8192,
    PreferSortAscending = 16384,
    PreferSortDescending = 32768,
    IndentEnable = 65536,
    IndentDisable = 131072,
    AngledHeader = 262144,
    IsEnabled = 16777216,
    IsVisible = 33554432,
    IsSorted = 67108864,
    IsHovered = 134217728,
    WidthMask = 24,
    IndentMask = 196608,
    StatusMask = 251658240,
    NoDirectResize = 1073741824,
}

[System.Flags]
public enum ImGuiTableFlags
{
    None = 0,
    Resizable = 1,
    Reorderable = 2,
    Hideable = 4,
    Sortable = 8,
    NoSavedSettings = 16,
    ContextMenuInBody = 32,
    RowBg = 64,
    BordersInnerH = 128,
    BordersOuterH = 256,
    BordersInnerV = 512,
    BordersOuterV = 1024,
    BordersH = 384,
    BordersV = 1536,
    BordersInner = 640,
    BordersOuter = 1280,
    Borders = 1920,
    NoBordersInBody = 2048,
    NoBordersInBodyUntilResize = 4096,
    SizingFixedFit = 8192,
    SizingFixedSame = 16384,
    SizingStretchProp = 24576,
    SizingStretchSame = 32768,
    NoHostExtendX = 65536,
    NoHostExtendY = 131072,
    NoKeepColumnsVisible = 262144,
    PreciseWidths = 524288,
    NoClip = 1048576,
    PadOuterX = 2097152,
    NoPadOuterX = 4194304,
    NoPadInnerX = 8388608,
    ScrollX = 16777216,
    ScrollY = 33554432,
    SortMulti = 67108864,
    SortTristate = 134217728,
    HighlightHoveredColumn = 268435456,
    SizingMask = 57344,
}

[System.Flags]
public enum ImGuiTableRowFlags
{
    None = 0,
    Headers = 1,
}

[System.Flags]
public enum ImGuiTreeNodeFlags
{
    None = 0,
    Selected = 1,
    Framed = 2,
    AllowOverlap = 4,
    NoTreePushOnOpen = 8,
    NoAutoOpenOnLog = 16,
    DefaultOpen = 32,
    OpenOnDoubleClick = 64,
    OpenOnArrow = 128,
    Leaf = 256,
    Bullet = 512,
    FramePadding = 1024,
    SpanAvailWidth = 2048,
    SpanFullWidth = 4096,
    SpanLabelWidth = 8192,
    SpanAllColumns = 16384,
    LabelSpanAllColumns = 32768,
    NavLeftJumpsBackHere = 131072,
    CollapsingHeader = 26,
}

[System.Flags]
public enum ImGuiViewportFlags
{
    None = 0,
    IsPlatformWindow = 1,
    IsPlatformMonitor = 2,
    OwnedByApp = 4,
}

[System.Flags]
public enum ImGuiWindowFlags
{
    None = 0,
    NoTitleBar = 1,
    NoResize = 2,
    NoMove = 4,
    NoScrollbar = 8,
    NoScrollWithMouse = 16,
    NoCollapse = 32,
    AlwaysAutoResize = 64,
    NoBackground = 128,
    NoSavedSettings = 256,
    NoMouseInputs = 512,
    MenuBar = 1024,
    HorizontalScrollbar = 2048,
    NoFocusOnAppearing = 4096,
    NoBringToFrontOnFocus = 8192,
    AlwaysVerticalScrollbar = 16384,
    AlwaysHorizontalScrollbar = 32768,
    NoNavInputs = 65536,
    NoNavFocus = 131072,
    UnsavedDocument = 262144,
    NoNav = 196608,
    NoDecoration = 43,
    NoInputs = 197120,
    ChildWindow = 16777216,
    Tooltip = 33554432,
    Popup = 67108864,
    Modal = 134217728,
    ChildMenu = 268435456,
}

public unsafe partial struct ImColor
{
    public Vector4 Value;
}

public unsafe partial struct ImDrawChannel
{
    public ImVector _CmdBuffer;
    public ImVector _IdxBuffer;
}

public unsafe partial struct ImDrawCmd
{
    public Vector4 ClipRect;
    public IntPtr TextureId;
    public uint VtxOffset;
    public uint IdxOffset;
    public uint ElemCount;
    public IntPtr UserCallback;
    public void* UserCallbackData;
    public int UserCallbackDataSize;
    public int UserCallbackDataOffset;
}

public unsafe partial struct ImDrawCmdHeader
{
    public Vector4 ClipRect;
    public IntPtr TextureId;
    public uint VtxOffset;
}

public unsafe partial struct ImDrawData
{
    public byte Valid;
    public int CmdListsCount;
    public int TotalIdxCount;
    public int TotalVtxCount;
    public ImVector CmdLists;
    public Vector2 DisplayPos;
    public Vector2 DisplaySize;
    public Vector2 FramebufferScale;
    public ImGuiViewport* OwnerViewport;
}

public unsafe partial struct ImDrawList
{
    public ImVector CmdBuffer;
    public ImVector IdxBuffer;
    public ImVector VtxBuffer;
    public ImDrawListFlags Flags;
    public uint _VtxCurrentIdx;
    public IntPtr _Data;
    public ImDrawVert* _VtxWritePtr;
    public ushort* _IdxWritePtr;
    public ImVector _Path;
    public ImDrawCmdHeader _CmdHeader;
    public ImDrawListSplitter _Splitter;
    public ImVector _ClipRectStack;
    public ImVector _TextureIdStack;
    public ImVector _CallbacksDataBuf;
    public float _FringeScale;
    public byte* _OwnerName;
}

public unsafe partial struct ImDrawListSplitter
{
    public int _Current;
    public int _Count;
    public ImVector _Channels;
}

public unsafe partial struct ImDrawVert
{
    public Vector2 pos;
    public Vector2 uv;
    public uint col;
}

public unsafe partial struct ImFont
{
    public ImVector IndexAdvanceX;
    public float FallbackAdvanceX;
    public float FontSize;
    public ImVector IndexLookup;
    public ImVector Glyphs;
    public ImFontGlyph* FallbackGlyph;
    public ImFontAtlas* ContainerAtlas;
    public ImFontConfig* ConfigData;
    public short ConfigDataCount;
    public short EllipsisCharCount;
    public ushort EllipsisChar;
    public ushort FallbackChar;
    public float EllipsisWidth;
    public float EllipsisCharStep;
    public byte DirtyLookupTables;
    public float Scale;
    public float Ascent;
    public float Descent;
    public int MetricsTotalSurface;
    public fixed byte Used4kPagesMap[2];
}

public unsafe partial struct ImFontAtlas
{
    public ImFontAtlasFlags Flags;
    public IntPtr TexID;
    public int TexDesiredWidth;
    public int TexGlyphPadding;
    public byte Locked;
    public void* UserData;
    public byte TexReady;
    public byte TexPixelsUseColors;
    public byte* TexPixelsAlpha8;
    public uint* TexPixelsRGBA32;
    public int TexWidth;
    public int TexHeight;
    public Vector2 TexUvScale;
    public Vector2 TexUvWhitePixel;
    public ImVector Fonts;
    public ImVector CustomRects;
    public ImVector ConfigData;
    public Vector4 TexUvLines_0;
    public Vector4 TexUvLines_1;
    public Vector4 TexUvLines_2;
    public Vector4 TexUvLines_3;
    public Vector4 TexUvLines_4;
    public Vector4 TexUvLines_5;
    public Vector4 TexUvLines_6;
    public Vector4 TexUvLines_7;
    public Vector4 TexUvLines_8;
    public Vector4 TexUvLines_9;
    public Vector4 TexUvLines_10;
    public Vector4 TexUvLines_11;
    public Vector4 TexUvLines_12;
    public Vector4 TexUvLines_13;
    public Vector4 TexUvLines_14;
    public Vector4 TexUvLines_15;
    public Vector4 TexUvLines_16;
    public Vector4 TexUvLines_17;
    public Vector4 TexUvLines_18;
    public Vector4 TexUvLines_19;
    public Vector4 TexUvLines_20;
    public Vector4 TexUvLines_21;
    public Vector4 TexUvLines_22;
    public Vector4 TexUvLines_23;
    public Vector4 TexUvLines_24;
    public Vector4 TexUvLines_25;
    public Vector4 TexUvLines_26;
    public Vector4 TexUvLines_27;
    public Vector4 TexUvLines_28;
    public Vector4 TexUvLines_29;
    public Vector4 TexUvLines_30;
    public Vector4 TexUvLines_31;
    public Vector4 TexUvLines_32;
    public Vector4 TexUvLines_33;
    public Vector4 TexUvLines_34;
    public Vector4 TexUvLines_35;
    public Vector4 TexUvLines_36;
    public Vector4 TexUvLines_37;
    public Vector4 TexUvLines_38;
    public Vector4 TexUvLines_39;
    public Vector4 TexUvLines_40;
    public Vector4 TexUvLines_41;
    public Vector4 TexUvLines_42;
    public Vector4 TexUvLines_43;
    public Vector4 TexUvLines_44;
    public Vector4 TexUvLines_45;
    public Vector4 TexUvLines_46;
    public Vector4 TexUvLines_47;
    public Vector4 TexUvLines_48;
    public Vector4 TexUvLines_49;
    public Vector4 TexUvLines_50;
    public Vector4 TexUvLines_51;
    public Vector4 TexUvLines_52;
    public Vector4 TexUvLines_53;
    public Vector4 TexUvLines_54;
    public Vector4 TexUvLines_55;
    public Vector4 TexUvLines_56;
    public Vector4 TexUvLines_57;
    public Vector4 TexUvLines_58;
    public Vector4 TexUvLines_59;
    public Vector4 TexUvLines_60;
    public Vector4 TexUvLines_61;
    public Vector4 TexUvLines_62;
    public Vector4 TexUvLines_63;
    public IntPtr* FontBuilderIO;
    public uint FontBuilderFlags;
    public int PackIdMouseCursors;
    public int PackIdLines;
}

public unsafe partial struct ImFontAtlasCustomRect
{
    public ushort X;
    public ushort Y;
    public ushort Width;
    public ushort Height;
    public uint GlyphID;
    public uint GlyphColored;
    public float GlyphAdvanceX;
    public Vector2 GlyphOffset;
    public ImFont* Font;
}

public unsafe partial struct ImFontConfig
{
    public void* FontData;
    public int FontDataSize;
    public byte FontDataOwnedByAtlas;
    public int FontNo;
    public float SizePixels;
    public int OversampleH;
    public int OversampleV;
    public byte PixelSnapH;
    public Vector2 GlyphExtraSpacing;
    public Vector2 GlyphOffset;
    public ushort* GlyphRanges;
    public float GlyphMinAdvanceX;
    public float GlyphMaxAdvanceX;
    public byte MergeMode;
    public uint FontBuilderFlags;
    public float RasterizerMultiply;
    public float RasterizerDensity;
    public ushort EllipsisChar;
    public fixed byte Name[40];
    public ImFont* DstFont;
}

public unsafe partial struct ImFontGlyph
{
    public uint Colored;
    public uint Visible;
    public uint Codepoint;
    public float AdvanceX;
    public float X0;
    public float Y0;
    public float X1;
    public float Y1;
    public float U0;
    public float V0;
    public float U1;
    public float V1;
}

public unsafe partial struct ImFontGlyphRangesBuilder
{
    public ImVector UsedChars;
}

public unsafe partial struct ImGuiIO
{
    public ImGuiConfigFlags ConfigFlags;
    public ImGuiBackendFlags BackendFlags;
    public Vector2 DisplaySize;
    public float DeltaTime;
    public float IniSavingRate;
    public byte* IniFilename;
    public byte* LogFilename;
    public void* UserData;
    public ImFontAtlas* Fonts;
    public float FontGlobalScale;
    public byte FontAllowUserScaling;
    public ImFont* FontDefault;
    public Vector2 DisplayFramebufferScale;
    public byte ConfigNavSwapGamepadButtons;
    public byte ConfigNavMoveSetMousePos;
    public byte ConfigNavCaptureKeyboard;
    public byte ConfigNavEscapeClearFocusItem;
    public byte ConfigNavEscapeClearFocusWindow;
    public byte ConfigNavCursorVisibleAuto;
    public byte ConfigNavCursorVisibleAlways;
    public byte MouseDrawCursor;
    public byte ConfigMacOSXBehaviors;
    public byte ConfigInputTrickleEventQueue;
    public byte ConfigInputTextCursorBlink;
    public byte ConfigInputTextEnterKeepActive;
    public byte ConfigDragClickToInputText;
    public byte ConfigWindowsResizeFromEdges;
    public byte ConfigWindowsMoveFromTitleBarOnly;
    public byte ConfigWindowsCopyContentsWithCtrlC;
    public byte ConfigScrollbarScrollByPage;
    public float ConfigMemoryCompactTimer;
    public float MouseDoubleClickTime;
    public float MouseDoubleClickMaxDist;
    public float MouseDragThreshold;
    public float KeyRepeatDelay;
    public float KeyRepeatRate;
    public byte ConfigErrorRecovery;
    public byte ConfigErrorRecoveryEnableAssert;
    public byte ConfigErrorRecoveryEnableDebugLog;
    public byte ConfigErrorRecoveryEnableTooltip;
    public byte ConfigDebugIsDebuggerPresent;
    public byte ConfigDebugHighlightIdConflicts;
    public byte ConfigDebugBeginReturnValueOnce;
    public byte ConfigDebugBeginReturnValueLoop;
    public byte ConfigDebugIgnoreFocusLoss;
    public byte ConfigDebugIniSettings;
    public byte* BackendPlatformName;
    public byte* BackendRendererName;
    public void* BackendPlatformUserData;
    public void* BackendRendererUserData;
    public void* BackendLanguageUserData;
    public byte WantCaptureMouse;
    public byte WantCaptureKeyboard;
    public byte WantTextInput;
    public byte WantSetMousePos;
    public byte WantSaveIniSettings;
    public byte NavActive;
    public byte NavVisible;
    public float Framerate;
    public int MetricsRenderVertices;
    public int MetricsRenderIndices;
    public int MetricsRenderWindows;
    public int MetricsActiveWindows;
    public Vector2 MouseDelta;
    public IntPtr Ctx;
    public Vector2 MousePos;
    public fixed byte MouseDown[5];
    public float MouseWheel;
    public float MouseWheelH;
    public ImGuiMouseSource MouseSource;
    public byte KeyCtrl;
    public byte KeyShift;
    public byte KeyAlt;
    public byte KeySuper;
    public ImGuiKey KeyMods;
    public ImGuiKeyData KeysData_0;
    public ImGuiKeyData KeysData_1;
    public ImGuiKeyData KeysData_2;
    public ImGuiKeyData KeysData_3;
    public ImGuiKeyData KeysData_4;
    public ImGuiKeyData KeysData_5;
    public ImGuiKeyData KeysData_6;
    public ImGuiKeyData KeysData_7;
    public ImGuiKeyData KeysData_8;
    public ImGuiKeyData KeysData_9;
    public ImGuiKeyData KeysData_10;
    public ImGuiKeyData KeysData_11;
    public ImGuiKeyData KeysData_12;
    public ImGuiKeyData KeysData_13;
    public ImGuiKeyData KeysData_14;
    public ImGuiKeyData KeysData_15;
    public ImGuiKeyData KeysData_16;
    public ImGuiKeyData KeysData_17;
    public ImGuiKeyData KeysData_18;
    public ImGuiKeyData KeysData_19;
    public ImGuiKeyData KeysData_20;
    public ImGuiKeyData KeysData_21;
    public ImGuiKeyData KeysData_22;
    public ImGuiKeyData KeysData_23;
    public ImGuiKeyData KeysData_24;
    public ImGuiKeyData KeysData_25;
    public ImGuiKeyData KeysData_26;
    public ImGuiKeyData KeysData_27;
    public ImGuiKeyData KeysData_28;
    public ImGuiKeyData KeysData_29;
    public ImGuiKeyData KeysData_30;
    public ImGuiKeyData KeysData_31;
    public ImGuiKeyData KeysData_32;
    public ImGuiKeyData KeysData_33;
    public ImGuiKeyData KeysData_34;
    public ImGuiKeyData KeysData_35;
    public ImGuiKeyData KeysData_36;
    public ImGuiKeyData KeysData_37;
    public ImGuiKeyData KeysData_38;
    public ImGuiKeyData KeysData_39;
    public ImGuiKeyData KeysData_40;
    public ImGuiKeyData KeysData_41;
    public ImGuiKeyData KeysData_42;
    public ImGuiKeyData KeysData_43;
    public ImGuiKeyData KeysData_44;
    public ImGuiKeyData KeysData_45;
    public ImGuiKeyData KeysData_46;
    public ImGuiKeyData KeysData_47;
    public ImGuiKeyData KeysData_48;
    public ImGuiKeyData KeysData_49;
    public ImGuiKeyData KeysData_50;
    public ImGuiKeyData KeysData_51;
    public ImGuiKeyData KeysData_52;
    public ImGuiKeyData KeysData_53;
    public ImGuiKeyData KeysData_54;
    public ImGuiKeyData KeysData_55;
    public ImGuiKeyData KeysData_56;
    public ImGuiKeyData KeysData_57;
    public ImGuiKeyData KeysData_58;
    public ImGuiKeyData KeysData_59;
    public ImGuiKeyData KeysData_60;
    public ImGuiKeyData KeysData_61;
    public ImGuiKeyData KeysData_62;
    public ImGuiKeyData KeysData_63;
    public ImGuiKeyData KeysData_64;
    public ImGuiKeyData KeysData_65;
    public ImGuiKeyData KeysData_66;
    public ImGuiKeyData KeysData_67;
    public ImGuiKeyData KeysData_68;
    public ImGuiKeyData KeysData_69;
    public ImGuiKeyData KeysData_70;
    public ImGuiKeyData KeysData_71;
    public ImGuiKeyData KeysData_72;
    public ImGuiKeyData KeysData_73;
    public ImGuiKeyData KeysData_74;
    public ImGuiKeyData KeysData_75;
    public ImGuiKeyData KeysData_76;
    public ImGuiKeyData KeysData_77;
    public ImGuiKeyData KeysData_78;
    public ImGuiKeyData KeysData_79;
    public ImGuiKeyData KeysData_80;
    public ImGuiKeyData KeysData_81;
    public ImGuiKeyData KeysData_82;
    public ImGuiKeyData KeysData_83;
    public ImGuiKeyData KeysData_84;
    public ImGuiKeyData KeysData_85;
    public ImGuiKeyData KeysData_86;
    public ImGuiKeyData KeysData_87;
    public ImGuiKeyData KeysData_88;
    public ImGuiKeyData KeysData_89;
    public ImGuiKeyData KeysData_90;
    public ImGuiKeyData KeysData_91;
    public ImGuiKeyData KeysData_92;
    public ImGuiKeyData KeysData_93;
    public ImGuiKeyData KeysData_94;
    public ImGuiKeyData KeysData_95;
    public ImGuiKeyData KeysData_96;
    public ImGuiKeyData KeysData_97;
    public ImGuiKeyData KeysData_98;
    public ImGuiKeyData KeysData_99;
    public ImGuiKeyData KeysData_100;
    public ImGuiKeyData KeysData_101;
    public ImGuiKeyData KeysData_102;
    public ImGuiKeyData KeysData_103;
    public ImGuiKeyData KeysData_104;
    public ImGuiKeyData KeysData_105;
    public ImGuiKeyData KeysData_106;
    public ImGuiKeyData KeysData_107;
    public ImGuiKeyData KeysData_108;
    public ImGuiKeyData KeysData_109;
    public ImGuiKeyData KeysData_110;
    public ImGuiKeyData KeysData_111;
    public ImGuiKeyData KeysData_112;
    public ImGuiKeyData KeysData_113;
    public ImGuiKeyData KeysData_114;
    public ImGuiKeyData KeysData_115;
    public ImGuiKeyData KeysData_116;
    public ImGuiKeyData KeysData_117;
    public ImGuiKeyData KeysData_118;
    public ImGuiKeyData KeysData_119;
    public ImGuiKeyData KeysData_120;
    public ImGuiKeyData KeysData_121;
    public ImGuiKeyData KeysData_122;
    public ImGuiKeyData KeysData_123;
    public ImGuiKeyData KeysData_124;
    public ImGuiKeyData KeysData_125;
    public ImGuiKeyData KeysData_126;
    public ImGuiKeyData KeysData_127;
    public ImGuiKeyData KeysData_128;
    public ImGuiKeyData KeysData_129;
    public ImGuiKeyData KeysData_130;
    public ImGuiKeyData KeysData_131;
    public ImGuiKeyData KeysData_132;
    public ImGuiKeyData KeysData_133;
    public ImGuiKeyData KeysData_134;
    public ImGuiKeyData KeysData_135;
    public ImGuiKeyData KeysData_136;
    public ImGuiKeyData KeysData_137;
    public ImGuiKeyData KeysData_138;
    public ImGuiKeyData KeysData_139;
    public ImGuiKeyData KeysData_140;
    public ImGuiKeyData KeysData_141;
    public ImGuiKeyData KeysData_142;
    public ImGuiKeyData KeysData_143;
    public ImGuiKeyData KeysData_144;
    public ImGuiKeyData KeysData_145;
    public ImGuiKeyData KeysData_146;
    public ImGuiKeyData KeysData_147;
    public ImGuiKeyData KeysData_148;
    public ImGuiKeyData KeysData_149;
    public ImGuiKeyData KeysData_150;
    public ImGuiKeyData KeysData_151;
    public ImGuiKeyData KeysData_152;
    public ImGuiKeyData KeysData_153;
    public byte WantCaptureMouseUnlessPopupClose;
    public Vector2 MousePosPrev;
    public Vector2 MouseClickedPos_0;
    public Vector2 MouseClickedPos_1;
    public Vector2 MouseClickedPos_2;
    public Vector2 MouseClickedPos_3;
    public Vector2 MouseClickedPos_4;
    public fixed double MouseClickedTime[5];
    public fixed byte MouseClicked[5];
    public fixed byte MouseDoubleClicked[5];
    public fixed ushort MouseClickedCount[5];
    public fixed ushort MouseClickedLastCount[5];
    public fixed byte MouseReleased[5];
    public fixed byte MouseDownOwned[5];
    public fixed byte MouseDownOwnedUnlessPopupClose[5];
    public byte MouseWheelRequestAxisSwap;
    public byte MouseCtrlLeftAsRightClick;
    public fixed float MouseDownDuration[5];
    public fixed float MouseDownDurationPrev[5];
    public fixed float MouseDragMaxDistanceSqr[5];
    public float PenPressure;
    public byte AppFocusLost;
    public byte AppAcceptingEvents;
    public ushort InputQueueSurrogate;
    public ImVector InputQueueCharacters;
}

public unsafe partial struct ImGuiInputTextCallbackData
{
    public IntPtr Ctx;
    public ImGuiInputTextFlags EventFlag;
    public ImGuiInputTextFlags Flags;
    public void* UserData;
    public ushort EventChar;
    public ImGuiKey EventKey;
    public byte* Buf;
    public int BufTextLen;
    public int BufSize;
    public byte BufDirty;
    public int CursorPos;
    public int SelectionStart;
    public int SelectionEnd;
}

public unsafe partial struct ImGuiKeyData
{
    public byte Down;
    public float DownDuration;
    public float DownDurationPrev;
    public float AnalogValue;
}

public unsafe partial struct ImGuiListClipper
{
    public IntPtr Ctx;
    public int DisplayStart;
    public int DisplayEnd;
    public int ItemsCount;
    public float ItemsHeight;
    public float StartPosY;
    public double StartSeekOffsetY;
    public void* TempData;
}

public unsafe partial struct ImGuiMultiSelectIO
{
    public ImVector Requests;
    public long RangeSrcItem;
    public long NavIdItem;
    public byte NavIdSelected;
    public byte RangeSrcReset;
    public int ItemsCount;
}

public unsafe partial struct ImGuiOnceUponAFrame
{
    public int RefFrame;
}

public unsafe partial struct ImGuiPayload
{
    public void* Data;
    public int DataSize;
    public uint SourceId;
    public uint SourceParentId;
    public int DataFrameCount;
    public fixed byte DataType[33];
    public byte Preview;
    public byte Delivery;
}

public unsafe partial struct ImGuiPlatformIO
{
    public IntPtr Platform_GetClipboardTextFn;
    public IntPtr Platform_SetClipboardTextFn;
    public void* Platform_ClipboardUserData;
    public IntPtr Platform_OpenInShellFn;
    public void* Platform_OpenInShellUserData;
    public IntPtr Platform_SetImeDataFn;
    public void* Platform_ImeUserData;
    public ushort Platform_LocaleDecimalPoint;
    public void* Renderer_RenderState;
}

public unsafe partial struct ImGuiPlatformImeData
{
    public byte WantVisible;
    public Vector2 InputPos;
    public float InputLineHeight;
}

public unsafe partial struct ImGuiSelectionBasicStorage
{
    public int Size;
    public byte PreserveOrder;
    public void* UserData;
    public IntPtr AdapterIndexToStorageId;
    public int _SelectionOrder;
    public ImGuiStorage _Storage;
}

public unsafe partial struct ImGuiSelectionExternalStorage
{
    public void* UserData;
    public IntPtr AdapterSetItemSelected;
}

public unsafe partial struct ImGuiSelectionRequest
{
    public ImGuiSelectionRequestType Type;
    public byte Selected;
    public sbyte RangeDirection;
    public long RangeFirstItem;
    public long RangeLastItem;
}

public unsafe partial struct ImGuiSizeCallbackData
{
    public void* UserData;
    public Vector2 Pos;
    public Vector2 CurrentSize;
    public Vector2 DesiredSize;
}

public unsafe partial struct ImGuiStorage
{
    public ImVector Data;
}

public unsafe partial struct ImGuiStyle
{
    public float Alpha;
    public float DisabledAlpha;
    public Vector2 WindowPadding;
    public float WindowRounding;
    public float WindowBorderSize;
    public Vector2 WindowMinSize;
    public Vector2 WindowTitleAlign;
    public ImGuiDir WindowMenuButtonPosition;
    public float ChildRounding;
    public float ChildBorderSize;
    public float PopupRounding;
    public float PopupBorderSize;
    public Vector2 FramePadding;
    public float FrameRounding;
    public float FrameBorderSize;
    public Vector2 ItemSpacing;
    public Vector2 ItemInnerSpacing;
    public Vector2 CellPadding;
    public Vector2 TouchExtraPadding;
    public float IndentSpacing;
    public float ColumnsMinSpacing;
    public float ScrollbarSize;
    public float ScrollbarRounding;
    public float GrabMinSize;
    public float GrabRounding;
    public float LogSliderDeadzone;
    public float TabRounding;
    public float TabBorderSize;
    public float TabMinWidthForCloseButton;
    public float TabBarBorderSize;
    public float TabBarOverlineSize;
    public float TableAngledHeadersAngle;
    public Vector2 TableAngledHeadersTextAlign;
    public ImGuiDir ColorButtonPosition;
    public Vector2 ButtonTextAlign;
    public Vector2 SelectableTextAlign;
    public float SeparatorTextBorderSize;
    public Vector2 SeparatorTextAlign;
    public Vector2 SeparatorTextPadding;
    public Vector2 DisplayWindowPadding;
    public Vector2 DisplaySafeAreaPadding;
    public float MouseCursorScale;
    public byte AntiAliasedLines;
    public byte AntiAliasedLinesUseTex;
    public byte AntiAliasedFill;
    public float CurveTessellationTol;
    public float CircleTessellationMaxError;
    public Vector4 Colors_0;
    public Vector4 Colors_1;
    public Vector4 Colors_2;
    public Vector4 Colors_3;
    public Vector4 Colors_4;
    public Vector4 Colors_5;
    public Vector4 Colors_6;
    public Vector4 Colors_7;
    public Vector4 Colors_8;
    public Vector4 Colors_9;
    public Vector4 Colors_10;
    public Vector4 Colors_11;
    public Vector4 Colors_12;
    public Vector4 Colors_13;
    public Vector4 Colors_14;
    public Vector4 Colors_15;
    public Vector4 Colors_16;
    public Vector4 Colors_17;
    public Vector4 Colors_18;
    public Vector4 Colors_19;
    public Vector4 Colors_20;
    public Vector4 Colors_21;
    public Vector4 Colors_22;
    public Vector4 Colors_23;
    public Vector4 Colors_24;
    public Vector4 Colors_25;
    public Vector4 Colors_26;
    public Vector4 Colors_27;
    public Vector4 Colors_28;
    public Vector4 Colors_29;
    public Vector4 Colors_30;
    public Vector4 Colors_31;
    public Vector4 Colors_32;
    public Vector4 Colors_33;
    public Vector4 Colors_34;
    public Vector4 Colors_35;
    public Vector4 Colors_36;
    public Vector4 Colors_37;
    public Vector4 Colors_38;
    public Vector4 Colors_39;
    public Vector4 Colors_40;
    public Vector4 Colors_41;
    public Vector4 Colors_42;
    public Vector4 Colors_43;
    public Vector4 Colors_44;
    public Vector4 Colors_45;
    public Vector4 Colors_46;
    public Vector4 Colors_47;
    public Vector4 Colors_48;
    public Vector4 Colors_49;
    public Vector4 Colors_50;
    public Vector4 Colors_51;
    public Vector4 Colors_52;
    public Vector4 Colors_53;
    public Vector4 Colors_54;
    public Vector4 Colors_55;
    public float HoverStationaryDelay;
    public float HoverDelayShort;
    public float HoverDelayNormal;
    public ImGuiHoveredFlags HoverFlagsForTooltipMouse;
    public ImGuiHoveredFlags HoverFlagsForTooltipNav;
}

public unsafe partial struct ImGuiTableColumnSortSpecs
{
    public uint ColumnUserID;
    public short ColumnIndex;
    public short SortOrder;
    public ImGuiSortDirection SortDirection;
}

public unsafe partial struct ImGuiTableSortSpecs
{
    public ImGuiTableColumnSortSpecs* Specs;
    public int SpecsCount;
    public byte SpecsDirty;
}

public unsafe partial struct ImGuiTextBuffer
{
    public ImVector Buf;
}

public unsafe partial struct ImGuiTextFilter
{
    public fixed byte InputBuf[256];
    public ImVector Filters;
    public int CountGrep;
}

public unsafe partial struct ImGuiTextRange
{
    public byte* b;
    public byte* e;
}

public unsafe partial struct ImGuiViewport
{
    public uint ID;
    public ImGuiViewportFlags Flags;
    public Vector2 Pos;
    public Vector2 Size;
    public Vector2 WorkPos;
    public Vector2 WorkSize;
    public void* PlatformHandle;
    public void* PlatformHandleRaw;
}

public unsafe partial struct ImVec2
{
    public float x;
    public float y;
}

public unsafe partial struct ImVec4
{
    public float x;
    public float y;
    public float z;
    public float w;
}

