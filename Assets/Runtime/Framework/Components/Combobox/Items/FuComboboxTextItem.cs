using ImGuiNET;

namespace Fu.Framework
{
    /// <summary>
    /// A ComboboxItem that add a Selectable Text item (Default Item)
    /// </summary>
    public struct FuComboboxTextItem : IComboboxItem
    {
        private string _text;
        private bool _enabled;
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public string Text { get => _text; set => _text = value; }

        /// <summary>
        /// A ComboboxItem that add a Selectable Text item (Default Item)
        /// </summary>
        /// <param name="text">text value of the selectable text</param>
        /// <param name="enabled">whatever the text is enabled (clickable + style)</param>
        public FuComboboxTextItem(string text, bool enabled)
        {
            _enabled = enabled;
            _text = text;
        }

        /// <summary>
        /// A ComboboxItem that add a Selectable Text item (Default Item)
        /// </summary>
        /// <param name="selected">whatever the text is selected</param>
        /// <returns>true if clicked</returns>
        public bool DrawItem(bool selected)
        {
            return ImGui.Selectable(_text, selected, _enabled ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled);
        }

        /// <summary>
        /// get the string representation of this item
        /// </summary>
        /// <returns>string that represent this item value</returns>
        public override string ToString()
        {
            return _text;
        }
    }
}