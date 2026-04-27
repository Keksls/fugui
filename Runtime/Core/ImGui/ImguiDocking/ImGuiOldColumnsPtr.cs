using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Old Columns Ptr data structure.
        /// </summary>
        public unsafe partial struct ImGuiOldColumnsPtr
        {
            #region State
            public ImGuiOldColumns* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Old Columns Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiOldColumnsPtr(ImGuiOldColumns* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Old Columns Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiOldColumnsPtr(IntPtr nativePtr) => NativePtr = (ImGuiOldColumns*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiOldColumnsPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiOldColumnsPtr(ImGuiOldColumns* nativePtr) => new ImGuiOldColumnsPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiOldColumns*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiOldColumns*(ImGuiOldColumnsPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiOldColumnsPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiOldColumnsPtr(IntPtr nativePtr) => new ImGuiOldColumnsPtr(nativePtr);
            #endregion

            #region State
            public ref uint ID => ref Unsafe.AsRef<uint>(&NativePtr->ID);
            public ref ImGuiOldColumnFlags Flags => ref Unsafe.AsRef<ImGuiOldColumnFlags>(&NativePtr->Flags);
            public ref bool IsFirstFrame => ref Unsafe.AsRef<bool>(&NativePtr->IsFirstFrame);
            public ref bool IsBeingResized => ref Unsafe.AsRef<bool>(&NativePtr->IsBeingResized);
            public ref int Current => ref Unsafe.AsRef<int>(&NativePtr->Current);
            public ref int Count => ref Unsafe.AsRef<int>(&NativePtr->Count);
            public ref float OffMinX => ref Unsafe.AsRef<float>(&NativePtr->OffMinX);
            public ref float OffMaxX => ref Unsafe.AsRef<float>(&NativePtr->OffMaxX);
            public ref float LineMinY => ref Unsafe.AsRef<float>(&NativePtr->LineMinY);
            public ref float LineMaxY => ref Unsafe.AsRef<float>(&NativePtr->LineMaxY);
            public ref float HostCursorPosY => ref Unsafe.AsRef<float>(&NativePtr->HostCursorPosY);
            public ref float HostCursorMaxPosX => ref Unsafe.AsRef<float>(&NativePtr->HostCursorMaxPosX);
            public ref ImRect HostInitialClipRect => ref Unsafe.AsRef<ImRect>(&NativePtr->HostInitialClipRect);
            public ref ImRect HostBackupClipRect => ref Unsafe.AsRef<ImRect>(&NativePtr->HostBackupClipRect);
            public ref ImRect HostBackupParentWorkRect => ref Unsafe.AsRef<ImRect>(&NativePtr->HostBackupParentWorkRect);
            public ref ImDrawListSplitter Splitter => ref Unsafe.AsRef<ImDrawListSplitter>(&NativePtr->Splitter);
            #endregion
        }
}