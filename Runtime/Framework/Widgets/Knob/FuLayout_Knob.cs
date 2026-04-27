using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the color set data structure.
    /// </summary>
    internal struct color_set
    {
        #region State
        internal Vector4 color;
        internal Vector4 hovered;
        internal Vector4 active;
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the color set class.
        /// </summary>
        /// <param name="_color">The color value.</param>
        /// <param name="_hovered">The hovered value.</param>
        /// <param name="_active">The active value.</param>
        internal color_set(Vector4 _color, Vector4 _hovered, Vector4 _active)
        {
            color = _color;
            hovered = _hovered;
            active = _active;
        }

        /// <summary>
        /// Initializes a new instance of the color set class.
        /// </summary>
        /// <param name="_color">The color value.</param>
        internal color_set(Vector4 _color)
        {
            color = _color;
            hovered = _color;
            active = _color;
        }
        #endregion
    }
}