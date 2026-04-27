#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !FUMOBILE
#define FUMOBILE
#endif

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
        #region State
        private readonly Event _textInputEvent = new Event();
        private static int _lastTextInputFrame = -1;
        private static readonly List<uint> _frameTextInput = new List<uint>();

        private TouchScreenKeyboard _touchKeyboard;
        private string _lastTouchKeyboardText = string.Empty;
        private bool _wasTextInputActive;

        private static Vector2 _lastTouchPosition;
        private static readonly bool[] _lastFrameMouseState = new bool[5]; // left, right, middle, X1, X2
        private static float _lastPressTime = 0f;
        private static bool _rightClicked = false;
        private static int _nbFramesSinceMouseLeftUp = 0;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Input Manager Platform class.
        /// </summary>
        public InputManagerPlatform() : base()
        { }
        #endregion

        /// <summary>
        /// Initialize the platform bindings for ImGui.
        /// </summary>
        /// <param name="io">ImGuiIOPtr instance to initialize with platform data.</param>
        /// <param name="pio">ImGuiPlatformIOPtr instance to initialize with platform data.</param>
        /// <param name="platformName">Optional name for the platform backend, used for identification.</param>
        /// <returns>True if initialization was successful, false otherwise.</returns>
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
        /// <param name="io">ImGuiIOPtr instance to update with input data.</param>
        /// <param name="displayRect">The rectangle representing the display area for ImGui.</param>
        /// <param name="updateMouse">Whether to update mouse state.</param>
        /// <param name="updateKeyboard">Whether to update keyboard state.</param>
        public override void PrepareFrame(ImGuiIOPtr io, Rect displayRect, bool updateMouse, bool updateKeyboard)
        {
            base.PrepareFrame(io, displayRect, updateMouse, updateKeyboard);

            if (updateKeyboard)
            {
#if FUMOBILE
                UpdateMobileKeyboard(io);
#else
                UpdateKeyboard(io);
#endif
            }

            if (updateMouse)
            {
#if FUMOBILE
                UpdatePointer(io);
#else
                UpdateMouse(io);
#endif
                UpdateCursor(io, ImGui.GetMouseCursor());
            }
        }

        /// <summary>
        /// Update keyboard state and text input for the current frame.
        /// </summary>
        /// <param name="io">ImGuiIOPtr instance to update with keyboard data.</param>
        private void UpdateKeyboard(ImGuiIOPtr io)
        {
            void Key(ImGuiKey imguiKey, KeyCode keyCode)
                => io.AddKeyEvent(imguiKey, Input.GetKey(keyCode));

            io.AddKeyEvent(ImGuiKey.Tab, Input.GetKey(KeyCode.Tab));
            io.AddKeyEvent(ImGuiKey.LeftArrow, Input.GetKey(KeyCode.LeftArrow));
            io.AddKeyEvent(ImGuiKey.RightArrow, Input.GetKey(KeyCode.RightArrow));
            io.AddKeyEvent(ImGuiKey.UpArrow, Input.GetKey(KeyCode.UpArrow));
            io.AddKeyEvent(ImGuiKey.DownArrow, Input.GetKey(KeyCode.DownArrow));
            io.AddKeyEvent(ImGuiKey.PageUp, Input.GetKey(KeyCode.PageUp));
            io.AddKeyEvent(ImGuiKey.PageDown, Input.GetKey(KeyCode.PageDown));
            io.AddKeyEvent(ImGuiKey.Home, Input.GetKey(KeyCode.Home));
            io.AddKeyEvent(ImGuiKey.End, Input.GetKey(KeyCode.End));
            io.AddKeyEvent(ImGuiKey.Insert, Input.GetKey(KeyCode.Insert));
            io.AddKeyEvent(ImGuiKey.Delete, Input.GetKey(KeyCode.Delete));
            io.AddKeyEvent(ImGuiKey.Backspace, Input.GetKey(KeyCode.Backspace));
            io.AddKeyEvent(ImGuiKey.Space, Input.GetKey(KeyCode.Space));
            io.AddKeyEvent(ImGuiKey.Enter, Input.GetKey(KeyCode.Return));
            io.AddKeyEvent(ImGuiKey.KeypadEnter, Input.GetKey(KeyCode.KeypadEnter));
            io.AddKeyEvent(ImGuiKey.Escape, Input.GetKey(KeyCode.Escape));

            Key(ImGuiKey.LeftCtrl, KeyCode.LeftControl);
            Key(ImGuiKey.RightCtrl, KeyCode.RightControl);
            Key(ImGuiKey.LeftShift, KeyCode.LeftShift);
            Key(ImGuiKey.RightShift, KeyCode.RightShift);
            Key(ImGuiKey.LeftAlt, KeyCode.LeftAlt);
            Key(ImGuiKey.RightAlt, KeyCode.RightAlt);
            Key(ImGuiKey.LeftSuper, KeyCode.LeftCommand);
            Key(ImGuiKey.RightSuper, KeyCode.RightCommand);
            Key(ImGuiKey.Menu, KeyCode.Menu);

            Key(ImGuiKey.AppBack, KeyCode.Escape);
            Key(ImGuiKey.AppForward, KeyCode.Return);

            if (Time.frameCount != _lastTextInputFrame)
            {
                _frameTextInput.Clear();

                while (Event.PopEvent(_textInputEvent))
                {
                    if (_textInputEvent.rawType == EventType.KeyDown &&
                        _textInputEvent.character != 0 &&
                        _textInputEvent.character != '\n')
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

            io.AddKeyEvent(ImGuiKey.Z, Input.GetKey(KeyCode.Z));
            io.AddKeyEvent(ImGuiKey.Y, Input.GetKey(KeyCode.Y));
            io.AddKeyEvent(ImGuiKey.C, Input.GetKey(KeyCode.C));
            io.AddKeyEvent(ImGuiKey.V, Input.GetKey(KeyCode.V));
            io.AddKeyEvent(ImGuiKey.X, Input.GetKey(KeyCode.X));
            io.AddKeyEvent(ImGuiKey.A, Input.GetKey(KeyCode.A));

            io.AddKeyEvent(ImGuiKey.ModCtrl, Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
            io.AddKeyEvent(ImGuiKey.ModAlt, Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));
            io.AddKeyEvent(ImGuiKey.ModSuper,
                Input.GetKey(KeyCode.LeftCommand) ||
                Input.GetKey(KeyCode.RightCommand) ||
                Input.GetKey(KeyCode.LeftWindows) ||
                Input.GetKey(KeyCode.RightWindows));
            io.AddKeyEvent(ImGuiKey.ModShift, Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
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
                if (_touchKeyboard == null ||
                    _touchKeyboard.status == TouchScreenKeyboard.Status.Canceled ||
                    _touchKeyboard.status == TouchScreenKeyboard.Status.Done)
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
        /// Updates ImGui pointer state from touchscreen on mobile platforms.
        /// Primary touch is mapped to left mouse and long press is mapped to right click.
        /// </summary>
        /// <param name="io">The ImGui IO pointer.</param>
        private static void UpdatePointer(ImGuiIOPtr io)
        {
            bool hasTouch = Input.touchCount > 0;
            bool leftPressed = false;
            Vector2 position = _lastTouchPosition;

            if (hasTouch)
            {
                Touch touch = Input.GetTouch(0);
                position = touch.position;

                leftPressed = touch.phase != TouchPhase.Ended &&
                              touch.phase != TouchPhase.Canceled;
            }

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

            if (!leftPressed)
            {
                if (_nbFramesSinceMouseLeftUp > 5)
                {
                    position = new Vector2(-1000f, -1000f);
                }
                else
                {
                    position = _lastTouchPosition;
                }

                _nbFramesSinceMouseLeftUp++;
            }
            else
            {
                _lastTouchPosition = position;
                _nbFramesSinceMouseLeftUp = 0;
            }

            io.AddMousePosEvent(position.x, io.DisplaySize.y - position.y);

            io.AddMouseButtonEvent(0, _lastFrameMouseState[0]);
            io.AddMouseButtonEvent(1, _lastFrameMouseState[1]);
            io.AddMouseButtonEvent(2, _lastFrameMouseState[2]);
            io.AddMouseButtonEvent(3, _lastFrameMouseState[3]);
            io.AddMouseButtonEvent(4, _lastFrameMouseState[4]);

            _lastFrameMouseState[0] = leftPressed;
            _lastFrameMouseState[1] = rightPressed;
            _lastFrameMouseState[2] = false;
            _lastFrameMouseState[3] = false;
            _lastFrameMouseState[4] = false;
        }

        /// <summary>
        /// Update mouse position, scroll and buttons state.
        /// </summary>
        /// <param name="io">ImGuiIOPtr instance to update with mouse data.</param>
        private void UpdateMouse(ImGuiIOPtr io)
        {
            Vector2 pos = Input.mousePosition;
            io.AddMousePosEvent(pos.x, io.DisplaySize.y - pos.y);

            Vector2 scroll = Input.mouseScrollDelta * Fugui.Settings.ScrollPower;
            io.AddMouseWheelEvent(scroll.x, scroll.y);

            io.AddMouseButtonEvent(0, Input.GetMouseButton(0));
            io.AddMouseButtonEvent(1, Input.GetMouseButton(1));
            io.AddMouseButtonEvent(2, Input.GetMouseButton(2));
        }
    }
}