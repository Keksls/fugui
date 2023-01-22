using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {

        public bool Toggle(string id, ref bool value, ToggleFlags flags = ToggleFlags.Default)
        {
            return _customToggle(id, ref value, null, null, flags);
        }

        public bool Toggle(string id, ref bool value, string textLeft, string textRight, ToggleFlags flags = ToggleFlags.Default)
        {
            return _customToggle(id, ref value, textLeft, textRight, flags);
        }

        protected virtual bool _customToggle(string id, ref bool value, string textLeft, string textRight, ToggleFlags flags)
        {
            beginElement(id, null);

            // and and get toggle data struct
            if (!_uiElementAnimationDatas.ContainsKey(id))
            {
                _uiElementAnimationDatas.Add(id, new UIElementAnimationData(!value));
            }
            UIElementAnimationData data = _uiElementAnimationDatas[id];

            // process Text Size
            string currentText = value ? textRight : textLeft;
            Vector2 textSize = Vector2.zero;
            if (!string.IsNullOrEmpty(currentText))
            {
                if (flags.HasFlag(ToggleFlags.MaximumTextSize))
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
            float height = string.IsNullOrEmpty(currentText) ? 16f : textSize.y + 4f;
            bool valueChanged = false;
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 size = new Vector2(string.IsNullOrEmpty(currentText) ? height * 2f : height * 2f + textSize.x, height);
            if (!flags.HasFlag(ToggleFlags.AlignLeft))
            {
                ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().x - size.x - 4f, 0f));
                ImGui.SameLine();
            }
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 center = pos + new Vector2(size.x / 2, size.y / 2);
            float radius = size.y / 2f - 3f;
            Vector2 startCursorPos = ImGui.GetCursorPos();

            // handle knob position animation
            Vector2 knobLeftPos = new Vector2(pos.x + 3f, center.y - radius);
            Vector2 knobRightPos = new Vector2(pos.x + size.x - (radius * 2f) - 3f, center.y - radius);
            Vector2 knobPos = Vector2.Lerp(knobLeftPos, knobRightPos, data.CurrentValue);

            // input states
            bool hovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            bool clicked = hovered && ImGui.IsMouseReleased(0);
            bool active = hovered && ImGui.IsMouseDown(0);

            Vector4 BGColor = value ? ThemeManager.GetColor(FuguiColors.Selected) : ThemeManager.GetColor(FuguiColors.FrameBg);
            Vector4 KnobColor = ThemeManager.GetColor(FuguiColors.Knob);
            Vector4 TextColor = value ? ThemeManager.GetColor(FuguiColors.SelectedText) : ThemeManager.GetColor(FuguiColors.Text);

            if (_nextIsDisabled)
            {
                BGColor *= 0.5f;
                KnobColor *= 0.5f;
            }
            else if (active)
            {
                KnobColor = ThemeManager.GetColor(FuguiColors.KnobActive);
                BGColor = value ? ThemeManager.GetColor(FuguiColors.SelectedActive) : ThemeManager.GetColor(FuguiColors.FrameBgActive);
            }
            else if (hovered)
            {
                KnobColor = ThemeManager.GetColor(FuguiColors.KnobHovered);
                BGColor = value ? ThemeManager.GetColor(FuguiColors.SelectedHovered) : ThemeManager.GetColor(FuguiColors.FrameBgHovered);
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
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + radius * 2f + 12f);
                }
                else
                {
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 8f);
                }
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 2f);
                FuGui.Push(ImGuiCol.Text, TextColor);
                ImGui.Text(currentText);
                FuGui.PopColor();
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