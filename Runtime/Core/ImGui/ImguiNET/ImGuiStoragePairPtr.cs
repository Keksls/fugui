using System;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Storage Pair Ptr data structure.
        /// </summary>
        public unsafe struct ImGuiStoragePairPtr
        {
            #region State
            public ImGuiStoragePair* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Storage Pair Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiStoragePairPtr(ImGuiStoragePair* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Storage Pair Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiStoragePairPtr(IntPtr nativePtr) => NativePtr = (ImGuiStoragePair*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiStoragePairPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiStoragePairPtr(ImGuiStoragePair* nativePtr) => new ImGuiStoragePairPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiStoragePair*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiStoragePair*(ImGuiStoragePairPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiStoragePairPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiStoragePairPtr(IntPtr nativePtr) => new ImGuiStoragePairPtr(nativePtr);
            #endregion
        }
}