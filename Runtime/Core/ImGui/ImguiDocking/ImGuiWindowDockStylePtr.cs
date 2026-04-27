using System;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Window Dock Style Ptr data structure.
        /// </summary>
        public unsafe partial struct ImGuiWindowDockStylePtr
        {
            #region State
            public ImGuiWindowDockStyle* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Window Dock Style Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiWindowDockStylePtr(ImGuiWindowDockStyle* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Window Dock Style Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiWindowDockStylePtr(IntPtr nativePtr) => NativePtr = (ImGuiWindowDockStyle*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiWindowDockStylePtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiWindowDockStylePtr(ImGuiWindowDockStyle* nativePtr) => new ImGuiWindowDockStylePtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiWindowDockStyle*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiWindowDockStyle*(ImGuiWindowDockStylePtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiWindowDockStylePtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiWindowDockStylePtr(IntPtr nativePtr) => new ImGuiWindowDockStylePtr(nativePtr);
            #endregion

            #region State
            public RangeAccessor<uint> Colors => new RangeAccessor<uint>(NativePtr->Colors, 6);
            #endregion
        }
}