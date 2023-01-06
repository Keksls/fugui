using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
    public unsafe partial struct ImGuiWindow
    {
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
    }
    public unsafe partial struct ImGuiWindowDockStyle
    {
        public fixed uint Colors[6];
    }
    public unsafe partial struct ImGuiWindowDockStylePtr
    {
        public ImGuiWindowDockStyle* NativePtr { get; }
        public ImGuiWindowDockStylePtr(ImGuiWindowDockStyle* nativePtr) => NativePtr = nativePtr;
        public ImGuiWindowDockStylePtr(IntPtr nativePtr) => NativePtr = (ImGuiWindowDockStyle*)nativePtr;
        public static implicit operator ImGuiWindowDockStylePtr(ImGuiWindowDockStyle* nativePtr) => new ImGuiWindowDockStylePtr(nativePtr);
        public static implicit operator ImGuiWindowDockStyle*(ImGuiWindowDockStylePtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImGuiWindowDockStylePtr(IntPtr nativePtr) => new ImGuiWindowDockStylePtr(nativePtr);
        public RangeAccessor<uint> Colors => new RangeAccessor<uint>(NativePtr->Colors, 6);
    }
    public unsafe partial struct ImVec2ih
    {
        public short x;
        public short y;
    }
    public unsafe partial struct ImVec2ihPtr
    {
        public ImVec2ih* NativePtr { get; }
        public ImVec2ihPtr(ImVec2ih* nativePtr) => NativePtr = nativePtr;
        public ImVec2ihPtr(IntPtr nativePtr) => NativePtr = (ImVec2ih*)nativePtr;
        public static implicit operator ImVec2ihPtr(ImVec2ih* nativePtr) => new ImVec2ihPtr(nativePtr);
        public static implicit operator ImVec2ih*(ImVec2ihPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImVec2ihPtr(IntPtr nativePtr) => new ImVec2ihPtr(nativePtr);
        public ref short x => ref Unsafe.AsRef<short>(&NativePtr->x);
        public ref short y => ref Unsafe.AsRef<short>(&NativePtr->y);
    }
    public unsafe partial struct ImGuiWindowPtr
    {
        public ImGuiWindow* NativePtr { get; }
        public ImGuiWindowPtr(ImGuiWindow* nativePtr) => NativePtr = nativePtr;
        public ImGuiWindowPtr(IntPtr nativePtr) => NativePtr = (ImGuiWindow*)nativePtr;
        public static implicit operator ImGuiWindowPtr(ImGuiWindow* nativePtr) => new ImGuiWindowPtr(nativePtr);
        public static implicit operator ImGuiWindow*(ImGuiWindowPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImGuiWindowPtr(IntPtr nativePtr) => new ImGuiWindowPtr(nativePtr);
        public NullTerminatedString Name => new NullTerminatedString(NativePtr->Name);
        public ref uint ID => ref Unsafe.AsRef<uint>(&NativePtr->ID);
        public ref ImGuiWindowFlags Flags => ref Unsafe.AsRef<ImGuiWindowFlags>(&NativePtr->Flags);
        public ref ImGuiWindowFlags FlagsPreviousFrame => ref Unsafe.AsRef<ImGuiWindowFlags>(&NativePtr->FlagsPreviousFrame);
        public ref ImGuiWindowClass WindowClass => ref Unsafe.AsRef<ImGuiWindowClass>(&NativePtr->WindowClass);
        public ImGuiViewportPPtr Viewport => new ImGuiViewportPPtr(NativePtr->Viewport);
        public ref uint ViewportId => ref Unsafe.AsRef<uint>(&NativePtr->ViewportId);
        public ref Vector2 ViewportPos => ref Unsafe.AsRef<Vector2>(&NativePtr->ViewportPos);
        public ref int ViewportAllowPlatformMonitorExtend => ref Unsafe.AsRef<int>(&NativePtr->ViewportAllowPlatformMonitorExtend);
        public ref Vector2 Pos => ref Unsafe.AsRef<Vector2>(&NativePtr->Pos);
        public ref Vector2 Size => ref Unsafe.AsRef<Vector2>(&NativePtr->Size);
        public ref Vector2 SizeFull => ref Unsafe.AsRef<Vector2>(&NativePtr->SizeFull);
        public ref Vector2 ContentSize => ref Unsafe.AsRef<Vector2>(&NativePtr->ContentSize);
        public ref Vector2 ContentSizeIdeal => ref Unsafe.AsRef<Vector2>(&NativePtr->ContentSizeIdeal);
        public ref Vector2 ContentSizeExplicit => ref Unsafe.AsRef<Vector2>(&NativePtr->ContentSizeExplicit);
        public ref Vector2 WindowPadding => ref Unsafe.AsRef<Vector2>(&NativePtr->WindowPadding);
        public ref float WindowRounding => ref Unsafe.AsRef<float>(&NativePtr->WindowRounding);
        public ref float WindowBorderSize => ref Unsafe.AsRef<float>(&NativePtr->WindowBorderSize);
        public ref int NameBufLen => ref Unsafe.AsRef<int>(&NativePtr->NameBufLen);
        public ref uint MoveId => ref Unsafe.AsRef<uint>(&NativePtr->MoveId);
        public ref uint ChildId => ref Unsafe.AsRef<uint>(&NativePtr->ChildId);
        public ref Vector2 Scroll => ref Unsafe.AsRef<Vector2>(&NativePtr->Scroll);
        public ref Vector2 ScrollMax => ref Unsafe.AsRef<Vector2>(&NativePtr->ScrollMax);
        public ref Vector2 ScrollTarget => ref Unsafe.AsRef<Vector2>(&NativePtr->ScrollTarget);
        public ref Vector2 ScrollTargetCenterRatio => ref Unsafe.AsRef<Vector2>(&NativePtr->ScrollTargetCenterRatio);
        public ref Vector2 ScrollTargetEdgeSnapDist => ref Unsafe.AsRef<Vector2>(&NativePtr->ScrollTargetEdgeSnapDist);
        public ref Vector2 ScrollbarSizes => ref Unsafe.AsRef<Vector2>(&NativePtr->ScrollbarSizes);
        public ref bool ScrollbarX => ref Unsafe.AsRef<bool>(&NativePtr->ScrollbarX);
        public ref bool ScrollbarY => ref Unsafe.AsRef<bool>(&NativePtr->ScrollbarY);
        public ref bool ViewportOwned => ref Unsafe.AsRef<bool>(&NativePtr->ViewportOwned);
        public ref bool Active => ref Unsafe.AsRef<bool>(&NativePtr->Active);
        public ref bool WasActive => ref Unsafe.AsRef<bool>(&NativePtr->WasActive);
        public ref bool WriteAccessed => ref Unsafe.AsRef<bool>(&NativePtr->WriteAccessed);
        public ref bool Collapsed => ref Unsafe.AsRef<bool>(&NativePtr->Collapsed);
        public ref bool WantCollapseToggle => ref Unsafe.AsRef<bool>(&NativePtr->WantCollapseToggle);
        public ref bool SkipItems => ref Unsafe.AsRef<bool>(&NativePtr->SkipItems);
        public ref bool Appearing => ref Unsafe.AsRef<bool>(&NativePtr->Appearing);
        public ref bool Hidden => ref Unsafe.AsRef<bool>(&NativePtr->Hidden);
        public ref bool IsFallbackWindow => ref Unsafe.AsRef<bool>(&NativePtr->IsFallbackWindow);
        public ref bool HasCloseButton => ref Unsafe.AsRef<bool>(&NativePtr->HasCloseButton);
        public ref sbyte ResizeBorderHeld => ref Unsafe.AsRef<sbyte>(&NativePtr->ResizeBorderHeld);
        public ref short BeginCount => ref Unsafe.AsRef<short>(&NativePtr->BeginCount);
        public ref short BeginOrderWithinParent => ref Unsafe.AsRef<short>(&NativePtr->BeginOrderWithinParent);
        public ref short BeginOrderWithinContext => ref Unsafe.AsRef<short>(&NativePtr->BeginOrderWithinContext);
        public ref uint PopupId => ref Unsafe.AsRef<uint>(&NativePtr->PopupId);
        public ref sbyte AutoFitFramesX => ref Unsafe.AsRef<sbyte>(&NativePtr->AutoFitFramesX);
        public ref sbyte AutoFitFramesY => ref Unsafe.AsRef<sbyte>(&NativePtr->AutoFitFramesY);
        public ref sbyte AutoFitChildAxises => ref Unsafe.AsRef<sbyte>(&NativePtr->AutoFitChildAxises);
        public ref bool AutoFitOnlyGrows => ref Unsafe.AsRef<bool>(&NativePtr->AutoFitOnlyGrows);
        public ref ImGuiDir AutoPosLastDirection => ref Unsafe.AsRef<ImGuiDir>(&NativePtr->AutoPosLastDirection);
        public ref sbyte HiddenFramesCanSkipItems => ref Unsafe.AsRef<sbyte>(&NativePtr->HiddenFramesCanSkipItems);
        public ref sbyte HiddenFramesCannotSkipItems => ref Unsafe.AsRef<sbyte>(&NativePtr->HiddenFramesCannotSkipItems);
        public ref sbyte HiddenFramesForRenderOnly => ref Unsafe.AsRef<sbyte>(&NativePtr->HiddenFramesForRenderOnly);
        public ref ImGuiCond SetWindowPosAllowFlags => ref Unsafe.AsRef<ImGuiCond>(&NativePtr->SetWindowPosAllowFlags);
        public ref ImGuiCond SetWindowSizeAllowFlags => ref Unsafe.AsRef<ImGuiCond>(&NativePtr->SetWindowSizeAllowFlags);
        public ref ImGuiCond SetWindowCollapsedAllowFlags => ref Unsafe.AsRef<ImGuiCond>(&NativePtr->SetWindowCollapsedAllowFlags);
        public ref ImGuiCond SetWindowDockAllowFlags => ref Unsafe.AsRef<ImGuiCond>(&NativePtr->SetWindowDockAllowFlags);
        public ref Vector2 SetWindowPosVal => ref Unsafe.AsRef<Vector2>(&NativePtr->SetWindowPosVal);
        public ref Vector2 SetWindowPosPivot => ref Unsafe.AsRef<Vector2>(&NativePtr->SetWindowPosPivot);
        public ImVector<uint> IDStack => new ImVector<uint>(NativePtr->IDStack);
        public ref ImGuiWindowTempData DC => ref Unsafe.AsRef<ImGuiWindowTempData>(&NativePtr->DC);
        public ref ImRect OuterRectClipped => ref Unsafe.AsRef<ImRect>(&NativePtr->OuterRectClipped);
        public ref ImRect InnerRect => ref Unsafe.AsRef<ImRect>(&NativePtr->InnerRect);
        public ref ImRect InnerClipRect => ref Unsafe.AsRef<ImRect>(&NativePtr->InnerClipRect);
        public ref ImRect WorkRect => ref Unsafe.AsRef<ImRect>(&NativePtr->WorkRect);
        public ref ImRect ParentWorkRect => ref Unsafe.AsRef<ImRect>(&NativePtr->ParentWorkRect);
        public ref ImRect ClipRect => ref Unsafe.AsRef<ImRect>(&NativePtr->ClipRect);
        public ref ImRect ContentRegionRect => ref Unsafe.AsRef<ImRect>(&NativePtr->ContentRegionRect);
        public ref ImVec2ih HitTestHoleSize => ref Unsafe.AsRef<ImVec2ih>(&NativePtr->HitTestHoleSize);
        public ref ImVec2ih HitTestHoleOffset => ref Unsafe.AsRef<ImVec2ih>(&NativePtr->HitTestHoleOffset);
        public ref int LastFrameActive => ref Unsafe.AsRef<int>(&NativePtr->LastFrameActive);
        public ref int LastFrameJustFocused => ref Unsafe.AsRef<int>(&NativePtr->LastFrameJustFocused);
        public ref float LastTimeActive => ref Unsafe.AsRef<float>(&NativePtr->LastTimeActive);
        public ref float ItemWidthDefault => ref Unsafe.AsRef<float>(&NativePtr->ItemWidthDefault);
        public ref ImGuiStorage StateStorage => ref Unsafe.AsRef<ImGuiStorage>(&NativePtr->StateStorage);
        public ImPtrVector<ImGuiOldColumnsPtr> ColumnsStorage => new ImPtrVector<ImGuiOldColumnsPtr>(NativePtr->ColumnsStorage, Unsafe.SizeOf<ImGuiOldColumns>());
        public ref float FontWindowScale => ref Unsafe.AsRef<float>(&NativePtr->FontWindowScale);
        public ref float FontDpiScale => ref Unsafe.AsRef<float>(&NativePtr->FontDpiScale);
        public ref int SettingsOffset => ref Unsafe.AsRef<int>(&NativePtr->SettingsOffset);
        public ImDrawListPtr DrawList => new ImDrawListPtr(NativePtr->DrawList);
        public ref ImDrawList DrawListInst => ref Unsafe.AsRef<ImDrawList>(&NativePtr->DrawListInst);
        public ImGuiWindowPtr ParentWindow => new ImGuiWindowPtr(NativePtr->ParentWindow);
        public ImGuiWindowPtr RootWindow => new ImGuiWindowPtr(NativePtr->RootWindow);
        public ImGuiWindowPtr RootWindowDockStop => new ImGuiWindowPtr(NativePtr->RootWindowDockStop);
        public ImGuiWindowPtr RootWindowForTitleBarHighlight => new ImGuiWindowPtr(NativePtr->RootWindowForTitleBarHighlight);
        public ImGuiWindowPtr RootWindowForNav => new ImGuiWindowPtr(NativePtr->RootWindowForNav);
        public ImGuiWindowPtr NavLastChildNavWindow => new ImGuiWindowPtr(NativePtr->NavLastChildNavWindow);
        public RangeAccessor<uint> NavLastIds => new RangeAccessor<uint>(NativePtr->NavLastIds, 2);
        public RangeAccessor<ImRect> NavRectRel => new RangeAccessor<ImRect>(&NativePtr->NavRectRel_0, 2);
        public ref int MemoryDrawListIdxCapacity => ref Unsafe.AsRef<int>(&NativePtr->MemoryDrawListIdxCapacity);
        public ref int MemoryDrawListVtxCapacity => ref Unsafe.AsRef<int>(&NativePtr->MemoryDrawListVtxCapacity);
        public ref bool MemoryCompacted => ref Unsafe.AsRef<bool>(&NativePtr->MemoryCompacted);
        public ref bool DockIsActive => ref Unsafe.AsRef<bool>(&NativePtr->DockIsActive);
        public ref bool DockTabIsVisible => ref Unsafe.AsRef<bool>(&NativePtr->DockTabIsVisible);
        public ref bool DockTabWantClose => ref Unsafe.AsRef<bool>(&NativePtr->DockTabWantClose);
        public ref short DockOrder => ref Unsafe.AsRef<short>(&NativePtr->DockOrder);
        public ref ImGuiWindowDockStyle DockStyle => ref Unsafe.AsRef<ImGuiWindowDockStyle>(&NativePtr->DockStyle);
        public ImGuiDockNodePtr DockNode => new ImGuiDockNodePtr(NativePtr->DockNode);
        public ImGuiDockNodePtr DockNodeAsHost => new ImGuiDockNodePtr(NativePtr->DockNodeAsHost);
        public ref uint DockId => ref Unsafe.AsRef<uint>(&NativePtr->DockId);
        public ref ImGuiItemStatusFlags DockTabItemStatusFlags => ref Unsafe.AsRef<ImGuiItemStatusFlags>(&NativePtr->DockTabItemStatusFlags);
        public ref ImRect DockTabItemRect => ref Unsafe.AsRef<ImRect>(&NativePtr->DockTabItemRect);      
    }
}