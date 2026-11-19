using ImGuiNET;

namespace Fu.Framework
{
    /// <summary>
    /// The text input focused in a Fugui context, typed in by a <see cref="FuVirtualKeyboard"/> as if a physical
    /// keyboard did: the characters and keys go to the input queue of the context.
    /// </summary>
    /// <remarks>
    /// For a keyboard drawn in another context than the text input (a 3D window under a screen window, for instance):
    /// a click on a key of the same context would take the focus from the text input.
    /// </remarks>
    public class FuContextKeyboardTarget : IFuVirtualKeyboardTarget
    {
        #region State
        private readonly FuContext _context;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a target over the text input focused in a context.
        /// </summary>
        /// <param name="context">The context of the text input.</param>
        public FuContextKeyboardTarget(FuContext context)
        {
            _context = context;
        }
        #endregion

        #region Methods
        /// <inheritdoc/>
        public void Insert(string text)
        {
            if (_context != null && !string.IsNullOrEmpty(text))
            {
                _context.IO.AddInputCharactersUTF8(text);
            }
        }

        /// <inheritdoc/>
        public void Backspace()
        {
            pressKey(ImGuiKey.Backspace);
        }

        /// <inheritdoc/>
        public void Submit()
        {
            pressKey(ImGuiKey.Enter);
        }

        /// <summary>
        /// Presses and releases a key: ImGui spreads the queued events over the next frames.
        /// </summary>
        /// <param name="key">The key.</param>
        private void pressKey(ImGuiKey key)
        {
            if (_context == null)
            {
                return;
            }

            _context.IO.AddKeyEvent(key, true);
            _context.IO.AddKeyEvent(key, false);
        }
        #endregion
    }
}
