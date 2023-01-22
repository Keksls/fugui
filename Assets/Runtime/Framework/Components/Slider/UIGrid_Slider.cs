namespace Fugui.Framework
{
    public partial class UIGrid
    {
        /// <summary>
        /// Draw a custom unity-style slider (Label + slider + input)
        /// </summary>
        /// <param name="text">Label and ID of the slider</param>
        /// <param name="value">refered value of the slider</param>
        /// <param name="min">minimum value of the slider</param>
        /// <param name="max">maximum value of the slider</param>
        /// <param name="isInt">whatever the slider is an Int slider (default is float). If true, the value will be rounded</param>
        /// <returns>true if value changed</returns>
        protected override bool _customSlider(string text, ref float value, float min, float max, bool isInt)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, UITextStyle.Default);
            return base._customSlider(text, ref value, min, max, isInt);
        }
    }
}