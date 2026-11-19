using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Virtual keyboard: its keys (digits, letters in QWERTY or AZERTY, a layer of symbols), its state (upper case,
    /// symbols, layout) and what a key does to its <see cref="Target"/>.
    /// </summary>
    /// <remarks>
    /// <para>Drawn by <c>Fugui.Layout.VirtualKeyboard</c> in a Fugui window. An application that draws its own keys (in
    /// a 3D world, with its own pointer) uses <see cref="GetKeyRects"/> to place them and <see cref="Press"/> when one is
    /// chosen: the layouts and the behaviour stay the same everywhere.</para>
    /// <para>Rows, top to bottom: the digits and Backspace; three rows of letters (or of symbols), the third starting
    /// with Shift; then Layout, Symbols, Space, ':' and Enter. The characters of addresses and logins (. - _ @ : /) are
    /// on the letters; the backslash of the Windows logins is with the symbols.</para>
    /// </remarks>
    public class FuVirtualKeyboard
    {
        #region State
        /// <summary>
        /// Width of a row, in units of a character key.
        /// </summary>
        public const float ROW_UNITS = 11f;

        private static readonly Dictionary<(FuVirtualKeyboardLayout, bool), FuVirtualKeyboardKey[][]> _rowsCache = new Dictionary<(FuVirtualKeyboardLayout, bool), FuVirtualKeyboardKey[][]>();
        private FuVirtualKeyboardLayout _layout;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the layout of the letters; a change raises <see cref="OnLayoutChanged"/>.
        /// </summary>
        public FuVirtualKeyboardLayout Layout
        {
            get => _layout;
            set
            {
                if (value != _layout)
                {
                    _layout = value;
                    OnLayoutChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the letters are typed in upper case (Shift is on, until pressed again).
        /// </summary>
        public bool IsShifted { get; set; }

        /// <summary>
        /// Gets or sets whether the rows of letters show the symbols.
        /// </summary>
        public bool IsSymbols { get; set; }

        /// <summary>
        /// Gets or sets what the keyboard types in, or null.
        /// </summary>
        public IFuVirtualKeyboardTarget Target { get; set; }

        /// <summary>
        /// Gets or sets the label of Backspace (an icon of the application, for instance).
        /// </summary>
        public string BackspaceLabel { get; set; } = "Del";

        /// <summary>
        /// Gets or sets the label of Shift.
        /// </summary>
        public string ShiftLabel { get; set; } = "Shift";

        /// <summary>
        /// Gets or sets the label of Space.
        /// </summary>
        public string SpaceLabel { get; set; } = "Space";

        /// <summary>
        /// Gets or sets the label of Enter.
        /// </summary>
        public string EnterLabel { get; set; } = "Enter";
        #endregion

        #region Events
        /// <summary>
        /// Raised with the new layout when it changes (the Layout key, or <see cref="Layout"/>).
        /// </summary>
        public event Action<FuVirtualKeyboardLayout> OnLayoutChanged;

        /// <summary>
        /// Raised after a key is pressed, with the key.
        /// </summary>
        public event Action<FuVirtualKeyboardKey> OnKeyPressed;

        /// <summary>
        /// Raised when Enter is pressed, after the target is told.
        /// </summary>
        public event Action OnSubmitted;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a keyboard.
        /// </summary>
        /// <param name="layout">Layout of the letters.</param>
        public FuVirtualKeyboard(FuVirtualKeyboardLayout layout = FuVirtualKeyboardLayout.Qwerty)
        {
            _layout = layout;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the rows of keys shown now (layout, letters or symbols), top to bottom.
        /// </summary>
        /// <returns>The rows of keys.</returns>
        public IReadOnlyList<FuVirtualKeyboardKey[]> GetRows()
        {
            return GetRows(_layout, IsSymbols);
        }

        /// <summary>
        /// Gets the rows of keys of a layout, top to bottom.
        /// </summary>
        /// <param name="layout">Layout of the letters.</param>
        /// <param name="isSymbols">True for the layer of symbols.</param>
        /// <returns>The rows of keys.</returns>
        public static IReadOnlyList<FuVirtualKeyboardKey[]> GetRows(FuVirtualKeyboardLayout layout, bool isSymbols)
        {
            if (!_rowsCache.TryGetValue((layout, isSymbols), out FuVirtualKeyboardKey[][] rows))
            {
                rows = createRows(layout, isSymbols);
                _rowsCache[(layout, isSymbols)] = rows;
            }

            return rows;
        }

        /// <summary>
        /// Places the keys shown now in an area: rows of equal height, keys sized by their width, each row centered.
        /// </summary>
        /// <param name="area">Area of the keyboard.</param>
        /// <param name="spacing">Space between two keys and between two rows.</param>
        /// <param name="keys">Filled with the placed keys (cleared first).</param>
        public void GetKeyRects(Rect area, float spacing, List<FuVirtualKeyboardKeyRect> keys)
        {
            keys.Clear();
            IReadOnlyList<FuVirtualKeyboardKey[]> rows = GetRows();
            float keyHeight = (area.height - spacing * (rows.Count - 1)) / rows.Count;
            float unit = (area.width + spacing) / ROW_UNITS;

            for (int row = 0; row < rows.Count; row++)
            {
                float units = 0f;

                foreach (FuVirtualKeyboardKey key in rows[row])
                {
                    units += key.Width;
                }

                float x = area.x + (area.width - (units * unit - spacing)) * .5f;
                float y = area.y + row * (keyHeight + spacing);

                for (int column = 0; column < rows[row].Length; column++)
                {
                    FuVirtualKeyboardKey key = rows[row][column];
                    keys.Add(new FuVirtualKeyboardKeyRect(key, new Rect(x, y, key.Width * unit - spacing, keyHeight), row, column));
                    x += key.Width * unit;
                }
            }
        }

        /// <summary>
        /// Gets the label of a key as shown now: the character (upper case while shifted), or the label of the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The label.</returns>
        public string GetLabel(FuVirtualKeyboardKey key)
        {
            switch (key.Type)
            {
                case FuVirtualKeyboardKeyType.Character:
                    return IsShifted ? key.Character.ToUpperInvariant() : key.Character;
                case FuVirtualKeyboardKeyType.Backspace:
                    return BackspaceLabel;
                case FuVirtualKeyboardKeyType.Shift:
                    return ShiftLabel;
                case FuVirtualKeyboardKeyType.Symbols:
                    return IsSymbols ? "ABC" : "#+=";
                case FuVirtualKeyboardKeyType.Layout:
                    return _layout == FuVirtualKeyboardLayout.Azerty ? "AZERTY" : "QWERTY";
                case FuVirtualKeyboardKeyType.Space:
                    return SpaceLabel;
                case FuVirtualKeyboardKeyType.Enter:
                    return EnterLabel;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets whether a key is switched on: Shift while shifted, Symbols while the symbols show.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>True for a key switched on.</returns>
        public bool IsOn(FuVirtualKeyboardKey key)
        {
            return (key.Type == FuVirtualKeyboardKeyType.Shift && IsShifted) || (key.Type == FuVirtualKeyboardKeyType.Symbols && IsSymbols);
        }

        /// <summary>
        /// Presses a key: types in the target, or switches the state of the keyboard.
        /// </summary>
        /// <param name="key">The key.</param>
        public void Press(FuVirtualKeyboardKey key)
        {
            switch (key.Type)
            {
                case FuVirtualKeyboardKeyType.Character:
                    Target?.Insert(IsShifted ? key.Character.ToUpperInvariant() : key.Character);
                    break;
                case FuVirtualKeyboardKeyType.Space:
                    Target?.Insert(" ");
                    break;
                case FuVirtualKeyboardKeyType.Backspace:
                    Target?.Backspace();
                    break;
                case FuVirtualKeyboardKeyType.Shift:
                    IsShifted = !IsShifted;
                    break;
                case FuVirtualKeyboardKeyType.Symbols:
                    IsSymbols = !IsSymbols;
                    break;
                case FuVirtualKeyboardKeyType.Layout:
                    Layout = _layout == FuVirtualKeyboardLayout.Azerty ? FuVirtualKeyboardLayout.Qwerty : FuVirtualKeyboardLayout.Azerty;
                    break;
                case FuVirtualKeyboardKeyType.Enter:
                    Target?.Submit();
                    OnSubmitted?.Invoke();
                    break;
            }

            OnKeyPressed?.Invoke(key);
        }

        /// <summary>
        /// Gets the layout of the keyboard of the computer: AZERTY for a French or Belgian keyboard (or a French system
        /// when the keyboard does not say), QWERTY otherwise.
        /// </summary>
        /// <returns>The layout.</returns>
        public static FuVirtualKeyboardLayout GetSystemLayout()
        {
            string keyboardLayout = UnityEngine.InputSystem.Keyboard.current != null ? UnityEngine.InputSystem.Keyboard.current.keyboardLayout : null;

            if (!string.IsNullOrEmpty(keyboardLayout))
            {
                string name = keyboardLayout.ToUpperInvariant();
                return name.Contains("FRENCH") || name.Contains("BELGIAN") || name.Contains("AZERTY") ? FuVirtualKeyboardLayout.Azerty : FuVirtualKeyboardLayout.Qwerty;
            }

            return CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "fr" ? FuVirtualKeyboardLayout.Azerty : FuVirtualKeyboardLayout.Qwerty;
        }

        /// <summary>
        /// Builds the rows of a layout.
        /// </summary>
        /// <param name="layout">Layout of the letters.</param>
        /// <param name="isSymbols">True for the layer of symbols.</param>
        /// <returns>The rows of keys.</returns>
        private static FuVirtualKeyboardKey[][] createRows(FuVirtualKeyboardLayout layout, bool isSymbols)
        {
            FuVirtualKeyboardKey[] digits = join(characters("1234567890"), new FuVirtualKeyboardKey(FuVirtualKeyboardKeyType.Backspace, null, 1f));
            FuVirtualKeyboardKey[] bottom =
            {
                new FuVirtualKeyboardKey(FuVirtualKeyboardKeyType.Layout, null, 2f),
                new FuVirtualKeyboardKey(FuVirtualKeyboardKeyType.Symbols, null, 1.5f),
                new FuVirtualKeyboardKey(FuVirtualKeyboardKeyType.Space, null, 4.5f),
                new FuVirtualKeyboardKey(FuVirtualKeyboardKeyType.Character, ":", 1f),
                new FuVirtualKeyboardKey(FuVirtualKeyboardKeyType.Enter, null, 2f)
            };

            if (isSymbols)
            {
                return new[]
                {
                    digits,
                    characters("!?#$%&*()=+"),
                    characters("\"':;,./\\[]|"),
                    characters("<>{}~^`@_-"),
                    bottom
                };
            }

            bool isAzerty = layout == FuVirtualKeyboardLayout.Azerty;
            FuVirtualKeyboardKey shift = new FuVirtualKeyboardKey(FuVirtualKeyboardKeyType.Shift, null, 1.5f);
            return new[]
            {
                digits,
                characters(isAzerty ? "azertyuiop-" : "qwertyuiop-"),
                characters(isAzerty ? "qsdfghjklm." : "asdfghjkl._"),
                join(new[] { shift }, characters(isAzerty ? "wxcvbn_@/" : "zxcvbnm@/")),
                bottom
            };
        }

        /// <summary>
        /// Gets one character key per character of a string.
        /// </summary>
        /// <param name="text">The characters.</param>
        /// <returns>The keys.</returns>
        private static FuVirtualKeyboardKey[] characters(string text)
        {
            FuVirtualKeyboardKey[] keys = new FuVirtualKeyboardKey[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                keys[i] = new FuVirtualKeyboardKey(FuVirtualKeyboardKeyType.Character, text[i].ToString(), 1f);
            }

            return keys;
        }

        /// <summary>
        /// Joins two lists of keys.
        /// </summary>
        /// <param name="first">The first keys.</param>
        /// <param name="second">The keys after them.</param>
        /// <returns>The keys, in order.</returns>
        private static FuVirtualKeyboardKey[] join(FuVirtualKeyboardKey[] first, params FuVirtualKeyboardKey[] second)
        {
            FuVirtualKeyboardKey[] keys = new FuVirtualKeyboardKey[first.Length + second.Length];
            first.CopyTo(keys, 0);
            second.CopyTo(keys, first.Length);
            return keys;
        }
        #endregion
    }
}
