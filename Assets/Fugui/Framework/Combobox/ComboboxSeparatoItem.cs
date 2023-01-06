using ImGuiNET;

namespace Fugui.Framework
{
    /// <summary>
    /// A ComboboxItem that add a Separator
    /// </summary>
    public struct ComboboxSeparatorItem : IComboboxItem
    {
        private string _text;
        private bool _enabled;
        public bool Enabled { get => _enabled; set => _enabled = value; }
        public string Text { get => _text; set => _text = value; }

        /// <summary>
        /// Draw a Separator (just execute a SameLine statement, ignore selection and click)
        /// </summary>
        /// <param name="selected">ignored</param>
        /// <returns>ignored</returns>
        public bool DrawItem(bool selected)
        {
            ImGui.Separator();
            return false;
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