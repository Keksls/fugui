using ImGuiNET;

namespace Fu.Framework
{
    /// <summary>
    /// A Selectable Item that draw a Button
    /// </summary>
    public struct FuSelectable_Button : IFuSelectable
    {
        private string _text;
        public string Text { get => _text; set => _text = value; }
        private FuElementSize _size;
        public FuElementSize Size { get => _size; set => _size = value; }
        private bool _enabled;
        public bool Enabled { get => _enabled; set => _enabled = value; }

        /// <summary>
        /// A Selectable Item that draw a Button
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="enabled">whatever the button is enabled (clickable + style)</param>
        public FuSelectable_Button(string text, bool enabled = true)
        {
            _text = text;
            _enabled = enabled;
            _size = FuElementSize.AutoSize;
        }

        /// <summary>
        /// A Selectable Item that draw a Button
        /// </summary>
        /// <param name="text">text of the button</param>
        /// <param name="size">size of the button</param>
        /// <param name="enabled">whatever the button is enabled (clickable + style)</param>
        public FuSelectable_Button(string text, FuElementSize size, bool enabled = true)
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
                FuButtonStyle.Highlight.Push(_enabled);
            }
            else
            {
                FuButtonStyle.Default.Push(_enabled);
            }
            bool clicked = ImGui.Button(Text, _size.GetSize());
            FuButtonStyle.Default.Pop();
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