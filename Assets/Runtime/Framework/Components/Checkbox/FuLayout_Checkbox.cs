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
        /// <param name="style">Style to use for the checkbox</param>
        /// <returns>True if the checkbox was clicked, false otherwise</returns>
        public virtual bool CheckBox(string text, ref bool isChecked)
        {
            bool clicked = false;
            beginElement(ref text, null); // Push the style for the checkbox element

            // return if item must no be draw
            if (!_drawItem)
            {
                return false;
            }

            // push colors
            if (_nextIsDisabled)
            {
                if (isChecked)
                {
                    Fugui.Push(ImGuiCol.CheckMark, FuThemeManager.GetColor(FuColors.Knob) * 0.3f);
                    Fugui.Push(ImGuiCol.FrameBg, FuThemeManager.GetColor(FuColors.CheckMark) * 0.3f);
                    Fugui.Push(ImGuiCol.FrameBgHovered, FuThemeManager.GetColor(FuColors.CheckMark) * 0.3f);
                    Fugui.Push(ImGuiCol.FrameBgActive, FuThemeManager.GetColor(FuColors.CheckMark) * 0.3f);
                }
                else
                {
                    Fugui.Push(ImGuiCol.CheckMark, FuThemeManager.GetColor(FuColors.Knob) * 0.3f);
                    Fugui.Push(ImGuiCol.FrameBg, FuThemeManager.GetColor(FuColors.FrameBg) * 0.3f);
                    Fugui.Push(ImGuiCol.FrameBgHovered, FuThemeManager.GetColor(FuColors.FrameBgHovered) * 0.3f);
                    Fugui.Push(ImGuiCol.FrameBgActive, FuThemeManager.GetColor(FuColors.FrameBgActive) * 0.3f);
                }
            }
            else
            {
                if (isChecked)
                {
                    Fugui.Push(ImGuiCol.CheckMark, FuThemeManager.GetColor(FuColors.Knob));
                    Fugui.Push(ImGuiCol.FrameBg, FuThemeManager.GetColor(FuColors.CheckMark));
                    Fugui.Push(ImGuiCol.FrameBgHovered, FuThemeManager.GetColor(FuColors.CheckMark) * 0.9f);
                    Fugui.Push(ImGuiCol.FrameBgActive, FuThemeManager.GetColor(FuColors.CheckMark) * 0.8f);
                }
                else
                {
                    Fugui.Push(ImGuiCol.CheckMark, FuThemeManager.GetColor(FuColors.Knob));
                    Fugui.Push(ImGuiCol.FrameBg, FuThemeManager.GetColor(FuColors.FrameBg));
                    Fugui.Push(ImGuiCol.FrameBgHovered, FuThemeManager.GetColor(FuColors.FrameBgHovered));
                    Fugui.Push(ImGuiCol.FrameBgActive, FuThemeManager.GetColor(FuColors.FrameBgActive));
                }
            }
            if (_nextIsDisabled)
            {
                bool value = isChecked; // Create a temporary variable to hold the value of isChecked
                ImGui.Checkbox(text, ref value); // Display a disabled checkbox with the given text label
            }
            else
            {
                clicked = ImGui.Checkbox(text, ref isChecked); // Display an enabled checkbox and update the value of isChecked based on user interaction
            }
            displayToolTip(); // Display a tooltip if one has been set for this element
            _elementHoverFramed = true; // Set the flag indicating that this element should have a hover frame drawn around it
            endElement(null); // Pop the style for the checkbox element
            Fugui.PopColor(4);
            return clicked; // Return a boolean indicating whether the checkbox was clicked by the user
        }
    }
}