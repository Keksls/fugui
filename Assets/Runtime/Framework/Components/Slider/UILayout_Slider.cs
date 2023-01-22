using ImGuiNET;
using System;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {
        #region Slider Int
        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref int value)
        {
            return Slider(text, ref value, 0, 100);
        }

        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">The minimum value that the user can select.</param>
        /// <param name="max">The maximum value that the user can select.</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref int value, int min, int max)
        {
            float val = value;
            bool valueChange = _customSlider(text, ref val, min, max, true);
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
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref float value)
        {
            return Slider(text, ref value, 0f, 100f);
        }

        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">The minimum value that the user can select.</param>
        /// <param name="max">The maximum value that the user can select.</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref float value, float min, float max)
        {
            return _customSlider(text, ref value, min, max, false);
        }
        #endregion

        /// <summary>
        /// Draw a custom unity-style slider (Label + slider + input)
        /// </summary>
        /// <param name="text">Label and ID of the slider</param>
        /// <param name="value">refered value of the slider</param>
        /// <param name="min">minimum value of the slider</param>
        /// <param name="max">maximum value of the slider</param>
        /// <param name="isInt">whatever the slider is an Int slider (default is float). If true, the value will be rounded</param>
        /// <returns>true if value changed</returns>
        protected virtual bool _customSlider(string text, ref float value, float min, float max, bool isInt)
        {
            text = beginElement(text, null);

            // Calculate the position and size of the slider
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            float knobRadius = 5f;
            float hoverPaddingY = 4f;
            float height = 20f;
            float lineHeight = 2f;
            float width = ImGui.GetContentRegionAvail().x - 60f;
            float x = cursorPos.x;
            float y = cursorPos.y + height / 2f;
            float oldValue = value;

            // is there place to draw slider
            if (width >= 24f)
            {
                // Calculate the position of the knob
                float knobPos = (x + knobRadius) + (width - knobRadius * 2f) * (value - min) / (max - min);
                // Check if the mouse is hovering over the slider
                bool isLineHovered = ImGui.IsMouseHoveringRect(new Vector2(x, y - hoverPaddingY - lineHeight), new Vector2(x + width, y + hoverPaddingY + lineHeight));
                // Check if the mouse is hovering over the knob
                bool isKnobHovered = ImGui.IsMouseHoveringRect(new Vector2(knobPos - knobRadius, y - knobRadius), new Vector2(knobPos + knobRadius, y + knobRadius));
                // Check if slider is dragging
                bool isDragging = _draggingSliders.Contains(text);
                // Calculate colors
                Vector4 leftLineColor = ThemeManager.GetColor(FuguiColors.CheckMark);
                Vector4 rightLineColor = ThemeManager.GetColor(FuguiColors.FrameBg);
                Vector4 knobColor = ThemeManager.GetColor(FuguiColors.Knob);
                if (_nextIsDisabled)
                {
                    leftLineColor *= 0.5f;
                    rightLineColor *= 0.5f;
                    knobColor *= 0.5f;
                }
                else
                {
                    if (isDragging)
                    {
                        knobColor = ThemeManager.GetColor(FuguiColors.KnobActive);
                    }
                    else if (isKnobHovered)
                    {
                        knobColor = ThemeManager.GetColor(FuguiColors.KnobHovered);
                    }
                }
                // Draw the left slider line
                ImGui.GetWindowDrawList().AddLine(new Vector2(x, y), new Vector2(knobPos, y), ImGui.GetColorU32(leftLineColor), lineHeight);
                // Draw the right slider line
                ImGui.GetWindowDrawList().AddLine(new Vector2(knobPos, y), new Vector2(x + width, y), ImGui.GetColorU32(rightLineColor), lineHeight);

                // Draw the knob
                if (!_nextIsDisabled)
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
                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPos, y), ((isKnobHovered || isDragging) && !_nextIsDisabled ? knobRadius : knobRadius * 0.8f) + 1f, ImGui.GetColorU32(ImGuiCol.Border), 32);
                // draw knob
                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPos, y), (isKnobHovered || isDragging) && !_nextIsDisabled ? knobRadius : knobRadius * 0.8f, ImGui.GetColorU32(knobColor), 32);

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
                if (_draggingSliders.Contains(text) && ImGui.IsMouseDown(0) && !_nextIsDisabled)
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

                ImGui.Dummy(new Vector2(width, 0f));
                ImGui.SameLine();
            }

            // draw drag input
            ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().x - 52f - 4f, 0f));
            ImGui.SameLine();
            string formatString = getFloatString("##sliderInput" + text, value);
            ImGui.PushItemWidth(52f);
            if (ImGui.InputFloat("##sliderInput" + text, ref value, 0f, 0f, isInt ? "%.0f" : formatString, _nextIsDisabled ? ImGuiInputTextFlags.ReadOnly : ImGuiInputTextFlags.None))
            {
                // Clamp the value to the min and max range
                value = Math.Clamp(value, min, max);
                if (isInt)
                {
                    value = (int)value;
                }
            }
            ImGui.PopItemWidth();
            updateFloatString("##sliderInput" + text, value);
            displayToolTip();
            _elementHoverFramed = true;
            endElement(null);
            return value != oldValue;
        }
    }
}