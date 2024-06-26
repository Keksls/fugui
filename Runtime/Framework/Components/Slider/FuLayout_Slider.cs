﻿using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        #region Slider Int
        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref int value, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            return Slider(text, ref value, 0, 100, flags, format);
        }

        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">The minimum value that the user can select.</param>
        /// <param name="max">The maximum value that the user can select.</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref int value, int min, int max, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            float val = value;
            bool valueChange = _customSlider(text, ref val, min, max, true, 1f, flags, format);
            value = (int)val;
            return valueChange;
        }
        #endregion

        #region Slider Float
        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="step">step of the slider value change</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref float value, float step = 0.01f, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            return Slider(text, ref value, 0f, 100f, step, flags, format);
        }

        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">The minimum value that the user can select.</param>
        /// <param name="max">The maximum value that the user can select.</param>
        /// <param name="step">step of the slider value change</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref float value, float min, float max, float step = 0.01f, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            return _customSlider(text, ref value, min, max, false, step, flags, format);
        }
        #endregion

        /// <summary>
        /// Draw a custom unity-style slider (slider + input)
        /// </summary>
        /// <param name="text">Label and ID of the slider</param>
        /// <param name="value">refered value of the slider</param>
        /// <param name="min">minimum value of the slider</param>
        /// <param name="max">maximum value of the slider</param>
        /// <param name="isInt">whatever the slider is an Int slider (default is float). If true, the value will be rounded</param>
        /// <param name="step">step of the slider value change</param>
        /// <param name="flags">behaviour flag of the slider</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>true if value changed</returns>
        protected virtual bool _customSlider(string text, ref float value, float min, float max, bool isInt, float step, FuSliderFlags flags, string format)
        {
            beginElement(ref text, FuFrameStyle.Default);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            // Calculate the position and size of the slider
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            float knobRadius = 5f * Fugui.CurrentContext.Scale;
            float hoverPaddingY = 5f * Fugui.CurrentContext.Scale;
            float height = ImGui.CalcTextSize("Ap").y + (ImGui.GetStyle().FramePadding.y * 2f);
            float lineHeight = 2f * Fugui.CurrentContext.Scale;
            float dragWidth = 52f * Fugui.CurrentContext.Scale;
            float width = ImGui.GetContentRegionAvail().x - dragWidth - (8f * Fugui.CurrentContext.Scale);
            if (flags.HasFlag(FuSliderFlags.NoDrag))
            {
                width += dragWidth + (8f * Fugui.CurrentContext.Scale);
            }
            float x = cursorPos.x;
            float y = cursorPos.y + height / 2f;
            float oldValue = value;

            switch (flags)
            {
                default:
                case FuSliderFlags.Default:
                    if (drawSlider(text, ref value, min, max, isInt, knobRadius, hoverPaddingY, lineHeight, width, x, y))
                    {
                        ImGui.SameLine();
                    }
                    drawDrag(text, ref value, min, max, isInt);
                    break;

                case FuSliderFlags.LeftDrag:
                    drawDrag(text, ref value, min, max, isInt);
                    _elementHoverFramedEnabled = true;
                    DrawHoverFrame();
                    x += dragWidth + (8f * Fugui.CurrentContext.Scale);
                    ImGui.SameLine();
                    drawSlider(text, ref value, min, max, isInt, knobRadius, hoverPaddingY, lineHeight, width, x, y);
                    _elementHoverFramedEnabled = false;
                    break;

                case FuSliderFlags.NoDrag:
                    drawSlider(text, ref value, min, max, isInt, knobRadius, hoverPaddingY, lineHeight, width, x, y);
                    break;
            }

            // clamp step
            float tmp = value / step;
            tmp = Mathf.Round(tmp);
            value = tmp * step;

            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, value != oldValue);

            endElement(FuFrameStyle.Default);
            return value != oldValue;

            // function that draw the slider drag
            void drawDrag(string text, ref float value, float min, float max, bool isInt)
            {
                string formatString = format != null ? format : getStringFormat(value);
                ImGui.PushItemWidth(dragWidth);
                if (ImGui.InputFloat("##" + text, ref value, 0f, 0f, isInt ? "%.0f" : formatString, LastItemDisabled ? ImGuiInputTextFlags.ReadOnly : ImGuiInputTextFlags.None))
                {
                    // Clamp the value to the min and max range
                    value = Math.Clamp(value, min, max);
                    if (isInt)
                    {
                        value = (int)value;
                    }
                }
                ImGui.PopItemWidth();
                displayToolTip();
                _elementHoverFramedEnabled = true;
                DrawHoverFrame();
            }

            // function that draw the slider
            bool drawSlider(string text, ref float value, float min, float max, bool isInt, float knobRadius, float hoverPaddingY, float lineHeight, float width, float x, float y)
            {
                // draw a dummy button so the cursor is well placed and we can get the state and prevent from clicking on multiple elements at time + get the state only if the widget is visible
                ImGui.InvisibleButton(text, new Vector2(width + (4f * Fugui.CurrentContext.Scale), height), ImGuiButtonFlags.None);

                // is there place to draw slider
                if (width >= 24f)
                {
                    // Calculate the position of the knob
                    float knobPos = (x + knobRadius) + (width - knobRadius * 2f) * (value - min) / (max - min);
                    // Check if the mouse is hovering over the slider
                    bool isLineHovered = IsItemHovered(new Vector2(x, y - hoverPaddingY - lineHeight), new Vector2(width, hoverPaddingY * 2f + lineHeight * 2f));
                    // Check if the mouse is hovering over the knob
                    bool isKnobHovered = IsItemHovered(new Vector2(knobPos - knobRadius, y - knobRadius), new Vector2(knobRadius * 2f, knobRadius * 2f));
                    // Check if slider is dragging
                    bool isDragging = _draggingSliders.Contains(text);
                    // Calculate colors
                    Vector4 leftLineColor = FuThemeManager.GetColor(FuColors.CheckMark);
                    Vector4 rightLineColor = FuThemeManager.GetColor(FuColors.FrameBg);
                    Vector4 knobColor = FuThemeManager.GetColor(FuColors.Knob);
                    if (LastItemDisabled)
                    {
                        leftLineColor *= 0.5f;
                        rightLineColor *= 0.5f;
                        knobColor *= 0.5f;
                    }
                    else
                    {
                        if (isDragging)
                        {
                            knobColor = FuThemeManager.GetColor(FuColors.KnobActive);
                        }
                        else if (isKnobHovered)
                        {
                            knobColor = FuThemeManager.GetColor(FuColors.KnobHovered);
                        }
                    }
                    // Draw the left slider line
                    ImGui.GetWindowDrawList().AddLine(new Vector2(x, y), new Vector2(knobPos, y), ImGui.GetColorU32(leftLineColor), lineHeight);
                    // Draw the right slider line
                    ImGui.GetWindowDrawList().AddLine(new Vector2(knobPos, y), new Vector2(x + width, y), ImGui.GetColorU32(rightLineColor), lineHeight);

                    // set mouse cursor
                    if ((isKnobHovered || isLineHovered) && !LastItemDisabled)
                    {
                        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    }

                    // Draw the knob
                    if (!LastItemDisabled)
                    {
                        if (_draggingSliders.Contains(text))
                        {
                            knobColor *= 0.7f;
                        }
                        else if (isKnobHovered)
                        {
                            knobColor *= 0.9f;
                        }
                        knobColor.w = 1f;
                    }
                    // draw knob border
                    ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPos, y), ((isKnobHovered || isDragging) && !LastItemDisabled ? knobRadius : knobRadius * 0.8f) + 1f, ImGui.GetColorU32(ImGuiCol.Border), 32);
                    // draw knob
                    ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPos, y), (isKnobHovered || isDragging) && !LastItemDisabled ? knobRadius : knobRadius * 0.8f, ImGui.GetColorU32(knobColor), 32);

                    // start dragging this slider
                    if ((isLineHovered || isKnobHovered) && !_draggingSliders.Contains(text) && ImGui.IsMouseClicked(0))
                    {
                        _draggingSliders.Add(text);
                    }

                    // stop dragging this slider
                    if (_draggingSliders.Contains(text) && !ImGui.IsMouseDown(0))
                    {
                        _draggingSliders.Remove(text);
                    }

                    // If the mouse is hovering over the knob, change the value when the mouse is clicked
                    if (_draggingSliders.Contains(text) && ImGui.IsMouseDown(0) && !LastItemDisabled)
                    {
                        // Calculate the new value based on the mouse position
                        float mouseX = ImGui.GetMousePos().x;
                        value = min + (mouseX - x - knobRadius) / (width - knobRadius * 2f) * (max - min);

                        // Clamp the value to the min and max range
                        value = Math.Clamp(value, min, max);
                        if (isInt)
                        {
                            value = (int)value;
                        }
                    }
                    return true;
                }
                return false;
            }
        }
    }
}