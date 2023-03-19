using Fu.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        private static Dictionary<string, bool> _collapsablesOpenStates = new Dictionary<string, bool>();

        /// <summary>
        /// Open a collapsable (by its id)
        /// </summary>
        /// <param name="id">id of the collapsable to open</param>
        public void OpenCollapsable(string id)
        {
            if (FuWindow.CurrentDrawingWindow != null)
            {
                id = id + "##" + FuWindow.CurrentDrawingWindow.ID;
            }
            if (_collapsablesOpenStates.ContainsKey(id))
            {
                _collapsablesOpenStates[id] = true;
            }
        }

        /// <summary>
        /// Close a collapsable (by its id)
        /// </summary>
        /// <param name="id">id of the collapsable to close</param>
        public void CloseCollapsable(string id)
        {
            if (FuWindow.CurrentDrawingWindow != null)
            {
                id = id + "##" + FuWindow.CurrentDrawingWindow.ID;
            }
            if (_collapsablesOpenStates.ContainsKey(id))
            {
                _collapsablesOpenStates[id] = false;
            }
        }

        /// <summary>
        /// Displays a collapsable UI element with the given identifier and content.
        /// </summary>
        /// <param name="id">The identifier of the element.</param>
        /// <param name="innerUI">The content to display within the collapsable element.</param>
        public void Collapsable(string id, Action innerUI, float indent = 16f)
        {
            // Use the default style for the collapsable element
            Collapsable(id, innerUI, FuButtonStyle.Collapsable, indent);
        }

        /// <summary>
        /// Displays a collapsable UI element with the given identifier, content, and style.
        /// </summary>
        /// <param name="text">The identifier of the element.</param>
        /// <param name="innerUI">The content to display within the collapsable element.</param>
        /// <param name="style">The style to apply to the element.</param>
        public void Collapsable(string text, Action innerUI, FuButtonStyle style, float indent = 16f, bool defaultOpen = true, float leftPartCustomUIWidth = 0f, Action leftPartCustomUI = null, float rightPartCustomUIWidth = 0f, Action rightPartCustomUI = null)
        {
            // Begin the element and apply the specified style
            beginElement(ref text, canBeHidden: false);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // register collapsable open state if not already done
            if (!_collapsablesOpenStates.ContainsKey(text))
            {
                _collapsablesOpenStates.Add(text, true);
            }
            // get collapsable open state
            bool open = _collapsablesOpenStates[text];

            // Adjust the padding and spacing for the frame and the items within it
            Fugui.Push(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
            // Set the font for the header to be bold and size 14
            Fugui.PushFont(14, FontType.Bold);
            // Display the collapsable header with the given identifier
            if (_customCollapsableButton(text, style, leftPartCustomUIWidth, rightPartCustomUIWidth, open))
            {
                open = !open;
                _collapsablesOpenStates[text] = open;
            }
            // Pop the font changes
            Fugui.PopFont();
            // Pop the spacing changes
            Fugui.PopStyle();

            // End the element
            endElement();

            // Draw up and down lines
            Vector2 min = ImGui.GetItemRectMin();
            Vector2 max = ImGui.GetItemRectMax();
            ImGui.GetWindowDrawList().AddLine(new Vector2(min.x, max.y), max, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.4f)));
            ImGui.GetWindowDrawList().AddLine(min, new Vector2(max.x, min.y), ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.6f)));

            // Draw custom UI if needed
            Vector2 btnMin = ImGui.GetItemRectMin();
            Vector2 btnMax = ImGui.GetItemRectMax();
            float carretWidth = 24f * Fugui.CurrentContext.Scale;
            // Draw left custom UI
            if (leftPartCustomUI != null)
            {
                ImGui.SetCursorScreenPos(new Vector2(btnMin.x + carretWidth, btnMin.y + 2f));
                leftPartCustomUI?.Invoke();
            }
            // Draw right custom UI
            if (rightPartCustomUI != null)
            {
                if (leftPartCustomUI != null)
                {
                    SameLine();
                }
                ImGui.SetCursorScreenPos(new Vector2(btnMax.x - rightPartCustomUIWidth, btnMin.y));
                rightPartCustomUI?.Invoke();
            }

            // if collapsable is open, indent content, draw it and unindent
            if (open)
            {
                // add natural spacing after lines
                ImGui.Spacing();
                ImGui.Indent(indent * Fugui.CurrentContext.Scale);
                innerUI();
                ImGui.Indent(-indent * Fugui.CurrentContext.Scale);
            }
        }

        /// <summary>
        /// Render a custom collapsable button with the given style and size
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="style">style of the button</param>
        /// <returns>true if clicked</returns>
        /// <param name="leftPartUIWidth">width of the left part UI</param>
        /// <param name="rightPartUIWidth">width of the right part UI</param>
        /// <param name="opened">whatever the collapsable is opened right now</param>
        /// <returns>true if clicked</returns>
        private unsafe bool _customCollapsableButton(string text, FuButtonStyle style, float leftPartUIWidth, float rightPartUIWidth, bool opened)
        {
            // clamp gradient strenght
            float gradientStrenght = 1f - Mathf.Clamp(FuThemeManager.CurrentTheme.CollapsableGradientStrenght, 0.1f, 1f);

            // add carret width to left part offset
            float carretWidth = 24f * Fugui.CurrentContext.Scale;
            leftPartUIWidth += carretWidth;

            // calc label size
            Vector2 label_size = ImGui.CalcTextSize(text, true);

            // get the current cursor pos so we draw at the right place
            Vector2 pos = ImGui.GetCursorScreenPos();

            // calc item size
            Vector2 padding = FuThemeManager.CurrentTheme.FramePadding * Fugui.CurrentContext.Scale;
            Vector2 region_max = ImGui.GetContentRegionMax();
            Vector2 size = new Vector2(
                Mathf.Max(4.0f, region_max.x - ImGuiNative.igGetCursorPosX()),
                label_size.y + padding.y * 2f);

            bool hovered = isItemHovered(pos, size);

            // custom process is mouse is hover full button rect
            bool hoverCustomUI = false;
            if (hovered)
            {
                // prevent btn behaviour if hovering left custom UI part
                if (leftPartUIWidth > carretWidth)
                {
                    Vector2 leftPartMin = pos;
                    leftPartMin.x += carretWidth;
                    Vector2 leftPartMax = leftPartMin;
                    leftPartMax.x += (leftPartUIWidth - carretWidth);
                    leftPartMax.y = pos.y + size.y;
                    // cancel btn click if hover custom UI
                    if (ImGui.IsMouseHoveringRect(leftPartMin, leftPartMax))
                    {
                        hoverCustomUI = true;
                    }
                }

                // prevent btn behaviour if hovering right custom UI part
                if (rightPartUIWidth > 0f)
                {
                    Vector2 rightPartMin = pos;
                    rightPartMin.x += size.x - rightPartUIWidth;
                    Vector2 rightPartMax = new Vector2(pos.x + size.x, pos.y + size.y);
                    // cancel btn click if hover custom UI
                    if (ImGui.IsMouseHoveringRect(rightPartMin, rightPartMax))
                    {
                        hoverCustomUI = true;
                    }
                }
            }

            // process element states 
            if (!hoverCustomUI)
            {
                // draw a dummy button to update cursor and get states
                bool clicked = ImGui.InvisibleButton(text, size, ImGuiButtonFlags.None) && !LastItemDisabled;
                setBaseElementState(text, pos, size, true, clicked);
            }
            else
            {
                ImGui.Dummy(size);
            }

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

            Vector2 align = new Vector2(0f, 0.5f);
            // set mouse cursor
            if (_lastItemHovered && !LastItemDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            // draw text
            size.x -= leftPartUIWidth;
            size.x -= rightPartUIWidth;
            _customTextClipped(size, text, pos + new Vector2(leftPartUIWidth, 0f), padding, label_size, align, style.TextStyle);

            // draw carret
            Color caretColor = LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text;
            if (!_lastItemHovered && !LastItemDisabled)
            {
                caretColor *= 0.8f;
            }
            if (opened)
            {
                Fugui.DrawCarret_Down(ImGui.GetWindowDrawList(), new Vector2(pos.x + carretWidth / 3f, pos.y + 1f), carretWidth / 3f, size.y, caretColor);
            }
            else
            {
                Fugui.DrawCarret_Right(ImGui.GetWindowDrawList(), new Vector2(pos.x + carretWidth / 3f, pos.y + 1f), carretWidth / 3f, size.y, caretColor);
            }

            // display the tooltip if necessary
            displayToolTip();

            // return whatever the button is clicked
            return _lastItemUpdate;
        }
    }
}