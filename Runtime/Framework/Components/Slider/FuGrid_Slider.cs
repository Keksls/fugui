namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a custom unity-style slider (Label + slider + input)
        /// </summary>
        /// <param name="text">Label and ID of the slider</param>
        /// <param name="value">refered value of the slider</param>
        /// <param name="min">minimum value of the slider</param>
        /// <param name="max">maximum value of the slider</param>
        /// <param name="isInt">whatever the slider is an Int slider (default is float). If true, the value will be rounded</param>
        /// <param name="step">step of the slider value change</param>
        /// <param name="flags">Behaviour flags of the Slider</param>
        ///<param name="format">string format of the displayed value (default is "%.2f")</param>
        /// <returns>true if value changed</returns>
        protected override bool _customSlider(string text, ref float value, float min, float max, bool isInt, float step, FuSliderFlags flags, string format)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base._customSlider(text, ref value, min, max, isInt, step, flags, format);
        }
    }
}