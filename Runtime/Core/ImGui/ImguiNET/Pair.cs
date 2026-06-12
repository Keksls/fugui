
namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Gui Storage Pair data structure.
    /// </summary>
    internal struct ImGuiStoragePair
    {
        #region State
        public uint Key;
        public UnionValue Value;
        #endregion
    }
}