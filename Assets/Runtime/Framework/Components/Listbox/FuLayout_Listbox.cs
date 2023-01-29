using Fu.Core;
using ImGuiNET;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fu.Framework
{
    public partial class FuLayout
    {
        #region Generic Types List
        /// <summary>
        /// This method creates a Listbox with the given text and items.
        /// </summary>
        /// <param name="text">The text to be displayed above the listbox.</param>
        /// <param name="items">The list of items to be displayed in the listbox.</param>
        public void Listbox<T>(string text, List<T> items)
        {
            Listbox<T>(text, items, null, null, FuListboxStyle.Default);
        }

        /// <summary>
        /// This method creates a Listbox with the given text and items, with specific height, and allows for actions to be performed when an item is selected and when the current item is retrieved.
        /// </summary>
        /// <param name="text">The text to be displayed above the listbox.</param>
        /// <param name="items">The list of items to be displayed in the listbox.</param>
        /// <param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="height">The height of the listbox component.</param>
        /// <param name="itemGetter">A function that returns the current item.</param>
        public void Listbox<T>(string text, List<T> items, Action<T> itemChange, int height, Func<T> itemGetter = null)
        {
            Listbox<T>(text, items, itemChange, itemGetter, FuListboxStyle.Default);
        }

        /// <summary>
        /// This method creates a Listbox with the given text and items, and allows for actions to be performed when an item is selected and when the current item is retrieved
        /// </summary>
        /// <param name="text">The text to be displayed above the listbox.</param>
        /// <param name="items">The list of items to be displayed in the listbox.</param>
        /// <param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="itemGetter">A function that returns the current item.</param>
        public void Listbox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter = null)
        {
            Listbox<T>(text, items, itemChange, itemGetter, FuListboxStyle.Default);
        }

        /// <summary>
        /// This method creates a Listbox with the given text and items, and allows for actions to be performed when an item is selected and when the current item is retrieved, with a specified style.
        /// </summary>
        /// <param name="text">The text to be displayed above the listbox.</param>
        /// <param name="items">The list of items to be displayed in the listbox.</param>
        /// <param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="itemGetter">A function that returns the current item.</param>
        /// <param name="style">The style of the listbox.</param>
        public void Listbox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter, FuListboxStyle style)
        {
            List<IListboxItem> cItems = new List<IListboxItem>();
            foreach (T item in items)
            {
                if (item is IListboxItem)
                {
                    cItems.Add((IListboxItem)item);
                }
                else
                {
                    cItems.Add(new FuListboxTextItem(Fugui.AddSpacesBeforeUppercase(item.ToString()), true));
                }
            }

            _customListbox(text, cItems, (index) =>
            {
                itemChange?.Invoke(items[index]);
            }, () => { return itemGetter?.Invoke()?.ToString(); }, style);
        }
        #endregion

        #region IListboxItems
        /// <summary>
        /// This method creates a custom listbox with the given parameters.
        /// </summary>
        /// <param name="text">The label text for the listbox.</param>
        /// <param name="items">A list of items to be displayed in the listbox.</param>
        /// <param name="itemChange">An action that is invoked when the selected item in the listbox changes.</param>
        /// <param name="itemGetter">A function that returns the currently selected item in the listbox as a string.</param>
        /// <param name="style">The style for the listbox.</param>
        protected virtual void _customListbox(string text, List<IListboxItem> items, Action<int> itemChange, Func<string> itemGetter, FuListboxStyle style)
        {
            text = beginElement(text, style);

            if (!_listboxSelectedItem.ContainsKey(text))
            {
                _listboxSelectedItem.Add(text, 0);
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
                            _listboxSelectedItem[text] = i;
                            break;
                        }
                        i++;
                    }
                }
            }

            int selectedIndex = _listboxSelectedItem[text];

            if (selectedIndex >= items.Count)
            {
                selectedIndex = items.Count - 1;
            }

            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 2f) * Fugui.CurrentContext.Scale);
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f) * Fugui.CurrentContext.Scale);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);

            if (ImGui.BeginListBox("##" + text))
            {
                // Pop the style to use the default style for the listbox
                Fugui.PopStyle();
                IsInsidePopUp = true;
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].DrawItem(i == selectedIndex) && items[i].Enabled)
                    {
                        // Update the selected index and perform the item change action
                        selectedIndex = i;
                        _listboxSelectedItem[text] = selectedIndex;
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
                ImGui.EndListBox();
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

        #region Fully custom listbox content
        /// <summary>
        /// Displays a listbox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the listbox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">The callback function that is called when an item is selected</param>
        /// <param name="height">The height of the list of items</param>
        public void Listbox(string text, string selectedItemText, Action callback, int height = 0)
        {
            Listbox(text, selectedItemText, callback, FuListboxStyle.Default, height);
        }

        /// <summary>
        /// Displays a listbox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the listbox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">The callback function that is called when an item is selected</param>
        /// <param name="style">The style of the listbox</param>
        /// <param name="height">The height of the list of items</param>
        public virtual void Listbox(string text, string selectedItemText, Action callback, FuListboxStyle style, int height = 0)
        {
            text = beginElement(text, style);
            height =(int)(height * Fugui.CurrentContext.Scale);
            // Adjust the padding for the frame and window
            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 2f) * Fugui.CurrentContext.Scale);
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f) * Fugui.CurrentContext.Scale);

            // Begin the listbox
            if (ImGui.BeginListBox(text))
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
                ImGui.EndListBox();
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