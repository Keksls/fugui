using Fu.Core;
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
        /// <returns>true if clicked</returns>
        public bool Button(string text, FuElementSize size, Vector2 padding, Vector2 textOffset, float gradientStrenght, FuButtonStyle style)
        {
            // begin the element
            beginElement(ref text, style, true);
            if (!_drawElement)
            {
                return false;
            }

            // draw the button
            bool clicked = _customButton(text, size.BrutSize, padding, textOffset, style, gradientStrenght);

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
        /// <returns>true if clicked</returns>
        private unsafe bool _customButton(string text, Vector2 size, Vector2 padding, Vector2 textOffset, FuButtonStyle style, float gradientStrenght)
        {
            // scale padding
            padding *= Fugui.CurrentContext.Scale;

            // clamp gradient strenght
            gradientStrenght = 1f - Mathf.Clamp(gradientStrenght, 0.1f, 1f);

            // calc label size
            Vector2 label_size = ImGui.CalcTextSize(text, true);

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
            bool clicked = ImGui.InvisibleButton(text, size);
            bool hovered = ImGuiNative.igIsItemHovered(ImGuiHoveredFlags.None) != 0;
            bool active = ImGuiNative.igIsItemActive() != 0;

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
                // draw button frame
                int vert_start_idx = drawList.VtxBuffer.Size;
                drawList.AddRectFilled(_currentItemStartPos, _currentItemStartPos + size, ImGuiNative.igGetColorU32_Vec4(bg1f), FuThemeManager.CurrentTheme.FrameRounding);
                int vert_end_idx = drawList.VtxBuffer.Size;
                ImGuiInternal.igShadeVertsLinearColorGradientKeepAlpha(drawList.NativePtr, vert_start_idx, vert_end_idx, _currentItemStartPos, bb.GetBL(), ImGuiNative.igGetColorU32_Vec4(bg1f), ImGuiNative.igGetColorU32_Vec4(bg2f));
            }
            // draw frame button
            else
            {
                drawList.AddRectFilled(_currentItemStartPos, _currentItemStartPos + size, ImGuiNative.igGetColorU32_Vec4(bg1f), FuThemeManager.CurrentTheme.FrameRounding);
            }

            // draw border
            if (FuThemeManager.CurrentTheme.FrameBorderSize > 0.0f)
            {
                drawList.AddRect(bb.Min, bb.Max, ImGuiNative.igGetColorU32_Col(ImGuiCol.Border, 1f), FuThemeManager.CurrentTheme.FrameRounding, 0, FuThemeManager.CurrentTheme.FrameBorderSize);
            }
            // Align whole block. We should defer that to the better rendering function when we'll have support for individual line alignment.
            Vector2 textPos = _currentItemStartPos;
            Vector2 align = FuThemeManager.CurrentTheme.ButtonTextAlign;

            if (align.x > 0.0f)
            {
                textPos.x = Mathf.Max(textPos.x, textPos.x + (bb.Max.x - textPos.x - label_size.x) * align.x);
            }
            if (align.y > 0.0f)
            {
                textPos.y = Mathf.Max(textPos.y, textPos.y + (bb.Max.y - textPos.y - label_size.y) * align.y);
            }

            // set mouse cursor
            if (hovered && !_nextIsDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            // draw text
            TextClipped(size, text, _currentItemStartPos + textOffset, padding, label_size, align);

            // display the tooltip if necessary
            displayToolTip();

            // return whatever the button is clicked
            return clicked && !_nextIsDisabled;
        }
    }
}