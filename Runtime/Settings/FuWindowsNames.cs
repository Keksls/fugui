using System.Runtime.CompilerServices;

namespace Fu
{
    /// <summary>
    /// Represents the Fu System Windows Names type.
    /// </summary>
    public class FuSystemWindowsNames
    {
        #region State
        /// <summary>
        /// Backing field for the empty Fugui window name.
        /// </summary>
        protected static FuWindowName _None = new FuWindowName(ushort.MaxValue - 1, "None", false, -1);

        /// <summary>
        /// Gets the empty Fugui window name.
        /// </summary>
        public static FuWindowName None { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _None; }

        /// <summary>
        /// Backing field for the Fugui settings window name.
        /// </summary>
        protected static FuWindowName _FuguiSettings = new FuWindowName(ushort.MaxValue - 3, "Fugui Settings", true, -1);

        /// <summary>
        /// Gets the Fugui settings window name.
        /// </summary>
        public static FuWindowName FuguiSettings { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _FuguiSettings; }
        #endregion
    }
}
