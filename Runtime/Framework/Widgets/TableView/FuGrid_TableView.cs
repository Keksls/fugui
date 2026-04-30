using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Data table widgets for grids.
    /// </summary>
    public partial class FuGrid
    {
        #region Methods
        /// <summary>
        /// Draw a labelled data table inside a grid row.
        /// </summary>
        /// <typeparam name="T">Row item type.</typeparam>
        /// <param name="label">Grid label and stable widget ID.</param>
        /// <param name="items">Source items.</param>
        /// <param name="columns">Column definitions.</param>
        /// <param name="selectedIndex">Selected source item index, or -1.</param>
        /// <param name="searchQuery">Optional search query. All terms must match one of the searchable fields.</param>
        /// <param name="searchTextGetter">Optional row-level search text. If null, searchable column text is used.</param>
        /// <param name="height">Table height. 0 uses auto height, positive values are scaled pixels, negative values subtract from available height.</param>
        /// <param name="flags">Table view behaviour flags.</param>
        /// <returns>true if the selected source index changed this frame.</returns>
        public override bool TableView<T>(string label, IList<T> items, IList<FuTableViewColumn<T>> columns, ref int selectedIndex, string searchQuery = null, Func<T, string> searchTextGetter = null, float height = 0f, FuTableViewFlags flags = FuTableViewFlags.Default)
        {
            if (!_gridCreated)
            {
                return false;
            }

            drawElementLabel(label, FuTextStyle.Default);
            return base.TableView("##" + label, items, columns, ref selectedIndex, searchQuery, searchTextGetter, height, flags);
        }
        #endregion
    }
}