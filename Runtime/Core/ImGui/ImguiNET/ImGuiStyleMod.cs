using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ImGuiNET
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe partial struct ImGuiStyleModData
    {
        [FieldOffset(0)] public fixed int BackupInt[2];
        [FieldOffset(0)] public fixed float BackupFloat[2];
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe partial struct ImGuiStyleMod
    {
        public ImGuiStyleVar VarIdx;
        public ImGuiStyleModData Data;
    }

    public unsafe partial struct ImGuiStyleModPtr
    {
        public ImGuiStyleMod* NativePtr { get; }

        public ImGuiStyleModPtr(ImGuiStyleMod* nativePtr) => NativePtr = nativePtr;

        public ImGuiStyleModPtr(IntPtr nativePtr) => NativePtr = (ImGuiStyleMod*)nativePtr;

        public static implicit operator ImGuiStyleModPtr(ImGuiStyleMod* nativePtr) => new ImGuiStyleModPtr(nativePtr);

        public static implicit operator ImGuiStyleMod*(ImGuiStyleModPtr wrappedPtr) => wrappedPtr.NativePtr;

        public static implicit operator ImGuiStyleModPtr(IntPtr nativePtr) => new ImGuiStyleModPtr(nativePtr);

        public ref ImGuiStyleVar VarIdx => ref Unsafe.AsRef<ImGuiStyleVar>(&NativePtr->VarIdx);
        public ref ImGuiStyleModData Data => ref Unsafe.AsRef<ImGuiStyleModData>(&NativePtr->Data);

    }
}