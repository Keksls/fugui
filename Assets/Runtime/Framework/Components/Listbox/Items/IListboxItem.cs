namespace Fu.Framework
{
    /// <summary>
    /// Interface that represent UIElements that can be displayed into a listbox
    /// </summary>
    public interface IListboxItem
    {
        public bool Enabled { get; set; }
        public string Text { get; set; }
        public bool DrawItem(bool selected);
    }
}