using Fugui.Framework;
using UnityEngine;

namespace Fugui.Core
{
    /// <summary>
    /// struct that represent mouse states for an UIWindow
    /// </summary>
    public class UIMouseState
    {
        /// <summary>
        /// button states by buttons (0 is left, 1 is right)
        /// </summary>
        internal UIMouseButtonState[] ButtonStates;
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

        /// <summary>
        /// instantiate a new UIMouseState and init mouse Buttons array
        /// </summary>
        public UIMouseState()
        {
            _position = Vector2Int.zero;
            _movement = Vector2.zero;
            _wheel = Vector2.zero;
            ButtonStates = new UIMouseButtonState[2];
            ButtonStates[0] = new UIMouseButtonState();
            ButtonStates[1] = new UIMouseButtonState();
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
        internal void SetPosition(UIWindow window)
        {
            if(window.Container == null)
            {
                return;
            }
            Vector2Int position = window.Container.LocalMousePos - window.LocalPosition;
            _movement = new Vector2(position.x, position.y) - new Vector2(Position.x, Position.y);
            _movement.y = -_movement.y;
            _position = position;
            _isHoverOverlay = false;
            // check whatever mouse is hover any overlay
            foreach (UIOverlay overlay in window.Overlays.Values)
            {
                _isHoverOverlay |= overlay.LocalRect.Contains(position);
            }
            _isHoverPupUp = !string.IsNullOrEmpty(UILayout.CurrentPopUpID) ? UILayout.CurrentPopUpRect.Contains(window.Container.LocalMousePos) : false;
        }

        /// <summary>
        /// is a mouse button down
        /// </summary>
        /// <param name="index">0 left, 1 right</param>
        /// <returns>true if down</returns>
        public bool IsDown(int index)
        {
            if (index >= ButtonStates.Length || index < 0)
            {
                return false;
            }
            return ButtonStates[index].IsDown;
        }

        /// <summary>
        /// is a mouse button up
        /// </summary>
        /// <param name="index">0 left, 1 right</param>
        /// <returns>true if up</returns>
        public bool IsUp(int index)
        {
            return ButtonStates[index].IsUp;
        }

        /// <summary>
        /// is a mouse button pressed
        /// </summary>
        /// <param name="index">0 left, 1 right</param>
        /// <returns>true if pressed</returns>
        public bool IsPressed(int index)
        {
            return ButtonStates[index].IsPressed;
        }
    }

    /// <summary>
    /// struct that represent a mouse button state
    /// </summary>
    internal struct UIMouseButtonState
    {
        internal bool IsPressed;
        internal bool IsDown;
        internal bool IsUp;

        /// <summary>
        /// Set a state for this frame
        /// </summary>
        /// <param name="state">true is pressed</param>
        internal void SetState(bool state)
        {
            // first frame pressed, down for this frame
            if (!IsPressed && state)
            {
                IsDown = true;
            }
            // already down or not pressed anymore, not down for this frame
            else
            {
                IsDown = false;
            }

            // pressed last frame and not pressed anymore, up for this frame
            if (IsPressed && !state)
            {
                IsUp = true;
            }
            // not pressed or still pressed, not up for this frame
            else
            {
                IsUp = false;
            }

            // pressed for this frame is new state
            IsPressed = state;
        }

        public override string ToString()
        {
            return "p. " + IsPressed + " d. " + IsDown + " u. " + IsUp;
        }
    }
}