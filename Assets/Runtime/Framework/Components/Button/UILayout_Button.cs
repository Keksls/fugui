using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    public partial class UILayout
    {
        /// <summary>
        /// Renders a button with the given text. The button will have the default size and style.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public bool Button(string text)
        {
            return Button(text, UIButtonStyle.FullSize, UIButtonStyle.Default);
        }

        /// <summary>
        /// Renders a button with the given text and size. The button will have the default style.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="size">The size of the button. If either dimension is set to -1, it will be set to the available content region size in that direction.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public bool Button(string text, Vector2 size)
        {
            return Button(text, size, UIButtonStyle.Default);
        }

        /// <summary>
        /// Renders a button with the given text and style. The button will have the default size.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="style">The style to apply to the button.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public bool Button(string text, UIButtonStyle style)
        {
            return Button(text, UIButtonStyle.FullSize, style);
        }

        /// <summary>
        /// Renders a button with the given text, size, and style.
        /// </summary>
        /// <param name="text">The text to display on the button.</param>
        /// <param name="size">The size of the button. If either dimension is set to -1, it will be set to the available content region size in that direction.</param>
        /// <param name="style">The style to apply to the button.</param>
        /// <returns>True if the button was clicked, false otherwise.</returns>
        public virtual bool Button(string text, Vector2 size, UIButtonStyle style)
        {
            text = beginElement(text, style); // apply style and check if the element should be disabled
            if (size.x == -1)
            {
                size.x = ImGui.GetContentRegionAvail().x; // set size to the available content region size if it is set to -1
            }
            if (size.y == -1)
            {
                size.y = 0; // set size to 0 if it is set to -1
            }
            bool clicked = ImGui.Button(text, size) & !_nextIsDisabled; // render the button and return true if it was clicked, false otherwise
            displayToolTip(); // display the tooltip if necessary
            endElement(style); // remove the style and draw the hover frame if necessary
            return clicked;
        }
    }
}