using UnityEngine;

namespace Fugui.Framework
{
    public partial class UIGrid
    {
        /// <summary>
        /// Display a custom color picker
        /// </summary>
        /// <param name="id">ID/Label of the colorpicked</param>
        /// <param name="color">reference od the color value</param>
        /// <param name="style">UIFrameStyle of the colorpicker</param>
        /// <returns>true if value change</returns>
        public override bool ColorPicker(string id, ref Vector4 color, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, UITextStyle.Default);
            return base.ColorPicker(id, ref color, style);
        }

        /// <summary>
        /// Display a alphaless custom color picker (without alpha)
        /// </summary>
        /// <param name="id">ID/Label of the colorpicked</param>
        /// <param name="color">reference od the color value</param>
        /// <param name="style">UIFrameStyle of the colorpicker</param>
        /// <returns>true if value change</returns>
        public override bool ColorPicker(string id, ref Vector3 color, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(id, UITextStyle.Default);
            return base.ColorPicker(id, ref color, style);
        }
    }
}