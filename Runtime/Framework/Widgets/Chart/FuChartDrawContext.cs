using ImGuiNET;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Hover information returned by a Fugui chart.
    /// </summary>
    public struct FuChartHoverState
    {
        #region State
        public bool IsHovered;
        public bool HasPoint;
        public int SeriesIndex;
        public int PointIndex;
        public string SeriesLabel;
        public Vector2 Value;
        public Vector2 ScreenPosition;
        public float Distance;
        #endregion
    }

    /// <summary>
    /// Drawlist context passed to chart custom drawing callbacks.
    /// </summary>
    public struct FuChartDrawContext
    {
        #region State
        public ImDrawListPtr DrawList { get; private set; }
        public Rect ChartRect { get; private set; }
        public Rect PlotRect { get; private set; }
        public Vector2 Min { get; private set; }
        public Vector2 Max { get; private set; }
        public Vector2 MouseValue { get; private set; }
        public FuChartHoverState Hover { get; private set; }
        public int SeriesIndex { get; private set; }
        public bool Disabled { get; private set; }
        public float Scale { get; private set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a chart draw context.
        /// </summary>
        /// <param name="drawList">ImGui drawlist used by the chart.</param>
        /// <param name="chartRect">Outer chart rectangle in screen coordinates.</param>
        /// <param name="plotRect">Plot rectangle in screen coordinates.</param>
        /// <param name="min">Minimum chart values.</param>
        /// <param name="max">Maximum chart values.</param>
        /// <param name="mouseValue">Mouse position converted to chart values.</param>
        /// <param name="hover">Current chart hover state.</param>
        /// <param name="seriesIndex">Current series index, or -1 for chart-level callbacks.</param>
        /// <param name="disabled">Whether the chart is disabled.</param>
        /// <param name="scale">Current Fugui scale.</param>
        public FuChartDrawContext(ImDrawListPtr drawList, Rect chartRect, Rect plotRect, Vector2 min, Vector2 max, Vector2 mouseValue, FuChartHoverState hover, int seriesIndex, bool disabled, float scale)
        {
            DrawList = drawList;
            ChartRect = chartRect;
            PlotRect = plotRect;
            Min = min;
            Max = max;
            MouseValue = mouseValue;
            Hover = hover;
            SeriesIndex = seriesIndex;
            Disabled = disabled;
            Scale = scale;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Convert chart values to screen coordinates inside the plot rectangle.
        /// </summary>
        /// <param name="value">Chart value to convert.</param>
        /// <returns>Screen position for the value.</returns>
        public Vector2 ToScreen(Vector2 value)
        {
            float xRange = Mathf.Max(0.000001f, Max.x - Min.x);
            float yRange = Mathf.Max(0.000001f, Max.y - Min.y);
            float x = PlotRect.xMin + ((value.x - Min.x) / xRange) * PlotRect.width;
            float y = PlotRect.yMax - ((value.y - Min.y) / yRange) * PlotRect.height;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Convert screen coordinates inside the plot rectangle to chart values.
        /// </summary>
        /// <param name="screen">Screen position to convert.</param>
        /// <returns>Chart value for the screen position.</returns>
        public Vector2 ToValue(Vector2 screen)
        {
            float xRange = Mathf.Max(0.000001f, Max.x - Min.x);
            float yRange = Mathf.Max(0.000001f, Max.y - Min.y);
            float x = Min.x + ((screen.x - PlotRect.xMin) / Mathf.Max(1f, PlotRect.width)) * xRange;
            float y = Min.y + ((PlotRect.yMax - screen.y) / Mathf.Max(1f, PlotRect.height)) * yRange;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Create the same context for a specific series index.
        /// </summary>
        /// <param name="seriesIndex">Series index currently being drawn.</param>
        /// <returns>A context bound to the requested series.</returns>
        public FuChartDrawContext WithSeries(int seriesIndex)
        {
            return new FuChartDrawContext(DrawList, ChartRect, PlotRect, Min, Max, MouseValue, Hover, seriesIndex, Disabled, Scale);
        }
        #endregion
    }
}
