using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ImGuiNET
{
    public unsafe partial struct ImRect
    {
        public Vector2 Min;
        public Vector2 Max;

        public Vector2 GetBL()
        {
            return new Vector2(Min.x, Max.y);
        }

        public ImRect(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }
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