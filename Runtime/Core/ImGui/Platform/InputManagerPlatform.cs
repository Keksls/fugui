using ImGuiNET;
using Fu.Core.DearImGui.Assets;
using UnityEngine;
using System.Collections.Generic;
using Fu.Framework;

namespace Fu.Core.DearImGui.Platform
{
    // TODO: Check this feature and remove from here when checked and done.
    // Implemented features:
    // [ ] Platform: Gamepad support. Enabled with io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad.
    // [~] Platform: IME support.

    /// <summary>
    /// Platform bindings for ImGui in Unity in charge of: mouse/keyboard/gamepad inputs, cursor shape, timing, windowing.
    /// </summary>
    internal sealed class InputManagerPlatform : PlatformBase
    {
        private readonly Event _textInputEvent = new Event();
        private static int _lastTextInputFrame = -1;
        private static List<uint> _frameTextInput = new List<uint>();

        private int[] _mainKeys;

        public InputManagerPlatform(CursorShapesAsset cursorShapes, IniSettingsAsset iniSettings) :
            base(cursorShapes, iniSettings)
        { }

        public override bool Initialize(ImGuiIOPtr io, string platformName)
        {
            base.Initialize(io, platformName);
            Fugui.Settings.ApplyTo(io);
            SetupKeyboard(io);

            return true;
        }

        public override void PrepareFrame(ImGuiIOPtr io, Rect displayRect, bool updateMouse, bool updateKeyboard)
        {
            base.PrepareFrame(io, displayRect, updateMouse, updateKeyboard);

            if (updateKeyboard)
            {
                UpdateKeyboard(io);
            }
            if (updateMouse)
            {
                UpdateMouse(io);
                UpdateCursor(io, ImGui.GetMouseCursor());
            }
        }

