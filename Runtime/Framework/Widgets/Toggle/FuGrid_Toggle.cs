namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Grid type.
    /// </summary>
    public partial class FuGrid
    {
        #region Methods
        /// <summary>
        /// Returns the custom toggle result.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <param name="value">The value value.</param>
        /// <param name="textLeft">The text Left value.</param>
        /// <param name="textRight">The text Right value.</param>
        /// <param name="flags">The flags value.</param>
        /// <returns>The result of the operation.</returns>
        protected override bool _customToggle(string text, ref bool value, string textLeft, string textRight, FuToggleFlags flags)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base._customToggle(text, ref value, textLeft, textRight, flags);
        }
        #endregion
    }
}