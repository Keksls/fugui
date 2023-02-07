using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        protected override void _buttonsGroup<T>(string text, List<T> items, Action<int> callback, Func<string> itemGetter, FuButtonsGroupFlags flags, FuButtonsGroupStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, FuTextStyle.Default);
            base._buttonsGroup<T>(text, items, callback, itemGetter, flags, style);
        }
    }
}