namespace Fugui.Framework
{
    public enum StateType
    {
        Danger,
        Success,
        Info,
        Warning
    }

    public enum ButtonsGroupFlags
    {
        Default = 0,
        AutoSizeButtons = 1,
        AlignLeft = 2
    }

    public enum ToggleFlags
    {
        Default = 0,
        MaximumTextSize = 1,
        AlignLeft = 2
    }

    public enum PanelFlags
    {
        Default = 0,
        NoScroll = 1,
        DrawBorders = 2
    }

    public enum ElementAlignement
    {
        Left,
        Center,
        Right
    }
}