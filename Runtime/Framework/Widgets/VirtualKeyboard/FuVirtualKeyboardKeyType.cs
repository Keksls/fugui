namespace Fu.Framework
{
    /// <summary>
    /// What a key of a <see cref="FuVirtualKeyboard"/> does.
    /// </summary>
    public enum FuVirtualKeyboardKeyType
    {
        /// <summary>
        /// Types its character (upper case while shifted, for a letter).
        /// </summary>
        Character = 0,

        /// <summary>
        /// Removes the character before the caret.
        /// </summary>
        Backspace = 1,

        /// <summary>
        /// Switches the upper case of the letters on and off.
        /// </summary>
        Shift = 2,

        /// <summary>
        /// Switches between the letters and the symbols.
        /// </summary>
        Symbols = 3,

        /// <summary>
        /// Switches the layout of the letters (QWERTY, AZERTY).
        /// </summary>
        Layout = 4,

        /// <summary>
        /// Types a space.
        /// </summary>
        Space = 5,

        /// <summary>
        /// Validates the text.
        /// </summary>
        Enter = 6
    }
}
