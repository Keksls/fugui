using ImGuiNET;
using System;
using System.Collections.Generic;

namespace Fu.Framework
{
    /// <summary>
    /// Represents the Fu Layout type.
    /// </summary>
    public partial class FuLayout
    {
        #region Methods
        /// <summary>
        /// Displays a ListBox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the ListBox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the ListBox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the ListBox. can be null if ListBox il not lined to an object's field</param>
        public void ListBoxEnum<TEnum>(string text, Action<int> itemChange = null, Func<TEnum> itemGetter = null) where TEnum : struct, IConvertible
        {
            ListBoxEnum<TEnum>(text, itemChange, itemGetter, FuElementSize.FullSize);
        }

        /// <summary>
        /// Displays a ListBox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the ListBox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the ListBox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the ListBox. can be null if ListBox il not lined to an object's field</param>
        /// <param name="size">The size to be applied to the ListBox</param>
        public void ListBoxEnum<TEnum>(string text, Action<int> itemChange, Func<TEnum> itemGetter, FuElementSize size) where TEnum : struct, IConvertible
        {
            FuSelectableBuilder.BuildFromEnum<TEnum>(out List<int> enumValues, out List<string> enumSelectables);
            string selectedItemString = itemGetter != null ? itemGetter.Invoke().ToString() : null;
            // Map visible enum indices inside the reusable popup state.
            _customListBox(text, enumSelectables, itemChange, null, selectedItemString, size, null, enumValues);
        }

        /// <summary>
        /// Displays a dropdown box with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the dropdown box.</param>
        /// <param name="items">The list of items to display in the dropdown box.</param>
        /// <param name="itemChange">The action to call when the selected item changes.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the ListBox. can be null if ListBox il not lined to an object's field</param>
        /// <param name="listUpdated">whatever the list has been updated since last call (list or values inside. it's for performances on large. You can handle it using ObservableCollections)
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        public void ListBox<T>(string text, List<T> items, Action<T> itemChange = null, Func<T> itemGetter = null, Func<bool> listUpdated = null)
        {
            ListBox<T>(text, items, itemChange, itemGetter, FuElementSize.FullSize, listUpdated);
        }

        /// <summary>
        /// Displays a dropdown box with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the dropdown box.</param>
        /// <param name="items">The list of items to display in the dropdown box.</param>
        /// <param name="itemChange">The action to call when the selected item changes.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the ListBox. can be null if ListBox il not lined to an object's field</param>
        /// <param name="listUpdated">whatever the list has been updated since last call (list or values inside. it's for performances on large. You can handle it using ObservableCollections)
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        /// <param name="size">The size to use for the dropdown box.</param>
        public void ListBox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter, FuElementSize size, Func<bool> listUpdated = null)
        {
            // Resolve the current value directly so no wrapper delegates are allocated for this frame.
            string selectedItemString = null;
            if (itemGetter != null)
            {
                T selectedItem = itemGetter.Invoke();
                selectedItemString = selectedItem is null ? null : selectedItem.ToString();
            }
            _customListBox(text, items, null, itemChange, selectedItemString, size, listUpdated);
        }

        /// <summary>
        /// Renders a ListBox with a list of custom items.
        /// </summary>
        ///<param name="text">The label for the ListBox.</param>
        ///<param name="items">The list of custom items to be displayed in the ListBox.</param>
        ///<param name="indexChange">Optional action receiving a selected index or mapped value.</param>
        ///<param name="itemChange">Optional action receiving the selected item.</param>
        /// <param name="selectedItemString">Current external selected value, or null.</param>
        ///<param name="size">The size for the ListBox element.</param>
        /// <param name="mappedCallbackValues">Optional callback values mapped from visible indices.</param>
        private void _customListBox<T>(string text, List<T> items, Action<int> indexChange, Action<T> itemChange, string selectedItemString, FuElementSize size, Func<bool> listUpdated = null, IList<int> mappedCallbackValues = null)
        {
            string windowId = FuWindow.CurrentDrawingWindow?.ID;
            string stateId = GetCachedCompositeId(text, "##FuSelectableListbox_", windowId);
            int selectedIndex = FuSelectableBuilder.GetSelectedIndex(stateId, items, selectedItemString);
            if (items.Count > 0)
            {
                selectedIndex = Math.Max(0, Math.Min(selectedIndex, items.Count - 1));
            }

            FuSelectablePopupState<T> state = GetSelectablePopupState<T>(stateId);
            state.Prepare(LastItemDisabled, stateId, items, selectedIndex, indexChange, itemChange, listUpdated, mappedCallbackValues, false);
            ListBox(text, state.DrawAction, size);
        }

        /// <summary>
        /// Displays a ListBox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the ListBox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">The callback function that is called when an item is selected</param>
        public void ListBox(string text, Action callback)
        {
            ListBox(text, callback, FuElementSize.FullSize);
        }

        /// <summary>
        /// Displays a ListBox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the ListBox</param>
        /// 
        /// <param name="callback">The callback function that is called when an item is selected</param>
        /// <param name="size">The size of the ListBox</param>
        public virtual void ListBox(string text, Action callback, FuElementSize size)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // Begin the ListBox
            if (ImGui.BeginListBox(text, size))
            {
                // execute the callback
                callback?.Invoke();
                // End the ListBox
                ImGui.EndListBox();
            }
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            // Display the tooltip
            displayToolTip();
            // End the element with the current ListBox size
            endElement();
        }
        #endregion
    }
}
