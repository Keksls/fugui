using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// A key of a <see cref="FuVirtualKeyboard"/> placed in an area: its row, its column and its rectangle.
    /// </summary>
    public readonly struct FuVirtualKeyboardKeyRect
    {
        #region State
        /// <summary>
        /// The key.
        /// </summary>
        public readonly FuVirtualKeyboardKey Key;

        /// <summary>
        /// Rectangle of the key, in the coordinates of the area given.
        /// </summary>
        public readonly Rect Rect;

        /// <summary>
        /// Row of the key, from the top.
        /// </summary>
        public readonly int Row;

        /// <summary>
        /// Column of the key in its row, from the left.
        /// </summary>
        public readonly int Column;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a placed key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="rect">Its rectangle.</param>
        /// <param name="row">Its row.</param>
        /// <param name="column">Its column.</param>
        public FuVirtualKeyboardKeyRect(FuVirtualKeyboardKey key, Rect rect, int row, int column)
        {
            Key = key;
            Rect = rect;
            Row = row;
            Column = column;
        }
        #endregion
    }
}