        private void SetupKeyboard(ImGuiIOPtr io)
        {
            // Map and store new keys by assigning io.KeyMap and setting value of array
            _mainKeys = new int[] {
                io.KeyMap[(int)ImGuiKey.Tab] = (int)KeyCode.Tab,
                io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)KeyCode.LeftArrow,
                io.KeyMap[(int)ImGuiKey.RightArrow] = (int)KeyCode.RightArrow,
                io.KeyMap[(int)ImGuiKey.UpArrow] = (int)KeyCode.UpArrow,
                io.KeyMap[(int)ImGuiKey.DownArrow] = (int)KeyCode.DownArrow,
                io.KeyMap[(int)ImGuiKey.PageUp] = (int)KeyCode.PageUp,
                io.KeyMap[(int)ImGuiKey.PageDown] = (int)KeyCode.PageDown,
                io.KeyMap[(int)ImGuiKey.Home] = (int)KeyCode.Home,
                io.KeyMap[(int)ImGuiKey.End] = (int)KeyCode.End,
                io.KeyMap[(int)ImGuiKey.Insert] = (int)KeyCode.Insert,
                io.KeyMap[(int)ImGuiKey.Delete] = (int)KeyCode.Delete,
                io.KeyMap[(int)ImGuiKey.Backspace] = (int)KeyCode.Backspace,
                io.KeyMap[(int)ImGuiKey.Space] = (int)KeyCode.Space,
                io.KeyMap[(int)ImGuiKey.Enter] = (int)KeyCode.Return,
                io.KeyMap[(int)ImGuiKey.Escape] = (int)KeyCode.Escape,

                io.KeyMap[(int)ImGuiKey.A] = (int)KeyCode.A,
                io.KeyMap[(int)ImGuiKey.B] = (int)KeyCode.B,
                io.KeyMap[(int)ImGuiKey.C] = (int)KeyCode.C,
                io.KeyMap[(int)ImGuiKey.D] = (int)KeyCode.D,
                io.KeyMap[(int)ImGuiKey.E] = (int)KeyCode.E,
                io.KeyMap[(int)ImGuiKey.F] = (int)KeyCode.F,
                io.KeyMap[(int)ImGuiKey.G] = (int)KeyCode.G,
                io.KeyMap[(int)ImGuiKey.H] = (int)KeyCode.H,
                io.KeyMap[(int)ImGuiKey.I] = (int)KeyCode.I,
                io.KeyMap[(int)ImGuiKey.J] = (int)KeyCode.J,
                io.KeyMap[(int)ImGuiKey.K] = (int)KeyCode.K,
                io.KeyMap[(int)ImGuiKey.L] = (int)KeyCode.L,
                io.KeyMap[(int)ImGuiKey.M] = (int)KeyCode.M,
                io.KeyMap[(int)ImGuiKey.N] = (int)KeyCode.N,
                io.KeyMap[(int)ImGuiKey.O] = (int)KeyCode.O,
                io.KeyMap[(int)ImGuiKey.P] = (int)KeyCode.P,
                io.KeyMap[(int)ImGuiKey.Q] = (int)KeyCode.Q,
                io.KeyMap[(int)ImGuiKey.R] = (int)KeyCode.R,
                io.KeyMap[(int)ImGuiKey.S] = (int)KeyCode.S,
                io.KeyMap[(int)ImGuiKey.T] = (int)KeyCode.T,
                io.KeyMap[(int)ImGuiKey.U] = (int)KeyCode.U,
                io.KeyMap[(int)ImGuiKey.V] = (int)KeyCode.V,
                io.KeyMap[(int)ImGuiKey.W] = (int)KeyCode.W,
                io.KeyMap[(int)ImGuiKey.X] = (int)KeyCode.X,
                io.KeyMap[(int)ImGuiKey.Y] = (int)KeyCode.Y,
                io.KeyMap[(int)ImGuiKey.Z] = (int)KeyCode.Z,

                io.KeyMap[(int)ImGuiKey.LeftCtrl] = (int)KeyCode.LeftControl,
                io.KeyMap[(int)ImGuiKey.LeftShift] = (int)KeyCode.LeftShift,
                io.KeyMap[(int)ImGuiKey.LeftAlt] = (int)KeyCode.LeftAlt,
                io.KeyMap[(int)ImGuiKey.LeftSuper] = (int)KeyCode.LeftCommand,
                io.KeyMap[(int)ImGuiKey.RightCtrl] = (int)KeyCode.RightControl,
                io.KeyMap[(int)ImGuiKey.RightShift] = (int)KeyCode.RightShift,
                io.KeyMap[(int)ImGuiKey.RightAlt] = (int)KeyCode.RightAlt,
                io.KeyMap[(int)ImGuiKey.RightSuper] = (int)KeyCode.RightCommand,
                io.KeyMap[(int)ImGuiKey.Menu] = (int)KeyCode.Menu,

                io.KeyMap[(int)ImGuiKey._0] = (int)KeyCode.Alpha0,
                io.KeyMap[(int)ImGuiKey._1] = (int)KeyCode.Alpha1,
                io.KeyMap[(int)ImGuiKey._2] = (int)KeyCode.Alpha2,
                io.KeyMap[(int)ImGuiKey._3] = (int)KeyCode.Alpha3,
                io.KeyMap[(int)ImGuiKey._4] = (int)KeyCode.Alpha4,
                io.KeyMap[(int)ImGuiKey._5] = (int)KeyCode.Alpha5,
                io.KeyMap[(int)ImGuiKey._6] = (int)KeyCode.Alpha6,
                io.KeyMap[(int)ImGuiKey._7] =(int)KeyCode.Alpha7,
                io.KeyMap[(int)ImGuiKey._8] = (int)KeyCode.Alpha8,
                io.KeyMap[(int)ImGuiKey._9] = (int)KeyCode.Alpha9,

                io.KeyMap[(int)ImGuiKey.F1] = (int)KeyCode.F1,
                io.KeyMap[(int)ImGuiKey.F2] = (int)KeyCode.F2,
                io.KeyMap[(int)ImGuiKey.F3] = (int)KeyCode.F3,
                io.KeyMap[(int)ImGuiKey.F4] = (int)KeyCode.F4,
                io.KeyMap[(int)ImGuiKey.F5] = (int)KeyCode.F5,
                io.KeyMap[(int)ImGuiKey.F6] = (int)KeyCode.F6,
                io.KeyMap[(int)ImGuiKey.F7] = (int)KeyCode.F7,
                io.KeyMap[(int)ImGuiKey.F8] = (int)KeyCode.F8,
                io.KeyMap[(int)ImGuiKey.F9] = (int)KeyCode.F9,
                io.KeyMap[(int)ImGuiKey.F10] = (int)KeyCode.F10,
                io.KeyMap[(int)ImGuiKey.F11] = (int)KeyCode.F11,
                io.KeyMap[(int)ImGuiKey.F12] = (int)KeyCode.F12,

                io.KeyMap[(int)ImGuiKey.Apostrophe] = (int)KeyCode.Quote,
                io.KeyMap[(int)ImGuiKey.Comma] = (int)KeyCode.Comma,
                io.KeyMap[(int)ImGuiKey.Minus] = (int)KeyCode.Minus,
                io.KeyMap[(int)ImGuiKey.Period] = (int)KeyCode.Period,
                io.KeyMap[(int)ImGuiKey.Slash] = (int)KeyCode.Slash,
                io.KeyMap[(int)ImGuiKey.Semicolon] = (int)KeyCode.Semicolon,
                io.KeyMap[(int)ImGuiKey.Equal] = (int)KeyCode.Equals,
                io.KeyMap[(int)ImGuiKey.LeftBracket] = (int)KeyCode.LeftBracket,
                io.KeyMap[(int)ImGuiKey.Backslash] = (int)KeyCode.Backslash,
                io.KeyMap[(int)ImGuiKey.RightBracket] = (int)KeyCode.RightBracket,
                io.KeyMap[(int)ImGuiKey.GraveAccent] = (int)KeyCode.BackQuote,
                io.KeyMap[(int)ImGuiKey.CapsLock] = (int)KeyCode.CapsLock,
                io.KeyMap[(int)ImGuiKey.ScrollLock] = (int)KeyCode.ScrollLock,
                io.KeyMap[(int)ImGuiKey.NumLock] = (int)KeyCode.Numlock,
                io.KeyMap[(int)ImGuiKey.PrintScreen] = (int)KeyCode.Print,
                io.KeyMap[(int)ImGuiKey.Pause] = (int)KeyCode.Pause,

                io.KeyMap[(int)ImGuiKey.Keypad0] = (int)KeyCode.Keypad0,
                io.KeyMap[(int)ImGuiKey.Keypad1] = (int)KeyCode.Keypad1,
                io.KeyMap[(int)ImGuiKey.Keypad2] = (int)KeyCode.Keypad2,
                io.KeyMap[(int)ImGuiKey.Keypad3] = (int)KeyCode.Keypad3,
                io.KeyMap[(int)ImGuiKey.Keypad4] = (int)KeyCode.Keypad4,
                io.KeyMap[(int)ImGuiKey.Keypad5] = (int)KeyCode.Keypad5,
                io.KeyMap[(int)ImGuiKey.Keypad6] = (int)KeyCode.Keypad6,
                io.KeyMap[(int)ImGuiKey.Keypad7] = (int)KeyCode.Keypad7,
                io.KeyMap[(int)ImGuiKey.Keypad8] = (int)KeyCode.Keypad8,
                io.KeyMap[(int)ImGuiKey.Keypad9] = (int)KeyCode.Keypad9,

                io.KeyMap[(int)ImGuiKey.KeypadDecimal] = (int)KeyCode.KeypadPeriod,
                io.KeyMap[(int)ImGuiKey.KeypadDivide] = (int)KeyCode.KeypadDivide,
                io.KeyMap[(int)ImGuiKey.KeypadMultiply] = (int)KeyCode.KeypadMultiply,
                io.KeyMap[(int)ImGuiKey.KeypadSubtract] = (int)KeyCode.KeypadMinus,
                io.KeyMap[(int)ImGuiKey.KeypadAdd] = (int)KeyCode.KeypadPlus,
                io.KeyMap[(int)ImGuiKey.KeypadEnter] = (int)KeyCode.KeypadEnter,
                io.KeyMap[(int)ImGuiKey.KeypadEqual] = (int)KeyCode.KeypadEquals
            };
        }

