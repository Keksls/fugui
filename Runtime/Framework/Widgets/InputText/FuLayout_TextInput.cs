using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        /// <summary>
        /// Displays a single-line text input field
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="style">the Frame Style to use</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, ref string text, FuFrameStyle style, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, "", ref text, 2048, 0f, style, width, flags);
        }
        /// <summary>
        /// Displays a single-line text input field
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, ref string text, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, "", ref text, 2048, 0f, FuFrameStyle.Default, width, flags);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, hint, ref text, 2048, 0f, FuFrameStyle.Default, width, flags);
        }

        /// <summary>
        /// Displays a single-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, uint size, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, hint, ref text, size, 0f, FuFrameStyle.Default, width, flags);
        }

        /// <summary>
        /// Displays a multi-line text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="height">The height of the input field</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public bool TextInput(string id, string hint, ref string text, uint size, float height, float width = 0f, FuInputTextFlags flags = FuInputTextFlags.Default)
        {
            return TextInput(id, hint, ref text, size, height, FuFrameStyle.Default, width, flags);
        }

        /// <summary>
        /// Displays a text input field with a hint
        /// </summary>
        /// <param name="id">A unique identifier for the text input field</param>
        /// <param name="hint">The hint that will be displayed when the field is empty</param>
        /// <param name="text">A reference to the string that will be edited</param>
        /// <param name="size">the maximum size of the text buffer</param>
        /// <param name="height">The height of the input field</param>
        /// <param name="width">The width of the input field</param>
        /// <param name="style">the Frame Style to use</param>
        /// <param name="flags">Flag for custom InputText Behaviour</param>
        /// <returns>true if value has just been edited</returns>
        public virtual bool TextInput(string id, string hint, ref string text, uint size, float height, FuFrameStyle style, float width, FuInputTextFlags flags)
        {
            bool edited;
            // Begin the element and apply the specified style
            beginElement(ref id, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return false;
            }

            edited = _internalTextInput(id, hint, ref text, size, height, width, flags);
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, edited);
            // Display a tool tip if one has been set
            displayToolTip();
            // Mark the element as hover framed
            _elementHoverFramedEnabled = true;
            // End the element
            endElement(style);
            // Return whether the text was edited
            return edited;
        }

        private bool _internalTextInput(string id, string hint, ref string text, uint size, float height, float width, FuInputTextFlags flags)
        {
            bool edited;
            if (width == 0)
            {
                width = ImGui.GetContentRegionAvail().x;
            }
            else if (width < 0)
            {
                width = ImGui.GetContentRegionAvail().x + width * Fugui.CurrentContext.Scale;
            }
            else
            {
                width *= Fugui.CurrentContext.Scale;
            }
            // Set the width of the next item to the width of the available content region
            ImGui.SetNextItemWidth(width);
            // prevent user for editing the value if the element is disabled
            if (LastItemDisabled)
            {
                flags |= FuInputTextFlags.ReadOnly;
            }
            // set multiline if height has a value
            if (height > 0)
            {
                edited = ImGui.InputTextMultiline(id, ref text, size, new Vector2(ImGui.GetContentRegionAvail().x, height * Fugui.CurrentContext.Scale), (ImGuiInputTextFlags)flags);
            }
            // Otherwise, create a single line text input with a hint
            else
            {
                edited = ImGui.InputTextWithHint(id, hint, ref text, size, (ImGuiInputTextFlags)flags);
            }

            return edited;
        }
    }
}