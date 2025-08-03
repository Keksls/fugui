using ImGuiNET;
using System.Collections.Generic;

namespace Fu.Core
{
    /// <summary>
    /// struct that represent Keyboard states for an UIWindow
    /// </summary>
    public class FuKeyboardState
    {
        private readonly FuWindow _window;
        private readonly ImGuiIOPtr _io;
        public bool KeyAlt { get { return (_window == null || _window.State == FuWindowState.Manipulating || FuWindow.InputFocusedWindow == _window) && _io.KeyAlt; } }
        public bool KeyCtrl { get { return (_window == null || _window.State == FuWindowState.Manipulating || FuWindow.InputFocusedWindow == _window) && _io.KeyCtrl; } }
        public bool KeyShift { get { return (_window == null || _window.State == FuWindowState.Manipulating || FuWindow.InputFocusedWindow == _window) && _io.KeyShift; } }
        public bool KeySuper { get { return (_window == null || _window.State == FuWindowState.Manipulating || FuWindow.InputFocusedWindow == _window) && _io.KeySuper; } }
        private static readonly HashSet<FuKeysCode> _currentPressedKeys = new HashSet<FuKeysCode>();
        private readonly FuKeyState[] _keysStates;
        private static int _minKeyValue;

        /// <summary>
        /// instantiate a new FuKeyboardState relatif to a FuWindow
        /// </summar>
        /// <param name="io">the ImGUi Io ptr attached to this Keyboard State</param>
        /// <param name="window">the window attached to this Keyboard State (optional)</param>
        public FuKeyboardState(ImGuiIOPtr io, FuWindow window = null)
        {
            _window = window;
            _io = io;
            // get and store FuKeysCode.MIN as int to avoid multiple cast at runtime
            if (_minKeyValue == 0)
            {
                _minKeyValue = (int)FuKeysCode.NamedKey_BEGIN;
            }
            // bind keys states and store it into an array
            _keysStates = new FuKeyState[(int)FuKeysCode.NamedKey_END - (int)FuKeysCode.NamedKey_BEGIN];
            for (int key = 0; key < _keysStates.Length; key++)
            {
                _keysStates[key] = new FuKeyState(key + _minKeyValue);
            }
        }

        /// <summary>
        /// Whatever a keyboard key is pressed
        /// </summary>
        /// <returns>true if Pressed</returns>
        public FuKeyState GetKeyStates(FuKeysCode key)
        {
            if (_window == null || _window.State == FuWindowState.Manipulating)
            {
                return _keysStates[(int)key - _minKeyValue];
            }
            return default;
        }

        /// <summary>
        /// Whatever a keyboard key is pressed
        /// </summary>
        /// <returns>true if Pressed</returns>
        public bool GetKeyPressed(FuKeysCode key)
        {
            if (_window == null || _window.State == FuWindowState.Manipulating)
            {
                return _keysStates[(int)key - _minKeyValue].IsPressed;
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
                return _keysStates[(int)key - _minKeyValue].IsDown;
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
                return _keysStates[(int)key - _minKeyValue].IsUp;
            }
            return false;
        }

        /// <summary>
        /// Update keyboard backed states for this frame
        /// Must called once by frame event in each windows and each container that implement keyboard state (juste next to mouse states update)
        /// </summary>
        internal void UpdateState()
        {
            switch (_window)
            {
                // window is null, so it's a container related keyboard, let's bind it whatever current input focused window
                case null:
                    for (int key = 0; key < _keysStates.Length; key++)
                    {
                        _keysStates[key].SetState(_io.KeysData[key].Down == 1);
                    }
                    break;

                // window is NOT nul, so it's a window related keyboard, let's bind it according to current input focused window
                case not null:
                    if ((_window.State == FuWindowState.Manipulating && FuWindow.InputFocusedWindow == null) || FuWindow.InputFocusedWindow == _window)
                    {
                        for (int key = 0; key < _keysStates.Length; key++)
                        {
                            bool keyState = _io.KeysData[key].Down == 1;

                            // the key has just been pressed this frame => KeyDown
                            if (!_keysStates[key].IsPressed && keyState)
                            {
                                // Set current window as input focus
                                FuWindow.InputFocusedWindow = _window;
                                // increase quantity of input holding the current input focused window
                                FuWindow.NbInputFocusedWindow++;
                                // add down key as pressed keys
                                _currentPressedKeys.Add((FuKeysCode)key + _minKeyValue);
                            }

                            // the key has just been released this frame => KeyUp
                            if (_keysStates[key].IsPressed && !keyState)
                            {
                                // remove up key from pressed keys
                                if (_currentPressedKeys.Remove((FuKeysCode)key + _minKeyValue))
                                {
                                    // decrease quantity of input holding the current input focused window
                                    FuWindow.NbInputFocusedWindow--;
                                }
                            }

                            // update key state
                            _keysStates[key].SetState(keyState);
                        }
                    }
                    // This window does NOT has focus, let's ignore binding
                    else
                    {
                        for (int key = 0; key < _keysStates.Length; key++)
                        {
                            _keysStates[key].SetState(false);
                        }
                    }
                    break;
            }
        }
    }
}