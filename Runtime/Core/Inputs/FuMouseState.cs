using ImGuiNET;
using System.Collections.Generic;
using UnityEngine;

namespace Fu
{
    /// <summary>
    /// struct that represent mouse states for an UIWindow
    /// </summary>
    public class FuMouseState
    {
        /// <summary>
        /// button states by buttons (0 is left, 1 is right)
        /// </summary>
        internal FuButtonState[] ButtonStates;
        private readonly FuButtonState[] _virtualButtonStates;
        private Vector2 _movement;
        public Vector2 Movement { get { return _movement; } }
        private Vector2 _wheel;
        public Vector2 Wheel { get { return _wheel; } }
        private Vector2Int _position;
        public Vector2Int Position { get { return _position; } }
        private bool _isHoverOverlay;
        public bool IsHoverOverlay { get { return _isHoverOverlay; } }
        private bool _isHoverPupUp;
        public bool IsHoverPopup { get { return _isHoverPupUp; } }
        private bool _isHoverTopBar;
        public bool IsHoverTopBar { get { return _isHoverTopBar; } }
        private bool _isHoverBottomBar;
        public bool IsHoverBottomBar { get { return _isHoverBottomBar; } }
        private static readonly HashSet<int> _currentPressedKeys = new HashSet<int>();

        /// <summary>
        /// instantiate a new UIMouseState and init mouse Buttons array
        /// </summary>
        public FuMouseState()
        {
            _position = Vector2Int.zero;
            _movement = Vector2.zero;
            _wheel = Vector2.zero;

            ButtonStates = new FuButtonState[3];
            ButtonStates[0] = new FuButtonState(0);
            ButtonStates[1] = new FuButtonState(1);
            ButtonStates[2] = new FuButtonState(2);

            _virtualButtonStates = new FuButtonState[3];
            _virtualButtonStates[0] = new FuButtonState(0);
            _virtualButtonStates[1] = new FuButtonState(1);
            _virtualButtonStates[2] = new FuButtonState(2);
        }

        /// <summary>
        /// set current mouse wheel
        /// </summary>
        /// <param name="wheel">wheel value</param>
        internal void SetWheel(Vector2 wheel)
        {
            _wheel = wheel;
        }

        /// <summary>
        /// set current mouse position
        /// </summary>
        /// <param name="window">window to set mouse position on</param>
        internal void SetPosition(FuWindow window)
        {
            if (window.Container == null)
            {
                return;
            }
            Vector2Int position = window.Container.LocalMousePos - window.LocalPosition;
            _movement = new Vector2(position.x, position.y) - new Vector2(Position.x, Position.y);
            _movement.y = -_movement.y;
            _position = position;
            _isHoverOverlay = false;
            // check whatever mouse is hover any overlay
            foreach (FuOverlay overlay in window.Overlays.Values)
            {
                _isHoverOverlay |= overlay.LocalRect.Contains(position);
            }
            _isHoverPupUp = Fugui.IsInsideAnyPopup(window.Container.LocalMousePos);
            _isHoverTopBar = window.UIHeader != null && window.TopBarHeight > 0f && position.y <= window.TopBarHeight + (window.WorkingAreaPosition.y - window.LocalPosition.y);
            _isHoverBottomBar = window.UIFooter != null && window.BottomBarHeight > 0f && position.y >= window.WorkingAreaSize.y + window.WorkingAreaPosition.y - window.BottomBarHeight;
        }

        /// <summary>
        /// set current mouse data
        /// </summary>
        /// <param name="container">container to set mouse position on</param>
        internal void UpdateState(IFuWindowContainer container)
        {
            _movement = new Vector2(container.LocalMousePos.x, container.LocalMousePos.y) - new Vector2(Position.x, Position.y);
            _movement.y = -_movement.y;
            _position = container.LocalMousePos;
            _isHoverOverlay = false;
            _isHoverPupUp = false;
            _isHoverTopBar = false;
            _isHoverBottomBar = false;

            // set container mouse buttons states
            bool btn0State = ImGuiNative.igIsMouseDown_Nil(ImGuiMouseButton.Left) != 0;
            bool btn1State = ImGuiNative.igIsMouseDown_Nil(ImGuiMouseButton.Right) != 0;
            bool btn2State = ImGuiNative.igIsMouseDown_Nil(ImGuiMouseButton.Middle) != 0;

            ButtonStates[0].SetState(btn0State, _position);
            ButtonStates[1].SetState(btn1State, _position);
            ButtonStates[2].SetState(btn2State, _position);

            // check whatever mouse is hover any overlay
            container.OnEachWindow((window) =>
            {
                _isHoverOverlay |= window.Mouse.IsHoverOverlay;
                _isHoverPupUp |= window.Mouse.IsHoverPopup;
                _isHoverTopBar |= window.Mouse.IsHoverTopBar;
                _isHoverBottomBar |= window.Mouse._isHoverBottomBar;
            });
        }

