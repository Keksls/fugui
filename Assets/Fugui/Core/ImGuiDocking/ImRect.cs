using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ImGuiNET
{
    public unsafe partial struct ImRect
    {
        public Vector2 Min;
        public Vector2 Max;
    }
    public unsafe partial struct ImRectPtr
    {
        public ImRect* NativePtr { get; }
        public ImRectPtr(ImRect* nativePtr) => NativePtr = nativePtr;
        public ImRectPtr(IntPtr nativePtr) => NativePtr = (ImRect*)nativePtr;
        public static implicit operator ImRectPtr(ImRect* nativePtr) => new ImRectPtr(nativePtr);
        public static implicit operator ImRect*(ImRectPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImRectPtr(IntPtr nativePtr) => new ImRectPtr(nativePtr);
        public ref Vector2 Min => ref Unsafe.AsRef<Vector2>(&NativePtr->Min);
        public ref Vector2 Max => ref Unsafe.AsRef<Vector2>(&NativePtr->Max);
    }
}