using Fu.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        #region Enum Types List
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
            FuSelectableBuilder.BuildFromEnum<TEnum>(out List<int> enumValues, out List<IFuSelectable> enumSelectables);
            // call the custom ListBox function, passing in the lists and the itemChange
            _customListBox(text, enumSelectables, (index) =>
            {
                itemChange?.Invoke(enumValues[index]);
            }, () => { return itemGetter?.Invoke().ToString(); }, size);
        }
        #endregion

        #region Generic Types List
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
            ListBox<T>(text, items, itemChange, itemGetter, FuElementSize.FullSize);
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
            List<IFuSelectable> cItems = FuSelectableBuilder.BuildFromList<T>(text, items, listUpdated?.Invoke() ?? true);
            // Display the custom ListBox and call the specified action when the selected item changes
            _customListBox(text, cItems, (index) =>
            {
                itemChange?.Invoke(items[index]);
            }, () => { return itemGetter?.Invoke()?.ToString(); }, size);
        }
        #endregion

        #region IListBoxItems
        ///<summary>
        /// Renders a ListBox with a list of custom items.
        ///</summary>
        ///<param name="text">The label for the ListBox.</param>
        ///<param name="items">The list of custom items to be displayed in the ListBox.</param>
        ///<param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the ListBox. can be null if ListBox il not lined to an object's field</param>
        ///<param name="size">The size for the ListBox element.</param>
        private void _customListBox(string text, List<IFuSelectable> items, Action<int> itemChange, Func<string> itemGetter, FuElementSize size)
        {
            // get the current selected index
            int selectedIndex = FuSelectableBuilder.GetSelectedIndex(text, items, itemGetter);

            // draw the ListBox
            ListBox(text, () =>
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i]?.DrawItem(i == selectedIndex) ?? false && items[i].Enabled)
                    {
                        // Update the selected index and invoke the item change action
                        selectedIndex = i;
                        FuSelectableBuilder.SetSelectedIndex(text, selectedIndex);
                        itemChange?.Invoke(i);
                    }
                }
            }, size);
        }
        #endregion

        #region Fully custom ListBox content
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
            if (!_drawItem)
            {
                return;
            }

            // Begin the ListBox
            if (ImGui.BeginListBox(text, size))
            {
                // Set the IsInsidePopUp flag to true
                IsInsidePopUp = true;
                // execute the callback
                callback?.Invoke();
                // Set the IsInsidePopUp flag to false
                IsInsidePopUp = false;
                // End the ListBox
                ImGui.EndListBox();
            }
            // Display the tooltip
            displayToolTip();
            // End the element with the current ListBox size
            endElement();
        }
        #endregion
    }
}