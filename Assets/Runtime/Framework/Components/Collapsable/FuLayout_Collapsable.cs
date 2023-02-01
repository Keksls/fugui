using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        /// <summary>
        /// Displays a collapsable UI element with the given identifier and content.
        /// </summary>
        /// <param name="id">The identifier of the element.</param>
        /// <param name="innerUI">The content to display within the collapsable element.</param>
        public void Collapsable(string id, Action innerUI, float indent = 16f)
        {
            // Use the default style for the collapsable element
            Collapsable(id, innerUI, FuCollapsableStyle.Default, indent);
        }

        /// <summary>
        /// Displays a collapsable UI element with the given identifier, content, and style.
        /// </summary>
        /// <param name="id">The identifier of the element.</param>
        /// <param name="innerUI">The content to display within the collapsable element.</param>
        /// <param name="style">The style to apply to the element.</param>
        public void Collapsable(string id, Action innerUI, FuCollapsableStyle style, float indent = 16f)
        {
            // Begin the element and apply the specified style
            beginElement(ref id, style);
            // return if item must no be draw
            if (!_drawItem)
            {
                return;
            }

            // Adjust the padding and spacing for the frame and the items within it
            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 4f) * Fugui.CurrentContext.Scale);
            Fugui.Push(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));

            // Set the font for the header to be bold and size 14
            Fugui.PushFont(14, FontType.Bold);
            // Display the collapsable header with the given identifier
            bool open = ImGui.CollapsingHeader(id, ImGuiTreeNodeFlags.CollapsingHeader | ImGuiTreeNodeFlags.DefaultOpen);
            // Pop the font changes
            Fugui.PopFont();
            // Display the tool tip for the element
            displayToolTip();
            // Pop the padding and spacing changes
            Fugui.PopStyle(2);
            // End the element
            endElement(style);

            // Draw up and down lines
            Vector2 min = ImGui.GetItemRectMin();
            Vector2 max = ImGui.GetItemRectMax();
            ImGui.GetWindowDrawList().AddLine(new Vector2(min.x, max.y), max, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.4f)));
            ImGui.GetWindowDrawList().AddLine(min, new Vector2(max.x, min.y), ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.6f)));

            // if collapsable is open, indent content, draw it and unindent
            if (open)
            {
                // dummy for natural frame padding after lines
                ImGui.Dummy(new Vector2(0f, 0f));
                ImGui.Indent(indent * Fugui.CurrentContext.Scale);
                innerUI();
                ImGui.Indent(-indent * Fugui.CurrentContext.Scale);
            }
        }
    }
}