using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Menu Columns Ptr data structure.
        /// </summary>
        public unsafe partial struct ImGuiMenuColumnsPtr
        {
            #region State
            public ImGuiMenuColumns* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Menu Columns Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiMenuColumnsPtr(ImGuiMenuColumns* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Menu Columns Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiMenuColumnsPtr(IntPtr nativePtr) => NativePtr = (ImGuiMenuColumns*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiMenuColumnsPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiMenuColumnsPtr(ImGuiMenuColumns* nativePtr) => new ImGuiMenuColumnsPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiMenuColumns*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiMenuColumns*(ImGuiMenuColumnsPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiMenuColumnsPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiMenuColumnsPtr(IntPtr nativePtr) => new ImGuiMenuColumnsPtr(nativePtr);
            #endregion

            #region State
            public ref float Spacing => ref Unsafe.AsRef<float>(&NativePtr->Spacing);
            public ref float Width => ref Unsafe.AsRef<float>(&NativePtr->Width);
            public ref float NextWidth => ref Unsafe.AsRef<float>(&NativePtr->NextWidth);
            public RangeAccessor<float> Pos => new RangeAccessor<float>(NativePtr->Pos, 3);
            public RangeAccessor<float> NextWidths => new RangeAccessor<float>(NativePtr->NextWidths, 3);
            #endregion
        }
}