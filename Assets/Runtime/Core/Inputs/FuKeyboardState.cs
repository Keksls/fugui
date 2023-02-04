using ImGuiNET;

namespace Fu.Core
{
    /// <summary>
    /// struct that represent Keyboard states for an UIWindow
    /// </summary>
    public class FuKeyboardState
    {
        private FuWindow _window;
        public bool KeyAlt { get { return _window.Container?.Context.IO.KeyAlt ?? false; } }
        public bool KeyCtrl { get { return _window.Container?.Context.IO.KeyCtrl ?? false; } }
        public bool KeyShift { get { return _window.Container?.Context.IO.KeyShift ?? false; } }
        public bool KeySuper { get { return _window.Container?.Context.IO.KeySuper ?? false; } }

        /// <summary>
        /// instantiate a new FuKeyboardState relatif to a FuWindow
        /// </summar>
        /// <param name="window">the window attached to this Keyboard State</param>
        public FuKeyboardState(FuWindow window)
        {
            _window = window;
        }

        /// <summary>
        /// Whatever a keyboard key is pressed
        /// </summary>
        /// <returns>true if Pressed</returns>
        public bool GetKeyPressed(FuKeysCode key)
        {
            if (_window.Container != null && _window.State == FuWindowState.Manipulating)
            {
                return _window.Container.Context.IO.KeysDown[(int)key];
            }
            return false;
        }

        /// <summary>
        /// Whatever a keyboard key just down this frame
        /// </summary>
        /// <returns>true if Down</returns>
        public bool GetKeyDown(FuKeysCode key)
        {
            if (_window.Container != null && _window.State == FuWindowState.Manipulating)
            {
                ImGuiKeyData data = _window.Container.Context.IO.KeysData[(int)key];
                return data.Down != 0 && data.DownDurationPrev == 0;
            }
            return false;
        }

        /// <summary>
        /// Whatever a keyboard key just up this frame
        /// </summary>
        /// <returns>true if Up</returns>
        public bool GetKeyUp(FuKeysCode key)
        {
            if (_window.Container != null && _window.State == FuWindowState.Manipulating)
            {
                ImGuiKeyData data = _window.Container.Context.IO.KeysData[(int)key];
                return data.Down == 0 && data.DownDurationPrev != -1;
            }
            return false;
        }
    }
}