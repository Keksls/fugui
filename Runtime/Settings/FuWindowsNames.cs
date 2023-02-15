using System.Runtime.CompilerServices;

namespace Fu.Core
{
    public class FuSystemWindowsNames
    {
        protected static FuWindowName _None = new FuWindowName(0, "None");
        public static FuWindowName None { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _None; }
        protected static FuWindowName _WindowsDefinitionManager = new FuWindowName(1, "WindowsDefinitionManager");
        public static FuWindowName WindowsDefinitionManager { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _WindowsDefinitionManager; }
        protected static FuWindowName _FuguiSettings = new FuWindowName(2, "FuguiSettings");
        public static FuWindowName FuguiSettings { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _FuguiSettings; }
        protected static FuWindowName _DockSpaceManager = new FuWindowName(3, "DockSpaceManager");
        public static FuWindowName DockSpaceManager { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _DockSpaceManager; }
        public static ushort FuguiReservedLastID = 3;
    }
}