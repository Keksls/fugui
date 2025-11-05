using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// Platform bindings for ImGui in Unity in charge of: mouse/keyboard/gamepad inputs, cursor shape, timing, windowing.
    /// </summary>
    internal sealed class InputManagerPlatform : PlatformBase
    {
        private readonly Event _textInputEvent = new Event();
        private static int _lastTextInputFrame = -1;
        private static List<uint> _frameTextInput = new List<uint>();

        public InputManagerPlatform() : base()
        { }

        /// <summary>
        /// Initialize the platform bindings for ImGui.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to initialize with platform data.</param>
        /// <param name="pio"> ImGuiPlatformIOPtr instance to initialize with platform data.</param>
        /// <param name="platformName"> Optional name for the platform backend, used for identification.</param>
        /// <returns> True if initialization was successful, false otherwise.</returns>
        public override bool Initialize(ImGuiIOPtr io, ImGuiPlatformIOPtr pio, string platformName = null)
        {
            base.Initialize(io, pio, "Input Manager (Old)");
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
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

            // Register A, Z, C, V, X keys for copy, paste, cut, undo
            io.AddKeyEvent(ImGuiKey.Z, Input.GetKey(KeyCode.Z));
            io.AddKeyEvent(ImGuiKey.Y, Input.GetKey(KeyCode.Y));
            io.AddKeyEvent(ImGuiKey.C, Input.GetKey(KeyCode.C));
            io.AddKeyEvent(ImGuiKey.V, Input.GetKey(KeyCode.V));
            io.AddKeyEvent(ImGuiKey.X, Input.GetKey(KeyCode.X));
            io.AddKeyEvent(ImGuiKey.A, Input.GetKey(KeyCode.A));

            // Handle Ctrl, Alt, Super, and Shift keys
            io.AddKeyEvent(ImGuiKey.ModCtrl, Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
            io.AddKeyEvent(ImGuiKey.ModAlt, Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
            io.AddKeyEvent(ImGuiKey.ModSuper, Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) ||
                                                     Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows));
            io.AddKeyEvent(ImGuiKey.ModShift, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }

        /// <summary>
        /// Update mouse position, scroll and buttons state.
        /// </summary>
        /// <param name="io"> ImGuiIOPtr instance to update with mouse data.</param>
        private void UpdateMouse(ImGuiIOPtr io)
        {
            // Position
            Vector2 pos = Input.mousePosition;
            io.AddMousePosEvent(pos.x, io.DisplaySize.y - pos.y);

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