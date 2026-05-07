namespace Fu
{
    /// <summary>
    /// Flags for configuring external windows
    /// </summary>
    [System.Flags]
    public enum FuExternalWindowFlags
    {
        None = 0,
        /// <summary>
        /// Use the native OS title bar for the window
        /// </summary>
        UseNativeTitleBar = 1,
        /// <summary>
        /// Do not show the window in the task bar
        /// </summary>
        NoTaskBarIcon = 2,
        /// <summary>
        /// Do not focus the window when it appears
        /// </summary>
        NoFocusOnAppearing = 4,
        /// <summary>
        /// Keep the window always on top of other windows
        /// </summary>
        AlwaysOnTop = 8,
        /// <summary>
        /// Do not make the window modal
        /// </summary>
        NoModal = 16,
        /// <summary>
        /// Do not send notifications when the window is opened or closed
        /// </summary>
        NoNotify = 32,
        /// <summary>
        /// Do not show the context menu on right click
        /// </summary>
        NoContextMenu = 64,

        /// <summary>
        /// Default external window configuration
        /// </summary>
        Default = UseNativeTitleBar | NoModal | NoNotify
    }

    /// <summary>
    /// Enum that represent an UI window state
    /// </summary>
    public enum FuWindowState
    {
        /// <summary>
        /// UI is not focused and can be drawed few times per seconds
        /// </summary>
        Idle,
        /// <summary>
        /// UI is focused or manipulated and must be drawed many times per seconds
        /// </summary>
        Manipulating
    }

    /// <summary>
    /// Define Fugui mouse buttons
    /// </summary>
    public enum FuMouseButton
    {
        /// <summary>
        /// None / default no button
        /// </summary>
        None = -1,
        /// <summary>
        /// Left mouse button
        /// </summary>
        Left = 0,
        /// <summary>
        /// Right mouse button
        /// </summary>
        Right = 1,
        /// <summary>
        /// Center mouse button
        /// </summary>
        Center = 2
    }

    /// <summary>
    /// Define Fugui font types
    /// </summary>
    public enum FontType
    {
        /// <summary>
        /// Regular is default font type
        /// </summary>
        Regular,
        /// <summary>
        /// Bold font (defined into FontConfig)
        /// </summary>
        Bold,
        /// <summary>
        /// Italic font (defined into FontConfig)
        /// </summary>
        Italic
    }

    /// <summary>
    /// Define the behaviour flags of a Fugui Window
    /// </summary>
    [System.Flags]
    public enum FuWindowFlags
    {
        /// <summary>
        /// No FuWindow behaviour flags.
        /// </summary>
        None = 0,
        /// <summary>
        /// Close this window when middle-clicking its title header or docked tab.
        /// </summary>
        CloseOnMiddleClick = 128,
        /// <summary>
        /// Default FuWindow behaviour
        /// </summary>
        Default = CloseOnMiddleClick,
        /// <summary>
        /// Prevent this window to be externalized
        /// </summary>
        NoExternalization = 1,
        /// <summary>
        /// Never dock this window into another
        /// </summary>
        NoDocking = 2,
        /// <summary>
        /// This window only show informations and user cannot interract with it,
        /// Use this flag for performances
        /// </summary>
        NoInterractions = 4,
        /// <summary>
        /// No other window can dock inside of this window
        /// </summary>
        NoDockingOverMe = 8,
        /// <summary>
        /// Can this window be instantied multiple times at once ? (draw multiple instance of this window)
        /// </summary>
        AllowMultipleWindow = 16,
        /// <summary>
        /// Can this window be closed
        /// </summary>
        NoClosable = 32,
        /// <summary>
        /// do NOT register windows definition to Fugui core on constructor
        /// </summary>
        NoAutoRegisterWindowDefinition = 64
    }

    /// <summary>
    /// Define the behaviour flags of an Overlay
    /// </summary>
    public enum FuOverlayFlags
    {
        Default = 0,
        /// <summary>
        /// The overlay can't be moved using drag button
        /// </summary>
        NoMove = 1,
        /// <summary>
        /// The overlay can't be collapsed using drag button
        /// </summary>
        NoClose = 2,
        /// <summary>
        /// The overlay don't display container background
        /// </summary>
        NoBackground = 4,
        /// <summary>
        /// User can't change overlay anchor using drag button context menu
        /// </summary>
        NoEditAnchor = 8
    }

    /// <summary>
    /// Define the different positions of the Drag button of an Overlay
    /// </summary>
    public enum FuOverlayDragPosition
    {
        /// <summary>
        /// Will place the drag button according the the anchol Location of the Overlay
        /// </summary>
        Auto,
        /// <summary>
        /// Top of the Overlay
        /// </summary>
        Top,
        /// <summary>
        /// Right of the overlay
        /// </summary>
        Right,
        /// <summary>
        /// Bottom of the overlay
        /// </summary>
        Bottom,
        /// <summary>
        /// Left of  the overlay
        /// </summary>
        Left
    }

    /// <summary>
    /// Define the different anchor points of an Overlay
    /// </summary>
    public enum FuOverlayAnchorLocation
    {
        /// <summary>
        /// anchor to top left corner
        /// </summary>
        TopLeft,
        /// <summary>
        /// anchor to top center
        /// </summary>
        TopCenter,
        /// <summary>
        /// anchor to top right corner
        /// </summary>
        TopRight,
        /// <summary>
        /// anchor to middle left side
        /// </summary>
        MiddleLeft,
        /// <summary>
        /// anchor to middle center
        /// </summary>
        MiddleCenter,
        /// <summary>
        /// anchor to middle right side
        /// </summary>
        MiddleRight,
        /// <summary>
        /// anchor to bottom left corner
        /// </summary>
        BottomLeft,
        /// <summary>
        /// anchor to bottom center
        /// </summary>
        BottomCenter,
        /// <summary>
        /// anchor to bottom right corner
        /// </summary>
        BottomRight
    }

    /// <summary>
    /// Fugui draw list handle. This is a zero-copy view over the native draw list owned by Fugui.
    /// </summary>
    public readonly struct FuDrawList
    {
        public readonly System.IntPtr NativePtr;

        public bool IsValid => NativePtr != System.IntPtr.Zero;
        public int VertexCount => ToImGui().VtxBuffer.Size;

        public FuDrawList(System.IntPtr nativePtr)
        {
            NativePtr = nativePtr;
        }

        internal unsafe FuDrawList(ImGuiNET.ImDrawList* nativePtr)
        {
            NativePtr = (System.IntPtr)nativePtr;
        }

        internal static FuDrawList FromImGui(ImGuiNET.ImDrawListPtr drawList)
        {
            unsafe
            {
                return new FuDrawList(drawList.NativePtr);
            }
        }

        internal ImGuiNET.ImDrawListPtr ToImGui()
        {
            return new ImGuiNET.ImDrawListPtr(NativePtr);
        }

        public void AddLine(UnityEngine.Vector2 p1, UnityEngine.Vector2 p2, uint color, float thickness = 1f)
        {
            ToImGui().AddLine(p1, p2, color, thickness);
        }

        public void AddRect(UnityEngine.Vector2 min, UnityEngine.Vector2 max, uint color, float rounding = 0f, FuDrawFlags flags = FuDrawFlags.None, float thickness = 1f)
        {
            ToImGui().AddRect(min, max, color, rounding, flags.ToImGui(), thickness);
        }

        public void AddRectFilled(UnityEngine.Vector2 min, UnityEngine.Vector2 max, uint color, float rounding = 0f, FuDrawFlags flags = FuDrawFlags.None)
        {
            ToImGui().AddRectFilled(min, max, color, rounding, flags.ToImGui());
        }

        public void AddCircle(UnityEngine.Vector2 center, float radius, uint color, int segments = 0, float thickness = 1f)
        {
            ToImGui().AddCircle(center, radius, color, segments, thickness);
        }

        public void AddCircleFilled(UnityEngine.Vector2 center, float radius, uint color, int segments = 0)
        {
            ToImGui().AddCircleFilled(center, radius, color, segments);
        }

        public void AddTriangleFilled(UnityEngine.Vector2 p1, UnityEngine.Vector2 p2, UnityEngine.Vector2 p3, uint color)
        {
            ToImGui().AddTriangleFilled(p1, p2, p3, color);
        }

        public void AddText(UnityEngine.Vector2 pos, uint color, string text)
        {
            ToImGui().AddText(pos, color, text);
        }

        public void AddImage(System.IntPtr textureId, UnityEngine.Vector2 min, UnityEngine.Vector2 max, UnityEngine.Vector2 uvMin, UnityEngine.Vector2 uvMax, uint color)
        {
            ToImGui().AddImage(textureId, min, max, uvMin, uvMax, color);
        }

        public void AddImageRounded(System.IntPtr textureId, UnityEngine.Vector2 min, UnityEngine.Vector2 max, UnityEngine.Vector2 uvMin, UnityEngine.Vector2 uvMax, uint color, float rounding, FuDrawFlags flags = FuDrawFlags.RoundCornersAll)
        {
            ToImGui().AddImageRounded(textureId, min, max, uvMin, uvMax, color, rounding, flags.ToImGui());
        }

        public void PushClipRect(UnityEngine.Vector2 min, UnityEngine.Vector2 max, bool intersectWithCurrentClipRect = false)
        {
            ToImGui().PushClipRect(min, max, intersectWithCurrentClipRect);
        }

        public void PopClipRect()
        {
            ToImGui().PopClipRect();
        }

        public void PathLineTo(UnityEngine.Vector2 pos)
        {
            ToImGui().PathLineTo(pos);
        }

        public void PathArcTo(UnityEngine.Vector2 center, float radius, float minAngle, float maxAngle, int segments = 0)
        {
            ToImGui().PathArcTo(center, radius, minAngle, maxAngle, segments);
        }

        public void PathStroke(uint color, FuDrawFlags flags = FuDrawFlags.None, float thickness = 1f)
        {
            ToImGui().PathStroke(color, flags.ToImGui(), thickness);
        }

        public void PrimReserve(int indexCount, int vertexCount)
        {
            ToImGui().PrimReserve(indexCount, vertexCount);
        }

        public void PrimWriteIdx(ushort idx)
        {
            ToImGui().PrimWriteIdx(idx);
        }

        public void PrimWriteVtx(UnityEngine.Vector2 pos, UnityEngine.Vector2 uv, uint color)
        {
            ToImGui().PrimWriteVtx(pos, uv, color);
        }
    }

    [System.Flags]
    public enum FuDrawFlags
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

    public enum FuStyleVar
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
        DockingSeparatorSize = 33,
    }

    [System.Flags]
    public enum FuDragDropFlags
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
    public enum FuWindowRuntimeFlags
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
        NoDocking = 524288,
        NoNav = 196608,
        NoDecoration = 43,
        NoInputs = 197120,
    }

    [System.Flags]
    public enum FuChildFlags
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

    [System.Flags]
    public enum FuTableColumnFlags
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
        WidthMask = 24,
        IndentMask = 196608,
    }

    [System.Flags]
    public enum FuConfigFlags
    {
        None = 0,
        NavEnableKeyboard = 1,
        NavEnableGamepad = 2,
        NoMouse = 16,
        NoMouseCursorChange = 32,
        NoKeyboard = 64,
        DockingEnable = 128,
        ViewportsEnable = 1024,
        DpiEnableScaleViewports = 16384,
        DpiEnableScaleFonts = 32768,
        IsSRGB = 1048576,
        IsTouchScreen = 2097152,
    }

    [System.Flags]
    public enum FuDockingFlags
    {
        None = 0,
        KeepAliveOnly = 1,
        NoDockingOverCentralNode = 4,
        PassthruCentralNode = 8,
        NoDockingSplit = 16,
        NoResize = 32,
        AutoHideTabBar = 64,
        NoUndocking = 128,
    }

    public enum FuDir
    {
        None = -1,
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3,
    }

    public enum FuMouseCursor
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
    }

    internal static class FuImGuiConversions
    {
        internal static ImGuiNET.ImDrawFlags ToImGui(this FuDrawFlags value) => (ImGuiNET.ImDrawFlags)value;
        internal static ImGuiNET.ImGuiStyleVar ToImGui(this FuStyleVar value) => (ImGuiNET.ImGuiStyleVar)value;
        internal static ImGuiNET.ImGuiDragDropFlags ToImGui(this FuDragDropFlags value) => (ImGuiNET.ImGuiDragDropFlags)value;
        internal static ImGuiNET.ImGuiWindowFlags ToImGui(this FuWindowRuntimeFlags value) => (ImGuiNET.ImGuiWindowFlags)value;
        internal static ImGuiNET.ImGuiChildFlags ToImGui(this FuChildFlags value) => (ImGuiNET.ImGuiChildFlags)value;
        internal static ImGuiNET.ImGuiTableColumnFlags ToImGui(this FuTableColumnFlags value) => (ImGuiNET.ImGuiTableColumnFlags)value;
        internal static ImGuiNET.ImGuiConfigFlags ToImGui(this FuConfigFlags value) => (ImGuiNET.ImGuiConfigFlags)value;
        internal static ImGuiNET.ImGuiDockNodeFlags ToImGui(this FuDockingFlags value) => (ImGuiNET.ImGuiDockNodeFlags)value;
        internal static ImGuiNET.ImGuiDir ToImGui(this FuDir value) => (ImGuiNET.ImGuiDir)value;
        internal static ImGuiNET.ImGuiMouseCursor ToImGui(this FuMouseCursor value) => (ImGuiNET.ImGuiMouseCursor)value;
        internal static FuMouseCursor ToFugui(this ImGuiNET.ImGuiMouseCursor value) => (FuMouseCursor)value;
    }
}
