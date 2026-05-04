using Fu;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        private static Dictionary<string, bool> _collapsablesOpenStates = new Dictionary<string, bool>();
        private static Dictionary<string, string> _collapsablesGroupOpenStates = new Dictionary<string, string>();
        #endregion

        #region Methods
        /// <summary>
        /// Open a collapsable (by its id)
        /// </summary>
        /// <param name="id">id of the collapsable to open</param>
        public void OpenCollapsable(string id, string groupID = null)
        {
            if (FuWindow.CurrentDrawingWindow != null)
            {
                id = id + "##" + FuWindow.CurrentDrawingWindow.ID;
            }

            if (!string.IsNullOrEmpty(groupID))
            {
                if (_collapsablesGroupOpenStates.ContainsKey(groupID))
                {
                    _collapsablesOpenStates[_collapsablesGroupOpenStates[groupID]] = false;
                }
                _collapsablesGroupOpenStates[groupID] = id;
            }
            _collapsablesOpenStates[id] = true;
        }

        /// <summary>
        /// Close a collapsable (by its id)
        /// </summary>
        /// <param name="id">id of the collapsable to close</param>
        public void CloseCollapsable(string id, string groupID = null)
        {
            if (FuWindow.CurrentDrawingWindow != null)
            {
                id = id + "##" + FuWindow.CurrentDrawingWindow.ID;
            }
            if (groupID != null && _collapsablesGroupOpenStates.ContainsKey(groupID) && _collapsablesGroupOpenStates[groupID] == id)
            {
                _collapsablesGroupOpenStates.Remove(groupID);
            }
            _collapsablesOpenStates[id] = false;
        }

        /// <summary>
        /// Check if a collapsable is open or not
        /// </summary>
        /// <param name="id">id of the collapsable to check</param>
        public bool IsCollapsableOpen(string id)
        {
            if (FuWindow.CurrentDrawingWindow != null)
            {
                id = id + "##" + FuWindow.CurrentDrawingWindow.ID;
            }
            if (_collapsablesOpenStates.TryGetValue(id, out bool isOpen))
            {
                return isOpen;
            }
            return false;
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
        /// <param name="defaultOpen">Whether the collapsable should be open by default.</param>
        /// <param name="drawCarret">Whether to draw a caret icon indicating the collapsable state.</param>
        /// <param name="groupID">Optional group ID to manage collapsable states within a group.</param>
        /// <param name="indent"> The indentation to apply to the content within the collapsable element.</param>
        /// <param name="leftPartCustomUI"> Optional custom UI to display on the left side of the collapsable header. It receives as parameter the available height to draw in.</param>
        /// <param name="leftPartCustomUIWidth">Width of the left custom UI part.</param>
        /// <param name="paddingY">Padding to apply vertically to the collapsable header.</param>
        /// <param name="rightPartCustomUI">Optional custom UI to display on the right side of the collapsable header. It receives as parameter the available height to draw in.</param>
        /// <param name="rightPartCustomUIWidth">Width of the right custom UI part.</param>
        public void Collapsable(string text, Action innerUI, FuButtonStyle style, float indent = 16f, bool defaultOpen = true, float leftPartCustomUIWidth = 0f, Action<float> leftPartCustomUI = null, float rightPartCustomUIWidth = 0f, Action<float> rightPartCustomUI = null, bool drawCarret = true, string groupID = null, float paddingY = 4f)
        {
            // check value
            if (leftPartCustomUI == null)
            {
                leftPartCustomUIWidth = 0f;
            }
            if (rightPartCustomUI == null)
            {
                rightPartCustomUIWidth = 0f;
            }
            // Begin the element and apply the specified style
            beginElement(ref text, canBeHidden: false);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // scale left and right ui widths
            leftPartCustomUIWidth *= Fugui.CurrentContext.Scale;
            rightPartCustomUIWidth *= Fugui.CurrentContext.Scale;

            // register collapsable open state if not already done
            if (!_collapsablesOpenStates.ContainsKey(text))
            {
                _collapsablesOpenStates.Add(text, defaultOpen);
            }
            // get collapsable open state
            bool open = _collapsablesOpenStates[text];

            // Adjust the padding and spacing for the frame and the items within it
            Fugui.Push(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 0f));
            // Set the font for the header to be bold and size 14
            Fugui.PushFont(FontType.Bold);
            // Display the collapsable header with the given identifier
            float frameInset = getCollapsableFrameInset();
            if (_customCollapsableButton(text, style, leftPartCustomUIWidth, rightPartCustomUIWidth, open, drawCarret, paddingY * Fugui.CurrentContext.Scale, frameInset))
            {
                open = !open;
                if (open && !string.IsNullOrEmpty(groupID))
                {
                    if (_collapsablesGroupOpenStates.ContainsKey(groupID))
                    {
                        _collapsablesOpenStates[_collapsablesGroupOpenStates[groupID]] = false;
                    }
                    _collapsablesGroupOpenStates[groupID] = text;
                }
                _collapsablesOpenStates[text] = open;
            }
            // Pop the font changes
            Fugui.PopFont();
            // Pop the spacing changes
            Fugui.PopStyle();

            // End the element
            endElement();

            // Draw up and down lines
            Vector2 afterHeaderCursorPos = ImGui.GetCursorScreenPos();
            Vector2 btnMin = ImGui.GetItemRectMin();
            Vector2 btnMax = ImGui.GetItemRectMax();
            applyCollapsableFrameInset(ref btnMin, ref btnMax, frameInset);
            float height = btnMax.y - btnMin.y;
            ImGui.GetWindowDrawList().AddLine(new Vector2(btnMin.x, btnMax.y), btnMax, ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.4f)));
            ImGui.GetWindowDrawList().AddLine(btnMin, new Vector2(btnMax.x, btnMin.y), ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.6f)));

            // Draw custom UI if needed
            float carretWidth = drawCarret ? 24f * Fugui.CurrentContext.Scale : 0f;
            // Draw left custom UI
            if (leftPartCustomUI != null)
            {
                ImGui.SetCursorScreenPos(new Vector2(btnMin.x + carretWidth + (2f * Fugui.CurrentContext.Scale), btnMin.y));
                leftPartCustomUI?.Invoke(height);
            }
            // Draw right custom UI
            if (rightPartCustomUI != null)
            {
                if (leftPartCustomUI != null)
                {
                    SameLine();
                }
                ImGui.SetCursorScreenPos(new Vector2(btnMax.x - rightPartCustomUIWidth, btnMin.y));
                rightPartCustomUI?.Invoke(height);
            }
            if (leftPartCustomUI != null || rightPartCustomUI != null)
            {
                ImGui.SetCursorScreenPos(afterHeaderCursorPos);
            }

            // if collapsable is open, indent content, draw it and unindent
            if (open)
            {
                // add natural spacing after lines
                ImGui.Spacing();
                if (indent != 0)
                    ImGui.Indent(indent * Fugui.CurrentContext.Scale);
                innerUI();
                if (indent != 0)
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
        private unsafe bool _customCollapsableButton(string text, FuButtonStyle style, float leftPartUIWidth, float rightPartUIWidth, bool opened, bool drawCarret, float paddingY, float frameInset)
        {
            // clamp gradient strenght
            float gradientStrenght = 1f - Mathf.Clamp(Fugui.Themes.CurrentTheme.CollapsableGradientStrenght, 0.1f, 1f);
            // add carret width to left part offset
            float carretWidth = drawCarret ? 24f * Fugui.Scale : 0f;

            // calc label size
            Vector2 label_size = ImGui.CalcTextSize(text, true);

            // get the current cursor pos so we draw at the right place
            Vector2 pos = ImGui.GetCursorScreenPos();
            frameInset = Mathf.Max(0f, frameInset);

            // calc item size
            Vector2 padding = new Vector2(Fugui.Themes.FramePadding.x, paddingY);
            Vector2 region_max = ImGui.GetContentRegionAvail() + ImGui.GetCursorScreenPos() - ImGui.GetWindowPos();
            Vector2 itemSize = new Vector2(
                Mathf.Max(4.0f, region_max.x - ImGuiNative.igGetCursorPosX()),
                label_size.y + padding.y * 2f);
            Vector2 framePos = pos + new Vector2(frameInset, 0f);
            Vector2 frameSize = new Vector2(Mathf.Max(1.0f, itemSize.x - frameInset * 2f), itemSize.y);

            bool hovered = IsItemHovered(framePos, frameSize);

            // custom process is mouse is hover full button rect
            bool hoverCustomUI = false;
            if (hovered)
            {
                // prevent btn behaviour if hovering left custom UI part
                if (leftPartUIWidth > 0f)
                {
                    Vector2 leftPartMin = framePos;
                    leftPartMin.x += carretWidth;
                    Vector2 leftPartMax = leftPartMin;
                    leftPartMax.x += leftPartUIWidth;
                    leftPartMax.y = framePos.y + frameSize.y;
                    // cancel btn click if hover custom UI
                    if (IsItemHovered(leftPartMin, leftPartMax - leftPartMin))
                    {
                        hoverCustomUI = true;
                    }
                }

                // prevent btn behaviour if hovering right custom UI part
                if (rightPartUIWidth > 0f)
                {
                    Vector2 rightPartMin = framePos;
                    rightPartMin.x += frameSize.x - rightPartUIWidth;
                    Vector2 rightPartMax = new Vector2(framePos.x + frameSize.x, framePos.y + frameSize.y);
                    // cancel btn click if hover custom UI
                    if (IsItemHovered(rightPartMin, rightPartMax - rightPartMin))
                    {
                        hoverCustomUI = true;
                    }
                }
            }

            // process element states 
            if (!hoverCustomUI)
            {
                // draw a dummy button to update cursor and get states
                ImGui.Dummy(itemSize);
                setBaseElementState(text, framePos, frameSize, true, false, !LastItemDisabled);
            }
            else
            {
                ImGui.Dummy(itemSize);
            }

            // get current draw list
            ImDrawListPtr drawList = ImGuiNative.igGetWindowDrawList();
            ImGuiStylePtr imStyle = ImGui.GetStyle();

            // get colors
            ImRect bb = new ImRect() { Min = framePos, Max = framePos + frameSize };
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
                drawList.AddRectFilled(framePos, framePos + frameSize, ImGuiNative.igGetColorU32_Vec4(bg1f), imStyle.FrameRounding);
                int vert_end_idx = drawList.VtxBuffer.Size;
                ImGuiInternal.igShadeVertsLinearColorGradientKeepAlpha(drawList.NativePtr, vert_start_idx, vert_end_idx, framePos, bb.GetBL(), ImGuiNative.igGetColorU32_Vec4(bg1f), ImGuiNative.igGetColorU32_Vec4(bg2f));
            }
            // draw frame button
            else
            {
                drawList.AddRectFilled(framePos, framePos + frameSize, ImGuiNative.igGetColorU32_Vec4(bg1f), imStyle.FrameRounding);
            }

            Vector2 align = new Vector2(0f, 0.5f);
            // set mouse cursor
            if (_lastItemHovered && !LastItemDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            // draw text
            Vector2 textSize = frameSize;
            textSize.x -= leftPartUIWidth;
            textSize.x -= rightPartUIWidth;
            textSize.x -= carretWidth;
            Vector3 txtPos = framePos;
            txtPos.x += leftPartUIWidth + carretWidth;

            EnboxedText(text, txtPos, textSize, Vector2.zero, Vector2.zero, new Vector2(0f, 0.5f), FuTextWrapping.Clip);

            // draw carret
            if (drawCarret)
            {
                Color caretColor = LastItemDisabled ? style.TextStyle.DisabledText : style.TextStyle.Text;
                if (!_lastItemHovered && !LastItemDisabled)
                {
                    caretColor *= 0.8f;
                }
                if (opened)
                {
                    Fugui.DrawCarret_Down(ImGui.GetWindowDrawList(), new Vector2(framePos.x + carretWidth / 3f, framePos.y + 1f), carretWidth / 3f, frameSize.y, caretColor);
                }
                else
                {
                    Fugui.DrawCarret_Right(ImGui.GetWindowDrawList(), new Vector2(framePos.x + carretWidth / 3f, framePos.y + 1f), carretWidth / 3f, frameSize.y, caretColor);
                }
            }

            // display the tooltip if necessary
            displayToolTip();

            // return whatever the button is clicked
            return _lastItemUpdate;
        }

        private static float getCollapsableFrameInset()
        {
            if (FuPanel.IsInsidePanel)
            {
                return Mathf.Ceil(Mathf.Max(0f, Fugui.Themes.ChildBorderSize));
            }
            if (FuWindow.CurrentDrawingWindow != null)
            {
                return Mathf.Ceil(Mathf.Max(0f, Fugui.Themes.WindowBorderSize));
            }
            return 0f;
        }

        private static void applyCollapsableFrameInset(ref Vector2 min, ref Vector2 max, float frameInset)
        {
            if (frameInset <= 0f)
            {
                return;
            }

            min.x += frameInset;
            max.x -= frameInset;
            if (max.x < min.x)
            {
                float center = (min.x + max.x) * 0.5f;
                min.x = center;
                max.x = center;
            }
        }
        #endregion
    }
}
