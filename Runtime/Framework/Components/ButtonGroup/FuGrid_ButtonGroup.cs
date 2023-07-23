using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    public partial class FuGrid
    {
        /// <summary>
        /// Displays a ButtonGroup with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the element.</param>
        /// <param name="items">The list of items to display in the buttonGroup.</param>
        /// <param name="callback">Callback raised whenever selected value change</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the buttonGroup. can be null if buttonGroup il not linked to an object's field
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        /// <param name="width">maximum width of the group (0 tu let fugui descide)</param>
        /// <param name="flags">behaviour flags of the button group</param>
        /// <param name="style">style of the element</param>
        protected override void _buttonsGroup<T>(string text, List<T> items, Action<int> callback, Func<string> itemGetter, float width, FuButtonsGroupFlags flags, FuButtonsGroupStyle style)
        {
            if (!_gridCreated)
            {
                return;
            }
            drawElementLabel(text, FuTextStyle.Default);
            base._buttonsGroup<T>(text, items, callback, itemGetter, width, flags, style);
        }
    }
}