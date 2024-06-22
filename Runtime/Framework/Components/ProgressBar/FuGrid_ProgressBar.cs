namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Renders a progress bar with the given text. The progress bar will have the default size and style.
        /// </summary>
        /// <param name="text">The text to display on the progress bar.</param>
        /// <param name="value">The value of the progress bar (between 0f and 1f)</param>
        /// <param name="isContinuous">Whatever the progressbar is continuous or idle</param>
        /// <param name="progressbarSize">size of the progressbar</param>
        /// <param name="textPosition">Position of the text value inside the progressbar</param>
        /// <param name="displayText">Text to display as value</param>
        protected override void ProgressBar(string text, float value, bool isContinuous, FuElementSize progressbarSize, ProgressBarTextPosition textPosition = ProgressBarTextPosition.Left, string displayText = null)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, FuTextStyle.Default);
            base.ProgressBar(text, value, isContinuous, progressbarSize, textPosition, displayText);
        }
    }
}