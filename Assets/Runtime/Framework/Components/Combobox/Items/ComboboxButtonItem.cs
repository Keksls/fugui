using ImGuiNET;
using UnityEngine;

namespace Fugui.Framework
{
    /// <summary>
    /// A ComboboxItem that draw a Button
    /// </summary>
    public struct ComboboxButtonItem : IComboboxItem
    {
        private string _text;
        public string Text { get => _text; set => _text = value; }
        private UIElementSize _size;
        public UIElementSize Size { get => _size; set => _size = value; }
        private bool _enabled;
        public bool Enabled { get => _enabled; set => _enabled = value; }

        /// <summary>
        /// A ComboboxItem that draw a Button
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="enabled">whatever the button is enabled (clickable + style)</param>
        public ComboboxButtonItem(string text, bool enabled = true)
        {
            _text = text;
            _enabled = enabled;
            _size = UIElementSize.AutoSize;
        }

        /// <summary>
        /// A ComboboxItem that draw a Button
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="enabled">whatever the button is enabled (clickable + style)</param>
        public ComboboxButtonItem(string text, UIElementSize size, bool enabled = true)
        {
            _text = text;
            _enabled = enabled;
            _size = size;
        }

        /// <summary>
        /// Draw the Combobox Item (call by Combobox)
        /// </summary>
        /// <param name="selected">whatever the item is selected</param>
        /// <returns>true if item is clicked</returns>
        public bool DrawItem(bool selected)
        {
            if (selected)
            {
                UIButtonStyle.Highlight.Push(_enabled);
            }
            else
            {
                UIButtonStyle.Default.Push(_enabled);
            }
            bool clicked = ImGui.Button(Text, _size.GetSize());
            UIButtonStyle.Default.Pop();
            return clicked;
        }

        /// <summary>
        /// get the string representation of this item
        /// </summary>
        /// <returns>string that represent this item value</returns>
        public override string ToString()
        {
            return Text;
        }
    }
}