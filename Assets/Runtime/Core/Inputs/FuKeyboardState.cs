using ImGuiNET;

namespace Fu.Core
{
    /// <summary>
    /// struct that represent Keyboard states for an UIWindow
    /// </summary>
    public class FuKeyboardState
    {
        private FuWindow _window;
        private ImGuiIOPtr _io;
        public bool KeyAlt { get { return _window.State == FuWindowState.Manipulating ? _io.KeyAlt : false; } }
        public bool KeyCtrl { get { return _window.State == FuWindowState.Manipulating ? _io.KeyCtrl : false; } }
        public bool KeyShift { get { return _window.State == FuWindowState.Manipulating ? _io.KeyShift : false; } }
        public bool KeySuper { get { return _window.State == FuWindowState.Manipulating ? _io.KeySuper : false; } }

        /// <summary>
        /// instantiate a new FuKeyboardState relatif to a FuWindow
        /// </summar>
        /// <param name="io">the ImGUi Io ptr attached to this Keyboard State</param>
        /// <param name="window">the window attached to this Keyboard State (optional)</param>
        public FuKeyboardState(ImGuiIOPtr io, FuWindow window = null)
        {
            _window = window;
            _io = io;
        }

        /// <summary>
        /// Whatever a keyboard key is pressed
        /// </summary>
        /// <returns>true if Pressed</returns>
        public bool GetKeyPressed(FuKeysCode key)
        {
            if (_window == null || _window.State == FuWindowState.Manipulating)
            {
                return _io.KeysDown[(int)key];
            }
            return false;
        }

        /// <summary>
        /// Whatever a keyboard key just down this frame
        /// </summary>
        /// <returns>true if Down</returns>
        public bool GetKeyDown(FuKeysCode key)
        {
            if (_window == null || _window.State == FuWindowState.Manipulating)
            {
                ImGuiKeyData data = _io.KeysData[(int)key];
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
            if (_window == null || _window.State == FuWindowState.Manipulating)
            {
                ImGuiKeyData data = _io.KeysData[(int)key];
                return data.Down == 0 && data.DownDurationPrev != -1;
            }
            return false;
        }
    }
}