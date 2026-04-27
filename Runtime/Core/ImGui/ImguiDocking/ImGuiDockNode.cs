using UnityEngine;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Gui Dock Node data structure.
    /// </summary>
    public unsafe partial struct ImGuiDockNode
    {
        #region State
        public uint ID;
        public ImGuiDockNodeFlags SharedFlags;
        public ImGuiDockNodeFlags LocalFlags;
        public ImGuiDockNodeState State;
        public ImGuiDockNode* ParentNode;
        public ImGuiDockNode* ChildNodes_0;
        public ImGuiDockNode* ChildNodes_1;
        public ImVector Windows;
        public ImGuiTabBar* TabBar;
        public Vector2 Pos;
        public Vector2 Size;
        public Vector2 SizeRef;
        public ImGuiAxis SplitAxis;
        public ImGuiWindowClass WindowClass;
        public ImGuiWindow* HostWindow;
        public ImGuiWindow* VisibleWindow;
        public ImGuiDockNode* CentralNode;
        public ImGuiDockNode* OnlyNodeWithWindows;
        public int LastFrameAlive;
        public int LastFrameActive;
        public int LastFrameFocused;
        public uint LastFocusedNodeId;
        public uint SelectedTabId;
        public uint WantCloseTabId;
        public ImGuiDataAuthority AuthorityForPos;
        public ImGuiDataAuthority AuthorityForSize;
        public ImGuiDataAuthority AuthorityForViewport;
        public byte IsVisible;
        public byte IsFocused;
        public byte HasCloseButton;
        public byte HasWindowMenuButton;
        public byte EnableCloseButton;
        public byte WantCloseAll;
        public byte WantLockSizeOnce;
        public byte WantMouseMove;
        public byte WantHiddenTabBarUpdate;
        public byte WantHiddenTabBarToggle;
        public byte MarkedForPosSizeWrite;
        #endregion
    }
    /// Lists the available Im Gui Dock Node State values.
    /// </summary>
    public enum ImGuiDockNodeState
    {
        _Unknown = 0,
        _HostWindowHiddenBecauseSingleWindow = 1,
        _HostWindowHiddenBecauseWindowsAreResizing = 2,
        _HostWindowVisible = 3,
    }
    /// <summary>
    /// Lists the available Im Gui Axis values.
    /// </summary>
    public enum ImGuiAxis
    {
        _None = -1,
        _X = 0,
        _Y = 1,
    }
    /// <summary>
    /// Lists the available Im Gui Data Authority values.
    /// </summary>
    public enum ImGuiDataAuthority
    {
        Auto = 0,
        DockNode = 1,
        Window = 2,
    }
}