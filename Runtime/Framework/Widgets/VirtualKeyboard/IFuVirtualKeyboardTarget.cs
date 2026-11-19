namespace Fu.Framework
{
    /// <summary>
    /// What a <see cref="FuVirtualKeyboard"/> types in: a string (<see cref="FuTextKeyboardTarget"/>), the text input
    /// focused in a Fugui context (<see cref="FuContextKeyboardTarget"/>), or any text of the application.
    /// </summary>
    public interface IFuVirtualKeyboardTarget
    {
        /// <summary>
        /// Types characters.
        /// </summary>
        /// <param name="text">Characters typed.</param>
        void Insert(string text);

        /// <summary>
        /// Removes the character before the caret.
        /// </summary>
        void Backspace();

        /// <summary>
        /// Validates the text (Enter).
        /// </summary>
        void Submit();
    }
}
