using System.Runtime.CompilerServices;

namespace Fu.Core
{
    public class FuSystemWindowsNames
    {
        protected static FuWindowName _None = new FuWindowName(ushort.MaxValue - 1, "None", false, -1);
        public static FuWindowName None { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _None; }
        protected static FuWindowName _WindowsDefinitionManager = new FuWindowName(ushort.MaxValue - 2, "Windows Definition Manager", true, -1);
        public static FuWindowName WindowsDefinitionManager { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _WindowsDefinitionManager; }
        protected static FuWindowName _FuguiSettings = new FuWindowName(ushort.MaxValue - 3, "Fugui Settings", false, -1);
        public static FuWindowName FuguiSettings { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _FuguiSettings; }
        protected static FuWindowName _DockSpaceManager = new FuWindowName(ushort.MaxValue - 4, "DockSpace Manager", true, -1);
        public static FuWindowName DockSpaceManager { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _DockSpaceManager; }
    }
}