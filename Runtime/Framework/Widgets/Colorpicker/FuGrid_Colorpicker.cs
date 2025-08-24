using UnityEngine;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Display a custom color picker
        /// </summary>
        /// <param name="text">ID/Label of the colorpicked</param>
        /// <param name="alpha">did the color picker must draw alpha line and support alpha</param>
        /// <param name="color">reference od the color value</param>
        /// <param name="style">UIFrameStyle of the colorpicker</param>
        /// <returns>true if value change</returns>
        protected override bool _customColorPicker(string text, bool alpha, ref Vector4 color, FuFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base._customColorPicker(text, alpha, ref color, style);
        }
    }
}