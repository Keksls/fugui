using Fu.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        #region Enums
        /// <summary>
        /// Displays a combobox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        public void ComboboxEnum<TEnum>(string text, Action<TEnum> itemChange, Func<TEnum> itemGetter = null) where TEnum : struct, IConvertible
        {
            ComboboxEnum<TEnum>(text, itemChange, itemGetter, FuComboboxStyle.Default);
        }

        /// <summary>
        /// Displays a combobox with all the enum values of type TEnum. The selected item can be changed by the user, and the change will be reported through the itemChange action.
        /// </summary>
        /// <typeparam name="TEnum">The type of the enum that will be displayed in the combobox. It must be an enumerated type.</typeparam>
        /// <param name="text">The label text to be displayed next to the combobox</param>
        /// <param name="itemChange">The action that will be called when the selected item changes</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        /// <param name="style">The style to be applied to the combobox</param>
        public void ComboboxEnum<TEnum>(string text, Action<TEnum> itemChange, Func<TEnum> itemGetter, FuComboboxStyle style) where TEnum : struct, IConvertible
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new ArgumentException("TEnum must be an enumerated type");
            }
            // list to store the enum values
            List<TEnum> enumValues = new List<TEnum>();
            // list to store the combobox items
            List<IComboboxItem> cItems = new List<IComboboxItem>();
            // iterate over the enum values and add them to the lists
            foreach (TEnum enumValue in Enum.GetValues(typeof(TEnum)))
            {
                enumValues.Add(enumValue);
                cItems.Add(new FuComboboxTextItem(Fugui.AddSpacesBeforeUppercase(enumValue.ToString()), true));
            }
            // call the custom combobox function, passing in the lists and the itemChange
            _customCombobox(text, cItems, (index) =>
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
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        public void Combobox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter = null)
        {
            Combobox<T>(text, items, itemChange, itemGetter, FuComboboxStyle.Default);
        }

        /// <summary>
        /// Displays a dropdown box with a list of items of type T and calls the specified action with the selected item when changed.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="text">The label of the dropdown box.</param>
        /// <param name="items">The list of items to display in the dropdown box.</param>
        /// <param name="itemChange">The action to call when the selected item changes.</param>
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        /// <param name="style">The style to use for the dropdown box.</param>
        public void Combobox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter, FuComboboxStyle style)
        {
            // Create a list of combobox items from the list of items
            List<IComboboxItem> cItems = new List<IComboboxItem>();
            foreach (T item in items)
            {
                // the item is already a combobox item
                if (item is IComboboxItem)
                {
                    cItems.Add((IComboboxItem)item);
                }
                else
                {
                    // Add the item to the list of combobox items
                    cItems.Add(new FuComboboxTextItem(Fugui.AddSpacesBeforeUppercase(item.ToString()), true));
                }
            }
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
        /// <param name="itemGetter">A func that return a way to get current stored value for the combobox. can be null if combobox il not lined to an object's field</param>
        ///<param name="style">The style for the combobox element.</param>
        protected virtual void _customCombobox(string text, List<IComboboxItem> items, Action<int> itemChange, Func<string> itemGetter, FuComboboxStyle style)
        {
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawItem)
            {
                return;
            }

            if (!_comboSelectedIndices.ContainsKey(text))
            {
                // Initialize the selected index for the combobox
                _comboSelectedIndices.Add(text, 0);
            }

            // Set current item as setted by getter
            if (itemGetter != null)
            {
                int i = 0;
                string selectedItemString = itemGetter.Invoke();
                if (!string.IsNullOrEmpty(selectedItemString))
                {
                    selectedItemString = Fugui.AddSpacesBeforeUppercase(selectedItemString);
                    foreach (var item in items)
                    {
                        if (item.ToString() == selectedItemString)
                        {
                            _comboSelectedIndices[text] = i;
                            break;
                        }
                        i++;
                    }
                }
            }

            int selectedIndex = _comboSelectedIndices[text];

            if (selectedIndex >= items.Count)
            {
                selectedIndex = items.Count - 1;
            }

            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 2f) * Fugui.CurrentContext.Scale);
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f) * Fugui.CurrentContext.Scale);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);

            if (ImGui.BeginCombo("##" + text, items[selectedIndex].ToString()))
            {
                // Pop the style to use the default style for the combo dropdown
                Fugui.PopStyle();
                IsInsidePopUp = true;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].DrawItem(i == selectedIndex) && items[i].Enabled)
                    {
                        // Update the selected index and perform the item change action
                        selectedIndex = i;
                        _comboSelectedIndices[text] = selectedIndex;
                        itemChange?.Invoke(i);
                    }
                }
                IsInsidePopUp = false;

                // Update the current pop-up window and ID
                if (CurrentPopUpID != text)
                {
                    CurrentPopUpWindowID = FuWindow.CurrentDrawingWindow?.ID;
                    CurrentPopUpID = text;
                }
                // Set CurrentPopUpRect to ImGui item rect
                CurrentPopUpRect = new Rect(ImGui.GetWindowPos(), ImGui.GetWindowSize());
                ImGui.EndCombo();
            }
            else
            {
                // Pop the style to use the default style for the combo dropdown
                Fugui.PopStyle();
                if (CurrentPopUpID == text)
                {
                    CurrentPopUpWindowID = null;
                    CurrentPopUpID = null;
                }
            }
            Fugui.PopStyle();
            displayToolTip();
            endElement(style);
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
        /// <param name="height">The height of the list of items</param>
        public void Combobox(string text, string selectedItemText, Action callback, int height = 0)
        {
            Combobox(text, selectedItemText, callback, FuComboboxStyle.Default, height);
        }

        /// <summary>
        /// Displays a combobox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the combobox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">The callback function that is called when an item is selected</param>
        /// <param name="style">The style of the combobox</param>
        /// <param name="height">The height of the list of items</param>
        public virtual void Combobox(string text, string selectedItemText, Action callback, FuComboboxStyle style, int height = 0)
        {
            beginElement(ref text, style);
            // return if item must no be draw
            if (!_drawItem)
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
                // Check if a height has been specified
                if (height > 0)
                {
                    // Invoke the callback with a fixed height for the combobox
                    callback?.Invoke();
                }
                else
                {
                    // Invoke the callback without a fixed height for the combobox
                    callback?.Invoke();
                }
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
            // Display the tooltip
            displayToolTip();
            // End the element with the current combobox style
            endElement(style);
        }
        #endregion
    }
}