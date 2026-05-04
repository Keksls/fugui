using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Fu.Samples.MobileDemo
{
    /// <summary>
    /// Window names used by the Fugui mobile demo sample.
    /// </summary>
    public class FuMobileDemoWindowNames : FuSystemWindowsNames
    {
        #region State
        private static FuWindowName _MobileDemo = new FuWindowName(201, "Mobile Demo", true, 30);

        /// <summary>
        /// Gets the mobile demo Fugui window name.
        /// </summary>
        public static FuWindowName MobileDemo { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _MobileDemo; }
        #endregion

        #region Methods
        /// <summary>
        /// Gets every Fugui window name declared by this sample.
        /// </summary>
        /// <returns>The registered mobile demo window names.</returns>
        public static List<FuWindowName> GetAllWindowsNames()
        {
            return new List<FuWindowName>()
            {
                _None,
                _FuguiSettings,
                _MobileDemo
            };
        }
        #endregion
    }
}
