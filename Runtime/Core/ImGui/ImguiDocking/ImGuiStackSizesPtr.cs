using System;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Stack Sizes Ptr data structure.
        /// </summary>
        public unsafe partial struct ImGuiStackSizesPtr
        {
            #region State
            public ImGuiStackSizes* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Stack Sizes Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiStackSizesPtr(ImGuiStackSizes* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Stack Sizes Ptr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiStackSizesPtr(IntPtr nativePtr) => NativePtr = (ImGuiStackSizes*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiStackSizesPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiStackSizesPtr(ImGuiStackSizes* nativePtr) => new ImGuiStackSizesPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiStackSizes*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiStackSizes*(ImGuiStackSizesPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiStackSizesPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiStackSizesPtr(IntPtr nativePtr) => new ImGuiStackSizesPtr(nativePtr);
            #endregion

            #region State
            public ref short SizeOfIDStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfIDStack);
            public ref short SizeOfColorStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfColorStack);
            public ref short SizeOfStyleVarStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfStyleVarStack);
            public ref short SizeOfFontStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfFontStack);
            public ref short SizeOfFocusScopeStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfFocusScopeStack);
            public ref short SizeOfGroupStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfGroupStack);
            public ref short SizeOfBeginPopupStack => ref Unsafe.AsRef<short>(&NativePtr->SizeOfBeginPopupStack);
            #endregion
        }
}