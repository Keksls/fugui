namespace Fu.Core
{
    /// <summary>
    /// struct that represent a mouse button state
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
}