using System;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Gui Old Columns data structure.
    /// </summary>
    public unsafe partial struct ImGuiOldColumns
    {
        #region State
        public uint ID;
        public ImGuiOldColumnFlags Flags;
        public byte IsFirstFrame;
        public byte IsBeingResized;
        public int Current;
        public int Count;
        public float OffMinX;
        public float OffMaxX;
        public float LineMinY;
        public float LineMaxY;
        public float HostCursorPosY;
        public float HostCursorMaxPosX;
        public ImRect HostInitialClipRect;
        public ImRect HostBackupClipRect;
        public ImRect HostBackupParentWorkRect;
        public ImVector Columns;
        public ImDrawListSplitter Splitter;
        #endregion
    }
    /// <summary>
    /// Lists the available Im Gui Old Column Flags values.
    /// </summary>
    [Flags]
    public enum ImGuiOldColumnFlags
    {
        None = 0,
        NoBorder = 1 << 0,
        NoResize = 1 << 1,
        NoPreserveWidths = 1 << 2,
        NoForceWithinWindow = 1 << 3,
        GrowParentContentsSize = 1 << 4,
    }
}