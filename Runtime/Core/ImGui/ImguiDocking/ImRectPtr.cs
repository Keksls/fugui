using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Rect Ptr data structure.
        /// </summary>
        public unsafe partial struct ImRectPtr
        {
            #region State
            public ImRect* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Rect Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImRectPtr(ImRect* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Rect Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImRectPtr(IntPtr nativePtr) => NativePtr = (ImRect*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImRectPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImRectPtr(ImRect* nativePtr) => new ImRectPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImRect*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImRect*(ImRectPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImRectPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImRectPtr(IntPtr nativePtr) => new ImRectPtr(nativePtr);
            #endregion

            #region State
            public ref Vector2 Min => ref Unsafe.AsRef<Vector2>(&NativePtr->Min);
            public ref Vector2 Max => ref Unsafe.AsRef<Vector2>(&NativePtr->Max);
            #endregion
        }
}