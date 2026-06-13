using ImGuiNET;
using System;
using System.Text;

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
    /// Fugui mouse cursor shapes. Values match ImGuiMouseCursor and are converted internally.
    /// </summary>
    public enum FuMouseCursor
    {
        Arrow = 0,
        TextInput = 1,
        ResizeAll = 2,
        ResizeNS = 3,
        ResizeEW = 4,
        ResizeNESW = 5,
        ResizeNWSE = 6,
        Hand = 7,
        NotAllowed = 8
    }

    /// <summary>
    /// Fugui invisible interaction button flags. Values match ImGuiButtonFlags and are converted internally.
    /// </summary>
    [System.Flags]
    public enum FuButtonFlags
    {
        None = 0,
        MouseButtonLeft = 1,
        MouseButtonRight = 2,
        MouseButtonMiddle = 4,
        MouseButtonMask = 7,
        EnableNav = 8
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
        /// Allow this window to be moved by dragging empty body space, not only its title header.
        /// </summary>
        MoveFromBody = 256,
        /// <summary>
        /// Prevent mouse input from making this window the Fugui input-focused window.
        /// </summary>
        NoMouseInputFocus = 512,
        /// <summary>
        /// Prevent keyboard input from making this window the Fugui input-focused window.
        /// </summary>
        NoKeyboardInputFocus = 1024,
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
    /// Declares the Fugui surface layer of a window.
    /// ImGui remains authoritative for native focus and final display order; Fugui uses this layer to describe intent and arbitrate inputs between Fugui-owned surfaces.
    /// </summary>
    public enum FuLayer
    {
        /// <summary>
        /// Default floating or docked interactive window layer.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Background windows such as HUDs. Fugui keeps them behind normal windows and they never block higher layers.
        /// </summary>
        Background = 1,
        /// <summary>
        /// Explicit alias for HUD surfaces. HUDs are background windows, not modals or popups.
        /// </summary>
        Hud = Background,
        /// <summary>
        /// Top layer for Fugui windows that must stay above normal windows. Fugui popups and notifications also register as top surfaces, but modals are not FuWindows.
        /// </summary>
        Top = 2
    }

    /// <summary>
    /// Define the ImGui window flags exposed through Fugui APIs.
    /// Values intentionally match ImGuiWindowFlags so they can be converted internally without remapping.
    /// </summary>
    [System.Flags]
    public enum FuWindowStyleFlags
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
        ChildWindow = 16777216,
        Tooltip = 33554432,
        Popup = 67108864,
        Modal = 134217728,
        ChildMenu = 268435456,
        DockNodeHost = 536870912,
        Default = NoCollapse
    }

    /// <summary>
    /// Fugui child-window flags. Values match ImGuiChildFlags and are converted internally.
    /// </summary>
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
        NavFlattened = 256
    }

    /// <summary>
    /// Fugui style variable slots. Values match ImGuiStyleVar and are converted internally.
    /// </summary>
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
        DockingSeparatorSize = 33
    }

    /// <summary>
    /// Fugui drag-drop behavior flags. Values match ImGuiDragDropFlags and are converted internally.
    /// </summary>
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
        AcceptPeekOnly = 3072
    }

    /// <summary>
    /// Fugui draw-list corner and path flags. Values match FuDrawFlags and are converted internally.
    /// </summary>
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
        RoundCornersMask = 496
    }

    /// <summary>
    /// Fugui table column flags. Values match ImGuiTableColumnFlags and are converted internally.
    /// </summary>
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
        IsEnabled = 16777216,
        IsVisible = 33554432,
        IsSorted = 67108864,
        IsHovered = 134217728,
        WidthMask = 24,
        IndentMask = 196608,
        StatusMask = 251658240,
        NoDirectResize = 1073741824
    }

    /// <summary>
    /// Fugui config flags applied to the internal ImGui IO. Values match ImGuiConfigFlags.
    /// </summary>
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
        IsTouchScreen = 2097152
    }

    /// <summary>
    /// Fugui dock node flags. Values match ImGuiDockNodeFlags.
    /// </summary>
    [System.Flags]
    public enum FuDockNodeFlags
    {
        None = 0,
        KeepAliveOnly = 1,
        NoDockingOverCentralNode = 4,
        PassthruCentralNode = 8,
        NoDockingSplit = 16,
        NoResize = 32,
        AutoHideTabBar = 64,
        NoUndocking = 128
    }

    /// <summary>
    /// Fugui direction enum. Values match ImGuiDir.
    /// </summary>
    public enum FuDirection
    {
        None = -1,
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3
    }

    /// <summary>
    /// Define which sides of a resizable Fugui window can be dragged.
    /// </summary>
    [System.Flags]
    public enum FuWindowResizeSides
    {
        /// <summary>
        /// No side can resize the window.
        /// </summary>
        None = 0,
        /// <summary>
        /// The left side can resize the window.
        /// </summary>
        Left = 1 << 0,
        /// <summary>
        /// The right side can resize the window.
        /// </summary>
        Right = 1 << 1,
        /// <summary>
        /// The bottom side can resize the window.
        /// </summary>
        Bottom = 1 << 2,
        /// <summary>
        /// The top side can resize the window.
        /// </summary>
        Top = 1 << 3,
        /// <summary>
        /// Default Fugui resize sides. Top is locked by default.
        /// </summary>
        Default = Left | Bottom | Right,
        /// <summary>
        /// All sides can resize the window.
        /// </summary>
        All = Left | Right | Bottom | Top
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
}
