using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ImGuiNET
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct ImGuiInputEventData
    {
        [FieldOffset(0)] public ImGuiInputEventMousePos MousePos;
        [FieldOffset(0)] public ImGuiInputEventMouseWheel MouseWheel;
        [FieldOffset(0)] public ImGuiInputEventMouseButton MouseButton;
        [FieldOffset(0)] public ImGuiInputEventKey Key;
        [FieldOffset(0)] public ImGuiInputEventText Text;
        [FieldOffset(0)] public ImGuiInputEventAppFocused AppFocused;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct ImGuiInputEvent
    {
        public ImGuiInputEventType Type;
        public ImGuiInputSource Source;
        public uint EventId;
        public ImGuiInputEventData Data;
        public byte AddedByTestEngine;
    }

    public unsafe partial struct ImGuiInputEventPtr
    {
        public ImGuiInputEvent* NativePtr { get; }

        public ImGuiInputEventPtr(ImGuiInputEvent* nativePtr) => NativePtr = nativePtr;

        public ImGuiInputEventPtr(IntPtr nativePtr) => NativePtr = (ImGuiInputEvent*)nativePtr;

        public static implicit operator ImGuiInputEventPtr(ImGuiInputEvent* nativePtr) => new ImGuiInputEventPtr(nativePtr);

        public static implicit operator ImGuiInputEvent*(ImGuiInputEventPtr wrappedPtr) => wrappedPtr.NativePtr;

        public static implicit operator ImGuiInputEventPtr(IntPtr nativePtr) => new ImGuiInputEventPtr(nativePtr);

        public ref ImGuiInputEventType Type => ref Unsafe.AsRef<ImGuiInputEventType>(&NativePtr->Type);
        public ref ImGuiInputSource Source => ref Unsafe.AsRef<ImGuiInputSource>(&NativePtr->Source);
        public ref uint EventId => ref Unsafe.AsRef<uint>(&NativePtr->EventId);
        public ref ImGuiInputEventData Data => ref Unsafe.AsRef<ImGuiInputEventData>(&NativePtr->Data);
        public ref byte AddedByTestEngine => ref Unsafe.AsRef<byte>(&NativePtr->AddedByTestEngine);

    }
}