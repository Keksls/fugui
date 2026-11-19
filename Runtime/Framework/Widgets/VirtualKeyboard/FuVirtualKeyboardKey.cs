using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// A key of a <see cref="FuVirtualKeyboard"/>: what it does, the character it types, and its width.
    /// </summary>
    public readonly struct FuVirtualKeyboardKey
    {
        #region State
        /// <summary>
        /// What the key does.
        /// </summary>
        public readonly FuVirtualKeyboardKeyType Type;

        /// <summary>
        /// Character typed by a <see cref="FuVirtualKeyboardKeyType.Character"/> key (lower case for a letter), or null.
        /// </summary>
        public readonly string Character;

        /// <summary>
        /// Width of the key, in units of a character key (a row holds <see cref="FuVirtualKeyboard.ROW_UNITS"/>).
        /// </summary>
        public readonly float Width;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a key.
        /// </summary>
        /// <param name="type">What the key does.</param>
        /// <param name="character">Character typed by a character key, or null.</param>
        /// <param name="width">Width, in units of a character key.</param>
        public FuVirtualKeyboardKey(FuVirtualKeyboardKeyType type, string character, float width)
        {
            Type = type;
            Character = character;
            Width = Mathf.Max(.1f, width);
        }
        #endregion
    }
}
