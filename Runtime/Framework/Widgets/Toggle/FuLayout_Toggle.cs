using ImGuiNET;
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
        /// Draw a Toggle with the specified parameters
        /// </summary>
        /// <param name="text">Label of the toggle</param>
        /// <param name="value">reference of the toggle value</param>
        /// <param name="flags">behaviour flags of the toggle</param>
        /// <returns>true if value changed</returns>
        public bool Toggle(string text, ref bool value, FuToggleFlags flags = FuToggleFlags.Default)
        {
            return _customToggle(text, ref value, null, null, flags);
        }

        /// <summary>
        /// Draw a Toggle with the specified parameters
        /// </summary>
        /// <param name="text">Label of the toggle</param>
        /// <param name="value">reference of the toggle value</param>
        /// <param name="textLeft">text when toggle is on left (deactivated)</param>
        /// <param name="textRight">text when toggle is on right (activated)</param>
        /// <param name="flags">behaviour flags of the toggle</param>
        /// <returns>true if value changed</returns>
        public bool Toggle(string text, ref bool value, string textLeft, string textRight, FuToggleFlags flags = FuToggleFlags.Default)
        {
            return _customToggle(text, ref value, textLeft, textRight, flags);
        }

        /// <summary>
        /// Draw a Toggle with the specified parameters
        /// </summary>
        /// <param name="text">Label of the toggle</param>
        /// <param name="value">reference of the toggle value</param>
        /// <param name="textLeft">text when toggle is on left (deactivated)</param>
        /// <param name="textRight">text when toggle is on right (activated)</param>
        /// <param name="flags">behaviour flags of the toggle</param>
        /// <returns>true if value changed</returns>
        protected virtual bool _customToggle(string text, ref bool value, string textLeft, string textRight, FuToggleFlags flags)
        {
            beginElement(ref text, null, true);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            // and and get toggle data struct
            if (!_uiElementAnimationDatas.ContainsKey(text))
            {
                _uiElementAnimationDatas.Add(text, new FuElementAnimationData(!value));
            }
            FuElementAnimationData data = _uiElementAnimationDatas[text];
            bool noEditable = flags.HasFlag(FuToggleFlags.NoEditable);

            // process Text Size
            string currentText = value ? textRight : textLeft;
            Vector2 textSize = Vector2.zero;
            if (!string.IsNullOrEmpty(currentText))
            {
                if (flags.HasFlag(FuToggleFlags.MaximumTextSize))
                {
                    Vector2 leftTextSize = ImGui.CalcTextSize(textLeft);
                    Vector2 rightTextSize = ImGui.CalcTextSize(textRight);
                    if (leftTextSize.x > rightTextSize.x)
                    {
                        textSize = leftTextSize;
                    }
                    else
                    {
                        textSize = rightTextSize;
                    }
                }
                else
                {
                    textSize = ImGui.CalcTextSize(currentText);
                }
            }

            // draw states
            float height = string.IsNullOrEmpty(currentText) ? 16f * Fugui.CurrentContext.Scale : textSize.y + 4f * Fugui.CurrentContext.Scale;
            bool valueChanged = false;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 size = new Vector2(string.IsNullOrEmpty(currentText) ? height * 2f : height * 2f + textSize.x, height);
            if (!flags.HasFlag(FuToggleFlags.AlignLeft))
            {
                ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().x - size.x - 4f * Fugui.CurrentContext.Scale, 0f));
                ImGui.SameLine();
            }
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 center = pos + new Vector2(size.x / 2, size.y / 2);
            float radius = size.y / 2f - 3f * Fugui.CurrentContext.Scale;

            // handle knob position animation
            Vector2 knobLeftPos = new Vector2(pos.x + 3f * Fugui.CurrentContext.Scale, center.y - radius);
            Vector2 knobRightPos = new Vector2(pos.x + size.x - (radius * 2f) - 3f * Fugui.CurrentContext.Scale, center.y - radius);
            Vector2 knobPos = Vector2.Lerp(knobLeftPos, knobRightPos, data.CurrentValue);

            // draw dummy to match ImGui layout
            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(size);

            // set states for this element
            setBaseElementState(text + currentText, pos, size, !noEditable, false, true);

            // handle click
            if (_lastItemUpdate)
            {
                value = !value;
                valueChanged = true;
            }

            // set mouse cursor
            if (_lastItemHovered && !LastItemDisabled && !noEditable)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            bool colValue = flags.HasFlag(FuToggleFlags.SwitchState) ? false : value;
            Vector4 BGColor = colValue ? Fugui.Themes.GetColor(FuColors.Selected) : ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
            Vector4 KnobColor = Fugui.Themes.GetColor(FuColors.Knob);

            if (LastItemDisabled)
            {
                BGColor *= 0.5f;
                KnobColor *= 0.5f;
            }
            else if (_lastItemActive && !noEditable)
            {
                KnobColor = Fugui.Themes.GetColor(FuColors.KnobActive);
                BGColor = colValue ? Fugui.Themes.GetColor(FuColors.SelectedActive) : ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive];
            }
            else if (_lastItemHovered && !noEditable)
            {
                KnobColor = Fugui.Themes.GetColor(FuColors.KnobHovered);
                BGColor = colValue ? Fugui.Themes.GetColor(FuColors.SelectedHovered) : ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered];
            }
            Vector4 BorderColor = BGColor * 0.66f;

            if (!value && !flags.HasFlag(FuToggleFlags.SwitchState))
            {
                BorderColor = Fugui.Themes.GetColor(FuColors.Text, LastItemDisabled ? 0.2f : 0.4f);
            }

            // draw background
            float rounding = size.y * 0.5f;
            DrawRoundedSegment(drawList, pos, pos + size, BGColor, rounding, true);
            // draw border
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(BorderColor), rounding, ImDrawFlags.RoundCornersAll, Mathf.Max(1f, Fugui.Themes.FrameBorderSize));
            // draw knob
            DrawValueKnob(drawList, knobPos + new Vector2(radius, radius), radius, KnobColor, _lastItemHovered, _lastItemActive, LastItemDisabled || noEditable);
            DrawWidgetFeedback(drawList, new Rect(pos, size), _lastItemActive, _lastItemHovered, LastItemDisabled || noEditable, rounding);

            // draw text
            if (!string.IsNullOrEmpty(currentText))
            {
                Vector4 textColor = colValue
                    ? Fugui.Themes.GetColor(FuColors.SelectedText)
                    : Fugui.Themes.GetColor(FuColors.Text);
                if (LastItemDisabled)
                {
                    textColor.w *= 0.5f;
                }
                float textX = value
                    ? pos.x + 8f * Fugui.CurrentContext.Scale
                    : pos.x + radius * 2f + 12f * Fugui.CurrentContext.Scale;
                Vector2 textPos = new Vector2(textX, pos.y + (size.y - textSize.y) * 0.5f);
                drawList.AddText(textPos, ImGui.GetColorU32(textColor), currentText);
            }

            data.Update(value, _animationEnabled);
            displayToolTip(_lastItemHovered);
            endElement(null);
            return valueChanged;
        }
        #endregion
    }
}
