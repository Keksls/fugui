using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
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
            Vector2 localPos = ImGui.GetCursorPos();
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
            setBaseElementState(text, pos, size, true, false, true);

            // handle click
            if (LastItemUpdate)
            {
                value = !value;
                valueChanged = true;
            }

            // set mouse cursor
            if (LastItemHovered && !_nextIsDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            Vector4 BGColor = value ? FuThemeManager.GetColor(FuColors.Selected) : FuThemeManager.GetColor(FuColors.FrameBg);
            Vector4 KnobColor = FuThemeManager.GetColor(FuColors.Knob);
            Vector4 TextColor = value ? FuThemeManager.GetColor(FuColors.SelectedText) : FuThemeManager.GetColor(FuColors.Text);

            if (_nextIsDisabled)
            {
                BGColor *= 0.5f;
                KnobColor *= 0.5f;
            }
            else if (LastItemActive)
            {
                KnobColor = FuThemeManager.GetColor(FuColors.KnobActive);
                BGColor = value ? FuThemeManager.GetColor(FuColors.SelectedActive) : FuThemeManager.GetColor(FuColors.FrameBgActive);
            }
            else if (LastItemHovered)
            {
                KnobColor = FuThemeManager.GetColor(FuColors.KnobHovered);
                BGColor = value ? FuThemeManager.GetColor(FuColors.SelectedHovered) : FuThemeManager.GetColor(FuColors.FrameBgHovered);
            }
            Vector4 BorderColor = BGColor * 0.66f;

            // draw background
            drawList.AddRectFilled(pos, pos + size, ImGui.GetColorU32(BGColor), 99);
            // draw border
            drawList.AddRect(pos, pos + size, ImGui.GetColorU32(BorderColor), 99);
            // draw knob
            drawList.AddCircleFilled(knobPos + new Vector2(radius, radius), radius, ImGui.GetColorU32(KnobColor), 32);

            // draw text
            if (!string.IsNullOrEmpty(currentText))
            {
                if (!value)
                {
                    ImGui.SetCursorPosX(localPos.x + radius * 2f + 12f * Fugui.CurrentContext.Scale);
                }
                else
                {
                    ImGui.SetCursorPosX(localPos.x + 8f * Fugui.CurrentContext.Scale);
                }
                ImGui.SetCursorPosY(localPos.y + 2f * Fugui.CurrentContext.Scale);
                Fugui.Push(ImGuiCol.Text, TextColor);
                ImGui.Text(currentText);
                Fugui.PopColor();
            }

            data.Update(value, _animationEnabled);
            displayToolTip(LastItemHovered);
            endElement(null);
            return valueChanged;
        }
    }
}