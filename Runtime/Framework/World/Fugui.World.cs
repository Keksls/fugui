namespace Fu
{
    /// <summary>
    /// Fugui world-space draw-list accessors.
    /// </summary>
    public static partial class Fugui
    {
        #region State
        private static readonly FuguiWorld _world = new FuguiWorld();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the Fugui world-space draw-list renderer.
        /// </summary>
        public static FuguiWorld World => _world;
        #endregion
    }
}
