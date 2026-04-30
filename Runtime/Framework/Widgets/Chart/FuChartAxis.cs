using System;
using UnityEngine;

namespace Fu.Framework
{
    /// <summary>
    /// Axis configuration used by Fugui charts.
    /// </summary>
    public sealed class FuChartAxis
    {
        #region State
        public bool AutoRange = true;
        public float Min = 0f;
        public float Max = 1f;
        public bool IncludeZero = false;
        public float PaddingRatio = 0.05f;
        public int TickCount = 5;
        public string Label = string.Empty;
        public string ValueFormat = "0.##";
        public Func<float, string> LabelFormatter;
        #endregion

        #region Methods
        /// <summary>
        /// Set a fixed axis range and disable automatic range fitting.
        /// </summary>
        /// <param name="min">Minimum axis value.</param>
        /// <param name="max">Maximum axis value.</param>
        /// <returns>This axis instance for fluent configuration.</returns>
        public FuChartAxis SetRange(float min, float max)
        {
            AutoRange = false;
            Min = min;
            Max = max;
            return this;
        }

        /// <summary>
        /// Set automatic range fitting for this axis.
        /// </summary>
        /// <param name="includeZero">Whether the automatic range must include zero.</param>
        /// <param name="paddingRatio">Extra range padding added around the data.</param>
        /// <returns>This axis instance for fluent configuration.</returns>
        public FuChartAxis SetAutoRange(bool includeZero = false, float paddingRatio = 0.05f)
        {
            AutoRange = true;
            IncludeZero = includeZero;
            PaddingRatio = Mathf.Max(0f, paddingRatio);
            return this;
        }

        /// <summary>
        /// Format a tick value for display.
        /// </summary>
        /// <param name="value">Axis value to format.</param>
        /// <returns>The formatted axis value.</returns>
        public string FormatValue(float value)
        {
            if (LabelFormatter != null)
            {
                return LabelFormatter(value);
            }

            return value.ToString(ValueFormat);
        }
        #endregion
    }
}
