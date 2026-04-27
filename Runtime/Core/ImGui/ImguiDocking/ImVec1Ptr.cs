using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Vec1 Ptr data structure.
        /// </summary>
        public unsafe partial struct ImVec1Ptr
        {
            #region State
            public ImVec1* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Vec1 Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImVec1Ptr(ImVec1* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Vec1 Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImVec1Ptr(IntPtr nativePtr) => NativePtr = (ImVec1*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImVec1Ptr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImVec1Ptr(ImVec1* nativePtr) => new ImVec1Ptr(nativePtr);
            /// <summary>
            /// Converts the value to ImVec1*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImVec1*(ImVec1Ptr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImVec1Ptr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImVec1Ptr(IntPtr nativePtr) => new ImVec1Ptr(nativePtr);
            #endregion

            #region State
            public ref float x => ref Unsafe.AsRef<float>(&NativePtr->x);
            #endregion
        }
}