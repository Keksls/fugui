namespace Fu.Framework
{
    /// <summary>
    /// Interface that represent FuElements that can be displayed and selected
    /// </summary>
    public interface IFuSelectable
    {
        public bool Enabled { get; set; }
        public string Text { get; set; }
        public bool DrawItem(bool selected);
    }
}