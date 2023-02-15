using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fu.Core
{
    public static class FuWindowsNames
    {
        private static FuWindowName _None = new FuWindowName(0, "None");
        public static FuWindowName None { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _None; }
        private static FuWindowName _WindowsDefinitionManager = new FuWindowName(2, "WindowsDefinitionManager");
        public static FuWindowName WindowsDefinitionManager { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _WindowsDefinitionManager; }
        private static FuWindowName _Tree = new FuWindowName(3, "Tree");
        public static FuWindowName Tree { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Tree; }
        private static FuWindowName _Captures = new FuWindowName(4, "Captures");
        public static FuWindowName Captures { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Captures; }
        private static FuWindowName _Inspector = new FuWindowName(5, "Inspector");
        public static FuWindowName Inspector { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Inspector; }
        private static FuWindowName _Metadata = new FuWindowName(6, "Metadata");
        public static FuWindowName Metadata { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Metadata; }
        private static FuWindowName _ToolBox = new FuWindowName(7, "ToolBox");
        public static FuWindowName ToolBox { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _ToolBox; }
        private static FuWindowName _MainCameraView = new FuWindowName(8, "MainCameraView");
        public static FuWindowName MainCameraView { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _MainCameraView; }
        private static FuWindowName _FuguiSettings = new FuWindowName(9, "FuguiSettings");
        public static FuWindowName FuguiSettings { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _FuguiSettings; }
        private static FuWindowName _DockSpaceManager = new FuWindowName(10, "DockSpaceManager");
        public static FuWindowName DockSpaceManager { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _DockSpaceManager; }
        private static FuWindowName _Modals = new FuWindowName(11, "Modals");
        public static FuWindowName Modals { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Modals; }

        public static List<FuWindowName> GetAllWindowsNames()
        {
            return new List<FuWindowName>()
            {
            _None,
            _WindowsDefinitionManager,
            _Tree,
            _Captures,
            _Inspector,
            _Metadata,
            _ToolBox,
            _MainCameraView,
            _FuguiSettings,
            _DockSpaceManager,
            _Modals,
            };
        }
    }
}