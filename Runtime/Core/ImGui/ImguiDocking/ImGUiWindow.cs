using System;
using UnityEngine;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Gui Window data structure.
    /// </summary>
    public unsafe partial struct ImGuiWindow
    {
        #region State
        public byte* Name;
        public uint ID;
        public ImGuiWindowFlags Flags;
        public ImGuiWindowFlags FlagsPreviousFrame;
        public ImGuiWindowClass WindowClass;
        public ImGuiViewportP* Viewport;
        public uint ViewportId;
        public Vector2 ViewportPos;
        public int ViewportAllowPlatformMonitorExtend;
        public Vector2 Pos;
        public Vector2 Size;
        public Vector2 SizeFull;
        public Vector2 ContentSize;
        public Vector2 ContentSizeIdeal;
        public Vector2 ContentSizeExplicit;
        public Vector2 WindowPadding;
        public float WindowRounding;
        public float WindowBorderSize;
        public int NameBufLen;
        public uint MoveId;
        public uint ChildId;
        public Vector2 Scroll;
        public Vector2 ScrollMax;
        public Vector2 ScrollTarget;
        public Vector2 ScrollTargetCenterRatio;
        public Vector2 ScrollTargetEdgeSnapDist;
        public Vector2 ScrollbarSizes;
        public byte ScrollbarX;
        public byte ScrollbarY;
        public byte ViewportOwned;
        public byte Active;
        public byte WasActive;
        public byte WriteAccessed;
        public byte Collapsed;
        public byte WantCollapseToggle;
        public byte SkipItems;
        public byte Appearing;
        public byte Hidden;
        public byte IsFallbackWindow;
        public byte HasCloseButton;
        public sbyte ResizeBorderHeld;
        public short BeginCount;
        public short BeginOrderWithinParent;
        public short BeginOrderWithinContext;
        public uint PopupId;
        public sbyte AutoFitFramesX;
        public sbyte AutoFitFramesY;
        public sbyte AutoFitChildAxises;
        public byte AutoFitOnlyGrows;
        public ImGuiDir AutoPosLastDirection;
        public sbyte HiddenFramesCanSkipItems;
        public sbyte HiddenFramesCannotSkipItems;
        public sbyte HiddenFramesForRenderOnly;
        public ImGuiCond SetWindowPosAllowFlags;
        public ImGuiCond SetWindowSizeAllowFlags;
        public ImGuiCond SetWindowCollapsedAllowFlags;
        public ImGuiCond SetWindowDockAllowFlags;
        public Vector2 SetWindowPosVal;
        public Vector2 SetWindowPosPivot;
        public ImVector IDStack;
        public ImGuiWindowTempData DC;
        public ImRect OuterRectClipped;
        public ImRect InnerRect;
        public ImRect InnerClipRect;
        public ImRect WorkRect;
        public ImRect ParentWorkRect;
        public ImRect ClipRect;
        public ImRect ContentRegionRect;
        public ImVec2ih HitTestHoleSize;
        public ImVec2ih HitTestHoleOffset;
        public int LastFrameActive;
        public int LastFrameJustFocused;
        public float LastTimeActive;
        public float ItemWidthDefault;
        public ImGuiStorage StateStorage;
        public ImVector ColumnsStorage;
        public float FontWindowScale;
        public float FontDpiScale;
        public int SettingsOffset;
        public ImDrawList* DrawList;
        public ImDrawList DrawListInst;
        public ImGuiWindow* ParentWindow;
        public ImGuiWindow* RootWindow;
        public ImGuiWindow* RootWindowDockStop;
        public ImGuiWindow* RootWindowForTitleBarHighlight;
        public ImGuiWindow* RootWindowForNav;
        public ImGuiWindow* NavLastChildNavWindow;
        public fixed uint NavLastIds[2];
        public ImRect NavRectRel_0;
        public ImRect NavRectRel_1;
        public int MemoryDrawListIdxCapacity;
        public int MemoryDrawListVtxCapacity;
        public byte MemoryCompacted;
        public byte DockIsActive;
        public byte DockTabIsVisible;
        public byte DockTabWantClose;
        public short DockOrder;
        public ImGuiWindowDockStyle DockStyle;
        public ImGuiDockNode* DockNode;
        public ImGuiDockNode* DockNodeAsHost;
        public uint DockId;
        public ImGuiItemStatusFlags DockTabItemStatusFlags;
        public ImRect DockTabItemRect;
        #endregion
    }
}