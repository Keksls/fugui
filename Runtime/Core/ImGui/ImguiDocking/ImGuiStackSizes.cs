
namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Stack Sizes data structure.
        /// </summary>
        public unsafe partial struct ImGuiStackSizes
        {
            #region State
            public short SizeOfIDStack;
            public short SizeOfColorStack;
            public short SizeOfStyleVarStack;
            public short SizeOfFontStack;
            public short SizeOfFocusScopeStack;
            public short SizeOfGroupStack;
            public short SizeOfBeginPopupStack;
            #endregion
        }
}