namespace Fu.Framework
{
    /// <summary>
    /// Search input widgets for grids.
    /// </summary>
    public partial class FuGrid
    {
        #region Methods
        /// <summary>
        /// Draw a labelled search field inside a grid row.
        /// </summary>
        /// <param name="label">Grid label and stable widget ID.</param>
        /// <param name="search">Search string to edit.</param>
        /// <param name="hint">Placeholder displayed when the search string is empty.</param>
        /// <param name="width">Widget width. 0 uses available width, positive values are scaled pixels, negative values subtract from available width.</param>
        /// <param name="style">Frame style to use.</param>
        /// <returns>true if the search string changed this frame.</returns>
        public override bool SearchBox(string label, ref string search, string hint, float width, FuFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }

            drawElementLabel(label, style.TextStyle);
            return base.SearchBox("##" + label, ref search, hint, width, style);
        }
        #endregion
    }
}