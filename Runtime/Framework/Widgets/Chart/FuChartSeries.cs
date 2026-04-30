using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Built-in chart series rendering modes.
    /// </summary>
    public enum FuChartSeriesType
    {
        /// <summary>
        /// Draw connected line segments.
        /// </summary>
        Line,
        /// <summary>
        /// Draw a filled area under connected line segments.
        /// </summary>
        Area,
        /// <summary>
        /// Draw one vertical bar per point.
        /// </summary>
        Bar,
        /// <summary>
        /// Draw one circular marker per point.
        /// </summary>
        Scatter,
        /// <summary>
        /// Let the user draw the series through a custom drawlist callback.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Data and rendering configuration for one chart series.
    /// </summary>
    public sealed class FuChartSeries
    {
        #region State
        public string Label { get; set; }
        public IList<Vector2> Points { get; set; }
        public IList<float> Values { get; set; }
        public float XStart { get; set; }
        public float XStep { get; set; } = 1f;
        public FuChartSeriesType Type { get; set; } = FuChartSeriesType.Line;
        public bool Visible { get; set; } = true;
        public Color? Color { get; set; }
        public Color? FillColor { get; set; }
        public float FillAlpha { get; set; } = 0.18f;
        public float LineThickness { get; set; } = 2f;
        public float PointRadius { get; set; } = 3f;
        public bool ShowPoints { get; set; } = false;
        public float BarWidthRatio { get; set; } = 0.72f;
        public float BarRounding { get; set; } = 2f;
        public float? Baseline { get; set; }
        public Action<FuChartDrawContext, FuChartSeries> CustomDraw { get; set; }
        public int Count => Points != null ? Points.Count : Values != null ? Values.Count : 0;
        #endregion

        #region Constructors
        /// <summary>
        /// Create a chart series backed by explicit X/Y points.
        /// </summary>
        /// <param name="label">Series label used in legends and tooltips.</param>
        /// <param name="points">Series points in chart value coordinates.</param>
        /// <param name="type">Built-in series rendering mode.</param>
        public FuChartSeries(string label, IList<Vector2> points, FuChartSeriesType type = FuChartSeriesType.Line)
        {
            Label = label;
            Points = points;
            Type = type;
        }

        /// <summary>
        /// Create a chart series backed by Y values and an implicit regular X axis.
        /// </summary>
        /// <param name="label">Series label used in legends and tooltips.</param>
        /// <param name="values">Series Y values.</param>
        /// <param name="type">Built-in series rendering mode.</param>
        /// <param name="xStart">First generated X value.</param>
        /// <param name="xStep">Generated X delta between values.</param>
        public FuChartSeries(string label, IList<float> values, FuChartSeriesType type = FuChartSeriesType.Line, float xStart = 0f, float xStep = 1f)
        {
            Label = label;
            Values = values;
            Type = type;
            XStart = xStart;
            XStep = xStep;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Create a chart series that delegates all drawing to a callback.
        /// </summary>
        /// <param name="label">Series label used in legends and tooltips.</param>
        /// <param name="draw">Custom draw callback that receives the drawlist context.</param>
        /// <returns>A custom chart series.</returns>
        public static FuChartSeries Custom(string label, Action<FuChartDrawContext, FuChartSeries> draw)
        {
            return new FuChartSeries(label, (IList<Vector2>)null, FuChartSeriesType.Custom)
            {
                CustomDraw = draw
            };
        }

        /// <summary>
        /// Get one point from either explicit points or implicit X/Y values.
        /// </summary>
        /// <param name="index">Point index.</param>
        /// <returns>The chart point at the requested index.</returns>
        public Vector2 GetPoint(int index)
        {
            if (Points != null)
            {
                return Points[index];
            }

            return new Vector2(XStart + index * XStep, Values[index]);
        }

        /// <summary>
        /// Get the color used to draw this series.
        /// </summary>
        /// <param name="fallback">Fallback color from the chart palette.</param>
        /// <returns>The resolved series color.</returns>
        public Color GetColor(Color fallback)
        {
            return Color.HasValue ? Color.Value : fallback;
        }

        /// <summary>
        /// Get the fill color used by area or bar rendering.
        /// </summary>
        /// <param name="lineColor">Resolved line color for the series.</param>
        /// <returns>The resolved fill color.</returns>
        public Color GetFillColor(Color lineColor)
        {
            Color fill = FillColor.HasValue ? FillColor.Value : lineColor;
            fill.a *= Mathf.Clamp01(FillAlpha);
            return fill;
        }
        #endregion
    }
}
