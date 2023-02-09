namespace Fu.Framework
{
    public partial class FuGrid
    {
        public override void ProgressBar(string text, float value, bool isContinuous, FuElementSize progressbarSize, ProgressBarTextPosition textPosition = ProgressBarTextPosition.Left)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, FuTextStyle.Default);
            base.ProgressBar(text, value, isContinuous, progressbarSize, textPosition);
        }
    }
}