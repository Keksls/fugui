using System;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Window Temp Data Ptr data structure.
        /// </summary>
        public unsafe partial struct ImGuiWindowTempDataPtr
        {
            #region State
            public ImGuiWindowTempData* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Window Temp Data Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiWindowTempDataPtr(ImGuiWindowTempData* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Window Temp Data Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiWindowTempDataPtr(IntPtr nativePtr) => NativePtr = (ImGuiWindowTempData*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiWindowTempDataPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiWindowTempDataPtr(ImGuiWindowTempData* nativePtr) => new ImGuiWindowTempDataPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiWindowTempData*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiWindowTempData*(ImGuiWindowTempDataPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiWindowTempDataPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiWindowTempDataPtr(IntPtr nativePtr) => new ImGuiWindowTempDataPtr(nativePtr);
            #endregion

            #region State
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
            #endregion
        }
}