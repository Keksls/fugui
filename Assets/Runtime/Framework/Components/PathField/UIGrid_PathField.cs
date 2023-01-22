using System;

namespace Fugui.Framework
{
    public partial class UIGrid
    {
        protected override void _pathField(string text, bool onlyFolder, Action<string> callback, UIFrameStyle style, string defaultPath = null, params ExtensionFilter[] extentions)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, UITextStyle.Default);
            base._pathField(text, onlyFolder, callback, style, defaultPath, extentions);
        }
    }
}