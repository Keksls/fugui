using ImGuiNET;
using System;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditorInternal.ReorderableList;

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
            float gradientStrenght = FuThemeManager.CurrentTheme.ButtonsGradientStrenght;
            if (gradientStrenght > 0f)
            {
                Vector2 padding = FuThemeManager.CurrentTheme.FramePadding;
                return ButtonGradient(text, size.BrutSize, gradientStrenght, padding, style);
            }
            else
            {
                // apply style and check if the element should be disabled
                beginElement(ref text, style, true);
                // return if item must no be draw
                if (!_drawElement)
                {
                    return false;
                }

                // render the button and return true if it was clicked, false otherwise
                bool clicked = ImGui.Button(text, size) & !_nextIsDisabled;
                // display the tooltip if necessary
                displayToolTip();
                // remove the style and draw the hover frame if necessary
                endElement(style);
                return clicked;
            }
        }

        // Implementation
        public unsafe bool ButtonGradient(string text, Vector2 size, float gradientStrenght, Vector2 padding, FuButtonStyle style)
        {
            beginElement(ref text, style, true);
            if (!_drawElement)
            {
                return false;
            }

            // scale padding
            padding *= Fugui.CurrentContext.Scale;

            // clamp gradient strenght
            gradientStrenght = 1f - Mathf.Clamp(gradientStrenght, 0.1f, 1f);

            // calc label size
            Vector2 label_size = ImGui.CalcTextSize(text);

            // calc item size 
            Vector2 region_max = default;
            if (size.x < 0.0f || size.y < 0.0f)
                region_max = ImGui.GetContentRegionMax();

            if (size.x == 0.0f)
                size.x = label_size.x + padding.x * 2f;
            else if (size.x < 0.0f)
                size.x = Mathf.Max(4.0f, region_max.x - ImGui.GetCursorPosX() + size.x);

            if (size.y == 0.0f)
                size.y = label_size.y + padding.y * 2f;
            else if (size.y < 0.0f)
                size.y = Mathf.Max(4.0f, region_max.y - ImGui.GetCursorPosY() + size.y);

            // save current cursor pos
            Vector2 pos = ImGui.GetCursorScreenPos();

            // draw a dummy button to update cursor
            ImGui.Dummy(size);

            // get buttons states
            bool hovered = ImGui.IsItemHovered();
            bool active = ImGui.IsItemActive();
            bool clicked = ImGui.IsItemClicked();

            // get colors
            ImRect bb = new ImRect() { Min = ImGui.GetItemRectMin(), Max = ImGui.GetItemRectMax() };
            Vector4 bg1f = style.Button;
            if (_nextIsDisabled)
            {
                bg1f = style.DisabledButton;
            }
            else if (active)
            {
                bg1f = style.ButtonActive;
            }
            else if (hovered)
            {
                bg1f = style.ButtonHovered;
            }
            Vector4 bg2f = new Vector4(bg1f.x * gradientStrenght, bg1f.y * gradientStrenght, bg1f.z * gradientStrenght, bg1f.w);

            // prepare colors
            if (active || hovered && !_nextIsDisabled)
            {
                // Modify colors (ultimately this can be prebaked in the style)
                float h_increase = (active && hovered) ? 0.02f : 0.02f;
                float v_increase = (active && hovered) ? 0.20f : 0.07f;
                // prepare color 1
                ImGui.ColorConvertRGBtoHSV(bg1f.x, bg1f.y, bg1f.z, out bg1f.x, out bg1f.y, out bg1f.z);
                bg1f.x = Mathf.Min(bg1f.x + h_increase, 1.0f);
                bg1f.z = Mathf.Min(bg1f.z + v_increase, 1.0f);
                ImGui.ColorConvertHSVtoRGB(bg1f.x, bg1f.y, bg1f.z, out bg1f.x, out bg1f.y, out bg1f.z);
                // prepare color 2
                ImGui.ColorConvertRGBtoHSV(bg2f.x, bg2f.y, bg2f.z, out bg2f.x, out bg2f.y, out bg2f.z);
                bg2f.z = Mathf.Min(bg2f.z + h_increase, 1.0f);
                bg2f.z = Mathf.Min(bg2f.z + v_increase, 1.0f);
                ImGui.ColorConvertHSVtoRGB(bg2f.x, bg2f.y, bg2f.z, out bg2f.x, out bg2f.y, out bg2f.z);
            }

            // draw button frame
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            int vert_start_idx = drawList.VtxBuffer.Size;
            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(bg1f), FuThemeManager.CurrentTheme.FrameRounding);
            int vert_end_idx = drawList.VtxBuffer.Size;
            ImGuiInternal.igShadeVertsLinearColorGradientKeepAlpha(drawList.NativePtr, vert_start_idx, vert_end_idx, pos, bb.GetBL(), ImGui.GetColorU32(bg1f), ImGui.GetColorU32(bg2f));

            // draw border
            if (FuThemeManager.CurrentTheme.FrameBorderSize > 0.0f)
                drawList.AddRect(bb.Min, bb.Max, ImGui.GetColorU32(ImGuiCol.Border), FuThemeManager.CurrentTheme.FrameRounding, 0, FuThemeManager.CurrentTheme.FrameBorderSize);

            // Align whole block. We should defer that to the better rendering function when we'll have support for individual line alignment.
            Vector2 textPos = pos;
            Vector2 align = FuThemeManager.CurrentTheme.ButtonTextAlign;
            if (align.x > 0.0f) textPos.x = Mathf.Max(textPos.x, textPos.x + (bb.Max.x - textPos.x - label_size.x) * align.x);
            if (align.y > 0.0f) textPos.y = Mathf.Max(textPos.y, textPos.y + (bb.Max.y - textPos.y - label_size.y) * align.y);
            // draw text
            TextClipped(size, text, pos, padding, label_size, align);
            //drawList.AddText(textPos, ImGui.GetColorU32(_nextIsDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text), text);

            displayToolTip(); // display the tooltip if necessary
            endElement(style);
            return clicked && !_nextIsDisabled;
        }
    }
}