using System;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Tab Bar Ptr data structure.
        /// </summary>
        public unsafe partial struct ImGuiTabBarPtr
        {
            #region State
            public ImGuiTabBar* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Tab Bar Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiTabBarPtr(ImGuiTabBar* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Tab Bar Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiTabBarPtr(IntPtr nativePtr) => NativePtr = (ImGuiTabBar*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiTabBarPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiTabBarPtr(ImGuiTabBar* nativePtr) => new ImGuiTabBarPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiTabBar*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiTabBar*(ImGuiTabBarPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiTabBarPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiTabBarPtr(IntPtr nativePtr) => new ImGuiTabBarPtr(nativePtr);
            #endregion

            #region State
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
            #endregion
        }
}