        /// <summary>
        /// set current mouse data
        /// </summary>
        /// <param name="window">window to set mouse position and button states on</param>
        internal void UpdateState(FuWindow window)
        {
            bool btn0State = ImGuiNative.igIsMouseDown_Nil(ImGuiMouseButton.Left) != 0;
            bool btn1State = ImGuiNative.igIsMouseDown_Nil(ImGuiMouseButton.Right) != 0;
            bool btn2State = ImGuiNative.igIsMouseDown_Nil(ImGuiMouseButton.Middle) != 0;

            // set brut states, without handling focus/hover and clicked window
            _virtualButtonStates[0].SetState(btn0State, Vector2Int.zero);
            _virtualButtonStates[1].SetState(btn1State, Vector2Int.zero);
            _virtualButtonStates[2].SetState(btn2State, Vector2Int.zero);

            // check if a button is Down this frame if this window is hover and no window has been clicked for now
            if ((FuWindow.InputFocusedWindow == null || FuWindow.InputFocusedWindow == window) && window.IsHovered)
            {
                for (int i = 0; i < ButtonStates.Length; i++)
                {
                    if (_virtualButtonStates[i].IsDown)
                    {
                        FuWindow.InputFocusedWindow = window;
                        // increase quantity of input holding the current input focused window
                        FuWindow.NbInputFocusedWindow++;
                        _currentPressedKeys.Add(i);
                    }
                }
            }

            // check if pressed button is Up this frame and pressed window is me
            if (_currentPressedKeys.Count > 0 && FuWindow.InputFocusedWindow == window)
            {
                for (int i = 0; i < ButtonStates.Length; i++)
                {
                    if (_currentPressedKeys.Contains(i) && _virtualButtonStates[i].IsUp)
                    {
                        // increase quantity of input holding the current input focused window
                        FuWindow.NbInputFocusedWindow--;
                        _currentPressedKeys.Remove(i);
                    }
                }
            }

            // no window has been pressed for now, let's just set states regulary
            if (FuWindow.InputFocusedWindow == null)
            {
                ButtonStates[0].SetState(window.IsHovered && btn0State, _position);
                ButtonStates[1].SetState(window.IsHovered && btn1State, _position);
                ButtonStates[2].SetState(window.IsHovered && btn2State, _position);
            }
            // a window is pressed, only this one should retrieve mouse inputs
            else
            {
                // we are the pressed window
                if (FuWindow.InputFocusedWindow == window)
                {
                    ButtonStates[0].SetState(btn0State, _position);
                    ButtonStates[1].SetState(btn1State, _position);
                    ButtonStates[2].SetState(btn2State, _position);
                }
                // we are another window, let's not handle inputs
                else
                {
                    ButtonStates[0].SetState(false, _position);
                    ButtonStates[1].SetState(false, _position);
                    ButtonStates[2].SetState(false, _position);
                }
            }

            // set mouse pos and wheel
            SetPosition(window);
            SetWheel(new Vector2(window.Container.Context.IO.MouseWheelH, window.Container.Context.IO.MouseWheel));
        }

        /// <summary>
        /// is a mouse button just down this frame
        /// </summary>
        /// <param name="mouseButton">Mouse button to check</param>
        /// <returns>true if down</returns>
        public bool IsDown(FuMouseButton mouseButton)
        {
            if (mouseButton == FuMouseButton.None)
            {
                return false;
            }
            return ButtonStates[(int)mouseButton].IsDown;
        }

        /// <summary>
        /// is a mouse button just up this frame
        /// </summary>
        /// <param name="mouseButton">Mouse button to check</param>
        /// <returns>true if up</returns>
        public bool IsUp(FuMouseButton mouseButton)
        {
            if (mouseButton == FuMouseButton.None)
            {
                return false;
            }
            return ButtonStates[(int)mouseButton].IsUp;
        }

        /// <summary>
        /// is a mouse button is currently pressed
        /// </summary>
        /// <param name="mouseButton">Mouse button to check</param>
        /// <returns>true if pressed</returns>
        public bool IsPressed(FuMouseButton mouseButton)
        {
            if (mouseButton == FuMouseButton.None)
            {
                return false;
            }
            return ButtonStates[(int)mouseButton].IsPressed;
        }

        /// <summary>
        /// is a mouse button is currently pressed
        /// </summary>
        /// <param name="mouseButton">Mouse button to check</param>
        /// <returns>true if pressed</returns>
        public bool IsClicked(FuMouseButton mouseButton)
        {
            if (mouseButton == FuMouseButton.None)
            {
                return false;
            }
            return ButtonStates[(int)mouseButton].IsUp && ButtonStates[(int)mouseButton].PressedMovement.magnitude <= Fugui.Settings.ClickMaxDist;
        }

        /// <summary>
        /// Get the movement the mouse has done since button is Down and Up (durring the Press operation)
        /// </summary>
        /// <param name="mouseButton">Mouse button to check</param>
        /// <returns>Vector2Int that represent the mouse movement</returns>
        public Vector2Int GetPressedMovement(FuMouseButton mouseButton)
        {
            if (mouseButton == FuMouseButton.None)
            {
                return default;
            }
            return ButtonStates[(int)mouseButton].PressedMovement;
        }
    }
}