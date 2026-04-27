using System;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Draw Data Builder Ptr data structure.
        /// </summary>
        public unsafe partial struct ImDrawDataBuilderPtr
        {
            #region State
            public ImDrawDataBuilder* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Draw Data Builder Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImDrawDataBuilderPtr(ImDrawDataBuilder* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Draw Data Builder Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImDrawDataBuilderPtr(IntPtr nativePtr) => NativePtr = (ImDrawDataBuilder*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImDrawDataBuilderPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImDrawDataBuilderPtr(ImDrawDataBuilder* nativePtr) => new ImDrawDataBuilderPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImDrawDataBuilder*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImDrawDataBuilder*(ImDrawDataBuilderPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImDrawDataBuilderPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImDrawDataBuilderPtr(IntPtr nativePtr) => new ImDrawDataBuilderPtr(nativePtr);
            #endregion

            #region State
            public RangeAccessor<ImVector> Layers => new RangeAccessor<ImVector>(&NativePtr->Layers_0, 2);
            #endregion
        }
}