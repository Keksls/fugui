﻿namespace Fugui.Framework
{
    public partial class UIGrid
    {
        /// <summary>
        /// Input Text Element
        /// </summary>
        /// <param name="label">Label/ID of the Element</param>
        /// <param name="hint">hover text of the input text (only if height = 0, don't wor on multiline textbox)</param>
        /// <param name="text">text value of the TextInput</param>
        /// <param name="size">buffer size of the text value</param>
        /// <param name="height">height of the input.</param>
        /// <param name="style">UIFrameStyle of the UI element</param>
        /// <returns>true if value change</returns>
        public override bool TextInput(string label, string hint, ref string text, uint size, float height, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(label, style.TextStyle);
            return base.TextInput(label, hint, ref text, size, height, style);
        }
    }
}