using System;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
    public unsafe partial struct ImGuiTabBar
    {
        public ImVector Tabs;
        public ImGuiTabBarFlags Flags;
        public uint ID;
        public uint SelectedTabId;
        public uint NextSelectedTabId;
        public uint VisibleTabId;
        public int CurrFrameVisible;
        public int PrevFrameVisible;
        public ImRect BarRect;
        public float CurrTabsContentsHeight;
        public float PrevTabsContentsHeight;
        public float WidthAllTabs;
        public float WidthAllTabsIdeal;
        public float ScrollingAnim;
        public float ScrollingTarget;
        public float ScrollingTargetDistToVisibility;
        public float ScrollingSpeed;
        public float ScrollingRectMinX;
        public float ScrollingRectMaxX;
        public uint ReorderRequestTabId;
        public sbyte ReorderRequestDir;
        public sbyte BeginCount;
        public byte WantLayout;
        public byte VisibleTabWasSubmitted;
        public byte TabsAddedNew;
        public short TabsActiveCount;
        public short LastTabItemIdx;
        public float ItemSpacingY;
        public Vector2 FramePadding;
        public Vector2 BackupCursorPos;
        public ImGuiTextBuffer TabsNames;
    }
    public unsafe partial struct ImGuiTabBarPtr
    {
        public ImGuiTabBar* NativePtr { get; }
        public ImGuiTabBarPtr(ImGuiTabBar* nativePtr) => NativePtr = nativePtr;
        public ImGuiTabBarPtr(IntPtr nativePtr) => NativePtr = (ImGuiTabBar*)nativePtr;
        public static implicit operator ImGuiTabBarPtr(ImGuiTabBar* nativePtr) => new ImGuiTabBarPtr(nativePtr);
        public static implicit operator ImGuiTabBar*(ImGuiTabBarPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImGuiTabBarPtr(IntPtr nativePtr) => new ImGuiTabBarPtr(nativePtr);
        public ImPtrVector<ImGuiTabItemPtr> Tabs => new ImPtrVector<ImGuiTabItemPtr>(NativePtr->Tabs, Unsafe.SizeOf<ImGuiTabItem>());
        public ref ImGuiTabBarFlags Flags => ref Unsafe.AsRef<ImGuiTabBarFlags>(&NativePtr->Flags);
        public ref uint ID => ref Unsafe.AsRef<uint>(&NativePtr->ID);
        public ref uint SelectedTabId => ref Unsafe.AsRef<uint>(&NativePtr->SelectedTabId);
        public ref uint NextSelectedTabId => ref Unsafe.AsRef<uint>(&NativePtr->NextSelectedTabId);
        public ref uint VisibleTabId => ref Unsafe.AsRef<uint>(&NativePtr->VisibleTabId);
        public ref int CurrFrameVisible => ref Unsafe.AsRef<int>(&NativePtr->CurrFrameVisible);
        public ref int PrevFrameVisible => ref Unsafe.AsRef<int>(&NativePtr->PrevFrameVisible);
        public ref ImRect BarRect => ref Unsafe.AsRef<ImRect>(&NativePtr->BarRect);
        public ref float CurrTabsContentsHeight => ref Unsafe.AsRef<float>(&NativePtr->CurrTabsContentsHeight);
        public ref float PrevTabsContentsHeight => ref Unsafe.AsRef<float>(&NativePtr->PrevTabsContentsHeight);
        public ref float WidthAllTabs => ref Unsafe.AsRef<float>(&NativePtr->WidthAllTabs);
        public ref float WidthAllTabsIdeal => ref Unsafe.AsRef<float>(&NativePtr->WidthAllTabsIdeal);
        public ref float ScrollingAnim => ref Unsafe.AsRef<float>(&NativePtr->ScrollingAnim);
        public ref float ScrollingTarget => ref Unsafe.AsRef<float>(&NativePtr->ScrollingTarget);
        public ref float ScrollingTargetDistToVisibility => ref Unsafe.AsRef<float>(&NativePtr->ScrollingTargetDistToVisibility);
        public ref float ScrollingSpeed => ref Unsafe.AsRef<float>(&NativePtr->ScrollingSpeed);
        public ref float ScrollingRectMinX => ref Unsafe.AsRef<float>(&NativePtr->ScrollingRectMinX);
        public ref float ScrollingRectMaxX => ref Unsafe.AsRef<float>(&NativePtr->ScrollingRectMaxX);
        public ref uint ReorderRequestTabId => ref Unsafe.AsRef<uint>(&NativePtr->ReorderRequestTabId);
        public ref sbyte ReorderRequestDir => ref Unsafe.AsRef<sbyte>(&NativePtr->ReorderRequestDir);
        public ref sbyte BeginCount => ref Unsafe.AsRef<sbyte>(&NativePtr->BeginCount);
        public ref bool WantLayout => ref Unsafe.AsRef<bool>(&NativePtr->WantLayout);
        public ref bool VisibleTabWasSubmitted => ref Unsafe.AsRef<bool>(&NativePtr->VisibleTabWasSubmitted);
        public ref bool TabsAddedNew => ref Unsafe.AsRef<bool>(&NativePtr->TabsAddedNew);
        public ref short TabsActiveCount => ref Unsafe.AsRef<short>(&NativePtr->TabsActiveCount);
        public ref short LastTabItemIdx => ref Unsafe.AsRef<short>(&NativePtr->LastTabItemIdx);
        public ref float ItemSpacingY => ref Unsafe.AsRef<float>(&NativePtr->ItemSpacingY);
        public ref Vector2 FramePadding => ref Unsafe.AsRef<Vector2>(&NativePtr->FramePadding);
        public ref Vector2 BackupCursorPos => ref Unsafe.AsRef<Vector2>(&NativePtr->BackupCursorPos);
        public ref ImGuiTextBuffer TabsNames => ref Unsafe.AsRef<ImGuiTextBuffer>(&NativePtr->TabsNames);      
    }
    public unsafe partial struct ImGuiTabItem
    {
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
    }
    public unsafe partial struct ImGuiTabItemPtr
    {
        public ImGuiTabItem* NativePtr { get; }
        public ImGuiTabItemPtr(ImGuiTabItem* nativePtr) => NativePtr = nativePtr;
        public ImGuiTabItemPtr(IntPtr nativePtr) => NativePtr = (ImGuiTabItem*)nativePtr;
        public static implicit operator ImGuiTabItemPtr(ImGuiTabItem* nativePtr) => new ImGuiTabItemPtr(nativePtr);
        public static implicit operator ImGuiTabItem*(ImGuiTabItemPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImGuiTabItemPtr(IntPtr nativePtr) => new ImGuiTabItemPtr(nativePtr);
        public ref uint ID => ref Unsafe.AsRef<uint>(&NativePtr->ID);
        public ref ImGuiTabItemFlags Flags => ref Unsafe.AsRef<ImGuiTabItemFlags>(&NativePtr->Flags);
        public ImGuiWindowPtr Window => new ImGuiWindowPtr(NativePtr->Window);
        public ref int LastFrameVisible => ref Unsafe.AsRef<int>(&NativePtr->LastFrameVisible);
        public ref int LastFrameSelected => ref Unsafe.AsRef<int>(&NativePtr->LastFrameSelected);
        public ref float Offset => ref Unsafe.AsRef<float>(&NativePtr->Offset);
        public ref float Width => ref Unsafe.AsRef<float>(&NativePtr->Width);
        public ref float ContentWidth => ref Unsafe.AsRef<float>(&NativePtr->ContentWidth);
        public ref short NameOffset => ref Unsafe.AsRef<short>(&NativePtr->NameOffset);
        public ref short BeginOrder => ref Unsafe.AsRef<short>(&NativePtr->BeginOrder);
        public ref short IndexDuringLayout => ref Unsafe.AsRef<short>(&NativePtr->IndexDuringLayout);
        public ref bool WantClose => ref Unsafe.AsRef<bool>(&NativePtr->WantClose);
    }
}