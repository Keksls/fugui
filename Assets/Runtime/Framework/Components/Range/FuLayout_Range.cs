using ImGuiNET;
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
        /// <param name="valueMin">The minimum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="valueMax">The maximum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Range(string text, ref int valueMin, ref int valueMax, FuSliderFlags flags = FuSliderFlags.Default)
        {
            return Range(text, ref valueMin, ref valueMax, 0, 100, flags);
        }

        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="valueMin">The minimum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="valueMax">The maximum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">The minimum value that the user can select.</param>
        /// <param name="max">The maximum value that the user can select.</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Range(string text, ref int valueMin, ref int valueMax, int min, int max, FuSliderFlags flags = FuSliderFlags.Default)
        {
            float valMin = valueMin;
            float valMax = valueMax;
            bool valueChange = _customRange(text, ref valMin, ref valMax, min, max, true, 1f, flags);
            valueMin = (int)valMin;
            valueMax = (int)valMax;
            return valueChange;
        }
        #endregion

        #region Slider Float
        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="valueMin">The minimum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="valueMax">The maximum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="step">step of the slider value change</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Range(string text, ref float valueMin, ref float valueMax, float step = 0.01f, FuSliderFlags flags = FuSliderFlags.Default)
        {
            return Range(text, ref valueMin, ref valueMax, 0f, 100f, step, flags);
        }

        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="valueMin">The minimum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="valueMax">The maximum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">The minimum value that the user can select.</param>
        /// <param name="max">The maximum value that the user can select.</param>
        /// <param name="step">step of the slider value change</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Range(string text, ref float valueMin, ref float valueMax, float min, float max, float step = 0.01f, FuSliderFlags flags = FuSliderFlags.Default)
        {
            return _customRange(text, ref valueMin, ref valueMax, min, max, false, step, flags);
        }
        #endregion

        /// <summary>
        /// Draw a custom unity-style slider (slider + input)
        /// </summary>
        /// <param name="text">Label and ID of the slider</param>
        /// <param name="valueMin">The minimum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="valueMax">The maximum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">minimum value of the slider</param>
        /// <param name="max">maximum value of the slider</param>
        /// <param name="isInt">whatever the slider is an Int slider (default is float). If true, the value will be rounded</param>
        /// <param name="step">step of the slider value change</param>
        /// <param name="flags">behaviour flag of the slider</param>
        /// <returns>true if value changed</returns>
        protected virtual bool _customRange(string text, ref float valueMin, ref float valueMax, float min, float max, bool isInt, float step, FuSliderFlags flags)
        {
            beginElement(ref text, FuFrameStyle.Default);
            // return if item must no be draw
            if (!_drawItem)
            {
                return false;
            }

            // Calculate the position and size of the slider
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            float knobRadius = 5f * Fugui.CurrentContext.Scale;
            float hoverPaddingY = 4f * Fugui.CurrentContext.Scale;
            float height = 20f * Fugui.CurrentContext.Scale;
            float lineHeight = 2f * Fugui.CurrentContext.Scale;
            float width = ImGui.GetContentRegionAvail().x - (8f * Fugui.CurrentContext.Scale);
            float x = cursorPos.x;
            float y = cursorPos.y + height / 2f;
            float dragWidth = width / 2f - ImGui.CalcTextSize("Max").x;

            // draw slider and drags
            bool updated = drawSlider(text, ref valueMin, ref valueMax, min, max, isInt, knobRadius, hoverPaddingY, lineHeight, width, x, y);
            if (!flags.HasFlag(FuSliderFlags.NoDrag))
            {
                updated |= drawDrag("Min", "##Min" + text, ref valueMin, min, valueMax, isInt);
                ImGuiNative.igSameLine(0f, -1f);
                updated |= drawDrag("Max", " ##Max" + text, ref valueMax, valueMin, max, isInt);
            }

            // draw fake dummy to help clipper work properly
            ImGuiNative.igSameLine(0f, -1f);
            float maxY = ImGui.GetItemRectMax().y;
            ImGui.SetCursorScreenPos(cursorPos);
            ImGui.Dummy(new Vector2(width, maxY - cursorPos.y));

            // do not draw hover frame
            _elementHoverFramed = false;
            // end the element
            endElement(FuFrameStyle.Default);
            return updated;

            // function that draw the slider drag
            bool drawDrag(string text, string id, ref float value, float min, float max, bool isInt)
            {
                ImGui.PushItemWidth(dragWidth);
                bool updated = false;
                ImGui.Text(text);
                ImGui.SameLine();
                if (ImGui.DragFloat(id, ref value, step, min, max, isInt ? "%.0f" : getFloatString(value), _nextIsDisabled ? ImGuiSliderFlags.NoInput : ImGuiSliderFlags.AlwaysClamp))
                {
                    updated = true;
                    // Clamp the value to the min and max range
                    value = Math.Clamp(value, min, max);
                    if (isInt)
                    {
                        value = (int)value;
                    }
                }
                ImGui.PopItemWidth();
                //updateFloatString("##sliderInput" + text, value);
                displayToolTip();
                _elementHoverFramed = true;
                drawHoverFrame();
                _elementHoverFramed = false;
                return updated;
            }

            // function that draw the slider
            bool drawSlider(string id, ref float valueMin, ref float valueMax, float min, float max, bool isInt, float knobRadius, float hoverPaddingY, float lineHeight, float width, float x, float y)
            {
                // get knobs ids
                string knobMinID = id + "KnobMin";
                string knobMaxID = id + "KnobMax";
                // Calculate the position of the knob
                float knobPosMin = (x + knobRadius) + (width - knobRadius * 2f) * (valueMin - min) / (max - min);
                float knobPosMax = (x + knobRadius) + (width - knobRadius * 2f) * (valueMax - min) / (max - min);
                // Check if the mouse is hovering over the knob
                bool isKnobMinHovered = ImGui.IsMouseHoveringRect(new Vector2(knobPosMin - knobRadius, y - knobRadius), new Vector2(knobPosMin + knobRadius, y + knobRadius));
                bool isKnobMaxHovered = ImGui.IsMouseHoveringRect(new Vector2(knobPosMax - knobRadius, y - knobRadius), new Vector2(knobPosMax + knobRadius, y + knobRadius));
                // Check if slider is dragging
                bool isDraggingMin = _draggingSliders.Contains(knobMinID);
                bool isDraggingMax = _draggingSliders.Contains(knobMaxID);

                // Calculate colors
                Vector4 insideLineColor = FuThemeManager.GetColor(FuColors.CheckMark);
                Vector4 outsideLineColor = FuThemeManager.GetColor(FuColors.FrameBg);
                Vector4 knobColorMin = FuThemeManager.GetColor(FuColors.Knob);
                Vector4 knobColorMax = FuThemeManager.GetColor(FuColors.Knob);
                if (_nextIsDisabled)
                {
                    insideLineColor *= 0.5f;
                    outsideLineColor *= 0.5f;
                    knobColorMin *= 0.5f;
                    knobColorMax *= 0.5f;
                }
                else
                {
                    // min knob
                    if (isDraggingMin)
                    {
                        knobColorMin = FuThemeManager.GetColor(FuColors.KnobActive);
                    }
                    else if (isKnobMinHovered)
                    {
                        knobColorMin = FuThemeManager.GetColor(FuColors.KnobHovered);
                    }
                    // max knob
                    if (isDraggingMax)
                    {
                        knobColorMax = FuThemeManager.GetColor(FuColors.KnobActive);
                    }
                    else if (isKnobMaxHovered)
                    {
                        knobColorMax = FuThemeManager.GetColor(FuColors.KnobHovered);
                    }
                }

                // Draw the left slider line
                ImGui.GetWindowDrawList().AddLine(new Vector2(x, y), new Vector2(knobPosMin, y), ImGui.GetColorU32(outsideLineColor), lineHeight);
                // Draw the center slider line
                ImGui.GetWindowDrawList().AddLine(new Vector2(knobPosMin, y), new Vector2(knobPosMax, y), ImGui.GetColorU32(insideLineColor), lineHeight);
                // Draw the right slider line
                ImGui.GetWindowDrawList().AddLine(new Vector2(knobPosMax, y), new Vector2(x + width, y), ImGui.GetColorU32(outsideLineColor), lineHeight);

                // Draw the knobs
                if (!_nextIsDisabled)
                {
                    // knob min
                    if (_draggingSliders.Contains(knobMinID))
                    {
                        knobColorMin *= 0.7f;
                    }
                    else if (isKnobMinHovered)
                    {
                        knobColorMin *= 0.9f;
                    }
                    knobColorMin.w = 1f;
                    // knob max
                    if (_draggingSliders.Contains(knobMaxID))
                    {
                        knobColorMax *= 0.7f;
                    }
                    else if (isKnobMaxHovered)
                    {
                        knobColorMax *= 0.9f;
                    }
                    knobColorMax.w = 1f;
                }

                // Min Knob ============
                // draw knob border
                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPosMin, y), ((isKnobMinHovered || isDraggingMin) && !_nextIsDisabled ? knobRadius : knobRadius * 0.8f) + 1f, ImGui.GetColorU32(ImGuiCol.Border), 32);
                // draw knob
                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPosMin, y), (isKnobMinHovered || isDraggingMin) && !_nextIsDisabled ? knobRadius : knobRadius * 0.8f, ImGui.GetColorU32(knobColorMin), 32);

                // Max Knob ============
                // draw knob border
                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPosMax, y), ((isKnobMaxHovered || isDraggingMax) && !_nextIsDisabled ? knobRadius : knobRadius * 0.8f) + 1f, ImGui.GetColorU32(ImGuiCol.Border), 32);
                // draw knob
                ImGui.GetWindowDrawList().AddCircleFilled(new Vector2(knobPosMax, y), (isKnobMaxHovered || isDraggingMax) && !_nextIsDisabled ? knobRadius : knobRadius * 0.8f, ImGui.GetColorU32(knobColorMax), 32);

                // dummy box the range
                ImGui.Dummy(new Vector2(width, height));

                // start dragging min knob
                if (isKnobMinHovered && !_draggingSliders.Contains(knobMinID) && ImGui.IsMouseClicked(0))
                {
                    _draggingSliders.Add(knobMinID);
                }
                // start dragging max knob
                if (isKnobMaxHovered && !_draggingSliders.Contains(knobMaxID) && ImGui.IsMouseClicked(0))
                {
                    _draggingSliders.Add(knobMaxID);
                }

                // stop dragging min knob
                if (_draggingSliders.Contains(knobMinID) && !ImGui.IsMouseDown(0))
                {
                    _draggingSliders.Remove(knobMinID);
                }
                // stop dragging max knob
                if (_draggingSliders.Contains(knobMaxID) && !ImGui.IsMouseDown(0))
                {
                    _draggingSliders.Remove(knobMaxID);
                }

                // If the mouse is hovering over the min knob, change the value when the mouse is clicked
                if (_draggingSliders.Contains(knobMinID) && ImGui.IsMouseDown(0) && !_nextIsDisabled)
                {
                    Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector4(8f, 4f));
                    ImGui.SetTooltip(valueMin.ToString());
                    Fugui.PopStyle();
                    // Calculate the new value based on the mouse position
                    float mouseX = ImGui.GetMousePos().x;
                    valueMin = min + (mouseX - x - knobRadius) / (width - knobRadius * 2f) * (max - min);

                    // clamp step
                    float tmp = valueMin / step;
                    tmp = Mathf.Round(tmp);
                    valueMin = tmp * step;

                    // Clamp the value to the min and max range
                    valueMin = Math.Clamp(valueMin, min, valueMax);
                    if (isInt)
                    {
                        valueMin = (int)valueMin;
                    }
                    return true;
                }

                // If the mouse is hovering over the max knob, change the value when the mouse is clicked
                if (_draggingSliders.Contains(knobMaxID) && ImGui.IsMouseDown(0) && !_nextIsDisabled)
                {
                    Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector4(8f, 4f));
                    ImGui.SetTooltip(valueMax.ToString());
                    Fugui.PopStyle();

                    // Calculate the new value based on the mouse position
                    float mouseX = ImGui.GetMousePos().x;
                    valueMax = min + (mouseX - x - knobRadius) / (width - knobRadius * 2f) * (max - min);

                    // clamp step
                    float tmp = valueMax / step;
                    tmp = Mathf.Round(tmp);
                    valueMax = tmp * step;

                    // Clamp the value to the min and max range
                    valueMax = Math.Clamp(valueMax, valueMin, max);
                    if (isInt)
                    {
                        valueMax = (int)valueMax;
                    }
                    return true;
                }

                return false;
            }
        }
    }
}