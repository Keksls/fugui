using System;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
        /// <summary>
        /// Represents the Im Gui Viewport PPtr data structure.
        /// </summary>
        public unsafe partial struct ImGuiViewportPPtr
        {
            #region State
            public ImGuiViewportP* NativePtr { get; }
            #endregion

            #region Constructors
            /// <summary>
            /// Initializes a new instance of the Im Gui Viewport PPtr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiViewportPPtr(ImGuiViewportP* nativePtr) => NativePtr = nativePtr;
            /// <summary>
            /// Initializes a new instance of the Im Gui Viewport PPtr class.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            public ImGuiViewportPPtr(IntPtr nativePtr) => NativePtr = (ImGuiViewportP*)nativePtr;
            #endregion

            #region Members
            /// <summary>
            /// Converts the value to ImGuiViewportPPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiViewportPPtr(ImGuiViewportP* nativePtr) => new ImGuiViewportPPtr(nativePtr);
            /// <summary>
            /// Converts the value to ImGuiViewportP*.
            /// </summary>
            /// <param name="wrappedPtr">The wrapped Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiViewportP*(ImGuiViewportPPtr wrappedPtr) => wrappedPtr.NativePtr;
            /// <summary>
            /// Converts the value to ImGuiViewportPPtr.
            /// </summary>
            /// <param name="nativePtr">The native Ptr value.</param>
            /// <returns>The result of the operation.</returns>
            public static implicit operator ImGuiViewportPPtr(IntPtr nativePtr) => new ImGuiViewportPPtr(nativePtr);
            #endregion

            #region State
            public ref ImGuiViewport _ImGuiViewport => ref Unsafe.AsRef<ImGuiViewport>(&NativePtr->_ImGuiViewport);
            public ref int Idx => ref Unsafe.AsRef<int>(&NativePtr->Idx);
            public ref int LastFrameActive => ref Unsafe.AsRef<int>(&NativePtr->LastFrameActive);
            public ref int LastFrontMostStampCount => ref Unsafe.AsRef<int>(&NativePtr->LastFrontMostStampCount);
            public ref uint LastNameHash => ref Unsafe.AsRef<uint>(&NativePtr->LastNameHash);
            public ref Vector2 LastPos => ref Unsafe.AsRef<Vector2>(&NativePtr->LastPos);
            public ref float Alpha => ref Unsafe.AsRef<float>(&NativePtr->Alpha);
            public ref float LastAlpha => ref Unsafe.AsRef<float>(&NativePtr->LastAlpha);
            public ref short PlatformMonitor => ref Unsafe.AsRef<short>(&NativePtr->PlatformMonitor);
            public ref bool PlatformWindowCreated => ref Unsafe.AsRef<bool>(&NativePtr->PlatformWindowCreated);
            public ImGuiWindowPtr Window => new ImGuiWindowPtr(NativePtr->Window);
            public RangeAccessor<int> DrawListsLastFrame => new RangeAccessor<int>(NativePtr->DrawListsLastFrame, 2);
            public RangeAccessor<ImDrawList> DrawLists => new RangeAccessor<ImDrawList>(&NativePtr->DrawLists_0, 2);
            public ref ImDrawData DrawDataP => ref Unsafe.AsRef<ImDrawData>(&NativePtr->DrawDataP);
            public ref ImDrawDataBuilder DrawDataBuilder => ref Unsafe.AsRef<ImDrawDataBuilder>(&NativePtr->DrawDataBuilder);
            public ref Vector2 LastPlatformPos => ref Unsafe.AsRef<Vector2>(&NativePtr->LastPlatformPos);
            public ref Vector2 LastPlatformSize => ref Unsafe.AsRef<Vector2>(&NativePtr->LastPlatformSize);
            public ref Vector2 LastRendererSize => ref Unsafe.AsRef<Vector2>(&NativePtr->LastRendererSize);
            public ref Vector2 WorkOffsetMin => ref Unsafe.AsRef<Vector2>(&NativePtr->WorkOffsetMin);
            public ref Vector2 WorkOffsetMax => ref Unsafe.AsRef<Vector2>(&NativePtr->WorkOffsetMax);
            public ref Vector2 CurrWorkOffsetMin => ref Unsafe.AsRef<Vector2>(&NativePtr->CurrWorkOffsetMin);
            public ref Vector2 CurrWorkOffsetMax => ref Unsafe.AsRef<Vector2>(&NativePtr->CurrWorkOffsetMax);       
            #endregion
        }
}