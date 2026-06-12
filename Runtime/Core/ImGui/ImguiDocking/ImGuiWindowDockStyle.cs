
namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Window Dock Style data structure.
        /// </summary>
        internal unsafe partial struct ImGuiWindowDockStyle
        {
            #region State
            public fixed uint Colors[6];
            #endregion
        }
}