
namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Menu Columns data structure.
        /// </summary>
        internal unsafe partial struct ImGuiMenuColumns
        {
            #region State
            public float Spacing;
            public float Width;
            public float NextWidth;
            public fixed float Pos[3];
            public fixed float NextWidths[3];
            #endregion
        }
}