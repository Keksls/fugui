﻿namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a CheckBox
        /// </summary>
        /// <param name="text">Element ID and Label</param>
        /// <param name="isChecked">whatever the checkbox is checked</param>
        /// <returns>true if value change</returns>
        public override bool CheckBox(string text, ref bool isChecked)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.CheckBox("##" + text, ref isChecked);
        }
    }
}