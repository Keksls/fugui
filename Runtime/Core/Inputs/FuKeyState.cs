
namespace Fu
{
    /// <summary>
    /// struct that represent a keyboard key state
    /// </summary>
    public struct FuKeyState
    {
        #region State
        internal int Index;
        public bool IsPressed;
        public bool IsDown;
        public bool IsUp;
        #endregion

        #region Constructors
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
        #endregion

        #region Methods
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

        /// <summary>
        /// Returns the to string result.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public override string ToString()
        {
            return "p. " + IsPressed + " d. " + IsDown + " u. " + IsUp;
        }
        #endregion
    }
}