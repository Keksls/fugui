using System;
using System.Collections.Generic;

namespace Fugui.Framework
{
    public partial class UIGrid
    {
        protected override void _buttonsGroup<T>(string text, List<T> items, Action<int> callback, int defaultSelected, ButtonsGroupFlags flags, UIButtonsGroupStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, UITextStyle.Default);
            base._buttonsGroup<T>(text, items, callback, defaultSelected, flags, style);
        }
    }
}