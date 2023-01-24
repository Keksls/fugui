using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {
        /// <summary>
        /// Displays a single-line text input field
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="style">the Frame Style to use</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, ref string text, UIFrameStyle style)
        {
            return TextInput(id, "", ref text, 2048, 0, style);
        }
        /// <summary>
        /// Displays a single-line text input field
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, ref string text)
        {
            return TextInput(id, "", ref text, 2048, 0, UIFrameStyle.Default);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text)
        {
            return TextInput(id, hint, ref text, 2048, 0, UIFrameStyle.Default);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, uint size)
        {
            return TextInput(id, hint, ref text, size, 0, UIFrameStyle.Default);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="height">The height of the input field</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, uint size, float height)
        {
            return TextInput(id, hint, ref text, size, height, UIFrameStyle.Default);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="height">The height of the input field</param>
        /// <returns>true if value has just been edited</returns>
        /// <param name="style">the Frame Style to use</param>
        public virtual bool TextInput(string id, string hint, ref string text, uint size, float height, UIFrameStyle style)
        {
            bool edited;
            // Begin the element and apply the specified style
            id = beginElement(id, style);
            // Set the width of the next item to the width of the available content region
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);
            // If a height was specified, create a multiline text input
            if (height > 0)
            {
                edited = ImGui.InputTextMultiline(id, ref text, size, new Vector2(ImGui.GetContentRegionAvail().x, height), ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CtrlEnterForNewLine);
            }
            // Otherwise, create a single line text input with a hint
            else
            {
                edited = ImGui.InputTextWithHint(id, hint, ref text, size, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CtrlEnterForNewLine);
            }
            // Display a tool tip if one has been set
            displayToolTip();
            // Mark the element as hover framed
            _elementHoverFramed = true;
            // End the element
            endElement(style);
            // Return whether the text was edited
            return edited;
        }
    }
}