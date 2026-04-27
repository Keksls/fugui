using System;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Tab Item data structure.
        /// </summary>
        public unsafe partial struct ImGuiTabItem
        {
            #region State
            public uint ID;
            public ImGuiTabItemFlags Flags;
            public ImGuiWindow* Window;
            public int LastFrameVisible;
            public int LastFrameSelected;
            public float Offset;
            public float Width;
            public float ContentWidth;
            public short NameOffset;
            public short BeginOrder;
            public short IndexDuringLayout;
            public byte WantClose;
            #endregion
        }
}