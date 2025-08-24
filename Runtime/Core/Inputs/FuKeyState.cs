using UnityEngine;

namespace Fu
{
    /// <summary>
    /// struct that represent a keyboard key state
    /// </summary>
    public struct FuKeyState
    {
        internal int Index;
        public bool IsPressed;
        public bool IsDown;
        public bool IsUp;

        /// <summary>
        /// construct a FuKeyState struct with default key state values for a given index
        /// </summary>
        /// <param name="index">Index of the key state (Mouse button index or KeyCode)</param>
        public FuKeyState(int index)
        {
            Index = index;
            IsDown = false;
            IsPressed = false;
            IsUp = false;
        }

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

    /// <summary>
    /// struct that represent a mouse button state
    /// </summary>
    public struct FuButtonState
    {
        internal int Index;
        public bool IsPressed;
        public bool IsDown;
        public bool IsUp;
        private Vector2Int _downPosition;
        public Vector2Int PressedMovement;

        /// <summary>
        /// construct a FuKeyState struct with default key state values for a given index
        /// </summary>
        /// <param name="index">Index of the key state (Mouse button index or KeyCode)</param>
        public FuButtonState(int index)
        {
            Index = index;
            IsDown = false;
            IsPressed = false;
            IsUp = false;
            _downPosition = Vector2Int.zero;
            PressedMovement = Vector2Int.zero;
        }

        /// <summary>
        /// Set a state for this frame
        /// </summary>
        /// <param name="state">true is pressed</param>
        internal void SetState(bool state, Vector2Int mousePosition)
        {
            // first frame pressed, down for this frame
            if (!IsPressed && state)
            {
                IsDown = true;
                _downPosition = mousePosition;
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
                PressedMovement = mousePosition - _downPosition;
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