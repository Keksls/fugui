using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Vec2ih Ptr data structure.
        /// </summary>
        public unsafe partial struct ImVec2ihPtr
        {
            #region State
            public ImVec2ih* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Vec2ih Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImVec2ihPtr(ImVec2ih* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Vec2ih Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImVec2ihPtr(IntPtr nativePtr) => NativePtr = (ImVec2ih*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImVec2ihPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImVec2ihPtr(ImVec2ih* nativePtr) => new ImVec2ihPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImVec2ih*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImVec2ih*(ImVec2ihPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImVec2ihPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImVec2ihPtr(IntPtr nativePtr) => new ImVec2ihPtr(nativePtr);
            #endregion

            #region State
            public ref short x => ref Unsafe.AsRef<short>(&NativePtr->x);
            public ref short y => ref Unsafe.AsRef<short>(&NativePtr->y);
            #endregion
        }
}