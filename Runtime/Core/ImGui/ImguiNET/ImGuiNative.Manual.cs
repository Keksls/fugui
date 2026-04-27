using System;
using System.Runtime.InteropServices;

namespace ImGuiNET
{
    /// <summary>
    /// Represents the Im Gui Native type.
    /// </summary>
    public static unsafe partial class ImGuiNative
    {
        #region Methods
        /// <summary>
        /// Runs the im gui platform io set platform get window pos workflow.
        /// </summary>
        /// <param name="platform_io">The platform io value.</param>
        /// <param name="funcPtr">The func Ptr value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGuiPlatformIO_Set_Platform_GetWindowPos(ImGuiPlatformIO* platform_io, IntPtr funcPtr);
        /// <summary>
        /// Runs the im gui platform io set platform get window size workflow.
        /// </summary>
        /// <param name="platform_io">The platform io value.</param>
        /// <param name="funcPtr">The func Ptr value.</param>
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImGuiPlatformIO_Set_Platform_GetWindowSize(ImGuiPlatformIO* platform_io, IntPtr funcPtr);
        #endregion
    }
}