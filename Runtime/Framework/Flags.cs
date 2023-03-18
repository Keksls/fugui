using ImGuiNET;

namespace Fu.Framework
{
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
        Right = 1
    }

    /// <summary>
    /// Define Fugui Knobs behaviour flags
    /// </summary>
    public enum FuKnobFlags
    {
        /// <summary>
        /// Default knob behaviour
        /// </summary>
        Default = 0,
        /// <summary>
        /// Display a tooltip hover value
        /// </summary>
        ValueTooltip = 1,
        /// <summary>
        /// Do not draw Drag float under the knob
        /// </summary>
        NoInput = 2
    }

    /// <summary>
    /// Define knob types
    /// </summary>
    public enum FuKnobVariant
    {
        Tick = 1,
        Dot = 2,
        Wiper = 4,
        WiperOnly = 8,
        WiperDot = 16,
        Stepped = 32,
        Space = 64,
    }

    /// <summary>
    /// Flag that represent the position of the text of a Progressbar
    /// </summary>
    public enum ProgressBarTextPosition
    {
        /// <summary>
        /// Text a the left of the progressbar
        /// </summary>
        Left,
        /// <summary>
        /// Text at the right of the progressbar
        /// </summary>
        Right,
        /// <summary>
        /// Text at the middle of the filler part
        /// </summary>
        Inside,
        /// <summary>
        /// No text
        /// </summary>
        None
    }

    /// <summary>
    /// Flag for custom InputText Behaviour.
    /// </summary>
    public enum FuInputTextFlags
    {
        /// <summary>
        /// The default input text flag.
        /// </summary>
        Default = ImGuiInputTextFlags.CtrlEnterForNewLine | ImGuiInputTextFlags.AutoSelectAll,
        /// <summary>
        /// Allow decimal characters.
        /// </summary>
        CharsDecimal = 0x1,
        /// <summary>
        /// Allow hexadecimal characters.
        /// </summary>
        CharsHexadecimal = 0x2,
        /// <summary>
        /// Force uppercase characters.
        /// </summary>
        CharsUppercase = 0x4,
        /// <summary>
        /// Disallow blank characters.
        /// </summary>
        CharsNoBlank = 0x8,
        /// <summary>
        /// Mask the input as a password.
        /// </summary>
        Password = 0x8000,
        /// <summary>
        /// Allow scientific notation characters.
        /// </summary>
        CharsScientific = 0x20000,
        /// <summary>
        /// Escape key will clear all input.
        /// </summary>
        EscapeClearsAll = 0x100000,
        /// <summary>
        /// Validate the input text box only on Enter pressed
        /// </summary>
        EnterReturnsTrue = 0x20,
        /// <summary>
        /// The input is in readonly mode, user can not edit it (prefere using DisableNextElement if you need to disable the widget)
        /// </summary>
        ReadOnly = 0x4000
    }

    /// <summary>
    /// Flags for configuring the grid
    /// </summary>
    public enum FuGridFlag
    {
        /// <summary>
        /// Default flag
        /// </summary>
        Default = 0,
        /// <summary>
        /// Draws background lines in the grid
        /// </summary>
        LinesBackground = 1,
        /// <summary>
        /// Does not automatically label the grid
        /// </summary>
        NoAutoLabels = 2,
        /// <summary>
        /// Does not disable labels on the grid
        /// </summary>
        DoNotDisableLabels = 4,
        /// <summary>
        /// Automatically displays tooltips on labels
        /// </summary>
        AutoToolTipsOnLabels = 8
    }

    /// <summary>
    /// The type of grid to display
    /// </summary>
    public enum FuGridType
    {
        /// <summary>
        /// Automatically adjust the grid
        /// </summary>
        Auto,
        /// <summary>
        /// Grid with fixed width columns
        /// </summary>
        FixedWidth,
        /// <summary>
        /// Grid with columns with width determined by a ratio
        /// </summary>
        RatioWidth,
        /// <summary>
        /// Grid with flexible columns
        /// </summary>
        FlexibleCols
    }

    /// <summary>
    /// The type of state to display
    /// </summary>
    public enum StateType
    {
        /// <summary>
        /// Display a danger state
        /// </summary>
        Danger = 0,
        /// <summary>
        /// Display a success state
        /// </summary>
        Success = 1,
        /// <summary>
        /// Display an informational state
        /// </summary>
        Info = 2,
        /// <summary>
        /// Display a warning state
        /// </summary>
        Warning = 3
    }

    /// <summary>
    /// Flags for configuring the slider
    /// </summary>
    public enum FuSliderFlags
    {
        /// <summary>
        /// Default flag
        /// </summary>
        Default = 0,
        /// <summary>
        /// Slider can be dragged from the left
        /// </summary>
        LeftDrag = 1,
        /// <summary>
        /// Slider cannot be dragged
        /// </summary>
        NoDrag = 2
    }

    /// <summary>
    /// Flags for configuring the button group
    /// </summary>
    public enum FuButtonsGroupFlags
    {
        /// <summary>
        /// Default flag
        /// </summary>
        Default = 0,
        /// <summary>
        /// Automatically size buttons in the group
        /// </summary>
        AutoSizeButtons = 1,
        /// <summary>
        /// Align buttons to the left
        /// </summary>
        AlignLeft = 2
    }

    /// <summary>
    /// Flags for configuring the toggle
    /// </summary>
    public enum FuToggleFlags
    {
        /// <summary>
        /// Default toggle flag
        /// </summary>
        Default = 0,
        /// <summary>
        /// The size of the toggle will be the size of the largest text
        /// </summary>
        MaximumTextSize = 1,
        /// <summary>
        /// Align toggle text to the left
        /// </summary>
        AlignLeft = 2,
        /// <summary>
        /// The state of this toggle can't be changed by user
        /// </summary>
        NoEditable = 4,
        /// <summary>
        /// The toggle is not an activation state, it's a switch state, so the color will always be the non selected one
        /// </summary>
        SwitchState = 8
    }

    /// <summary>
    /// Flag to behave Fugui Panel
    /// </summary>
    public enum FuPanelFlags
    {
        /// <summary>
        /// Default panel flag
        /// </summary>
        Default = 0,
        /// <summary>
        /// No Scrollbar
        /// </summary>
        NoScroll = 1,
        /// <summary>
        /// Draw Borders
        /// </summary>
        DrawBorders = 2
    }

    /// <summary>
    /// Flag that represent an aligmenent position
    /// </summary>
    public enum FuElementAlignement
    {
        /// <summary>
        /// Align element to the left
        /// </summary>
        Left = 0,
        /// <summary>
        /// Align element to the center
        /// </summary>
        Center = 1,
        /// <summary>
        /// Align element to the right
        /// </summary>
        Right = 2
    }
}