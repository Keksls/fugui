namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Grid type.
    /// </summary>
    public partial class FuGrid
    {
        #region Methods
        /// <summary>
        /// Draw a Radio Button
        /// </summary>
        /// <param name="text">Element ID and Label</param>
        /// <param name="isChecked">whatever the checkbox is checked</param>
        /// <param name="style">Checkbox style to apply</param>
        /// <returns>true if value change</returns>
        public override bool RadioButton(string text, bool isChecked, FuFrameStyle style)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, style.TextStyle);
            return base.RadioButton("##" + text, isChecked, style);
        }
        #endregion
    }
}