using ImGuiNET;
using System.Diagnostics;
using UnityEngine;
using System;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        /// <summary>
        /// Draw a clickable text element
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="style">style of the text to draw</param>
        /// <returns>whatever the text is clicked</returns>
        public virtual bool ClickableText(string text, FuTextStyle style)
        {
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            Vector2 rectMin = ImGui.GetCursorScreenPos() - new Vector2(4f, 0f);
            Vector2 rectMax = rectMin + ImGui.CalcTextSize(text) + FuThemeManager.CurrentTheme.FramePadding;
            bool hovered = ImGui.IsMouseHoveringRect(rectMin, rectMax);
            bool active = hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            bool clicked = hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left);

            if (hovered)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            Color textColor = style.Text;
            if (active)
            {
                textColor *= 0.8f;
            }
            else if (hovered)
            {
                textColor *= 0.9f;
            }
            Fugui.Push(ImGuiCol.Text, textColor);
            ImGui.Text(text);
            Fugui.PopColor();
            endElement(style);

            return clicked;
        }

        /// <summary>
        /// Draw a clickable URL text element
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="URL">URL to open on text click</param>
        /// <param name="style">style of the text to draw</param>
        /// <returns>whatever the text is clicked</returns>
        public virtual void TextURL(string text, string URL, FuTextStyle style)
        {
            beginElement(ref text, style, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            Vector2 rectMin = ImGui.GetCursorScreenPos() - new Vector2(4f, 0f);
            Vector2 rectMax = rectMin + ImGui.CalcTextSize(text) + FuThemeManager.CurrentTheme.FramePadding;
            bool hovered = ImGui.IsMouseHoveringRect(rectMin, rectMax);
            bool active = hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);
            bool clicked = hovered && ImGui.IsMouseReleased(ImGuiMouseButton.Left);

            Color textColor = style.Text;
            if (active)
            {
                textColor *= 0.8f;
            }
            else if (hovered)
            {
                textColor *= 0.9f;
            }
            Fugui.Push(ImGuiCol.Text, textColor);
            ImGui.Text(text);
            if (hovered)
            {
                SetToolTip(URL, FuTextStyle.Default);
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                AddUnderLine();
            }
            Fugui.PopColor();

            if (clicked)
            {
                try
                {
                    Process.Start(URL);
                }
                catch (Exception ex)
                {
                    Fugui.Notify("fail to open URI", ex.Message, StateType.Danger);
                }
            }

            endElement(style);
        }
    }
}