using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {

        public bool Toggle(string id, ref bool value, FuToggleFlags flags = FuToggleFlags.Default)
        {
            return _customToggle(id, ref value, null, null, flags);
        }

        public bool Toggle(string id, ref bool value, string textLeft, string textRight, FuToggleFlags flags = FuToggleFlags.Default)
        {
            return _customToggle(id, ref value, textLeft, textRight, flags);
        }

        protected virtual bool _customToggle(string id, ref bool value, string textLeft, string textRight, FuToggleFlags flags)
        {
            beginElement(id, null);

            // and and get toggle data struct
            if (!_uiElementAnimationDatas.ContainsKey(id))
            {
                _uiElementAnimationDatas.Add(id, new FuElementAnimationData(!value));
            }
            FuElementAnimationData data = _uiElementAnimationDatas[id];

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
            Vector2 startCursorPos = ImGui.GetCursorPos();

            // handle knob position animation
            Vector2 knobLeftPos = new Vector2(pos.x + 3f * Fugui.CurrentContext.Scale, center.y - radius);
            Vector2 knobRightPos = new Vector2(pos.x + size.x - (radius * 2f) - 3f * Fugui.CurrentContext.Scale, center.y - radius);
            Vector2 knobPos = Vector2.Lerp(knobLeftPos, knobRightPos, data.CurrentValue);

            // input states
            bool hovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            bool clicked = hovered && ImGui.IsMouseReleased(0);
            bool active = hovered && ImGui.IsMouseDown(0);

            Vector4 BGColor = value ? FuThemeManager.GetColor(FuColors.Selected) : FuThemeManager.GetColor(FuColors.FrameBg);
            Vector4 KnobColor = FuThemeManager.GetColor(FuColors.Knob);
            Vector4 TextColor = value ? FuThemeManager.GetColor(FuColors.SelectedText) : FuThemeManager.GetColor(FuColors.Text);

            if (_nextIsDisabled)
            {
                BGColor *= 0.5f;
                KnobColor *= 0.5f;
            }
            else if (active)
            {
                KnobColor = FuThemeManager.GetColor(FuColors.KnobActive);
                BGColor = value ? FuThemeManager.GetColor(FuColors.SelectedActive) : FuThemeManager.GetColor(FuColors.FrameBgActive);
            }
            else if (hovered)
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
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + radius * 2f + 12f * Fugui.CurrentContext.Scale);
                }
                else
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8f * Fugui.CurrentContext.Scale);
                }
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2f * Fugui.CurrentContext.Scale);
                Fugui.Push(ImGuiCol.Text, TextColor);
                ImGui.Text(currentText);
                Fugui.PopColor();
            }

            // draw dummy to match ImGui layout
            ImGui.SetCursorPos(startCursorPos);
            ImGui.Dummy(size);

            // handle hover and click
            if (clicked && !_nextIsDisabled)
            {
                value = !value;
                valueChanged = true;
            }

            data.Update(value, _animationEnabled);

            endElement(null);
            return valueChanged;
        }
    }
}