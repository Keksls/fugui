using Fu.Framework;
using UnityEngine;

namespace Fu.Core
{
    /// <summary>
    /// struct that represent mouse states for an UIWindow
    /// </summary>
    public class FuMouseState
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
        private bool _isHoverTopBar;
        public bool IsHoverTopBar { get { return _isHoverTopBar; } }

        /// <summary>
        /// instantiate a new UIMouseState and init mouse Buttons array
        /// </summary>
        public FuMouseState()
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
            _isHoverPupUp = !string.IsNullOrEmpty(FuLayout.CurrentPopUpID) ? FuLayout.CurrentPopUpRect.Contains(window.Container.LocalMousePos) : false;
            _isHoverTopBar = window.UITopBar != null && window.TopBarHeight > 0f && position.y <= window.TopBarHeight + (window.WorkingAreaPosition.y - window.LocalPosition.y);
        }

        /// <summary>
        /// set current mouse position
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
            // check whatever mouse is hover any overlay
            container.OnEachWindow((window) =>
            {
                _isHoverOverlay |= window.Mouse.IsHoverOverlay;
                _isHoverPupUp |= window.Mouse.IsHoverPopup;
                _isHoverTopBar |= window.Mouse.IsHoverTopBar;
            });
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