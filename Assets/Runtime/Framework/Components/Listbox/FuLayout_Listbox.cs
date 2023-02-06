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
        /// Draw a List box binded on an Enum
        /// </summary>
        /// <typeparam name="TEnum">Enum type to bind</typeparam>
        /// <param name="text">ID/Label if the Listbox</param>
        /// <param name="itemChange">whenever the selected value change (int is enum value)</param>
        /// <param name="itemGetter">must return the current selected value as text</param>
        public void Listbox<TEnum>(string text, Action<int> itemChange, Func<string> itemGetter) where TEnum : struct, IConvertible
        {
            Listbox<TEnum>(text, itemChange, itemGetter, FuElementSize.FullSize);
        }

        /// <summary>
        /// Draw a List box binded on an Enum
        /// </summary>
        /// <typeparam name="TEnum">Enum type to bind</typeparam>
        /// <param name="text">ID/Label if the Listbox</param>
        /// <param name="itemChange">whenever the selected value change (int is enum value)</param>
        /// <param name="itemGetter">must return the current selected value as text</param>
        /// <param name="size">size of the listbox</param>
        public void Listbox<TEnum>(string text, Action<int> itemChange, Func<string> itemGetter, FuElementSize size) where TEnum : struct, IConvertible
        {
            FuSelectableBuilder.BuildFromEnum<TEnum>(out List<int> enumValues, out List<IFuSelectable> enumSelectables);

            // call the custom combobox function, passing in the lists and the itemChange
            _customListbox(text, enumSelectables, (index) =>
            {
                itemChange?.Invoke(enumValues[index]);
            }, itemGetter, size);
        }
        #endregion

        #region Generic Types List
        /// <summary>
        /// Draw a Listbox with the given text and items, and allows for actions to be performed when an item is selected and when the current item is retrieved, with a specified style.
        /// </summary>
        /// <param name="text">The text to be displayed above the listbox.</param>
        /// <param name="items">The list of items to be displayed in the listbox.</param>
        /// <param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="itemGetter">A function that returns the current item.</param>
        /// <param name="listUpdated">whatever the list has been updated since last call (list or values inside. it's for performances on large. You can handle it using ObservableCollections)
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        public void Listbox<T>(string text, List<T> items, Action<T> itemChange = null, Func<T> itemGetter = null, Func<bool> listUpdated = null)
        {
            Listbox(text, items, itemChange, itemGetter, FuElementSize.FullSize, listUpdated);
        }

        /// <summary>
        /// Draw a Listbox with the given text and items, and allows for actions to be performed when an item is selected and when the current item is retrieved, with a specified style.
        /// </summary>
        /// <param name="text">The text to be displayed above the listbox.</param>
        /// <param name="items">The list of items to be displayed in the listbox.</param>
        /// <param name="itemChange">The action to be performed when an item is selected.</param>
        /// <param name="itemGetter">A function that returns the current item.</param>
        /// <param name="size">The size of the list of items</param>
        /// <param name="listUpdated">whatever the list has been updated since last call (list or values inside. it's for performances on large. You can handle it using ObservableCollections)
        /// If you keep it as null, values will be reprocess each frames (better accuratie, but can lead on slowing down on large lists)</param>
        public void Listbox<T>(string text, List<T> items, Action<T> itemChange, Func<T> itemGetter, FuElementSize size, Func<bool> listUpdated = null)
        {
            List<IFuSelectable> cItems = FuSelectableBuilder.BuildFromList<T>(text, items, listUpdated?.Invoke() ?? true);
            _customListbox(text, cItems, (index) =>
            {
                itemChange?.Invoke(items[index]);
            }, () => { return itemGetter?.Invoke()?.ToString(); }, size);
        }
        #endregion

        #region IListboxItems
        /// <summary>
        /// Draw a custom listbox with the given parameters.
        /// </summary>
        /// <param name="text">The label text for the listbox.</param>
        /// <param name="items">A list of items to be displayed in the listbox.</param>
        /// <param name="itemChange">An action that is invoked when the selected item in the listbox changes.</param>
        /// <param name="itemGetter">A function that returns the currently selected item in the listbox as a string.</param>
        /// <param name="size">The size of the list of items</param>
        protected virtual void _customListbox(string text, List<IFuSelectable> items, Action<int> itemChange, Func<string> itemGetter, FuElementSize size)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawItem)
            {
                return;
            }

            if (!_selectableSelectedIndices.ContainsKey(text))
            {
                _selectableSelectedIndices.Add(text, 0);
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
                            _selectableSelectedIndices[text] = i;
                            break;
                        }
                        i++;
                    }
                }
            }

            int selectedIndex = _selectableSelectedIndices[text];
            if (selectedIndex >= items.Count)
            {
                selectedIndex = items.Count - 1;
            }

            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 2f) * Fugui.CurrentContext.Scale);
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f) * Fugui.CurrentContext.Scale);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().x);

            if (ImGui.BeginListBox("##" + text, size))
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
                        _selectableSelectedIndices[text] = selectedIndex;
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
            endElement();
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
        public virtual void Listbox(string text, string selectedItemText, Action callback)
        {
            Listbox(text, selectedItemText, callback, FuElementSize.FullSize);
        }

        /// <summary>
        /// Displays a listbox that allows the user to choose from a list of predefined items. 
        /// When an item is selected, the specified callback function is called.
        /// </summary>
        /// <param name="text">The label displayed next to the listbox</param>
        /// <param name="selectedItemText">The currently selected item</param>
        /// <param name="callback">The callback function that is called when an item is selected</param>
        /// <param name="size">The size of the list of items</param>
        public virtual void Listbox(string text, string selectedItemText, Action callback, FuElementSize size)
        {
            beginElement(ref text);
            // return if item must no be draw
            if (!_drawItem)
            {
                return;
            }

            // Adjust the padding for the frame and window
            Fugui.Push(ImGuiStyleVar.FramePadding, new Vector2(8f, 2f) * Fugui.CurrentContext.Scale);
            Fugui.Push(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f) * Fugui.CurrentContext.Scale);

            // Begin the listbox
            if (ImGui.BeginListBox(text, size))
            {
                // Pop the padding styles
                Fugui.PopStyle();
                IsInsidePopUp = true;
                // Invoke the callback
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
            endElement();
        }
        #endregion
    }
}