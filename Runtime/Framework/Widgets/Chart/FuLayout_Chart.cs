using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Drawlist-based chart widgets.
    /// </summary>
    public partial class FuLayout
    {
        #region State
        private static readonly FuChartOptions DEFAULT_CHART_OPTIONS = new FuChartOptions();
        private static readonly Color[] DEFAULT_CHART_PALETTE = new Color[]
        {
            new Color(0.24f, 0.58f, 0.96f, 1f),
            new Color(0.18f, 0.78f, 0.52f, 1f),
            new Color(0.95f, 0.64f, 0.20f, 1f),
            new Color(0.88f, 0.32f, 0.42f, 1f),
            new Color(0.58f, 0.44f, 0.92f, 1f),
            new Color(0.20f, 0.76f, 0.84f, 1f),
        };

        private FuChartSeries[] _singleChartSeries;
        private FuChartSeries _temporaryValueChartSeries;
        private FuChartSeries _temporaryPointChartSeries;
        private float _currentChartDisabledAlpha = 0.45f;
        #endregion

        #region Methods
        /// <summary>
        /// Draw a chart from one series.
        /// </summary>
        /// <param name="id">Unique chart ID.</param>
        /// <param name="series">Series to render.</param>
        /// <param name="options">Optional chart rendering options.</param>
        /// <returns>true if the chart is hovered this frame.</returns>
        public bool Chart(string id, FuChartSeries series, FuChartOptions options = null)
        {
            FuChartHoverState hover;
            return Chart(id, series, options, out hover);
        }

        /// <summary>
        /// Draw a chart from one series and return hover details.
        /// </summary>
        /// <param name="id">Unique chart ID.</param>
        /// <param name="series">Series to render.</param>
        /// <param name="options">Optional chart rendering options.</param>
        /// <param name="hover">Hover details for the chart.</param>
        /// <returns>true if the chart is hovered this frame.</returns>
        public bool Chart(string id, FuChartSeries series, FuChartOptions options, out FuChartHoverState hover)
        {
            if (_singleChartSeries == null)
            {
                _singleChartSeries = new FuChartSeries[1];
            }

            _singleChartSeries[0] = series;
            bool hovered = Chart(id, _singleChartSeries, options, out hover);
            _singleChartSeries[0] = null;
            return hovered;
        }

        /// <summary>
        /// Draw a chart from implicit X/Y values.
        /// </summary>
        /// <param name="id">Unique chart ID.</param>
        /// <param name="values">Y values to render.</param>
        /// <param name="type">Built-in series rendering mode.</param>
        /// <param name="options">Optional chart rendering options.</param>
        /// <returns>true if the chart is hovered this frame.</returns>
        public bool Chart(string id, IList<float> values, FuChartSeriesType type = FuChartSeriesType.Line, FuChartOptions options = null)
        {
            if (_temporaryValueChartSeries == null)
            {
                _temporaryValueChartSeries = new FuChartSeries(id, values, type);
            }

            _temporaryValueChartSeries.Label = id;
            _temporaryValueChartSeries.Values = values;
            _temporaryValueChartSeries.Points = null;
            _temporaryValueChartSeries.Type = type;
            return Chart(id, _temporaryValueChartSeries, options);
        }

        /// <summary>
        /// Draw a chart from explicit X/Y points.
        /// </summary>
        /// <param name="id">Unique chart ID.</param>
        /// <param name="points">Points to render.</param>
        /// <param name="type">Built-in series rendering mode.</param>
        /// <param name="options">Optional chart rendering options.</param>
        /// <returns>true if the chart is hovered this frame.</returns>
        public bool Chart(string id, IList<Vector2> points, FuChartSeriesType type = FuChartSeriesType.Line, FuChartOptions options = null)
        {
            if (_temporaryPointChartSeries == null)
            {
                _temporaryPointChartSeries = new FuChartSeries(id, points, type);
            }

            _temporaryPointChartSeries.Label = id;
            _temporaryPointChartSeries.Points = points;
            _temporaryPointChartSeries.Values = null;
            _temporaryPointChartSeries.Type = type;
            return Chart(id, _temporaryPointChartSeries, options);
        }

        /// <summary>
        /// Draw a chart from multiple series.
        /// </summary>
        /// <param name="id">Unique chart ID.</param>
        /// <param name="series">Series to render.</param>
        /// <param name="options">Optional chart rendering options.</param>
        /// <returns>true if the chart is hovered this frame.</returns>
        public bool Chart(string id, IList<FuChartSeries> series, FuChartOptions options = null)
        {
            FuChartHoverState hover;
            return Chart(id, series, options, out hover);
        }

        /// <summary>
        /// Draw a chart from multiple series and return hover details.
        /// </summary>
        /// <param name="id">Unique chart ID.</param>
        /// <param name="series">Series to render.</param>
        /// <param name="options">Optional chart rendering options.</param>
        /// <param name="hover">Hover details for the chart.</param>
        /// <returns>true if the chart is hovered this frame.</returns>
        public virtual bool Chart(string id, IList<FuChartSeries> series, FuChartOptions options, out FuChartHoverState hover)
        {
            hover = CreateEmptyChartHover();
            FuChartOptions chartOptions = ResolveChartOptions(options);
            _currentChartDisabledAlpha = chartOptions.Style.DisabledAlpha;
            string chartID = id;

            beginElement(ref chartID, canBeHidden: false);
            if (!_drawElement)
            {
                return false;
            }

            Vector2 size = ResolveChartSize(chartOptions);
            Vector2 position = ImGui.GetCursorScreenPos();
            Rect chartRect = new Rect(position, size);
            ImGui.InvisibleButton("##" + chartID + "_canvas", size, ImGuiButtonFlags.None);

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            bool hovered = !LastItemDisabled && ImGui.IsItemHovered();
            bool hasSeries = HasVisibleChartSeries(series);
            DrawChartFrame(drawList, chartRect, chartOptions);

            if (!hasSeries)
            {
                DrawChartEmptyState(drawList, chartRect, chartOptions);
                setBaseElementState(chartID, position, size, true, false);
                displayToolTip();
                endElement();
                return hovered;
            }

            FuChartBounds bounds = ResolveChartBounds(series, chartOptions);
            Rect plotRect = ResolveChartPlotRect(chartRect, series, chartOptions);
            Vector2 mouseValue = CreateChartDrawContext(drawList, chartRect, plotRect, bounds, Vector2.zero, hover, -1, chartOptions).ToValue(ImGui.GetMousePos());
            FuChartDrawContext context = CreateChartDrawContext(drawList, chartRect, plotRect, bounds, mouseValue, hover, -1, chartOptions);

            DrawChartPlotBackground(drawList, plotRect, chartOptions);
            DrawChartGridAndAxes(drawList, plotRect, bounds, chartOptions);
            DrawChartLegend(drawList, chartRect, series, chartOptions);

            if (hovered && IsPointInsideRect(ImGui.GetMousePos(), plotRect))
            {
                hover = FindChartHover(series, context, chartOptions);
                hover.IsHovered = true;
                context = CreateChartDrawContext(drawList, chartRect, plotRect, bounds, mouseValue, hover, -1, chartOptions);
            }

            chartOptions.BeforePlotDraw?.Invoke(context);
            if (chartOptions.Flags.HasFlag(FuChartFlags.ClipPlot))
            {
                drawList.PushClipRect(plotRect.min, plotRect.max, true);
            }

            DrawChartSeries(drawList, series, context, chartOptions);

            if (chartOptions.Flags.HasFlag(FuChartFlags.ClipPlot))
            {
                drawList.PopClipRect();
            }

            chartOptions.AfterPlotDraw?.Invoke(context);
            DrawChartHover(drawList, context, chartOptions);

            setBaseElementState(chartID, position, size, true, false);
            displayToolTip();
            endElement();
            return hovered;
        }

        /// <summary>
        /// Resolve null options and ensure nested options exist.
        /// </summary>
        /// <param name="options">User-provided chart options.</param>
        /// <returns>Resolved chart options.</returns>
        private FuChartOptions ResolveChartOptions(FuChartOptions options)
        {
            FuChartOptions chartOptions = options ?? DEFAULT_CHART_OPTIONS;
            if (chartOptions.XAxis == null)
            {
                chartOptions.XAxis = new FuChartAxis();
            }
            if (chartOptions.YAxis == null)
            {
                chartOptions.YAxis = new FuChartAxis();
            }
            if (chartOptions.Style == null)
            {
                chartOptions.Style = new FuChartStyle();
            }
            return chartOptions;
        }

        /// <summary>
        /// Resolve the final chart canvas size.
        /// </summary>
        /// <param name="options">Chart options.</param>
        /// <returns>Chart size in screen pixels.</returns>
        private Vector2 ResolveChartSize(FuChartOptions options)
        {
            Vector2 size = options.Size.GetSize();
            if (size.x <= 0f)
            {
                size.x = ImGui.GetContentRegionAvail().x;
            }
            if (size.y <= 0f)
            {
                size.y = 240f * Fugui.CurrentContext.Scale;
            }

            size.x = Mathf.Max(96f * Fugui.CurrentContext.Scale, size.x);
            size.y = Mathf.Max(72f * Fugui.CurrentContext.Scale, size.y);
            return size;
        }

        /// <summary>
        /// Check whether at least one visible series should draw something.
        /// </summary>
        /// <param name="series">Series to inspect.</param>
        /// <returns>true if at least one visible series is present.</returns>
        private bool HasVisibleChartSeries(IList<FuChartSeries> series)
        {
            if (series == null)
            {
                return false;
            }

            for (int i = 0; i < series.Count; i++)
            {
                FuChartSeries item = series[i];
                if (item != null && item.Visible)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Compute chart bounds from axes and visible data.
        /// </summary>
        /// <param name="series">Series to scan.</param>
        /// <param name="options">Chart options.</param>
        /// <returns>Resolved chart bounds.</returns>
        private FuChartBounds ResolveChartBounds(IList<FuChartSeries> series, FuChartOptions options)
        {
            bool hasData = false;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;

            if (options.XAxis.AutoRange || options.YAxis.AutoRange)
            {
                ScanChartDataBounds(series, ref hasData, ref xMin, ref xMax, ref yMin, ref yMax);
            }

            if (!hasData)
            {
                xMin = options.XAxis.AutoRange ? 0f : options.XAxis.Min;
                xMax = options.XAxis.AutoRange ? 1f : options.XAxis.Max;
                yMin = options.YAxis.AutoRange ? 0f : options.YAxis.Min;
                yMax = options.YAxis.AutoRange ? 1f : options.YAxis.Max;
            }

            if (!options.XAxis.AutoRange)
            {
                xMin = options.XAxis.Min;
                xMax = options.XAxis.Max;
            }
            if (!options.YAxis.AutoRange)
            {
                yMin = options.YAxis.Min;
                yMax = options.YAxis.Max;
            }

            if (options.XAxis.AutoRange)
            {
                ApplyChartAxisPadding(options.XAxis, ref xMin, ref xMax);
            }
            if (options.YAxis.AutoRange)
            {
                ApplyChartAxisPadding(options.YAxis, ref yMin, ref yMax);
            }

            EnsureChartRange(ref xMin, ref xMax);
            EnsureChartRange(ref yMin, ref yMax);
            return new FuChartBounds(xMin, xMax, yMin, yMax);
        }

        /// <summary>
        /// Scan all visible data points for automatic axis ranges.
        /// </summary>
        /// <param name="series">Series to scan.</param>
        /// <param name="hasData">Whether at least one finite point has been found.</param>
        /// <param name="xMin">Minimum X value.</param>
        /// <param name="xMax">Maximum X value.</param>
        /// <param name="yMin">Minimum Y value.</param>
        /// <param name="yMax">Maximum Y value.</param>
        private void ScanChartDataBounds(IList<FuChartSeries> series, ref bool hasData, ref float xMin, ref float xMax, ref float yMin, ref float yMax)
        {
            if (series == null)
            {
                return;
            }

            for (int seriesIndex = 0; seriesIndex < series.Count; seriesIndex++)
            {
                FuChartSeries item = series[seriesIndex];
                if (item == null || !item.Visible)
                {
                    continue;
                }

                int count = item.Count;
                for (int pointIndex = 0; pointIndex < count; pointIndex++)
                {
                    Vector2 point = item.GetPoint(pointIndex);
                    if (!IsFiniteChartPoint(point))
                    {
                        continue;
                    }

                    hasData = true;
                    xMin = Mathf.Min(xMin, point.x);
                    xMax = Mathf.Max(xMax, point.x);
                    yMin = Mathf.Min(yMin, point.y);
                    yMax = Mathf.Max(yMax, point.y);
                }
            }
        }

        /// <summary>
        /// Add automatic padding and optional zero inclusion to one axis range.
        /// </summary>
        /// <param name="axis">Axis options.</param>
        /// <param name="min">Range minimum.</param>
        /// <param name="max">Range maximum.</param>
        private void ApplyChartAxisPadding(FuChartAxis axis, ref float min, ref float max)
        {
            if (axis.IncludeZero)
            {
                min = Mathf.Min(min, 0f);
                max = Mathf.Max(max, 0f);
            }

            float range = Mathf.Max(0.000001f, max - min);
            float padding = range * Mathf.Max(0f, axis.PaddingRatio);
            min -= padding;
            max += padding;
        }

        /// <summary>
        /// Ensure an axis range has a usable non-zero size.
        /// </summary>
        /// <param name="min">Range minimum.</param>
        /// <param name="max">Range maximum.</param>
        private void EnsureChartRange(ref float min, ref float max)
        {
            if (Mathf.Abs(max - min) > 0.000001f)
            {
                return;
            }

            float center = min;
            min = center - 0.5f;
            max = center + 0.5f;
        }

        /// <summary>
        /// Compute the inner plot rectangle after labels and legend space.
        /// </summary>
        /// <param name="chartRect">Outer chart rectangle.</param>
        /// <param name="series">Series used by the legend.</param>
        /// <param name="options">Chart options.</param>
        /// <returns>Plot rectangle in screen coordinates.</returns>
        private Rect ResolveChartPlotRect(Rect chartRect, IList<FuChartSeries> series, FuChartOptions options)
        {
            float scale = Fugui.CurrentContext.Scale;
            float left = options.Flags.HasFlag(FuChartFlags.AxisLabels) ? options.PlotPaddingLeft * scale : 8f * scale;
            float right = options.PlotPaddingRight * scale;
            float top = options.PlotPaddingTop * scale + GetChartLegendHeight(chartRect, series, options);
            float bottom = options.Flags.HasFlag(FuChartFlags.AxisLabels) ? options.PlotPaddingBottom * scale : 8f * scale;
            float width = Mathf.Max(24f * scale, chartRect.width - left - right);
            float height = Mathf.Max(24f * scale, chartRect.height - top - bottom);
            return new Rect(chartRect.xMin + left, chartRect.yMin + top, width, height);
        }

        /// <summary>
        /// Create a draw context for chart or series callbacks.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="chartRect">Outer chart rectangle.</param>
        /// <param name="plotRect">Plot rectangle.</param>
        /// <param name="bounds">Chart bounds.</param>
        /// <param name="mouseValue">Mouse value in chart coordinates.</param>
        /// <param name="hover">Hover state.</param>
        /// <param name="seriesIndex">Current series index.</param>
        /// <param name="options">Chart options.</param>
        /// <returns>A draw context for callbacks.</returns>
        private FuChartDrawContext CreateChartDrawContext(ImDrawListPtr drawList, Rect chartRect, Rect plotRect, FuChartBounds bounds, Vector2 mouseValue, FuChartHoverState hover, int seriesIndex, FuChartOptions options)
        {
            return new FuChartDrawContext(drawList, chartRect, plotRect, new Vector2(bounds.XMin, bounds.YMin), new Vector2(bounds.XMax, bounds.YMax), mouseValue, hover, seriesIndex, LastItemDisabled, Fugui.CurrentContext.Scale);
        }

        /// <summary>
        /// Draw the outer chart frame.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="chartRect">Outer chart rectangle.</param>
        /// <param name="options">Chart options.</param>
        private void DrawChartFrame(ImDrawListPtr drawList, Rect chartRect, FuChartOptions options)
        {
            float rounding = ResolveChartRounding(options);
            drawList.AddRectFilled(chartRect.min, chartRect.max, ResolveChartColor(options.Style.BackgroundColor, Fugui.Themes.GetColor(FuColors.WindowBg), 1f), rounding);
            if (options.Flags.HasFlag(FuChartFlags.Frame))
            {
                drawList.AddRect(chartRect.min, chartRect.max, ResolveChartColor(options.Style.FrameColor, Fugui.Themes.GetColor(FuColors.Border), 1f), rounding);
            }
        }

        /// <summary>
        /// Draw the inner plot background.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="plotRect">Plot rectangle.</param>
        /// <param name="options">Chart options.</param>
        private void DrawChartPlotBackground(ImDrawListPtr drawList, Rect plotRect, FuChartOptions options)
        {
            if (!options.Flags.HasFlag(FuChartFlags.PlotBackground))
            {
                return;
            }

            drawList.AddRectFilled(plotRect.min, plotRect.max, ResolveChartColor(options.Style.PlotBackgroundColor, Fugui.Themes.GetColor(FuColors.FrameBg), 0.60f), 0f);
        }

        /// <summary>
        /// Draw a centered empty state when no series is visible.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="chartRect">Outer chart rectangle.</param>
        /// <param name="options">Chart options.</param>
        private void DrawChartEmptyState(ImDrawListPtr drawList, Rect chartRect, FuChartOptions options)
        {
            string text = "No chart data";
            Vector2 textSize = ImGui.CalcTextSize(text);
            Vector2 textPos = chartRect.center - textSize * 0.5f;
            drawList.AddText(textPos, ResolveChartColor(options.Style.TextColor, Fugui.Themes.GetColor(FuColors.TextDisabled), 1f), text);
        }

        /// <summary>
        /// Draw grid lines, tick labels and axes.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="plotRect">Plot rectangle.</param>
        /// <param name="bounds">Chart bounds.</param>
        /// <param name="options">Chart options.</param>
        private void DrawChartGridAndAxes(ImDrawListPtr drawList, Rect plotRect, FuChartBounds bounds, FuChartOptions options)
        {
            uint gridColor = ResolveChartColor(options.Style.GridColor, Fugui.Themes.GetColor(FuColors.Border), 0.35f);
            uint axisColor = ResolveChartColor(options.Style.AxisColor, Fugui.Themes.GetColor(FuColors.Text), 0.65f);
            uint textColor = ResolveChartColor(options.Style.TextColor, Fugui.Themes.GetColor(FuColors.Text), 0.72f);

            DrawChartTicks(drawList, plotRect, bounds, options.XAxis, true, options, gridColor, textColor);
            DrawChartTicks(drawList, plotRect, bounds, options.YAxis, false, options, gridColor, textColor);

            if (options.Flags.HasFlag(FuChartFlags.Axes))
            {
                drawList.AddLine(new Vector2(plotRect.xMin, plotRect.yMax), plotRect.max, axisColor, 1f);
                drawList.AddLine(plotRect.min, new Vector2(plotRect.xMin, plotRect.yMax), axisColor, 1f);
            }

            if (options.Flags.HasFlag(FuChartFlags.ZeroLine) && bounds.YMin < 0f && bounds.YMax > 0f)
            {
                float zeroY = ValueToScreenY(0f, plotRect, bounds);
                drawList.AddLine(new Vector2(plotRect.xMin, zeroY), new Vector2(plotRect.xMax, zeroY), axisColor, 1.25f);
            }
        }

        /// <summary>
        /// Draw ticks for one chart axis.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="plotRect">Plot rectangle.</param>
        /// <param name="bounds">Chart bounds.</param>
        /// <param name="axis">Axis to draw.</param>
        /// <param name="horizontal">Whether this is the X axis.</param>
        /// <param name="options">Chart options.</param>
        /// <param name="gridColor">Grid line color.</param>
        /// <param name="textColor">Tick text color.</param>
        private void DrawChartTicks(ImDrawListPtr drawList, Rect plotRect, FuChartBounds bounds, FuChartAxis axis, bool horizontal, FuChartOptions options, uint gridColor, uint textColor)
        {
            int tickCount = Mathf.Clamp(axis.TickCount, 2, 16);
            for (int i = 0; i < tickCount; i++)
            {
                float t = tickCount <= 1 ? 0f : (float)i / (float)(tickCount - 1);
                float value = horizontal ? Mathf.Lerp(bounds.XMin, bounds.XMax, t) : Mathf.Lerp(bounds.YMin, bounds.YMax, t);
                if (horizontal)
                {
                    float x = Mathf.Lerp(plotRect.xMin, plotRect.xMax, t);
                    if (options.Flags.HasFlag(FuChartFlags.Grid))
                    {
                        drawList.AddLine(new Vector2(x, plotRect.yMin), new Vector2(x, plotRect.yMax), gridColor, 1f);
                    }
                    if (options.Flags.HasFlag(FuChartFlags.AxisLabels))
                    {
                        DrawChartXAxisLabel(drawList, axis.FormatValue(value), x, plotRect.yMax + 4f * Fugui.CurrentContext.Scale, textColor);
                    }
                }
                else
                {
                    float y = Mathf.Lerp(plotRect.yMax, plotRect.yMin, t);
                    if (options.Flags.HasFlag(FuChartFlags.Grid))
                    {
                        drawList.AddLine(new Vector2(plotRect.xMin, y), new Vector2(plotRect.xMax, y), gridColor, 1f);
                    }
                    if (options.Flags.HasFlag(FuChartFlags.AxisLabels))
                    {
                        DrawChartYAxisLabel(drawList, axis.FormatValue(value), plotRect.xMin - 6f * Fugui.CurrentContext.Scale, y, textColor);
                    }
                }
            }

            DrawChartAxisTitle(drawList, plotRect, axis, horizontal, options, textColor);
        }

        /// <summary>
        /// Draw an X axis tick label centered on its tick.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="label">Tick label.</param>
        /// <param name="x">Tick screen X position.</param>
        /// <param name="y">Tick label screen Y position.</param>
        /// <param name="color">Text color.</param>
        private void DrawChartXAxisLabel(ImDrawListPtr drawList, string label, float x, float y, uint color)
        {
            Vector2 textSize = ImGui.CalcTextSize(label);
            drawList.AddText(new Vector2(x - textSize.x * 0.5f, y), color, label);
        }

        /// <summary>
        /// Draw a Y axis tick label aligned on the left of the plot.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="label">Tick label.</param>
        /// <param name="x">Tick label right edge screen X position.</param>
        /// <param name="y">Tick screen Y position.</param>
        /// <param name="color">Text color.</param>
        private void DrawChartYAxisLabel(ImDrawListPtr drawList, string label, float x, float y, uint color)
        {
            Vector2 textSize = ImGui.CalcTextSize(label);
            drawList.AddText(new Vector2(x - textSize.x, y - textSize.y * 0.5f), color, label);
        }

        /// <summary>
        /// Draw an optional axis title.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="plotRect">Plot rectangle.</param>
        /// <param name="axis">Axis options.</param>
        /// <param name="horizontal">Whether this is the X axis.</param>
        /// <param name="options">Chart options.</param>
        /// <param name="color">Text color.</param>
        private void DrawChartAxisTitle(ImDrawListPtr drawList, Rect plotRect, FuChartAxis axis, bool horizontal, FuChartOptions options, uint color)
        {
            if (!options.Flags.HasFlag(FuChartFlags.AxisLabels) || string.IsNullOrEmpty(axis.Label))
            {
                return;
            }

            Vector2 textSize = ImGui.CalcTextSize(axis.Label);
            Vector2 position = horizontal
                ? new Vector2(plotRect.center.x - textSize.x * 0.5f, plotRect.yMax + 18f * Fugui.CurrentContext.Scale)
                : new Vector2(plotRect.xMin - textSize.x - 6f * Fugui.CurrentContext.Scale, plotRect.yMin - textSize.y - 2f * Fugui.CurrentContext.Scale);
            drawList.AddText(position, color, axis.Label);
        }

        /// <summary>
        /// Draw the chart legend in the chart header area.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="chartRect">Outer chart rectangle.</param>
        /// <param name="series">Series to show.</param>
        /// <param name="options">Chart options.</param>
        private void DrawChartLegend(ImDrawListPtr drawList, Rect chartRect, IList<FuChartSeries> series, FuChartOptions options)
        {
            if (!options.Flags.HasFlag(FuChartFlags.Legend) || series == null)
            {
                return;
            }

            float scale = Fugui.CurrentContext.Scale;
            float x = chartRect.xMin + 8f * scale;
            float y = chartRect.yMin + 6f * scale;
            float maxX = chartRect.xMax - 8f * scale;
            float lineHeight = ImGui.GetTextLineHeight();
            uint textColor = ResolveChartColor(options.Style.TextColor, Fugui.Themes.GetColor(FuColors.Text), 0.85f);

            for (int i = 0; i < series.Count; i++)
            {
                FuChartSeries item = series[i];
                if (item == null || !item.Visible || string.IsNullOrEmpty(item.Label))
                {
                    continue;
                }

                Vector2 textSize = ImGui.CalcTextSize(item.Label);
                float itemWidth = 22f * scale + textSize.x + 14f * scale;
                if (x + itemWidth > maxX)
                {
                    x = chartRect.xMin + 8f * scale;
                    y += lineHeight + 4f * scale;
                }

                uint color = ToChartColorU32(item.GetColor(GetChartPaletteColor(options, i)), 1f);
                Vector2 swatchMin = new Vector2(x, y + lineHeight * 0.5f - 3f * scale);
                drawList.AddRectFilled(swatchMin, swatchMin + new Vector2(14f * scale, 6f * scale), color, 3f * scale);
                drawList.AddText(new Vector2(x + 20f * scale, y), textColor, item.Label);
                x += itemWidth;
            }
        }

        /// <summary>
        /// Calculate the vertical space needed by the chart legend.
        /// </summary>
        /// <param name="chartRect">Outer chart rectangle.</param>
        /// <param name="series">Series to show in the legend.</param>
        /// <param name="options">Chart options.</param>
        /// <returns>Legend height in screen pixels.</returns>
        private float GetChartLegendHeight(Rect chartRect, IList<FuChartSeries> series, FuChartOptions options)
        {
            if (!options.Flags.HasFlag(FuChartFlags.Legend) || series == null)
            {
                return 0f;
            }

            float scale = Fugui.CurrentContext.Scale;
            float x = chartRect.xMin + 8f * scale;
            float maxX = chartRect.xMax - 8f * scale;
            float lineHeight = ImGui.GetTextLineHeight();
            int rows = 0;

            for (int i = 0; i < series.Count; i++)
            {
                FuChartSeries item = series[i];
                if (item == null || !item.Visible || string.IsNullOrEmpty(item.Label))
                {
                    continue;
                }

                if (rows == 0)
                {
                    rows = 1;
                }

                float itemWidth = 22f * scale + ImGui.CalcTextSize(item.Label).x + 14f * scale;
                if (x + itemWidth > maxX)
                {
                    rows++;
                    x = chartRect.xMin + 8f * scale;
                }
                x += itemWidth;
            }

            return rows == 0 ? 0f : rows * (lineHeight + 4f * scale) + 4f * scale;
        }

        /// <summary>
        /// Draw all visible chart series.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="series">Series to draw.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="options">Chart options.</param>
        private void DrawChartSeries(ImDrawListPtr drawList, IList<FuChartSeries> series, FuChartDrawContext context, FuChartOptions options)
        {
            if (series == null)
            {
                return;
            }

            int barSeriesCount = CountChartBarSeries(series);
            int barSeriesIndex = 0;
            for (int i = 0; i < series.Count; i++)
            {
                FuChartSeries item = series[i];
                if (item == null || !item.Visible)
                {
                    continue;
                }

                Color color = item.GetColor(GetChartPaletteColor(options, i));
                switch (item.Type)
                {
                    case FuChartSeriesType.Area:
                        DrawChartAreaSeries(drawList, item, context.WithSeries(i), options, color);
                        break;
                    case FuChartSeriesType.Bar:
                        DrawChartBarSeries(drawList, item, context.WithSeries(i), options, color, barSeriesIndex, Mathf.Max(1, barSeriesCount));
                        barSeriesIndex++;
                        break;
                    case FuChartSeriesType.Scatter:
                        DrawChartScatterSeries(drawList, item, context.WithSeries(i), options, color);
                        break;
                    case FuChartSeriesType.Custom:
                        item.CustomDraw?.Invoke(context.WithSeries(i), item);
                        break;
                    default:
                        DrawChartLineSeries(drawList, item, context.WithSeries(i), options, color, item.ShowPoints);
                        break;
                }
            }
        }

        /// <summary>
        /// Draw a line chart series.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="series">Series to draw.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="options">Chart options.</param>
        /// <param name="color">Resolved series color.</param>
        /// <param name="drawPoints">Whether markers should be drawn on the line.</param>
        private void DrawChartLineSeries(ImDrawListPtr drawList, FuChartSeries series, FuChartDrawContext context, FuChartOptions options, Color color, bool drawPoints)
        {
            int count = series.Count;
            if (count <= 0)
            {
                return;
            }

            int stride = ResolveChartRenderStride(count, options);
            uint lineColor = ToChartColorU32(color, 1f);
            Vector2 previousPoint = Vector2.zero;
            bool hasPrevious = false;
            int lastDrawnIndex = -1;

            for (int i = 0; i < count; i += stride)
            {
                DrawChartLinePoint(drawList, series, context, i, lineColor, ref previousPoint, ref hasPrevious, drawPoints);
                lastDrawnIndex = i;
            }

            if (lastDrawnIndex != count - 1)
            {
                DrawChartLinePoint(drawList, series, context, count - 1, lineColor, ref previousPoint, ref hasPrevious, drawPoints);
            }
        }

        /// <summary>
        /// Draw one line chart point and connect it to the previous point.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="series">Series being drawn.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="pointIndex">Point index to draw.</param>
        /// <param name="lineColor">Packed line color.</param>
        /// <param name="previousPoint">Previous screen point.</param>
        /// <param name="hasPrevious">Whether a valid previous point exists.</param>
        /// <param name="drawPoint">Whether a point marker should be drawn.</param>
        private void DrawChartLinePoint(ImDrawListPtr drawList, FuChartSeries series, FuChartDrawContext context, int pointIndex, uint lineColor, ref Vector2 previousPoint, ref bool hasPrevious, bool drawPoint)
        {
            Vector2 value = series.GetPoint(pointIndex);
            if (!IsFiniteChartPoint(value))
            {
                hasPrevious = false;
                return;
            }

            Vector2 screen = context.ToScreen(value);
            if (hasPrevious)
            {
                drawList.AddLine(previousPoint, screen, lineColor, Mathf.Max(1f, series.LineThickness * context.Scale));
            }
            if (drawPoint)
            {
                drawList.AddCircleFilled(screen, Mathf.Max(1f, series.PointRadius * context.Scale), lineColor, 16);
            }

            previousPoint = screen;
            hasPrevious = true;
        }

        /// <summary>
        /// Draw an area chart series.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="series">Series to draw.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="options">Chart options.</param>
        /// <param name="color">Resolved series color.</param>
        private void DrawChartAreaSeries(ImDrawListPtr drawList, FuChartSeries series, FuChartDrawContext context, FuChartOptions options, Color color)
        {
            int count = series.Count;
            if (count <= 0)
            {
                return;
            }

            int stride = ResolveChartRenderStride(count, options);
            uint fillColor = ToChartColorU32(series.GetFillColor(color), 1f);
            float baseline = ResolveChartBaseline(series, context);
            Vector2 previousValue = Vector2.zero;
            bool hasPrevious = false;
            int lastDrawnIndex = -1;

            for (int i = 0; i < count; i += stride)
            {
                DrawChartAreaSegment(drawList, series, context, i, baseline, fillColor, ref previousValue, ref hasPrevious);
                lastDrawnIndex = i;
            }

            if (lastDrawnIndex != count - 1)
            {
                DrawChartAreaSegment(drawList, series, context, count - 1, baseline, fillColor, ref previousValue, ref hasPrevious);
            }

            DrawChartLineSeries(drawList, series, context, options, color, series.ShowPoints);
        }

        /// <summary>
        /// Draw one filled area segment from the previous point to the current point.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="series">Series being drawn.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="pointIndex">Point index to draw.</param>
        /// <param name="baseline">Area baseline value.</param>
        /// <param name="fillColor">Packed fill color.</param>
        /// <param name="previousValue">Previous chart value.</param>
        /// <param name="hasPrevious">Whether a valid previous value exists.</param>
        private void DrawChartAreaSegment(ImDrawListPtr drawList, FuChartSeries series, FuChartDrawContext context, int pointIndex, float baseline, uint fillColor, ref Vector2 previousValue, ref bool hasPrevious)
        {
            Vector2 value = series.GetPoint(pointIndex);
            if (!IsFiniteChartPoint(value))
            {
                hasPrevious = false;
                return;
            }

            if (hasPrevious)
            {
                Vector2 a = context.ToScreen(previousValue);
                Vector2 b = context.ToScreen(value);
                Vector2 baseA = context.ToScreen(new Vector2(previousValue.x, baseline));
                Vector2 baseB = context.ToScreen(new Vector2(value.x, baseline));
                // The area is drawn as segment quads, avoiding a large temporary polygon allocation.
                drawList.AddTriangleFilled(a, b, baseB, fillColor);
                drawList.AddTriangleFilled(a, baseB, baseA, fillColor);
            }

            previousValue = value;
            hasPrevious = true;
        }

        /// <summary>
        /// Draw a vertical bar chart series.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="series">Series to draw.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="options">Chart options.</param>
        /// <param name="color">Resolved series color.</param>
        /// <param name="barSeriesIndex">Index among visible bar series.</param>
        /// <param name="barSeriesCount">Number of visible bar series.</param>
        private void DrawChartBarSeries(ImDrawListPtr drawList, FuChartSeries series, FuChartDrawContext context, FuChartOptions options, Color color, int barSeriesIndex, int barSeriesCount)
        {
            int count = series.Count;
            if (count <= 0)
            {
                return;
            }

            int stride = ResolveChartRenderStride(count, options);
            uint fillColor = ToChartColorU32(series.GetFillColor(color), 1f);
            float baseline = ResolveChartBaseline(series, context);

            for (int i = 0; i < count; i += stride)
            {
                DrawChartBar(drawList, series, context, i, baseline, fillColor, barSeriesIndex, barSeriesCount);
            }

            if ((count - 1) % stride != 0)
            {
                DrawChartBar(drawList, series, context, count - 1, baseline, fillColor, barSeriesIndex, barSeriesCount);
            }
        }

        /// <summary>
        /// Draw one chart bar.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="series">Series being drawn.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="pointIndex">Point index to draw.</param>
        /// <param name="baseline">Bar baseline value.</param>
        /// <param name="fillColor">Packed fill color.</param>
        /// <param name="barSeriesIndex">Index among visible bar series.</param>
        /// <param name="barSeriesCount">Number of visible bar series.</param>
        private void DrawChartBar(ImDrawListPtr drawList, FuChartSeries series, FuChartDrawContext context, int pointIndex, float baseline, uint fillColor, int barSeriesIndex, int barSeriesCount)
        {
            Vector2 value = series.GetPoint(pointIndex);
            if (!IsFiniteChartPoint(value))
            {
                return;
            }

            float slotWidth = EstimateChartBarSlotWidth(series, context, pointIndex);
            float groupedWidth = slotWidth / Mathf.Max(1, barSeriesCount);
            float barWidth = Mathf.Max(1f, groupedWidth * Mathf.Clamp01(series.BarWidthRatio));
            float groupStart = -slotWidth * 0.5f + groupedWidth * barSeriesIndex + (groupedWidth - barWidth) * 0.5f;
            Vector2 top = context.ToScreen(value);
            Vector2 basePoint = context.ToScreen(new Vector2(value.x, baseline));
            float left = top.x + groupStart;
            float right = left + barWidth;
            float minY = Mathf.Min(top.y, basePoint.y);
            float maxY = Mathf.Max(top.y, basePoint.y);
            drawList.AddRectFilled(new Vector2(left, minY), new Vector2(right, maxY), fillColor, Mathf.Max(0f, series.BarRounding * context.Scale));
        }

        /// <summary>
        /// Draw a scatter chart series.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="series">Series to draw.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="options">Chart options.</param>
        /// <param name="color">Resolved series color.</param>
        private void DrawChartScatterSeries(ImDrawListPtr drawList, FuChartSeries series, FuChartDrawContext context, FuChartOptions options, Color color)
        {
            int count = series.Count;
            if (count <= 0)
            {
                return;
            }

            int stride = ResolveChartRenderStride(count, options);
            uint pointColor = ToChartColorU32(color, 1f);
            float radius = Mathf.Max(1f, series.PointRadius * context.Scale);

            for (int i = 0; i < count; i += stride)
            {
                DrawChartScatterPoint(drawList, series, context, i, pointColor, radius);
            }

            if ((count - 1) % stride != 0)
            {
                DrawChartScatterPoint(drawList, series, context, count - 1, pointColor, radius);
            }
        }

        /// <summary>
        /// Draw one scatter point.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="series">Series being drawn.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="pointIndex">Point index to draw.</param>
        /// <param name="pointColor">Packed point color.</param>
        /// <param name="radius">Point radius in screen pixels.</param>
        private void DrawChartScatterPoint(ImDrawListPtr drawList, FuChartSeries series, FuChartDrawContext context, int pointIndex, uint pointColor, float radius)
        {
            Vector2 value = series.GetPoint(pointIndex);
            if (!IsFiniteChartPoint(value))
            {
                return;
            }

            drawList.AddCircleFilled(context.ToScreen(value), radius, pointColor, 16);
        }

        /// <summary>
        /// Draw chart crosshair and tooltip for the current hover state.
        /// </summary>
        /// <param name="drawList">ImGui drawlist.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="options">Chart options.</param>
        private void DrawChartHover(ImDrawListPtr drawList, FuChartDrawContext context, FuChartOptions options)
        {
            if (!context.Hover.IsHovered)
            {
                return;
            }

            Vector2 mouse = ImGui.GetMousePos();
            if (options.Flags.HasFlag(FuChartFlags.Crosshair))
            {
                uint crosshairColor = ResolveChartColor(options.Style.CrosshairColor, Fugui.Themes.GetColor(FuColors.Text), 0.30f);
                drawList.AddLine(new Vector2(mouse.x, context.PlotRect.yMin), new Vector2(mouse.x, context.PlotRect.yMax), crosshairColor, 1f);
                drawList.AddLine(new Vector2(context.PlotRect.xMin, mouse.y), new Vector2(context.PlotRect.xMax, mouse.y), crosshairColor, 1f);
            }

            if (context.Hover.HasPoint)
            {
                uint pointColor = ResolveChartColor(options.Style.TooltipPointColor, Fugui.Themes.GetColor(FuColors.Text), 1f);
                drawList.AddCircleFilled(context.Hover.ScreenPosition, 4f * context.Scale, pointColor, 16);
            }

            if (options.Flags.HasFlag(FuChartFlags.Tooltip))
            {
                DrawChartTooltip(context, options);
            }
        }

        /// <summary>
        /// Draw the ImGui tooltip for the current chart hover.
        /// </summary>
        /// <param name="context">Chart draw context.</param>
        /// <param name="options">Chart options.</param>
        private void DrawChartTooltip(FuChartDrawContext context, FuChartOptions options)
        {
            if (!ImGui.BeginTooltip())
            {
                return;
            }

            if (options.TooltipFormatter != null)
            {
                ImGui.TextUnformatted(options.TooltipFormatter(context.Hover));
            }
            else if (context.Hover.HasPoint)
            {
                ImGui.TextUnformatted(string.IsNullOrEmpty(context.Hover.SeriesLabel) ? "Point" : context.Hover.SeriesLabel);
                ImGui.TextUnformatted("X: " + options.XAxis.FormatValue(context.Hover.Value.x));
                ImGui.TextUnformatted("Y: " + options.YAxis.FormatValue(context.Hover.Value.y));
            }
            else
            {
                ImGui.TextUnformatted("X: " + options.XAxis.FormatValue(context.MouseValue.x));
                ImGui.TextUnformatted("Y: " + options.YAxis.FormatValue(context.MouseValue.y));
            }

            ImGui.EndTooltip();
        }

        /// <summary>
        /// Find the closest rendered point to the mouse.
        /// </summary>
        /// <param name="series">Series to search.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="options">Chart options.</param>
        /// <returns>Hover state for the nearest point, or only the mouse value.</returns>
        private FuChartHoverState FindChartHover(IList<FuChartSeries> series, FuChartDrawContext context, FuChartOptions options)
        {
            FuChartHoverState hover = CreateEmptyChartHover();
            hover.IsHovered = true;
            hover.Value = context.MouseValue;
            hover.ScreenPosition = ImGui.GetMousePos();

            if (series == null)
            {
                return hover;
            }

            Vector2 mouse = ImGui.GetMousePos();
            float bestDistanceSq = Mathf.Max(1f, options.HoverRadius * context.Scale);
            bestDistanceSq *= bestDistanceSq;

            for (int seriesIndex = 0; seriesIndex < series.Count; seriesIndex++)
            {
                FuChartSeries item = series[seriesIndex];
                if (item == null || !item.Visible || item.Type == FuChartSeriesType.Custom)
                {
                    continue;
                }

                int count = item.Count;
                int stride = ResolveChartRenderStride(count, options);
                for (int pointIndex = 0; pointIndex < count; pointIndex += stride)
                {
                    TrySetNearestChartPoint(item, seriesIndex, pointIndex, context, mouse, ref bestDistanceSq, ref hover);
                }

                if (count > 0 && (count - 1) % stride != 0)
                {
                    TrySetNearestChartPoint(item, seriesIndex, count - 1, context, mouse, ref bestDistanceSq, ref hover);
                }
            }

            return hover;
        }

        /// <summary>
        /// Update hover state if the requested point is closer to the mouse.
        /// </summary>
        /// <param name="series">Series to test.</param>
        /// <param name="seriesIndex">Series index.</param>
        /// <param name="pointIndex">Point index.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="mouse">Mouse screen position.</param>
        /// <param name="bestDistanceSq">Current best squared distance.</param>
        /// <param name="hover">Hover state to update.</param>
        private void TrySetNearestChartPoint(FuChartSeries series, int seriesIndex, int pointIndex, FuChartDrawContext context, Vector2 mouse, ref float bestDistanceSq, ref FuChartHoverState hover)
        {
            Vector2 value = series.GetPoint(pointIndex);
            if (!IsFiniteChartPoint(value))
            {
                return;
            }

            Vector2 screen = context.ToScreen(value);
            float distanceSq = (screen - mouse).sqrMagnitude;
            if (distanceSq > bestDistanceSq)
            {
                return;
            }

            bestDistanceSq = distanceSq;
            hover.HasPoint = true;
            hover.SeriesIndex = seriesIndex;
            hover.PointIndex = pointIndex;
            hover.SeriesLabel = series.Label;
            hover.Value = value;
            hover.ScreenPosition = screen;
            hover.Distance = Mathf.Sqrt(distanceSq);
        }

        /// <summary>
        /// Count visible bar series for grouped bar layout.
        /// </summary>
        /// <param name="series">Series to inspect.</param>
        /// <returns>Number of visible bar series.</returns>
        private int CountChartBarSeries(IList<FuChartSeries> series)
        {
            int count = 0;
            if (series == null)
            {
                return count;
            }

            for (int i = 0; i < series.Count; i++)
            {
                if (series[i] != null && series[i].Visible && series[i].Type == FuChartSeriesType.Bar)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Resolve point stride used to cap rendered points per series.
        /// </summary>
        /// <param name="count">Number of source points.</param>
        /// <param name="options">Chart options.</param>
        /// <returns>Point stride for drawing and hit testing.</returns>
        private int ResolveChartRenderStride(int count, FuChartOptions options)
        {
            if (count <= 0 || options.MaxRenderedPointsPerSeries <= 0)
            {
                return 1;
            }

            return Mathf.Max(1, Mathf.CeilToInt((float)count / (float)options.MaxRenderedPointsPerSeries));
        }

        /// <summary>
        /// Estimate the slot width for one bar from neighbouring X positions.
        /// </summary>
        /// <param name="series">Bar series.</param>
        /// <param name="context">Chart draw context.</param>
        /// <param name="pointIndex">Point index.</param>
        /// <returns>Estimated slot width in screen pixels.</returns>
        private float EstimateChartBarSlotWidth(FuChartSeries series, FuChartDrawContext context, int pointIndex)
        {
            int count = series.Count;
            if (count <= 1)
            {
                return Mathf.Max(4f, context.PlotRect.width * 0.10f);
            }

            Vector2 point = series.GetPoint(pointIndex);
            float leftDistance = float.MaxValue;
            float rightDistance = float.MaxValue;
            if (pointIndex > 0)
            {
                leftDistance = Mathf.Abs(point.x - series.GetPoint(pointIndex - 1).x);
            }
            if (pointIndex < count - 1)
            {
                rightDistance = Mathf.Abs(series.GetPoint(pointIndex + 1).x - point.x);
            }

            float dataSlot = Mathf.Min(leftDistance, rightDistance);
            if (dataSlot == float.MaxValue || dataSlot <= 0f)
            {
                dataSlot = Mathf.Abs(context.Max.x - context.Min.x) / Mathf.Max(1, count);
            }

            float x0 = context.ToScreen(new Vector2(point.x, point.y)).x;
            float x1 = context.ToScreen(new Vector2(point.x + dataSlot, point.y)).x;
            return Mathf.Max(2f, Mathf.Abs(x1 - x0));
        }

        /// <summary>
        /// Resolve the baseline used by area and bar charts.
        /// </summary>
        /// <param name="series">Series options.</param>
        /// <param name="context">Chart draw context.</param>
        /// <returns>Baseline chart value.</returns>
        private float ResolveChartBaseline(FuChartSeries series, FuChartDrawContext context)
        {
            if (series.Baseline.HasValue)
            {
                return series.Baseline.Value;
            }

            return context.Min.y <= 0f && context.Max.y >= 0f ? 0f : context.Min.y;
        }

        /// <summary>
        /// Resolve a theme or custom chart color and apply disabled alpha.
        /// </summary>
        /// <param name="customColor">Optional custom color.</param>
        /// <param name="fallback">Fallback color.</param>
        /// <param name="alphaMultiplier">Additional alpha multiplier.</param>
        /// <returns>Packed ImGui color.</returns>
        private uint ResolveChartColor(Color? customColor, Vector4 fallback, float alphaMultiplier)
        {
            Color color = customColor.HasValue ? customColor.Value : (Color)fallback;
            return ToChartColorU32(color, alphaMultiplier);
        }

        /// <summary>
        /// Convert a Unity color to an ImGui packed color with disabled alpha.
        /// </summary>
        /// <param name="color">Color to convert.</param>
        /// <param name="alphaMultiplier">Additional alpha multiplier.</param>
        /// <returns>Packed ImGui color.</returns>
        private uint ToChartColorU32(Color color, float alphaMultiplier)
        {
            color.a *= alphaMultiplier;
            if (LastItemDisabled)
            {
                color.a *= _currentChartDisabledAlpha;
            }

            return ImGui.GetColorU32((Vector4)color);
        }

        /// <summary>
        /// Get a palette color for one series.
        /// </summary>
        /// <param name="options">Chart options.</param>
        /// <param name="seriesIndex">Series index.</param>
        /// <returns>Palette color.</returns>
        private Color GetChartPaletteColor(FuChartOptions options, int seriesIndex)
        {
            Color[] palette = options.Style.Palette != null && options.Style.Palette.Length > 0 ? options.Style.Palette : DEFAULT_CHART_PALETTE;
            return palette[Mathf.Abs(seriesIndex) % palette.Length];
        }

        /// <summary>
        /// Resolve frame rounding for a chart.
        /// </summary>
        /// <param name="options">Chart options.</param>
        /// <returns>Frame rounding in screen pixels.</returns>
        private float ResolveChartRounding(FuChartOptions options)
        {
            return options.Style.FrameRounding >= 0f ? options.Style.FrameRounding * Fugui.CurrentContext.Scale : Fugui.Themes.FrameRounding;
        }

        /// <summary>
        /// Convert a Y value to a screen position.
        /// </summary>
        /// <param name="value">Y value.</param>
        /// <param name="plotRect">Plot rectangle.</param>
        /// <param name="bounds">Chart bounds.</param>
        /// <returns>Screen Y coordinate.</returns>
        private float ValueToScreenY(float value, Rect plotRect, FuChartBounds bounds)
        {
            float range = Mathf.Max(0.000001f, bounds.YMax - bounds.YMin);
            return plotRect.yMax - ((value - bounds.YMin) / range) * plotRect.height;
        }

        /// <summary>
        /// Check whether a point is finite and safe to draw.
        /// </summary>
        /// <param name="point">Point to test.</param>
        /// <returns>true if both coordinates are finite.</returns>
        private bool IsFiniteChartPoint(Vector2 point)
        {
            return !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsInfinity(point.x) && !float.IsInfinity(point.y);
        }

        /// <summary>
        /// Check whether a screen point is inside a rectangle.
        /// </summary>
        /// <param name="point">Screen point.</param>
        /// <param name="rect">Rectangle to test.</param>
        /// <returns>true if the point is inside the rectangle.</returns>
        private bool IsPointInsideRect(Vector2 point, Rect rect)
        {
            return point.x >= rect.xMin && point.x <= rect.xMax && point.y >= rect.yMin && point.y <= rect.yMax;
        }

        /// <summary>
        /// Create an empty hover state.
        /// </summary>
        /// <returns>An initialized empty hover state.</returns>
        private FuChartHoverState CreateEmptyChartHover()
        {
            return new FuChartHoverState()
            {
                SeriesIndex = -1,
                PointIndex = -1,
                Distance = float.MaxValue
            };
        }
        #endregion

        #region Nested Types
        private struct FuChartBounds
        {
            #region State
            public float XMin;
            public float XMax;
            public float YMin;
            public float YMax;
            #endregion

            #region Constructors
            /// <summary>
            /// Create chart bounds.
            /// </summary>
            /// <param name="xMin">Minimum X value.</param>
            /// <param name="xMax">Maximum X value.</param>
            /// <param name="yMin">Minimum Y value.</param>
            /// <param name="yMax">Maximum Y value.</param>
            public FuChartBounds(float xMin, float xMax, float yMin, float yMax)
            {
                XMin = xMin;
                XMax = xMax;
                YMin = yMin;
                YMax = yMax;
            }
            #endregion
        }
        #endregion
    }
}
