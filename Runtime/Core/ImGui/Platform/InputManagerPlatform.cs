using Fu.Core.DearImGui.Assets;
using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Core.DearImGui.Platform
{
    /// <summary>
    /// Platform bindings for ImGui in Unity in charge of: mouse/keyboard/gamepad inputs, cursor shape, timing, windowing.
    /// </summary>
    internal sealed class InputManagerPlatform : PlatformBase
    {
        private readonly Event _textInputEvent = new Event();
        private static int _lastTextInputFrame = -1;
        private static List<uint> _frameTextInput = new List<uint>();

        public InputManagerPlatform(CursorShapesAsset cursorShapes, IniSettingsAsset iniSettings) :
            base(cursorShapes, iniSettings)
        { }

        /// <summary>
        /// Initialize the platform bindings for ImGui.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to initialize with platform data.</param>
        /// <param name="platformName"> Name of the platform, used for logging or identification.</param>
        /// <returns> True if initialization was successful, false otherwise.</returns>
        public override bool Initialize(ImGuiIOPtr io, ImGuiPlatformIOPtr pio, string platformName)
        {
            base.Initialize(io, pio, platformName);
            Fugui.Settings.ApplyTo(io);
            return true;
        }

        /// <summary>
        /// Prepare the ImGui frame with mouse and keyboard input.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to update with input data.</param>
        /// <param name="displayRect"> The rectangle representing the display area for ImGui.</param>
        /// <param name="updateMouse"> Whether to update mouse state.</param>
        /// <param name="updateKeyboard"> Whether to update keyboard state.</param>
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

        /// <summary>
        /// Update keyboard state and text input for the current frame.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to update with keyboard data.</param>
        private void UpdateKeyboard(ImGuiIOPtr io)
        {
            // Helper
            void Key(ImGuiKey imguiKey, KeyCode keyCode)
                => io.AddKeyEvent(imguiKey, Input.GetKey(keyCode));

            // Letters
            for (int i = 0; i < 26; i++)
                Key((ImGuiKey)((int)ImGuiKey.A + i), KeyCode.A + i);

            // Numbers
            for (int i = 0; i <= 9; i++)
                Key((ImGuiKey)((int)ImGuiKey._0 + i), KeyCode.Alpha0 + i);

            // Function keys
            for (int i = 1; i <= 12; i++)
                Key((ImGuiKey)((int)ImGuiKey.F1 + (i - 1)), KeyCode.F1 + (i - 1));
            // Arrows and navigation
            Key(ImGuiKey.Tab, KeyCode.Tab);
            Key(ImGuiKey.LeftArrow, KeyCode.LeftArrow);
            Key(ImGuiKey.RightArrow, KeyCode.RightArrow);
            Key(ImGuiKey.UpArrow, KeyCode.UpArrow);
            Key(ImGuiKey.DownArrow, KeyCode.DownArrow);
            Key(ImGuiKey.PageUp, KeyCode.PageUp);
            Key(ImGuiKey.PageDown, KeyCode.PageDown);
            Key(ImGuiKey.Home, KeyCode.Home);
            Key(ImGuiKey.End, KeyCode.End);
            Key(ImGuiKey.Insert, KeyCode.Insert);
            Key(ImGuiKey.Delete, KeyCode.Delete);
            Key(ImGuiKey.Backspace, KeyCode.Backspace);
            Key(ImGuiKey.Space, KeyCode.Space);
            Key(ImGuiKey.Enter, KeyCode.Return);
            Key(ImGuiKey.Escape, KeyCode.Escape);

            // Modifiers
            Key(ImGuiKey.LeftCtrl, KeyCode.LeftControl);
            Key(ImGuiKey.RightCtrl, KeyCode.RightControl);
            Key(ImGuiKey.LeftShift, KeyCode.LeftShift);
            Key(ImGuiKey.RightShift, KeyCode.RightShift);
            Key(ImGuiKey.LeftAlt, KeyCode.LeftAlt);
            Key(ImGuiKey.RightAlt, KeyCode.RightAlt);
            Key(ImGuiKey.LeftSuper, KeyCode.LeftCommand);   // or LeftWindows
            Key(ImGuiKey.RightSuper, KeyCode.RightCommand); // or RightWindows
            Key(ImGuiKey.Menu, KeyCode.Menu);

            // Keypad
            for (int i = 0; i <= 9; i++)
                Key((ImGuiKey)((int)ImGuiKey.Keypad0 + i), KeyCode.Keypad0 + i);

            Key(ImGuiKey.KeypadDecimal, KeyCode.KeypadPeriod);
            Key(ImGuiKey.KeypadDivide, KeyCode.KeypadDivide);
            Key(ImGuiKey.KeypadMultiply, KeyCode.KeypadMultiply);
            Key(ImGuiKey.KeypadSubtract, KeyCode.KeypadMinus);
            Key(ImGuiKey.KeypadAdd, KeyCode.KeypadPlus);
            Key(ImGuiKey.KeypadEnter, KeyCode.KeypadEnter);
            Key(ImGuiKey.KeypadEqual, KeyCode.KeypadEquals);

            // Symbols
            Key(ImGuiKey.Apostrophe, KeyCode.Quote);
            Key(ImGuiKey.Comma, KeyCode.Comma);
            Key(ImGuiKey.Minus, KeyCode.Minus);
            Key(ImGuiKey.Period, KeyCode.Period);
            Key(ImGuiKey.Slash, KeyCode.Slash);
            Key(ImGuiKey.Semicolon, KeyCode.Semicolon);
            Key(ImGuiKey.Equal, KeyCode.Equals);
            Key(ImGuiKey.LeftBracket, KeyCode.LeftBracket);
            Key(ImGuiKey.Backslash, KeyCode.Backslash);
            Key(ImGuiKey.RightBracket, KeyCode.RightBracket);
            Key(ImGuiKey.GraveAccent, KeyCode.BackQuote);

            // Toggles
            Key(ImGuiKey.CapsLock, KeyCode.CapsLock);
            Key(ImGuiKey.ScrollLock, KeyCode.ScrollLock);
            Key(ImGuiKey.NumLock, KeyCode.Numlock);
            Key(ImGuiKey.PrintScreen, KeyCode.Print);
            Key(ImGuiKey.Pause, KeyCode.Pause);

            // App navigation (optional)
            Key(ImGuiKey.AppBack, KeyCode.Escape);   // Or JoystickButton6
            Key(ImGuiKey.AppForward, KeyCode.Return); // Or JoystickButton7

            // Text input (UTF16)
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

            foreach (uint character in _frameTextInput)
            {
                io.AddInputCharacter((ushort)character);
            }
        }

        /// <summary>
        /// Update mouse position, scroll and buttons state.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to update with mouse data.</param>
        private void UpdateMouse(ImGuiIOPtr io)
        {
            // Position
            Vector2 pos = Input.mousePosition;
            io.AddMousePosEvent(pos.x, pos.y);

            // Scroll
            Vector2 scroll = Input.mouseScrollDelta * Fugui.Settings.ScrollPower;
            io.AddMouseWheelEvent(scroll.x, scroll.y);

            // Mouse buttons
            io.AddMouseButtonEvent(0, Input.GetMouseButton(0)); // Left
            io.AddMouseButtonEvent(1, Input.GetMouseButton(1)); // Right
            io.AddMouseButtonEvent(2, Input.GetMouseButton(2)); // Middle
        }
    }
}