using System;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Dock Node Ptr data structure.
        /// </summary>
        public unsafe partial struct ImGuiDockNodePtr
        {
            #region State
            public ImGuiDockNode* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Dock Node Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiDockNodePtr(ImGuiDockNode* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Dock Node Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiDockNodePtr(IntPtr nativePtr) => NativePtr = (ImGuiDockNode*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiDockNodePtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiDockNodePtr(ImGuiDockNode* nativePtr) => new ImGuiDockNodePtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiDockNode*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiDockNode*(ImGuiDockNodePtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiDockNodePtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiDockNodePtr(IntPtr nativePtr) => new ImGuiDockNodePtr(nativePtr);
            #endregion

            #region State
            public ref uint ID => ref Unsafe.AsRef<uint>(&NativePtr->ID);
            public ref ImGuiDockNodeFlags SharedFlags => ref Unsafe.AsRef<ImGuiDockNodeFlags>(&NativePtr->SharedFlags);
            public ref ImGuiDockNodeFlags LocalFlags => ref Unsafe.AsRef<ImGuiDockNodeFlags>(&NativePtr->LocalFlags);
            public ref ImGuiDockNodeState State => ref Unsafe.AsRef<ImGuiDockNodeState>(&NativePtr->State);
            public ImGuiDockNodePtr ParentNode => new ImGuiDockNodePtr(NativePtr->ParentNode);
            public RangeAccessor<ImGuiDockNode> ChildNodes => new RangeAccessor<ImGuiDockNode>(&NativePtr->ChildNodes_0, 2);
            public ImVector<ImGuiWindowPtr> Windows => new ImVector<ImGuiWindowPtr>(NativePtr->Windows);
            public ImGuiTabBarPtr TabBar => new ImGuiTabBarPtr(NativePtr->TabBar);
            public ref Vector2 Pos => ref Unsafe.AsRef<Vector2>(&NativePtr->Pos);
            public ref Vector2 Size => ref Unsafe.AsRef<Vector2>(&NativePtr->Size);
            public ref Vector2 SizeRef => ref Unsafe.AsRef<Vector2>(&NativePtr->SizeRef);
            public ref ImGuiAxis SplitAxis => ref Unsafe.AsRef<ImGuiAxis>(&NativePtr->SplitAxis);
            public ref ImGuiWindowClass WindowClass => ref Unsafe.AsRef<ImGuiWindowClass>(&NativePtr->WindowClass);
            public ImGuiWindowPtr HostWindow => new ImGuiWindowPtr(NativePtr->HostWindow);
            public ImGuiWindowPtr VisibleWindow => new ImGuiWindowPtr(NativePtr->VisibleWindow);
            public ImGuiDockNodePtr CentralNode => new ImGuiDockNodePtr(NativePtr->CentralNode);
            public ImGuiDockNodePtr OnlyNodeWithWindows => new ImGuiDockNodePtr(NativePtr->OnlyNodeWithWindows);
            public ref int LastFrameAlive => ref Unsafe.AsRef<int>(&NativePtr->LastFrameAlive);
            public ref int LastFrameActive => ref Unsafe.AsRef<int>(&NativePtr->LastFrameActive);
            public ref int LastFrameFocused => ref Unsafe.AsRef<int>(&NativePtr->LastFrameFocused);
            public ref uint LastFocusedNodeId => ref Unsafe.AsRef<uint>(&NativePtr->LastFocusedNodeId);
            public ref uint SelectedTabId => ref Unsafe.AsRef<uint>(&NativePtr->SelectedTabId);
            public ref uint WantCloseTabId => ref Unsafe.AsRef<uint>(&NativePtr->WantCloseTabId);
            public ref ImGuiDataAuthority AuthorityForPos => ref Unsafe.AsRef<ImGuiDataAuthority>(&NativePtr->AuthorityForPos);
            public ref ImGuiDataAuthority AuthorityForSize => ref Unsafe.AsRef<ImGuiDataAuthority>(&NativePtr->AuthorityForSize);
            public ref ImGuiDataAuthority AuthorityForViewport => ref Unsafe.AsRef<ImGuiDataAuthority>(&NativePtr->AuthorityForViewport);
            public ref bool IsVisible => ref Unsafe.AsRef<bool>(&NativePtr->IsVisible);
            public ref bool IsFocused => ref Unsafe.AsRef<bool>(&NativePtr->IsFocused);
            public ref bool HasCloseButton => ref Unsafe.AsRef<bool>(&NativePtr->HasCloseButton);
            public ref bool HasWindowMenuButton => ref Unsafe.AsRef<bool>(&NativePtr->HasWindowMenuButton);
            public ref bool EnableCloseButton => ref Unsafe.AsRef<bool>(&NativePtr->EnableCloseButton);
            public ref bool WantCloseAll => ref Unsafe.AsRef<bool>(&NativePtr->WantCloseAll);
            public ref bool WantLockSizeOnce => ref Unsafe.AsRef<bool>(&NativePtr->WantLockSizeOnce);
            public ref bool WantMouseMove => ref Unsafe.AsRef<bool>(&NativePtr->WantMouseMove);
            public ref bool WantHiddenTabBarUpdate => ref Unsafe.AsRef<bool>(&NativePtr->WantHiddenTabBarUpdate);
            public ref bool WantHiddenTabBarToggle => ref Unsafe.AsRef<bool>(&NativePtr->WantHiddenTabBarToggle);
            public ref bool MarkedForPosSizeWrite => ref Unsafe.AsRef<bool>(&NativePtr->MarkedForPosSizeWrite);
            #endregion

            #region Methods
            /// <summary>
            /// Runs the destroy workflow.
            /// </summary>
            public void Destroy()
            {
                NativeDocking.ImGuiDockNode_destroy((ImGuiDockNode*)(NativePtr));
            }
            /// <summary>
            /// Gets the merged flags.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public ImGuiDockNodeFlags GetMergedFlags()
            {
                ImGuiDockNodeFlags ret = NativeDocking.ImGuiDockNode_GetMergedFlags((ImGuiDockNode*)(NativePtr));
                return ret;
            }
            /// <summary>
            /// Returns the is central node result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public bool IsCentralNode()
            {
                byte ret = NativeDocking.ImGuiDockNode_IsCentralNode((ImGuiDockNode*)(NativePtr));
                return ret != 0;
            }
            /// <summary>
            /// Returns the is dock space result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public bool IsDockSpace()
            {
                byte ret = NativeDocking.ImGuiDockNode_IsDockSpace((ImGuiDockNode*)(NativePtr));
                return ret != 0;
            }
            /// <summary>
            /// Returns the is empty result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public bool IsEmpty()
            {
                byte ret = NativeDocking.ImGuiDockNode_IsEmpty((ImGuiDockNode*)(NativePtr));
                return ret != 0;
            }
            /// <summary>
            /// Returns the is floating node result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public bool IsFloatingNode()
            {
                return NativeDocking.ImGuiDockNode_IsFloatingNode((ImGuiDockNode*)(NativePtr));
            }
            /// <summary>
            /// Returns the is hidden tab bar result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public bool IsHiddenTabBar()
            {
                byte ret = NativeDocking.ImGuiDockNode_IsHiddenTabBar((ImGuiDockNode*)(NativePtr));
                return ret != 0;
            }
            /// <summary>
            /// Returns the is leaf node result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public bool IsLeafNode()
            {
                byte ret = NativeDocking.ImGuiDockNode_IsLeafNode((ImGuiDockNode*)(NativePtr));
                return ret != 0;
            }
            /// <summary>
            /// Returns the is no tab bar result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public bool IsNoTabBar()
            {
                byte ret = NativeDocking.ImGuiDockNode_IsNoTabBar((ImGuiDockNode*)(NativePtr));
                return ret != 0;
            }
            /// <summary>
            /// Returns the is root node result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public bool IsRootNode()
            {
                byte ret = NativeDocking.ImGuiDockNode_IsRootNode((ImGuiDockNode*)(NativePtr));
                return ret != 0;
            }
            /// <summary>
            /// Returns the is split node result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public bool IsSplitNode()
            {
                byte ret = NativeDocking.ImGuiDockNode_IsSplitNode((ImGuiDockNode*)(NativePtr));
                return ret != 0;
            }
            /// <summary>
            /// Returns the rect result.
            /// </summary>
            /// <returns>The result of the operation.</returns>
            public ImRect Rect()
            {
                ImRect __retval;
                NativeDocking.ImGuiDockNode_Rect(&__retval, (ImGuiDockNode*)(NativePtr));
                return __retval;
            }
            #endregion
        }
}