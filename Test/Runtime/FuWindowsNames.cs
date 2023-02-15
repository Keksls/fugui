using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Fu.Core
{
    public class FuWindowsNames : FuSystemWindowsNames
    {
        private static FuWindowName _Tree = new FuWindowName(4, "Tree");
        public static FuWindowName Tree { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Tree; }
        private static FuWindowName _Captures = new FuWindowName(5, "Captures");
        public static FuWindowName Captures { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Captures; }
        private static FuWindowName _Inspector = new FuWindowName(6, "Inspector");
        public static FuWindowName Inspector { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Inspector; }
        private static FuWindowName _Metadata = new FuWindowName(7, "Metadata");
        public static FuWindowName Metadata { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Metadata; }
        private static FuWindowName _ToolBox = new FuWindowName(8, "ToolBox");
        public static FuWindowName ToolBox { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _ToolBox; }
        private static FuWindowName _MainCameraView = new FuWindowName(9, "MainCameraView");
        public static FuWindowName MainCameraView { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _MainCameraView; }
        private static FuWindowName _Modals = new FuWindowName(10, "Modals");
        public static FuWindowName Modals { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Modals; }
        public static List<FuWindowName> GetAllWindowsNames()
        {
            return new List<FuWindowName>()
            {
                _None,
                _WindowsDefinitionManager,
                _FuguiSettings,
                _DockSpaceManager,
                _Tree,
                _Captures,
                _Inspector,
                _Metadata,
                _ToolBox,
                _MainCameraView,
                _Modals,
            };
        }
    }
}