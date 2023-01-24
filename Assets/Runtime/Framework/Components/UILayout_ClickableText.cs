using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {
        /// <summary>
        /// Draw a clickable text element
        /// </summary>
        /// <param name="text">text to draw</param>
        /// <param name="style">style of the text to draw</param>
        /// <returns>whatever the text is clicked</returns>
        public virtual bool ClickableText(string text, UITextStyle style)
        {
            beginElement(text, style);

            Vector2 rectMin = ImGui.GetCursorScreenPos() - new Vector2(4f, 0f);
            Vector2 rectMax = rectMin + ImGui.CalcTextSize(text) + ThemeManager.CurrentTheme.FramePadding;
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
            FuGui.Push(ImGuiCol.Text, textColor);
            ImGui.Text(text);
            FuGui.PopColor();
            endElement(style);

            return clicked;
        }
    }
}