﻿namespace Fugui.Framework
{
    public partial class UIGrid
    {
        /// <summary>
        /// Draw a Radio Button
        /// </summary>
        /// <param name="text">Element ID and Label</param>
        /// <param name="isChecked">whatever the checkbox is checked</param>
        /// <param name="style">Checkbox style to apply</param>
        /// <returns>true if value change</returns>
        public override bool RadioButton(string text, bool isChecked, UIFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            return base.RadioButton("##" + text, isChecked, style);
        }
    }
}