using System;

namespace ImGuiNET
{
    public unsafe partial struct ImDrawDataBuilder
    {
        public ImVector Layers_0;
        public ImVector Layers_1;
    }
    public unsafe partial struct ImDrawDataBuilderPtr
    {
        public ImDrawDataBuilder* NativePtr { get; }
        public ImDrawDataBuilderPtr(ImDrawDataBuilder* nativePtr) => NativePtr = nativePtr;
        public ImDrawDataBuilderPtr(IntPtr nativePtr) => NativePtr = (ImDrawDataBuilder*)nativePtr;
        public static implicit operator ImDrawDataBuilderPtr(ImDrawDataBuilder* nativePtr) => new ImDrawDataBuilderPtr(nativePtr);
        public static implicit operator ImDrawDataBuilder*(ImDrawDataBuilderPtr wrappedPtr) => wrappedPtr.NativePtr;
        public static implicit operator ImDrawDataBuilderPtr(IntPtr nativePtr) => new ImDrawDataBuilderPtr(nativePtr);
        public RangeAccessor<ImVector> Layers => new RangeAccessor<ImVector>(&NativePtr->Layers_0, 2);
    }
}