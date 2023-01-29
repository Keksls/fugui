using UnityEngine;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Display a custom color picker
        /// </summary>
        /// <param name="id">ID/Label of the colorpicked</param>
        /// <param name="color">reference od the color value</param>
        /// <param name="style">UIFrameStyle of the colorpicker</param>
        /// <returns>true if value change</returns>
        public override bool ColorPicker(string id, ref Vector4 color, FuFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, FuTextStyle.Default);
            return base.ColorPicker(id, ref color, style);
        }

        /// <summary>
        /// Display a alphaless custom color picker (without alpha)
        /// </summary>
        /// <param name="id">ID/Label of the colorpicked</param>
        /// <param name="color">reference od the color value</param>
        /// <param name="style">UIFrameStyle of the colorpicker</param>
        /// <returns>true if value change</returns>
        public override bool ColorPicker(string id, ref Vector3 color, FuFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, FuTextStyle.Default);
            return base.ColorPicker(id, ref color, style);
        }
    }
}