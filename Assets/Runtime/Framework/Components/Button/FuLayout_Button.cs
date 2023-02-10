using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        /// <summary>
        /// Draw a custom pixel perfect padding button
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <returns>whatever the button is clicked</returns>
        public bool UnpaddedButton(string text)
        {
            return UnpaddedButton(text, Vector2.zero, Vector2.zero, FuButtonStyle.Default);
        }

        /// <summary>
        /// Draw a custom pixel perfect padding button
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="padding">inner padding of the button</param>
        /// <returns>whatever the button is clicked</returns>
        public bool UnpaddedButton(string text, Vector2 padding)
        {
            return UnpaddedButton(text, padding, Vector2.zero, FuButtonStyle.Default);
        }

        /// <summary>
        /// Draw a custom pixel perfect padding button
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="padding">inner padding of the button</param>
        /// <param name="textOffset">Offset of the text inside the button rect</param>
        /// <returns>whatever the button is clicked</returns>
        public bool UnpaddedButton(string text, Vector2 padding, Vector2 textOffset)
        {
            return UnpaddedButton(text, padding, textOffset, FuButtonStyle.Default);
        }

        /// <summary>
        /// Draw a custom pixel perfect padding button
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="style">style of the button</param>
        /// <returns>whatever the button is clicked</returns>
        public bool UnpaddedButton(string text, FuButtonStyle style)
        {
            return UnpaddedButton(text, Vector2.zero, Vector2.zero, style);
        }

        /// <summary>
        /// Draw a custom pixel perfect padding button
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="padding">inner padding of the button</param>
        /// <param name="textOffset">Offset of the text inside the button rect</param>
        /// <param name="style">style of the button</param>
        /// <returns>whatever the button is clicked</returns>
        public bool UnpaddedButton(string text, Vector2 padding, Vector2 textOffset, FuButtonStyle style)
        {
            beginElement(ref text, null, true);
            if (!_drawElement)
            {
                return false;
            }

            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 btnSize = textSize + new Vector2(2f + (padding.x * 2f), 2f + (padding.y * 2f));
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            bool hovered = ImGui.IsMouseHoveringRect(pos, pos + btnSize);
            bool active = hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            bool clicked = hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left);

            Vector4 btnColor = style.Button;
            if (_nextIsDisabled)
            {
                btnColor = style.DisabledButton;
            }
            else
            {
                if (active)
                {
                    btnColor = style.ButtonActive;
                }
                else if (hovered)
                {
                    btnColor = style.ButtonHovered;
                }
            }
            uint btnColorUInt = ImGui.GetColorU32(btnColor);
            uint textColorUInt = ImGui.GetColorU32(_nextIsDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text);
            uint borderColorUInt = ImGui.GetColorU32(ImGuiCol.Border);
            float rounding = FuThemeManager.CurrentTheme.FrameRounding;

            // draw button
            drawList.AddRectFilled(pos + Vector2.one, pos + btnSize - Vector2.one, btnColorUInt, rounding);
            // draw border
            drawList.AddRect(pos, pos + btnSize, borderColorUInt, rounding);
            // draw text
            drawList.AddText(pos + padding + Vector2.one + textOffset, textColorUInt, text);

            Dummy(btnSize);
            endElement();

            return clicked;
        }

        /// <summary>
        /// Renders a button with the given text. The button will have the default size and style.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public bool Button(string text)
        {
            return Button(text, FuElementSize.FullSize, FuButtonStyle.Default);
        }

        /// <summary>
        /// Renders a button with the given text and size. The button will have the default style.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="size">The size of the button. If either dimension is set to -1, it will be set to the available content region size in that direction.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public bool Button(string text, FuElementSize size)
        {
            return Button(text, size, FuButtonStyle.Default);
        }

        /// <summary>
        /// Renders a button with the given text and style. The button will have the default size.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="style">The style to apply to the button.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public bool Button(string text, FuButtonStyle style)
        {
            return Button(text, FuElementSize.FullSize, style);
        }

        /// <summary>
        /// Renders a button with the given text, size, and style.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="size">The size of the button. If either dimension is set to -1, it will be set to the available content region size in that direction.</param>
        /// <param name="style">The style to apply to the button.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public virtual bool Button(string text, FuElementSize size, FuButtonStyle style)
        {
            beginElement(ref text, style, true); // apply style and check if the element should be disabled
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            bool clicked = ImGui.Button(text, size) & !_nextIsDisabled; // render the button and return true if it was clicked, false otherwise
            displayToolTip(); // display the tooltip if necessary
            endElement(style); // remove the style and draw the hover frame if necessary
            return clicked;
        }
    }
}