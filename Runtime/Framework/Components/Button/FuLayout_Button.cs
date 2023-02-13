using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text)
        {
            return Button(text, FuElementSize.FullSize.BrutSize, FuThemeManager.CurrentTheme.FramePadding, FuButtonStyle.Default, FuThemeManager.CurrentTheme.ButtonsGradientStrenght);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size)
        {
            return Button(text, size.BrutSize, FuThemeManager.CurrentTheme.FramePadding, FuButtonStyle.Default, FuThemeManager.CurrentTheme.ButtonsGradientStrenght);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="style">style of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size, FuButtonStyle style)
        {
            return Button(text, size.BrutSize, FuThemeManager.CurrentTheme.FramePadding, style, FuThemeManager.CurrentTheme.ButtonsGradientStrenght);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="style">style of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuButtonStyle style)
        {
            return Button(text, FuElementSize.FullSize.BrutSize, FuThemeManager.CurrentTheme.FramePadding, style, FuThemeManager.CurrentTheme.ButtonsGradientStrenght);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="padding">padding of the text inside the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size, Vector2 padding)
        {
            return Button(text, size.BrutSize, padding, FuButtonStyle.Default, FuThemeManager.CurrentTheme.ButtonsGradientStrenght);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="padding">padding of the text inside the button</param>
        /// <param name="gradientStrenght">strenght of the button gradient (typicaly the one of the theme)</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size, Vector2 padding, float gradientStrenght)
        {
            return Button(text, size.BrutSize, padding, FuButtonStyle.Default, gradientStrenght);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="padding">padding of the text inside the button</param>
        /// <param name="style">style of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size, Vector2 padding, FuButtonStyle style)
        {
            return Button(text, size.BrutSize, padding, style, FuThemeManager.CurrentTheme.ButtonsGradientStrenght);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="padding">padding of the text inside the button</param>
        /// <param name="gradientStrenght">strenght of the button gradient (typicaly the one of the theme)</param>
        /// <param name="style">style of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size, Vector2 padding, FuButtonStyle style, float gradientStrenght)
        {
            return Button(text, size.BrutSize, padding, Vector2.zero, gradientStrenght, style);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="padding">padding of the text inside the button</param>
        /// <param name="textOffset">offset of the text inside the button</param>
        /// <param name="style">style of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size, Vector2 padding, Vector2 textOffset, FuButtonStyle style)
        {
            return Button(text, size.BrutSize, padding, textOffset, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, style);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="padding">padding of the text inside the button</param>
        /// <param name="textOffset">offset of the text inside the button</param>
        /// <param name="gradientStrenght">strenght of the button gradient (typicaly the one of the theme)</param>
        /// <param name="style">style of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size, Vector2 padding, Vector2 textOffset, float gradientStrenght, FuButtonStyle style)
        {
            return _customButton(text, size.BrutSize, padding, textOffset, style, gradientStrenght);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="padding">padding of the text inside the button</param>
        /// <param name="textOffset">offset of the text inside the button</param>
        /// <param name="gradientStrenght">strenght of the button gradient (typicaly the one of the theme)</param>
        /// <param name="style">style of the button</param>
        /// <returns>true if clicked</returns>
        private unsafe bool _customButton(string text, Vector2 size, Vector2 padding, Vector2 textOffset, FuButtonStyle style, float gradientStrenght)
        {
            // begin the element
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
                size.x = Mathf.Max(4.0f, region_max.x - ImGuiNative.igGetCursorPosX() + size.x);
            if (size.y == 0.0f)
                size.y = label_size.y + padding.y * 2f;
            else if (size.y < 0.0f)
                size.y = Mathf.Max(4.0f, region_max.y - ImGuiNative.igGetCursorPosY() + size.y);

            // save current cursor pos
            Vector2 pos = ImGui.GetCursorScreenPos();

            // draw a dummy button to update cursor
            ImGuiNative.igDummy(size);

            // get buttons states
            bool hovered = ImGuiNative.igIsItemHovered(ImGuiHoveredFlags.None) != 0;
            bool active = ImGuiNative.igIsItemActive() != 0;
            bool clicked = ImGuiNative.igIsItemClicked(ImGuiMouseButton.Left) != 0;

            // get current draw list
            ImDrawListPtr drawList = ImGuiNative.igGetWindowDrawList();

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

            // draw gradient button
            if (gradientStrenght > 0f)
            {
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
                int vert_start_idx = drawList.VtxBuffer.Size;
                drawList.AddRectFilled(pos, pos + size, ImGuiNative.igGetColorU32_Vec4(bg1f), FuThemeManager.CurrentTheme.FrameRounding);
                int vert_end_idx = drawList.VtxBuffer.Size;
                ImGuiInternal.igShadeVertsLinearColorGradientKeepAlpha(drawList.NativePtr, vert_start_idx, vert_end_idx, pos, bb.GetBL(), ImGuiNative.igGetColorU32_Vec4(bg1f), ImGuiNative.igGetColorU32_Vec4(bg2f));
            }
            // draw frame button
            else
            {
                drawList.AddRectFilled(pos, pos + size, ImGuiNative.igGetColorU32_Vec4(bg1f), FuThemeManager.CurrentTheme.FrameRounding);
            }

            // draw border
            if (FuThemeManager.CurrentTheme.FrameBorderSize > 0.0f)
                drawList.AddRect(bb.Min, bb.Max, ImGuiNative.igGetColorU32_Col(ImGuiCol.Border, 1f), FuThemeManager.CurrentTheme.FrameRounding, 0, FuThemeManager.CurrentTheme.FrameBorderSize);

            // Align whole block. We should defer that to the better rendering function when we'll have support for individual line alignment.
            Vector2 textPos = pos;
            Vector2 align = FuThemeManager.CurrentTheme.ButtonTextAlign;
            if (align.x > 0.0f) textPos.x = Mathf.Max(textPos.x, textPos.x + (bb.Max.x - textPos.x - label_size.x) * align.x);
            if (align.y > 0.0f) textPos.y = Mathf.Max(textPos.y, textPos.y + (bb.Max.y - textPos.y - label_size.y) * align.y);

            // draw text
            TextClipped(size, text, pos + textOffset, padding, label_size, align);

            // display the tooltip if necessary
            displayToolTip();

            // end the element
            endElement(style);

            // return whatever the button is clicked
            return clicked && !_nextIsDisabled;
        }

        // ================ old default button version 
        //// apply style and check if the element should be disabled
        //beginElement(ref text, style, true);
        //// return if item must no be draw
        //if (!_drawElement)
        //{
        //    return false;
        //}

        //// render the button and return true if it was clicked, false otherwise
        //bool clicked = ImGui.Button(text, size) & !_nextIsDisabled;
        //// display the tooltip if necessary
        //displayToolTip();
        //// remove the style and draw the hover frame if necessary
        //endElement(style);
        //return clicked;
    }
}