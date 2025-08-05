using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fu.Core.DearImGui.Platform
{
    /// <summary>
    /// Platform bindings for ImGui in Unity in charge of: mouse/keyboard/gamepad inputs, cursor shape, timing, windowing.
    /// </summary>
    internal sealed class InputSystemPlatform : PlatformBase
    {
        private readonly List<char> _textInput = new List<char>();
        private int[] _mainKeys;
        private Keyboard _keyboard = null;

        public InputSystemPlatform() : base()
        { }

        /// <summary>
        /// Initialize the ImGui input system, setting up event listeners and configuring ImGui IO.
        /// </summary>
        /// <param name="io"> ImGui IO pointer.</param>
        /// <param name="pio"> ImGui Platform IO pointer.</param>
        /// <returns> True if initialization was successful, false otherwise.</returns>
        public override bool Initialize(ImGuiIOPtr io, ImGuiPlatformIOPtr pio, string platformName = null)
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            base.Initialize(io, pio, "Input System (New)");
            Fugui.Settings.ApplyTo(io);

            unsafe
            {
                PlatformCallbacks.SetClipboardFunctions(PlatformCallbacks.GetClipboardTextCallback, PlatformCallbacks.SetClipboardTextCallback);
            }

            SetupKeyboard(io, Keyboard.current);

            return true;
        }

        /// <summary>
        /// Shutdown the ImGui input system, unregistering event listeners and cleaning up resources.
        /// </summary>
        /// <param name="io"> ImGui IO pointer.</param>
        /// <param name="pio"> ImGui Platform IO pointer.</param>
        public override void Shutdown(ImGuiIOPtr io, ImGuiPlatformIOPtr pio)
        {
            base.Shutdown(io, pio);
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        /// <summary>
        /// Prepare the ImGui frame with the current input state.
        /// </summary>
        /// <param name="io"> ImGui IO pointer.</param>
        /// <param name="displayRect"> The display rectangle for the ImGui context.</param>
        /// <param name="updateMouse"> Whether to update the mouse state.</param>
        /// <param name="updateKeyboard"> Whether to update the keyboard state.</param>
        public override void PrepareFrame(ImGuiIOPtr io, Rect displayRect, bool updateMouse, bool updateKeyboard)
        {
            base.PrepareFrame(io, displayRect, updateMouse, updateKeyboard);

            if (updateKeyboard)
                UpdateKeyboard(io, Keyboard.current);
            if (updateMouse)
                UpdateMouse(io, Mouse.current);

            UpdateCursor(io, ImGui.GetMouseCursor());
            UpdateGamepad(io, Gamepad.current);
        }

        /// <summary>
        /// Update the ImGui input state with the current mouse state.
        /// </summary>
        /// <param name="io"> ImGui IO pointer.</param>
        /// <param name="mouse"> The current mouse instance.</param>
        private static void UpdateMouse(ImGuiIOPtr io, Mouse mouse)
        {
            if (mouse == null) return;

            // Update position and visibility
            if (io.WantSetMousePos)
            {
                mouse.WarpCursorPosition(Utils.ImGuiToScreen(io.MousePos));
            }

            // Cursor position
            io.AddMousePosEvent(mouse.position.x.ReadValue(), io.DisplaySize.y - mouse.position.y.ReadValue());

            // Scroll (120 = 1 "tick" Windows)
            Vector2 mouseScroll = mouse.scroll.ReadValue() * Fugui.Settings.ScrollPower;
            io.AddMouseWheelEvent(mouseScroll.x, mouseScroll.y);

            // Buttons (0 = left, 1 = right, 2 = middle)
            io.AddMouseButtonEvent(0, mouse.leftButton.isPressed);
            io.AddMouseButtonEvent(1, mouse.rightButton.isPressed);
            io.AddMouseButtonEvent(2, mouse.middleButton.isPressed);

            // Optional : support for additional mouse buttons (X1, X2)
            io.AddMouseButtonEvent(3, mouse.backButton?.isPressed ?? false);  // X1
            io.AddMouseButtonEvent(4, mouse.forwardButton?.isPressed ?? false); // X2
        }

        /// <summary>
        /// Update the ImGui input state with the current gamepad state.
        /// </summary>
        /// <param name="io"> ImGui IO pointer.</param>
        /// <param name="gamepad"> The current gamepad instance.</param>
        private static void UpdateGamepad(ImGuiIOPtr io, Gamepad gamepad)
        {
            // Check if gamepad is available and if gamepad navigation is enabled.
            if (gamepad == null || (io.ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) == 0)
            {
                io.BackendFlags &= ~ImGuiBackendFlags.HasGamepad;
                return;
            }

            io.BackendFlags |= ImGuiBackendFlags.HasGamepad;

            // Face buttons
            io.AddKeyAnalogEvent(ImGuiKey.GamepadFaceDown, gamepad.buttonSouth.isPressed, gamepad.buttonSouth.ReadValue()); // A / Cross
            io.AddKeyAnalogEvent(ImGuiKey.GamepadFaceRight, gamepad.buttonEast.isPressed, gamepad.buttonEast.ReadValue());  // B / Circle
            io.AddKeyAnalogEvent(ImGuiKey.GamepadFaceLeft, gamepad.buttonWest.isPressed, gamepad.buttonWest.ReadValue());  // X / Square
            io.AddKeyAnalogEvent(ImGuiKey.GamepadFaceUp, gamepad.buttonNorth.isPressed, gamepad.buttonNorth.ReadValue());// Y / Triangle

            // D-Pad
            io.AddKeyAnalogEvent(ImGuiKey.GamepadDpadLeft, gamepad.dpad.left.isPressed, gamepad.dpad.left.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadDpadRight, gamepad.dpad.right.isPressed, gamepad.dpad.right.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadDpadUp, gamepad.dpad.up.isPressed, gamepad.dpad.up.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadDpadDown, gamepad.dpad.down.isPressed, gamepad.dpad.down.ReadValue());

            // Shoulders
            io.AddKeyAnalogEvent(ImGuiKey.GamepadL1, gamepad.leftShoulder.isPressed, gamepad.leftShoulder.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadR1, gamepad.rightShoulder.isPressed, gamepad.rightShoulder.ReadValue());

            // Triggers (analog)
            io.AddKeyAnalogEvent(ImGuiKey.GamepadL2, gamepad.leftTrigger.ReadValue() > 0f, gamepad.leftTrigger.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadR2, gamepad.rightTrigger.ReadValue() > 0f, gamepad.rightTrigger.ReadValue());

            // Sticks click
            io.AddKeyAnalogEvent(ImGuiKey.GamepadL3, gamepad.leftStickButton.isPressed, gamepad.leftStickButton.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadR3, gamepad.rightStickButton.isPressed, gamepad.rightStickButton.ReadValue());

            // Left Stick directions
            io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickLeft, gamepad.leftStick.left.ReadValue() > 0f, gamepad.leftStick.left.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickRight, gamepad.leftStick.right.ReadValue() > 0f, gamepad.leftStick.right.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickUp, gamepad.leftStick.up.ReadValue() > 0f, gamepad.leftStick.up.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadLStickDown, gamepad.leftStick.down.ReadValue() > 0f, gamepad.leftStick.down.ReadValue());

            // Right Stick directions
            io.AddKeyAnalogEvent(ImGuiKey.GamepadRStickLeft, gamepad.rightStick.left.ReadValue() > 0f, gamepad.rightStick.left.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadRStickRight, gamepad.rightStick.right.ReadValue() > 0f, gamepad.rightStick.right.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadRStickUp, gamepad.rightStick.up.ReadValue() > 0f, gamepad.rightStick.up.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadRStickDown, gamepad.rightStick.down.ReadValue() > 0f, gamepad.rightStick.down.ReadValue());

            // Start / Back
            io.AddKeyAnalogEvent(ImGuiKey.GamepadStart, gamepad.startButton.isPressed, gamepad.startButton.ReadValue());
            io.AddKeyAnalogEvent(ImGuiKey.GamepadBack, gamepad.selectButton.isPressed, gamepad.selectButton.ReadValue());
        }

        /// <summary>
        /// Setup the keyboard input for ImGui.
        /// </summary>
        /// <param name="io"> ImGui IO pointer.</param>
        /// <param name="keyboard"> The current keyboard instance.</param>
        private void SetupKeyboard(ImGuiIOPtr io, Keyboard keyboard)
        {
            if (_keyboard != null)
            {
                _keyboard.onTextInput -= _textInput.Add;
            }

            _keyboard = keyboard;
            _keyboard.onTextInput += _textInput.Add;

            // Stocker uniquement les touches que tu veux suivre dans UpdateKeyboard()
            _mainKeys = new int[] {
                (int)Key.A, (int)Key.C, (int)Key.V, (int)Key.X, (int)Key.Y, (int)Key.Z,
                (int)Key.Tab, (int)Key.LeftArrow, (int)Key.RightArrow,
                (int)Key.UpArrow, (int)Key.DownArrow, (int)Key.PageUp, (int)Key.PageDown,
                (int)Key.Home, (int)Key.End, (int)Key.Insert, (int)Key.Delete,
                (int)Key.Backspace, (int)Key.Space, (int)Key.Escape, (int)Key.Enter,
                (int)Key.NumpadEnter
            };
        }

        /// <summary>
        /// Update the ImGui input state with the current keyboard state.
        /// </summary>
        /// <param name="io"> ImGui IO pointer.</param>
        /// <param name="keyboard"> The current keyboard instance.</param>
        private void UpdateKeyboard(ImGuiIOPtr io, Keyboard keyboard)
        {
            if (keyboard == null) return;

            // Remplacer les touches principales
            io.AddKeyEvent(ImGuiKey.A, keyboard[Key.A].isPressed);
            io.AddKeyEvent(ImGuiKey.C, keyboard[Key.C].isPressed);
            io.AddKeyEvent(ImGuiKey.V, keyboard[Key.V].isPressed);
            io.AddKeyEvent(ImGuiKey.X, keyboard[Key.X].isPressed);
            io.AddKeyEvent(ImGuiKey.Y, keyboard[Key.Y].isPressed);
            io.AddKeyEvent(ImGuiKey.Z, keyboard[Key.Z].isPressed);

            io.AddKeyEvent(ImGuiKey.Tab, keyboard[Key.Tab].isPressed);
            io.AddKeyEvent(ImGuiKey.LeftArrow, keyboard[Key.LeftArrow].isPressed);
            io.AddKeyEvent(ImGuiKey.RightArrow, keyboard[Key.RightArrow].isPressed);
            io.AddKeyEvent(ImGuiKey.UpArrow, keyboard[Key.UpArrow].isPressed);
            io.AddKeyEvent(ImGuiKey.DownArrow, keyboard[Key.DownArrow].isPressed);
            io.AddKeyEvent(ImGuiKey.PageUp, keyboard[Key.PageUp].isPressed);
            io.AddKeyEvent(ImGuiKey.PageDown, keyboard[Key.PageDown].isPressed);
            io.AddKeyEvent(ImGuiKey.Home, keyboard[Key.Home].isPressed);
            io.AddKeyEvent(ImGuiKey.End, keyboard[Key.End].isPressed);
            io.AddKeyEvent(ImGuiKey.Insert, keyboard[Key.Insert].isPressed);
            io.AddKeyEvent(ImGuiKey.Delete, keyboard[Key.Delete].isPressed);
            io.AddKeyEvent(ImGuiKey.Backspace, keyboard[Key.Backspace].isPressed);
            io.AddKeyEvent(ImGuiKey.Space, keyboard[Key.Space].isPressed);
            io.AddKeyEvent(ImGuiKey.Escape, keyboard[Key.Escape].isPressed);
            io.AddKeyEvent(ImGuiKey.Enter, keyboard[Key.Enter].isPressed);
            io.AddKeyEvent(ImGuiKey.KeypadEnter, keyboard[Key.NumpadEnter].isPressed);

            // Modifiers
            io.AddKeyEvent(ImGuiKey.LeftShift, keyboard[Key.LeftShift].isPressed);
            io.AddKeyEvent(ImGuiKey.RightShift, keyboard[Key.RightShift].isPressed);
            io.AddKeyEvent(ImGuiKey.LeftCtrl, keyboard[Key.LeftCtrl].isPressed);
            io.AddKeyEvent(ImGuiKey.RightCtrl, keyboard[Key.RightCtrl].isPressed);
            io.AddKeyEvent(ImGuiKey.LeftAlt, keyboard[Key.LeftAlt].isPressed);
            io.AddKeyEvent(ImGuiKey.RightAlt, keyboard[Key.RightAlt].isPressed);
            io.AddKeyEvent(ImGuiKey.LeftSuper, keyboard[Key.LeftMeta].isPressed);
            io.AddKeyEvent(ImGuiKey.RightSuper, keyboard[Key.RightMeta].isPressed);

            // Text input
            for (int i = 0; i < _textInput.Count; i++)
            {
                io.AddInputCharacter(_textInput[i]);
            }

            _textInput.Clear();
        }

        /// <summary>
        /// Handle device changes, specifically for keyboard layout changes or device changes.
        /// </summary>
        /// <param name="device"> The input device that changed.</param>
        /// <param name="chang e"> The type of change that occurred.</param>
        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device is Keyboard keyboard)
            {
                // Keyboard layout change, remap main keys.
                if (change == InputDeviceChange.ConfigurationChanged)
                {
                    SetupKeyboard(ImGui.GetIO(), keyboard);
                }

                // Keyboard device changed, setup again.
                if (Keyboard.current != _keyboard)
                {
                    SetupKeyboard(ImGui.GetIO(), Keyboard.current);
                }
            }
        }
    }
}