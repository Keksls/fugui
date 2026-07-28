namespace Fu
{
    /// <summary>
    /// Fugui-managed ImGui stack protection.
    /// </summary>
    public static partial class Fugui
    {
        /// <summary>
        /// Captures the current depths of every ImGui stack managed by Fugui.
        /// </summary>
        /// <returns>The stack depths to restore when the protected rendering block exits.</returns>
        internal static FuImGuiStackSnapshot CaptureImGuiStackSnapshot()
        {
            // Only Fugui-owned global stacks are captured; window-local native stacks use local finally blocks.
            return new FuImGuiStackSnapshot(NbPushColor, NbPushStyle, NbPushFont);
        }

        /// <summary>
        /// Restores every Fugui-managed ImGui stack to a previously captured depth.
        /// </summary>
        /// <param name="snapshot">Stack depths captured before entering the protected rendering block.</param>
        internal static void RestoreImGuiStackSnapshot(FuImGuiStackSnapshot snapshot)
        {
            // Nested finally blocks guarantee that one failed native pop cannot prevent the other stacks from being restored.
            try
            {
                RestoreColorStack(snapshot.ColorCount);
            }
            finally
            {
                try
                {
                    RestoreStyleStack(snapshot.StyleCount);
                }
                finally
                {
                    RestoreFontStack(snapshot.FontCount);
                }
            }
        }

        /// <summary>
        /// Pops colors added after the protected rendering block began.
        /// </summary>
        /// <param name="targetCount">Color stack depth to restore.</param>
        private static void RestoreColorStack(int targetCount)
        {
            // Never pop entries that were owned by the caller before this boundary.
            int extraPushCount = NbPushColor - targetCount;
            if (extraPushCount > 0)
            {
                PopColor(extraPushCount);
            }
        }

        /// <summary>
        /// Pops style variables added after the protected rendering block began.
        /// </summary>
        /// <param name="targetCount">Style stack depth to restore.</param>
        private static void RestoreStyleStack(int targetCount)
        {
            // Never pop entries that were owned by the caller before this boundary.
            int extraPushCount = NbPushStyle - targetCount;
            if (extraPushCount > 0)
            {
                PopStyle(extraPushCount);
            }
        }

        /// <summary>
        /// Pops fonts added after the protected rendering block began.
        /// </summary>
        /// <param name="targetCount">Font stack depth to restore.</param>
        private static void RestoreFontStack(int targetCount)
        {
            // Never pop entries that were owned by the caller before this boundary.
            int extraPushCount = NbPushFont - targetCount;
            if (extraPushCount > 0)
            {
                PopFont(extraPushCount);
            }
        }
    }
}
