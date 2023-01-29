using System;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        protected override void _pathField(string text, bool onlyFolder, Action<string> callback, FuFrameStyle style, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, FuTextStyle.Default);
            base._pathField(text, onlyFolder, callback, style, defaultPath, extentions);
        }
    }
}