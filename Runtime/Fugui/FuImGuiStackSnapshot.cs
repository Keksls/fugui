namespace Fu
{
    /// <summary>
    /// Captures the Fugui-managed ImGui stack depths at a protected rendering boundary.
    /// </summary>
    internal readonly struct FuImGuiStackSnapshot
    {
        internal int ColorCount { get; }
        internal int StyleCount { get; }
        internal int FontCount { get; }

        /// <summary>
        /// Creates a snapshot from the current Fugui-managed stack depths.
        /// </summary>
        /// <param name="colorCount">Current color stack depth.</param>
        /// <param name="styleCount">Current style stack depth.</param>
        /// <param name="fontCount">Current font stack depth.</param>
        internal FuImGuiStackSnapshot(int colorCount, int styleCount, int fontCount)
        {
            // A snapshot is immutable so nested rendering boundaries cannot alter their parent's baseline.
            ColorCount = colorCount;
            StyleCount = styleCount;
            FontCount = fontCount;
        }
    }
}
