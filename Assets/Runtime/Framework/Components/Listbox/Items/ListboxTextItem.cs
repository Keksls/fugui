using ImGuiNET;

namespace Fugui.Framework
{

    public struct ListboxTextItem : IListboxItem
    {
        private string _text;
        private bool _enabled;

        public bool Enabled { get => _enabled; set => _enabled = value; }
        public string Text { get => _text; set => _text = value; }

        public ListboxTextItem(string text, bool enabled)
        {
            _enabled = enabled;
            _text = text;
        }

        public bool DrawItem(bool selected)
        {
            return ImGui.Selectable(_text, selected, _enabled ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled);
        }

        public override string ToString()
        {
            return _text;
        }
    }
}