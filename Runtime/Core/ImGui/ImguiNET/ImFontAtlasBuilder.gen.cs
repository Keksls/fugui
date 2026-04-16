using System;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Text;

namespace ImGuiNET
{
    public unsafe partial struct ImFontAtlasBuilder
    {
    }
    public unsafe partial struct ImFontAtlasBuilderPtr
    {
        public ImFontAtlasBuilder* NativePtr { get; }
        public ImFontAtlasBuilderPtr(ImFontAtlasBuilder* nativePtr) => NativePtr = nativePtr;
        public ImFontAtlasBuilderPtr(IntPtr nativePtr) => NativePtr = (ImFontAtlasBuilder*)nativePtr;
        public static implicit operator ImFontAtlasBuilderPtr(ImFontAtlasBuilder* nativePtr) => new ImFontAtlasBuilderPtr(nativePtr);
        public static implicit operator ImFontAtlasBuilder*(ImFontAtlasBuilderPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImFontAtlasBuilderPtr(IntPtr nativePtr) => new ImFontAtlasBuilderPtr(nativePtr);
    }
}
