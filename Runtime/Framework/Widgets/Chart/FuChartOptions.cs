using System;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Visual customization for Fugui charts.
    /// </summary>
    public sealed class FuChartStyle
    {
        #region State
        public Color? BackgroundColor;
        public Color? PlotBackgroundColor;
        public Color? FrameColor;
        public Color? GridColor;
        public Color? AxisColor;
        public Color? TextColor;
        public Color? CrosshairColor;
        public Color? TooltipPointColor;
        public Color[] Palette;
        public float FrameRounding = -1f;
        public float DisabledAlpha = 0.45f;
        #endregion
    }

    /// <summary>
    /// Runtime options used to draw a Fugui chart.
    /// </summary>
    public sealed class FuChartOptions
    {
        #region State
        public FuElementSize Size = new FuElementSize(-1f, 240f);
        public FuChartFlags Flags = FuChartFlags.Default;
        public FuChartAxis XAxis = new FuChartAxis();
        public FuChartAxis YAxis = new FuChartAxis();
        public FuChartStyle Style = new FuChartStyle();
        public float PlotPaddingLeft = 46f;
        public float PlotPaddingRight = 14f;
        public float PlotPaddingTop = 12f;
        public float PlotPaddingBottom = 30f;
        public int MaxRenderedPointsPerSeries = 2048;
        public float HoverRadius = 8f;
        public Func<FuChartHoverState, string> TooltipFormatter;
        public Action<FuChartDrawContext> BeforePlotDraw;
        public Action<FuChartDrawContext> AfterPlotDraw;
        #endregion
    }
}
