using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Drawlist-based chart widgets for grids.
    /// </summary>
    public partial class FuGrid
    {
        #region Methods
        /// <summary>
        /// Draw a labelled chart inside a grid row.
        /// </summary>
        /// <param name="label">Grid label and stable chart ID.</param>
        /// <param name="series">Series to render.</param>
        /// <param name="options">Optional chart rendering options.</param>
        /// <param name="hover">Hover details for the chart.</param>
        /// <returns>true if the chart is hovered this frame.</returns>
        public override bool Chart(string label, IList<FuChartSeries> series, FuChartOptions options, out FuChartHoverState hover)
        {
            hover = new FuChartHoverState();
            if (!_gridCreated)
            {
                return false;
            }

            drawElementLabel(label, FuTextStyle.Default);
            return base.Chart("##" + label, series, options, out hover);
        }
        #endregion
    }
}
