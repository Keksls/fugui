using System.Collections.Generic;
using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        // Keys placed by the last virtual keyboard drawn, reused from one frame to the next.
        private static readonly List<FuVirtualKeyboardKeyRect> _virtualKeyboardKeys = new List<FuVirtualKeyboardKeyRect>();
        #endregion

        #region Methods
        /// <summary>
        /// Draws a virtual keyboard at the cursor, as wide as the space left, and presses the key clicked.
        /// </summary>
        /// <param name="id">ID of the keyboard, unique in the window.</param>
        /// <param name="keyboard">The keyboard: its layout, its state and its target.</param>
        /// <returns>True when a key was pressed this frame.</returns>
        public bool VirtualKeyboard(string id, FuVirtualKeyboard keyboard)
        {
            return VirtualKeyboard(id, keyboard, 0f);
        }

        /// <summary>
        /// Draws a virtual keyboard at the cursor, as wide as the space left, and presses the key clicked.
        /// </summary>
        /// <param name="id">ID of the keyboard, unique in the window.</param>
        /// <param name="keyboard">The keyboard: its layout, its state and its target.</param>
        /// <param name="keyHeight">Height of a key, unscaled; 0 for one and a half frame height.</param>
        /// <returns>True when a key was pressed this frame.</returns>
        public bool VirtualKeyboard(string id, FuVirtualKeyboard keyboard, float keyHeight)
        {
            if (keyboard == null)
            {
                return false;
            }

            float spacing = ImGui.GetStyle().ItemSpacing.x;
            float height = keyHeight > 0f ? keyHeight * Fugui.CurrentContext.Scale : ImGui.GetFrameHeight() * 1.5f;
            int rows = keyboard.GetRows().Count;
            Vector2 start = ImGui.GetCursorScreenPos();
            Vector2 size = new Vector2(ImGui.GetContentRegionAvail().x, rows * height + (rows - 1) * spacing);
            keyboard.GetKeyRects(new Rect(start, size), spacing, _virtualKeyboardKeys);

            // The key is pressed once every key is drawn: pressing changes the keys (shift, symbols, layout).
            FuVirtualKeyboardKey? pressed = null;

            foreach (FuVirtualKeyboardKeyRect key in _virtualKeyboardKeys)
            {
                FuButtonStyle style = key.Key.Type == FuVirtualKeyboardKeyType.Enter ? FuButtonStyle.Highlight : (keyboard.IsOn(key.Key) ? FuButtonStyle.Selected : FuButtonStyle.Default);
                string text = keyboard.GetLabel(key.Key) + "##" + id + "_" + key.Row + "_" + key.Column;
                ImGui.SetCursorScreenPos(key.Rect.position);

                beginElement(ref text, style, true);
                if (!_drawElement)
                {
                    continue;
                }

                if (_customButton(text, key.Rect.size, Vector2.zero, Vector2.zero, style, Fugui.Themes.CurrentTheme.ButtonsGradientStrenght, true, .5f))
                {
                    pressed = key.Key;
                }

                endElement(style);
            }

            // The cursor goes under the keyboard, as after one element of its size.
            ImGui.SetCursorScreenPos(start);
            ImGui.Dummy(size);

            if (pressed.HasValue)
            {
                keyboard.Press(pressed.Value);
                return true;
            }

            return false;
        }
        #endregion
    }
}
