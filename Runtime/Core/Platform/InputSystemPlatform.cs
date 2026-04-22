#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif
using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Fu
{
    /// <summary>
    /// Platform bindings for ImGui in Unity in charge of: mouse/keyboard/gamepad inputs, cursor shape, timing, windowing.
    /// </summary>
    internal sealed class InputSystemPlatform : PlatformBase
    {
        private readonly List<char> _textInput = new List<char>();
        private Keyboard _keyboard = null;
        private Dictionary<ImGuiKey, KeyControl> _keyControls;
        private TouchScreenKeyboard _touchKeyboard;
        private string _lastTouchKeyboardText = string.Empty;
        private bool _wasTextInputActive;
        private static Vector2 _lastTouchPosition;
        private static bool[] _lastFrameMouseState = new bool[5]; // left, right, middle, X1, X2
        private static float _lastPressTime = 0f;
        private static bool _rightClicked = false;

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
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
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
            {
#if FUMOBILE
                UpdateMobileKeyboard(io);
#else
                UpdateKeyboard(io, Keyboard.current);
#endif
            }
            if (updateMouse)
            {
#if FUMOBILE
                UpdatePointer(io);
#else
                UpdateMouse(io, Mouse.current);
#endif
            }

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

            //bool simulateMobileBehaviour = false;
            //if(simulateMobileBehaviour)
            //{
            //    // Position
            //    Vector2 position = new Vector2(
            //        mouse.position.x.ReadValue(),
            //        io.DisplaySize.y - mouse.position.y.ReadValue()
            //    );

            //    // Handle right-click emulation: if the primary touch is held for more than 2 seconds, trigger a right-click event.
            //    bool leftPressed = mouse.leftButton.isPressed;
            //    if (leftPressed && !_lastFrameMouseState[0])
            //    {
            //        _lastPressTime = 0f;
            //        _rightClicked = false;
            //    }
            //    if (!leftPressed && _lastFrameMouseState[0])
            //    {
            //        _lastPressTime = 0f;
            //        _rightClicked = false;
            //    }
            //    if (_lastFrameMouseState[0] && leftPressed)
            //    {
            //        _lastPressTime += Time.deltaTime;
            //    }
            //    bool rightPressed = false;
            //    if (!_rightClicked && leftPressed && _lastPressTime >= 1.0f)
            //    {
            //        _rightClicked = true;
            //        rightPressed = true;
            //    }

            //    if (!leftPressed)
            //    {
            //        position = _lastTouchPosition;
            //    }
            //    else
            //    {
            //        _lastTouchPosition = position;
            //    }

            //    // Cursor position
            //    io.AddMousePosEvent(position.x, position.y);

            //    // Scroll (120 = 1 "tick" Windows)
            //    Vector2 mouseScroll = mouse.scroll.ReadValue() * Fugui.Settings.ScrollPower;
            //    io.AddMouseWheelEvent(mouseScroll.x, mouseScroll.y);

            //    // Buttons (0 = left, 1 = right, 2 = middle)
            //    io.AddMouseButtonEvent(0, _lastFrameMouseState[0]);
            //    io.AddMouseButtonEvent(1, _lastFrameMouseState[1]);
            //    io.AddMouseButtonEvent(2, _lastFrameMouseState[2]);

            //    // Optional : support for additional mouse buttons (X1, X2)
            //    io.AddMouseButtonEvent(3, _lastFrameMouseState[3]); // X1
            //    io.AddMouseButtonEvent(4, _lastFrameMouseState[4]); // X2

            //    _lastFrameMouseState[0] = leftPressed;
            //    _lastFrameMouseState[1] = rightPressed;
            //    _lastFrameMouseState[2] = false;
            //    _lastFrameMouseState[3] = false;
            //    _lastFrameMouseState[4] = false;
            //}
            //else
            //{
                // Update position and visibility
                if (io.WantSetMousePos)
                {
                    mouse.WarpCursorPosition(new Vector2(io.MousePos.x, ImGui.GetIO().DisplaySize.y - io.MousePos.y));
                }

                // Position
                Vector2 position = new Vector2(
                    mouse.position.x.ReadValue(),
                    io.DisplaySize.y - mouse.position.y.ReadValue()
                );

                // Cursor position
                io.AddMousePosEvent(position.x, position.y);

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
            //}
        }

        /// <summary>
        /// Updates ImGui pointer state from mouse or touchscreen.
        /// Mouse is used on desktop, primary touch is mapped to left mouse on mobile.
        /// </summary>
        /// <param name="io">The ImGui IO pointer.</param>
        private static void UpdatePointer(ImGuiIOPtr io)
        {
            Touchscreen touch = Touchscreen.current;
            if (touch == null)
            {
                return;
            }

            TouchControl primaryTouch = touch.primaryTouch;
            if (primaryTouch == null)
            {
                return;
            }

            bool leftPressed = primaryTouch.press.isPressed;

            // Handle right-click emulation: if the primary touch is held for more than 2 seconds, trigger a right-click event.
            if (leftPressed && !_lastFrameMouseState[0])
            {
                _lastPressTime = 0f;
                _rightClicked = false;
            }
            if (!leftPressed && _lastFrameMouseState[0])
            {
                _lastPressTime = 0f;
                _rightClicked = false;
            }
            if (_lastFrameMouseState[0] && leftPressed)
            {
                _lastPressTime += Time.deltaTime;
            }
            bool rightPressed = false;
            if (!_rightClicked && leftPressed && _lastPressTime >= 1.0f)
            {
                _rightClicked = true;
                rightPressed = true;
            }

            // Cursor position
            Vector2 position = primaryTouch.position.ReadValue();
            io.AddMousePosEvent(position.x, io.DisplaySize.y - position.y);

            // Buttons (0 = left, 1 = right, 2 = middle)
            io.AddMouseButtonEvent(0, _lastFrameMouseState[0]);
            io.AddMouseButtonEvent(1, _lastFrameMouseState[1]);
            io.AddMouseButtonEvent(2, _lastFrameMouseState[2]);

            // Optional : support for additional mouse buttons (X1, X2)
            io.AddMouseButtonEvent(3, _lastFrameMouseState[3]); // X1
            io.AddMouseButtonEvent(4, _lastFrameMouseState[4]); // X2

            _lastFrameMouseState[0] = leftPressed;
            _lastFrameMouseState[1] = rightPressed;
            _lastFrameMouseState[2] = false;
            _lastFrameMouseState[3] = false;
            _lastFrameMouseState[4] = false;
        }

        /// <summary>
        /// Updates the on-screen keyboard on mobile platforms and forwards typed characters to ImGui.
        /// </summary>
        /// <param name="io">The ImGui IO pointer.</param>
        private void UpdateMobileKeyboard(ImGuiIOPtr io)
        {
#if UNITY_ANDROID || UNITY_IOS
            bool wantsTextInput = io.WantTextInput;

            if (wantsTextInput)
            {
                if (_touchKeyboard == null || _touchKeyboard.status == TouchScreenKeyboard.Status.Canceled || _touchKeyboard.status == TouchScreenKeyboard.Status.Done)
                {
                    _touchKeyboard = TouchScreenKeyboard.Open(
                        _lastTouchKeyboardText,
                        TouchScreenKeyboardType.Default,
                        false,
                        true,
                        false,
                        false);
                }

                if (_touchKeyboard != null)
                {
                    string currentText = _touchKeyboard.text ?? string.Empty;

                    if (currentText.Length > _lastTouchKeyboardText.Length)
                    {
                        for (int i = _lastTouchKeyboardText.Length; i < currentText.Length; i++)
                        {
                            io.AddInputCharacter(currentText[i]);
                        }
                    }
                    else if (currentText.Length < _lastTouchKeyboardText.Length)
                    {
                        int removedCount = _lastTouchKeyboardText.Length - currentText.Length;
                        for (int i = 0; i < removedCount; i++)
                        {
                            io.AddKeyEvent(ImGuiKey.Backspace, true);
                            io.AddKeyEvent(ImGuiKey.Backspace, false);
                        }
                    }

                    _lastTouchKeyboardText = currentText;

                    if (_touchKeyboard.status == TouchScreenKeyboard.Status.Done ||
                        _touchKeyboard.status == TouchScreenKeyboard.Status.Canceled ||
                        _touchKeyboard.status == TouchScreenKeyboard.Status.LostFocus)
                    {
                        _touchKeyboard = null;
                    }
                }
            }
            else
            {
                if (_touchKeyboard != null)
                {
                    _touchKeyboard.active = false;
                    _touchKeyboard = null;
                }

                _lastTouchKeyboardText = string.Empty;
            }

            _wasTextInputActive = wantsTextInput;
#endif
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

            // get the current keyboard layout and map main keys (regardless of layout => azert, qwerty, etc.)
            _keyControls = new Dictionary<ImGuiKey, KeyControl>
            {
                { ImGuiKey.A, keyboard.FindKeyOnCurrentKeyboardLayout("a") },
                { ImGuiKey.C, keyboard.FindKeyOnCurrentKeyboardLayout("c") },
                { ImGuiKey.V, keyboard.FindKeyOnCurrentKeyboardLayout("v") },
                { ImGuiKey.X, keyboard.FindKeyOnCurrentKeyboardLayout("x") },
                { ImGuiKey.Y, keyboard.FindKeyOnCurrentKeyboardLayout("y") },
                { ImGuiKey.Z, keyboard.FindKeyOnCurrentKeyboardLayout("z") }
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

            // handle A, C, V, X, Y, Z keys
            if (_keyControls != null)
            {
                foreach (var kvp in _keyControls)
                {
                    io.AddKeyEvent(kvp.Key, kvp.Value.isPressed);
                }
            }

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

            // Handle Ctrl, Alt, Super, and Shift keys
            io.AddKeyEvent(ImGuiKey.ModCtrl, keyboard[Key.LeftCtrl].isPressed || keyboard[Key.RightCtrl].isPressed);
            io.AddKeyEvent(ImGuiKey.ModAlt, keyboard[Key.LeftAlt].isPressed || keyboard[Key.RightAlt].isPressed);
            io.AddKeyEvent(ImGuiKey.ModSuper, keyboard[Key.LeftMeta].isPressed || keyboard[Key.RightMeta].isPressed);
            io.AddKeyEvent(ImGuiKey.ModShift, keyboard[Key.LeftShift].isPressed || keyboard[Key.RightShift].isPressed);
        }

        /// <summary>
        /// Handle device changes, specifically for keyboard layout changes or device changes.
        /// </summary>
        /// <param name="device"> The input device that changed.</param>
        /// <param name="change"> The type of change that occurred.</param>
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