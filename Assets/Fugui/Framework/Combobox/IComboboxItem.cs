namespace Fugui.Framework
{
    /// <summary>
    /// Interface that represent UIElements that can be displayed into a combobox
    /// </summary>
    public interface IComboboxItem
    {
        public bool Enabled { get; set; }
        public string Text { get; set; }
        public bool DrawItem(bool selected);
    }
}