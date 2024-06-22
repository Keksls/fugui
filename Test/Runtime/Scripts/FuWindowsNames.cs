using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Fu.Core
{
    public class FuWindowsNames : FuSystemWindowsNames
    {
        private static FuWindowName _Tree = new FuWindowName(4, "Tree", true, -1);
        public static FuWindowName Tree { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Tree; }
        private static FuWindowName _Inspector = new FuWindowName(6, "Inspector", true, -1);
        public static FuWindowName Inspector { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Inspector; }
        private static FuWindowName _Widgets = new FuWindowName(11, "Widgets", true, -1);
        public static FuWindowName Widgets { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Widgets; }
        private static FuWindowName _MainCameraView = new FuWindowName(9, "3D View", true, -1);
        public static FuWindowName MainCameraView { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _MainCameraView; }
        private static FuWindowName _Popups = new FuWindowName(10, "Popups", true, -1);
        public static FuWindowName Popups { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Popups; }
        public static List<FuWindowName> GetAllWindowsNames()
        {
            return new List<FuWindowName>()
            {
                _None,
                _WindowsDefinitionManager,
                _FuguiSettings,
                _DockSpaceManager,
                _Tree,
                _Inspector,
                _Widgets,
                _MainCameraView,
                _Popups,
            };
        }
    }
}