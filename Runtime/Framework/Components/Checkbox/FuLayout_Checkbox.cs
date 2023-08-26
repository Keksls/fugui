using Fu.Core;
using ImGuiNET;

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
            text = "##" + text;
            beginElement(ref text, null); // Push the style for the checkbox element

            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            // push colors
            if (LastItemDisabled)
            {
                if (isChecked)
                {
                    Fugui.Push(ImGuiCols.CheckMark, FuThemeManager.GetColor(FuColors.Knob) * 0.5f);
                    Fugui.Push(ImGuiCols.FrameBg, FuThemeManager.GetColor(FuColors.CheckMark) * 0.5f);
                    Fugui.Push(ImGuiCols.FrameBgHovered, FuThemeManager.GetColor(FuColors.CheckMark) * 0.5f);
                    Fugui.Push(ImGuiCols.FrameBgActive, FuThemeManager.GetColor(FuColors.CheckMark) * 0.5f);
                }
                else
                {
                    Fugui.Push(ImGuiCols.CheckMark, FuThemeManager.GetColor(FuColors.Knob) * 0.5f);
                    Fugui.Push(ImGuiCols.FrameBg, FuThemeManager.GetColor(FuColors.FrameBg) * 0.5f);
                    Fugui.Push(ImGuiCols.FrameBgHovered, FuThemeManager.GetColor(FuColors.FrameBg) * 0.5f);
                    Fugui.Push(ImGuiCols.FrameBgActive, FuThemeManager.GetColor(FuColors.FrameBg) * 0.5f);
                }
            }
            else
            {
                if (isChecked)
                {
                    Fugui.Push(ImGuiCols.CheckMark, FuThemeManager.GetColor(FuColors.Knob));
                    Fugui.Push(ImGuiCols.FrameBg, FuThemeManager.GetColor(FuColors.CheckMark));
                    Fugui.Push(ImGuiCols.FrameBgHovered, FuThemeManager.GetColor(FuColors.CheckMark) * 0.9f);
                    Fugui.Push(ImGuiCols.FrameBgActive, FuThemeManager.GetColor(FuColors.CheckMark) * 0.8f);
                }
                else
                {
                    Fugui.Push(ImGuiCols.CheckMark, FuThemeManager.GetColor(FuColors.Knob));
                    Fugui.Push(ImGuiCols.FrameBg, FuThemeManager.GetColor(FuColors.FrameBg));
                    Fugui.Push(ImGuiCols.FrameBgHovered, FuThemeManager.GetColor(FuColors.FrameBgHovered));
                    Fugui.Push(ImGuiCols.FrameBgActive, FuThemeManager.GetColor(FuColors.FrameBgActive));
                }
            }
            // reduce border strenght
            Fugui.Push(ImGuiCols.Border, FuThemeManager.GetColor(FuColors.Border) * 0.5f);
            ImGui.PushID(text);
            if (LastItemDisabled)
            {
                bool value = isChecked; // Create a temporary variable to hold the value of isChecked
                ImGui.Checkbox("", ref value); // Display a disabled checkbox with the given text label
            }
            else
            {
                clicked = ImGui.Checkbox("", ref isChecked); // Display an enabled checkbox and update the value of isChecked based on user interaction
            }
            // Pop the style for the checkbox element
            Fugui.PopColor(5);
            // Display a tooltip if one has been set for this element
            displayToolTip();
            // Set the flag indicating that this element should have a hover frame drawn around it
            _elementHoverFramedEnabled = true;
            // process element states
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, clicked);
            endElement(null);
            ImGui.PopID();

            return clicked; // Return a boolean indicating whether the checkbox was clicked by the user
        }
    }
}