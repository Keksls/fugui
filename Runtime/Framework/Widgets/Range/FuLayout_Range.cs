using ImGuiNET;
using System;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region Methods
        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="valueMin">The minimum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="valueMax">The maximum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Range(string text, ref int valueMin, ref int valueMax, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            return Range(text, ref valueMin, ref valueMax, 0, 100, flags, format);
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
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Range(string text, ref int valueMin, ref int valueMax, int min, int max, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            float valMin = valueMin;
            float valMax = valueMax;
            bool valueChange = _customRange(text, ref valMin, ref valMax, min, max, true, 1f, flags, format);
            valueMin = (int)valMin;
            valueMax = (int)valMax;
            return valueChange;
        }

        /// <summary>
        /// Creates a horizontal slider widget that allows the user to choose an integer value from a range.
        /// </summary>
        /// <param name="text">The label for the slider.</param>
        /// <param name="valueMin">The minimum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="valueMax">The maximum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="step">step of the slider value change</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Range(string text, ref float valueMin, ref float valueMax, float step = 0.01f, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            return _customRange(text, ref valueMin, ref valueMax, 0f, 100f, false, step, flags, format);
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
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Range(string text, ref float valueMin, ref float valueMax, float min, float max, float step = 0.01f, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            return _customRange(text, ref valueMin, ref valueMax, min, max, false, step, flags, format);
        }

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
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>true if value changed</returns>
        protected virtual bool _customRange(string text, ref float valueMin, ref float valueMax, float min, float max, bool isInt, float step, FuSliderFlags flags, string format)
        {
            beginElement(ref text, FuFrameStyle.Default);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            // Calculate the position and size of the slider
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            float knobRadius = Application.isMobilePlatform ? 8f * Fugui.Scale : 5f * Fugui.Scale;
            float hoverPaddingY = Application.isMobilePlatform ? 8f * Fugui.Scale : 5f * Fugui.Scale;
            float height = ImGui.CalcTextSize("Ap").y + (ImGui.GetStyle().FramePadding.y * 2f);
            float lineHeight = Application.isMobilePlatform ? 3f * Fugui.Scale : 2f * Fugui.Scale;
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
            ImGui.InvisibleButton(text, new Vector2(width, maxY - cursorPos.y), ImGuiButtonFlags.None);

            // do not draw hover frame
            _elementHoverFramedEnabled = false;
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, updated);
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
                string formatString = format != null ? format : getStringFormat(value);
                if (ImGui.DragFloat(id, ref value, step, min, max, isInt ? "%.0f" : formatString, LastItemDisabled ? ImGuiSliderFlags.NoInput : ImGuiSliderFlags.AlwaysClamp))
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
                displayToolTip();
                _elementHoverFramedEnabled = true;
                DrawHoverFrame();
                _elementHoverFramedEnabled = false;
                return updated;
            }

            // function that draw the slider
            bool drawSlider(string id, ref float valueMin, ref float valueMax, float min, float max, bool isInt, float knobRadius, float hoverPaddingY, float lineHeight, float width, float x, float y)
            {
                // get knobs ids
                string knobMinID = id + "KnobMin";
                string knobMaxID = id + "KnobMax";
                // Calculate the position of the knob
                float range = max - min;
                if (Mathf.Abs(range) <= float.Epsilon)
                {
                    return false;
                }

                float valueStartX = x + knobRadius;
                float valueWidth = Mathf.Max(1f, width - knobRadius * 2f);
                float knobPosMin = valueStartX + valueWidth * Mathf.Clamp01((valueMin - min) / range);
                float knobPosMax = valueStartX + valueWidth * Mathf.Clamp01((valueMax - min) / range);
                // Check if the mouse is hovering over the knob
                bool rawKnobMinHovered = IsItemHovered(new Vector2(knobPosMin - knobRadius, y - knobRadius), new Vector2(knobRadius * 2f, knobRadius * 2f));
                bool rawKnobMaxHovered = IsItemHovered(new Vector2(knobPosMax - knobRadius, y - knobRadius), new Vector2(knobRadius * 2f, knobRadius * 2f));
                bool rawLineHovered = IsItemHovered(new Vector2(x, y - hoverPaddingY - lineHeight), new Vector2(width, hoverPaddingY * 2f + lineHeight * 2f));
                // Check if slider is dragging
                bool isDraggingMin = _draggingSliders.Contains(knobMinID);
                bool isDraggingMax = _draggingSliders.Contains(knobMaxID);
                bool isDragging = isDraggingMin || isDraggingMax;
                bool suppressHoverFeedback = ImGui.IsAnyItemActive() || IsAnyItemActive || IsThereAnyDraggingSlider;
                bool isKnobMinHovered = rawKnobMinHovered && !suppressHoverFeedback;
                bool isKnobMaxHovered = rawKnobMaxHovered && !suppressHoverFeedback;
                bool isLineHovered = rawLineHovered && !suppressHoverFeedback;

                // Calculate colors
                Vector4 insideLineColor = Fugui.Themes.GetColor(FuColors.CheckMark);
                Vector4 outsideLineColor = Fugui.Themes.GetColor(FuColors.FrameBg);
                Vector4 knobColorMin = Fugui.Themes.GetColor(FuColors.Knob);
                Vector4 knobColorMax = Fugui.Themes.GetColor(FuColors.Knob);
                if (LastItemDisabled)
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
                        knobColorMin = Fugui.Themes.GetColor(FuColors.KnobActive);
                        insideLineColor = Fugui.Themes.GetColor(FuColors.HighlightActive);
                    }
                    else if (isKnobMinHovered)
                    {
                        knobColorMin = Fugui.Themes.GetColor(FuColors.KnobHovered);
                        insideLineColor = Fugui.Themes.GetColor(FuColors.HighlightHovered);
                    }
                    // max knob
                    if (isDraggingMax)
                    {
                        knobColorMax = Fugui.Themes.GetColor(FuColors.KnobActive);
                        insideLineColor = Fugui.Themes.GetColor(FuColors.HighlightActive);
                    }
                    else if (isKnobMaxHovered)
                    {
                        knobColorMax = Fugui.Themes.GetColor(FuColors.KnobHovered);
                        insideLineColor = Fugui.Themes.GetColor(FuColors.HighlightHovered);
                    }
                }

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                ImDrawListPtr foregroundDrawList = ImGui.GetForegroundDrawList();
                float trackRounding = Fugui.Themes.FrameRounding;
                Vector2 trackMin = new Vector2(valueStartX, y - lineHeight * 0.5f);
                Vector2 trackMax = new Vector2(valueStartX + valueWidth, y + lineHeight * 0.5f);
                DrawRoundedSegment(drawList, trackMin, trackMax, outsideLineColor, trackRounding);
                DrawRoundedSegment(drawList, new Vector2(knobPosMin, trackMin.y), new Vector2(knobPosMax, trackMax.y), insideLineColor, trackRounding);
                if (isLineHovered && !LastItemDisabled)
                {
                    Vector4 hoverLine = Fugui.Themes.GetColor(FuColors.FrameHoverFeedback);
                    hoverLine.w = Mathf.Max(hoverLine.w, 0.35f);
                    drawList.AddRect(trackMin, trackMax, ImGui.GetColorU32(hoverLine), trackRounding, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, Fugui.CurrentContext.Scale));
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                // Draw the knobs
                if (!LastItemDisabled)
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
                float visualKnobRadiusMin = (isKnobMinHovered || isDraggingMin) && !LastItemDisabled ? knobRadius : knobRadius * 0.82f;
                if (isDraggingMin)
                {
                    DrawValueKnob(foregroundDrawList, new Vector2(knobPosMin, y), visualKnobRadiusMin, knobColorMin, isKnobMinHovered, true, LastItemDisabled);
                }
                else
                {
                    DrawValueKnobWithWindowClip(drawList, new Vector2(knobPosMin, y), visualKnobRadiusMin, knobColorMin, isKnobMinHovered, false, LastItemDisabled);
                }

                // Max Knob ============
                float visualKnobRadiusMax = (isKnobMaxHovered || isDraggingMax) && !LastItemDisabled ? knobRadius : knobRadius * 0.82f;
                if (isDraggingMax)
                {
                    DrawValueKnob(foregroundDrawList, new Vector2(knobPosMax, y), visualKnobRadiusMax, knobColorMax, isKnobMaxHovered, true, LastItemDisabled);
                }
                else
                {
                    DrawValueKnobWithWindowClip(drawList, new Vector2(knobPosMax, y), visualKnobRadiusMax, knobColorMax, isKnobMaxHovered, false, LastItemDisabled);
                }

                if (isDraggingMin && !LastItemDisabled)
                {
                    DrawValueBubble(foregroundDrawList, FormatValueBubble(valueMin, isInt, format), new Vector2(knobPosMin, y - knobRadius));
                }
                if (isDraggingMax && !LastItemDisabled)
                {
                    DrawValueBubble(foregroundDrawList, FormatValueBubble(valueMax, isInt, format), new Vector2(knobPosMax, y - knobRadius));
                }

                // dummy box the range
                ImGui.Dummy(new Vector2(width, height));

                // start dragging min knob
                if (rawKnobMinHovered && !_draggingSliders.Contains(knobMinID) && ImGui.IsMouseClicked(0))
                {
                    _draggingSliders.Add(knobMinID);
                }
                // start dragging max knob
                if (rawKnobMaxHovered && !_draggingSliders.Contains(knobMaxID) && ImGui.IsMouseClicked(0))
                {
                    _draggingSliders.Add(knobMaxID);
                }

                // set mouse cursor
                if ((isKnobMinHovered || isKnobMaxHovered) && !LastItemDisabled)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
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
                if (_draggingSliders.Contains(knobMinID) && ImGui.IsMouseDown(0) && !LastItemDisabled)
                {
                    // Calculate the new value based on the mouse position
                    float mouseX = ImGui.GetMousePos().x;
                    valueMin = min + (mouseX - valueStartX) / valueWidth * range;

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
                if (_draggingSliders.Contains(knobMaxID) && ImGui.IsMouseDown(0) && !LastItemDisabled)
                {
                    // Calculate the new value based on the mouse position
                    float mouseX = ImGui.GetMousePos().x;
                    valueMax = min + (mouseX - valueStartX) / valueWidth * range;

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
        #endregion
    }
}
