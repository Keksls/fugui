using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Fu
{
    /// <summary>
    /// Represents the Fu Windows Names type.
    /// </summary>
    public class FuWindowsNames : FuSystemWindowsNames
    {
        #region State
        private static FuWindowName _Tree = new FuWindowName(4, "Tree", true, -1);

        /// <summary>
        /// Gets the Tree Fugui window name.
        /// </summary>
        public static FuWindowName Tree { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Tree; }

        private static FuWindowName _Inspector = new FuWindowName(6, "Inspector", true, -1);

        /// <summary>
        /// Gets the Inspector Fugui window name.
        /// </summary>
        public static FuWindowName Inspector { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Inspector; }

        private static FuWindowName _Widgets = new FuWindowName(11, "Widgets", true, -1);

        /// <summary>
        /// Gets the Widgets Fugui window name.
        /// </summary>
        public static FuWindowName Widgets { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Widgets; }

        private static FuWindowName _MainCameraView = new FuWindowName(9, "3D View", true, -1);

        /// <summary>
        /// Gets the 3D View Fugui window name.
        /// </summary>
        public static FuWindowName MainCameraView { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _MainCameraView; }

        private static FuWindowName _Popups = new FuWindowName(10, "Popups", true, -1);

        /// <summary>
        /// Gets the Popups Fugui window name.
        /// </summary>
        public static FuWindowName Popups { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _Popups; }

        private static FuWindowName _NodalEditor = new FuWindowName(12, "Nodal Editor", true, -1);

        /// <summary>
        /// Gets the Nodal Editor Fugui window name.
        /// </summary>
        public static FuWindowName NodalEditor { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _NodalEditor; }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the all windows names.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public static List<FuWindowName> GetAllWindowsNames()
        {
            return new List<FuWindowName>()
            {
                _None,
                _FuguiSettings,
                _Tree,
                _Inspector,
                _Widgets,
                _MainCameraView,
                _Popups,
                _NodalEditor
            };
        }
        #endregion
    }
}
