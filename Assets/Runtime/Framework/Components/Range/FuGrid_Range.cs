namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a custom unity-style slider (slider + input)
        /// </summary>
        /// <param name="text">Label and ID of the slider</param>
        /// <param name="valueMin">The minimum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="valueMax">The maximum value of the range slider, which will be updated if the user interacts with the slider.</param>
        /// <param name="min">minimum value of the slider</param>
        /// <param name="max">maximum value of the slider</param>
        /// <param name="isInt">whatever the slider is an Int slider (default is float). If true, the value will be rounded</param>
        /// <param name="step">step of the slider value change</param>
        /// <param name="flags">behaviour flag of the slider</param>
        /// <returns>true if value changed</returns>
        protected override bool _customRange(string text, ref float valueMin, ref float valueMax, float min, float max, bool isInt, float step, FuSliderFlags flags)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base._customRange(text, ref valueMin, ref valueMax, min, max, isInt, step, flags);
        }
    }
}