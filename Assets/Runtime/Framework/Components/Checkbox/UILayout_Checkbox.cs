using ImGuiNET;

namespace Fugui.Framework
{
    public partial class UILayout
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
            text = beginElement(text, null); // Push the style for the checkbox element

            // push colors
            if (_nextIsDisabled)
            {
                if (isChecked)
                {
                    FuGui.Push(ImGuiCol.CheckMark, ThemeManager.GetColor(FuguiColors.Knob) * 0.3f);
                    FuGui.Push(ImGuiCol.FrameBg, ThemeManager.GetColor(FuguiColors.CheckMark) * 0.3f);
                    FuGui.Push(ImGuiCol.FrameBgHovered, ThemeManager.GetColor(FuguiColors.CheckMark) * 0.3f);
                    FuGui.Push(ImGuiCol.FrameBgActive, ThemeManager.GetColor(FuguiColors.CheckMark) * 0.3f);
                }
                else
                {
                    FuGui.Push(ImGuiCol.CheckMark, ThemeManager.GetColor(FuguiColors.Knob) * 0.3f);
                    FuGui.Push(ImGuiCol.FrameBg, ThemeManager.GetColor(FuguiColors.FrameBg) * 0.3f);
                    FuGui.Push(ImGuiCol.FrameBgHovered, ThemeManager.GetColor(FuguiColors.FrameBgHovered) * 0.3f);
                    FuGui.Push(ImGuiCol.FrameBgActive, ThemeManager.GetColor(FuguiColors.FrameBgActive) * 0.3f);
                }
            }
            else
            {
                if (isChecked)
                {
                    FuGui.Push(ImGuiCol.CheckMark, ThemeManager.GetColor(FuguiColors.Knob));
                    FuGui.Push(ImGuiCol.FrameBg, ThemeManager.GetColor(FuguiColors.CheckMark));
                    FuGui.Push(ImGuiCol.FrameBgHovered, ThemeManager.GetColor(FuguiColors.CheckMark) * 0.9f);
                    FuGui.Push(ImGuiCol.FrameBgActive, ThemeManager.GetColor(FuguiColors.CheckMark) * 0.8f);
                }
                else
                {
                    FuGui.Push(ImGuiCol.CheckMark, ThemeManager.GetColor(FuguiColors.Knob));
                    FuGui.Push(ImGuiCol.FrameBg, ThemeManager.GetColor(FuguiColors.FrameBg));
                    FuGui.Push(ImGuiCol.FrameBgHovered, ThemeManager.GetColor(FuguiColors.FrameBgHovered));
                    FuGui.Push(ImGuiCol.FrameBgActive, ThemeManager.GetColor(FuguiColors.FrameBgActive));
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
            FuGui.PopColor(4);
            return clicked; // Return a boolean indicating whether the checkbox was clicked by the user
        }
    }
}