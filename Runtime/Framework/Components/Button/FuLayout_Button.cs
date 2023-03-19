using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        // use this to store the position of the current active button. We don't use the ID, because the ID of a button is sometimes multiple on some button (eg. Icons). So position is more accurate
        private static Vector2 _currentActiveButtonPosition = Vector2.zero;

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text)
        {
            return Button(text, FuElementSize.FullSize.BrutSize, FuThemeManager.CurrentTheme.FramePadding, Vector2.zero, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, FuButtonStyle.Default);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size)
        {
            return Button(text, size.BrutSize, FuThemeManager.CurrentTheme.FramePadding, Vector2.zero, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, FuButtonStyle.Default);
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
            return Button(text, size.BrutSize, FuThemeManager.CurrentTheme.FramePadding, Vector2.zero, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, style);
        }

        /// <summary>
        /// Render a button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="style">style of the button</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuButtonStyle style)
        {
            return Button(text, FuElementSize.FullSize.BrutSize, FuThemeManager.CurrentTheme.FramePadding, Vector2.zero, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, style);
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
            return Button(text, size.BrutSize, padding, Vector2.zero, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, FuButtonStyle.Default);
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
            return Button(text, size.BrutSize, padding, Vector2.zero, gradientStrenght, FuButtonStyle.Default);
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
            return Button(text, size.BrutSize, padding, Vector2.zero, FuThemeManager.CurrentTheme.ButtonsGradientStrenght, style);
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
        /// <param name="bordered">draw borders arround button</param>
        /// <param name="alignment">alignment of the button text</param>
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size, Vector2 padding, Vector2 textOffset, float gradientStrenght, FuButtonStyle style, bool bordered = true, float alignment = -1f)
        {
            // begin the element
            beginElement(ref text, style, true);
            if (!_drawElement)
            {
                return false;
            }

            // draw the button
            bool clicked = _customButton(text, size.BrutSize, padding, textOffset, style, gradientStrenght, bordered, alignment);

            // end the element
            endElement(style);
            return clicked;
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
        /// <param name="bordered">draw borders arround button</param>
        /// <returns>true if clicked</returns>
        private unsafe bool _customButton(string text, Vector2 size, Vector2 padding, Vector2 textOffset, FuButtonStyle style, float gradientStrenght, bool bordered = true, float alignment = -1f, float textWidthOffset = 0f)
        {
            // scale padding
            padding *= Fugui.CurrentContext.Scale;

            // clamp gradient strenght
            gradientStrenght = 1f - Mathf.Clamp(gradientStrenght, 0.1f, 1f);

            // calc label size
            Vector2 label_size = ImGui.CalcTextSize(text, true);

            // get the current cursor pos so we draw at the right place
            Vector2 pos = ImGui.GetCursorScreenPos();

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

            // draw a dummy button to update cursor and get states
            bool clicked = ImGui.InvisibleButton(text, size, ImGuiButtonFlags.None) && !LastItemDisabled;
            setBaseElementState(text, pos, size, true, clicked);

            // get current draw list
            ImDrawListPtr drawList = ImGuiNative.igGetWindowDrawList();
            ImGuiStylePtr imStyle = ImGui.GetStyle();

            // get colors
            ImRect bb = new ImRect() { Min = ImGui.GetItemRectMin(), Max = ImGui.GetItemRectMax() };
            Vector4 bg1f = style.Button;
            if (LastItemDisabled)
            {
                bg1f = style.DisabledButton;
            }
            else if (_lastItemActive)
            {
                bg1f = style.ButtonActive;
            }
            else if (_lastItemHovered)
            {
                bg1f = style.ButtonHovered;
            }

            // draw gradient button
            if (gradientStrenght > 0f)
            {
                Vector4 bg2f = new Vector4(bg1f.x * gradientStrenght, bg1f.y * gradientStrenght, bg1f.z * gradientStrenght, bg1f.w);
                // draw button frame
                int vert_start_idx = drawList.VtxBuffer.Size;
                drawList.AddRectFilled(pos, pos + size, ImGuiNative.igGetColorU32_Vec4(bg1f), imStyle.FrameRounding);
                int vert_end_idx = drawList.VtxBuffer.Size;
                ImGuiInternal.igShadeVertsLinearColorGradientKeepAlpha(drawList.NativePtr, vert_start_idx, vert_end_idx, pos, bb.GetBL(), ImGuiNative.igGetColorU32_Vec4(bg1f), ImGuiNative.igGetColorU32_Vec4(bg2f));
            }
            // draw frame button
            else
            {
                drawList.AddRectFilled(pos, pos + size, ImGuiNative.igGetColorU32_Vec4(bg1f), imStyle.FrameRounding);
            }

            // draw border
            if (imStyle.FrameBorderSize > 0.0f && bordered)
            {
                drawList.AddRect(bb.Min, bb.Max, ImGuiNative.igGetColorU32_Col(ImGuiCol.Border, 1f), imStyle.FrameRounding, 0, imStyle.FrameBorderSize);
            }

            // calculate alignment
            Vector2 align = alignment == -1f ? imStyle.ButtonTextAlign : new Vector2(alignment, 0.5f);

            // set mouse cursor
            if (_lastItemHovered && !LastItemDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            // draw text
            size.x -= textWidthOffset;
            _customTextClipped(size, text, pos + textOffset, padding, label_size, align, style.TextStyle);

            // display the tooltip if necessary
            displayToolTip();

            // return whatever the button is clicked
            return _lastItemUpdate;
        }
    }
}