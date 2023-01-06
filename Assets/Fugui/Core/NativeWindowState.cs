namespace Fugui.Core
{
    /// <summary>
    /// Enum that represent an UI window state
    /// </summary>
    public enum NativeWindowState
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
}