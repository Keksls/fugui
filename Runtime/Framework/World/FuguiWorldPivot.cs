namespace Fu
{
    /// <summary>
    /// Describes the local pivot used to place a Fugui world surface.
    /// </summary>
    public enum FuguiWorldPivot
    {
        /// <summary>
        /// Places the surface origin at the top left corner.
        /// </summary>
        TopLeft,

        /// <summary>
        /// Places the surface origin at the top center edge.
        /// </summary>
        TopCenter,

        /// <summary>
        /// Places the surface origin at the top right corner.
        /// </summary>
        TopRight,

        /// <summary>
        /// Places the surface origin at the middle left edge.
        /// </summary>
        MiddleLeft,

        /// <summary>
        /// Places the surface origin at the center of the surface.
        /// </summary>
        Center,

        /// <summary>
        /// Places the surface origin at the middle right edge.
        /// </summary>
        MiddleRight,

        /// <summary>
        /// Places the surface origin at the bottom left corner.
        /// </summary>
        BottomLeft,

        /// <summary>
        /// Places the surface origin at the bottom center edge.
        /// </summary>
        BottomCenter,

        /// <summary>
        /// Places the surface origin at the bottom right corner.
        /// </summary>
        BottomRight
    }
}
