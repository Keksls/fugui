﻿namespace Fu.Core
{
    public enum FuWindowFlags
    {
        Default = 0,
        NoExternalization = 1,
        NoDocking = 2,
        NoInterractions = 4,
        NoDockingOverMe = 8,
        AllowMultipleWindow = 16
    }

    public enum FuExternalWindowFlags
    {
        Default = 0,
        ShowWindowTitle = 1
    }

    public enum FuOverlayFlags
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

    public enum FuOverlayDragPosition
    {
        Auto,
        Top,
        Right,
        Bottom,
        Left
    }
}