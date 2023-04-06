namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Draw a gradient picker
        /// </summary>
        /// <param name="text">text / ID of the gradient</param>
        /// <param name="gradient">gradient to edit</param>
        /// <param name="width">width of the gradient picker popup</param>
        /// <param name="height">height of the gradient preview on popup</param>
        /// <returns>whatever the gradient has been edited this frame</returns>
        public override bool Gradient(string text, ref FuGradient gradient, float width = 256f, float height = 24f)
        {
            if (!_gridCreated)
            {
                return false;
            }
            drawElementLabel(text, FuTextStyle.Default);
            return base.Gradient(text, ref gradient, width, height);
        }
    }
}