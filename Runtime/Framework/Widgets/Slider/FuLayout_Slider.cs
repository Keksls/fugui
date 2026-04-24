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
        /// <param name="value">The current value of the slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>True if the value was changed by the user, false otherwise.</returns>
        public bool Slider(string text, ref int value, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            return Slider(text, ref value, 0, 100, new FuElementSize(-1f, 4f), flags, format);
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
            bool valueChange = _customSlider(text, ref val, min, max, true, 1f, new FuElementSize(-1f, 4f), flags, format);
            value = (int)val;
            return valueChange;
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
        public bool Slider(string text, ref int value, int min, int max, FuElementSize sliderSize, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            float val = value;
            bool valueChange = _customSlider(text, ref val, min, max, true, 1f, sliderSize, flags, format);
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
            return Slider(text, ref value, 0f, 100f, new FuElementSize(-1f, 4f), step, flags, format);
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
            return _customSlider(text, ref value, min, max, false, step, new FuElementSize(-1f, 4f), flags, format);
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
        public bool Slider(string text, ref float value, float min, float max, FuElementSize sliderSize, float step = 0.01f, FuSliderFlags flags = FuSliderFlags.Default, string format = null)
        {
            return _customSlider(text, ref value, min, max, false, step, sliderSize, flags, format);
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
        protected virtual bool _customSlider(string text, ref float value, float min, float max, bool isInt, float step, FuElementSize sliderSize, FuSliderFlags flags, string format)
        {
            beginElement(ref text, FuFrameStyle.Default);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            // Calculate the position and size of the slider
            Vector2 cursorPos = ImGui.GetCursorScreenPos();
            float knobRadius = 5f * Fugui.Scale * (Application.isMobilePlatform ? 1.8f : 1f);
            float hoverPaddingY = 0f;
            float height = ImGui.CalcTextSize("Ap").y + (ImGui.GetStyle().FramePadding.y * 2f);

            Vector2 size = sliderSize.GetSize();
            if(size.y > height)
                height = size.y;

            if(height < 14f * Fugui.Scale)
                hoverPaddingY = (14f * Fugui.Scale - height) / 2f;

            float lineHeight = size.y * (Application.isMobilePlatform ? 1.8f : 1f);
            float dragWidth = 52f * Fugui.Scale;
            float width = size.x;
            if (!flags.HasFlag(FuSliderFlags.NoDrag))
            {
                width -= dragWidth + (8f * Fugui.Scale);
            }

            float x = cursorPos.x;
            float y = cursorPos.y + height / 2f;
            float oldValue = value;

            bool leftDrag = flags.HasFlag(FuSliderFlags.LeftDrag);
            bool noDrag = flags.HasFlag(FuSliderFlags.NoDrag);

            if (noDrag)
            {
                drawSlider(text, ref value, min, max, isInt, knobRadius, hoverPaddingY, lineHeight, width, x, y, flags);
            }
            else if (leftDrag)
            {
                drawDrag(text, ref value, min, max, isInt);
                _elementHoverFramedEnabled = true;
                DrawHoverFrame();
                x += dragWidth + (8f * Fugui.Scale);
                ImGui.SameLine();
                drawSlider(text, ref value, min, max, isInt, knobRadius, hoverPaddingY, lineHeight, width, x, y, flags);
                _elementHoverFramedEnabled = false;
            }
            else
            {
                if (drawSlider(text, ref value, min, max, isInt, knobRadius, hoverPaddingY, lineHeight, width, x, y, flags))
                {
                    ImGui.SameLine();
                }

                drawDrag(text, ref value, min, max, isInt);
            }

            // clamp step
            float tmp = value / step;
            tmp = Mathf.Round(tmp);
            value = tmp * step;

            // set states for this element
            setBaseElementState(text, _currentItemStartPos, new Vector2(width, height), true, value != oldValue);

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
            bool drawSlider(string text, ref float value, float min, float max, bool isInt, float knobRadius, float hoverPaddingY, float lineHeight, float width, float x, float y, FuSliderFlags flags)
            {
                ImGui.InvisibleButton(text, new Vector2(width + (4f * Fugui.Scale), height), ImGuiButtonFlags.None);

                if (width < 24f * Fugui.Scale)
                {
                    return false;
                }

                float range = max - min;
                if (Mathf.Abs(range) <= float.Epsilon)
                {
                    return true;
                }

                bool noKnobs = flags.HasFlag(FuSliderFlags.NoKnobs);
                bool updateOnBarClick = flags.HasFlag(FuSliderFlags.UpdateOnBarClick) || noKnobs;

                float normalizedValue = Mathf.Clamp01((value - min) / range);
                float knobPos = x + width * normalizedValue;

                float knobHitRadius = knobRadius;
                float lineHoverPaddingY = hoverPaddingY;

                bool isLineHovered = IsItemHovered(
                    new Vector2(x, y - lineHoverPaddingY - lineHeight),
                    new Vector2(width, lineHoverPaddingY * 2f + lineHeight * 2f)
                );

                bool isKnobHovered = !noKnobs && IsItemHovered(
                    new Vector2(knobPos - knobHitRadius, y - knobHitRadius),
                    new Vector2(knobHitRadius * 2f, knobHitRadius * 2f)
                );

                bool isDragging = _draggingSliders.Contains(text);

                Vector4 leftLineColor = Fugui.Themes.GetColor(FuColors.CheckMark);
                Vector4 rightLineColor = Fugui.Themes.GetColor(FuColors.FrameBg);
                Vector4 knobColor = Fugui.Themes.GetColor(FuColors.Knob);

                if (LastItemDisabled)
                {
                    leftLineColor *= 0.5f;
                    rightLineColor *= 0.5f;
                    knobColor *= 0.5f;
                }
                else if (isDragging)
                {
                    knobColor = Fugui.Themes.GetColor(FuColors.KnobActive);
                }
                else if (isKnobHovered)
                {
                    knobColor = Fugui.Themes.GetColor(FuColors.KnobHovered);
                }

                ImGui.GetWindowDrawList().AddLine(new Vector2(x, y), new Vector2(knobPos, y), ImGui.GetColorU32(leftLineColor), lineHeight);
                ImGui.GetWindowDrawList().AddLine(new Vector2(knobPos, y), new Vector2(x + width, y), ImGui.GetColorU32(rightLineColor), lineHeight);

                if ((isKnobHovered || isLineHovered) && !LastItemDisabled)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (!noKnobs)
                {
                    if (!LastItemDisabled)
                    {
                        if (isDragging)
                        {
                            knobColor *= 0.7f;
                        }
                        else if (isKnobHovered)
                        {
                            knobColor *= 0.9f;
                        }

                        knobColor.w = 1f;
                    }

                    float visualKnobRadius = (isKnobHovered || isDragging) && !LastItemDisabled ? knobRadius : knobRadius * 0.8f;

                    ImGui.GetWindowDrawList().AddCircleFilled(
                        new Vector2(knobPos, y),
                        visualKnobRadius + 1f,
                        ImGui.GetColorU32(ImGuiCol.Border),
                        32
                    );

                    ImGui.GetWindowDrawList().AddCircleFilled(
                        new Vector2(knobPos, y),
                        visualKnobRadius,
                        ImGui.GetColorU32(knobColor),
                        32
                    );
                }

                if (!LastItemDisabled)
                {
                    bool shouldStartDrag = false;

                    if (!noKnobs && isKnobHovered && ImGui.IsMouseClicked(0))
                    {
                        shouldStartDrag = true;
                    }

                    if (updateOnBarClick && isLineHovered && ImGui.IsMouseClicked(0))
                    {
                        shouldStartDrag = true;
                    }

                    if (shouldStartDrag && !_draggingSliders.Contains(text))
                    {
                        _draggingSliders.Add(text);
                    }
                }

                if (_draggingSliders.Contains(text) && !ImGui.IsMouseDown(0))
                {
                    _draggingSliders.Remove(text);
                }

                if (_draggingSliders.Contains(text) && ImGui.IsMouseDown(0) && !LastItemDisabled)
                {
                    float mouseX = ImGui.GetMousePos().x;
                    value = min + ((mouseX - x) / width) * range;
                    value = Mathf.Clamp(value, min, max);

                    if (isInt)
                    {
                        value = (int)value;
                    }
                }

                return true;
            }
        }
    }
}