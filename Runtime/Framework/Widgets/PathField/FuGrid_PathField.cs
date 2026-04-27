using System;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Grid type.
    /// </summary>
    public partial class FuGrid
    {
        #region Methods
        /// <summary>
        /// Runs the path field workflow.
        /// </summary>
        /// <param name="text">The text value.</param>
        /// <param name="onlyFolder">The only Folder value.</param>
        /// <param name="callback">The callback value.</param>
        /// <param name="style">The style value.</param>
        /// <param name="defaultPath">The default Path value.</param>
        /// <param name="extentions">The extentions value.</param>
        protected override void _pathField(string text, bool onlyFolder, Action<string> callback, FuFrameStyle style, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, FuTextStyle.Default);
            base._pathField(text, onlyFolder, callback, style, defaultPath, extentions);
        }
        #endregion
    }
}