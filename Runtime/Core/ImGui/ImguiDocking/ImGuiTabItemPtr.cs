using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Tab Item Ptr data structure.
        /// </summary>
        public unsafe partial struct ImGuiTabItemPtr
        {
            #region State
            public ImGuiTabItem* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Tab Item Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiTabItemPtr(ImGuiTabItem* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Tab Item Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiTabItemPtr(IntPtr nativePtr) => NativePtr = (ImGuiTabItem*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiTabItemPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiTabItemPtr(ImGuiTabItem* nativePtr) => new ImGuiTabItemPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiTabItem*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiTabItem*(ImGuiTabItemPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiTabItemPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiTabItemPtr(IntPtr nativePtr) => new ImGuiTabItemPtr(nativePtr);
            #endregion

            #region State
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
            #endregion
        }
}