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
        /// Displays a combobox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        public void ComboboxEnum<TEnum>(string text, Action<int> itemChange, Func<TEnum> itemGetter = null) where TEnum : struct, IConvertible
        {
            ComboboxEnum<TEnum>(text, itemChange, itemGetter, FuComboboxStyle.Default);
        }

        /// <summary>
        /// Displays a combobox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        /// <param name="style">The style to be applied to the combobox</param>
        public void ComboboxEnum<TEnum>(string text, Action<int> itemChange, Func<TEnum> itemGetter, FuComboboxStyle style) where TEnum : struct, IConvertible
        {
            FuSelectableBuilder.BuildFromEnum<TEnum>(out List<int> enumValues, out List<IFuSelectable> enumSelectables);
            // call the custom combobox function, passing in the lists and the itemChange
            _customCombobox(text, enumSelectables, (index) =>
            {
                itemChange?.Invoke(enumValues[index]);
            }, () => { return itemGetter?.Invoke().ToString(); }, style);
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
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        /// <param name="listUpdated">whatever the list has been updated since last call (list or values inside. it's for performances on large. You can handle it using ObservableCollections)
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        public void Combobox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter = null, Func<bool> listUpdated = null)
        {
            Combobox<T>(text, items, itemChange, itemGetter, FuComboboxStyle.Default, listUpdated);
        }

        /// <summary>
        /// Displays a dropdown box with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the dropdown box.</param>
        /// <param name="items">The list of items to display in the dropdown box.</param>
        /// <param name="itemChange">The action to call when the selected item changes.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        /// <param name="style">The style to use for the dropdown box.</param>
        /// <param name="listUpdated">whatever the list has been updated since last call (list or values inside. it's for performances on large. You can handle it using ObservableCollections)
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        public void Combobox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter, FuComboboxStyle style, Func<bool> listUpdated = null)
        {
            List<IFuSelectable> cItems = FuSelectableBuilder.BuildFromList<T>(text, items, listUpdated?.Invoke() ?? true);
            // Display the custom combobox and call the specified action when the selected item changes
            _customCombobox(text, cItems, (index) =>
            {
                itemChange?.Invoke(items[index]);
            }, () => { return itemGetter?.Invoke()?.ToString(); }, style);
        }
        #endregion

        #region IComboboxItems
        ///<summary>
        /// Renders a combobox with a list of custom items.
        ///</summary>
        ///<param name="text">The label for the combobox.</param>
        ///<param name="items">The list of custom items to be displayed in the combobox.</param>
        ///<param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not linked to an object's field</param>
        ///<param name="style">The style for the combobox element.</param>
        private void _customCombobox(string text, List<IFuSelectable> items, Action<int> itemChange, Func<string> itemGetter, FuComboboxStyle style)
        {
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // get the current selected index
            int selectedIndex = FuSelectableBuilder.GetSelectedIndex(text, items, itemGetter);

            // draw the combobox
            Combobox(text, items[selectedIndex].Text, () =>
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
            });
        }
        #endregion

        #region Fully custom combobox content
        /// <summary>
        /// Displays a combobox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the combobox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">The callback function that is called when an item is selected</param>
        public void Combobox(string text, string selectedItemText, Action callback)
        {
            Combobox(text, selectedItemText, callback, FuComboboxStyle.Default);
        }

        /// <summary>
        /// Displays a combobox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the combobox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">The callback function that is called when an item is selected</param>
        /// <param name="style">The style of the combobox</param>
        public virtual void Combobox(string text, string selectedItemText, Action callback, FuComboboxStyle style)
        {
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawElement)
            {
                return;
            }

            // Adjust the padding for the frame and window
            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 2f) * Fugui.CurrentContext.Scale);
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f) * Fugui.CurrentContext.Scale);

            // Begin the combobox
            if (ImGui.BeginCombo(text, selectedItemText))
            {
                // Pop the padding styles
                Fugui.PopStyle();
                IsInsidePopUp = true;
                // execute the callback
                callback?.Invoke();
                // Set the IsInsidePopUp flag to false
                IsInsidePopUp = false;

                // Check if the CurrentPopUpID is not equal to the given text
                if (CurrentPopUpID != text)
                {
                    // Set the CurrentPopUpWindowID to the current drawing window ID
                    CurrentPopUpWindowID = FuWindow.CurrentDrawingWindow?.ID;
                    // Set the CurrentPopUpID to the given text
                    CurrentPopUpID = text;
                }
                // Set CurrentPopUpRect to ImGui item rect
                CurrentPopUpRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                // End the combobox
                ImGui.EndCombo();
            }
            else
            {
                // Pop the padding styles
                Fugui.PopStyle();
                // Check if the CurrentPopUpID is equal to the given text
                if (CurrentPopUpID == text)
                {
                    // Set the CurrentPopUpWindowID to null
                    CurrentPopUpWindowID = null;
                    // Set the CurrentPopUpID to null
                    CurrentPopUpID = null;
                }
            }
            // Pop the padding styles
            Fugui.PopStyle();
            // set states for this element
            setBaseElementState(text, _currentItemStartPos, ImGui.GetItemRectMax() - _currentItemStartPos, true, false);
            // Display the tooltip
            displayToolTip();
            // End the element with the current combobox style
            endElement(style);
        }
        #endregion
    }
}