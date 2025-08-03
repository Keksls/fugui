using System;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
    public unsafe partial struct ImGuiWindowTempData
    {
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
    }
    public unsafe partial struct ImGuiStackSizes
    {
        public short SizeOfIDStack;
        public short SizeOfColorStack;
        public short SizeOfStyleVarStack;
        public short SizeOfFontStack;
        public short SizeOfFocusScopeStack;
        public short SizeOfGroupStack;
        public short SizeOfBeginPopupStack;
    }
    public unsafe partial struct ImGuiStackSizesPtr
    {
        public ImGuiStackSizes* NativePtr { get; }
        public ImGuiStackSizesPtr(ImGuiStackSizes* nativePtr) => NativePtr = nativePtr;
        public ImGuiStackSizesPtr(IntPtr nativePtr) => NativePtr = (ImGuiStackSizes*)nativePtr;
        public static implicit operator ImGuiStackSizesPtr(ImGuiStackSizes* nativePtr) => new ImGuiStackSizesPtr(nativePtr);
        public static implicit operator ImGuiStackSizes*(ImGuiStackSizesPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImGuiStackSizesPtr(IntPtr nativePtr) => new ImGuiStackSizesPtr(nativePtr);
        public ref short SizeOfIDStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfIDStack);
        public ref short SizeOfColorStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfColorStack);
        public ref short SizeOfStyleVarStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfStyleVarStack);
        public ref short SizeOfFontStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfFontStack);
        public ref short SizeOfFocusScopeStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfFocusScopeStack);
        public ref short SizeOfGroupStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfGroupStack);
        public ref short SizeOfBeginPopupStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfBeginPopupStack);
    }
    public enum ImGuiLayoutType
    {
        Horizontal = 0,
        Vertical = 1,
    }
    public unsafe partial struct ImGuiMenuColumns
    {
        public float Spacing;
        public float Width;
        public float NextWidth;
        public fixed float Pos[3];
        public fixed float NextWidths[3];
    }
    public unsafe partial struct ImGuiMenuColumnsPtr
    {
        public ImGuiMenuColumns* NativePtr { get; }
        public ImGuiMenuColumnsPtr(ImGuiMenuColumns* nativePtr) => NativePtr = nativePtr;
        public ImGuiMenuColumnsPtr(IntPtr nativePtr) => NativePtr = (ImGuiMenuColumns*)nativePtr;
        public static implicit operator ImGuiMenuColumnsPtr(ImGuiMenuColumns* nativePtr) => new ImGuiMenuColumnsPtr(nativePtr);
        public static implicit operator ImGuiMenuColumns*(ImGuiMenuColumnsPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImGuiMenuColumnsPtr(IntPtr nativePtr) => new ImGuiMenuColumnsPtr(nativePtr);
        public ref float Spacing => ref Unsafe.AsRef<float>(&NativePtr->Spacing);
        public ref float Width => ref Unsafe.AsRef<float>(&NativePtr->Width);
        public ref float NextWidth => ref Unsafe.AsRef<float>(&NativePtr->NextWidth);
        public RangeAccessor<float> Pos => new RangeAccessor<float>(NativePtr->Pos, 3);
        public RangeAccessor<float> NextWidths => new RangeAccessor<float>(NativePtr->NextWidths, 3);
    }
    public enum ImGuiNavLayer
    {
        _Main = 0,
        _Menu = 1,
        _COUNT = 2,
    }

    public unsafe partial struct ImGuiWindowTempDataPtr
    {
        public ImGuiWindowTempData* NativePtr { get; }
        public ImGuiWindowTempDataPtr(ImGuiWindowTempData* nativePtr) => NativePtr = nativePtr;
        public ImGuiWindowTempDataPtr(IntPtr nativePtr) => NativePtr = (ImGuiWindowTempData*)nativePtr;
        public static implicit operator ImGuiWindowTempDataPtr(ImGuiWindowTempData* nativePtr) => new ImGuiWindowTempDataPtr(nativePtr);
        public static implicit operator ImGuiWindowTempData*(ImGuiWindowTempDataPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImGuiWindowTempDataPtr(IntPtr nativePtr) => new ImGuiWindowTempDataPtr(nativePtr);
        public ref Vector2 CursorPos => ref Unsafe.AsRef<Vector2>(&NativePtr->CursorPos);
        public ref Vector2 CursorPosPrevLine => ref Unsafe.AsRef<Vector2>(&NativePtr->CursorPosPrevLine);
        public ref Vector2 CursorStartPos => ref Unsafe.AsRef<Vector2>(&NativePtr->CursorStartPos);
        public ref Vector2 CursorMaxPos => ref Unsafe.AsRef<Vector2>(&NativePtr->CursorMaxPos);
        public ref Vector2 IdealMaxPos => ref Unsafe.AsRef<Vector2>(&NativePtr->IdealMaxPos);
        public ref Vector2 CurrLineSize => ref Unsafe.AsRef<Vector2>(&NativePtr->CurrLineSize);
        public ref Vector2 PrevLineSize => ref Unsafe.AsRef<Vector2>(&NativePtr->PrevLineSize);
        public ref float CurrLineTextBaseOffset => ref Unsafe.AsRef<float>(&NativePtr->CurrLineTextBaseOffset);
        public ref float PrevLineTextBaseOffset => ref Unsafe.AsRef<float>(&NativePtr->PrevLineTextBaseOffset);
        public ref ImVec1 Indent => ref Unsafe.AsRef<ImVec1>(&NativePtr->Indent);
        public ref ImVec1 ColumnsOffset => ref Unsafe.AsRef<ImVec1>(&NativePtr->ColumnsOffset);
        public ref ImVec1 GroupOffset => ref Unsafe.AsRef<ImVec1>(&NativePtr->GroupOffset);
        public ref uint LastItemId => ref Unsafe.AsRef<uint>(&NativePtr->LastItemId);
        public ref ImGuiItemStatusFlags LastItemStatusFlags => ref Unsafe.AsRef<ImGuiItemStatusFlags>(&NativePtr->LastItemStatusFlags);
        public ref ImRect LastItemRect => ref Unsafe.AsRef<ImRect>(&NativePtr->LastItemRect);
        public ref ImRect LastItemDisplayRect => ref Unsafe.AsRef<ImRect>(&NativePtr->LastItemDisplayRect);
        public ref ImGuiNavLayer NavLayerCurrent => ref Unsafe.AsRef<ImGuiNavLayer>(&NativePtr->NavLayerCurrent);
        public ref int NavLayerActiveMask => ref Unsafe.AsRef<int>(&NativePtr->NavLayerActiveMask);
        public ref int NavLayerActiveMaskNext => ref Unsafe.AsRef<int>(&NativePtr->NavLayerActiveMaskNext);
        public ref uint NavFocusScopeIdCurrent => ref Unsafe.AsRef<uint>(&NativePtr->NavFocusScopeIdCurrent);
        public ref bool NavHideHighlightOneFrame => ref Unsafe.AsRef<bool>(&NativePtr->NavHideHighlightOneFrame);
        public ref bool NavHasScroll => ref Unsafe.AsRef<bool>(&NativePtr->NavHasScroll);
        public ref bool MenuBarAppending => ref Unsafe.AsRef<bool>(&NativePtr->MenuBarAppending);
        public ref Vector2 MenuBarOffset => ref Unsafe.AsRef<Vector2>(&NativePtr->MenuBarOffset);
        public ref ImGuiMenuColumns MenuColumns => ref Unsafe.AsRef<ImGuiMenuColumns>(&NativePtr->MenuColumns);
        public ref int TreeDepth => ref Unsafe.AsRef<int>(&NativePtr->TreeDepth);
        public ref uint TreeJumpToParentOnPopMask => ref Unsafe.AsRef<uint>(&NativePtr->TreeJumpToParentOnPopMask);
        public ImVector<ImGuiWindowPtr> ChildWindows => new ImVector<ImGuiWindowPtr>(NativePtr->ChildWindows);
        public ImGuiStoragePtr StateStorage => new ImGuiStoragePtr(NativePtr->StateStorage);
        public ImGuiOldColumnsPtr CurrentColumns => new ImGuiOldColumnsPtr(NativePtr->CurrentColumns);
        public ref int CurrentTableIdx => ref Unsafe.AsRef<int>(&NativePtr->CurrentTableIdx);
        public ref ImGuiLayoutType LayoutType => ref Unsafe.AsRef<ImGuiLayoutType>(&NativePtr->LayoutType);
        public ref ImGuiLayoutType ParentLayoutType => ref Unsafe.AsRef<ImGuiLayoutType>(&NativePtr->ParentLayoutType);
        public ref int FocusCounterRegular => ref Unsafe.AsRef<int>(&NativePtr->FocusCounterRegular);
        public ref int FocusCounterTabStop => ref Unsafe.AsRef<int>(&NativePtr->FocusCounterTabStop);
        public ref ImGuiItemFlags ItemFlags => ref Unsafe.AsRef<ImGuiItemFlags>(&NativePtr->ItemFlags);
        public ref float ItemWidth => ref Unsafe.AsRef<float>(&NativePtr->ItemWidth);
        public ref float TextWrapPos => ref Unsafe.AsRef<float>(&NativePtr->TextWrapPos);
        public ImVector<float> ItemWidthStack => new ImVector<float>(NativePtr->ItemWidthStack);
        public ImVector<float> TextWrapPosStack => new ImVector<float>(NativePtr->TextWrapPosStack);
        public ref ImGuiStackSizes StackSizesOnBegin => ref Unsafe.AsRef<ImGuiStackSizes>(&NativePtr->StackSizesOnBegin);
    }

    public unsafe partial struct ImVec1
    {
        public float x;
    }
    public unsafe partial struct ImVec1Ptr
    {
        public ImVec1* NativePtr { get; }
        public ImVec1Ptr(ImVec1* nativePtr) => NativePtr = nativePtr;
        public ImVec1Ptr(IntPtr nativePtr) => NativePtr = (ImVec1*)nativePtr;
        public static implicit operator ImVec1Ptr(ImVec1* nativePtr) => new ImVec1Ptr(nativePtr);
        public static implicit operator ImVec1*(ImVec1Ptr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImVec1Ptr(IntPtr nativePtr) => new ImVec1Ptr(nativePtr);
        public ref float x => ref Unsafe.AsRef<float>(&NativePtr->x);
    }
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