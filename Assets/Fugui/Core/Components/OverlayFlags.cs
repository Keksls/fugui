﻿namespace Fugui.Core
{
    public enum OverlayFlags
    {
        Default = 0,
        /// <summary>
        /// The overlay can't be moved using drag button
        /// </summary>
        NoMove = 1,
        /// <summary>
        /// The overlay can't be collapsed using drag button
        /// </summary>
        NoClose = 2,
        /// <summary>
        /// The overlay don't display container background
        /// </summary>
        NoBackground = 4,
        /// <summary>
        /// User can't change overlay anchor using drag button context menu
        /// </summary>
        NoEditAnchor = 8
    }

    public enum OverlayDragPosition
    {
        Auto,
        Top,
        Right,
        Bottom,
        Left
    }
}