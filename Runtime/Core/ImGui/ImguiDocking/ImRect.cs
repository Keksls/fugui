using UnityEngine;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Rect data structure.
    /// </summary>
    public unsafe partial struct ImRect
    {
        #region State
        public Vector2 Min;
        public Vector2 Max;
        #endregion

        #region Methods
        /// <summary>
        /// Gets the bl.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public Vector2 GetBL()
        {
            return new Vector2(Min.x, Max.y);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the Im Rect class.
        /// </summary>
        /// <param name="min">The min value.</param>
        /// <param name="max">The max value.</param>
        public ImRect(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }
        #endregion
    }
}