        private void UpdateKeyboard(ImGuiIOPtr io)
        {
            for (int keyIndex = 0; keyIndex < _mainKeys.Length; keyIndex++)
            {
                int key = _mainKeys[keyIndex];
                io.KeysDown[key] = Input.GetKey((KeyCode)key);
            }

            // Keyboard modifiers
            io.KeyShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            io.KeyCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            io.KeyAlt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            io.KeySuper = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) ||
                Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows);


            // Text Input get, do it once per frame and store it to share between contexts
            if (Time.frameCount != _lastTextInputFrame)
            {
                _frameTextInput.Clear();
                while (Event.PopEvent(_textInputEvent))
                {
                    if (_textInputEvent.rawType == EventType.KeyDown &&
                        _textInputEvent.character != 0 && _textInputEvent.character != '\n')
                    {
                        _frameTextInput.Add(_textInputEvent.character);
                    }
                }
                _lastTextInputFrame = Time.frameCount;
            }

            // Text input set
            foreach (uint keycode in _frameTextInput)
            {
                io.AddInputCharacter(keycode);
            }
        }

        private void UpdateMouse(ImGuiIOPtr io)
        {
            io.MousePos = Utils.ScreenToImGui(Input.mousePosition);

            io.MouseWheel = Input.mouseScrollDelta.y;
            io.MouseWheelH = Input.mouseScrollDelta.x;

            io.MouseDown[0] = Input.GetMouseButton(0);
            io.MouseDown[1] = Input.GetMouseButton(1);
            io.MouseDown[2] = Input.GetMouseButton(2);
        }
    }
}