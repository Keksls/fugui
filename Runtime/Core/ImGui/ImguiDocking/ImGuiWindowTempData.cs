using System;
using UnityEngine;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Gui Window Temp Data data structure.
    /// </summary>
    public unsafe partial struct ImGuiWindowTempData
    {
        #region State
        public Vector2 CursorPos;
        public Vector2 CursorPosPrevLine;
        public Vector2 CursorStartPos;
        public Vector2 CursorMaxPos;
        public Vector2 IdealMaxPos;
        public Vector2 CurrLineSize;
        public Vector2 PrevLineSize;
        public float CurrLineTextBaseOffset;
        public float PrevLineTextBaseOffset;
        public ImVec1 Indent;
        public ImVec1 ColumnsOffset;
        public ImVec1 GroupOffset;
        public uint LastItemId;
        public ImGuiItemStatusFlags LastItemStatusFlags;
        public ImRect LastItemRect;
        public ImRect LastItemDisplayRect;
        public ImGuiNavLayer NavLayerCurrent;
        public int NavLayerActiveMask;
        public int NavLayerActiveMaskNext;
        public uint NavFocusScopeIdCurrent;
        public byte NavHideHighlightOneFrame;
        public byte NavHasScroll;
        public byte MenuBarAppending;
        public Vector2 MenuBarOffset;
        public ImGuiMenuColumns MenuColumns;
        public int TreeDepth;
        public uint TreeJumpToParentOnPopMask;
        public ImVector ChildWindows;
        public ImGuiStorage* StateStorage;
        public ImGuiOldColumns* CurrentColumns;
        public int CurrentTableIdx;
        public ImGuiLayoutType LayoutType;
        public ImGuiLayoutType ParentLayoutType;
        public int FocusCounterRegular;
        public int FocusCounterTabStop;
        public ImGuiItemFlags ItemFlags;
        public float ItemWidth;
        public float TextWrapPos;
        public ImVector ItemWidthStack;
        public ImVector TextWrapPosStack;
        public ImGuiStackSizes StackSizesOnBegin;
        #endregion
    }
    /// Lists the available Im Gui Layout Type values.
    /// </summary>
    public enum ImGuiLayoutType
    {
        Horizontal = 0,
        Vertical = 1,
    }
    /// Lists the available Im Gui Nav Layer values.
    /// </summary>
    public enum ImGuiNavLayer
    {
        _Main = 0,
        _Menu = 1,
        _COUNT = 2,
    }
    /// Lists the available Im Gui Item Status Flags values.
    /// </summary>
    [System.Flags]
    public enum ImGuiItemStatusFlags
    {
        None = 0,
        HoveredRect = 1 << 0,
        HasDisplayRect = 1 << 1,
        Edited = 1 << 2,
        ToggledSelection = 1 << 3,
        ToggledOpen = 1 << 4,
        HasDeactivated = 1 << 5,
        Deactivated = 1 << 6,
    }
}