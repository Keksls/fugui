namespace Fu.Core
{
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
    public enum FuWindowFlags
    {
        /// <summary>
        /// Default FuWindow behaviour
        /// </summary>
        Default = 0,
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
        AllowMultipleWindow = 16
    }

    /// <summary>
    /// Define behaviour flags of an External window
    /// </summary>
    public enum FuExternalWindowFlags
    {
        /// <summary>
        /// Default behaviour
        /// </summary>
        Default = 0,
        /// <summary>
        /// Show the Window's window titleBar and border
        /// </summary>
        ShowWindowTitle = 1
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