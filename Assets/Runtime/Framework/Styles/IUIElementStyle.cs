namespace Fugui.Framework
{
    /// <summary>
    /// Interface for a UI element style.
    /// </summary>
    public interface IUIElementStyle
    {
        /// <summary>
        /// Pushes the style onto the stack, with the given enabled state.
        /// </summary>
        /// <param name="isEnabled">Whether the style should be enabled or disabled.</param>
        public void Push(bool isEnabled);

        /// <summary>
        /// Pops the style from the stack.
        /// </summary>
        public void Pop();
    }
}