using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        /// <summary>
        /// Renders a checkbox with the given text and returns true if the checkbox was clicked. The value of the checkbox is stored in the provided boolean variable.
        /// </summary>
        /// <param name="text">Text to display next to the checkbox</param>
        /// <param name="isChecked">Boolean variable to store the value of the checkbox</param>
        /// <returns>True if the checkbox was clicked, false otherwise</returns>
        public virtual bool CheckBox(string text, ref bool isChecked)
        {
            bool clicked = false;
            beginElement(ref text, null); // Push the style for the checkbox element

            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            if (LastItemDisabled)
            {
                bool value = isChecked; // Create a temporary variable to hold the value of isChecked
                _inernalCheckbox(text, ref value); // Display a disabled checkbox with the given text label
            }
            else
            {
                clicked = _inernalCheckbox(text, ref isChecked); // Display an enabled checkbox and update the value of isChecked based on user interaction
            }

            // draw text if any
            if (!text.StartsWith("##"))
            {
                // draw text
                SameLine();
                CenterNextItemH(text, ImGui.GetFrameHeight());
                if (ClickableText(text))
                {
                    clicked = true;
                    isChecked = !isChecked;
                }
            }

            // Display a tooltip if one has been set for this element
            displayToolTip();
            // Set the flag indicating that this element should have a hover frame drawn around it
            _elementHoverFramedEnabled = true;
            // process element states
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, clicked);
            endElement(null);

            return clicked; // Return a boolean indicating whether the checkbox was clicked by the user
        }

        /// <summary>
        /// Internal method to render the checkbox and handle its state
        /// </summary>
        /// <param name="text"> The text label for the checkbox</param>
        /// <param name="isChecked"> Reference to the boolean variable that holds the checkbox state</param>
        /// <returns> True if the checkbox state was changed, false otherwise</returns>
        private unsafe bool _inernalCheckbox(string text, ref bool isChecked)
        {
            // calculate size
            float lenght = ImGui.GetFrameHeight();
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 size = new Vector2(lenght, lenght);

            // draw a dummy to update cursor and get states
            ImGui.Dummy(size);
            setBaseElementState(text, pos, size, true, false, true);

            // get current draw list
            ImDrawListPtr drawList = ImGuiNative.igGetWindowDrawList();

            // get colors
            Vector4 borderColor, bgColor, checkColor;
            if (!isChecked)
            {
                if (LastItemDisabled)
                {
                    borderColor = Fugui.Themes.GetColor(FuColors.Border) * 0.5f;
                    bgColor = Fugui.Themes.GetColor(FuColors.FrameBg) * 0.5f;
                    checkColor = Fugui.Themes.GetColor(FuColors.Knob) * 0.5f;
                }
                else if (_lastItemActive)
                {
                    borderColor = Fugui.Themes.GetColor(FuColors.Border);
                    bgColor = Fugui.Themes.GetColor(FuColors.FrameBgActive);
                    checkColor = Fugui.Themes.GetColor(FuColors.Knob);
                }
                else if (_lastItemHovered)
                {
                    borderColor = Fugui.Themes.GetColor(FuColors.Border);
                    bgColor = Fugui.Themes.GetColor(FuColors.FrameBgHovered);
                    checkColor = Fugui.Themes.GetColor(FuColors.Knob);
                }
                else
                {
                    borderColor = Fugui.Themes.GetColor(FuColors.Border);
                    bgColor = Fugui.Themes.GetColor(FuColors.FrameBg);
                    checkColor = Fugui.Themes.GetColor(FuColors.Knob);
                }
            }
            else
            {
                if (LastItemDisabled)
                {
                    borderColor = Fugui.Themes.GetColor(FuColors.Border) * 0.5f;
                    bgColor = Fugui.Themes.GetColor(FuColors.CheckMark) * 0.5f;
                    checkColor = Fugui.Themes.GetColor(FuColors.Knob) * 0.5f;
                }
                else if (_lastItemActive)
                {
                    borderColor = Fugui.Themes.GetColor(FuColors.Border);
                    bgColor = Fugui.Themes.GetColor(FuColors.CheckMark) * 0.8f;
                    checkColor = Fugui.Themes.GetColor(FuColors.Knob);
                }
                else if (_lastItemHovered)
                {
                    borderColor = Fugui.Themes.GetColor(FuColors.Border);
                    bgColor = Fugui.Themes.GetColor(FuColors.CheckMark) * 0.9f;
                    checkColor = Fugui.Themes.GetColor(FuColors.Knob);
                }
                else
                {
                    borderColor = Fugui.Themes.GetColor(FuColors.Border);
                    bgColor = Fugui.Themes.GetColor(FuColors.CheckMark);
                    checkColor = Fugui.Themes.GetColor(FuColors.Knob);
                }
            }

            // draw background
            drawList.AddRectFilled(pos, pos + size, ImGui.ColorConvertFloat4ToU32(bgColor), Fugui.Themes.FrameRounding, ImDrawFlags.None);
            // draw border
            drawList.AddRect(pos, pos + size, ImGui.ColorConvertFloat4ToU32(borderColor), Fugui.Themes.FrameRounding, ImDrawFlags.None, Fugui.Themes.FrameBorderSize);
            // draw check
            if (isChecked)
            {
                Fugui.DrawCheckMark(drawList, pos + new Vector2(Fugui.Themes.FrameBorderSize * 2, Fugui.Themes.FrameBorderSize * 2), checkColor, size.y - Fugui.Themes.FrameBorderSize * 4);
            }

            // set mouse cursor
            if (_lastItemHovered && !LastItemDisabled)
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            // display the tooltip if necessary
            displayToolTip();

            // toggle value if clicked
            if (_lastItemUpdate)
            {
                isChecked = !isChecked;
            }

            return _lastItemUpdate;
        }
    }
}